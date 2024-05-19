using Atrufulgium.EternalDreamCatcher.Base;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Atrufulgium.EternalDreamCatcher.BulletField {
    public class FieldRenderer : IDisposable {

        readonly ComputeBuffer posBuffer = new(Field.MAX_BULLETS, sizeof(float) * 3);
        readonly NativeArray<float3> posBufferValues = new(Field.MAX_BULLETS, Allocator.Persistent);

        readonly ComputeBuffer rBuffer = new(Field.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> rBufferValues = new(Field.MAX_BULLETS, Allocator.Persistent);
        readonly ComputeBuffer gBuffer = new(Field.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> gBufferValues = new(Field.MAX_BULLETS, Allocator.Persistent);

        NativeReference<int> active = new(Allocator.Persistent);
        // (32 bits layer, 32 bits texture ID)
        readonly NativeParallelHashMap<long, int> countPerLayer = new(64, Allocator.Persistent);
        readonly NativeParallelHashMap<long, int> startingIndices = new(64, Allocator.Persistent);
        readonly NativeParallelHashSet<long> sorteds = new(64, Allocator.Persistent);

        readonly Material material;
        readonly Mesh mesh;
        readonly Texture2D[] bulletTextures;

        readonly int possesID;
        readonly int arrayOffsetID;
        readonly int mainTexID;
        readonly int rBufferID;
        readonly int gBufferID;

        public FieldRenderer(Material material, Mesh mesh, Texture2D[] bulletTextures) {
            this.material = material;
            this.mesh = mesh;
            this.bulletTextures = bulletTextures;

            possesID = Shader.PropertyToID("_BulletPosses");
            arrayOffsetID = Shader.PropertyToID("_InstanceOffset");
            mainTexID = Shader.PropertyToID("_BulletTex");
            rBufferID = Shader.PropertyToID("_BulletReds");
            gBufferID = Shader.PropertyToID("_BulletGreens");
        }

        /// <summary>
        /// Adds commands to a buffer to render a bullet field.
        /// This assumes the current render target is set correctly.
        /// </summary>
        public unsafe void RenderField(Field field, CommandBuffer buffer) {
            // TODO: This only works if
            /// <see cref="SystemInfo.supportsInstancing"/>
            // and
            /// <see cref="SystemInfo.supportsComputeShaders"/>
            // are true. Add a fallback.

            if (field.Active == 0)
                return;

            // Copy the field data orderly into buffers.
            active.Value = field.Active;
            new PrepareRenderFieldJob() {
                possesC = posBufferValues,
                rsC = rBufferValues,
                gsC = gBufferValues,
                countPerLayer = countPerLayer,
                startingIndices = startingIndices,
                sorteds = sorteds,

                active = active,
                xC = field.x,
                yC = field.y,
                zC = field.z,
                texC = field.textureID,
                layerC = field.layer,
                rC = field.innerColor,
                gC = field.outerColor,
            }.Run();

            // Now render.
            // The buffers can just be set as they don't care about the order.
            // Our material however needs to take into account the layer and
            // texture data, giving `sorteds.Count` rendering passses.
            buffer.SetBufferData(posBuffer, posBufferValues, 0, 0, field.Active);
            buffer.SetGlobalBuffer(possesID, posBuffer);
            buffer.SetBufferData(rBuffer, rBufferValues, 0, 0, field.Active);
            buffer.SetGlobalBuffer(rBufferID, rBuffer);
            buffer.SetBufferData(gBuffer, gBufferValues, 0, 0, field.Active);
            buffer.SetGlobalBuffer(gBufferID, gBuffer);
            foreach (var key in sorteds) {
                buffer.SetGlobalTexture(mainTexID, bulletTextures[key & 0xffff_ffff]);
                buffer.SetGlobalInt(arrayOffsetID, startingIndices[key]);
                buffer.DrawMeshInstancedProcedural(mesh, 0, material, -1, countPerLayer[key]);
            }
        }

        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct PrepareRenderFieldJob : IJob {

            public NativeArray<float3> possesC;
            public NativeArray<float4> rsC;
            public NativeArray<float4> gsC;
            public NativeParallelHashMap<long, int> countPerLayer;
            public NativeParallelHashMap<long, int> startingIndices;
            public NativeParallelHashSet<long> sorteds;

            [ReadOnly]
            public NativeReference<int> active;
            [ReadOnly]
            public NativeArray<float> xC;
            [ReadOnly]
            public NativeArray<float> yC;
            [ReadOnly]
            public NativeArray<float> zC;
            [ReadOnly]
            public NativeArray<int> texC;
            [ReadOnly]
            public NativeArray<int> layerC;
            [ReadOnly]
            public NativeArray<float4> rC;
            [ReadOnly]
            public NativeArray<float4> gC;

            public void Execute() {
                var posses = (float3*)possesC.GetUnsafePtr();
                var rs = (float4*)rsC.GetUnsafePtr();
                var gs = (float4*)gsC.GetUnsafePtr();

                var x = (float*)xC.GetUnsafeReadOnlyPtr();
                var y = (float*)yC.GetUnsafeReadOnlyPtr();
                var z = (float*)zC.GetUnsafeReadOnlyPtr();
                var tex = (int*)texC.GetUnsafeReadOnlyPtr();
                var layer = (int*)layerC.GetUnsafeReadOnlyPtr();
                var r = (float4*)rC.GetUnsafeReadOnlyPtr();
                var g = (float4*)gC.GetUnsafeReadOnlyPtr();

                // The idea: as we're copying over data _anyway_, might as well
                // order it for very easy draw calls.
                // First, count how many of each (textreID, layer) there are.
                // This allows us to make the `intermediatePosses` array have the
                // following shape:
                //   [(0,0) bullets] [(0,1) bullets] .. [(1,0) bullets] .. [etc]
                // which we can take into account when copying.
                // (This int-tuple is collapsed into a long.)
                countPerLayer.Clear();
                sorteds.Clear();

                // Get how much space to allocate for each
                // (I could SIMD this one but ehhhh)
                for (int i = 0; i < active.Value; i++) {
                    var key = ((long)layer[i] << 32) + tex[i];
                    countPerLayer.Increment(key);
                    sorteds.Add(key);
                }

                // Calculate starting indices
                // (Note that default tuple ordering is lexicographic. This matches
                //  our "higher layer = drawn on top".)
                int cumulative = 0;
                startingIndices.Clear();
                foreach (var key in sorteds) {
                    startingIndices.Add(key, cumulative);
                    cumulative += countPerLayer[key];
                }

                // We're again going to recount `countPerLayer`.
                // This time because it has the meaning "this is the offset wrt
                // the current startingIndex".
                countPerLayer.Clear();

                for (int i = 0; i < active.Value; i++) {
                    var key = ((long)layer[i] << 32) + tex[i];
                    // (First increment, then -1 to guarantee existence.)
                    // (Yes, pure laziness.)
                    countPerLayer.Increment(key);
                    int bufferIndex = startingIndices[key] + countPerLayer[key] - 1;
                    posses[bufferIndex] = new(x[i], y[i], z[i]);
                    rs[bufferIndex] = r[i];
                    gs[bufferIndex] = g[i];
                }
            }
        }

        public void Dispose() {
            posBuffer.Dispose();
            posBufferValues.Dispose();
            rBuffer.Dispose();
            rBufferValues.Dispose();
            gBuffer.Dispose();
            gBufferValues.Dispose();

            active.Dispose();

            countPerLayer.Dispose();
            startingIndices.Dispose();
            sorteds.Dispose();
        }

        /// <summary>
        /// Reads the current window's resolution, and calculates the size of
        /// the texture needed to render just the bullet field part.
        /// </summary>
        public static int2 GetBulletFieldResolution() {
            // TODO: 2x etc
            
            // The scaling is entirely wrt height currently.
            var screenHeight = Screen.currentResolution.height;
            // In the texture of 2560x1920, it takes up 1418x1664.
            float height = screenHeight * (1664f / 1920f);
            float width = height * (1418f / 1664f);
            return new((int)width, (int)height);
        }
    }
}
