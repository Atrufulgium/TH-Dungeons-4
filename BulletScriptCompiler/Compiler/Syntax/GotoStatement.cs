using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Semantics;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing a goto.
    /// </summary>
    internal class GotoStatement : Statement, IEmittable {
        public GotoLabelStatement Target { get; private set; }

        public GotoStatement(GotoLabelStatement target, Location location = default)
            : base(location) {
            Target = target;
        }

        public override string ToString()
            => $"[goto]\ntarget:\n{Indent(Target)}";

        public override string ToCompactString()
            => $"<goto>                   goto {Target.Name}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => new List<Diagnostic>();

        public GotoStatement WithTarget(GotoLabelStatement target)
            => new(target, Location);

        List<HLOP> IEmittable.Emit(SemanticModel _) => HLOP.Jump(Target.Name);
    }
}
