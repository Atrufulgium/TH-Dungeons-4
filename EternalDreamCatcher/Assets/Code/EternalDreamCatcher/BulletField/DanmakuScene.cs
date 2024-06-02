using Atrufulgium.EternalDreamCatcher.Base;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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
    public class DanmakuScene<TGameInput> : IDisposable where TGameInput : unmanaged, IGameInput {

        public const int MAX_BULLETS = Field.MAX_BULLETS;

        readonly Field bulletField = new(true);
        readonly FieldRenderer bulletFieldRenderer;
        NativeReference<TGameInput> input;

        NativeReference<int> gameTick;

        public DanmakuScene(
            Material material,
            Mesh rectMesh,
            Texture2D[] bulletTextures,
            TGameInput input
        ) {
            bulletFieldRenderer = new(material, rectMesh, bulletTextures);
            this.input = new(input, Allocator.Persistent);

            gameTick = new(0, Allocator.Persistent);
        }

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

        /// <inheritdoc cref="ScheduleTick(int, JobHandle)"/>
        public JobHandle ScheduleTick(JobHandle dep = default) {
            ref JobHandle handle = ref dep;

            // (TODO: Actually multithread shit when getting to the VMs and
            //  player movement and collision and stuff.)
            handle = new MoveBulletsJob(in bulletField).Schedule(handle);

            handle = new IncrementTickJob(gameTick, input).Schedule(handle);

            return handle;
        }

        /// <inheritdoc cref="Field.CreateBullet(ref BulletCreationParams)"/>
        public BulletReference? CreateBullet(ref BulletCreationParams bullet)
            => bulletField.CreateBullet(ref bullet);

        /// <summary>
        /// Adds commands to a buffer to fully render this scene onto the
        /// current active render target.
        /// </summary>
        public void Render(CommandBuffer buffer) {
            bulletFieldRenderer.RenderField(bulletField, buffer);
        }

        public void Dispose() {
            bulletField.Dispose();
            bulletFieldRenderer.Dispose();
            input.Dispose();
            gameTick.Dispose();
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

            public void Execute() {
                gameTick.Value++;
                var input = this.input.Value;
                input.GameTick = gameTick.Value;
            }
        }
    }
}
