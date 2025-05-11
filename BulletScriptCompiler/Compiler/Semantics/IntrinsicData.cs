using Atrufulgium.BulletScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Atrufulgium.BulletScript.Compiler.Syntax.Type;

// Note: Whenever adding a method, you also need to add an opcode probably, and
// also need to update Atrufulgium.BulletScript.Compiler.Syntax.EmittingPainAndSuffering.cs.

namespace Atrufulgium.BulletScript.Compiler.Semantics {
    internal static class IntrinsicData {

        /// <summary>
        /// Returns all intrinsic variables (that should be added to a syntax tree).
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<VariableDeclaration> GetIntrinsicVariables() {
            List<VariableDeclaration> res = new();
            var loc = Location.CompilerIntroduced;

            void AddIntrinsic(string name, Syntax.Type type, Expression initializer) {
                res.Add(new VariableDeclaration(
                    new(name, loc),
                    type,
                    loc,
                    initializer
                ));
            }

            // Remark: These are checked for syntactic correctness.
            // Still be careful they correspond to the semantics we want.
            // Remark II: These initial values are not used anywhere.
            // Be sure to keep this in sync with
            /// <see cref="BytecodeOutput"/>
            /// <see cref="EmitWalker.ExplicitVariableIDs"/>
            // and probably the docs.

            AddIntrinsic("bullettype", Syntax.Type.String, new LiteralExpression("\"error\"", loc));
            AddIntrinsic("spawnrotation", Float, new LiteralExpression(0, loc));
            AddIntrinsic("spawnspeed", Float, new LiteralExpression(1, loc));
            AddIntrinsic("spawnrelative", Float, new LiteralExpression(1, loc));
            AddIntrinsic("spawnposition", Vector2,
                new MatrixExpression(new List<Expression>() {
                    new LiteralExpression(0, loc),
                    new LiteralExpression(0, loc)
                }, 2, 1, loc)
            );

            // TODO: Move these to BSS
            AddIntrinsic("harmsenemies", Float, new LiteralExpression(0, loc));
            AddIntrinsic("harmsplayers", Float, new LiteralExpression(1, loc));
            AddIntrinsic("clearimmune", Float, new LiteralExpression(0, loc));
            AddIntrinsic("autoclear", Float, new LiteralExpression(1, loc));
            AddIntrinsic("clearingtype", Float, new LiteralExpression(0, loc));
            AddIntrinsic("usepivot", Float, new LiteralExpression(0, loc));

            return res;
        }

