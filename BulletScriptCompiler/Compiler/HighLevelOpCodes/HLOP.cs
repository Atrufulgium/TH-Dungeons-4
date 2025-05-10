using System.Collections;
using System.Collections.Generic;
using static Atrufulgium.BulletScript.Compiler.HighLevelOpCodes.ThisJustSavesFourKeystrokesEachTime;

namespace Atrufulgium.BulletScript.Compiler.HighLevelOpCodes {
    /// <summary>
    /// Represents a "high level" opcode. High level opcodes do not have their
    /// arguments filled in numerically, but still remain some semantic info.
    /// <br/>
    /// In particular:
    /// <list type="bullet">
    /// <item> (Conditional) jumps index by label and not by instruction nr. </item>
    /// <item> Variables still use their names instead of being an index. </item>
    /// <item> String values are still in-line. </item>
    /// </list>
    /// </summary>
    internal readonly struct HLOP : IEnumerable<IOPArgument> {

        public readonly float opcode;
        public readonly IOPArgument arg1;
        public readonly IOPArgument arg2;
        public readonly IOPArgument arg3;

        static readonly Dictionary<float, string> opNames = new();

        private HLOP(float opcode, string name) : this(opcode, None.Singleton, name) { }
        private HLOP(float opcode, IOPArgument arg1, string name)
            : this(opcode, arg1, None.Singleton, name) { }
        private HLOP(float opcode, IOPArgument arg1, IOPArgument arg2, string name)
            : this(opcode, arg1, arg2, None.Singleton, name) { }
        private HLOP(float opcode, IOPArgument arg1, IOPArgument arg2, IOPArgument arg3, string name) {
            this.opcode = opcode;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            opNames[opcode] = name;
        }

        /// <summary>
        /// Convert a high level opcode to a low level opcode.
        /// <br/>
        /// Negative opcodes do not return anything.
        /// </summary>
        /// <inheritdoc cref="IOPArgument.ToFloat(Dictionary{string, float}, Dictionary{string, float}, Dictionary{string, float})"/>
        public (float, float, float, float)? ToLowLevel(
            Dictionary<string, float> explicitGotoTargets,
            Dictionary<string, float> explicitVariableIDs,
            Dictionary<string, float> explicitStringIDs
        ) => opcode >= 0 ? (
            opcode,
            arg1.ToFloat(explicitGotoTargets, explicitVariableIDs, explicitStringIDs),
            arg2.ToFloat(explicitGotoTargets, explicitVariableIDs, explicitStringIDs),
            arg3.ToFloat(explicitGotoTargets, explicitVariableIDs, explicitStringIDs)
        ) : null;

        public override string ToString() => $"[op] {opNames[opcode],15} | {arg1} | {arg2} | {arg3}";

        // This conversion is for convenience for emittable methods that return
        // a list of opcodes, to allow simply returning a single.
        public static implicit operator List<HLOP>(HLOP op)
            => new() { op };

        // the [[BIG LIST]]
        // (see the specification for more info on what these opcodes actually do.)

        // not translated to low level opcodes
        public static HLOP NOOP()                                   => new(-2, "NOOP");
        public static HLOP GotoLabel(string label)                  => new(-1, InstructionRef(label), "GotoLabel"); // bit hacky but other instructionrefs should match
        // general ops
        public static HLOP Equal(string r, float v, string i)       => new( 1, FloatRef(r), FloatLit(v), FloatRef(i), "Equal");
        public static HLOP Equal(string r, string i, string j)      => new( 2, FloatRef(r), FloatRef(i), FloatRef(j), "Equal");
        public static HLOP EqualString(string r, string i, string j)=> new( 3, FloatRef(r), StringRef(i), StringRef(j), "EqualString");
        public static HLOP LT(string r, float v, string i)          => new( 4, FloatRef(r), FloatLit(v), FloatRef(i), "LessThan");
        public static HLOP LT(string r, string i, float v)          => new( 5, FloatRef(r), FloatRef(i), FloatLit(v), "LessThan");
        public static HLOP LT(string r, string i, string j)         => new( 6, FloatRef(r), FloatRef(i), FloatRef(j), "LessThan");
        public static HLOP LTE(string r, float v, string i)         => new( 7, FloatRef(r), FloatLit(v), FloatRef(i), "LessThanEqual");
        public static HLOP LTE(string r, string i, float v)         => new( 8, FloatRef(r), FloatRef(i), FloatLit(v), "LessThanEqual");
        public static HLOP LTE(string r, string i, string j)        => new( 9, FloatRef(r), FloatRef(i), FloatRef(j), "LessThanEqual");
        public static HLOP Set(string id, float value)              => new(10, FloatRef(id), FloatLit(value), "Set");
        public static HLOP Set(string id1, string id2)              => new(11, FloatRef(id1), FloatRef(id2), "Set");
        public static HLOP SetString(string id1, string id2)        => new(12, StringRef(id1), StringRef(id2), "SetString");
        public static HLOP IndexedGet(string id, float value)       => new(13, FloatRef(id), FloatLit(value), "IndexedGet");
        public static HLOP IndexedGet(string id1, string id2)       => new(14, FloatRef(id1), FloatRef(id2), "IndexedGet");
        public static HLOP IndexedSet(string id, float v, float v2) => new(15, FloatRef(id), FloatLit(v), FloatLit(v2), "IndexedSet");
        public static HLOP IndexedSet(string i, float v, string i2) => new(16, FloatRef(i), FloatLit(v), FloatRef(i2), "IndexedSet");
        public static HLOP IndexedSet(string i, string i2, float v) => new(17, FloatRef(i), FloatRef(i2), FloatLit(v), "IndexedSet");
        public static HLOP IndexedSet(string i, string j, string k) => new(18, FloatRef(i), FloatRef(j), FloatRef(k), "IndexedSet");
        public static HLOP Jump(string label)                       => new(19, InstructionRef(label), "Jump");
        public static HLOP JumpConditional(string label, string id) => new(20, InstructionRef(label), FloatRef(id), "JumpConditional");
        public static HLOP Pause(float value)                       => new(21, FloatLit(value), "Pause");
        public static HLOP Pause(string id)                         => new(22, FloatRef(id), "Pause");
        // misc intrinsics
        public static HLOP Message(string id)                       => new(32, FloatRef(id), "Message");
        public static HLOP Message(float value)                     => new(33, FloatLit(value), "Message");
        public static HLOP Spawn()                                  => new(34, "Spawn");
        public static HLOP Destroy()                                => new(35, "Destroy");
        public static HLOP LoadBackground(string id)                => new(36, StringRef(id), "LoadBackground");
        public static HLOP AddScript()                              => new(37, "AddScript");
        public static HLOP AddScript(string id)                     => new(38, StringRef(id), "AddScript");
        public static HLOP AddScript(string id, float value)        => new(39, StringRef(id), FloatLit(value), "AddScript");
        public static HLOP AddScript(string id1, string id2)        => new(40, StringRef(id1), FloatRef(id2), "AddScript");
        public static HLOP StartScript(string id)                   => new(41, StringRef(id), "StartScript");
        public static HLOP StartScript(string id, float value)      => new(42, StringRef(id), FloatLit(value), "StartScript");
        public static HLOP StartScript(string id1, string id2)      => new(43, StringRef(id1), FloatRef(id2), "StartScript");
        public static HLOP StartScriptMany(string id)               => new(44, StringRef(id), "StartScriptMany");
        public static HLOP StartScriptMany(string id, float value)  => new(45, StringRef(id), FloatLit(value), "StartScriptMany");
        public static HLOP StartScriptMany(string id1, string id2)  => new(46, StringRef(id1), FloatRef(id2), "StartScriptMany");
        public static HLOP Depivot()                                => new(47, "Depivot");
        public static HLOP AddRotation(float value)                 => new(48, FloatLit(value), "AddRotation");
        public static HLOP AddRotation(string id)                   => new(49, FloatRef(id), "AddRotation");
        public static HLOP SetRotation(float value)                 => new(50, FloatLit(value), "SetRotation");
        public static HLOP SetRotation(string id)                   => new(51, FloatRef(id), "SetRotation");
        public static HLOP AddSpeed(float value)                    => new(52, FloatLit(value), "AddSpeed");
        public static HLOP AddSpeed(string id)                      => new(53, FloatRef(id), "AddSpeed");
        public static HLOP SetSpeed(float value)                    => new(54, FloatLit(value), "SetSpeed");
        public static HLOP SetSpeed(string id)                      => new(55, FloatRef(id), "SetSpeed");
        public static HLOP FacePlayer()                             => new(56, "FacePlayer");
        public static HLOP AngleToPlayer(string id)                 => new(57, FloatRef(id), "AngleToPlayer");
        public static HLOP Gimmick(string id)                       => new(58, StringRef(id), "Gimmick");
        public static HLOP Gimmick(string id, float value)          => new(59, StringRef(id), FloatLit(value), "Gimmick");
        public static HLOP Gimmick(string id, string id2)           => new(60, StringRef(id), FloatRef(id2), "Gimmick");
        public static HLOP Gimmick(string id, float v1, float v2)   => new(61, StringRef(id), FloatLit(v1), FloatLit(v2), "Gimmick");
        public static HLOP Gimmick(string id, float v1, string i2)  => new(62, StringRef(id), FloatLit(v1), FloatRef(i2), "Gimmick");
        public static HLOP Gimmick(string id, string i2, float v2)  => new(63, StringRef(id), FloatRef(i2), FloatLit(v2), "Gimmick");
        public static HLOP Gimmick(string id, string i2, string i3) => new(64, StringRef(id), FloatRef(i2), FloatRef(i3), "Gimmick");
        // float math
        public static HLOP Not(string res, string a)                => new(80, FloatRef(res), FloatRef(a), "Not");
        public static HLOP Add(string res, float  a, string b)      => new(81, FloatRef(res), FloatLit(a), FloatRef(b), "Add");
        public static HLOP Add(string res, string a, string b)      => new(82, FloatRef(res), FloatRef(a), FloatRef(b), "Add");
        public static HLOP Sub(string res, float  a, string b)      => new(83, FloatRef(res), FloatLit(a), FloatRef(b), "Sub");
        public static HLOP Sub(string res, string a, float  b)      => new(84, FloatRef(res), FloatRef(a), FloatLit(b), "Sub");
        public static HLOP Sub(string res, string a, string b)      => new(85, FloatRef(res), FloatRef(a), FloatRef(b), "Sub");
        public static HLOP Mul(string res, float  a, string b)      => new(86, FloatRef(res), FloatLit(a), FloatRef(b), "Mul");
        public static HLOP Mul(string res, string a, string b)      => new(87, FloatRef(res), FloatRef(a), FloatRef(b), "Mul");
        public static HLOP Div(string res, float  a, string b)      => new(88, FloatRef(res), FloatLit(a), FloatRef(b), "Div");
        public static HLOP Div(string res, string a, float  b)      => new(89, FloatRef(res), FloatRef(a), FloatLit(b), "Div");
        public static HLOP Div(string res, string a, string b)      => new(90, FloatRef(res), FloatRef(a), FloatRef(b), "Div");
        public static HLOP Mod(string res, float  a, string b)      => new(91, FloatRef(res), FloatLit(a), FloatRef(b), "Mod");
        public static HLOP Mod(string res, string a, float  b)      => new(92, FloatRef(res), FloatRef(a), FloatLit(b), "Mod");
        public static HLOP Mod(string res, string a, string b)      => new(93, FloatRef(res), FloatRef(a), FloatRef(b), "Mod");
        public static HLOP Pow(string res, float  a, string b)      => new(94, FloatRef(res), FloatLit(a), FloatRef(b), "Pow");
        public static HLOP Pow(string res, string a, float  b)      => new(95, FloatRef(res), FloatRef(a), FloatLit(b), "Pow");
        public static HLOP Pow(string res, string a, string b)      => new(96, FloatRef(res), FloatRef(a), FloatRef(b), "Pow");
        public static HLOP Square(string res, string a)             => new(97, FloatRef(res), FloatRef(a), "Square");
        public static HLOP Sin(string res, string a)                => new(98, FloatRef(res), FloatRef(a), "Sin");
        public static HLOP Cos(string res, string a)                => new(99, FloatRef(res), FloatRef(a), "Cos");
        public static HLOP Tan(string res, string a)                => new(100, FloatRef(res), FloatRef(a), "Tan");
        public static HLOP Asin(string res, string a)               => new(101, FloatRef(res), FloatRef(a), "Asin");
        public static HLOP Acos(string res, string a)               => new(102, FloatRef(res), FloatRef(a), "Acos");
        public static HLOP Atan(string res, string a)               => new(103, FloatRef(res), FloatRef(a), "Atan");
        public static HLOP Atan2(string res, float  a, string b)    => new(104, FloatRef(res), FloatLit(a), FloatRef(b), "Atan2");
        public static HLOP Atan2(string res, string a, float  b)    => new(105, FloatRef(res), FloatRef(a), FloatLit(b), "Atan2");
        public static HLOP Atan2(string res, string a, string b)    => new(106, FloatRef(res), FloatRef(a), FloatRef(b), "Atan2");
        public static HLOP Ceil(string res, string a)               => new(107, FloatRef(res), FloatRef(a), "Ceil");
        public static HLOP Floor(string res, string a)              => new(108, FloatRef(res), FloatRef(a), "Floor");
        public static HLOP Round(string res, string a)              => new(109, FloatRef(res), FloatRef(a), "Round");
        public static HLOP Abs(string res, string a)                => new(110, FloatRef(res), FloatRef(a), "Abs");
        public static HLOP Rng(string res, float  a, float  b)      => new(111, FloatRef(res), FloatLit(a), FloatLit(b), "Rng");
        public static HLOP Rng(string res, float  a, string b)      => new(112, FloatRef(res), FloatLit(a), FloatRef(b), "Rng");
        public static HLOP Rng(string res, string a, float  b)      => new(113, FloatRef(res), FloatRef(a), FloatLit(b), "Rng");
        public static HLOP Rng(string res, string a, string b)      => new(114, FloatRef(res), FloatRef(a), FloatRef(b), "Rng");
        public static HLOP Distance(string res, float  a, string b) => new(115, FloatRef(res), FloatLit(a), FloatRef(b), "Distance");
        public static HLOP Distance(string res, string a, float  b) => new(116, FloatRef(res), FloatRef(a), FloatLit(b), "Distance");
        public static HLOP Distance(string res, string a, string b) => new(117, FloatRef(res), FloatRef(a), FloatRef(b), "Distance");
        // vector math (excl matrix mul)
        public static HLOP Set4(string res, string a)               => new(128, FloatRef(res), FloatRef(a), "Set4");
        public static HLOP Equal4(string res, string a, string b)   => new(129, FloatRef(res), FloatRef(a), FloatRef(b), "Equal4");
        public static HLOP LT4(string res, string a, string b)      => new(130, FloatRef(res), FloatRef(a), FloatRef(b), "LessThan4");
        public static HLOP LTE4(string res, string a, string b)     => new(131, FloatRef(res), FloatRef(a), FloatRef(b), "LessThanEqual4");
        public static HLOP Negate4(string res, string a)            => new(132, FloatRef(res), FloatRef(a), "Negate4");
        public static HLOP Not4(string res, string a)               => new(133, FloatRef(res), FloatRef(a), "Not4");
        public static HLOP Add4(string res, string a, string b)     => new(134, FloatRef(res), FloatRef(a), FloatRef(b), "Add4");
        public static HLOP Sub4(string res, string a, string b)     => new(135, FloatRef(res), FloatRef(a), FloatRef(b), "Sub4");
        public static HLOP Mul4(string res, string a, string b)     => new(136, FloatRef(res), FloatRef(a), FloatRef(b), "Mul4");
        public static HLOP Div4(string res, string a, string b)     => new(137, FloatRef(res), FloatRef(a), FloatRef(b), "Div4");
        public static HLOP Mod4(string res, string a, string b)     => new(138, FloatRef(res), FloatRef(a), FloatRef(b), "Mod4");
        public static HLOP Pow4(string res, string a, string b)     => new(139, FloatRef(res), FloatRef(a), FloatRef(b), "Pow4");
        public static HLOP Square4(string res, string a)            => new(140, FloatRef(res), FloatRef(a), "Square4");
        public static HLOP Sin4(string res, string a)               => new(141, FloatRef(res), FloatRef(a), "Sin4");
        public static HLOP Cos4(string res, string a)               => new(142, FloatRef(res), FloatRef(a), "Cos4");
        public static HLOP Tan4(string res, string a)               => new(143, FloatRef(res), FloatRef(a), "Tan4");
        public static HLOP Asin4(string res, string a)              => new(144, FloatRef(res), FloatRef(a), "Asin4");
        public static HLOP Acos4(string res, string a)              => new(145, FloatRef(res), FloatRef(a), "Acos4");
        public static HLOP Atan4(string res, string a)              => new(146, FloatRef(res), FloatRef(a), "Atan4");
        public static HLOP Atan24(string res, string a, string b)   => new(147, FloatRef(res), FloatRef(a), FloatRef(b), "Atan24");
        public static HLOP Ceil4(string res, string a)              => new(148, FloatRef(res), FloatRef(a), "Ceil4");
        public static HLOP Floor4(string res, string a)             => new(149, FloatRef(res), FloatRef(a), "Floor4");
        public static HLOP Round4(string res, string a)             => new(150, FloatRef(res), FloatRef(a), "Round4");
        public static HLOP Abs4(string res, string a)               => new(151, FloatRef(res), FloatRef(a), "Abs4");
        public static HLOP Rng4(string res, string a, string b)     => new(152, FloatRef(res), FloatRef(a), FloatRef(b), "Rng4");
        public static HLOP Length4(string res, string a)            => new(153, FloatRef(res), FloatRef(a), "Length4");
        public static HLOP Distance4(string r, string a, string b)  => new(154, FloatRef(r  ), FloatRef(a), FloatRef(b), "Distance4");
        public static HLOP Polar(string res, float  a, float  b)    => new(155, FloatRef(res), FloatLit(a), FloatLit(b), "Polar");
        public static HLOP Polar(string res, float  a, string b)    => new(156, FloatRef(res), FloatLit(a), FloatRef(b), "Polar");
        public static HLOP Polar(string res, string a, float  b)    => new(157, FloatRef(res), FloatRef(a), FloatLit(b), "Polar");
        public static HLOP Polar(string res, string a, string b)    => new(158, FloatRef(res), FloatRef(a), FloatRef(b), "Polar");
        // matrix mul
        public static HLOP MatrixMul(int u, int v, int w, string res, string a, string b)
            => new(192 + u + 4* v + 16* w, FloatRef(res), FloatRef(a), FloatRef(b), $"MatrixMul{u}{v}{w}");

        public IEnumerator<IOPArgument> GetEnumerator() {
            yield return arg1;
            yield return arg2;
            yield return arg3;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
