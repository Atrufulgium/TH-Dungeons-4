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
    internal class HLOP {

        public float OpCode => opcode;
        readonly float opcode;
        readonly IOPArgument arg1;
        readonly IOPArgument arg2;
        readonly IOPArgument arg3;

        private HLOP(float opcode) : this(opcode, None.Singleton) { }
        private HLOP(float opcode, IOPArgument arg1)
            : this(opcode, arg1, None.Singleton) { }
        private HLOP(float opcode, IOPArgument arg1, IOPArgument arg2)
            : this(opcode, arg1, arg2, None.Singleton) { }
        private HLOP(float opcode, IOPArgument arg1, IOPArgument arg2, IOPArgument arg3) {
            this.opcode = opcode;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
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

        // This conversion is for convenience for emittable methods that return
        // a list of opcodes, to allow simply returning a single.
        public static implicit operator List<HLOP>(HLOP op)
            => new() { op };

        // the [[BIG LIST]]
        // (see the specification for more info on what these opcodes actually do.)

        // not translated to low level opcodes
        public static HLOP NOOP                                     => new(-2);
        public static HLOP GotoLabel(string label)                  => new(-1, InstructionRef(label)); // bit hacky but other instructionrefs should match
        // general ops
        public static HLOP EqualFloat(string id, float value)       => new( 0, FloatRef(id), FloatLit(value));
        public static HLOP EqualFloat(string id1, string id2)       => new( 1, FloatRef(id1), FloatRef(id2));
        public static HLOP EqualPointer(string id1, string id2)     => new( 2, FloatRef(id1), FloatRef(id2));
        public static HLOP LessThan(float value, string id)         => new( 3, FloatLit(value), FloatRef(id));
        public static HLOP LessThan(string id, float value)         => new( 4, FloatRef(id), FloatLit(value));
        public static HLOP LessThan(string id1, string id2)         => new( 5, FloatRef(id1), FloatRef(id2));
        public static HLOP LessThanEqual(float value, string id)    => new( 6, FloatLit(value), FloatRef(id));
        public static HLOP LessThanEqual(string id, float value)    => new( 7, FloatRef(id), FloatLit(value));
        public static HLOP LessThanEqual(string id1, string id2)    => new( 8, FloatRef(id1), FloatRef(id2));
        public static HLOP Set(string id, float value)              => new( 9, FloatRef(id), FloatLit(value));
        public static HLOP Set(string id1, string id2)              => new(10, FloatRef(id1), FloatRef(id2));
        public static HLOP IndexedGet(string id, float value)       => new(11, FloatRef(id), FloatLit(value));
        public static HLOP IndexedGet(string id1, string id2)       => new(12, FloatRef(id1), FloatRef(id2));
        public static HLOP IndexedSet(string id, float v, float v2) => new(13, FloatRef(id), FloatLit(v), FloatLit(v2));
        public static HLOP IndexedSet(string i, float v, string i2) => new(14, FloatRef(i), FloatLit(v), FloatRef(i2));
        public static HLOP IndexedSet(string i, string i2, float v) => new(15, FloatRef(i), FloatRef(i2), FloatLit(v));
        public static HLOP IndexedSet(string i, string j, string k) => new(16, FloatRef(i), FloatRef(j), FloatRef(k));
        public static HLOP Jump(string label)                       => new(17, InstructionRef(label));
        public static HLOP JumpConditional(string label, string id) => new(18, InstructionRef(label), FloatRef(id));
        public static HLOP Pause(float value)                       => new(19, FloatLit(value));
        public static HLOP Pause(string id)                         => new(20, FloatRef(id));
        // misc intrinsics
        public static HLOP Message(string id)                       => new(21, FloatRef(id));
        public static HLOP Spawn                                    => new(22);
        public static HLOP LoadBackground(string id)                => new(23, StringRef(id));
        public static HLOP AddScript(string id)                     => new(24, StringRef(id));
        public static HLOP AddScript(string id, float value)        => new(25, StringRef(id), FloatLit(value));
        public static HLOP AddScript(string id1, string id2)        => new(26, StringRef(id1), FloatRef(id2));
        public static HLOP StartScript(string id)                   => new(27, StringRef(id));
        public static HLOP StartScript(string id, float value)      => new(28, StringRef(id), FloatLit(value));
        public static HLOP StartScript(string id1, string id2)      => new(29, StringRef(id1), FloatRef(id2));
        public static HLOP StartScriptMany(string id)               => new(30, StringRef(id));
        public static HLOP StartScriptMany(string id, float value)  => new(31, StringRef(id), FloatLit(value));
        public static HLOP StartScriptMany(string id1, string id2)  => new(32, StringRef(id1), FloatRef(id2));
        public static HLOP Depivot                                  => new(33);
        public static HLOP AddRotation(float value)                 => new(34, FloatLit(value));
        public static HLOP AddRotation(string id)                   => new(35, FloatRef(id));
        public static HLOP SetRotation(float value)                 => new(36, FloatLit(value));
        public static HLOP SetRotation(string id)                   => new(37, FloatRef(id));
        public static HLOP AddSpeed(float value)                    => new(38, FloatLit(value));
        public static HLOP AddSpeed(string id)                      => new(39, FloatRef(id));
        public static HLOP SetSpeed(float value)                    => new(40, FloatLit(value));
        public static HLOP SetSpeed(string id)                      => new(41, FloatRef(id));
        public static HLOP FacePlayer                               => new(42);
        public static HLOP AngleToPlayer(string id)                 => new(43, FloatRef(id));
        public static HLOP Gimmick(string id)                       => new(44, StringRef(id));
        public static HLOP Gimmick(string id, float value)          => new(45, StringRef(id), FloatLit(value));
        public static HLOP Gimmick(string id, string id2)           => new(46, StringRef(id), FloatRef(id2));
        public static HLOP Gimmick(string id, float v1, float v2)   => new(47, StringRef(id), FloatLit(v1), FloatLit(v2));
        public static HLOP Gimmick(string id, float v1, string i2)  => new(48, StringRef(id), FloatLit(v1), FloatRef(i2));
        public static HLOP Gimmick(string id, string i2, float v2)  => new(49, StringRef(id), FloatRef(i2), FloatLit(v2));
        public static HLOP Gimmick(string id, string i2, string i3) => new(50, StringRef(id), FloatRef(i2), FloatRef(i3));
        // float math
        public static HLOP Negate(string res, string a)             => new(51, FloatRef(res), FloatRef(a));
        public static HLOP Add(string res, float  a, string b)      => new(52, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Add(string res, string a, string b)      => new(53, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Sub(string res, float  a, string b)      => new(54, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Sub(string res, string a, float  b)      => new(55, FloatRef(res), FloatRef(a), FloatLit(b));
        public static HLOP Sub(string res, string a, string b)      => new(56, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Mul(string res, float  a, string b)      => new(57, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Mul(string res, string a, string b)      => new(58, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Div(string res, float  a, string b)      => new(59, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Div(string res, string a, float  b)      => new(60, FloatRef(res), FloatRef(a), FloatLit(b));
        public static HLOP Div(string res, string a, string b)      => new(61, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Mod(string res, float  a, string b)      => new(62, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Mod(string res, string a, float  b)      => new(63, FloatRef(res), FloatRef(a), FloatLit(b));
        public static HLOP Mod(string res, string a, string b)      => new(64, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Pow(string res, float  a, string b)      => new(65, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Pow(string res, string a, float  b)      => new(66, FloatRef(res), FloatRef(a), FloatLit(b));
        public static HLOP Pow(string res, string a, string b)      => new(67, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Square(string res, string a)             => new(68, FloatRef(res), FloatRef(a));
        public static HLOP Sin(string res, string a)                => new(69, FloatRef(res), FloatRef(a));
        public static HLOP Cos(string res, string a)                => new(70, FloatRef(res), FloatRef(a));
        public static HLOP Tan(string res, string a)                => new(71, FloatRef(res), FloatRef(a));
        public static HLOP Asin(string res, string a)               => new(72, FloatRef(res), FloatRef(a));
        public static HLOP Acos(string res, string a)               => new(73, FloatRef(res), FloatRef(a));
        public static HLOP Atan(string res, string a)               => new(74, FloatRef(res), FloatRef(a));
        public static HLOP Atan2(string res, float  a, string b)    => new(75, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Atan2(string res, string a, float  b)    => new(76, FloatRef(res), FloatRef(a), FloatLit(b));
        public static HLOP Atan2(string res, string a, string b)    => new(77, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Ceil(string res, string a)               => new(78, FloatRef(res), FloatRef(a));
        public static HLOP Floor(string res, string a)              => new(79, FloatRef(res), FloatRef(a));
        public static HLOP Round(string res, string a)              => new(80, FloatRef(res), FloatRef(a));
        public static HLOP Abs(string res, string a)                => new(81, FloatRef(res), FloatRef(a));
        public static HLOP Rng(string res, float  a, float  b)      => new(82, FloatRef(res), FloatLit(a), FloatLit(b));
        public static HLOP Rng(string res, float  a, string b)      => new(83, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Rng(string res, string a, float  b)      => new(84, FloatRef(res), FloatRef(a), FloatLit(b));
        public static HLOP Rng(string res, string a, string b)      => new(85, FloatRef(res), FloatRef(a), FloatRef(b));
        // vector math (excl matrix mul)
        public static HLOP Equal4(string res, string a, string b)   => new(86, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP LT4(string res, string a, string b)      => new(87, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP LTE4(string res, string a, string b)     => new(88, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Negate4(string res, string a)            => new(89, FloatRef(res), FloatRef(a));
        public static HLOP Add4(string res, string a, string b)     => new(91, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Sub4(string res, string a, string b)     => new(92, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Mul4(string res, string a, string b)     => new(93, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Div4(string res, string a, string b)     => new(94, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Mod4(string res, string a, string b)     => new(95, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Pow4(string res, string a, string b)     => new(96, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Square4(string res, string a)            => new(97, FloatRef(res), FloatRef(a));
        public static HLOP Sin4(string res, string a)               => new(98, FloatRef(res), FloatRef(a));
        public static HLOP Cos4(string res, string a)               => new(99, FloatRef(res), FloatRef(a));
        public static HLOP Tan4(string res, string a)               => new(100, FloatRef(res), FloatRef(a));
        public static HLOP Asin4(string res, string a)              => new(101, FloatRef(res), FloatRef(a));
        public static HLOP Acos4(string res, string a)              => new(102, FloatRef(res), FloatRef(a));
        public static HLOP Atan4(string res, string a)              => new(103, FloatRef(res), FloatRef(a));
        public static HLOP Atan24(string res, string a, string b)   => new(104, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Ceil4(string res, string a)              => new(105, FloatRef(res), FloatRef(a));
        public static HLOP Floor4(string res, string a)             => new(106, FloatRef(res), FloatRef(a));
        public static HLOP Round4(string res, string a)             => new(107, FloatRef(res), FloatRef(a));
        public static HLOP Abs4(string res, string a)               => new(108, FloatRef(res), FloatRef(a));
        public static HLOP Rng4(string res, string a, string b)     => new(109, FloatRef(res), FloatRef(a), FloatRef(b));
        public static HLOP Length4(string res, string a)            => new(110, FloatRef(res), FloatRef(a));
        public static HLOP Distance4(string r, string a, string b)  => new(111, FloatRef(r  ), FloatRef(a), FloatRef(b));
        public static HLOP Polar(string res, float  a, float  b)    => new(112, FloatRef(res), FloatLit(a), FloatLit(b));
        public static HLOP Polar(string res, float  a, string b)    => new(112, FloatRef(res), FloatLit(a), FloatRef(b));
        public static HLOP Polar(string res, string a, float  b)    => new(112, FloatRef(res), FloatRef(a), FloatLit(b));
        public static HLOP Polar(string res, string a, string b)    => new(112, FloatRef(res), FloatRef(a), FloatRef(b));
        // matrix mul
        public static HLOP MatrixMul(int u, int v, int w, string res, string a, string b)
            => new(192 + u + 4* v + 16* w, FloatRef(res), FloatRef(a), FloatRef(b));
    }
}
