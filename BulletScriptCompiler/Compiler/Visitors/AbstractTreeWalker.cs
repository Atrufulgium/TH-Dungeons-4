using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// Walks through a <see cref="Node"/> and all of its children.
    /// </summary>
    /// <remarks>
    /// Suppose you want to extract all loops for some reason. You'd implement that as follows:
    /// <code>
    ///     class LiteralCollectorWalker : TreeWalker {
    ///         public List&lt;LoopStatement&gt; Loops { get; private set; } = new();
    ///         protected override void VisitLoopStatement(LoopStatement node) {
    ///             Loops.Add(node);
    ///             base.VisitLoopStatement(node);
    ///         }
    ///     }
    /// </code>
    /// In this case, we want to call the base method as children may also have
    /// loops in them. In general, the effect is as follows:
    /// <list type="bullet">
    /// <item>
    /// <b>At the start:</b> Depth-first like tree walk behaviour.
    /// </item>
    /// <item>
    /// <b>At the end:</b> Breadth-first like tree walk behaviour.
    /// </item>
    /// <item>
    /// <b>Omitted entirely:</b> Children are not visited.
    /// </item>
    /// </list>
    /// These are not the only options, but do give a general idea of your options.
    /// </remarks>
    internal abstract class AbstractTreeWalker : IVisitor {

        bool IVisitor.IsReadOnly => true;
        public Node? VisitResult { get; private set; }

        private readonly List<Diagnostic> diagnostics = new();
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; init; }

        public void AddDiagnostic(Diagnostic diagnostic) => diagnostics.Add(diagnostic);

        public SemanticModel Model { set; protected get; }

        public AbstractTreeWalker() {
            Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics);
            Model = SemanticModel.Empty;
        }

        void IVisitor.Visit(Node node) {
            diagnostics.Clear();
            VisitResult = node;
            Visit(node);
        }

        [DoesNotReturn]
        void ThrowUnknownNodeException(Node node)
            => throw new NotImplementedException($"TreeWalker has no knowledge of {node.GetType()} nodes.");

        // Note when adding syntax nodes:
        // Always include intermediate classes in the hierarchy for broader
        // override options. Always have the full "Visit" calls in the bottom-
        // most class. (Doing them all over the place gives weird semantics to
        // these otherwise nicely behaving methods.)

        public virtual void Visit(Node node) {
            if (node is Declaration decl) { VisitDeclaration(decl); return; }
            if (node is Expression expr) { VisitExpression(expr); return; }
            if (node is Statement stat) { VisitStatement(stat); return; }
            if (node is Block block) { VisitBlock(block); return; }
            if (node is Root root) { VisitRoot(root); return; }
            ThrowUnknownNodeException(node);
        }

        protected virtual void VisitDeclaration(Declaration node) {
            if (node is MethodDeclaration meth) { VisitMethodDeclaration(meth); return; }
            if (node is VariableDeclaration varb) { VisitVariableDeclaration(varb); return; }
            ThrowUnknownNodeException(node);
        }

        protected virtual void VisitExpression(Expression node) {
            if (node is AssignmentExpression ass) { VisitAssignmentExpression(ass); return; }
            if (node is BinaryExpression bine) { VisitBinaryExpression(bine); return; }
            if (node is IdentifierName id) { VisitIdentifierName(id); return; }
            if (node is IndexExpression ind) { VisitIndexExpression(ind); return; }
            if (node is InvocationExpression call) { VisitInvocationExpression(call); return; }
            if (node is LiteralExpression lit) { VisitLiteralExpression(lit); return; }
            if (node is MatrixExpression mat) { VisitMatrixExpression(mat); return; }
            if (node is PolarExpression pole) { VisitPolarExpression(pole); return; }
            if (node is PostfixUnaryExpression post) { VisitPostfixUnaryExpression(post); return; }
            if (node is PrefixUnaryExpression pre) { VisitPrefixUnaryExpression(pre); return; }
            ThrowUnknownNodeException(node);
        }

        protected virtual void VisitLoopStatement(LoopStatement node) {
            if (node is ForStatement fors) { VisitForStatement(fors); return; }
            if (node is RepeatStatement reps) { VisitRepeatStatement(reps); return; }
            if (node is WhileStatement whil) { VisitWhileStatement(whil); return; }
            ThrowUnknownNodeException(node);
        }

        protected virtual void VisitStatement(Statement node) {
            if (node is LoopStatement loop) { VisitLoopStatement(loop); return; }
            if (node is BreakStatement brea) { VisitBreakStatement(brea); return; }
            if (node is ConditionalGotoStatement cond) { VisitConditionalGotoStatement(cond); return; }
            if (node is ContinueStatement cont) { VisitContinueStatement(cont); return; }
            if (node is ExpressionStatement expr) { VisitExpressionStatement(expr); return; }
            if (node is GotoLabelStatement gotl) { VisitGotoLabelStatement(gotl); return; }
            if (node is GotoStatement got) { VisitGotoStatement(got); return; }
            if (node is IfStatement ifs) { VisitIfStatement(ifs); return; }
            if (node is LocalDeclarationStatement loco) { VisitLocalDeclarationStatement(loco); return; }
            if (node is ReturnStatement ret) { VisitReturnStatement(ret); return; }
            ThrowUnknownNodeException(node);
        }

        protected virtual void VisitAssignmentExpression(AssignmentExpression node) {
            Visit(node.LHS);
            Visit(node.RHS);
        }

        protected virtual void VisitBinaryExpression(BinaryExpression node) {
            Visit(node.LHS);
            Visit(node.RHS);
        }

        protected virtual void VisitBlock(Block node) {
            foreach (var s in node.Statements)
                Visit(s);
        }

        protected virtual void VisitBreakStatement(BreakStatement node) { }

        protected virtual void VisitConditionalGotoStatement(ConditionalGotoStatement node) {
            Visit(node.Condition);
            // Skip over the intermediate block as we do NOT want to read that.
            Visit(node.TrueBranch.Statements[0]);
        }

        protected virtual void VisitContinueStatement(ContinueStatement node) { }

        protected virtual void VisitExpressionStatement(ExpressionStatement node) {
            Visit(node.Statement);
        }

        protected virtual void VisitForStatement(ForStatement node) {
            if (node.Initializer != null)
                Visit(node.Initializer);
            Visit(node.Condition);
            if (node.Increment != null)
                Visit(node.Increment);
            Visit(node.Body);
        }

        protected virtual void VisitGotoLabelStatement(GotoLabelStatement node) { }

        protected virtual void VisitGotoStatement(GotoStatement node) {
            Visit(node.Target);
        }

        protected virtual void VisitIdentifierName(IdentifierName node) { }

        protected virtual void VisitIfStatement(IfStatement node) {
            if (node is ConditionalGotoStatement cond) {
                VisitConditionalGotoStatement(cond);
                return;
            }

            Visit(node.Condition);
            Visit(node.TrueBranch);
            if (node.FalseBranch != null)
                Visit(node.FalseBranch);
        }

        protected virtual void VisitIndexExpression(IndexExpression node) {
            Visit(node.Expression);
            Visit(node.Index);
        }

        protected virtual void VisitInvocationExpression(InvocationExpression node) {
            Visit(node.Target);
            foreach (var a in node.Arguments)
                Visit(a);
        }

        protected virtual void VisitLiteralExpression(LiteralExpression node) { }

        protected virtual void VisitLocalDeclarationStatement(LocalDeclarationStatement node) {
            if (node is LocalDeclarationStatementWithoutInit noInit) {
                VisitLocalDeclarationStatementWithoutInit(noInit);
                return;
            }
            Visit(node.Declaration);
        }

        protected virtual void VisitLocalDeclarationStatementWithoutInit(LocalDeclarationStatementWithoutInit node) {
            Visit(node.Declaration);
        }

        protected virtual void VisitMatrixExpression(MatrixExpression node) {
            foreach (var e in node.Entries)
                Visit(e);
        }

        protected virtual void VisitMethodDeclaration(MethodDeclaration node) {
            Visit(node.Identifier);
            foreach (var a in node.Arguments)
                Visit(a);
            Visit(node.Body);
        }

        protected virtual void VisitPolarExpression(PolarExpression node) {
            Visit(node.Angle);
            Visit(node.Radius);
        }

        protected virtual void VisitPostfixUnaryExpression(PostfixUnaryExpression node) {
            Visit(node.Expression);
        }

        protected virtual void VisitPrefixUnaryExpression(PrefixUnaryExpression node) {
            Visit(node.Expression);
        }

        protected virtual void VisitRepeatStatement(RepeatStatement node) {
            if (node.Count != null)
                Visit(node.Count);
            Visit(node.Body);
        }

        protected virtual void VisitReturnStatement(ReturnStatement node) {
            if (node.ReturnValue != null)
                Visit(node.ReturnValue);
        }

        protected virtual void VisitRoot(Root node) {
            foreach (var d in node.Declarations)
                Visit(d);
            foreach (var s in node.RootLevelStatements)
                Visit(s);
        }

        protected virtual void VisitVariableDeclaration(VariableDeclaration node) {
            Visit(node.Identifier);
            if (node.Initializer != null)
                Visit(node.Initializer);
        }

        protected virtual void VisitWhileStatement(WhileStatement node) {
            Visit(node.Condition);
            Visit(node.Body);
        }
    }
}
