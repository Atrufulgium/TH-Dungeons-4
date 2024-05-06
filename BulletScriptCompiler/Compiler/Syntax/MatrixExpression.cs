using System.Collections.ObjectModel;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
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
            Location location = default
        ) : base(location) {
            Entries = new(entries);
            Rows = rows;
            Cols = cols;
        }

        public override string ToString()
            => $"[matrix{Rows}x{Cols}]\nentries:\n{Indent(Entries)}";

        public override string ToCompactString() {
            string res = "[";
            bool firstRow = true;
            for (int row = 0; row < Rows; row++) {
                if (firstRow) {
                    firstRow = false;
                } else {
                    res += ";";
                }

                bool firstCol = false;
                for (int col = 0; col < Cols; col++) {
                    if (firstCol) {
                        firstCol = false;
                    } else {
                        res += " ";
                    }

                    res += Entries[row * Cols + col].ToCompactString();
                }
            }
            res += "]";
            return res;
        }

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Entries.SelectMany(e => e.ValidateTree(path.Append(this)));

        public MatrixExpression WithEntries(IList<Expression> entries, int rows, int cols)
            => new(entries, rows, cols, Location);
    }
}
