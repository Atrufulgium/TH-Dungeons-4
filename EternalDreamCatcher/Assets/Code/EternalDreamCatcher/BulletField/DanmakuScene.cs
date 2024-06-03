using Atrufulgium.EternalDreamCatcher.Base;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Atrufulgium.EternalDreamCatcher.BulletField {

    /// <summary>
    /// A <see cref="Field"/> consists of just the bullet data.
    /// <br/>
    /// A <see cref="DanmakuScene{TGameInput}"/> consists of everything in addition, the
    /// player, the enemy/ies, and basically everything else 2D that does
    /// something and/or has to be rendered.
    /// </summary>
    // (I like how the job system makes interface boxing *explicit* and has you
    //  manually require the interface to be unmanaged as a generic.)
    // Unfortunately need to split the class into a non-generic and generic
    // part because of the TGameInput bit.
    public abstract class DanmakuScene : IDisposable {

        public const int MAX_BULLETS = Field.MAX_BULLETS;
        protected readonly Field bulletField = new(true);
        protected readonly FieldRenderer bulletFieldRenderer;

        protected Mesh quadMesh;
        protected Material entityMaterial;
        protected Texture2D[] entityTextures;

        protected NativeReference<Player> player;
        protected NativeReference<int> gameTick;

        protected NativeReference<Circle> playerHitbox;
        protected NativeReference<Circle> playerGrazebox;
        protected NativeList<BulletReference> playerHitboxResult;
        protected NativeList<BulletReference> playerGrazeboxResult;

        readonly int entityTexID;
        readonly int entityPosScaleID;

        public DanmakuScene(
            Mesh quadMesh,
            Material bulletMaterial,
            Texture2D[] bulletTextures,
            Material entityMaterial,
            Texture2D[] entityTextures,
            NativeReference<Player> player
        ) {
            bulletFieldRenderer = new(bulletMaterial, quadMesh, bulletTextures);
            this.quadMesh = quadMesh;
            this.entityMaterial = entityMaterial;
            this.entityTextures = entityTextures;

            this.player = player;
            gameTick = new(0, Allocator.Persistent);

            playerHitbox = new(Allocator.Persistent);
            playerGrazebox = new(Allocator.Persistent);
            playerHitboxResult = new(32, Allocator.Persistent);
            playerGrazeboxResult = new(512, Allocator.Persistent);

            entityTexID = Shader.PropertyToID("_EntityTex");
            entityPosScaleID = Shader.PropertyToID("_EntityPosScale");
        }

        /// <inheritdoc cref="ScheduleTick(int, JobHandle)"/>
        public abstract JobHandle ScheduleTick(JobHandle dep = default);

        /// <summary>
        /// Proceeds the simulation of this DanmakuScene. Returns a
        /// <see cref="JobHandle"/> of the <b>scheduled</b> job.
        /// </summary>
        /// <param name="ticks">
        /// A strictly positive number of ticks to handle.
        /// A tick represents 1/60th of a second.
        /// </param>
        /// <param name="dep">
        /// An optional job to make this job depend on.
        /// </param>
        public JobHandle ScheduleTick(int ticks, JobHandle dep = default) {
            if (ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticks), "Can only tick a positive number of times.");

            ref JobHandle handle = ref dep;
            for (int i = 0; i < ticks; i++)
                handle = ScheduleTick(dep);
            return handle;
        }

        /// <inheritdoc cref="Field.CreateBullet(ref BulletCreationParams)"/>
        public BulletReference? CreateBullet(ref BulletCreationParams bullet)
            => bulletField.CreateBullet(ref bullet);

        /// <summary>
        /// Adds commands to a buffer to fully render this scene onto the
        /// current active render target.
        /// </summary>
        public unsafe void Render(CommandBuffer buffer) {
            var p = player.GetUnsafeTypedPtr();
            // For the time being put player rendering here.
            // Once I get a more general "renderable entities" system, move it.
            // Bullets are more important than any entity, so put them below.
            buffer.SetGlobalTexture(entityTexID, entityTextures[0]);
            buffer.SetGlobalVector(entityPosScaleID, new float4(p->position, 0, 1));
            buffer.DrawMesh(
                quadMesh, matrix: default, entityMaterial, 0, 0
            );

            bulletFieldRenderer.RenderField(bulletField, buffer);
        }

        public void Dispose() {
            bulletField.Dispose();
            bulletFieldRenderer.Dispose();
            gameTick.Dispose();
            playerHitbox.Dispose();
            playerGrazebox.Dispose();
            playerHitboxResult.Dispose();
            playerGrazeboxResult.Dispose();
        }
    }

    public class DanmakuScene<TGameInput> : DanmakuScene where TGameInput : unmanaged, IGameInput {
        NativeReference<TGameInput> input;

        public DanmakuScene(
            Mesh rectMesh,
            Material bulletMaterial,
            Texture2D[] bulletTextures,
            Material entityMaterial,
            Texture2D[] entityTextures,
            NativeReference<TGameInput> input,
            NativeReference<Player> player
        ) : base(rectMesh, bulletMaterial, bulletTextures, entityMaterial, entityTextures, player) {
            this.input = input;
        }

        public override JobHandle ScheduleTick(JobHandle dep = default) {
            // NOTE: CombineDependencies may cause jobs to run already, as in
            // JobHandle.ScheduleBatchedJobs().
            // Look into the performance implications of this.
            ref JobHandle handle = ref dep;

            // Update positions and VMs
            var moveHandle1 = new MoveBulletsJob(in bulletField).Schedule(handle);
            var moveHandle2 = new MovePlayerJob<TGameInput>(
                in player, in input,
                in playerHitbox, in playerGrazebox
            ).Schedule(handle);
            handle = JobHandle.CombineDependencies(moveHandle1, moveHandle2);

            // Handle VM messages and `affectedBullet` sets

            // Check player collisions (note that bullets may be deleted already)
            var collideHandle1 = new BulletCollisionJob(in bulletField, in playerHitbox, playerHitboxResult).Schedule(handle);
            var collideHandle2 = new BulletCollisionJob(in bulletField, in playerGrazebox, playerGrazeboxResult).Schedule(handle);
            handle = JobHandle.CombineDependencies(collideHandle1, collideHandle2);

            // Post-process deletions
            handle = new PostProcessPlayerCollisionJob(
                player, in bulletField,
                playerHitboxResult, playerGrazeboxResult
            ).Schedule(handle);
            handle = new PostProcessDeletionsJob(in bulletField).Schedule(handle);

            // Prepare the next frame
            handle = new IncrementTickJob(gameTick, input).Schedule(handle);

            return handle;
        }

        /// <summary>
        /// Increments the current game tick both generally and for the game
        /// input.
        /// </summary>
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
        private struct IncrementTickJob : IJob {

            public NativeReference<int> gameTick;
            public NativeReference<TGameInput> input;

            public IncrementTickJob(NativeReference<int> gameTick, NativeReference<TGameInput> input) {
                this.gameTick = gameTick;
                this.input = input;
            }

            public unsafe void Execute() {
                gameTick.Value++;
                input.GetUnsafeTypedPtr()->GameTick = gameTick.Value;
            }
        }

        /// <summary>
        /// Calls <see cref="Field.FinalizeDeletion"/>.
        /// </summary>
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
        private struct PostProcessDeletionsJob : IJob {
            public Field field;
            public PostProcessDeletionsJob(in Field field) => this.field = field;
            public void Execute() => field.FinalizeDeletion();
        }
    }
}
