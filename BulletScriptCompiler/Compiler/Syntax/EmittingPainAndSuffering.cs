using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Semantics;
using System;
using System.Collections.Generic;

// I may have had to rethink this, but I'm in too deep at this point.
// There are four classes emitting a total of three opcodes.
// Then there's THESE three monsters emitting like 200.

// A more sane idea would be to have each HLOP match to instrinsic strings.
// With reflection, grab a list of all HLOPs, do the tests, and return properly.
// Oh well.

namespace Atrufulgium.BulletScript.Compiler.Syntax {

    /// Most <see cref="IEmittable"/> syntax nodes handle opcode emission
    /// themselves. However, <i>some</i> are particularly disgusting, so I'm
    /// making them partial and containing their emission here.

    internal static class EmitHelpers {
        public static InvalidOperationException IntrinsicsMustBeConstantFolded(Node node)
            => new($"Intrinsic methods `{node.ToCompactString()}` must be constant folded. For most, having all arguments floats is not allowed.");

        /// <summary>
        /// Extracts the following data from an invocation:
        /// <list type="bullet">
        /// <item> The fully qualified name of the method. </item>
        /// <item> For up to two arguments, the string/float values. </item>
        /// <item> For up to two arguments, a bool indicating which it is. </item>
        /// </list>
        /// If we expect a string for the Xth argument, use <c>sX</c>.
        /// <br/>
        /// Otherwise, it depends on <c>isLitX</c>: literals use <c>fX</c>.
        /// </summary>
        public static (
            string name,
            string s1, string s2,
            float f1, float f2,
            bool isLit1, bool isLit2
        ) GetArgInfo(InvocationExpression invocation, SemanticModel model) {
            string name = model.GetSymbolInfo(invocation).FullyQualifiedName;
            var args = invocation.Arguments;
            // yes this is ugly and I'm discarding the entire type system by
            // doing this
            string s1 = "";
            string s2 = "";
            float f1 = 0;
            float f2 = 0;
            bool isLit1 = false;
            bool isLit2 = false;
            if (args.Count >= 1) {
                if (args[0] is LiteralExpression l) {
                    isLit1 = true;
                    if (l.StringValue != null)
                        s1 = l.StringValue;
                    if (l.FloatValue != null)
                        f1 = l.FloatValue.Value;
                } else if (args[0] is IdentifierName i) {
                    s1 = i.Name;
                } else throw new InvalidOperationException("Assumed only identifiers or literals");
            }
            if (args.Count >= 2) {
                if (args[1] is LiteralExpression l) {
                    isLit2 = true;
                    if (l.StringValue != null)
                        s2 = l.StringValue;
                    if (l.FloatValue != null)
                        f2 = l.FloatValue.Value;
                } else if (args[1] is IdentifierName i) {
                    s2 = i.Name;
                } else throw new InvalidOperationException("Assumed only identifiers or literals");
            }
            return (name, s1, s2, f1, f2, isLit1, isLit2);
        }
    }

    internal partial class IntrinsicInvocationAssignmentStatement {

        // These ops are easy: their single argument is never an identifier.
        // They do have a float and vector version though.
        // Note that float2, float3, and float4 are all just float4.
        readonly List<(string name, Func<string, string, HLOP> floatOp, Func<string, string, HLOP> vecOp)> easyOps = new() {
            ("sin(float)", HLOP.Sin, HLOP.Sin4),
            ("cos(float)", HLOP.Cos, HLOP.Cos4),
            ("tan(float)", HLOP.Tan, HLOP.Tan4),
            ("asin(float)", HLOP.Asin, HLOP.Asin4),
            ("acos(float)", HLOP.Acos, HLOP.Acos4),
            ("atan(float)", HLOP.Atan, HLOP.Atan4),
            ("ceil(float)", HLOP.Ceil, HLOP.Ceil4),
            ("floor(float)", HLOP.Floor, HLOP.Floor4),
            ("round(float)", HLOP.Round, HLOP.Round4),
            ("abs(float)", HLOP.Abs, HLOP.Abs4),
            ("length(float)", HLOP.Abs, HLOP.Length4),
        };
        
