using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletEngine.TickStrategies;
using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// A <see cref="BulletField"/> consists of just the bullet data.
    /// <br/>
    /// A <see cref="DanmakuScene{TGameInput}"/> consists of everything in addition, the
    /// player, the enemy/ies, and basically everything else 2D that does
    /// something and/or has to be rendered.
    /// </summary>
    // (I like how the job system makes interface boxing *explicit* and has you
    //  manually require the interface to be unmanaged as a generic.)
    // Unfortunately need to split the class into a non-generic and generic
    // part because of the TGameInput bit.
    // Even if we later have to unroll this generic again in the overwritten
    // `DanmakuScene<T>.ScheduleTick()`, I can't just use
    // NativeReference<IGameInput> as that arg may be managed, blegh.
    // We _need_ the generic constraint to satisfy NativeReference<>.
    public abstract class DanmakuScene : IDisposable {

        public const int MAX_BULLETS = BulletField.MAX_BULLETS;
        internal readonly BulletField bulletField = new(true);
        protected readonly BulletFieldRenderer bulletFieldRenderer;

        protected Mesh quadMesh;
        protected Material entityMaterial;
        protected Texture2D[] entityTextures;

        internal NativeReference<Player> player;
        internal NativeReference<int> gameTick;

        internal NativeReference<Circle> playerHitbox;
        internal NativeReference<Circle> playerGrazebox;
        internal NativeList<BulletReference> playerHitboxResult;
        internal NativeList<BulletReference> playerGrazeboxResult;

        protected VMList templates;
        /*internal*/public NativeList<VM> activeVMs;

        // The VMsCommandsJob needs this array.
        internal NativeList<BulletReference> createdBullets;

        readonly int gameTickID;
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

            templates = new(Array.Empty<string>(), Array.Empty<VM>());
            activeVMs = new(Allocator.Persistent);

            createdBullets = new(Allocator.Persistent);

            gameTickID = Shader.PropertyToID("_BulletTime");
            entityTexID = Shader.PropertyToID("_EntityTex");
            entityPosScaleID = Shader.PropertyToID("_EntityPosScale");
        }

        public abstract JobHandle ScheduleTick(JobHandle dep = default);
        public abstract JobHandle ScheduleTick(int ticks, JobHandle dep = default);

        /// <inheritdoc cref="BulletField.CreateBullet(ref BulletCreationParams)"/>
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
            buffer.SetGlobalFloat(gameTickID, gameTick.Value / 60f);
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
            templates.Dispose();
            activeVMs.Dispose();
            createdBullets.Dispose();
        }
    }

    public class DanmakuScene<TGameInput> : DanmakuScene where TGameInput : unmanaged, IGameInput {
        internal NativeReference<TGameInput> input;

        public ITickStrategy<TGameInput> TickStrategy { get; set; }

        public DanmakuScene(
            Mesh rectMesh,
            Material bulletMaterial,
            Texture2D[] bulletTextures,
            Material entityMaterial,
            Texture2D[] entityTextures,
            NativeReference<TGameInput> input,
            NativeReference<Player> player,
            ITickStrategy<TGameInput> tickStrategy
        ) : base(rectMesh, bulletMaterial, bulletTextures, entityMaterial, entityTextures, player) {
            this.input = input;
            TickStrategy = tickStrategy;
        }

        public override JobHandle ScheduleTick(JobHandle dep = default)
            => TickStrategy.ScheduleTick(this, dep);

        public override JobHandle ScheduleTick(int ticks, JobHandle dep = default)
            => TickStrategy.ScheduleTick(this, dep, ticks);
    }
}
