using System.Runtime.CompilerServices;
using Unity.Burst;

// See https://forum.unity.com/threads/add-burstcompiler-isburstcompiled-to-check-if-a-method-is-burst-compiled-or-not.1442896/
namespace Atrufulgium.EternalDreamCatcher.Base {
    public static class BurstUtils {
        /// <summary>
        /// Whether the current code compilation is burst-compiled.
        /// <br/>
        /// This has minor overhead for non-burst compiled code. This has zero
        /// overhead for burst-compiled code.
        /// </summary>
        public static bool IsBurstCompiled {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                bool burst = true;
                Managed(ref burst);
                return burst;

                [BurstDiscard]
                static void Managed(ref bool burst) => burst = false;
            }
        }
    }
}
