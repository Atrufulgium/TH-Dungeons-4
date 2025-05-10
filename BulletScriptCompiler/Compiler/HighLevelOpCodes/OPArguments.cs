using Atrufulgium.BulletScript.Compiler.Helpers;

namespace Atrufulgium.BulletScript.Compiler.HighLevelOpCodes {
    internal interface IOPArgument {
        /// <summary>
        /// Convert a HLOP argument to a low level opcode argument.
        /// </summary>
        /// <param name="explicitGotoTargets">
        /// The target instruction of each goto label.
        /// </param>
        /// <param name="explicitVariableIDs">
        /// The location in memory assigned to a variable.
        /// </param>
        /// <param name="explicitStringIDs">
        /// The location in string memory for each literal string.
        /// </param>
        public float ToFloat(
            Dictionary<string, float> explicitGotoTargets,
            Dictionary<string, float> explicitVariableIDs,
            Dictionary<string, float> explicitStringIDs
        );
    }

    /// <summary>
    /// Represents a "n/a" argument. For instance, the no-op instruction has
    /// no argument, so you use this one thrice.
    /// </summary>
    internal class None : IOPArgument {
        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => 0;

        public static None Singleton = new();

        public override string ToString() => new('-', 20);
    }

    /// <summary>
    /// Represents a pointer in float memory. This may be used both for floats
    /// and matrices. In the latter case, it accesses the first entry of the
    /// matrix. See the docs for details.
    /// </summary>
    internal class FloatRef : IOPArgument {

        public readonly string id;
        public FloatRef(string id) => this.id = id;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs) {
            if (explicitVariableIDs.TryGetValue(id, out var f))
                return f;

            // We use a VARNAME+n syntax to describe offsets.
            // Calculate those offsets here.
            var parts = id.Split('+');
            if (parts.Length != 2)
                throw new ArgumentException("Malformed variable ID. Either expect \"VARNAME\" or \"VARNAME+n\" describing an offset.");
            
            var basePos = explicitVariableIDs[parts[0]];
            return basePos + int.Parse(parts[1]);
        }

        public override string ToString() => $"[f] {id.Truncate(16, ^10),16}";
    }

    /// <summary>
    /// Represents a literal float directly embedded in bytecode.
    /// </summary>
    internal class FloatLit : IOPArgument {

        public readonly float value;
        public FloatLit(float value) => this.value = value;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => value;

        public override string ToString() => $"[f] {value,16}";
    }

    /// <summary>
    /// Represents a pointer in string memory if not delimited by "".
    /// <br/>
    /// Represents a string literal (to be replaced with a pointer) if
    /// delimited by "".
    /// </summary>
    internal class StringRef : IOPArgument {

        public readonly string id;
        public StringRef(string id) => this.id = id;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => explicitStringIDs[id];

        public override string ToString() => $"[s] {id.Truncate(16, ^10),16}";
    }

    /// <summary>
    /// Represents an instruction index.
    /// </summary>
    internal class InstructionRef : IOPArgument {

        public string Label { get; init; }
        public InstructionRef(string label) => Label = label;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => explicitGotoTargets[Label];

        public override string ToString() => $"[i] {Label.Truncate(16, ^10),16}";
    }

    // I'm not calling it a "factory". That is just too proper.
    internal static class ThisJustSavesFourKeystrokesEachTime {
        public static None None => None.Singleton;
        public static FloatRef FloatRef(string id) => new(id);
        public static FloatLit FloatLit(float value) => new(value);
        public static StringRef StringRef(string id) => new(id);
        public static InstructionRef InstructionRef(string label) => new(label);
    }
}
