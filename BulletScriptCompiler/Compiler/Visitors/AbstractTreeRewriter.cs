using Atrufulgium.BulletScript.Compiler.Helpers;
using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// Walks through a <see cref="Node"/> and all of its children, and allows
    /// updates to happen. As syntax trees are immutable, you need to apply
    /// the resulting tree yourself somewhere.
    /// </summary>
    /// <remarks>
    /// Suppose you want to change all <c>break;</c>s to <c>continue;</c>s
    /// and you want to remove all <c>continue;</c>s, for some reason.
    /// You'd implement that as follows:
    /// <code>
    ///     class BreakContinueRewriter : TreeRewriter {
    ///         protected override Node? VisitBreakStatement(BreakStatement node)
    ///             => new ContinueStatement(node.Location);
    ///         protected override Node? VisitContinueStatement(ContinueStatement node)
    ///             => null;
    ///     }
    /// </code>
    /// Note that "when it makes sense", you can remove nodes by returning <c>null</c>.
    /// <br/>
    /// In this case, we don't want to call the base methods.
    /// In general, the effect of base methods is as follows:
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
    /// Note that you do not have access to semantic information of newly
    /// introduced notes, nor to nodes after the base call.
    /// <br/>
    /// Also note that in the above example, the only reason the newly
    /// introduced <c>continue;</c> statements weren't immediately discarded as
    /// "non-present" by setting them to <c>null</c> in the other rewrite, is
    /// because we did omitted the base call. If we had written
    /// <code>
    ///             => base.VisitContinueStatement(new ContinueStatement(node.Location));
    /// </code>
    /// instead, it would have disappeared.
    /// </list>
    /// These are not the only options, but do give a general idea of your options.
    /// <para>
    /// Also note that despite all return types being nullable, <b>the base
    /// methods do not return null, ever</b>.
    /// <br/>
    /// ou only need to consider <c>null</c> in case you yourself introduce it.
    /// Go ham with the forgiving operator.
    /// </para>
    /// <para>
    /// Similarly, all the base methods <b>return the type they accept</b>.
    /// Again, hard to put in the type system, so go ham with casts.
    /// </para>
    /// </remarks>
    internal abstract class AbstractTreeRewriter : IVisitor {

        bool IVisitor.IsReadOnly => true;
        public Node? VisitResult { get; private set; }

        private readonly List<Diagnostic> diagnostics = new();
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; init; }

        public void AddDiagnostic(Diagnostic diagnostic) => diagnostics.Add(diagnostic);

        public SemanticModel Model { set; protected get; }

        public AbstractTreeRewriter() {
            Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics);
            Model = SemanticModel.Empty;
        }

        void IVisitor.Visit(Node node) {
            diagnostics.Clear();
            var res = Visit(node);
            if (res != null)
                foreach (var diag in res.ValidateTree())
                    AddDiagnostic(diag);
            VisitResult = res;
        }

        [DoesNotReturn]
        void ThrowUnknownNodeException(Node node)
            => throw new NotImplementedException($"TreeRewriter has no knowledge of {node.GetType()} nodes.");

        // Note when adding syntax nodes:
        // Always include intermediate classes in the hierarchy for broader
        // override options. Always have the full "Visit" calls in the bottom-
        // most class. (Doing them all over the place gives weird semantics to
        // these otherwise nicely behaving methods.)

        public virtual Node? Visit(Node node) {
            if (node is ITransientNode inode) throw new PersistentTransientException(inode);
            if (node is Declaration decl) return VisitDeclaration(decl);
            if (node is Expression expr) return VisitExpression(expr);
            if (node is Statement stat) return VisitStatement(stat);
            if (node is Block block) return VisitBlock(block);
            if (node is Root root) return VisitRoot(root);
            ThrowUnknownNodeException(node);
            throw new UnreachablePathException();
        }

        /// <summary>
        /// Visits a node, and returns the result as <typeparamref name="T"/>. <br/>
        /// If the result is <c>null</c>, and the generic type is not
        /// nullable, throws an <see cref="ArgumentNullException"/>. <br/>
        /// If the result is not castable to <typeparamref name="T"/>, throws an <see cref="InvalidCastException"/>.
        /// </summary>
        private T VisitAs<T>(Node node) where T : Node? {
            Node? nullableRes = Visit(node);
            var t = typeof(T);
            
            // The not-nullable T case.
            if (Nullable.GetUnderlyingType(t) == null) {
                Node res = nullableRes ?? throw new ArgumentNullException(nameof(node), "Require a non-null value, but a Visit `override` returned null.");
                return (T)res;
            }

            // The nullable T case.

            // (The c# compiler cannot know that that weird if-statement
            //  guarantees nullability is not a problem here.)
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
            return (T)nullableRes;
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }

        protected virtual Node? VisitDeclaration(Declaration node) {
            if (node is MethodDeclaration meth) return VisitMethodDeclaration(meth);
            if (node is VariableDeclaration varb) return VisitVariableDeclaration(varb);
            ThrowUnknownNodeException(node);
            throw new UnreachablePathException();
        }

        protected virtual Node? VisitExpression(Expression node) {
            if (node is AssignmentExpression ass) return VisitAssignmentExpression(ass);
            if (node is BinaryExpression bine) return VisitBinaryExpression(bine);
            if (node is IdentifierName id) return VisitIdentifierName(id);
            if (node is IndexExpression ind) return VisitIndexExpression(ind);
            if (node is InvocationExpression call) return VisitInvocationExpression(call);
            if (node is LiteralExpression lit) return VisitLiteralExpression(lit);
            if (node is MatrixExpression mat) return VisitMatrixExpression(mat);
            if (node is PolarExpression pole) return VisitPolarExpression(pole);
            if (node is PostfixUnaryExpression post) return VisitPostfixUnaryExpression(post);
            if (node is PrefixUnaryExpression pre) return VisitPrefixUnaryExpression(pre);
            ThrowUnknownNodeException(node);
            throw new UnreachablePathException();
        }

        protected virtual Node? VisitLoopStatement(LoopStatement node) {
            if (node is ForStatement fors) return VisitForStatement(fors);
            if (node is RepeatStatement reps) return VisitRepeatStatement(reps);
            if (node is WhileStatement whil) return VisitWhileStatement(whil);
            ThrowUnknownNodeException(node);
            throw new UnreachablePathException();
        }

        protected virtual Node? VisitStatement(Statement node) {
            if (node is LoopStatement loop) return VisitLoopStatement(loop);
            if (node is BreakStatement brea) return VisitBreakStatement(brea);
            if (node is ContinueStatement cont) return VisitContinueStatement(cont);
            if (node is ExpressionStatement expr) return VisitExpressionStatement(expr);
            if (node is GotoStatement got) return VisitGotoStatement(got);
            if (node is GotoLabelStatement gotl) return VisitGotoLabelStatement(gotl);
            if (node is IfStatement ifs) return VisitIfStatement(ifs);
            if (node is LocalDeclarationStatement loco) return VisitLocalDeclarationStatement(loco);
            if (node is ReturnStatement ret) return VisitReturnStatement(ret);
            ThrowUnknownNodeException(node);
            throw new UnreachablePathException();
        }

        protected virtual Node? VisitAssignmentExpression(AssignmentExpression node)
            => node
            .WithLHS(VisitAs<IdentifierName>(node.LHS))
            .WithRHS(VisitAs<Expression>(node.RHS));

        protected virtual Node? VisitBinaryExpression(BinaryExpression node)
            => node
                .WithLHS(VisitAs<Expression>(node.LHS))
                .WithRHS(VisitAs<Expression>(node.RHS));

        protected virtual Node? VisitBlock(Block node) {
            List<Statement> resultingBlock = new();
            foreach (var s in node.Statements) {
                var statement = Visit(s);
                if (statement is MultipleStatements m) {
                    resultingBlock.AddRange(m.Flatten());
                } else if (statement != null)
                    resultingBlock.Add((Statement)statement);
            }
            return node.WithStatements(resultingBlock);
        }

        protected virtual Node? VisitBreakStatement(BreakStatement node) => node;

        protected virtual Node? VisitConditionalGotoStatement(ConditionalGotoStatement node)
            => node
            .WithCondition(VisitAs<IdentifierName>(node.Condition))
            // Skip over the intermediate block as we do NOT want to modify that.
            .WithTarget(VisitAs<GotoStatement>(node.TrueBranch.Statements[0]));

        protected virtual Node? VisitContinueStatement(ContinueStatement node) => node;

        protected virtual Node? VisitExpressionStatement(ExpressionStatement node)
            => node.WithExpression(VisitAs<Expression>(node.Statement));

        protected virtual Node? VisitForStatement(ForStatement node) {
            if (node.Initializer != null)
                node = node.WithInitializer(VisitAs<Statement?>(node.Initializer));
            node = node.WithCondition(VisitAs<Expression>(node.Condition));
            if (node.Increment != null)
                node = node.WithIncrement(VisitAs<Expression?>(node.Increment));
            return node.WithBody(VisitAs<Block>(node.Body));
        }

        protected virtual Node? VisitGotoLabelStatement(GotoLabelStatement node) => node;

        protected virtual Node? VisitGotoStatement(GotoStatement node)
            => node.WithTarget(VisitAs<GotoLabelStatement>(node));

        protected virtual Node? VisitIdentifierName(IdentifierName node) => node;

        protected virtual Node? VisitIfStatement(IfStatement node) {
            if (node is ConditionalGotoStatement cond) return VisitConditionalGotoStatement(cond);

            node = node
                .WithCondition(VisitAs<Expression>(node.Condition))
                .WithTrueBranch(VisitAs<Block>(node.TrueBranch));
            if (node.FalseBranch != null)
                node = node.WithFalseBranch(VisitAs<Block?>(node.FalseBranch));
            return node;
        }

        protected virtual Node? VisitIndexExpression(IndexExpression node)
            => node
            .WithExpression(VisitAs<Expression>(node.Expression))
            .WithIndex(VisitAs<MatrixExpression>(node.Index));

        protected virtual Node? VisitInvocationExpression(InvocationExpression node) {
            node = node.WithTarget(VisitAs<IdentifierName>(node.Target));
            
            List<Expression> args = new();
            foreach (var a in node.Arguments) {
                var arg = Visit(a);
                if (arg != null)
                    args.Add((Expression)arg);
            }
            return node.WithArguments(args);
        }

        protected virtual Node? VisitLiteralExpression(LiteralExpression node) => node;

        protected virtual Node? VisitLocalDeclarationStatement(LocalDeclarationStatement node)
            => node.WithDeclaration(VisitAs<VariableDeclaration>(node.Declaration));

        protected virtual Node? VisitMatrixExpression(MatrixExpression node) {
            // Do not allow nulls in this list unlike the others, as that screws
            // up the rows x cols thing.
            List<Expression> entries = new();
            foreach (var e in node.Entries)
                entries.Add(VisitAs<Expression>(e));
            return node.WithEntries(entries, node.Rows, node.Cols);
        }

        protected virtual Node? VisitMethodDeclaration(MethodDeclaration node) {
            node = node.WithIdentifier(VisitAs<IdentifierName>(node.Identifier));

            List<LocalDeclarationStatement> args = new();
            foreach (var a in node.Arguments) {
                var arg = Visit(a);
                if (arg != null)
                    args.Add((LocalDeclarationStatement)arg);
            }
            return node
                .WithArguments(args)
                .WithBody(VisitAs<Block>(node.Body));
        }

        protected virtual Node? VisitPolarExpression(PolarExpression node)
            => node
            .WithAngle(VisitAs<Expression>(node.Angle))
            .WithRadius(VisitAs<Expression>(node.Radius));

        protected virtual Node? VisitPostfixUnaryExpression(PostfixUnaryExpression node)
            => node.WithExpression(VisitAs<Expression>(node.Expression));

        protected virtual Node? VisitPrefixUnaryExpression(PrefixUnaryExpression node)
            => node.WithExpression(VisitAs<Expression>(node.Expression));

        protected virtual Node? VisitRepeatStatement(RepeatStatement node) {
            if (node.Count != null) {
                node = node.WithCount(VisitAs<Expression?>(node.Count));
            }
            return node.WithBody(VisitAs<Block>(node.Body));
        }

        protected virtual Node? VisitReturnStatement(ReturnStatement node) {
            if (node.ReturnValue != null) {
                node = node.WithReturnValue(VisitAs<Expression?>(node.ReturnValue));
            }
            return node;
        }

        protected virtual Node? VisitRoot(Root node) {
            if (node.Declarations.Count > 0) {
                List<Declaration> decls = new();
                foreach (var d in node.Declarations) {
                    var decl = Visit(d);
                    if (decl != null)
                        decls.Add((Declaration)decl);
                }
                return node.WithDeclarations(decls);
            }

            List<Statement> stats = new();
            foreach (var s in node.RootLevelStatements) {
                var stat = Visit(s);
                if (stat is MultipleStatements m) {
                    stats.AddRange(m.Flatten());
                } else if (stat != null)
                    stats.Add((Statement)stat);
            }
            return node.WithRootLevelStatements(stats);
        }

        protected virtual Node? VisitVariableDeclaration(VariableDeclaration node) {
            node = node.WithIdentifier(VisitAs<IdentifierName>(node.Identifier));
            if (node.Initializer != null)
                node = node.WithInitializer(VisitAs<Expression?>(node.Initializer));
            return node;
        }

        protected virtual Node? VisitWhileStatement(WhileStatement node)
            => node
            .WithCondition(VisitAs<Expression>(node.Condition))
            .WithBody(VisitAs<Block>(node.Body));
    }
}
