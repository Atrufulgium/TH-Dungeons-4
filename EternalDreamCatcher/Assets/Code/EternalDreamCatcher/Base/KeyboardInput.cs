using Unity.Mathematics;
using UnityEngine;

namespace Atrufulgium.EternalDreamCatcher.Base {
    /// <summary>
    /// Standard-layout currently-not-customizable keyboard game input.
    /// </summary>
    internal struct KeyboardInput : IUnityGameInput {
        public int GameTick { set { } }

        public int2 MoveDirection { get; private set; }

        public bool IsShooting { get; private set; }

        public bool IsBombing { get; private set; }

        public bool IsFocusing { get; private set; }

        public void HandleInputMainThread() {
            MoveDirection = (Input.GetKey(KeyCode.LeftArrow) ? new int2(-1, 0) : default)
                + (Input.GetKey(KeyCode.RightArrow) ? new int2(1, 0) : default)
                + (Input.GetKey(KeyCode.UpArrow) ? new int2(0, -1) : default)
                + (Input.GetKey(KeyCode.DownArrow) ? new int2(0, 1) : default);
            IsShooting = Input.GetKey(KeyCode.Z);
            IsBombing = Input.GetKeyDown(KeyCode.X);
            IsFocusing = Input.GetKey(KeyCode.LeftShift);
        }
    }
}
