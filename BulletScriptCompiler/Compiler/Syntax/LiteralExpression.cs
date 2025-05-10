using Atrufulgium.BulletScript.Compiler.Helpers;
using System;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
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
            Location location = default
        ) : base(location) {
            if (value[0] != '"' || value[^1] != '"')
                throw new ArgumentException("The string must be enclosed in \".");
            StringValue = value;
            FloatValue = null;
        }

        public LiteralExpression(
            float value,
            Location location = default
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

        public override string ToCompactString()
            => StringValue != null ? StringValue :
                FloatValue != null ? FloatValue.ToString()!:
                throw new UnreachablePathException();

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => new List<Diagnostic>();

        public LiteralExpression WithStringValue(string value)
            => new(value, Location);
        public LiteralExpression WithFloatValue(float value)
            => new(value, Location);
    }
}
