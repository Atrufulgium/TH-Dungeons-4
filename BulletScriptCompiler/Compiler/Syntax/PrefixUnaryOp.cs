using Atrufulgium.BulletScript.Compiler.Helpers;

namespace Atrufulgium.BulletScript.Compiler.Syntax {

    /// <summary>
    /// Represents an operator that may appear in binary expressions.
    /// </summary>
    internal readonly struct PrefixUnaryOp : IEquatable<PrefixUnaryOp> {

        readonly byte id;

        private PrefixUnaryOp(byte id) => this.id = id;

        public static readonly PrefixUnaryOp Error = new(0);
        public static readonly PrefixUnaryOp Negate = new(1);
        public static readonly PrefixUnaryOp Not = new(2);

        /// <summary>
        /// If you need to do something with a PrefixUnaryOp, but don't want to
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
            Func<T> negFunc,
            Func<T> notFunc
        ) => id switch {
            0 => errorFunc(),
            1 => negFunc(),
            2 => notFunc(),
            _ => throw new UnreachablePathException()
        };

        public bool Equals(PrefixUnaryOp other)
            => id == other.id;

        public override bool Equals(object? obj)
            => obj is PrefixUnaryOp op && Equals(op);

        public override int GetHashCode() => id;

        public static bool operator ==(PrefixUnaryOp a, PrefixUnaryOp b) => a.Equals(b);
        public static bool operator !=(PrefixUnaryOp a, PrefixUnaryOp b) => !(a == b);

        /// <summary>
        /// Tries to convert a string into a valid operator.
        /// If unsuccesfull, returns the <see cref="Error"/> operator.
        /// </summary>
        public static PrefixUnaryOp FromString(string value) {
            if (value == "-") return Negate;
            if (value == "!") return Not;
            return Error;
        }

        public override string ToString()
            => Handle(
                errorFunc: () => "invalid prefix",
                negFunc: () => "-",
                notFunc: () => "!"
            );
    }
}
