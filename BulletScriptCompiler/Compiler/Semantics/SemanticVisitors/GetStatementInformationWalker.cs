using Atrufulgium.BulletScript.Compiler.Helpers;
using Atrufulgium.BulletScript.Compiler.Syntax;
using Atrufulgium.BulletScript.Compiler.Visitors;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;
using BType = Atrufulgium.BulletScript.Compiler.Syntax.Type;

namespace Atrufulgium.BulletScript.Compiler.Semantics.SemanticVisitors {
    /// <summary>
    /// Symbol analysis is easiest in a few passes:
    /// <list type="number">
    /// <item> Add intrinsic information. </item>
    /// <item> Get all top-level function and variable info. </item>
    /// <item> Go through all function bodies line by line. </item>
    /// <item> Collate this partial data into a proper symbol table. </item>
    /// </list>
    /// This represents the third pass.
    /// </summary>
    /// <remarks>
    /// In particular, this gets the following data:
    /// <list type="bullet">
    /// <item> Definite type information for every variable. </item>
    /// <item> For methods, all edges of the call graph. </item>
    /// <item> For variables, the reading and writing nodes. </item>
    /// </list>
    /// </remarks>
    internal class GetStatementInformationWalker : AbstractTreeWalker {

        readonly PartialSymbolTable table;
        /// <summary>
        /// Explicit node types, not semantically connected. E.g. if a
        /// declaration gets assigned a type, all its references won't be
        /// updated with it. Do not use directly, use <see cref="GetType(Expression)"/>.
        /// </summary>
        public readonly Dictionary<Expression, BType> nodeTypes = new();

        readonly List<(string source, string target)> callGraph = new();
        readonly HashSet<string> readVariables = new();
        readonly HashSet<string> writtenVariables = new();
        readonly Dictionary<Node, string> symbolNameMap = new();

        MethodDeclaration? currentMethod = null;
        BType currentExpectedReturnType = BType.Error;

        public GetStatementInformationWalker(PartialSymbolTable table) {
            this.table = table;
        }

        protected override void VisitRoot(Root node) {
            currentMethod = null;
            currentExpectedReturnType = BType.Void;
            base.VisitRoot(node);
            table.SetExtraData(callGraph, readVariables, writtenVariables, symbolNameMap);
        }

        protected override void VisitMethodDeclaration(MethodDeclaration node) {
            currentMethod = node;
            currentExpectedReturnType = table.GetType(table.GetFullyQualifiedName(node));
            if (currentExpectedReturnType == BType.Error)
                throw new ArgumentException("This node should have a known return type already!", nameof(node));

            // Do not call the base as that goes to the identifiername.
            // We don't want that.
            // Manually visit everything but the identifier.
            foreach (var a in node.Arguments)
                Visit(a);
            Visit(node.Body);

            // Also some sanity checks for built-in methods.
            if (node.Identifier.Name == "main") {
                if (currentExpectedReturnType != BType.Void
                    || node.Arguments.Count != 1
                    || !BType.TypesAreCompatible(node.Arguments[0].Declaration.Type, BType.Float))
                    AddDiagnostic(MainMethodWrong(node));
            } else if (node.Identifier.Name == "on_message") {
                if (currentExpectedReturnType != BType.Void
                    || node.Arguments.Count != 1
                    || !BType.TypesAreCompatible(node.Arguments[0].Declaration.Type, BType.Float))
                    AddDiagnostic(OnMessageMethodWrong(node));
            } else if (node.Identifier.Name.StartsWith("on_health<")) {
                if (currentExpectedReturnType != BType.Void || node.Arguments.Count != 0)
                    AddDiagnostic(OnHealthMethodWrong(node));
            } else if (node.Identifier.Name.StartsWith("on_time<")) {
                if (currentExpectedReturnType != BType.Void || node.Arguments.Count != 0)
                    AddDiagnostic(OnTimeMethodWrong(node));
            } else if (node.Identifier.Name == "on_health") {
                AddDiagnostic(OnHealthWithoutArg(node.Identifier));
            } else if (node.Identifier.Name == "on_time") {
                AddDiagnostic(OnTimeWithoutArg(node.Identifier));
            }

            symbolNameMap[node] = table.GetFullyQualifiedName(node);
        }

