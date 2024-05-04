namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing either
    /// <code>
    ///     if (condition) { goto condition-true; }
    ///     ..
    ///     condition-true:
    /// </code>
    /// or
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

        public override string ToString()
            => $"[conditional goto]\ncondition:\n{Indent(Condition)}\ntarget:\n{Indent(TrueBranch.Statements[0])}";

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
    }
}
