using Unity.Mathematics;
using UnityEngine;

namespace Atrufulgium.EternalDreamCatcher.Base {
    /// <summary>
    /// Standard-layout currently-not-customizable keyboard game input.
    /// </summary>
    internal struct KeyboardInput : IUnityGameInput {
        int gameTick;
        public int GameTick { set { gameTick = value; } }

        int2 moveDirection;
        public int2 MoveDirection { readonly get => moveDirection; private set => moveDirection = value; }

        bool isShooting;
        public bool IsShooting { readonly get => isShooting; private set => isShooting = value; }

        bool isBombing;
        public bool IsBombing { readonly get => isBombing; private set => isBombing = value; }

        bool isFocusing;
        public bool IsFocusing { readonly get => isFocusing; private set => isFocusing = value; }

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
