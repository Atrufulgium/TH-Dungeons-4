using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// Contains all parameters for <see cref="BulletField.CreateBullet(BulletCreationParams)"/>
    /// in one convenient container.
    /// </summary>
    // It's really only supposed to be used in place of arguments and not
    // stored or whatever. Because of that, `ref`.
    // (FFS give me `record struct`...)
    public readonly ref struct BulletCreationParams {
        public readonly float2 spawnPosition;
        public readonly float2 movement;
        public readonly float hitboxSize;
        public readonly int textureID;
        public readonly int layer;
        public readonly float4 outerColor;
        public readonly float4 innerColor;
        public readonly MiscBulletProps bulletProps;
        public readonly float renderScale;

        public BulletCreationParams(
            float2 spawnPosition,
            float2 movement,
            float hitboxSize,
            int textureID,
            int layer,
            float4 outerColor,
            float4 innerColor,
            MiscBulletProps bulletProps,
            float renderScale
        ) {
            this.spawnPosition = spawnPosition;
            this.movement = movement;
            this.hitboxSize = hitboxSize;
            this.textureID = textureID;
            this.layer = layer;
            this.outerColor = outerColor;
            this.innerColor = innerColor;
            this.bulletProps = bulletProps;
            this.renderScale = renderScale;
        }
    }
}
