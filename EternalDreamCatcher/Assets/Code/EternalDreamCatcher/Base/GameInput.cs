using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.Base {
    /// <summary>
    /// This class represents all input the player can do during danmaku.
    /// </summary>
    public readonly struct GameInput {
        // Bit pattern:
        // 2 LSB: A value 0~2 storing horizontal movement (+1).
        // 3~4 bit: A value 0~2 storing vertical movement (+1).
        // 5th bit: whether shooting
        // 6th bit: whether bombing
        // 7th bit: whether focusing

        private readonly byte value;

        /// <summary>
        /// The <b>unnormalized</b> move direction of the player this tick.
        /// One of (0,0), (±1, 0), (0, ±1), or (±1,±1).
        /// </summary>
        public readonly int2 MoveDirection => (new int2(value, (value >> 2)) & 0b11) - 1;

        const byte SHOOTING_MASK = 0b1_0000;
        /// <summary>
        /// Whether the player is firing any bullets this tick.
        /// </summary>
        public readonly bool IsShooting => (value & SHOOTING_MASK) > 0;

        const byte BOMBING_MASK = 0b10_0000;
        /// <summary>
        /// Whether the player activated a bomb this tick. This should only be
        /// true on the down press, and not the ticks afterward.
        /// <br/>
        /// Any cooldown of the bomb is not the responsibility of this class.
        /// </summary>
        public readonly bool IsBombing => (value & BOMBING_MASK) > 0;

        const byte FOCUSING_MASK = 0b100_0000;
        /// <summary>
        /// Whether the player is in focused movement this tick.
        /// </summary>
        public readonly bool IsFocusing => (value & FOCUSING_MASK) > 0;

        public GameInput(int2 moveDirection, bool isShooting, bool isBombing, bool isFocusing) {
            byte value = 0;
            moveDirection = math.clamp(moveDirection, -1, 1) + 1;
            value += (byte)moveDirection.x;
            value += (byte)(moveDirection.y << 2);

            if (isShooting) value += SHOOTING_MASK;
            if (isBombing) value += BOMBING_MASK;
            if (isFocusing) value += FOCUSING_MASK;

            this.value = value;
        }
    }
}