using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    public struct Player {
        /// <summary>
        /// Where the player is on the field. This is not bounds-checked in
        /// this struct.
        /// </summary>
        public float2 position;

        /// <summary>
        /// How many lives the player has remaining.
        /// </summary>
        public int remainingLives;
        /// <summary>
        /// How many bombs the player has remaining.
        /// </summary>
        public int RemainingBombs;

        /// <summary>
        /// How fast the player is when focusing, in units/tick.
        /// </summary>
        public float focusedSpeed;
        /// <summary>
        /// How fast the player is when not focusing, in units/tick.
        /// </summary>
        public float unfocusedSpeed;

        /// <summary>
        /// How many units the player hitbox is large.
        /// </summary>
        public float hitboxRadius;
        /// <summary>
        /// How many units the player's grazebox is large.
        /// </summary>
        public float grazeboxRadius;

        /// <summary>
        /// How many bullets the player has grazed.
        /// </summary>
        public int graze;
    }
}
