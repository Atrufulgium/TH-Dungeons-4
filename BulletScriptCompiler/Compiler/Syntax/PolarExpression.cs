﻿namespace Atrufulgium.BulletScript.Compiler.Syntax {
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
}
