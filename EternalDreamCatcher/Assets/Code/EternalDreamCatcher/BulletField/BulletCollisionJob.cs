using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletField {

    /// <summary>
    /// This jobs checks whether a bullet intersects a circle.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct BulletCollisionJob : IJob {

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

        [SkipLocalsInit]
        public unsafe void Execute() {
            float4* xs = (float4*)bulletXs.GetUnsafeReadOnlyPtr();
            float4* ys = (float4*)bulletYs.GetUnsafeReadOnlyPtr();
            float4* rs = (float4*)bulletRadii.GetUnsafeReadOnlyPtr();

            int max = activeBullets.Value / 4;
            if (activeBullets.Value % 4 != 0)
                max++;

            float2 pos = target.Value.Center;

            for (int i = 0; i < max; i++) {
                float4 dx = pos.x - xs[i];
                float4 dy = pos.y - ys[i];
                float4 r = rs[i] + target.Value.Radius;
                bool4 res = (dx * dx + dy * dy < r * r);
                if (Hint.Unlikely(math.any(res))) {
                    if (res.x) collided.Add((BulletReference)(i * 4 + 0));
                    if (res.y) collided.Add((BulletReference)(i * 4 + 1));
                    if (res.z) collided.Add((BulletReference)(i * 4 + 2));
                    if (res.w) collided.Add((BulletReference)(i * 4 + 3));
                }
            }
        }

        /// <summary>
        /// Checks for all bullets in the field whether they hit <paramref name="hitbox"/>.
        /// Bullets that hit get put into <paramref name="collided"/>, a list
        /// that is cleared at the start of the job.
        /// </summary>
        public static void Run(Field field, Circle hitbox, NativeList<BulletReference> collided) {
            collided.Clear();
            var job = new BulletCollisionJob() {
                bulletXs = field.x,
                bulletYs = field.y,
                bulletRadii = field.radius,
                activeBullets = new(field.Active, Allocator.TempJob),

                target = new(hitbox, Allocator.TempJob),
                collided = collided
            };
            job.Run();
            job.activeBullets.Dispose();
            job.target.Dispose();
        }
    }
}