        List<HLOP> IEmittable.Emit(SemanticModel model) {
            string res = Identifier.Name;
            var (name, s1, s2, f1, f2, isLit1, isLit2) = EmitHelpers.GetArgInfo(Invocation, model);

            // General tactic: filter out the non-vector variant with explicit
            // comparison. Then the vector variant remains as "method_name(".
            if (name == "random(float,float)") {
                return (isLit1, isLit2) switch {
                    (true, true) => HLOP.Rng(res, f1, f2),
                    (true, false) => HLOP.Rng(res, f1, s2),
                    (false, true) => HLOP.Rng(res, s1, f2),
                    (false, false) => HLOP.Rng(res, s1, s2)
                };
            }
            if (name.StartsWith("random("))
                return HLOP.Rng4(res, s1, s2);

            foreach (var e in easyOps) {
                if (name == e.name) {
                    return isLit1 switch {
                        true => throw EmitHelpers.IntrinsicsMustBeConstantFolded(this),
                        false => e.floatOp(res, s1)
                    };
                }
                int parensIndex = e.name.IndexOf("(");
                string cut = e.name[..(parensIndex + 1)];
                if (name.StartsWith(cut))
                    return e.vecOp(res, s1);
            }

            if (name == "atan2(float,float)") {
                return (isLit1, isLit2) switch {
                    (true, true) => throw EmitHelpers.IntrinsicsMustBeConstantFolded(this),
                    (true, false) => HLOP.Atan2(res, f1, s2),
                    (false, true) => HLOP.Atan2(res, s1, f2),
                    (false, false) => HLOP.Atan2(res, s1, s2)
                };
            }
            if (name.StartsWith("atan2("))
                return HLOP.Atan24(res, s1, s2);

            if (name == "turns2rad(float)") {
                return isLit1 switch {
                    true => throw EmitHelpers.IntrinsicsMustBeConstantFolded(this),
                    false => HLOP.Angle2Rad(res, s1)
                };
            }
            if (name.StartsWith("turns2rad("))
                return HLOP.Angle2Rad4(res, s1);

            if (name == "rad2turns(float)") {
                return isLit1 switch {
                    true => throw EmitHelpers.IntrinsicsMustBeConstantFolded(this),
                    false => HLOP.Rad2Angle(res, s1)
                };
            }
            if (name.StartsWith("rad2turns("))
                return HLOP.Rad2Angle4(res, s1);

            if (name == "distance(float,float)") {
                return (isLit1, isLit2) switch {
                    (true, true) => throw EmitHelpers.IntrinsicsMustBeConstantFolded(this),
                    (true, false) => HLOP.Distance(res, f1, s2),
                    (false, true) => HLOP.Distance(res, s1, f2),
                    (false, false) => HLOP.Distance(res, s1, s2)
                };
            }
            if (name.StartsWith("distance("))
                return HLOP.Distance4(res, s1, s2);

            if (name == "turnstoplayer()")
                return HLOP.AngleToPlayer(res);

            throw new NotImplementedException($"Unknown intrinsic `{ToCompactString()}`");
        }
    }





