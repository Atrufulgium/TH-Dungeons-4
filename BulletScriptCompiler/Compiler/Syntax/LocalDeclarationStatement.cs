﻿using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents declaring a variable inside a method.
    /// </summary>
    internal class LocalDeclarationStatement : Statement {
        public VariableDeclaration Declaration { get; private set; }

        public LocalDeclarationStatement(
            VariableDeclaration declaration,
            Location location = default
        ) : base(location) {
            Declaration = declaration;
        }

        public override string ToString()
            => $"[local declaration]\ndeclaration:\n{Indent(Declaration)}";

        public override string ToCompactString()
            => Declaration.ToCompactString();

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Declaration.ValidateTree(path.Append(this));

        public LocalDeclarationStatement WithDeclaration(VariableDeclaration declaration)
            => new(declaration, Location);
    }
}
