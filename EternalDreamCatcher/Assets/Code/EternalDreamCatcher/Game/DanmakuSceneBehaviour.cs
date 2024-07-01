#nullable disable // Monobehaviour start instead of constructor
using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletEngine;
using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Atrufulgium.EternalDreamCatcher.Game {

    [RequireComponent(typeof(Camera))]
    public class DanmakuSceneBehaviour : MonoBehaviour {

        NativeReference<KeyboardInput> gameInput;
        DanmakuScene danmakuScene;
        [Min(1)]
        public int TicksPerTick = 1;

        public Material bulletMaterial;
        public Material entityMaterial;
        // Assign this the default quad.
        // Note to self: it has vertices (Å}0.5f,Å}0.5f).
        public Mesh quadMesh;
        new Camera camera;

        CommandBuffer buffer;

        public RawImage target;
        RenderTexture texture;

        public Texture2D[] bulletTextures;
        public Texture2D[] entityTextures;

        int tempRTid;

        NativeReference<Player> player;

        List<IDisposable> testDisposables = new();

        void Start() {
            if (bulletMaterial == null) throw new System.NullReferenceException();
            if (quadMesh == null) throw new System.NullReferenceException();
            if (bulletTextures == null || bulletTextures.Length == 0) throw new System.NullReferenceException();

            // TODO: Put this someplace better
            Application.targetFrameRate = 60;

            player = new(new() {
                position = new(0.5f, 1.1f),
                remainingLives = 3,
                RemainingBombs = 2,
                focusedSpeed = 0.25f / 60,
                unfocusedSpeed = 0.66f / 60,
                hitboxRadius = 0.005f,
                grazeboxRadius = 0.025f
            }, Allocator.Persistent);

            gameInput = new(new(), Allocator.Persistent);

            danmakuScene = new DanmakuScene<KeyboardInput>(quadMesh, bulletMaterial, bulletTextures, entityMaterial, entityTextures, gameInput, player);
            danmakuScene.activeVMs.Add(CreateVM());

            camera = GetComponent<Camera>();

            tempRTid = Shader.PropertyToID("_tempFieldRendererRT");

            var size = BulletFieldRenderer.GetBulletFieldResolution();
            texture = new(size.x, size.y, 0);
            target.texture = texture;
        }

        unsafe VM CreateVM() {
            static unsafe float Int(int value) => *(float*)&value;
            const float NA = float.NaN;
            NativeReference<Unity.Mathematics.Random> rngRef = new(new(230), Allocator.Persistent);
            NativeReference<uint> uintRef = new(0, Allocator.Persistent);
            VM vm = new(new float4[] {
// High-level:
//  float t;
//  spawnspeed = 0.12f;
//  soawboisutuib = [0.5; 0.5];
//  repeat {
//      t += 0.025f;
//      spawnrotation += sin(t) + 0.25f;
//      spawn();
//      spawnrotation += 0.25f;
//      spawn();
//      spawnrotation += 0.25f;
//      spawn();
//      spawnrotation += 0.25f;
//      spawn();
//      wait(5);
//  }
// Low level:
// mem[13] = 0 (implicit)
// spawnspeed = 0.12
new(Int(10), Int(2), 0.12f, NA),
// spawnposition = [0.5; 0.5]
new(Int(10), Int(4), 0.5f, NA),
new(Int(10), Int(5), 0.5f, NA),
// label: LOOP
// mem[13] = 0.025 + mem[13]
new(Int(81), Int(13), 0.025f, Int(13)),
// mem[14] = sin(mem[13])
new(Int(98), Int(14), Int(13), NA),
// mem[14] = 0.25 + mem[14]
new(Int(81), Int(14), 0.25f, Int(14)),
// spawnrotation = spawnrotation + mem[14]
new(Int(82), Int(1), Int(1), Int(14)),
// spawn
new(Int(34), NA, NA, NA),
// spawnrotation = 0.25 + spawnrotation
new(Int(81), Int(1), 0.25f, Int(1)),
// spawn
new(Int(34), NA, NA, NA),
// spawnrotation = 0.25 + spawnrotation
new(Int(81), Int(1), 0.25f, Int(1)),
// spawn
new(Int(34), NA, NA, NA),
// spawnrotation = 0.25 + spawnrotation
new(Int(81), Int(1), 0.25f, Int(1)),
// spawn
new(Int(34), NA, NA, NA),
// wait 5
new(Int(21), 5, NA, NA),
// goto LOOP
new(Int(19), Int(3-1), NA, NA)
            }, 32, 32, Array.Empty<string>(), default, uintRef.GetUnsafeTypedPtr(), rngRef.GetUnsafeTypedPtr());
            testDisposables.Add(rngRef);
            testDisposables.Add(uintRef);
            testDisposables.Add(vm);
            return vm;
        }

        JobHandle currentUpdate;
        private unsafe void Update() {
            // (This does nothing with `default`, and does not throw.)
            currentUpdate.Complete();

            gameInput.GetUnsafeTypedPtr()->HandleInputMainThread();

            currentUpdate = danmakuScene.ScheduleTick(TicksPerTick);
            // Note: Do not be tempted by JobHandle.ScheduleBatchedJobs().
            // Even with the gap between the end of this Update() and the start
            // of the jobs, not having it is faster. idk why.
            // Adding that call removes the gap but has more overhead.
            // (Unless that overhead was specifically because I x1200'd, with
            //  "reasonable" values it helps, if only by É s's?)
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
            gameInput.Dispose();
            // TODO: Remove once not testing anymore
            foreach (var d in testDisposables)
                d.Dispose();
            danmakuScene.Dispose();
            buffer.Dispose();
            player.Dispose();
        }

        private void SetBuffer() {
            if (buffer == null) {
                buffer = new() { name = "BulletFieldRendering" };
            } else {
                // Gets re-added anyways
                camera.RemoveCommandBuffer(CameraEvent.AfterEverything, buffer);
                buffer.Clear();
            }

            var size = BulletFieldRenderer.GetBulletFieldResolution();
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
