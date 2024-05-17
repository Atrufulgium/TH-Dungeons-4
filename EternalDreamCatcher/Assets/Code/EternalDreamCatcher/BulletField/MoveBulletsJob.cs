﻿using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletField {

    /// <summary>
    /// A job that moves all active bullets.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct MoveBulletsJob : IJob {

        public NativeArray<float> bulletXs;
        public NativeArray<float> bulletYs;
        [ReadOnly]
        public NativeArray<float> bulletMovementXs;
        [ReadOnly]
        public NativeArray<float> bulletMovementYs;
        [ReadOnly]
        public NativeReference<int> activeBullets;

        [SkipLocalsInit]
        public unsafe void Execute() {
            // This is correct as long as the BulletField.MAX_BULLETS is a four-multiple.
            float4* x = (float4*)bulletXs.GetUnsafePtr();
            float4* y = (float4*)bulletYs.GetUnsafePtr();
            float4* dx = (float4*)bulletMovementXs.GetUnsafeReadOnlyPtr();
            float4* dy = (float4*)bulletMovementYs.GetUnsafeReadOnlyPtr();

            int max = activeBullets.Value / 4;
            if (activeBullets.Value % 4 != 0)
                max++;

            for (int i = 0; i < max; i++) {
                x[i] += dx[i];
                y[i] += dy[i];
            }
        }

        /// <summary>
        /// Updates all bullet positions in a given field. This is executed on
        /// the <b>same thread</b>, typically the main thread, which you might
        /// not want.
        /// </summary>
        public static void Run(Field field) {
            var job = new MoveBulletsJob() {
                bulletXs = field.x,
                bulletYs = field.y,
                bulletMovementXs = field.dx,
                bulletMovementYs = field.dy,
                activeBullets = new(field.Active, Allocator.TempJob)
            };
            job.Run();
            job.activeBullets.Dispose();
        }
    }
}
