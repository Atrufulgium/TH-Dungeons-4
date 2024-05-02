using Atrufulgium.BulletScript.Compiler.Helpers;
using System.Text.RegularExpressions;

namespace Atrufulgium.BulletScript.Compiler.Syntax {

    /// <summary>
    /// Represents a type that may appear in code.
    /// </summary>
    internal readonly struct Type : IEquatable<Type> {

        readonly byte id;

        private Type(byte id) => this.id = id;

        // order s.t. compatible types are more specific if lower id.
        /// <summary> Not a valid Type. </summary>
        public static readonly Type Error = new(0);
        /// <summary> Does not represent a type. Only valid for functions. </summary>
        public static readonly Type Void = new(1);
        /// <summary> A number. </summary>
        public static readonly Type Float = new(2);
        /// <summary> A (constant) string. </summary>
        public static readonly Type String = new(3);
        /// <summary> A 1-vector. </summary>
        public static readonly Type Vector1 = new(11);
        /// <summary> A 2-vector. </summary>
        public static readonly Type Vector2 = new(21);
        /// <summary> A 3-vector. </summary>
        public static readonly Type Vector3 = new(31);
        /// <summary> A 4-vector. </summary>
        public static readonly Type Vector4 = new(41);
        /// <summary> A 2x2-matrix. </summary>
        public static readonly Type Matrix2x2 = new(22);
        /// <summary> A 2x3-matrix. </summary>
        public static readonly Type Matrix2x3 = new(23);
        /// <summary> A 2x4-matrix. </summary>
        public static readonly Type Matrix2x4 = new(24);
        /// <summary> A 3x2-matrix. </summary>
        public static readonly Type Matrix3x2 = new(32);
        /// <summary> A 3x3-matrix. </summary>
        public static readonly Type Matrix3x3 = new(33);
        /// <summary> A 3x4-matrix. </summary>
        public static readonly Type Matrix3x4 = new(34);
        /// <summary> A 4x2-matrix. </summary>
        public static readonly Type Matrix4x2 = new(42);
        /// <summary> A 4x3-matrix. </summary>
        public static readonly Type Matrix4x3 = new(43);
        /// <summary> A 4x4-matrix. </summary>
        public static readonly Type Matrix4x4 = new(44);
        /// <summary> A matrix whose size is not yet determined. </summary>
        public static readonly Type MatrixUnspecified = new(99);

        public static Type Vector(int size) {
            if (size is < 1 or > 4)
                throw new ArgumentOutOfRangeException(nameof(size), "Only vectors size 1--4 are allowed.");
            return new((byte)(size * 10 + 1));
        }

        public static Type Matrix(int rows, int cols) {
            if (rows is < 1 or > 4)
                throw new ArgumentOutOfRangeException(nameof(rows), "Only matrix heights 1--4 are allowed.");
            if (cols is < 1 or > 4)
                throw new ArgumentOutOfRangeException(nameof(cols), "Only matrix widths 1--4 are allowed.");
            if (rows == 1)
                return Vector(cols);
            if (cols == 1)
                return Vector(rows);
            return new Type((byte)(rows * 10 + cols));
        }

        /// <summary>
        /// If you need to do something with a Type, but don't want to
        /// do a switch statement every single time, use this.
        /// <br/>
        /// This method requires you to handle all possibilities, without any
        /// opportunity to forget, and without requiring to handle a general
        /// case that cannot happen *cough* switches *cough*.
        /// <br/>
        /// Using named parameters when calling this is recommended.
        /// </summary>
        /// <param name="vectorFunc">The argument is the number of entries in the vector.</param>
        /// <param name="matrixFunc">The arguments are respectively the row and column count.</param>
        public T Handle<T>(
            Func<T> errorFunc,
            Func<T> voidFunc,
            Func<T> floatFunc,
            Func<T> stringFunc,
            Func<int, T> vectorFunc,
            Func<int, int, T> matrixFunc,
            Func<T> matrixUnspecifiedFunc
        ) => id switch {
            0 => errorFunc(),
            1 => voidFunc(),
            2 => floatFunc(),
            3 => stringFunc(),
            11 => vectorFunc(1),
            21 => vectorFunc(2),
            31 => vectorFunc(3),
            41 => vectorFunc(4),
            22 => matrixFunc(2,2),
            23 => matrixFunc(2,3),
            24 => matrixFunc(2,4),
            32 => matrixFunc(3,2),
            33 => matrixFunc(3,3),
            34 => matrixFunc(3,4),
            42 => matrixFunc(4,2),
            43 => matrixFunc(4,3),
            44 => matrixFunc(4,4),
            99 => matrixUnspecifiedFunc(),
            _ => throw new UnreachablePathException(),
        };

        /// <summary>
        /// If this is a <see cref="Vector1"/> (or <see cref="Float"/>),
        /// <see cref="Vector2"/>, <see cref="Vector3"/>, or <see cref="Vector4"/>,
        /// returns <c>true</c> and an integer 1--4 specifiying which it is.
        /// </summary>
        /// <returns></returns>
        public bool TryGetVectorSize(out int size) {
            size = 0;
            if (id == 2) {
                size = 1;
                return true;
            }
            if (id is not (11 or 21 or 31 or 41)) return false;
            size = id / 10;
            return true;
        }

        /// <summary>
        /// If this is any matrix (except <see cref="MatrixUnspecified"/>) or
        /// vector or float, returns <c>true</c> and integers 1--4 specifying
        /// the rows and cols.
        /// Vectors are automatically seen as "standing".
        /// </summary>
        public bool TryGetMatrixSize(out (int rows, int cols) size) {
            size = (0, 0);
            if (id == 2) {
                size = (1, 1);
                return true;
            }
            if (id <= 10 || id == 99)
                return false;
            size = (id / 10, id % 10);
            return true;
        }

        /// <summary>
        /// Whether this is an (unespecified) matrix, or vector.
        /// </summary>
        public bool IsMatrix => id >= 4;

        public bool Equals(Type other)
            => id == other.id;

        public override bool Equals(object? obj)
            => obj is Type type && Equals(type);

        public override int GetHashCode() => id;

        public static bool operator ==(Type a, Type b) => a.Equals(b);
        public static bool operator !=(Type a, Type b) => !(a == b);

        static readonly Regex matrixSizedRegex = new(@"^matrix[1-4]x[1-4]$");

        /// <summary>
        /// Tries to convert a string into a valid type.
        /// If unsuccessful, returns the <see cref="Error"/> Type.
        /// </summary>
        public static Type FromString(string value) {
            if (value == "void") return Void;
            if (value == "float") return Float;
            if (value == "string") return String;
            if (value == "matrix") return MatrixUnspecified;

            if (!matrixSizedRegex.IsMatch(value)) return Error;

            int rows = value[6] - '0';
            int cols = value[8] - '0';

            if (rows == 1)
                return new((byte)(10 * cols + 1));
            if (cols == 1)
                return new((byte)(10 * rows + 1));
            return new((byte)(10 * rows + cols));
        }

        public override string ToString()
            => Handle(
                errorFunc: () => "invalid type",
                voidFunc: () => "void",
                floatFunc: () => "float",
                stringFunc: () => "string",
                vectorFunc: (int n) => $"matrix1x{n}",
                matrixFunc: (int r, int c) => $"matrix{r}x{c}",
                matrixUnspecifiedFunc: () => "matrix"
            );

        /// <summary>
        /// Whether <paramref name="from"/> can also be seen as <paramref name="to"/>.
        /// </summary>
        public static bool TypesAreCompatible(Type from, Type to) {
            if (from == to)
                return true;
            if (from == MatrixUnspecified && to.IsMatrix)
                return true;
            if (from.IsMatrix && to == MatrixUnspecified)
                return true;
            if (from == Float && (to == MatrixUnspecified || to == Vector1))
                return true;
            if ((from == MatrixUnspecified || from == Vector1) && to == Float)
                return true;
            return false;
        }

        /// <summary>
        /// If <see cref="TypesAreCompatible(Type, Type)"/>, returns the more
        /// specific type of the two. Otherwise returns <see cref="Error"/>.
        /// </summary>
        public static Type GetMoreSpecificType(Type a, Type b) {
            if (!TypesAreCompatible(a, b))
                return Error;
            return new(Math.Min(a.id, b.id));
        }
    }
}
