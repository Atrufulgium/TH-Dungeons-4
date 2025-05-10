using Atrufulgium.BulletScript.Compiler.Helpers;
using System;

namespace Atrufulgium.BulletScript.Compiler.Syntax {

    /// <summary>
    /// Represents an operator that may appear in binary expressions.
    /// </summary>
    internal readonly struct PostfixUnaryOp : IEquatable<PostfixUnaryOp> {

        readonly byte id;

        private PostfixUnaryOp(byte id) => this.id = id;

        public static readonly PostfixUnaryOp Error = new(0);
        public static readonly PostfixUnaryOp Increment = new(1);
        public static readonly PostfixUnaryOp Decrement = new(2);

        /// <summary>
        /// If you need to do something with a PostfixUnaryOp, but don't want to
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
            Func<T> incrementFunc,
            Func<T> decrementFunc
        ) => id switch {
            0 => errorFunc(),
            1 => incrementFunc(),
            2 => decrementFunc(),
            _ => throw new UnreachablePathException()
        };

        public bool Equals(PostfixUnaryOp other)
            => id == other.id;

        public override bool Equals(object? obj)
            => obj is PostfixUnaryOp op && Equals(op);

        public override int GetHashCode() => id;

        public static bool operator ==(PostfixUnaryOp a, PostfixUnaryOp b) => a.Equals(b);
        public static bool operator !=(PostfixUnaryOp a, PostfixUnaryOp b) => !(a == b);

        /// <summary>
        /// Tries to convert a string into a valid operator.
        /// If unsuccesfull, returns the <see cref="Error"/> operator.
        /// </summary>
        public static PostfixUnaryOp FromString(string value) {
            if (value == "++") return Increment;
            if (value == "--") return Decrement;
            return Error;
        }

        public override string ToString()
            => Handle(
                errorFunc: () => "invalid prefix",
                incrementFunc: () => "++",
                decrementFunc: () => "--"
            );
    }
}
