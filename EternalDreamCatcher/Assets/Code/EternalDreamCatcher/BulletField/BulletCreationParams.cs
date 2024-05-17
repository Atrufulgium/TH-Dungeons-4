using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletField {

    /// <summary>
    /// Contains all parameters for <see cref="Field.CreateBullet(BulletCreationParams)"/>
    /// in one convenient container.
    /// </summary>
    // It's really only supposed to be used in place of arguments and not
    // stored or whatever. Because of that, `ref`.
    public readonly ref struct BulletCreationParams {
        public readonly float2 spawnPosition;
        public readonly float2 movement;
        public readonly float hitboxSize;

        public BulletCreationParams(float2 spawnPosition, float2 movement, float hitboxSize) {
            this.spawnPosition = spawnPosition;
            this.movement = movement;
            this.hitboxSize = hitboxSize;
        }
    }
}