        protected override void VisitVariableDeclaration(VariableDeclaration node) {
            var res = table.TryUpdate(node, containingMethod: currentMethod);
            if (res != null)
                AddDiagnostic(res);

            // Do not call the base as that goes to the identifiername and then
            // complains the thing I'm defining doesn't exist yet.
            // Instead only do the initializer.
            if (node.Initializer != null)
                base.Visit(node.Initializer);

            symbolNameMap[node] = table.GetFullyQualifiedName(node, currentMethod);
        }

        /// <summary>
        /// Gets the type of a node <i>that has been processed already</i>.
        /// If something went wrong / it does not exist, returns <see cref="Syntax.Type.Error"/>.
        /// </summary>
        BType GetType(Expression node)
            => nodeTypes.ContainsKey(node) ? nodeTypes[node] : BType.Error;

        // All expressions (and some statements) with their type parsing.
        // Types need to be determined detph-first, so the base call must be
        // the first line of each of these methods.
        // After all, you can't determine the type of "a+b" if you don't know
        // a and b's yet.

        // First statements

        protected override void VisitForStatement(ForStatement node) {
            base.VisitForStatement(node);
            if (!BType.TypesAreCompatible(GetType(node.Condition), BType.Float))
                AddDiagnostic(ConditionMustBeFloat(node.Condition));
        }

        protected override void VisitIfStatement(IfStatement node) {
            base.VisitIfStatement(node);
            if (!BType.TypesAreCompatible(GetType(node.Condition), BType.Float))
                AddDiagnostic(ConditionMustBeFloat(node.Condition));
        }

        protected override void VisitLocalDeclarationStatement(LocalDeclarationStatement node) {
            base.VisitLocalDeclarationStatement(node);
            BType? overridenType = null;
            if (node.Declaration.Initializer != null)
                overridenType = GetType(node.Declaration.Initializer);
            // No point in parsing stuff if we're off the rails already
            if (overridenType != BType.Error) {
                var res = table.TryUpdate(node.Declaration, currentMethod, overridenType);
                if (res != null)
                    AddDiagnostic(res);
            }
        }

        protected override void VisitRepeatStatement(RepeatStatement node) {
            base.VisitRepeatStatement(node);
            if (node.Count != null)
                if (!BType.TypesAreCompatible(GetType(node.Count), BType.Float))
                    AddDiagnostic(RepeatCountMustBeFloat(node.Count));
        }

        protected override void VisitReturnStatement(ReturnStatement node) {
            base.VisitReturnStatement(node);
            if (node.ReturnValue is Expression e) {
                if (currentExpectedReturnType == BType.Void) {
                    AddDiagnostic(VoidMayNotReturnExpression(node.ReturnValue));
                } else {
                    var actualType = GetType(e);
                    if (BType.TypesAreCompatible(currentExpectedReturnType, actualType)) {
                        currentExpectedReturnType = BType.GetMoreSpecificType(currentExpectedReturnType, actualType);
                    } else {
                        AddDiagnostic(MismatchingReturnType(node, currentExpectedReturnType, actualType));
                    }
                }
            } else {
                if (currentExpectedReturnType != BType.Void)
                    AddDiagnostic(MismatchingReturnType(node, currentExpectedReturnType, BType.Void));
            }
        }

        // Then expressions

        bool isAssignmentLHSIdentifier = false;
        protected override void VisitAssignmentExpression(AssignmentExpression node) {
            // Do this manually in order to not trigger the "read" when
            // visiting the identifier LHS.

            isAssignmentLHSIdentifier = true;
            base.Visit(node.LHS);
            isAssignmentLHSIdentifier = false;
            base.Visit(node.RHS);

            var lhs = GetType(node.LHS);
            var rhs = GetType(node.RHS);
            var res = CombineBinopTypes(lhs, rhs, node.OP.ToString());
            CheckIntroducedError(node.RHS, lhs, rhs, res, node.OP.ToString(), true);
            nodeTypes[node] = res;
            writtenVariables.Add(table.GetFullyQualifiedName(node.LHS, currentMethod));
            if (node.OP != AssignmentOp.Set)
                readVariables.Add(table.GetFullyQualifiedName(node.LHS, currentMethod));
            // Note: I would disallow assignments `matrix = matrix`, but it is
            // impossible to have an ill-defined rhs size.
        }

