using Atrufulgium.BulletScript.Compiler.Parsing;
using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;
using Atrufulgium.BulletScript.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// To compile code, call <see cref="Compile(string)"/>.
    /// See the extended documentation for the syntax.
    /// </summary>
    public static class Compiler {
        /// <summary>
        /// Compiles a piece of standalone BulletScript code.
        /// </summary>
        public static CompilationResult Compile(string code) {
            try {

                // To AST
                var (tokens, diags) = new Lexer().ToTokens(code);
                if (CheckErrors(diags, diags, out CompilationResult? res)) return res;

                var (root, diags2) = new Parser().ToTree(tokens);
                diags.AddRange(diags2);
                if (CheckErrors(diags2, diags, out res)) return res;
                if (root == null)
                    throw new NullReferenceException("Unexpected null tree, without any diagnostics.");

                var diags3 = root.ValidateTree();
                diags.AddRange(diags3);
                if (CheckErrors(diags3, diags, out res)) return res;

                // Initial semantics
                var semanticModel = new SemanticModel(root);
                var diags4 = semanticModel.Diagnostics;
                diags.AddRange(diags4);
                if (CheckErrors(diags4, diags, out res)) return res;

                // Might wanna allow other orders in the future
                var visitors = StandardCompilationOrder;

                foreach (var visitor in visitors) {
                    // Let the visitor do its thing
                    visitor.Model = semanticModel;
                    visitor.Visit(root);

                    // Check if the visitor could've done its thing
                    var diagsN1 = visitor.Diagnostics;
                    diags.AddRange(diagsN1);
                    if (CheckErrors(diagsN1, diags, out res)) return res;
                    if (visitor.VisitResult == null)
                        throw new NullReferenceException($"Unexpected null tree after visitor {visitor}, without any diagnostics.");
                    if (visitor.VisitResult is not Root)
                        throw new InvalidOperationException($"Unexpected tree non-Root tree root {visitor.VisitResult} after visitor {visitor}, without any diagnostics.");
                    root = (Root)visitor.VisitResult;

                    // Check if the visitor wasn't a moron
                    var diagsN2 = root.ValidateTree();
                    diags.AddRange(diagsN2);
                    if (CheckErrors(diagsN2, diags, out res)) return res;

                    semanticModel = new SemanticModel(root);
                    var diagsN3 = semanticModel.Diagnostics;
                    diags.AddRange(diagsN3);
                    if (CheckErrors(diagsN3, diags, out res)) return res;
                }

                // Grab emission data and apply it
                var emitWalker = visitors.OfType<EmitWalker>().First();
                var bytecode = new BytecodeOutput(emitWalker);
                return new CompilationResult(diags, bytecode);

            } catch (Exception e) {
                return new CompilationResult(new[] { DiagnosticRules.InternalUnknown(e) });
            }
        }

        internal static bool CheckErrors(
            IEnumerable<Diagnostic> checkDiagnostics,
            IEnumerable<Diagnostic> allDiagnostics,
            [NotNullWhen(true)] out CompilationResult? res
        ) {
            if (checkDiagnostics.Where(d => d.DiagnosticLevel == DiagnosticLevel.Error).Any()) {
                res = new(allDiagnostics.ToList());
                return true;
            }
            res = null;
            return false;
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
            // After:
            // No two-index indexers remain, and matrix indexers are VM-friendly.
            new PrepareVMIndexersRewriter(),
            // After:
            // There is are no artihmetic nodes containing only literals.
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
            // The goto labels of VM methods, if any, are surrounded by ##s.
            // The variable names of VM method arguments, if any, are of the
            // form `##fullyqualifiedmethodname(types)##`.
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
            // After:
            // All expressions are emittable.
            new AcknowledgeSimpleAssignmentsRewriter(),
            // Before:
            // Tree is in emittable form.
            // Labels that may not be removed are ##'d.
            new RemoveGotoUnreachableRewriter(),
            // Before:
            // Tree is in emittable form.
            // After:
            // All high level VM opcodes in
            /// <see cref="EmitWalker.OPCodes"/>
            new EmitWalker()
        };
    }
}
