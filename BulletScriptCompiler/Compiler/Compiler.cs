using Atrufulgium.BulletScript.Compiler.Visitors;
using System;

namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// To compile code, call <see cref="Compiler.Compile(string)"/>.
    /// See the extended documentation for the syntax.
    /// </summary>
    public class Compiler {
        /// <summary>
        /// Compiles a piece of BulletScript.
        /// </summary>
        public static CompilationResult Compile(string code) {
            throw new NotImplementedException();
        }

        internal static IVisitor[] StandardCompilationOrder => new IVisitor[] {
            // (No assumptions)
            new ThatsUncalledForRewriter(),
            // After:
            // The only loop remaining is eternal repeat.
            new LoopsToEternalRepeatRewriter(),
            // Before:
            // The only loop is eternal repeat.
            // After:
            // There are no if-statements or loop structures, only goto.
            new RepeatsAndBranchesToGotoRewriter(),
            // After:
            // No more ++ or --.
            new ExplicitIncrementsRewriter(),
            // (No assumptions)
            new SimplifyNotRewriter(),
            // (No assumptions)
            new ConstantFoldRewriter(),
            // After:
            // All assignments are simple.
            new SimpleAssignmentRewriter(),
            // Before:
            // All assignments are simple, and there is no ++ or --.
            // After:
            // The RHS of any assignment is a single, non-nested expression.
            new FlattenArithmeticRewriter(),
            // Before these three:
            // Methods are not called inside other methods arguments.
            // After these three:
            // The only remaining methods are invocations, and the tree is in
            // statement form.
            new ExtractMethodArgsRewriter(),
            new ExtractReturnRewriter(),
            new RemoveMethodsRewriter(),
            // Before:
            // Tree in statement form.
            // After:
            // All variable declarations do not initialize their values.
            new FlattenInitializationsRewriter(),
            // Before:
            // Tree in statement form.
            // Only intrinsics are in simple or compound assignments, or
            // statements. No other invocations.
            // After:
            // Emittable intrinsics.
            new AcknowledgeIntrinsicsRewriter(),
            // Before:
            // Tree in statement form.
            // AcknowledgeInvocationRewriter has run, and all expressions
            // are part of a simple assignment whose RHS contains no nested
            // expressions, other than literals or identifiers being contained.
            new AcknowledgeSimpleAssignmentsRewriter(),
            // Before:
            // Tree in statement form.
            // Everything is emittable.
            new EmitWalker()
        };
    }
}
