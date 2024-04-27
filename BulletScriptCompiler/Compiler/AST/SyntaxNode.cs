using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.AST {
    /// <summary>
    /// The base node all AST nodes derive from.
    /// </summary>
    internal abstract class Node {
        public Location Location { get; private set; }

        public Node(Location location) {
            Location = location;
        }
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

        public MethodDeclaration(
            IdentifierName identifier,
            IdentifierName type,
            IList<LocalDeclarationStatement> arguments,
            Location location
        ) : base(identifier, type, location) {
            Arguments = new(arguments);
        }
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
    }

    /// <summary>
    /// Represents a block of statements.
    /// </summary>
    internal class Block : Node {
        public ReadOnlyCollection<Statement> Statements { get; private set; }

        public Block(IList<Statement> statements, Location location) : base(location) {
            Statements = new(statements);
        }
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
    }

    /// <summary>
    /// Represents the <c>continue;</c> statement in a loop.
    /// </summary>
    internal class ContinueStatement : Statement {
        public ContinueStatement(Location location) : base(location) { }
    }

    /// <summary>
    /// Represents a <c>for</c>-loop.
    /// </summary>
    internal class ForStatement : Statement {
        public ForStatement(Location location) : base(location) { }
    }

    /// <summary>
    /// Represents a <c>loop</c>-loop.
    /// </summary>
    internal class LoopStatement : Statement {
        public LoopStatement(Location location) : base(location) { }
    }

    /// <summary>
    /// Represents a <c>repeat</c>-loop.
    /// </summary>
    internal class RepeatStatement : Statement {
        public RepeatStatement(Location location) : base(location) { }
    }

    /// <summary>
    /// Represents a <c>while</c>-loop.
    /// </summary>
    internal class WhileStatement : Statement {
        public WhileStatement(Location location) : base(location) { }
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
    }

    /// <summary>
    /// Represents an expression as a stand-alone statement.
    /// </summary>
    internal class ExpressionStatement : Statement {
        public Statement Statement { get; private set; }

        public ExpressionStatement(
            Statement statement,
            Location location
        ) : base(location) {
            Statement = statement;
        }
    }

    /// <summary>
    /// Represents a <c>return</c>-statement, optionally with a value.
    /// </summary>
    internal class ReturnStatement : Statement {
        public Expression? ReturnValue { get; private set; }

        public ReturnStatement(
            Expression? returnValue,
            Location location
        ) : base(location) {
            ReturnValue = returnValue;
        }
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
    /// operator <c>∘</c>.
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
    }

    /// <summary>
    /// <para>
    /// Represents an expression of the form <c>a ∘ b</c> for some
    /// operator <c>∘</c>.
    /// </para>
    /// <para>
    /// This includes the indexing operator <c>a[b]</c>.
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
    }

    /// <summary>
    /// <para>
    /// Represents a literal value, such as <c>23</c>, or <c>"hi"</c>.
    /// </para>
    /// <para>
    /// Note that "literal matrices" aren't a thing.
    /// </para>
    /// </summary>
    internal class LiteralExpression : Expression {
        public string Value { get; private set; }

        public LiteralExpression(
            string value,
            Location location
        ) : base(location) {
            Value = value;
        }
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
    }

    /// <summary>
    /// Represents a name, such as a type, variable, or function name.
    /// </summary>
    internal class IdentifierName : Expression {
        public string Name { get; private set; }

        public IdentifierName(string name, Location location) : base(location) {
            Name = name;
        }
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
    }
}
