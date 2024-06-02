#nullable disable // Monobehaviour start instead of constructor
using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletField;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Atrufulgium.EternalDreamCatcher.Game {

    [RequireComponent(typeof(Camera))]
    public class DanmakuFieldBehaviour : MonoBehaviour {

        DanmakuScene<KeyboardInput> danmakuScene;
        [Min(1)]
        public int TicksPerTick = 1;

        public Material bulletMaterial;
        // Assign this the default quad.
        // Note to self: it has vertices (}0.5f,}0.5f).
        public Mesh bulletMesh;
        new Camera camera;

        CommandBuffer buffer;

        public RawImage target;
        RenderTexture texture;

        public Texture2D[] bulletTextures;

        int tempRTid;

        void Start() {
            if (bulletMaterial == null) throw new System.NullReferenceException();
            if (bulletMesh == null) throw new System.NullReferenceException();
            if (bulletTextures == null || bulletTextures.Length == 0) throw new System.NullReferenceException();

            danmakuScene = new(bulletMaterial, bulletMesh, bulletTextures, new());
            var rng = new Unity.Mathematics.Random(230);
            for (int i = 0; i < Field.MAX_BULLETS; i++) {
                var bulletData = new BulletCreationParams(
                    rng.NextFloat2(), rng.NextFloat2Direction() * 0.0005f,
                    0.01f,
                    rng.NextInt(0, 24), 0,
                    (Vector4)Color.HSVToRGB(rng.NextFloat(), 1, 1), new(1, 1, 1, 1),
                    (MiscBulletProps)rng.NextInt(),
                    1//rng.NextFloat(0.5f, 1.5f) * rng.NextFloat(0.5f, 1.5f) * rng.NextFloat(0.5f, 1.5f)
                );
                danmakuScene.CreateBullet(ref bulletData);
            }

            camera = GetComponent<Camera>();

            tempRTid = Shader.PropertyToID("_tempFieldRendererRT");

            var size = FieldRenderer.GetBulletFieldResolution();
            texture = new(size.x, size.y, 0);
            target.texture = texture;
        }

        JobHandle currentUpdate;
        private void Update() {
            // (This does nothing with `default`, and does not throw.)
            currentUpdate.Complete();

            currentUpdate = danmakuScene.ScheduleTick(TicksPerTick);
            // Note: Do not be tempted by JobHandle.ScheduleBatchedJobs().
            // Even with the gap between the end of this Update() and the start
            // of the jobs, not having it is faster. idk why.
            // Adding that call removes the gap but has more overhead.
            // (Unless that overhead was specifically because I x1200'd, with
            //  "reasonable" values it helps, if only by Ęs's?)
            // Funny thing though: this is one of the rare cases in which the
            // code in the editor profiles __faster__. Because the ~2.50ms
            // EditorLoop is also used for the jobs stuff on other threads.
            // (Okay that's about it wrt this call.)
            JobHandle.ScheduleBatchedJobs();
        }

        private void OnPreRender() {
            currentUpdate.Complete();
            SetBuffer();
        }

        private void OnDestroy() {
            danmakuScene.Dispose();
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

            danmakuScene.Render(buffer);

            buffer.Blit(tempRTid, texture);

            // Canvas is drawn on top of "everything".
            camera.AddCommandBuffer(
                CameraEvent.AfterEverything,
                buffer
            );
        }
    }
}