        protected override void VisitBinaryExpression(BinaryExpression node) {
            base.VisitBinaryExpression(node);
            var lhs = GetType(node.LHS);
            var rhs = GetType(node.RHS);
            var res = CombineBinopTypes(lhs, rhs, node.OP.ToString());
            CheckIntroducedError(node.RHS, lhs, rhs, res, node.OP.ToString());
            nodeTypes[node] = res;
        }

        protected override void VisitIdentifierName(IdentifierName node) {
            base.VisitIdentifierName(node);
            // grab from the table
            // note: not being called from invocation.
            var fqn = table.GetFullyQualifiedName(node, currentMethod);
            var type = table.GetType(fqn);
            if (type == BType.Error || type == BType.MatrixUnspecified)
                AddDiagnostic(UnknownTypeAtThisPoint(node));
            nodeTypes[node] = type;
            if (!isAssignmentLHSIdentifier)
                readVariables.Add(fqn);

            symbolNameMap[node] = fqn;
        }

        protected override void VisitIndexExpression(IndexExpression node) {
            base.VisitIndexExpression(node);
            // check whether expression is matrixtyped
            // this then always returns float
            if (!GetType(node.Expression).IsMatrix)
                AddDiagnostic(CanOnlyIndexMatrices(node.Expression));
            nodeTypes[node] = BType.Float;
        }

        protected override void VisitInvocationExpression(InvocationExpression node) {
            // Not calling base and manually going through the arguments.
            // This is because also reaching Target gives unknown stuff.
            foreach (var a in node.Arguments)
                Visit(a);

            // grab from the table
            // ignore the identifier's "known" nodeType
            var fqn = table.GetFullyQualifiedMethodName(node.Target, node.Arguments.Select(a => GetType(a)));
            var type = table.GetType(fqn);
            if (type == BType.Error) {
                // Don't do the diagnostic if any of the *arguments* are errors.
                bool honestTypeError = true;
                foreach (var a in node.Arguments) {
                    if (GetType(a) == BType.Error) {
                        honestTypeError = false;
                        break;
                    }
                }
                if (honestTypeError)
                    AddDiagnostic(UndefinedMethodOrOverload(node));
            }
            nodeTypes[node] = type;

            if (currentMethod != null)
                callGraph.Add((table.GetFullyQualifiedName(currentMethod), fqn));

            symbolNameMap[node] = fqn;
        }

        protected override void VisitLiteralExpression(LiteralExpression node) {
            base.VisitLiteralExpression(node);
            // literally the easiest
            if (node.StringValue != null)
                nodeTypes[node] = BType.String;
            else if (node.FloatValue != null)
                nodeTypes[node] = BType.Float;
            else
                throw new ArgumentException("Unsupported literal type in node.", nameof(node));
        }

        protected override void VisitMatrixExpression(MatrixExpression node) {
            base.VisitMatrixExpression(node);
            // matrices may only contain floats
            foreach (var entry in node.Entries) {
                if (GetType(entry) != BType.Float)
                    AddDiagnostic(MatricesMustBeFloats(entry));
            }
            // Even if it goes wrong, assume we have a proper matrix going forward.
            nodeTypes[node] = BType.Matrix(node.Rows, node.Cols);
        }

        protected override void VisitPolarExpression(PolarExpression node) {
            base.VisitPolarExpression(node);
            // matrices may only contain floats
            if (GetType(node.Angle) != BType.Float)
                AddDiagnostic(PolarMustBeFloats(node.Angle));
            if (GetType(node.Radius) != BType.Float)
                AddDiagnostic(PolarMustBeFloats(node.Radius));
            // Even if it goes wrong, assume it went right going forward.
            nodeTypes[node] = BType.Vector2;
        }

        protected override void VisitPostfixUnaryExpression(PostfixUnaryExpression node) {
            base.VisitPostfixUnaryExpression(node);
            // only allow postfixes for floats
            // i don't see too much sense in matrix++
            if (GetType(node.Expression) != BType.Float)
                AddDiagnostic(IncrementDecrementMustBeFloat(node.Expression));
            // Again, there is only one thing this can be when correct, so assume that.
            nodeTypes[node] = BType.Float;

            // "Read" gets handled by the child identifier.
            if (node.Expression is IdentifierName name)
                writtenVariables.Add(table.GetFullyQualifiedName(name, currentMethod));
        }