    internal partial class IntrinsicInvocationStatement {
        List<HLOP> IEmittable.Emit(SemanticModel model) {
            var (name, s1, s2, f1, f2, isLit1, isLit2) = EmitHelpers.GetArgInfo(Invocation, model);
            var symbol = model.GetSymbolInfo(Invocation);
            bool isIntrinsicReturningNonVoid = symbol.IsIntrinsic && symbol.Type != Type.Void;

            // The one without any vector things, hooray.
            // Still a lot of permutations.

            return (name, isLit1, isLit2) switch {
                ("wait(float)", true, _) => HLOP.Pause(f1),
                ("wait(float)", false, _) => HLOP.Pause(s1),
                ("message(float)", true, _) => HLOP.Message(f1),
                ("message(float)", false, _) => HLOP.Message(s1),
                ("spawn()", _, _) => HLOP.Spawn(),
                ("destroy()", _, _) => HLOP.Destroy(),
                ("loadbackground(string)", _, _) => HLOP.LoadBackground(s1),
                ("addscript()", _, _) => HLOP.AddScript(),
                ("addscript(string)", _, _) => HLOP.AddScript(s1),
                ("addscript(string,float)", true, _) => HLOP.AddScript(s1, f2),
                ("addscript(string,float)", false, _) => HLOP.AddScript(s1, s2),
                ("startscript(string)", _, _) => HLOP.StartScript(s1),
                ("startscript(string,float)", true, _) => HLOP.StartScript(s1, f2),
                ("startscript(string,float)", false, _) => HLOP.StartScript(s1, s2),
                ("startscriptmany(string)", _, _) => HLOP.StartScriptMany(s1),
                ("startscriptmany(string,float)", true, _) => HLOP.StartScriptMany(s1, f2),
                ("startscriptmany(string,float)", false, _) => HLOP.StartScriptMany(s1, s2),
                ("depivot()", _, _) => HLOP.Depivot(),
                ("rotate(float)", true, _) => HLOP.AddRotation(f1),
                ("rotate(float)", false, _) => HLOP.AddRotation(s1),
                ("setrotation(float)", true, _) => HLOP.SetRotation(f1),
                ("setrotation(float)", false, _) => HLOP.SetRotation(s1),
                ("faceplayer()", _, _) => HLOP.FacePlayer(),
                ("addspeed(float)", true, _) => HLOP.AddSpeed(f1),
                ("addspeed(float)", false, _) => HLOP.AddSpeed(s1),
                ("setspeed(float)", true, _) => HLOP.SetSpeed(f1),
                ("setspeed(float)", false, _) => HLOP.SetSpeed(s1),
                ("gimmick(string)", _, _) => HLOP.Gimmick(s1),
                ("gimmick(string,float)", _, true) => HLOP.Gimmick(s1, f2),
                ("gimmick(string,float)", _, false) => HLOP.Gimmick(s1, s2),
                ("gimmick(string,float,float)", _, true)
                    => Invocation.Arguments[2] is LiteralExpression f3 ? HLOP.Gimmick(s1, f2, f3.FloatValue!.Value)
                     : Invocation.Arguments[2] is IdentifierName s3 ? HLOP.Gimmick(s1, f2, s3.Name)
                     : throw new InvalidOperationException("Assumed args are identifiers or literals."),
                ("gimmick(string,float,float)", _, false)
                   => Invocation.Arguments[2] is LiteralExpression f3 ? HLOP.Gimmick(s1, s2, f3.FloatValue!.Value)
                    : Invocation.Arguments[2] is IdentifierName s3 ? HLOP.Gimmick(s1, s2, s3.Name)
                    : throw new InvalidOperationException("Assumed args are identifiers or literals."),
                ("print(string)", _, _) => HLOP.Print(s1),
                ("print(float)", true, _) => HLOP.Print(f1),
                ("print(float)", false, _) => HLOP.Print(s1, 1, 1),
                ("print(matrix)", _, _) => model.GetExpressionType(Invocation.Arguments[0])
                    .TryGetMatrixSize(out var size)
                    ? HLOP.Print(s1, size.rows, size.cols)
                    : throw new InvalidOperationException("Assumed arg was interpretable as matrix."),
                // This assumes statement-level non-void intrinsics are pure,
                // which is the case if you assume RNG to be "pure" rng.
                _ when isIntrinsicReturningNonVoid => new List<HLOP>(),
                _ => throw new NotImplementedException($"Unknown intrinsic {ToCompactString()}")
            };
        }
    }





    internal partial class SimpleAssignmentStatement {

        // uggggly :(
        // The idea is that matrices of multiple rows need their operator
        // applied multiple times. So this handles that.
        // Exceptions: Vectors are always 1 op, and 2x2 mats are also 1 op.
        // Note that we assume everything is stored in 4-vectors, and we don't
        // care about the garbage in the entries we don't care about.
        static List<HLOP> VectoredOp(Func<string, string, HLOP> op, string lhs, string rhs, Type type) {
            List<HLOP> ret = op(lhs, rhs);
            if (type.TryGetVectorSize(out _))
                return ret;
            // We're larger than a vector.
            if (!type.TryGetMatrixSize(out var size))
                throw new InvalidOperationException("Vectored ops may only be applied to floats/vectors/matrices.");
            (int rows, int cols) = size;
            // 2x2 matrices are special and put into one row anyway
            if (rows == 2 && cols == 2)
                return ret;
            // Otherwise, one row per matrix row.
            for (int i = 1; i < rows; i++)
                ret.Add(op(lhs + "+" + i*4, rhs + "+" + i*4));
            return ret;
        }

