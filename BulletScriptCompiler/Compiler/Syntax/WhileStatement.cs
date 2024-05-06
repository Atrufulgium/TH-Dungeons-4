namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents a <c>while</c>-loop.
    /// </summary>
    internal class WhileStatement : LoopStatement {
        public Expression Condition { get; private set; }
        public Block Body { get; private set; }

        public WhileStatement(Expression condition, Block body, Location location = default) : base(location) {
            Condition = condition;
            Body = body;
        }

        public override string ToString()
            => $"[while loop]\ncondition:\n{Indent(Condition)}\nbody:\n{Indent(Body)}";

        public override string ToCompactString()
            => $"[while loop]             while ({Condition})\n{CompactIndent(Body)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Condition.ValidateTree(path.Append(this))
            .Concat(Body.ValidateTree(path.Append(this)));

        public WhileStatement WithCondition(Expression condition)
            => new(condition, Body, Location);
        public WhileStatement WithBody(Block body)
            => new(Condition, body, Location);
    }
}
