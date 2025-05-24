using Atrufulgium.EternalDreamCatcher.Base;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {
    /// <summary>
    /// Moves the player depending on the game input.
    /// Also writes the new hit- and grazeboxes.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct MovePlayerJob<TGameInput> : IJob where TGameInput : unmanaged, IGameInput {

        public NativeReference<Player> player;
        // This is needed for a really ugly workaround.
        // See
        /// <seealso cref="DanmakuScene{TGameInput}.SchedulePostVMGameTickJob(JobHandle)"/>
        // for some more comments on this.
        [NativeDisableUnsafePtrRestriction]
        public TGameInput* gameInput;
        [WriteOnly]
        public NativeReference<Circle> hitbox;
        [WriteOnly]
        public NativeReference<Circle> grazebox;

        public MovePlayerJob(
            in NativeReference<Player> player,
            TGameInput* gameInput,
            in NativeReference<Circle> hitbox,
            in NativeReference<Circle> grazebox
        ) {
            this.player = player;
            this.gameInput = gameInput;
            this.hitbox = hitbox;
            this.grazebox = grazebox;
        }

        public unsafe void Execute() {
            MovePlayerPass<TGameInput>.Execute(player, gameInput, ref hitbox, ref grazebox);
        }
    }

    /// <inheritdoc cref="MovePlayerJob{TGameInput}"/>
    internal static class MovePlayerPass<TGameInput> where TGameInput : unmanaged, IGameInput {
        public static unsafe void Execute(
            in NativeReference<Player> player,
            in TGameInput* gameInput,
            ref NativeReference<Circle> hitbox,
            ref NativeReference<Circle> grazebox
        ) {
            var p = player.GetUnsafeTypedPtr();
            float2 delta = gameInput->MoveDirection;
            delta = math.normalizesafe(delta);
            delta *= gameInput->IsFocusing ? p->focusedSpeed : p->unfocusedSpeed;
            float2 newPos = p->position + delta;
            newPos = math.clamp(newPos, new(0.02f, 0.05f), new(0.98f, 1.15f));
            p->position = newPos;
            hitbox.Value = new(newPos, p->hitboxRadius);
            grazebox.Value = new(newPos, p->grazeboxRadius);
        }
    }
}
