using static Atrufulgium.BulletScript.Compiler.DiagnosticRules;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
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
}
