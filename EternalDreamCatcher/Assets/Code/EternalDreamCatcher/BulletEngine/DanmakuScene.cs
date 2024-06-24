using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using System;
using Unity.Burst;
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
    // TODO: As burst can't handle generics properly, just... don't?
    public abstract class DanmakuScene : IDisposable {

        public const int MAX_BULLETS = BulletField.MAX_BULLETS;
        protected readonly BulletField bulletField = new(true);
        protected readonly BulletFieldRenderer bulletFieldRenderer;

        protected Mesh quadMesh;
        protected Material entityMaterial;
        protected Texture2D[] entityTextures;

        protected NativeReference<Player> player;
        protected NativeReference<int> gameTick;

        protected NativeReference<Circle> playerHitbox;
        protected NativeReference<Circle> playerGrazebox;
        protected NativeList<BulletReference> playerHitboxResult;
        protected NativeList<BulletReference> playerGrazeboxResult;

        protected VMList templates;
        protected NativeList<VM> activeVMs;

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

            gameTickID = Shader.PropertyToID("_BulletTime");
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
#if UNITY_EDITOR
            // See the "TODO: NOTE: OBNOXIOUS:" comment below.
            if (ticks == 1)
                Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = true;
            else
                Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = false;
#endif

            ref JobHandle handle = ref dep;
            for (int i = 0; i < ticks; i++)
                handle = ScheduleTick(dep);
            return handle;
        }

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
            // WARNING: EVERY .Schedule() HERE MUST HAVE A HANDLE ARG
            // YOU"RE DOING IT WRONG OTHERWISE
            // You don't notice it until you run it at [ridiculus]x speed in
            // the player, but then you just freeze and all threads are Burst
            // and waiting. Notice that behaviour? This is the problem.
            
            // NOTE: CombineDependencies may cause jobs to run already, as in
            // JobHandle.ScheduleBatchedJobs().
            // Look into the performance implications of this.
            ref JobHandle handle = ref dep;

            // Run VMs
            // We cannot assume how many VMs there actually are, so schedule a
            // predictable, always-non-zero number of jobs.
            // Note: Scheduling X jobs with X available worker threads that are
            // all idle does not actually guarantee an even distribution of
            // work, but in the player the workers are saturated anyway so who
            // gives.
            int concurrentVMs = Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount;
            concurrentVMs = math.max(1, concurrentVMs);
            NativeArray<JobHandle> vmHandles = new(concurrentVMs, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < concurrentVMs; i++) {
                vmHandles[i] = new VMJobMany() {
                    vms = activeVMs,
                    jobIndex = i,
                    totalJobs = concurrentVMs
                }.Schedule(handle);
            }
            handle = JobHandle.CombineDependencies(vmHandles);
            vmHandles.Dispose();
            // TODO: Message handling, detecting and disabling VMs with zero
            // bullets. Unfortunately, both of these operations are sequental :(

            // Update positions
            var moveHandle1 = new MoveBulletsJob(in bulletField).Schedule(handle);
            var moveHandle2 = ScheduleMovePlayerJob(handle);
            handle = JobHandle.CombineDependencies(moveHandle1, moveHandle2);

            // Check player collisions (note that bullets may be deleted already)
            var collideHandle1 = new BulletCollisionJob(in bulletField, in playerHitbox, playerHitboxResult).Schedule(handle);
            var collideHandle2 = new BulletCollisionJob(in bulletField, in playerGrazebox, playerGrazeboxResult).Schedule(handle);
            handle = JobHandle.CombineDependencies(collideHandle1, collideHandle2);

            // Post-process deletions
            // TODO: NOTE: OBNOXIOUS: for both of these jobs, the jobs debugger
            // suddenly takes up an exponential amount of time verifying the
            // dependency graph. Scheduling 1 iteration, fine, 30 takes up the
            // entire frame, 60 crashes Unity.
            // The workaround: in the editor, toggle Jobs > JobsDebugger.
            // The ScheduleTick(int, JobHandle) overload automatically enables
            // it when doing one tick and disables it when doing multiple ticks.
            // This disables the race condition check however.
            // Disabled and I can run 500 iterations a frame no problem.
            handle = new PostProcessPlayerCollisionJob(
                player, in bulletField,
                playerHitboxResult, playerGrazeboxResult
            ).Schedule(handle);
            handle = new PostProcessDeletionsJob(in bulletField).Schedule(handle);

            // Prepare the next frame
            handle = new IncrementTickJob(gameTick, input).Schedule(handle);

            return handle;
        }

        // Burst can _only_ compile generic jobs whose constructor is of the
        // form `MyJob<ExplicitType>`. Constructors of the form `MyJob<T>` for
        // some ambient T type doesn't work. So...
        // Unroll the generic into explicit types. :(
        unsafe JobHandle ScheduleMovePlayerJob(JobHandle deps) {
            // Also, I _would_ use those nice is/switch expressions, but...
            // https://sharplab.io/#v2:D4AQzABAzgLgTgVwMYwgWQDwGkB8EDeAsAFARkTgQCWAdqlANwnkWRoAUmuEAtgJQFmLclAgBeXgDpGQsgF8SC4iUogATBADC2PEVLkueKk2X6ytVADF2AsUegB3KjCQALQWeGYLRjXYg0AKYO6Bg+7FRqfNIANLLCAPrieEEhhhHRUPFyJnJAA=
            // They box. This is a hot loop. Ffs.
            // Even uglier manual pointer shit it is.
            TGameInput* t = input.GetUnsafeTypedPtr();
            if (typeof(TGameInput) == typeof(KeyboardInput)) {
                return new MovePlayerJob<KeyboardInput>(
                    in player, (KeyboardInput*)t, in playerHitbox, in playerGrazebox
                ).Schedule(deps);
            }

            // If other, fall back to non-bursted code. The only downside is
            // the performance, which is much more acceptable than crashing.
            // Easily recognisable by large blue blobs instead of thin green
            // blobs in Unity's profiler.
            return new MovePlayerJob<TGameInput>(
                in player, t, in playerHitbox, in playerGrazebox
            ).Schedule(deps);
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
        /// Calls <see cref="BulletField.FinalizeDeletion"/>.
        /// </summary>
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
        private struct PostProcessDeletionsJob : IJob {
            public BulletField field;
            public PostProcessDeletionsJob(in BulletField field) => this.field = field;
            public void Execute() => field.FinalizeDeletion();
        }
    }
}