        static List<HLOP> VectoredOp(Func<string, string, string, HLOP> op, string lhs, string rhs1, string rhs2, Type type) {
            List<HLOP> ret = op(lhs, rhs1, rhs2);
            if (type.TryGetVectorSize(out _))
                return ret;
            // We're larger than a vector.
            if (!type.TryGetMatrixSize(out var size))
                throw new InvalidOperationException("Vectored ops may only be applied to floats/vectors/matrices.");
            (int rows, int cols) = size;
            // 2x2 matrices are special and put into one row anyway
            if (rows == 2 && cols == 2)
                return ret;
            // Otherwise, one row per matrix row.
            for (int i = 1; i < rows; i++)
                ret.Add(op(lhs + "+" + i*4, rhs1 + "+" + i*4, rhs2 + "+" + i*4));
            return ret;
        }

        List<HLOP> IEmittable.Emit(SemanticModel model) {
            string lhs = LHS.Name;
            if (RHS is IdentifierName id) return EmitIdentifier(model, lhs, id);
            if (RHS is LiteralExpression lit) return EmitLiteral(lhs, lit);
            if (RHS is PrefixUnaryExpression un) return EmitUnary(model, lhs, un);
            if (RHS is BinaryExpression bin) return EmitBinary(model, lhs, bin);
            if (RHS is IndexExpression ind) return EmitIndex(lhs, ind);
            if (RHS is MatrixExpression mat) return EmitMatrix(model, lhs, mat);
            if (RHS is PolarExpression pol) return EmitPolar(lhs, pol);
            throw new NotSupportedException($"Bad node `{ToCompactString()}`");
        }

        static List<HLOP> EmitIdentifier(SemanticModel model, string lhs, IdentifierName rhs) {
            var type = model.GetExpressionType(rhs);
            if (Type.TypesAreCompatible(type, Type.Float))
                return HLOP.Set(lhs, rhs.Name);
            if (type == Type.String)
                return HLOP.SetString(lhs, rhs.Name);
            return VectoredOp(HLOP.Set4, lhs, rhs.Name, type);
        }

        static List<HLOP> EmitLiteral(string lhs, LiteralExpression rhs) {
            if (rhs.FloatValue != null)
                return HLOP.Set(lhs, rhs.FloatValue.Value);
            if (rhs.StringValue != null)
                return HLOP.SetString(lhs, rhs.StringValue);
            throw new NotImplementedException($"Unknown literal type `{rhs.ToCompactString()}`");
        }

        static List<HLOP> EmitUnary(SemanticModel model, string lhs, PrefixUnaryExpression rhs, Syntax.Type type = default) {
            if (type == default)
                type = model.GetExpressionType(rhs);
            // Guaranteed the RHS is an identifier
            var id = (IdentifierName)rhs.Expression;

            if (Type.TypesAreCompatible(type, Type.Float))
                return rhs.OP.Handle(
                    errorFunc: () => throw new NotSupportedException($"Unary RHS `{rhs.ToCompactString()}` is not identifier"),
                    negFunc: () => HLOP.Sub(lhs, 0, id.Name),
                    notFunc: () => HLOP.Not(lhs, id.Name)
                );
            return rhs.OP.Handle(
                errorFunc: () => throw new NotSupportedException(),
                negFunc: () => VectoredOp(HLOP.Negate4, lhs, id.Name, type),
                notFunc: () => VectoredOp(HLOP.Not4, lhs, id.Name, type)
            );
        }

