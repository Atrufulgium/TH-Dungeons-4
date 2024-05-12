namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing an intrinsic call that does not return.
    /// </summary>
    internal partial class IntrinsicInvocationStatement : Statement, IEmittable {

        public InvocationExpression Invocation { get; private set; }

        public IntrinsicInvocationStatement(InvocationExpression invocation)
            : base(Location.CompilerIntroduced) {
            Invocation = invocation;
        }

        public override string ToString()
            => $"[intr. invocation stat.]\ninvocation:\n{Indent(Invocation)}";

        public override string ToCompactString()
            => $"<intr. invocation stat.> {Invocation.ToCompactString()}";

        public IntrinsicInvocationStatement WithInvocation(InvocationExpression invocation)
            => new(invocation);

        // No decent validation as this depends on semantics, and the tree
        // itself does not have access to that.
        // I'm not gonna include the billion intrinsics by hand.
    }
}
