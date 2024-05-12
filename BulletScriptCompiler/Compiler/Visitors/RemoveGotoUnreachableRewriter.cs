using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrites:
    /// <list type="bullet">
    /// <item>
    /// Any unreachable goto is removed.
    /// </item>
    /// <item>
    /// Any code that is unreachable due to jumps
    /// <code>
    ///     goto some-label;
    ///     ..
    ///     first-label-after-goto:
    /// </code>
    /// turns into
    /// <code>
    ///     goto some-label;
    ///     first-label-after-goto:
    /// </code>
    /// </item>
    /// <item>
    /// After taking the above into account, jumping to the next statement
    /// <code>
    ///     goto next-statement;
    ///     next-statement:
    /// </code>
    /// turns into
    /// <code>
    ///     next-statement:
    /// </code>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> Root is in statement form and everything is emittable. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> Idem. </item>
    /// </list>
    /// </remarks>
    // This does not perform more complex analysis that could remove chunks
    // of code if you look at the structure of label--goto more in-depth.
    // TODO: This also removes unused special method labels. That is bad.
    internal class RemoveGotoUnreachableRewriter : AbstractTreeRewriter {

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count > 0)
                throw new VisitorAssumptionFailedException("Assumed tree was in statement form.");

            List<Statement> statementsBefore = new(node.RootLevelStatements);
            List<Statement> statementsAfter = new();

            while (true) {
                HashSet<GotoLabelStatement> gotoLabels = new();
                HashSet<GotoLabelStatement> wentToTargets = new();

                foreach (var s in statementsBefore) {
                    if (s is not IEmittable)
                        throw new VisitorAssumptionFailedException("Assumed tree contained only emittable nodes.");

                    if (s is GotoLabelStatement label)
                        gotoLabels.Add(label);
                    if (s is GotoStatement got)
                        wentToTargets.Add(got.Target);
                    if (s is ConditionalGotoStatement cond)
                        wentToTargets.Add(cond.Target);
                }

                gotoLabels.ExceptWith(wentToTargets);
                var unusedLabels = gotoLabels;

                bool reachable = true;
                string? directlyPreviousReachableGotoTarget = null;
                foreach (var s in statementsBefore) {
                    if (s is GotoLabelStatement label) {
                        if (unusedLabels.Contains(label))
                            continue;

                        if (directlyPreviousReachableGotoTarget != null && label.Name == directlyPreviousReachableGotoTarget)
                            statementsAfter.RemoveAt(statementsAfter.Count - 1);
                        reachable = true;
                    }

                    if (reachable) {
                        directlyPreviousReachableGotoTarget = null;
                        statementsAfter.Add(s);
                    }

                    if (s is GotoStatement got) {
                        if (reachable)
                            directlyPreviousReachableGotoTarget = got.Target.Name;
                        reachable = false;
                    }
                }

                if (statementsBefore.Count == statementsAfter.Count)
                    return node.WithRootLevelStatements(statementsAfter);

                (statementsBefore, statementsAfter) = (statementsAfter, statementsBefore);
                statementsAfter.Clear();
            }
        }
    }
}