        static readonly Dictionary<
            BinaryOp, (
            Func<string, string, string, HLOP> variableOp,
            Func<string,  float, string, HLOP> literalOp,
            Func<string, string, string, HLOP> vectorOp
        )> commutativeOps = new() {
            { BinaryOp.Add, (HLOP.Add, HLOP.Add, HLOP.Add4) },
            { BinaryOp.Mul, (HLOP.Mul, HLOP.Mul, HLOP.Mul4) },
            { BinaryOp.And, (HLOP.Mul, HLOP.Mul, HLOP.Mul4) },
            { BinaryOp.Or,  (HLOP.Add, HLOP.Add, HLOP.Add4) },
            { BinaryOp.Eq,  (HLOP.Equal, HLOP.Equal, HLOP.Equal4) },
        };

        static readonly Dictionary<
            BinaryOp, (
            Func<string, string, string, HLOP> variableOp,
            Func<string,  float, string, HLOP> literalOp1,
            Func<string, string,  float, HLOP> literalOp2,
            Func<string, string, string, HLOP> vectorOp
        )> nonCommutativeOps = new() {
            { BinaryOp.Sub, (HLOP.Sub, HLOP.Sub, HLOP.Sub, HLOP.Sub4) },
            { BinaryOp.Div, (HLOP.Div, HLOP.Div, HLOP.Div, HLOP.Div4) },
            { BinaryOp.Mod, (HLOP.Mod, HLOP.Mod, HLOP.Mod, HLOP.Mod4) },
            { BinaryOp.Pow, (HLOP.Pow, HLOP.Pow, HLOP.Pow, HLOP.Pow4) },
            { BinaryOp.Lt,  (HLOP.LT,  HLOP.LT,  HLOP.LT,  HLOP.LT4 ) },
            { BinaryOp.Lte, (HLOP.LTE, HLOP.LTE, HLOP.LTE, HLOP.LTE4) },
        };

        static readonly Dictionary<BinaryOp, BinaryOp> invertedOps = new() {
            { BinaryOp.Neq, BinaryOp.Eq },
        };

        static readonly Dictionary<BinaryOp, BinaryOp> swappedOps = new() {
            { BinaryOp.Gt, BinaryOp.Lt },
            { BinaryOp.Gte, BinaryOp.Lte }
        };

        static List<HLOP> HandleNonCommutativeBinop(
            string lhs,
            Expression rhs1, Expression rhs2,
            Type type,
            (Func<string, string, string, HLOP> variableOp,
             Func<string,  float, string, HLOP> literalOp1,
             Func<string, string,  float, HLOP> literalOp2,
             Func<string, string, string, HLOP> vectorOp) ops
        ) {
            // Non-vector case is ironically harder at this point
            if (!Type.TypesAreCompatible(type, Type.Float))
                return VectoredOp(ops.vectorOp, lhs, ((IdentifierName)rhs1).Name, ((IdentifierName)rhs2).Name, type);
            
            // Note: guaranteed not both LHS and RHS literal
            if (rhs1 is IdentifierName id1) {
                if (rhs2 is IdentifierName id2) {
                    return ops.variableOp(lhs, id1.Name, id2.Name);
                } else if (rhs2 is LiteralExpression lit2) {
                    return ops.literalOp2(lhs, id1.Name, lit2.FloatValue!.Value);
                }
            } else if (rhs1 is LiteralExpression lit1) {
                if (rhs2 is IdentifierName id2) {
                    return ops.literalOp1(lhs, lit1.FloatValue!.Value, id2.Name);
                }
            }
            throw new InvalidOperationException();
        }

        static List<HLOP> HandleCommutativeBinop(
            string lhs,
            Expression rhs1, Expression rhs2,
            Type type,
            (Func<string, string, string, HLOP> variableOp,
             Func<string,  float, string, HLOP> literalOp,
             Func<string, string, string, HLOP> vectorOp) ops
        ) {
            if (!Type.TypesAreCompatible(type, Type.Float))
                return VectoredOp(ops.vectorOp, lhs, ((IdentifierName)rhs1).Name, ((IdentifierName)rhs2).Name, type);
            
            if (rhs1 is IdentifierName id1) {
                if (rhs2 is IdentifierName id2) {
                    return ops.variableOp(lhs, id1.Name, id2.Name);
                } else if (rhs2 is LiteralExpression lit2) {
                    return ops.literalOp(lhs, lit2.FloatValue!.Value, id1.Name);
                }
            } else if (rhs1 is LiteralExpression lit1) {
                if (rhs2 is IdentifierName id2) {
                    return ops.literalOp(lhs, lit1.FloatValue!.Value, id2.Name);
                }
            }
            throw new InvalidOperationException();
        }

