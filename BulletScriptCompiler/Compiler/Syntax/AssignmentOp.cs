using Atrufulgium.BulletScript.Compiler.Helpers;

namespace Atrufulgium.BulletScript.Compiler.Syntax {

    /// <summary>
    /// Represents an operator that may appear in assignments.
    /// </summary>
    internal readonly struct AssignmentOp : IEquatable<AssignmentOp> {

        readonly byte id;

        private AssignmentOp(byte id) => this.id = id;

        public static readonly AssignmentOp Error = new(0);
        public static readonly AssignmentOp Set = new(1);
        public static readonly AssignmentOp Add = new(2);
        public static readonly AssignmentOp Sub = new(3);
        public static readonly AssignmentOp Mul = new(4);
        public static readonly AssignmentOp Div = new(5);
        public static readonly AssignmentOp Mod = new(6);
        public static readonly AssignmentOp Pow = new(7);
        public static readonly AssignmentOp And = new(8);
        public static readonly AssignmentOp Or = new(9);

        /// <summary>
        /// If you need to do something with an AssignmentOp, but don't want to
        /// do a switch statement every single time, use this.
        /// <br/>
        /// This method requires you to handle all possibilities, without any
        /// opportunity to forget, and without requiring to handle a general
        /// case that cannot happen *cough* switches *cough*.
        /// <br/>
        /// Using named parameters when calling this is recommended.
        /// </summary>
        public T Handle<T>(
            Func<T> errorFunc,
            Func<T> setFunc,
            Func<T> addFunc,
            Func<T> subFunc,
            Func<T> mulFunc,
            Func<T> divFunc,
            Func<T> modFunc,
            Func<T> powFunc,
            Func<T> andFunc,
            Func<T> orFunc
        ) => id switch {
            0 => errorFunc(),
            1 => setFunc(),
            2 => addFunc(),
            3 => subFunc(),
            4 => mulFunc(),
            5 => divFunc(),
            6 => modFunc(),
            7 => powFunc(),
            8 => andFunc(),
            9 => orFunc(),
            _ => throw new UnreachablePathException()
        };

        public bool Equals(AssignmentOp other)
            => id == other.id;

        public override bool Equals(object? obj)
            => obj is AssignmentOp op && Equals(op);

        public override int GetHashCode() => id;

        public static bool operator ==(AssignmentOp a, AssignmentOp b) => a.Equals(b);
        public static bool operator !=(AssignmentOp a, AssignmentOp b) => !(a == b);

        /// <summary>
        /// Tries to convert a string into a valid operator.
        /// If unsuccesfull, returns the <see cref="Error"/> operator.
        /// Both the `∘=` and `∘` format are supported.
        /// </summary>
        public static AssignmentOp FromString(string value) {
            if (value == "=") return Set;
            if (value is "+=" or "+") return Add;
            if (value is "-=" or "-") return Sub;
            if (value is "*=" or "*") return Mul;
            if (value is "/=" or "/") return Div;
            if (value is "%=" or "%") return Mod;
            if (value is "^=" or "^") return Pow;
            if (value is "&=" or "&") return And;
            if (value is "|=" or "|") return Or;
            return Error;
        }

        // This is used in
        /// <see cref="Semantics.SemanticVisitors.GetStatementInformationWalker.CombineBinopTypes(Type, Type, string)"/>
        public override string ToString()
            => Handle(
                errorFunc: () => "invalid assignment op",
                setFunc: () => "=",
                addFunc: () => "+",
                subFunc: () => "-",
                mulFunc: () => "*",
                divFunc: () => "/",
                modFunc: () => "%",
                powFunc: () => "^",
                andFunc: () => "&",
                orFunc: () => "|"
            );

        /// <summary>
        /// If a assignment operator corresponds to a binary operator, returns
        /// true and outputs that operator.
        /// </summary>
        public static bool TryGetBinop(AssignmentOp op, out BinaryOp binaryOp) {
            binaryOp = op.Handle(
                errorFunc: () => BinaryOp.Error,
                setFunc: () => BinaryOp.Error,
                addFunc: () => BinaryOp.Add,
                subFunc: () => BinaryOp.Sub,
                mulFunc: () => BinaryOp.Mul,
                divFunc: () => BinaryOp.Div,
                modFunc: () => BinaryOp.Mod,
                powFunc: () => BinaryOp.Pow,
                andFunc: () => BinaryOp.And,
                orFunc:  () => BinaryOp.Or
            );
            return binaryOp != BinaryOp.Error;
        }
    }
}
