using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Semantics;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// This interface represents tree nodes that directly correspond to an
    /// emittable (high-level) opcode.
    /// <br/>
    /// The goal of compilation is to create a full tree of emittable nodes.
    /// </summary>
    internal interface IEmittable {
        /// <summary>
        /// Convert this syntax node into zero, one, or multiple opcodes.
        /// </summary>
        public List<HLOP> Emit(SemanticModel model);
    }
}
