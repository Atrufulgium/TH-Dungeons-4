using Atrufulgium.BulletScript.Compiler.Helpers;

namespace Atrufulgium.BulletScript.Compiler.Syntax {

    /// <summary>
    /// Represents an operator that may appear in binary expressions.
    /// </summary>
    internal readonly struct BinaryOp : IEquatable<BinaryOp> {

        readonly byte id;

        private BinaryOp(byte id) => this.id = id;

        public static readonly BinaryOp Error = new(0);
        public static readonly BinaryOp Add = new(2);
        public static readonly BinaryOp Sub = new(3);
        public static readonly BinaryOp Mul = new(4);
        public static readonly BinaryOp Div = new(5);
        public static readonly BinaryOp Mod = new(6);
        public static readonly BinaryOp Pow = new(7);
        public static readonly BinaryOp And = new(8);
        public static readonly BinaryOp Or = new(9);
        public static readonly BinaryOp Eq = new(10);
        public static readonly BinaryOp Neq = new(11);
        public static readonly BinaryOp Gte = new(12);
        public static readonly BinaryOp Gt = new(13);
        public static readonly BinaryOp Lte = new(14);
        public static readonly BinaryOp Lt = new(15);

        /// <summary>
        /// If you need to do something with a BinaryOp, but don't want to
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
            Func<T> addFunc,
            Func<T> subFunc,
            Func<T> mulFunc,
            Func<T> divFunc,
            Func<T> modFunc,
            Func<T> powFunc,
            Func<T> andFunc,
            Func<T> orFunc,
            Func<T> eqFunc,
            Func<T> neqFunc,
            Func<T> gteFunc,
            Func<T> gtFunc,
            Func<T> lteFunc,
            Func<T> ltFunc
        ) => id switch {
            0 => errorFunc(),
            2 => addFunc(),
            3 => subFunc(),
            4 => mulFunc(),
            5 => divFunc(),
            6 => modFunc(),
            7 => powFunc(),
            8 => andFunc(),
            9 => orFunc(),
            10 => eqFunc(),
            11 => neqFunc(),
            12 => gteFunc(),
            13 => gtFunc(),
            14 => lteFunc(),
            15 => ltFunc(),
            _ => throw new UnreachablePathException()
        };

        public bool Equals(BinaryOp other)
            => id == other.id;

        public override bool Equals(object? obj)
            => obj is BinaryOp op && Equals(op);

        public override int GetHashCode() => id;

        public static bool operator ==(BinaryOp a, BinaryOp b) => a.Equals(b);
        public static bool operator !=(BinaryOp a, BinaryOp b) => !(a == b);

        /// <summary>
        /// Tries to convert a string into a valid operator.
        /// If unsuccesfull, returns the <see cref="Error"/> operator.
        /// </summary>
        public static BinaryOp FromString(string value) {
            if (value == "+") return Add;
            if (value == "-") return Sub;
            if (value == "*") return Mul;
            if (value == "/") return Div;
            if (value == "%") return Mod;
            if (value == "^") return Pow;
            if (value == "&") return And;
            if (value == "|") return Or;
            if (value == "==") return Eq;
            if (value == "!=") return Neq;
            if (value == ">=") return Gte;
            if (value == ">") return Gt;
            if (value == "<=") return Lte;
            if (value == "<") return Lt;
            return Error;
        }

        public override string ToString()
            => Handle(
                errorFunc: () => "invalid binary op",
                addFunc: () => "+",
                subFunc: () => "-",
                mulFunc: () => "*",
                divFunc: () => "/",
                modFunc: () => "%",
                powFunc: () => "^",
                andFunc: () => "&",
                orFunc: () => "|",
                eqFunc: () => "==",
                neqFunc: () => "!=",
                gteFunc: () => ">=",
                gtFunc: () => ">",
                lteFunc: () => "<=",
                ltFunc: () => "<"
            );
    }
}
