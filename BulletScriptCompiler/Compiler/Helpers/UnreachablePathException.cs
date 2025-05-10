using System;

namespace Atrufulgium.BulletScript.Compiler.Helpers {
    /// <summary>
    /// An exception that indicates the current path is unreachable.
    /// Use when the c# compiler is too dumb to see that.
    /// </summary>
    internal class UnreachablePathException : Exception {
        /// <inheritdoc cref="UnreachablePathException"/>
        public UnreachablePathException() : base("This unreachable path was actually reached during runtime. This is very not allowed.") { }
    }
}
