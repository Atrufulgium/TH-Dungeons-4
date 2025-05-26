using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    /// <summary>
    /// Moves the player depending on the game input.
    /// Also writes the new hit- and grazeboxes.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct MovePlayerJob : IJob {

        public NativeReference<Player> player;
        [ReadOnly]
        public NativeList<GameInput> input;
        [ReadOnly]
        public NativeReference<int> gameTick;
        [WriteOnly]
        public NativeReference<Circle> hitbox;
        [WriteOnly]
        public NativeReference<Circle> grazebox;

        public MovePlayerJob(
            in NativeReference<Player> player,
            in NativeList<GameInput> input,
            in NativeReference<int> gameTick,
            in NativeReference<Circle> hitbox,
            in NativeReference<Circle> grazebox
        ) {
            this.player = player;
            this.input = input;
            this.gameTick = gameTick;
            this.hitbox = hitbox;
            this.grazebox = grazebox;
        }

        public void Execute() {
            MovePlayerPass.Execute(player, input, gameTick, ref hitbox, ref grazebox);
        }
    }

    /// <inheritdoc cref="MovePlayerJob"/>
    internal static class MovePlayerPass {
        public static unsafe void Execute(
            in NativeReference<Player> player,
            in NativeList<GameInput> input,
            in NativeReference<int> gameTick,
            ref NativeReference<Circle> hitbox,
            ref NativeReference<Circle> grazebox
        ) {
            var p = player.GetUnsafeTypedPtr();
            var gameInput = input[gameTick.Value];
            float2 delta = gameInput.MoveDirection;
            delta = math.normalizesafe(delta);
            delta *= gameInput.IsFocusing ? p->focusedSpeed : p->unfocusedSpeed;
            float2 newPos = p->position + delta;
            newPos = math.clamp(newPos, new(0.02f, 0.05f), new(0.98f, 1.15f));
            p->position = newPos;
            hitbox.Value = new(newPos, p->hitboxRadius);
            grazebox.Value = new(newPos, p->grazeboxRadius);
        }
    }
}
