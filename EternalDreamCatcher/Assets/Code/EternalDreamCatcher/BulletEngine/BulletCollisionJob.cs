using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// This jobs checks whether a bullet intersects a circle.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct BulletCollisionJob : IJob {
        [ReadOnly]
        public NativeArray<float> bulletXs;
        [ReadOnly]
        public NativeArray<float> bulletYs;
        [ReadOnly]
        public NativeArray<float> bulletRadii;
        [ReadOnly]
        public NativeReference<int> activeBullets;

        /// <summary>
        /// What hitbox to test intersection with.
        /// </summary>
        [ReadOnly]
        public NativeReference<Circle> target;
        /// <summary>
        /// A list of all bullets that collided with the target.
        /// </summary>
        [WriteOnly]
        public NativeList<BulletReference> collided;

        /// <summary>
        /// This job checks for all bullets in the field whether they hit <paramref name="hitbox"/>.
        /// Bullets that hit get put into <paramref name="collided"/>, a list
        /// that is cleared at the start of the job.
        /// </summary>
        public BulletCollisionJob(in BulletField field, in NativeReference<Circle> hitbox, NativeList<BulletReference> collided) {
            bulletXs = field.x;
            bulletYs = field.y;
            bulletRadii = field.radius;
            activeBullets = field.active;

            target = hitbox;
            this.collided = collided;
        }

        public readonly void Execute() {
            BulletCollisionPass.Execute(
                in bulletXs,
                in bulletYs,
                in bulletRadii,
                in activeBullets,
                in target,
                in collided
            );
        }

        /// <summary>
        /// Checks for all bullets in the field whether they hit <paramref name="hitbox"/>.
        /// Bullets that hit get put into <paramref name="collided"/>, a list
        /// that is cleared at the start of the job.
        /// </summary>
        public static void Run(
            in BulletField field,
            in Circle hitbox,
            in NativeList<BulletReference> collided
        ) {
            collided.Clear();
            var circle = new NativeReference<Circle>(hitbox, Allocator.TempJob);
            var job = new BulletCollisionJob(in field, circle, collided);
            job.Run();
            job.Dispose();
            circle.Dispose();
        }

        public void Dispose() {
            target.Dispose();
        }
    }

    /// <inheritdoc cref="BulletCollisionJob"/>
    internal static class BulletCollisionPass {
        [SkipLocalsInit]
        public static unsafe void Execute(
            in NativeArray<float> bulletXs,
            in NativeArray<float> bulletYs,
            in NativeArray<float> bulletRadii,
            in NativeReference<int> activeBullets,
            in NativeReference<Circle> hitbox,
            in NativeList<BulletReference> collided
        ) {
            float4* xs = (float4*)bulletXs.GetUnsafeReadOnlyPtr();
            float4* ys = (float4*)bulletYs.GetUnsafeReadOnlyPtr();
            float4* rs = (float4*)bulletRadii.GetUnsafeReadOnlyPtr();

            // Note that we need to exclude all bullets in the final SIMD bin
            // that don't actually exist.
            int active = activeBullets.Value;
            int max = active / 4;
            var mod = active % 4;
            bool4 finalIterMask = new(true, true, true, true);
            if (mod != 0) {
                finalIterMask = new(mod >= 1, mod >= 2, mod >= 3, false);
                max++;
            }

            float2 pos = hitbox.Value.Center;
            float radius = hitbox.Value.Radius;

            for (int i = 0; i < max; i++) {
                float4 dx = pos.x - xs[i];
                float4 dy = pos.y - ys[i];
                float4 r = rs[i] + radius;
                bool4 res = (dx * dx + dy * dy < r * r);

                if (Hint.Unlikely(i == max - 1))
                    res &= finalIterMask;
                if (Hint.Unlikely(math.any(res))) {
                    if (res.x) collided.Add((BulletReference)(i * 4 + 0));
                    if (res.y) collided.Add((BulletReference)(i * 4 + 1));
                    if (res.z) collided.Add((BulletReference)(i * 4 + 2));
                    if (res.w) collided.Add((BulletReference)(i * 4 + 3));
                }
            }
        }
    }
}
