using Atrufulgium.EternalDreamCatcher.Base;
using Atrufulgium.EternalDreamCatcher.BulletEngine.TickStrategies;
using Atrufulgium.EternalDreamCatcher.BulletScriptVM;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// A <see cref="BulletField"/> consists of just the bullet data.
    /// <br/>
    /// A <see cref="DanmakuScene"/> consists of everything in addition, the
    /// player, the enemy/ies, and basically everything else 2D that does
    /// something and/or has to be rendered.
    /// </summary>
    public class DanmakuScene : IDisposable {

        public const int MAX_BULLETS = BulletField.MAX_BULLETS;
        internal readonly BulletField bulletField = new(true);
        protected readonly BulletFieldRenderer bulletFieldRenderer;
        
        public ITickStrategy TickStrategy { get; set; }

        protected Mesh quadMesh;
        protected Material entityMaterial;
        protected Texture2D[] entityTextures;

        internal NativeReference<Player> player;
        internal NativeReference<int> gameTick;
        // Main gameplay: fill [N] ticks when skipping [N] ticks.
        // Replays: fill this up completely at the beginning.
        internal NativeList<GameInput> input;

        internal NativeReference<Circle> playerHitbox;
        internal NativeReference<Circle> playerGrazebox;
        internal NativeList<BulletReference> playerHitboxResult;
        internal NativeList<BulletReference> playerGrazeboxResult;

        protected VMList templates;
        /*internal*/public NativeList<VM> activeVMs;

        // The VMsCommandsJob needs this array.
        internal NativeList<BulletReference> createdBullets;

        readonly int gameTickID;
        readonly int entityTexID;
        readonly int entityPosScaleID;

        public DanmakuScene(
            Mesh quadMesh,
            Material bulletMaterial,
            Texture2D[] bulletTextures,
            Material entityMaterial,
            Texture2D[] entityTextures,
            NativeReference<Player> player,
            ITickStrategy tickStrategy
        ) {
            bulletFieldRenderer = new(bulletMaterial, quadMesh, bulletTextures);
            TickStrategy = tickStrategy;
            this.quadMesh = quadMesh;
            this.entityMaterial = entityMaterial;
            this.entityTextures = entityTextures;

            this.player = player;
            gameTick = new(0, Allocator.Persistent);
            // Pre-allocate half an hour of gameplay worth of input space
            input = new(60 * 60 * 30, Allocator.Persistent);

            playerHitbox = new(Allocator.Persistent);
            playerGrazebox = new(Allocator.Persistent);
            playerHitboxResult = new(32, Allocator.Persistent);
            playerGrazeboxResult = new(512, Allocator.Persistent);

            templates = new(Array.Empty<string>(), Array.Empty<VM>());
            activeVMs = new(Allocator.Persistent);

            createdBullets = new(Allocator.Persistent);

            gameTickID = Shader.PropertyToID("_BulletTime");
            entityTexID = Shader.PropertyToID("_EntityTex");
            entityPosScaleID = Shader.PropertyToID("_EntityPosScale");
        }

        public JobHandle ScheduleTick(JobHandle dep = default)
            => TickStrategy.ScheduleTick(this, dep);

        public JobHandle ScheduleTick(int ticks, JobHandle dep = default)
            => TickStrategy.ScheduleTick(this, dep, ticks);

        /// <inheritdoc cref="BulletField.CreateBullet(ref BulletCreationParams)"/>
        public BulletReference? CreateBullet(ref BulletCreationParams bullet)
            => bulletField.CreateBullet(ref bullet);

        /// <summary>
        /// Adds commands to a buffer to fully render this scene onto the
        /// current active render target.
        /// </summary>
        public void Render(CommandBuffer buffer) {
            // For the time being put player rendering here.
            // Once I get a more general "renderable entities" system, move it.
            // Bullets are more important than any entity, so put them below.
            buffer.SetGlobalTexture(entityTexID, entityTextures[0]);
            buffer.SetGlobalVector(entityPosScaleID, new float4(player.Value.position, 0, 1));
            buffer.SetGlobalFloat(gameTickID, gameTick.Value / 60f);
            buffer.DrawMesh(
                quadMesh, matrix: default, entityMaterial, 0, 0
            );

            bulletFieldRenderer.RenderField(bulletField, buffer);
        }

        public void Dispose() {
            bulletField.Dispose();
            bulletFieldRenderer.Dispose();
            gameTick.Dispose();
            input.Dispose();
            playerHitbox.Dispose();
            playerGrazebox.Dispose();
            playerHitboxResult.Dispose();
            playerGrazeboxResult.Dispose();
            templates.Dispose();
            activeVMs.Dispose();
            createdBullets.Dispose();
        }
    }
}
