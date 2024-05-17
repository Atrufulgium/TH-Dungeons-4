using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Atrufulgium.EternalDreamCatcher.BulletField {
    public class FieldRenderer : IDisposable {

        readonly ComputeBuffer posBuffer = new(Field.MAX_BULLETS, sizeof(float) * 3);
        readonly NativeArray<float3> intermediatePosses = new(Field.MAX_BULLETS, Allocator.Persistent);

        readonly Material material;
        readonly Mesh mesh;

        int possesID;

        public FieldRenderer(Material material, Mesh mesh) {
            this.material = material;
            this.mesh = mesh;

            possesID = Shader.PropertyToID("_BulletPosses");
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

            var posses = (float3*)intermediatePosses.GetUnsafePtr();
            var x = (float*)field.x.GetUnsafePtr();
            var y = (float*)field.y.GetUnsafePtr();
            var z = (float*)field.z.GetUnsafePtr();
            for (int i = 0; i < field.Active; i++) {
                posses[i] = new(x[i], y[i], z[i]);
            }

            buffer.SetBufferData(posBuffer, intermediatePosses, 0, 0, field.Active);
            buffer.SetGlobalBuffer(possesID, posBuffer);
            buffer.DrawMeshInstancedProcedural(mesh, 0, material, -1, field.Active);
        }

        public void Dispose() {
            posBuffer.Dispose();
            intermediatePosses.Dispose();
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
