using System;

namespace Atrufulgium.BulletScript.Compiler.Parsing {
    // "Don't use exception-based control flow" they say.
    // Unfortunately I'm doing recursive descent :(
    /// <summary>
    /// An exception that denotes compilation went wrong at some point.
    /// <para>
    /// Note that this is <i>different</i> from error diagnostics, and that the
    /// end-user will not see the messages here.
    /// </para>
    /// <para>
    /// Whenever throwing this, be sure to also send a Diagnostic to the
    /// relevant context.
    /// </para>
    /// </summary>
    internal class MalformedCodeException : Exception {
        public MalformedCodeException() { }
        public MalformedCodeException(string message) : base(message) { }
        public MalformedCodeException(string message, Exception inner) : base(message, inner) { }
    }
}