        static List<HLOP> EmitBinary(SemanticModel model, string lhs, BinaryExpression rhs) {
            // Almost literally everything is predictable arithmetic.
            // Exception: string ==, != string.
            var rhs1Type = model.GetExpressionType(rhs.LHS);
            var rhs2Type = model.GetExpressionType(rhs.RHS);
            if ((rhs.OP == BinaryOp.Eq || rhs.OP == BinaryOp.Neq)
                && rhs1Type == Type.String && rhs2Type == Type.String) {
                string lhsStr = rhs.LHS is IdentifierName id1 ? id1.Name : ((LiteralExpression)rhs.LHS).StringValue!;
                string rhsStr = rhs.RHS is IdentifierName id2 ? id2.Name : ((LiteralExpression)rhs.RHS).StringValue!;
                if (rhs.OP == BinaryOp.Eq)
                    return HLOP.EqualString(lhs, lhsStr, rhsStr);
                else
                    return new() {
                        HLOP.EqualString(lhs, lhsStr, rhsStr),
                        HLOP.Not(lhs, lhs)
                    };
            }
            // Exception: matrix multiplication
            if (rhs.OP == BinaryOp.Mul) {
                if (!Type.TypesAreCompatible(rhs1Type, rhs2Type)
                    || (rhs1Type.TryGetMatrixSize(out var size) && size.cols == size.rows && size.cols > 1)) {
                    rhs1Type.TryGetMatrixSize(out var size1);
                    rhs2Type.TryGetMatrixSize(out var size2);

                    // Vectors automatically update to match
                    if (size1.cols != size2.rows) {
                        // Match opportunity
                        if (size1.cols == size2.cols || size1.rows == size2.rows) {
                            // Actually need a match because a vector was screwed with
                            if (size1.rows == 1 || size1.cols == 1)
                                size1 = (size1.cols, size1.rows);
                            if (size2.rows == 1 || size2.cols == 1)
                                size2 = (size2.cols, size2.rows);
                        }
                    }

                    string mat1 = ((IdentifierName)rhs.LHS).Name;
                    string mat2 = ((IdentifierName)rhs.RHS).Name;
                    return HLOP.MatrixMul(size1.rows, size1.cols, size2.cols, lhs, mat1, mat2);
                }
            }

            var type = model.GetExpressionType(rhs);
            // Inverted ops need to be postprocessed with a not.
            bool inverted = false;
            if (invertedOps.TryGetValue(rhs.OP, out var invertedOp)) {
                inverted = true;
                var rhslhs = rhs.LHS;
                rhs = rhs.WithOP(invertedOp);
            }

            // There is no a>b or a>=b, so map these to b<a or b<=a resp.
            if (swappedOps.TryGetValue(rhs.OP, out var swappedOp)) {
                var rhslhs = rhs.LHS;
                rhs = rhs
                    .WithLHS(rhs.RHS)
                    .WithRHS(rhslhs)
                    .WithOP(swappedOp);
            }

            var op = rhs.OP;

            List<HLOP> ret;
            // First a special case: squaring is a separate construct.
            if (rhs.OP == BinaryOp.Pow && rhs.RHS is LiteralExpression lit && lit.FloatValue!.Value == 2) {
                ret = VectoredOp(HLOP.Square, lhs, ((IdentifierName)rhs.LHS).Name, type);
            } else

            // An op is either coommutative or noncommutative.
            if (commutativeOps.TryGetValue(op, out var ops))
                ret = HandleCommutativeBinop(lhs, rhs.LHS, rhs.RHS, type, ops);
            else if (nonCommutativeOps.TryGetValue(op, out var ops2))
                ret = HandleNonCommutativeBinop(lhs, rhs.LHS, rhs.RHS, type, ops2);
            else throw new InvalidOperationException();

            // If we inverted, apply the not.
            if (inverted) {
                ret.AddRange(
                    EmitUnary(model, lhs, new PrefixUnaryExpression(new IdentifierName(lhs), PrefixUnaryOp.Not), type)
                );
            }
            return ret;
        }

