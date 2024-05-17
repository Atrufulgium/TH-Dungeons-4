#nullable disable // Monobehaviour start instead of constructor
using Atrufulgium.EternalDreamCatcher.BulletField;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Atrufulgium.EternalDreamCatcher.Game {

    [RequireComponent(typeof(Camera))]
    public class ApplyFieldRendererBehaviour : MonoBehaviour {

        public TestBehaviour fieldContainer;
        Field field;
        public Material bulletMaterial;
        public Mesh bulletMesh;
        FieldRenderer fieldRenderer;
        new Camera camera;

        CommandBuffer buffer;

        public RawImage target;
        RenderTexture texture;

        int tempRTid;

        void Start() {
            field = fieldContainer.field;
            fieldRenderer = new(bulletMaterial, bulletMesh);
            camera = GetComponent<Camera>();

            tempRTid = Shader.PropertyToID("_tempFieldRendererRT");

            var size = FieldRenderer.GetBulletFieldResolution();
            texture = new(size.x, size.y, 0);
            target.texture = texture;
        }

        private void OnPreRender() {
            SetBuffer();
        }

        private void OnDestroy() {
            fieldRenderer.Dispose();
            buffer.Dispose();
        }

        private void SetBuffer() {
            if (buffer != null)
                camera.RemoveCommandBuffer(CameraEvent.AfterEverything, buffer);

            buffer = new() { name = "BulletFieldRendering" };
            var size = FieldRenderer.GetBulletFieldResolution();
            buffer.GetTemporaryRT(tempRTid, size.x, size.y);
            buffer.SetRenderTarget(new RenderTargetIdentifier(tempRTid));
            buffer.ClearRenderTarget(true, true, Color.black, 0);

            fieldRenderer.RenderField(field, buffer);

            buffer.Blit(tempRTid, texture);

            // Canvas is drawn on top of "everything".
            camera.AddCommandBuffer(
                CameraEvent.AfterEverything,
                buffer
            );
        }
    }
}