        // The reason we don't include these explicitly in the tree, is because
        // they must have a body (or I must define an `extern` keyword).
        // Allowing that is too much effort, so I'm not bothering.
        /// <summary>
        /// Adds all intrinsic methods to <paramref name="table"/>.
        /// </summary>
        public static void ApplyIntrinsicMethods(PartialSymbolTable table) {
            void AddIntrinsic(string name, Syntax.Type returnType, params (Syntax.Type type, string name)[] args) {
                var loc = Location.CompilerIntroduced;
                MethodDeclaration fakeMethod = new(
                    new(name, loc),
                    returnType,
                    args.Select(
                        a => new LocalDeclarationStatement(
                            new VariableDeclaration(
                                new(a.name, loc),
                                a.type,
                                loc
                            ),
                            loc
                        )
                    ).ToList(),
                    new Block(Array.Empty<Statement>(), loc),
                    loc
                );
                var res = table.TryUpdate(fakeMethod, isIntrinsicMethod: true);
            }

            // Remark: These are *not* checked for correctness like everything else.
            // Please be careful.

            // After adding anything, be sure to update EmittingPainAndSuffering.cs:
            // - IntrinsicInvocationStatement if void;
            // - IntrinsicInvocationAssignmentStatement if nonvoid.

            // If I don't do this it complains about ambiguity.
            Syntax.Type Void = Syntax.Type.Void;
            Syntax.Type String = Syntax.Type.String;

            AddIntrinsic("wait", Void, (Float, "seconds"));
            AddIntrinsic("message", Void, (Float, "value"));
            AddIntrinsic("spawn", Void);
            AddIntrinsic("destroy", Void);
            AddIntrinsic("loadbackground", Void, (String, "background_id"));
            AddIntrinsic("addscript", Void);
            AddIntrinsic("addscript", Void, (String, "script_id"));
            AddIntrinsic("addscript", Void, (String, "script_id"), (Float, "passed_value"));
            AddIntrinsic("startscript", Void, (String, "script_id"));
            AddIntrinsic("startscript", Void, (String, "script_id"), (Float, "passed_value"));
            AddIntrinsic("startscriptmany", Void, (String, "script_id"));
            AddIntrinsic("startscriptmany", Void, (String, "script_id"), (Float, "passed_value"));
            AddIntrinsic("depivot", Void);
            AddIntrinsic("rotate", Void, (Float, "amount"));
            AddIntrinsic("setrotation", Void, (Float, "value"));
            AddIntrinsic("faceplayer", Void);
            AddIntrinsic("turnstoplayer", Float);
            AddIntrinsic("addspeed", Void, (Float, "amount"));
            AddIntrinsic("setspeed", Void, (Float, "value"));
            AddIntrinsic("gimmick", Void, (String, "gimmick"));
            AddIntrinsic("gimmick", Void, (String, "gimmick"), (Float, "param"));
            AddIntrinsic("gimmick", Void, (String, "gimmick"), (Float, "param1"), (Float, "param2"));
            AddIntrinsic("random", Float, (Float, "lower"), (Float, "upper"));
            AddIntrinsic("random", Vector2, (Vector2, "lower"), (Vector2, "upper"));
            AddIntrinsic("random", Vector3, (Vector3, "lower"), (Vector3, "upper"));
            AddIntrinsic("random", Vector4, (Vector4, "lower"), (Vector4, "upper"));
            AddIntrinsic("print", Void, (Float, "value"));
            AddIntrinsic("print", Void, (MatrixUnspecified, "value"));
            AddIntrinsic("print", Void, (String, "value"));
            AddIntrinsic("sin", Float, (Float, "value"));
            AddIntrinsic("sin", Vector2, (Vector2, "value"));
            AddIntrinsic("sin", Vector3, (Vector3, "value"));
            AddIntrinsic("sin", Vector4, (Vector4, "value"));
            AddIntrinsic("cos", Float, (Float, "value"));
            AddIntrinsic("cos", Vector2, (Vector2, "value"));
            AddIntrinsic("cos", Vector3, (Vector3, "value"));
            AddIntrinsic("cos", Vector4, (Vector4, "value"));
            AddIntrinsic("tan", Float, (Float, "value"));
            AddIntrinsic("tan", Vector2, (Vector2, "value"));
            AddIntrinsic("tan", Vector3, (Vector3, "value"));
            AddIntrinsic("tan", Vector4, (Vector4, "value"));
            AddIntrinsic("asin", Float, (Float, "value"));
            AddIntrinsic("asin", Vector2, (Vector2, "value"));
            AddIntrinsic("asin", Vector3, (Vector3, "value"));
            AddIntrinsic("asin", Vector4, (Vector4, "value"));
            AddIntrinsic("acos", Float, (Float, "value"));
            AddIntrinsic("acos", Vector2, (Vector2, "value"));
            AddIntrinsic("acos", Vector3, (Vector3, "value"));
            AddIntrinsic("acos", Vector4, (Vector4, "value"));
            AddIntrinsic("atan", Float, (Float, "value"));
            AddIntrinsic("atan", Vector2, (Vector2, "value"));
            AddIntrinsic("atan", Vector3, (Vector3, "value"));
            AddIntrinsic("atan", Vector4, (Vector4, "value"));
            AddIntrinsic("atan2", Float, (Float, "y"), (Float, "x"));
            AddIntrinsic("atan2", Vector2, (Vector2, "y"), (Vector2, "x"));
            AddIntrinsic("atan2", Vector3, (Vector3, "y"), (Vector3, "x"));
            AddIntrinsic("atan2", Vector4, (Vector4, "y"), (Vector4, "x"));
            AddIntrinsic("turns2rad", Float, (Float, "turns"));
            AddIntrinsic("turns2rad", Vector2, (Vector2, "turns"));
            AddIntrinsic("turns2rad", Vector3, (Vector3, "turns"));
            AddIntrinsic("turns2rad", Vector4, (Vector4, "turns"));
            AddIntrinsic("rad2turns", Float, (Float, "radians"));
            AddIntrinsic("rad2turns", Vector2, (Vector2, "radians"));
            AddIntrinsic("rad2turns", Vector3, (Vector3, "radians"));
            AddIntrinsic("rad2turns", Vector4, (Vector4, "radians"));
            AddIntrinsic("ceil", Float, (Float, "value"));
            AddIntrinsic("ceil", Vector2, (Vector2, "value"));
            AddIntrinsic("ceil", Vector3, (Vector3, "value"));
            AddIntrinsic("ceil", Vector4, (Vector4, "value"));
            AddIntrinsic("floor", Float, (Float, "value"));
            AddIntrinsic("floor", Vector2, (Vector2, "value"));
            AddIntrinsic("floor", Vector3, (Vector3, "value"));
            AddIntrinsic("floor", Vector4, (Vector4, "value"));
            AddIntrinsic("round", Float, (Float, "value"));
            AddIntrinsic("round", Vector2, (Vector2, "value"));
            AddIntrinsic("round", Vector3, (Vector3, "value"));
            AddIntrinsic("round", Vector4, (Vector4, "value"));
            AddIntrinsic("abs", Float, (Float, "value"));
            AddIntrinsic("abs", Vector2, (Vector2, "value"));
            AddIntrinsic("abs", Vector3, (Vector3, "value"));
            AddIntrinsic("abs", Vector4, (Vector4, "value"));
            AddIntrinsic("length", Float, (Float, "vector"));
            AddIntrinsic("length", Float, (Vector2, "vector"));
            AddIntrinsic("length", Float, (Vector3, "vector"));
            AddIntrinsic("length", Float, (Vector4, "vector"));
            AddIntrinsic("distance", Float, (Float, "a"), (Float, "b"));
            AddIntrinsic("distance", Float, (Vector2, "a"), (Vector2, "b"));
            AddIntrinsic("distance", Float, (Vector3, "a"), (Vector3, "b"));
            AddIntrinsic("distance", Float, (Vector4, "a"), (Vector4, "b"));
            // These are actually rewritten in compiletime and have _no_
            // associated emission/opcodes. See
            /// <see cref="Visitors.ConstantFoldRewriter.VisitInvocationExpression(InvocationExpression)"/>
            AddIntrinsic("mrows", Float, (MatrixUnspecified, "m"));
            AddIntrinsic("mcols", Float, (MatrixUnspecified, "m"));
        }
    }
}
