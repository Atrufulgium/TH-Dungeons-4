using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrites:
    /// <list type="bullet">
    /// <item>
    /// Eternal repeat loops
    /// <code>
    ///     repeat {
    ///         ...
    ///     }
    /// </code>
    /// turn into
    /// <code>
    ///     continue-label:
    ///     ...
    ///     goto continue-label;
    ///     break-label:
    /// </code>
    /// </item>
    /// <item>
    /// Breaks and continues
    /// <code>
    ///     break;
    ///     continue;
    /// </code>
    /// turn into
    /// <code>
    ///     goto break-label;
    ///     goto continue-label;
    /// </code>
    /// </item>
    /// <item>
    /// Branches
    /// <code>
    ///     if (condition) { .. }
    ///     [else { .. }]
    /// </code>
    /// turn into
    /// <code>
    ///     float global#if#temp = !condition;
    ///     if (global#if#temp) { goto [done|false-branch]; }
    ///     ..
    ///     [goto done;
    ///     false-branch:
    ///     ..
    ///     ]
    ///     done:
    /// </code>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> The only loops mechanism is <c>repeat { .. }</c>. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b>
    /// There are no loop mechanisms, and all IfStatements are ConditionalGotoStatements.</item>
    /// </list>
    /// </remarks>
    internal class RepeatsAndBranchesToGotoRewriter : AbstractTreeRewriter {

        int id = 0;
        GotoLabelStatement GetContinueLabel() => new($"continue-label-{id++}");
        GotoLabelStatement GetBreakLabel() => new($"break-label-{id++}");
        GotoLabelStatement GetDoneLabel() => new($"done-label-{id++}");
        GotoLabelStatement GetFalseBranchLabel() => new($"false-branch-label-{id++}");

        readonly Stack<GotoLabelStatement> continueLabels = new();
        readonly Stack<GotoLabelStatement> breakLabels = new();

        readonly IdentifierName globalIfTemp = new("global#if#temp");

        protected override Node? VisitRepeatStatement(RepeatStatement node) {
            continueLabels.Push(GetContinueLabel());
            breakLabels.Push(GetBreakLabel());

            node = (RepeatStatement)base.VisitRepeatStatement(node)!;
            var ret = new MultipleStatements(
                continueLabels.Peek(),
                new MultipleStatements(node.Body.Statements),
                new GotoStatement(continueLabels.Peek()),
                breakLabels.Peek()
            );

            continueLabels.Pop();
            breakLabels.Pop();
            return ret;
        }

        protected override Node? VisitBreakStatement(BreakStatement node)
            => new GotoStatement(breakLabels.Peek());
        protected override Node? VisitContinueStatement(ContinueStatement node)
            => new GotoStatement(continueLabels.Peek());

        protected override Node? VisitIfStatement(IfStatement node) {
            var doneLabel = GetDoneLabel();
            var falseBranchLabel = GetFalseBranchLabel();

            node = (IfStatement)base.VisitIfStatement(node)!;
            var decl = new LocalDeclarationStatement(
                new VariableDeclaration(
                    globalIfTemp,
                    Syntax.Type.Float,
                    initializer: new PrefixUnaryExpression(
                        node.Condition,
                        PrefixUnaryOp.Not
                    )
                )
            );

            if (node.FalseBranch == null) {
                return new MultipleStatements(
                    decl,
                    new ConditionalGotoStatement(globalIfTemp, doneLabel),
                    new MultipleStatements(
                        node.TrueBranch.Statements
                    ),
                    doneLabel
                );
            }
            return new MultipleStatements(
                decl,
                new ConditionalGotoStatement(globalIfTemp, falseBranchLabel),
                new MultipleStatements(
                    node.TrueBranch.Statements
                ),
                new GotoStatement(doneLabel),
                falseBranchLabel,
                new MultipleStatements(
                    node.FalseBranch.Statements
                ),
                doneLabel
            );
        }

        protected override Node? VisitLoopStatement(LoopStatement node) {
            if (node is RepeatStatement r && r.Count == null)
                return VisitRepeatStatement(r);
            throw new VisitorAssumptionFailedException("Assume only `repeat { .. }` loops exist.");
        }
    }
}