        protected override void VisitPrefixUnaryExpression(PrefixUnaryExpression node) {
            base.VisitPrefixUnaryExpression(node);
            // here we are `-` or `!`, which *does* make sense to want to do
            // entry-wise. So only disallow strings, and otherwise return "the same".
            if (GetType(node.Expression) == BType.String) {
                AddDiagnostic(NegateNotMustBeNumeric(node.Expression));
                nodeTypes[node] = BType.Error;
            } else {
                nodeTypes[node] = GetType(node.Expression);
            }
        }

        /// <summary>
        /// When applying an operation to the <paramref name="lhs"/> and <paramref name="rhs"/>,
        /// returns what the resulting type is. If incompatible, returns the
        /// <see cref="Syntax.Type.Error" /> type instead.
        /// </summary>
        /// <param name="opRepresentation">
        /// This string must be the result of either
        /// <list type="bullet">
        /// <item><see cref="AssignmentOp.ToString"/>; or</item>
        /// <item><see cref="BinaryOp.ToString"/>.</item>
        /// </list>
        /// Otherwise, this will throw an <see cref="ArgumentException"/>.
        /// </param>
        static BType CombineBinopTypes(BType lhs, BType rhs, string opRepresentation) {
            if (lhs == BType.Error || rhs == BType.Error)
                return BType.Error;
            if (lhs == BType.Void || rhs == BType.Void)
                return BType.Error;

            return opRepresentation switch {
                "=" or "+" or "-" or "/" or "%" or "^" or "&" or "|" or ">=" or ">" or "<=" or "<"
                    => CombineRegularArithmetic(lhs, rhs),
                "*" => CombineMul(lhs, rhs),
                "==" or "!="
                    => CombineEqualities(lhs, rhs),
                _ => throw new ArgumentException($"The given operator \"{opRepresentation}\" is not known.", nameof(opRepresentation)),
            };
        }

        /// <summary>
        /// This is <see cref="CombineBinopTypes(BType, BType, string)"/> where
        /// the operator is one of `=`, `+` `-` `/` `%` `^` `&` `|`, `>=`, `>`,
        /// `<=`, or `<`, which are both valid between numbers and same-sized
        /// matrices as entrywise ops.
        /// <br/>
        /// Notably, `*` is excluded due to matrix multiplication semantics.
        /// <br/>
        /// Furthermore, `==` and `!=` are excluded due to string stuff.
        /// </summary>
        static BType CombineRegularArithmetic(BType lhs, BType rhs) {
            if (lhs == BType.String || rhs == BType.String)
                return BType.Error;

            // Non-error non-string types on both hs.
            // This is only well-defined if same size.
            // This is handled by this method:
            return BType.GetMoreSpecificType(lhs, rhs);
        }

        static BType CombineMul(BType lhs, BType rhs) {
            if (lhs == BType.String || rhs == BType.String)
                return BType.Error;

            // Non-error non-string types on both hs.
            // Component-wise multiplication or square matmul.
            // This is as the regular case.
            var componentWiseType = BType.GetMoreSpecificType(lhs, rhs);
            if (componentWiseType != BType.Error)
                return componentWiseType;

            // Matmul
            // Recall only (u×v) x (v×w) is allowed, returning (u×w).
            if (!lhs.TryGetMatrixSize(out var size1))
                throw new UnreachablePathException();
            if (!rhs.TryGetMatrixSize(out var size2))
                throw new UnreachablePathException();

            if (size1.cols != size2.rows)
                return BType.Error;
            return BType.Matrix(size1.rows, size2.cols);
        }

        static BType CombineEqualities(BType lhs, BType rhs) {
            if (lhs == BType.String && rhs == BType.String)
                return BType.String;
            return CombineRegularArithmetic(lhs, rhs);
        }

        void CheckIntroducedError(Node location, BType lhs, BType rhs, BType res, string op, bool isAssignment = false) {
            // The, if any, error was not introduced in this node.
            if (lhs == BType.Error || rhs == BType.Error || res != BType.Error)
                return;

            if (op == "*" && lhs.IsMatrix && rhs.IsMatrix) {
                lhs.TryGetMatrixSize(out var size1);
                rhs.TryGetMatrixSize(out var size2);
                AddDiagnostic(MatrixMulSizeMismatch(location, size1, size2));
            } else if (!isAssignment) {
                AddDiagnostic(IncompatibleBinop(location, lhs, rhs, op));
            }
            if (isAssignment)
                AddDiagnostic(ClashingAssignment(location, lhs, rhs));
        }
    }
}
