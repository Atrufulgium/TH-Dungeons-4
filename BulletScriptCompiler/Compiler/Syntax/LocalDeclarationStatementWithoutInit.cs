using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Semantics;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing the declaration of a variable that does
    /// not have initialization.
    /// </summary>
    internal class LocalDeclarationStatementWithoutInit : LocalDeclarationStatement, IEmittable {
        
        public LocalDeclarationStatementWithoutInit(
            IdentifierName name,
            Type type,
            Location location = default
        ) : base(new(name, type), location) { }

        public override string ToString()
            => $"[local declaration noinit]\nidentifier:\n{Indent(Declaration.Identifier)}\ntype:\n{Indent(Declaration.Type.ToString())}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Declaration.ValidateTree(path.Append(this));

        new public LocalDeclarationStatementWithoutInit WithDeclaration(VariableDeclaration declaration)
            => new(declaration.Identifier, declaration.Type);

        List<HLOP> IEmittable.Emit(SemanticModel _) => new(capacity: 0);
    }
}
