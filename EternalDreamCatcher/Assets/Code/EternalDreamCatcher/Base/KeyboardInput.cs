using Unity.Mathematics;
using UnityEngine;

namespace Atrufulgium.EternalDreamCatcher.Base {
    /// <summary>
    /// Standard-layout currently-not-customizable keyboard game input.
    /// </summary>
    internal struct KeyboardInput : IGameInput {
        public int GameTick { set { } }

        public int2 MoveDirection => (Input.GetKey(KeyCode.LeftArrow) ? new int2(-1, 0) : default)
            + (Input.GetKey(KeyCode.RightArrow) ? new int2(1, 0) : default)
            + (Input.GetKey(KeyCode.UpArrow) ? new int2(0, -1) : default)
            + (Input.GetKey(KeyCode.DownArrow) ? new int2(0, 1) : default);

        public bool IsShooting => Input.GetKey(KeyCode.Z);

        public bool IsBombing => Input.GetKeyDown(KeyCode.X);

        public bool IsFocusing => Input.GetKey(KeyCode.LeftShift);
    }
}
