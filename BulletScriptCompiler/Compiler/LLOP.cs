using System.Runtime.InteropServices;

namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// A low-level opcode. See the documentation.
    /// <br/>
    /// This is castable to a float4, Vector4, (float,float,float,float), etc.
    /// The first entry will be the OPCode, the rest the arguments.
    /// </summary>
    /// <remarks>
    /// The main reason for this struct existing is because Unity's so old it
    /// doesn't even have value types by default, and it's breaking stuff.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct LLOP {
        /// <summary> Opcode. See the extended docs. </summary>
        public readonly float opcode;
        /// <summary> First arg. May not necessarily be used. See the extended docs. </summary>
        public readonly float arg1;
        /// <summary> First arg. May not necessarily be used. See the extended docs. </summary>
        public readonly float arg2;
        /// <summary> First arg. May not necessarily be used. See the extended docs. </summary>
        public readonly float arg3;

        /// <inheritdoc cref="LLOP"/>
        public LLOP(float opcode, float arg1, float arg2, float arg3) {
            this.opcode = opcode;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }
    }
}