        static List<HLOP> EmitIndex(string lhs, IndexExpression rhs) {
            // Note:
            /// <see cref="Visitors.PrepareVMIndexersRewriter"/>
            // ensures that matrix indices take into account the structure of
            // matrices in VM memory, so we do not need to do any handling any
            // more.
            // The VM has to ensure that the access does not go out of bounds

            // TODO: Expressions of the form set[ind] = ... is not handled here.
            var expr = rhs.Expression;
            var ind = rhs.Index;
            if (ind.Entries.Count != 1)
                throw new InvalidOperationException("Expected one-dimensional index.");
            var index = ind.Entries[0];

            if (expr is not IdentifierName indexed)
                throw new InvalidOperationException("Expected index to apply to an identifier.");

            if (index is IdentifierName nameIndex)
                return HLOP.IndexedGet(lhs, indexed.Name, nameIndex.Name);
            if (index is LiteralExpression literalIndex) {
                if (literalIndex.FloatValue.HasValue)
                    return HLOP.IndexedGet(lhs, indexed.Name, literalIndex.FloatValue.Value);
                throw new InvalidOperationException("Expected float index.");
            }
            throw new InvalidOperationException("Expected name or literal index, but it was something else. Did you forget a rewrite?");

        }

        static List<HLOP> EmitMatrix(SemanticModel model, string lhs, MatrixExpression rhs) {
            var type = model.GetExpressionType(rhs);
            if (!type.TryGetMatrixSize(out var size))
                throw new InvalidOperationException("Expected a matrix.");
            (int rows, int cols) = size;
            List<HLOP> ret = new();
            // Make vectors lay down
            if (cols == 1) {
                (rows, cols) = (cols, rows);
            }
            // Handle 2x2 specially
            if (rows == 2 && cols == 2) {
                for (int i = 0; i < 4; i++) {
                    if (rhs.Entries[i] is IdentifierName id)
                        ret.Add(HLOP.Set(lhs + "+" + i, id.Name));
                    else if (rhs.Entries[i] is LiteralExpression lit)
                        ret.Add(HLOP.Set(lhs + "+" + i, lit.FloatValue!.Value));
                    else
                        throw new NotImplementedException($"Unknown literal type `{rhs.Entries[i].ToCompactString()}`");
                }
                return ret;
            }
            // Regular matrices
            for (int row = 0; row < rows; row++) {
                for (int col = 0; col < cols; col++) {
                    var entry = rhs.Entries[col + cols * row];
                    var index = lhs + "+" + (col + 4 * row);
                    if (entry is IdentifierName id)
                        ret.Add(HLOP.Set(index, id.Name));
                    else if (entry is LiteralExpression lit)
                        ret.Add(HLOP.Set(index, lit.FloatValue!.Value));
                    else
                        throw new NotImplementedException($"Unknown literal type `{entry.ToCompactString()}`");
                }
            }
            return ret;
        }

        static List<HLOP> EmitPolar(string lhs, PolarExpression rhs)
            => (rhs.Angle is LiteralExpression, rhs.Radius is LiteralExpression) switch {
                (true, true) => HLOP.Polar(lhs,
                    (rhs.Angle as LiteralExpression)!.FloatValue!.Value,
                    (rhs.Radius as LiteralExpression)!.FloatValue!.Value),
                (true, false) => HLOP.Polar(lhs,
                    (rhs.Angle as LiteralExpression)!.FloatValue!.Value,
                    (rhs.Radius as IdentifierName)!.Name),
                (false, true) => HLOP.Polar(lhs,
                    (rhs.Angle as IdentifierName)!.Name,
                    (rhs.Radius as LiteralExpression)!.FloatValue!.Value),
                (false, false) => HLOP.Polar(lhs,
                    (rhs.Angle as IdentifierName)!.Name,
                    (rhs.Radius as IdentifierName)!.Name)
            };
    }
}
