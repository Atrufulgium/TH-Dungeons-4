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
    }

    /// <summary>
    /// Represents a pointer in float memory. This may be used both for floats
    /// and matrices. In the latter case, it accesses the first entry of the
    /// matrix. See the docs for details.
    /// </summary>
    internal class FloatRef : IOPArgument {

        readonly string id;
        public FloatRef(string id) => this.id = id;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => explicitVariableIDs[id];
    }

    /// <summary>
    /// Represents a literal float directly embedded in bytecode.
    /// </summary>
    internal class FloatLit : IOPArgument {

        readonly float value;
        public FloatLit(float value) => this.value = value;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => value;
    }

    /// <summary>
    /// Represents a pointer in string memory.
    /// </summary>
    internal class StringRef : IOPArgument {

        readonly string id;
        public StringRef(string id) => this.id = id;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => explicitStringIDs[id];
    }

    /// <summary>
    /// Represents a reference to a float in float memory.
    /// </summary>
    internal class InstructionRef : IOPArgument {

        public string Label { get; init; }
        public InstructionRef(string label) => Label = label;

        public float ToFloat(Dictionary<string, float> explicitGotoTargets, Dictionary<string, float> explicitVariableIDs, Dictionary<string, float> explicitStringIDs)
            => explicitGotoTargets[Label];
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
