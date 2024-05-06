using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents an expression as a stand-alone statement.
    /// </summary>
    internal class ExpressionStatement : Statement {
        public Expression Statement { get; private set; }

        public ExpressionStatement(
            Expression statement,
            Location location = default
        ) : base(location) {
            Statement = statement;
        }

        public override string ToString()
            => $"[expression statement]\nstatement:\n{Indent(Statement)}";

        public override string ToCompactString()
            => $"[expression]             {Statement.ToCompactString()}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path) {
            var childValidations = Statement.ValidateTree(path.Append(this));
            if (Statement is not (AssignmentExpression or InvocationExpression or PostfixUnaryExpression))
                childValidations = childValidations.Prepend(InvalidExpressionStatement(this));
            return childValidations;
        }

        public ExpressionStatement WithExpression(Expression statement)
            => new(statement, Location);
    }
}
