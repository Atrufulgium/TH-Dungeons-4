using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Atrufulgium.EternalDreamCatcher.BulletEngine {

    /// <summary>
    /// Increments the current game tick.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    internal struct IncrementTickJob : IJob {

        public NativeReference<int> gameTick;

        public IncrementTickJob(NativeReference<int> gameTick) {
            this.gameTick = gameTick;
        }

        public void Execute() {
            IncrementTickPass.Execute(ref gameTick);
        }
    }

    /// <inheritdoc cref="IncrementTickJob{TGameInput}"/>
    internal static class IncrementTickPass {
        public static void Execute(ref NativeReference<int> gameTick) {
                gameTick.Value++;
        }
    }
}
