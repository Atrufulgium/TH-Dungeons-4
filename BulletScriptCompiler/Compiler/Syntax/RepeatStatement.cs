namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents a <c>repeat</c>-loop.
    /// </summary>
    internal class RepeatStatement : LoopStatement {
        public Expression? Count { get; private set; }
        public Block Body { get; private set; }

        public RepeatStatement(Block body, Location location = default, Expression? count = null) : base(location) {
            Count = count;
            Body = body;
        }

        public override string ToString()
            => $"[repeat loop]\ncount:\n{Indent(Count)}\nbody:\n{Indent(Body)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => (Count?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>())
            .Concat(Body.ValidateTree(path.Append(this)));

        public RepeatStatement WithBody(Block body)
            => new(body, Location, Count);
        public RepeatStatement WithCount(Expression? count)
            => new(Body, Location, count);
    }
}
