using Atrufulgium.EternalDreamCatcher.Base;
using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Atrufulgium.EternalDreamCatcher.BulletField {
    public class FieldRenderer : IDisposable {

        // x, y, scale, rot
        readonly ComputeBuffer transformBuffer = new(Field.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> transformBufferValues = new(Field.MAX_BULLETS, Allocator.Persistent);

        readonly ComputeBuffer rBuffer = new(Field.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> rBufferValues = new(Field.MAX_BULLETS, Allocator.Persistent);
        readonly ComputeBuffer gBuffer = new(Field.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> gBufferValues = new(Field.MAX_BULLETS, Allocator.Persistent);

        NativeReference<int> active = new(Allocator.Persistent);
        // Layout of these long keys:
        // 32 bits layer; 16 bits additive 0/1; 16 bits texture ID
        readonly NativeParallelHashMap<long, int> countPerLayer = new(64, Allocator.Persistent);
        readonly NativeParallelHashMap<long, int> startingIndices = new(64, Allocator.Persistent);
        readonly NativeParallelHashSet<long> keysSet = new(64, Allocator.Persistent);
        readonly NativeList<long> sortedKeys = new(64, Allocator.Persistent);

        readonly Material material;
        readonly Mesh rectMesh;
        readonly Texture2D[] bulletTextures;

        readonly int transformsID;
        readonly int rotsID;
        readonly int scalesID;
        readonly int arrayOffsetID;
        readonly int mainTexID;
        readonly int rBufferID;
        readonly int gBufferID;

        public FieldRenderer(Material material, Mesh rectMesh, Texture2D[] bulletTextures) {
            this.material = material;
            this.rectMesh = rectMesh;
            this.bulletTextures = bulletTextures;

            transformsID = Shader.PropertyToID("_BulletTransforms");
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
                transformsC = transformBufferValues,
                rsC = rBufferValues,
                gsC = gBufferValues,
                countPerLayer = countPerLayer,
                startingIndices = startingIndices,
                keysSet = keysSet,
                sortedKeys = sortedKeys,

                bulletField = field
            }.Run();

            // Now render.
            // The buffers can just be set as they don't care about the order.
            // Our material however needs to take into account the layer and
            // texture data, giving `sorteds.Count` rendering passses.
            buffer.SetBufferData(transformBuffer, transformBufferValues, 0, 0, field.Active);
            buffer.SetGlobalBuffer(transformsID, transformBuffer);
            buffer.SetBufferData(rBuffer, rBufferValues, 0, 0, field.Active);
            buffer.SetGlobalBuffer(rBufferID, rBuffer);
            buffer.SetBufferData(gBuffer, gBufferValues, 0, 0, field.Active);
            buffer.SetGlobalBuffer(gBufferID, gBuffer);
            foreach (var key in sortedKeys) {
                buffer.SetGlobalTexture(mainTexID, bulletTextures[key & 0xffff]);
                buffer.SetGlobalInt(arrayOffsetID, startingIndices[key]);
                // Pass 0 is regular rendering, pass 1 is additive rendering.
                int pass = 0;
                if ((key & 0x1_0000) != 0)
                    pass = 1;
                buffer.DrawMeshInstancedProcedural(rectMesh, 0, material, pass, countPerLayer[key]);
            }
        }

        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct PrepareRenderFieldJob : IJob {

            public NativeArray<float4> transformsC;
            public NativeArray<float4> rsC;
            public NativeArray<float4> gsC;
            public NativeParallelHashMap<long, int> countPerLayer;
            public NativeParallelHashMap<long, int> startingIndices;
            public NativeParallelHashSet<long> keysSet;
            public NativeList<long> sortedKeys;

            public Field bulletField;

            public void Execute() {
                var transforms = (float4*)transformsC.GetUnsafePtr();
                var rs = (float4*)rsC.GetUnsafePtr();
                var gs = (float4*)gsC.GetUnsafePtr();

                var x = (float*)bulletField.x.GetUnsafeReadOnlyPtr();
                var y = (float*)bulletField.y.GetUnsafeReadOnlyPtr();
                var dx = (float*)bulletField.dx.GetUnsafeReadOnlyPtr();
                var dy = (float*)bulletField.dy.GetUnsafeReadOnlyPtr();
                var tex = (int*)bulletField.textureID.GetUnsafeReadOnlyPtr();
                var layer = (int*)bulletField.layer.GetUnsafeReadOnlyPtr();
                var r = (float4*)bulletField.innerColor.GetUnsafeReadOnlyPtr();
                var g = (float4*)bulletField.outerColor.GetUnsafeReadOnlyPtr();
                var prop = (MiscBulletProps*)bulletField.prop.GetUnsafeReadOnlyPtr();
                var renderScales = (float*)bulletField.renderScale.GetUnsafeReadOnlyPtr();

                // The idea: as we're copying over data _anyway_, might as well
                // order it for very easy draw calls.
                // First, count how many of each (textreID, layer) there are.
                // This allows us to make the `intermediatePosses` array have the
                // following shape:
                //   [(0,0) bullets] [(0,1) bullets] .. [(1,0) bullets] .. [etc]
                // which we can take into account when copying.
                // (This int-tuple is collapsed into a long.)
                countPerLayer.Clear();
                keysSet.Clear();
                sortedKeys.Clear();

                // Get how much space to allocate for each
                // (I could SIMD this one but ehhhh)
                for (int i = 0; i < bulletField.Active; i++) {
                    var key = GetKey(i, tex, layer, prop);
                    countPerLayer.Increment(key);
                    keysSet.Add(key);
                }

                foreach (var key in keysSet)
                    sortedKeys.Add(key);
                sortedKeys.Sort();

                // Calculate starting indices
                int cumulative = 0;
                startingIndices.Clear();
                foreach (var key in sortedKeys) {
                    startingIndices.Add(key, cumulative);
                    cumulative += countPerLayer[key];
                }

                // We're again going to recount `countPerLayer`.
                // This time because it has the meaning "this is the offset wrt
                // the current startingIndex".
                countPerLayer.Clear();

                for (int i = 0; i < bulletField.Active; i++) {
                    var key = GetKey(i, tex, layer, prop);
                    // (First increment, then -1 to guarantee existence.)
                    // (Yes, pure laziness.)
                    countPerLayer.Increment(key);
                    int bufferIndex = startingIndices[key] + countPerLayer[key] - 1;

                    // Now we can just set all data.
                    float4 transform = default;
                    transform.xy = new(x[i], y[i]);
                    transform.z = renderScales[i];
                    rs[bufferIndex] = r[i];
                    gs[bufferIndex] = g[i];

                    var p = prop[i];
                    // (i have trust issues with "hasflag")
                    if (Hint.Unlikely((p & MiscBulletProps.RotationFixed) != 0)) {
                        transform.w = 0;
                    } else if (Hint.Unlikely((p & MiscBulletProps.RotationContinuous) != 0)) {
                        transform.w = 230; // Message value.
                    } else if (Hint.Unlikely((p & MiscBulletProps.RotationWiggle) != 0)) {
                        transform.w = 231; // Message value.
                    } else {
                        // Mess with the angle so that "0" means "down".
                        transform.w = math.atan2(dy[i], dx[i]) - math.PI/2;
                    }
                    transforms[bufferIndex] = transform;
                }
            }

            public long GetKey(int i, int* tex, int* layer, MiscBulletProps* prop) {
                long ret = tex[i];
                if ((prop[i] & MiscBulletProps.RendersAdditiviely) != 0)
                    ret += (long)1 << 16;
                ret += (long)layer[i] << 32;
                return ret;
            }
        }

        public void Dispose() {
            transformBuffer.Dispose();
            transformBufferValues.Dispose();
            rBuffer.Dispose();
            rBufferValues.Dispose();
            gBuffer.Dispose();
            gBufferValues.Dispose();

            active.Dispose();

            countPerLayer.Dispose();
            startingIndices.Dispose();
            keysSet.Dispose();
            sortedKeys.Dispose();
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
