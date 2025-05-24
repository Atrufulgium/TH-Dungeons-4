using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// A job that moves all active bullets.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct MoveBulletsJob : IJob {

        public NativeArray<float> bulletXs;
        public NativeArray<float> bulletYs;
        [ReadOnly]
        public NativeArray<float> bulletMovementXs;
        [ReadOnly]
        public NativeArray<float> bulletMovementYs;
        [ReadOnly]
        public NativeReference<int> activeBullets;

        public MoveBulletsJob(in BulletField field) {
            bulletXs = field.x;
            bulletYs = field.y;
            bulletMovementXs = field.dx;
            bulletMovementYs = field.dy;
            activeBullets = field.active;
        }

        public readonly void Execute() {
            MoveBulletsPass.Execute(
                in bulletXs,
                in bulletYs,
                in bulletMovementXs,
                in bulletMovementYs,
                in activeBullets
            );
        }

        /// <summary>
        /// Updates all bullet positions in a given field. This is executed on
        /// the <b>same thread</b>, typically the main thread, which you might
        /// not want.
        /// </summary>
        public static void Run(in BulletField field) {
            var job = new MoveBulletsJob(field);
            job.Run();
        }
    }

    /// <inheritdoc cref="MoveBulletsJob"/>
    internal static class MoveBulletsPass {
        [SkipLocalsInit]
        public static unsafe void Execute(
            in NativeArray<float> bulletXs,
            in NativeArray<float> bulletYs,
            in NativeArray<float> bulletMovementXs,
            in NativeArray<float> bulletMovementYs,
            in NativeReference<int> activeBullets
        ) {
            // This is correct as long as the BulletField.MAX_BULLETS is a four-multiple.
            float4* x = (float4*)bulletXs.GetUnsafePtr();
            float4* y = (float4*)bulletYs.GetUnsafePtr();
            float4* dx = (float4*)bulletMovementXs.GetUnsafeReadOnlyPtr();
            float4* dy = (float4*)bulletMovementYs.GetUnsafeReadOnlyPtr();

            // Yes, with this we also move bullets that are not actually active.
            // But that's fine.
            int active = activeBullets.Value;
            int max = active / 4;
            if (active % 4 != 0)
                max++;

            for (int i = 0; i < max; i++) {
                x[i] += dx[i];
                y[i] += dy[i];
            }

        }
    }
}
