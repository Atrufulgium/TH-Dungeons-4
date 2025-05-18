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

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    public class BulletFieldRenderer : IDisposable {

        // x, y, scale, rot
        readonly ComputeBuffer transformBuffer = new(BulletField.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> transformBufferValues = new(BulletField.MAX_BULLETS, Allocator.Persistent);

        readonly ComputeBuffer rBuffer = new(BulletField.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> rBufferValues = new(BulletField.MAX_BULLETS, Allocator.Persistent);
        readonly ComputeBuffer gBuffer = new(BulletField.MAX_BULLETS, sizeof(float) * 4);
        readonly NativeArray<float4> gBufferValues = new(BulletField.MAX_BULLETS, Allocator.Persistent);

        NativeReference<int> active = new(Allocator.Persistent);
        // Layout of these long keys:
        // 32 bits layer; 16 bits additive 0/1; 16 bits texture ID
        readonly NativeParallelHashMap<long, int> countPerLayer = new(64, Allocator.Persistent);
        readonly NativeParallelHashMap<long, int> startingIndices = new(64, Allocator.Persistent);
        readonly NativeList<long> sortedKeys = new(64, Allocator.Persistent);
        // We need an intermediate place to order the data.
        readonly NativeList<ComputeData> intermediate = new(BulletField.MAX_BULLETS, Allocator.Persistent);

        readonly Material material;
        readonly Mesh rectMesh;
        readonly Texture2D[] bulletTextures;

        readonly int transformsID;
        readonly int arrayOffsetID;
        readonly int mainTexID;
        readonly int rBufferID;
        readonly int gBufferID;

        public BulletFieldRenderer(Material material, Mesh rectMesh, Texture2D[] bulletTextures) {
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
        public unsafe void RenderField(BulletField field, CommandBuffer buffer) {
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
                sortedKeys = sortedKeys,
                intermediate = intermediate,

                bulletField = field
            }.Run();

            // Now render.
            // The buffers can just be set as they don't care about the order.
            // Our material however needs to take into account the layer and
            // texture data, giving `sorteds.Count` rendering passses.
            // TODO: Occassionally bullets flicker out of existence for a moment.
            // Also, sometimes they just appear late? This seems layer-related.
            // After exactly 160 ticks after start, a really obvious ofuda
            // appears with my current setup.
            // Is this an off-by-one? Do a frame-by-frame analysis of the first few seconds.
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

        private readonly struct ComputeData : IComparable<ComputeData> {
            public readonly float4 ComputeTransform; // x, y, scale, rot
            public readonly float4 ComputeRed; // What "red" turns into
            public readonly float4 ComputeGreen; // What "green" turns into
            public readonly long OrderKey; // (layer, textureID)
            public readonly long SuborderID; // monotonous

            public ComputeData(BulletField f, ushort bulletReference) {
                float4 transform = default;
                transform.xy = new(f.x[bulletReference], f.y[bulletReference]);
                transform.z = f.renderScale[bulletReference];

                var p = f.prop[bulletReference];
                // (bulletReference have trust issues with "hasflag")
                if (Hint.Unlikely((p & MiscBulletProps.RotationFixed) != 0)) {
                    transform.w = 0;
                } else if (Hint.Unlikely((p & MiscBulletProps.RotationContinuous) != 0)) {
                    transform.w = 230; // Message value.
                } else if (Hint.Unlikely((p & MiscBulletProps.RotationWiggle) != 0)) {
                    transform.w = 231; // Message value.
                } else {
                    // Mess with the angle so that "0" means "down".
                    transform.w = math.atan2(f.dy[bulletReference], f.dx[bulletReference]) - math.PI / 2;
                }

                ComputeTransform = transform;
                ComputeRed = f.innerColor[bulletReference];
                ComputeGreen = f.outerColor[bulletReference];

                OrderKey = BulletToKey(bulletReference, ref f);
                SuborderID = f.id[bulletReference];
            }

            public int CompareTo(ComputeData other) {
                var res = OrderKey.CompareTo(other.OrderKey);
                if (res != 0)
                    return res;
                return SuborderID.CompareTo(other.SuborderID);
            }

            /// <summary>
            /// Turns a bullet into its coarse draw order key.
            /// </summary>
            static unsafe long BulletToKey(int bulletReference, ref BulletField f) {
                long ret = f.textureID[bulletReference];
                if ((f.prop[bulletReference] & MiscBulletProps.RendersAdditiviely) != 0)
                    ret += (long)1 << 16;
                ret += (long)f.layer[bulletReference] << 32;
                return ret;
            }
        }

        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct PrepareRenderFieldJob : IJob {

            public NativeArray<float4> transformsC;
            public NativeArray<float4> rsC;
            public NativeArray<float4> gsC;
            public NativeParallelHashMap<long, int> countPerLayer;
            public NativeParallelHashMap<long, int> startingIndices;
            public NativeList<long> sortedKeys;
            public NativeList<ComputeData> intermediate;

            public BulletField bulletField;

            public void Execute() {
                var transforms = (float4*)transformsC.GetUnsafePtr();
                var rs = (float4*)rsC.GetUnsafePtr();
                var gs = (float4*)gsC.GetUnsafePtr();

                // The idea: as we're copying over data _anyway_, might as well
                // order it for very easy draw calls.
                // First, count how many of each (layer, textureID) there are.
                // This allows us to make the `intermediatePosses` array have the
                // following shape:
                //   [(0,0) bullets] [(0,1) bullets] .. [(1,0) bullets] .. [etc]
                // (This int-tuple is collapsed into a long.)
                // We can then make sure the data we copy over is sorted by
                // (layer, textureID, bulletID).
                // This third param is needed because otherwise bullet deletion
                // will cause flickering by putting other bullets on top
                // suddenly.
                countPerLayer.Clear();
                sortedKeys.Clear();
                startingIndices.Clear();

                // Add and sort the data.
                // (We always need to clear data but only need to do other
                //  stuff when there are active bullets.)
                intermediate.Clear();
                int active = bulletField.Active;
                if (active == 0)
                    return;

                for (ushort i = 0; i < active; i++) {
                    intermediate.Add(new(bulletField, i));
                }
                intermediate.Sort();

                // Copy into the buffers.
                // Keep track of the keys -- they are contiguous so adding
                // their metadata is easy.
                long currentKey = intermediate[0].OrderKey;
                int currentCount = 0;
                startingIndices.Add(currentKey, 0);
                for (int i = 0; i < active; i++) {
                    transforms[i] = intermediate[i].ComputeTransform;
                    rs[i] = intermediate[i].ComputeRed;
                    gs[i] = intermediate[i].ComputeGreen;
                    currentCount++;

                    if (currentKey != intermediate[i].OrderKey) {
                        sortedKeys.Add(currentKey);
                        countPerLayer.Add(currentKey, currentCount);
                        
                        currentKey = intermediate[i].OrderKey;
                        currentCount = 0;
                        startingIndices.Add(currentKey, i);
                    }
                }

                if (currentCount != 0) {
                    sortedKeys.Add(currentKey);
                    countPerLayer.Add(currentKey, currentCount);
                }
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
            sortedKeys.Dispose();
            intermediate.Dispose();
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
