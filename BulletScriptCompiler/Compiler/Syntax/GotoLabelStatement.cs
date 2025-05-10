using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Semantics;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing a target for goto's.
    /// </summary>
    internal class GotoLabelStatement : Statement, IEmittable {
        public string Name { get; private set; }

        public GotoLabelStatement(string name, Location location = default)
            : base(location) {
            Name = name;
        }

        public override string ToString()
            => $"[label]\nname:\n{Indent(Name)}";

        public override string ToCompactString()
            => $"<goto label>             {Name}:";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => new List<Diagnostic>();

        List<HLOP> IEmittable.Emit(SemanticModel _) => HLOP.GotoLabel(Name);
    }
}
