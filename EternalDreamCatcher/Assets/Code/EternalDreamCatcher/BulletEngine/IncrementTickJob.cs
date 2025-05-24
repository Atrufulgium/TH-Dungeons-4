using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// Increments the current game tick both generally and for the game
    /// input.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct IncrementTickJob : IJob {

        public NativeReference<int> gameTick;
        [NativeDisableUnsafePtrRestriction]
        public int* inputGameTick;

        public IncrementTickJob(NativeReference<int> gameTick, int* inputGameTick) {
            this.gameTick = gameTick;
            this.inputGameTick = inputGameTick;
        }

        public unsafe void Execute() {
            IncrementTickPass.Execute(ref gameTick, inputGameTick);
        }
    }

    /// <inheritdoc cref="IncrementTickJob{TGameInput}"/>
    internal static class IncrementTickPass {
        public static unsafe void Execute(
            ref NativeReference<int> gameTick,
            int* inputGameTick) {
                gameTick.Value++;
                *inputGameTick = gameTick.Value;
        }
    }
}
