using System;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// Walkers and rewrites make assumptions about what nodes can or cannot
    /// exist. Throw this exception when those assumptions are not satisfied.
    /// </summary>
    internal class VisitorAssumptionFailedException : Exception {
        public VisitorAssumptionFailedException(string reason)
            : base($"Assumption was not satisfied: {reason}.") { }
    }
}
