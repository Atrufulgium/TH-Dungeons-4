using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
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
            Location location = default,
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

        public ForStatement WithCondition(Expression condition)
            => new(condition, Body, Location, Initializer, Increment);
        public ForStatement WithBody(Block body)
            => new(Condition, body, Location, Initializer, Increment);
        public ForStatement WithInitializer(Statement? initializer)
            => new(Condition, Body, Location, initializer, Increment);
        public ForStatement WithIncrement(Expression? increment)
            => new(Condition, Body, Location, Initializer, increment);
    }
}
