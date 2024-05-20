using System;

namespace Atrufulgium.EternalDreamCatcher.BulletField {
    /// <summary>
    /// Contains "small" 1- and 2-bit bullet properties that should not take up
    /// an entire byte or even int in the array.
    /// </summary>
    [Flags]
    public enum MiscBulletProps {
        /// <summary>
        /// This bullet clears once it is far enough away from the screen.
        /// </summary>
        ClearOnEdge = 1 << 0,
        /// <summary>
        /// This bullet clears when it interacts with a bomb, or after dying.
        /// </summary>
        ClearOnBomb = 1 << 1,
        /// <summary>
        /// This bullet clears whenever it clears with another bullet.
        /// <br/>
        /// This only makes sense with <see cref="DestroysPlayerBullets"/> or
        /// <see cref="DestroysEnemyBullets"/>.
        /// </summary>
        ClearOnClear = 1 << 2,

        /// <summary>
        /// Whether, when colliding with a player bullet, that bullet is
        /// destroyed.
        /// </summary>
        DestroysPlayerBullets = 1 << 3,
        /// <summary>
        /// Whether, when colliding with a non-player bullet, that bullet is
        /// destroyed.
        /// </summary>
        DestroysEnemyBullets = 1 << 4,

        /// <summary>
        /// Whether this bullet can kill the player.
        /// </summary>
        HarmsPlayers = 1 << 5,
        /// <summary>
        /// Whether this bullet can harm enemies.
        /// </summary>
        HarmsEnemies = 1 << 6,

        /// <summary>
        /// Whether this bullet is allowed to spawn close, or on top of, the
        /// player.
        /// </summary>
        CanSpawnOnPlayer = 1 << 7,

        /// <summary>
        /// Whether this bullet is "glowy".
        /// </summary>
        RendersAdditiviely = 1 << 8,

        /// <summary>
        /// Whether instead of regular rotation, this bullet has a fixed
        /// rotation of 0.
        /// <br/>
        /// Mutually incompatible with <see cref="RotationContinuous"/> and
        /// <see cref="RotationWiggle"/>.
        /// </summary>
        RotationFixed = 1 << 9,
        /// <summary>
        /// Whether instead of regular rotation, this bullet spins at a fixed
        /// rate.
        /// <br/>
        /// Mutually incompatible with <see cref="RotationFixed"/> and
        /// <see cref="RotationWiggle"/>.
        /// </summary>
        RotationContinuous = 1 << 10,
        /// <summary>
        /// Whether instead of regular rotation, this bullet wiggles back and
        /// forth.
        /// <br/>
        /// Mutually incompatible with <see cref="RotationFixed"/> and
        /// <see cref="RotationContinuous"/>.
        /// </summary>
        RotationWiggle = 1 << 11,

        /// <summary>
        /// Whether this bullet can be "pivoted" to, meaning that each pivoted
        /// bullet's position and movement is not with respect to the field,
        /// but instead with respect to the pivoted bullet.
        /// </summary>
        IsPivot = 1 << 12,
        /// <summary>
        /// Whether to pivot this bullet to the last bullet created with
        /// <see cref="IsPivot"/> set.
        /// </summary>
        Pivoted = 1 << 13,
    }
}
