using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Semantics;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing
    /// <code>
    ///     if (condition) { goto condition-true; }
    ///     ..
    ///     condition-true:
    /// </code>
    /// that can also be used to simulate if-else blocks as follows:
    /// <code>
    ///     if (condition) { goto condition-true; }
    ///     ..
    ///     goto done;
    ///     condition-true:
    ///     ..
    ///     done:
    /// </code>
    /// </summary>
    internal class ConditionalGotoStatement : IfStatement, IEmittable {

        public ConditionalGotoStatement(
            IdentifierName condition,
            GotoLabelStatement gotoTarget,
            Location location = default
        ) : base(
            condition,
            new Block(
                new List<Statement> { 
                    new GotoStatement(gotoTarget, gotoTarget.Location)
                },
                gotoTarget.Location
            ),
            location: location,
            falseBranch: null
        ) { }

        public IdentifierName IdentifierCondition => (IdentifierName)Condition;
        public GotoLabelStatement Target => ((GotoStatement)TrueBranch.Statements[0]).Target;

        public override string ToString()
            => $"[conditional goto]\ncondition:\n{Indent(Condition)}\ntarget:\n{Indent(TrueBranch.Statements[0])}";

        public override string ToCompactString()
            => $"<conditional goto>       if {IdentifierCondition.Name} goto {Target.Name}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var diags = base.ValidateTree(path);
            if (TrueBranch.Statements.Count != 1 || TrueBranch.Statements[0] is not GotoStatement)
                diags = diags.Append(DiagnosticRules.InternalMalformedConditionalGoto(TrueBranch));
            if (Condition is not IdentifierName)
                diags = diags.Append(DiagnosticRules.InternalMalformedConditionalGoto(Condition));
            return diags;
        }

        public ConditionalGotoStatement WithCondition(IdentifierName condition)
            => new(condition, (GotoLabelStatement)TrueBranch.Statements[0], Location);
        public ConditionalGotoStatement WithTarget(GotoLabelStatement target)
            => new((IdentifierName)Condition, target, Location);
        public ConditionalGotoStatement WithTarget(GotoStatement target)
            => new((IdentifierName)Condition, target.Target, Location);

        List<HLOP> IEmittable.Emit(SemanticModel _) => HLOP.JumpConditional(Target.Name, IdentifierCondition.Name);
    }
}
