﻿using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents a <c>return</c>-statement, optionally with a value.
    /// </summary>
    internal class ReturnStatement : Statement {
        public Expression? ReturnValue { get; private set; }

        public ReturnStatement(
            Location location = default,
            Expression? returnValue = null
        ) : base(location) {
            ReturnValue = returnValue;
        }

        public override string ToString()
            => $"[return]\nvalue:\n{Indent(ReturnValue)}";

        public override string ToCompactString()
            => ReturnValue == null ? "[return]                 [none]" : $"[return]                 {ReturnValue.ToCompactString()}";

        // Return may not be in a non-function, but that's impossible.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => ReturnValue?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>();

        public ReturnStatement WithReturnValue(Expression? returnValue)
            => new(Location, returnValue);
    }
}
