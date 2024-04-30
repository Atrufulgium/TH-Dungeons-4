using Atrufulgium.BulletScript.Compiler.Helpers;
using Atrufulgium.BulletScript.Compiler.Parsing;
using System.Collections.ObjectModel;
using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// The base node all AST nodes derive from.
    /// </summary>
    internal abstract class Node {
        public Location Location { get; private set; }

        public Node(Location location) {
            Location = location;
        }

        private const int indent = 4;
        protected static string Indent(string? str) {
            string indentStr = new(' ', indent);

            if (str == null || str == "")
                return indentStr + "[none]";

            return indentStr + str.ReplaceLineEndings()
                .ReplaceLineEndings()
                .Replace(Environment.NewLine, Environment.NewLine + indentStr);
        }
        protected static string Indent(Node? node)
            => Indent(node?.ToString());
        protected static string Indent<T>(IReadOnlyCollection<T>? nodes) where T : Node
            => nodes == null ? Indent((string?)null) : Indent(string.Join('\n', nodes));

        /// <summary>
        /// Check whether there are any obvious syntactic problems with the tree,
        /// with a given path of parent nodes.
        /// </summary>
        /// <param name="path">
        /// The path used to reach this node.
        /// </param>
        public abstract IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path);
        /// <summary>
        /// Checks whether there are any obvious syntactic problems with the tree.
        /// </summary>
        public IEnumerable<Diagnostic> ValidateTree()
            => ValidateTree(new List<Node>() { this } );
    }

    /// <summary>
    /// Represents the root node of the tree. This either contains a block of
    /// statements representing an implicit <c>main()</c> method, OR, function
    /// and variable definitions.
    /// </summary>
    internal class Root : Node {
        public ReadOnlyCollection<Declaration> Declarations { get; private set; }
        public ReadOnlyCollection<Statement> RootLevelStatements { get; private set; }

        public Root(IList<Declaration> declarations, Location location) : base(location) {
            Declarations = new(declarations);
            RootLevelStatements = new(Array.Empty<Statement>());
        }

        public Root(IList<Statement> statements, Location location) : base(location) {
            Declarations = new(Array.Empty<Declaration>());
            RootLevelStatements = new(statements);
        }

        public override string ToString() {
            string ret = "[root]\n";
            if (Declarations.Count > 0) {
                ret += $"declarations:\n{Indent(Declarations)}";
            } else {
                ret += $"statements:\n{Indent(RootLevelStatements)}";
            }
            return ret;
        }

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Declarations.SelectMany(d => d.ValidateTree(path.Append(this)))
            .Concat(RootLevelStatements.SelectMany(s => s.ValidateTree(path.Append(this))));
    }

    /// <summary>
    /// Represents any declaration.
    /// </summary>
    internal abstract class Declaration : Node {
        public IdentifierName Identifier { get; private set; }
        public IdentifierName Type { get; private set; }

        public Declaration(
            IdentifierName identifier,
            IdentifierName type,
            Location location
        ) : base(location) {
            Identifier = identifier;
            Type = type;
        }
    }

    /// <summary>
    /// <para>
    /// Represents declaring a function, such as <c>function void main(float value)</c>,
    /// <c>function matrix2x2 my_use_function(float value)</c>, or
    /// <c>function void on_health&lt;0.75&gt;()</c>.
    /// </para>
    /// <para>
    /// Note that the "generic" part is considered part of the identifier name.
    /// </para>
    /// </summary>
    internal class MethodDeclaration : Declaration {
        public ReadOnlyCollection<LocalDeclarationStatement> Arguments { get; private set; }
        public Block Body { get; private set; }

        public MethodDeclaration(
            IdentifierName identifier,
            IdentifierName type,
            IList<LocalDeclarationStatement> arguments,
            Block body,
            Location location
        ) : base(identifier, type, location) {
            Arguments = new(arguments);
            Body = body;
        }

        public override string ToString()
            => $"[method declaration]\nidentifier:\n{Indent(Identifier)}\ntype:\n{Indent(Type)}\n"
            + $"arguments:\n{Indent(Arguments)}\nblock:\n{Indent(Body)}";

        // IdentifierNames are trivially fine and don't need to be checked.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Arguments.SelectMany(s => s.ValidateTree(path.Append(this)))
            .Concat(Body.ValidateTree(path.Append(this)));
    }

    /// <summary>
    /// Represents declaring a variable.
    /// </summary>
    internal class VariableDeclaration : Declaration {
        public Expression? Initializer { get; private set; }

        public VariableDeclaration(
            IdentifierName identifier,
            IdentifierName type,
            Location location,
            Expression? initializer = null
        ) : base(identifier, type, location) {
            Initializer = initializer;
        }

        public override string ToString()
            => $"[variable declaration]\nidentifier:\n{Indent(Identifier)}\ntype:\n{Indent(Type)}\n"
            + $"initializer:\n{Indent(Initializer)}";

        // IdentifierNames are trivially fine and don't need to be checked.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Initializer?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>();
    }

    /// <summary>
    /// Represents a block of statements.
    /// </summary>
    internal class Block : Node {
        public ReadOnlyCollection<Statement> Statements { get; private set; }

        public Block(IList<Statement> statements, Location location) : base(location) {
            Statements = new(statements);
        }

        public override string ToString()
            => $"[block]\nstatements:\n{Indent(Statements)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Statements.SelectMany(s => s.ValidateTree(path.Append(this)));
    }

    /// <summary>
    /// Everything that can roughly be seen as "one" line of code.
    /// </summary>
    internal abstract class Statement : Node {
        public Statement(Location location) : base(location) { }
    }

    /// <summary>
    /// Represents the <c>break;</c> statement in a loop.
    /// </summary>
    internal class BreakStatement : Statement {
        public BreakStatement(Location location) : base(location) { }

        public override string ToString()
            => "[break]";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            if (path.OfType<LoopStatement>().Any())
                return new List<Diagnostic>();
            return new List<Diagnostic>() { BreakNotInLoop(this) };
        }
    }

    /// <summary>
    /// Represents the <c>continue;</c> statement in a loop.
    /// </summary>
    internal class ContinueStatement : Statement {
        public ContinueStatement(Location location) : base(location) { }

        public override string ToString()
            => "[continue]";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            if (path.OfType<LoopStatement>().Any())
                return new List<Diagnostic>();
            return new List<Diagnostic>() { ContinueNotInLoop(this) };
        }
    }

    /// <summary>
    /// Represents any looping mechanism.
    /// </summary>
    internal abstract class LoopStatement : Statement {
        public LoopStatement(Location location) : base(location) { }
    }

    /// <summary>
    /// Represents a <c>for</c>-loop.
    /// </summary>
    internal class ForStatement : LoopStatement {
        /// <summary>
        /// Either a <see cref="LocalDeclarationStatement"/> or an
        /// <see cref="ExpressionStatement"/> representing an
        /// <see cref="InvocationExpression"/> or <see cref="AssignmentExpression"/>.
        /// </summary>
        public Statement? Initializer { get; private set; }
        public Expression Condition { get; private set; }
        public Expression? Increment { get; private set; }
        public Block Body { get; private set; }

        public ForStatement(
            Expression condition,
            Block body,
            Location location,
            Statement? initializer = null,
            Expression? increment = null
        ) : base(location) {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Body = body;
        }

        public override string ToString()
            => $"[for loop]\ninitializer:\n{Indent(Initializer)}\ncondition:\n{Indent(Condition)}\n"
            + $"increment:\n{Indent(Increment)}\nbody:\n{Indent(Body)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var diags = (Initializer?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>())
                .Concat(Condition.ValidateTree(path.Append(this)))
                .Concat(Increment?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>())
                .Concat(Body.ValidateTree(path.Append(this)));

            if (Initializer != null) {
                if (Initializer is LocalDeclarationStatement)
                    return diags;
                if (Initializer is not ExpressionStatement expr) {
                    return diags.Prepend(InvalidForInitializer(this));
                }
                if (expr.Statement is not (InvocationExpression or AssignmentExpression))
                    return diags.Prepend(InvalidForInitializer(this));
            }
            return diags;
        }
    }

    /// <summary>
    /// Represents a <c>repeat</c>-loop.
    /// </summary>
    internal class RepeatStatement : LoopStatement {
        public Expression? Count { get; private set; }
        public Block Body { get; private set; }

        public RepeatStatement(Block body, Location location, Expression? count = null) : base(location) {
            Count = count;
            Body = body;
        }

        public override string ToString()
            => $"[repeat loop]\ncount:\n{Indent(Count)}\nbody:\n{Indent(Body)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => (Count?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>())
            .Concat(Body.ValidateTree(path.Append(this)));
    }

    /// <summary>
    /// Represents a <c>while</c>-loop.
    /// </summary>
    internal class WhileStatement : LoopStatement {
        public Expression Condition { get; private set; }
        public Block Body { get; private set; }

        public WhileStatement(Expression condition, Block body, Location location) : base(location) {
            Condition = condition;
            Body = body;
        }

        public override string ToString()
            => $"[while loop]\ncondition:\n{Indent(Condition)}\nbody:\n{Indent(Body)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Condition.ValidateTree(path.Append(this))
            .Concat(Body.ValidateTree(path.Append(this)));
    }

    /// <summary>
    /// Represents an <c>if</c>-statement, optionally with an <c>else</c> clause.
    /// </summary>
    internal class IfStatement : Statement {
        public Expression Condition { get; private set; }
        public Block TrueBranch { get; private set; }
        public Block? FalseBranch { get; private set; }

        public IfStatement(
            Expression condition,
            Block trueBranch,
            Location location,
            Block? falseBranch = null
        ) : base(location) {
            Condition = condition;
            TrueBranch = trueBranch;
            FalseBranch = falseBranch;
        }

        public override string ToString()
            => $"[if]\ncondition:\n{Indent(Condition)}\ntrue:\n{Indent(TrueBranch)}\nfalse:\n{Indent(FalseBranch)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Condition.ValidateTree(path.Append(this))
            .Concat(TrueBranch.ValidateTree(path.Append(this)))
            .Concat(FalseBranch?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>());
    }

    /// <summary>
    /// Represents declaring a variable inside a method.
    /// </summary>
    internal class LocalDeclarationStatement : Statement {
        public VariableDeclaration Declaration { get; private set; }

        public LocalDeclarationStatement(
            VariableDeclaration declaration,
            Location location
        ) : base(location) {
            Declaration = declaration;
        }

        public override string ToString()
            => $"[local declaration]\ndeclaration:\n{Indent(Declaration)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Declaration.ValidateTree(path.Append(this));
    }

    /// <summary>
    /// Represents an expression as a stand-alone statement.
    /// </summary>
    internal class ExpressionStatement : Statement {
        public Expression Statement { get; private set; }

        public ExpressionStatement(
            Expression statement,
            Location location
        ) : base(location) {
            Statement = statement;
        }

        public override string ToString()
            => $"[expression statement]\nstatement:\n{Indent(Statement)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Statement.ValidateTree(path.Append(this));
    }

    /// <summary>
    /// Represents a <c>return</c>-statement, optionally with a value.
    /// </summary>
    internal class ReturnStatement : Statement {
        public Expression? ReturnValue { get; private set; }

        public ReturnStatement(
            Location location,
            Expression? returnValue = null
        ) : base(location) {
            ReturnValue = returnValue;
        }

        public override string ToString()
            => $"[return]\nvalue:\n{Indent(ReturnValue)}";

        // Return may not be in a non-function, but that's impossible.
        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => ReturnValue?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>();
    }

    /// <summary>
    /// Expressions are the "meat" of the code. They, roughly, correspond to
    /// something you can put on the right-hand side of an equality.
    /// </summary>
    internal abstract class Expression : Node {
        public Expression(Location location) : base(location) { }
    }

    /// <summary>
    /// Represents an expression of the form <c>a ∘= b</c> for some (or no)
    /// operator <c>∘</c>. <see cref="OP"/> excludes the <c>=</c>-sign when in
    /// <c>∘=</c> form.
    /// </summary>
    internal class AssignmentExpression : Expression {
        public IdentifierName LHS { get; private set; }
        public string OP { get; private set; }
        public Expression RHS { get; private set; }

        public AssignmentExpression(
            IdentifierName lhs,
            string op,
            Expression rhs,
            Location location
        ) : base(location) {
            LHS = lhs;
            OP = op;
            RHS = rhs;
        }

        public override string ToString()
            => $"[assignment]\nlhs:\n{Indent(LHS)}\nop:\n{Indent(OP)}\nrhs:\n{Indent(RHS)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var childValidation = LHS.ValidateTree(path.Append(this))
                .Concat(RHS.ValidateTree(path.Append(this)));

            // Only allow assignments as a root level statement.
            // i.e. the parent must directly be an ExpressionStatement.
            var parent = path.LastOrDefault();
            if (parent != null && parent is not ExpressionStatement)
                return childValidation.Prepend(AssignmentOnlyAsStatement(this));
            return childValidation;
        }
    }

    /// <summary>
    /// <para>
    /// Represents an expression of the form <c>a ∘ b</c> for some
    /// operator <c>∘</c>.
    /// </para>
    /// </summary>
    internal class BinaryExpression : Expression {
        public Expression LHS { get; private set; }
        public string OP { get; private set; }
        public Expression RHS { get; private set; }

        public BinaryExpression(
            Expression lhs,
            string op,
            Expression rhs,
            Location location
        ) : base(location) {
            LHS = lhs;
            OP = op;
            RHS = rhs;
        }

        public override string ToString()
            => $"[binop]\nlhs:\n{Indent(LHS)}\nop:\n{Indent(OP)}\nrhs:\n{Indent(RHS)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => LHS.ValidateTree(path.Append(this))
            .Concat(RHS.ValidateTree(path.Append(this)));
    }

    /// <summary>
    /// Represents an expression of the form <c>a[b]</c>.
    /// </summary>
    internal class IndexExpression : Expression {
        public Expression Expression { get; private set; }
        public MatrixExpression Index { get; private set; }

        public IndexExpression(
            Expression expression,
            MatrixExpression index,
            Location location
        ) : base(location) {
            Expression = expression;
            Index = index;
        }

        public override string ToString()
            => $"[index]\nexpression:\n{Indent(Expression)}\nindex:\n{Indent(Index)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var ret = Expression.ValidateTree(path.Append(this));
            // Indices may only be 1x1, 1x2, or 2x1.
            if ((Index.Rows, Index.Cols) is not ((1,1) or (1,2) or (2,1))) {
                ret = ret.Append(IndexMatrixWrongSize(this));
            }
            return ret.Concat(Index.ValidateTree(path.Append(this)));
        }
    }

    /// <summary>
    /// Represents an expression of the form <c>∘a</c> for some
    /// operator <c>∘</c>.
    /// </summary>
    internal class PrefixUnaryExpression : Expression {
        public Expression Expression { get; private set; }
        public string OP { get; private set; }

        public PrefixUnaryExpression(
            Expression expression,
            string op,
            Location location
        ) : base(location) {
            Expression = expression;
            OP = op;
        }

        public override string ToString()
            => $"[prefix]\nop:\n{Indent(OP)}\nexpression:\n{Indent(Expression)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Expression.ValidateTree(path.Append(this));
    }

    /// <summary>
    /// Represents an expression of the form <c>a∘</c> for some
    /// operator <c>∘</c>.
    /// </summary>
    internal class PostfixUnaryExpression : Expression {
        public Expression Expression { get; private set; }
        public string OP { get; private set; }

        public PostfixUnaryExpression(
            Expression expression,
            string op,
            Location location
        ) : base(location) {
            Expression = expression;
            OP = op;
        }

        public override string ToString()
            => $"[postfix]\nop:\n{Indent(OP)}\nexpression:\n{Indent(Expression)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Expression.ValidateTree(path.Append(this));
    }

    /// <summary>
    /// <para>
    /// Represents a literal value, such as <c>23</c>, or <c>"hi"</c>. The set
    /// one is nonnull, the unset one is null.
    /// </para>
    /// <para>
    /// Note that "literal matrices" aren't a thing.
    /// </para>
    /// </summary>
    internal class LiteralExpression : Expression {
        public string? StringValue { get; private set; }
        public float? FloatValue { get; private set; }

        public LiteralExpression(
            string value,
            Location location
        ) : base(location) {
            if (value[0] != '"' || value[^1] != '"')
                throw new ArgumentException("The string must be enclosed in \".");
            StringValue = value;
            FloatValue = null;
        }

        public LiteralExpression(
            float value,
            Location location
        ) : base(location) {
            StringValue = null;
            FloatValue = value;
        }

        public override string ToString() {
            if (StringValue != null)
                return $"[literal string]\nvalue:\n{Indent(StringValue)}";
            if (FloatValue != null)
                return $"[literal float]\nvalue:\n{Indent(FloatValue.ToString())}";
            throw new UnreachablePathException();
        }

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => new List<Diagnostic>();
    }

    /// <summary>
    /// Represents a matrix, such as <c>[1]</c> or <c>[1 2; 3 4; 5 6]</c>,
    /// but not <c>[angle:radius]</c>.
    /// </summary>
    internal class MatrixExpression : Expression {
        public ReadOnlyCollection<Expression> Entries { get; private set; }
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        public MatrixExpression(
            IList<Expression> entries,
            int rows,
            int cols,
            Location location
        ) : base(location) {
            Entries = new(entries);
            Rows = rows;
            Cols = cols;
        }

        public override string ToString()
            => $"[matrix{Rows}x{Cols}]\nentries:\n{Indent(Entries)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Entries.SelectMany(e => e.ValidateTree(path.Append(this)));
    }

    /// <summary>
    /// Represents a matrix written down as <c>[angle:radius]</c>.
    /// </summary>
    internal class PolarExpression : Expression {
        public Expression Angle { get; private set; }
        public Expression Radius { get; private set; }

        public PolarExpression(
            Expression angle,
            Expression radius,
            Location location
        ) : base(location) {
            Angle = angle;
            Radius = radius;
        }

        public override string ToString()
            => $"[polar]\nangle:\n{Indent(Angle)}\nradius:\n{Indent(Radius)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Angle.ValidateTree(path.Append(this))
            .Concat(Radius.ValidateTree(path.Append(this)));
    }

    /// <summary>
    /// Represents a name, such as a type, variable, or function name.
    /// </summary>
    internal class IdentifierName : Expression {
        public string Name { get; private set; }

        public IdentifierName(string name, Location location) : base(location) {
            Name = name;
        }

        public IdentifierName(Token token) : base(token.Location) {
            Name = token.Value;
        }

        public override string ToString()
            => $"[identifier name]\nname:\n{Indent(Name)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => new List<Diagnostic>();
    }

    /// <summary>
    /// Represents a function call, whether built-in or not.
    /// </summary>
    internal class InvocationExpression : Expression {
        public IdentifierName Target { get; private set; }
        public ReadOnlyCollection<Expression> Arguments { get; private set; }

        public InvocationExpression(
            IdentifierName target,
            IList<Expression> arguments,
            Location location
        ) : base(location) {
            Target = target;
            Arguments = new(arguments);
        }

        public override string ToString()
            => $"[invocation]\ntarget:\n{Indent(Target)}\nargs:\n{Indent(Arguments)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Target.ValidateTree(path.Append(this))
            .Concat(Arguments.SelectMany(e => e.ValidateTree(path.Append(this))));
    }
}
