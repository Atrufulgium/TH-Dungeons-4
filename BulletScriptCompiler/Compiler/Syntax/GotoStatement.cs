﻿namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing a goto.
    /// </summary>
    internal class GotoStatement : Statement, IEmittable {
        public GotoLabelStatement Target { get; private set; }

        public GotoStatement(GotoLabelStatement target, Location location = default)
            : base(location) {
            Target = target;
        }

        public override string ToString()
            => $"[goto]\ntarget:\n{Indent(Target)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => new List<Diagnostic>();

        public GotoStatement WithTarget(GotoLabelStatement target)
            => new(target, Location);
    }
}