namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// A non-user node representing an intrinsic call that writes into an
    /// identifier.
    /// </summary>
    internal class IntrinsicInvocationAssignmentStatement : Statement, IEmittable {

        public IdentifierName Identifier { get; private set; }
        public InvocationExpression Invocation { get; private set; }

        public IntrinsicInvocationAssignmentStatement(
            IdentifierName identifier,
            InvocationExpression invocation
        ) : base(Location.CompilerIntroduced) {
            Identifier = identifier;
            Invocation = invocation;
        }

        public override string ToString()
            => $"<intr. invocation assi.>\nidentifier:\n{Indent(Identifier)}\ninvocation:\n{Indent(Invocation)}";

        public override string ToCompactString()
            => $"<intr. invocation assi.> {Identifier.Name} = {Invocation.ToCompactString()}";

        public IntrinsicInvocationAssignmentStatement WithIdentifier(IdentifierName identifier)
            => new(identifier, Invocation);
        public IntrinsicInvocationAssignmentStatement WithInvocation(InvocationExpression invocation)
            => new(Identifier, invocation);

        // No decent validation as this depends on semantics, and the tree
        // itself does not have access to that.
        // I'm not gonna include the billion intrinsics by hand.
    }
}
