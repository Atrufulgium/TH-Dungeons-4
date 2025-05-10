using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Semantics;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// Walks through the entire tree, emitting all nodes into <see cref="OPCodes"/>.
    /// <br/>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> The root contains only emittable root-level statements. </item>
    /// </list>
    /// </summary>
    internal class EmitWalker : AbstractTreeWalker {
        public IReadOnlyCollection<HLOP> OPCodes { get; private set; }
        readonly List<HLOP> opCodes = new();

        // Bad software design: Be sure to keep this in sync with
        /// <see cref="IntrinsicData"/>
        /// <see cref="BytecodeOutput"/>
        public IReadOnlyDictionary<string, float> ExplicitVariableIDs { get; private set; }
        readonly Dictionary<string, float> explicitVariableIDs = new() {
            { "bullettype", 0 },
            { "spawnrotation", 1 },
            { "spawnspeed", 2 },
            { "spawnrelative", 3 },
            { "spawnposition", 4 },
            // TODO: Rewriter that enforces these names
            { "main(float).value", 9 },
            { "on_message(float).value", 10 },
            { "on_charge(float).value", 11 },
            { "on_screen_leave(float).value", 12 }
        };

        float nextVariableID = 0;

        public EmitWalker() { 
            OPCodes = new ReadOnlyCollection<HLOP>(opCodes);
            ExplicitVariableIDs = new ReadOnlyDictionary<string, float>(explicitVariableIDs);
        }

        protected override void VisitRoot(Root node) {
            if (node.Declarations.Count > 0)
                throw new VisitorAssumptionFailedException("The tree should be in root-level statement form.");

            foreach (var s in node.RootLevelStatements) {
                if (s is IEmittable e)
                    opCodes.AddRange(e.Emit(Model));
                else
                    throw new VisitorAssumptionFailedException($"Every root-level statement should be emittable. Problem: {s.ToCompactString()}");
            }

            // This is the last place we have a semantic model available, so
            // also have EmitWalker prepare the locations of the variables in
            // VM memory by walking over all IdentifierNames in-tree.
            nextVariableID = explicitVariableIDs.Values.Max() + 1;
            base.VisitRoot(node);

            // Add a final placeholder 4-aligned 16 after the last bit of
            // memory so that getters can't go out of bounds.
            explicitVariableIDs.Add("n/a", (nextVariableID + 16)/4 * 4);
        }

        protected override void VisitIdentifierName(IdentifierName node) {
            base.VisitIdentifierName(node);
            var symbol = Model.GetSymbolInfo(node) ?? throw new NullReferenceException("What happened? IdentifierNames should have symbols!");

            if (symbol is not VariableSymbol var)
                return;

            var name = var.FullyQualifiedName;

            if (explicitVariableIDs.ContainsKey(name))
                return;

            // Floats take up 1. Vectors take up 4.
            if (var.Type.TryGetVectorSize(out int vectorSize)) {
                explicitVariableIDs.Add(name, nextVariableID);
                nextVariableID += vectorSize == 1 ? 1 : 4;
                return;
            }

            // Matrices take up 4*rows.
            if (var.Type.TryGetMatrixSize(out var matrixSize)) {
                explicitVariableIDs.Add(name, nextVariableID);
                nextVariableID += 4 * matrixSize.rows;
                return;
            }

            // Other than that there's strings, but they don't go in float mem.
        }
    }
}
