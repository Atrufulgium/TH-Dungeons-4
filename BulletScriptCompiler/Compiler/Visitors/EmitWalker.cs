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
        public IReadOnlyDictionary<string, int> ExplicitVariableIDs { get; private set; }
        readonly Dictionary<string, int> explicitVariableIDs = new() {
            { "bullettype", 0 },
            { "spawnrotation", 1 },
            { "spawnspeed", 2 },
            { "spawnrelative", 3 },
            { "spawnposition", 4 },
            { "##main(float)##", 9 },
            { "##on_message(float)##", 10 },
            { "##on_charge(float)##", 11 },
            { "##on_screen_leave(float)##", 12 }
        };

        /// <summary>
        /// The location of the first variable that's in the block that needs
        /// to be swapped out on context switches.
        /// </summary>
        public int MainMethodMemoryStart => mainMethodMemoryStart;

        int nextVariableID = 0;

        /// <summary>
        /// The location of the first variable that's either in
        /// <see cref="localFloatVariables"/> or <see cref="localVectorVariables"/>.
        /// </summary>
        int mainMethodMemoryStart = 0;

        public EmitWalker() { 
            OPCodes = new ReadOnlyCollection<HLOP>(opCodes);
            ExplicitVariableIDs = new ReadOnlyDictionary<string, int>(explicitVariableIDs);
            seenVariables = new(explicitVariableIDs.Keys);
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

            // Now use the variables we've meticulously extracted in
            /// <see cref="VisitIdentifierName(IdentifierName)"/>.
            int lastVectorStart = explicitVariableIDs["spawnposition"];
            HandleGlobalOrLocalChunk(globalFloatVariables, globalVectorVariables, ref lastVectorStart);
            mainMethodMemoryStart = nextVariableID;
            HandleGlobalOrLocalChunk(localFloatVariables, localVectorVariables, ref lastVectorStart);

            // Add a final placeholder 4-aligned after the last indexable
            // memory so that getters can't go out of bounds.
            int safeAfterAll = (nextVariableID + 16) / 4 * 4;
            int safeAfterVectors = lastVectorStart + 16;
            explicitVariableIDs.Add("n/a", Math.Min(safeAfterAll, safeAfterVectors));
        }

        protected override void VisitIdentifierName(IdentifierName node) {
            base.VisitIdentifierName(node);
            var symbol = Model.GetSymbolInfo(node) ?? throw new NullReferenceException("What happened? IdentifierNames should have symbols!");

            if (symbol is not VariableSymbol var)
                return;

            var name = var.FullyQualifiedName;

            if (seenVariables.Contains(name))
                return;

            // Very simple heuristic, but correct where it matters nonetheless:
            // - In top-level-statement type files, there's no context
            //   switching so this doesn't matter anyways.
            // - Otherwise, every FQN contains delimiters `#` when something is
            //   not at the top level.
            // - This might accidentally take intermediate result vars with it
            //   in top-level mode, but that doesn't really matter.
            // This could be a little coarser even (as control flow within
            // methods really doesn't matter with how local it is), but eh.
            bool local = name.Contains("#");

            // Floats take up 1. Vectors take up 4.
            if (var.Type.TryGetVectorSize(out int vectorSize)) {
                if (vectorSize == 1) {
                    (local ? localFloatVariables : globalFloatVariables)
                        .Enqueue(name);
                } else {
                    (local ? localVectorVariables : globalVectorVariables)
                        .Add(name, 1);
                }
                seenVariables.Add(name);
                return;
            }

            // Matrices take up 4*rows.
            if (var.Type.TryGetMatrixSize(out var matrixSize)) {
                (local ? localVectorVariables : globalVectorVariables)
                    .Add(name, matrixSize.rows);
                seenVariables.Add(name);
                return;
            }

            // Other than that there's strings, but they don't go in float mem.
        }

        /// <summary>
        /// Add both <paramref name="floatVariables"/> and <paramref name="vectorVariables"/>
        /// to <see cref="explicitVariableIDs"/> in an order that 4-aligns the
        /// vector variables.
        /// <br/>
        /// The starting index of the last vector variable is stored in
        /// <paramref name="lastVectorStart"/>.
        /// </summary>
        private void HandleGlobalOrLocalChunk(
            Queue<string> floatVariables,
            Dictionary<string, int> vectorVariables,
            ref int lastVectorStart
        ) {
            // Add enough floats from the floats list to 4-align.
            // If there's not enough, jump ahead.
            var orderedFloatVariables = new Stack<string>(floatVariables);
            while (nextVariableID % 4 != 0) {
                if (orderedFloatVariables.TryPop(out var name)) {
                    explicitVariableIDs.Add(name, nextVariableID);
                    // There's left, so put the next one after
                    nextVariableID++;
                } else {
                    // If nothing left, round up to the next four-align
                    nextVariableID = (nextVariableID / 4 + 1) * 4;
                }
            }

            // Add the 4-aligned variables.
            foreach (var kv in vectorVariables) {
                explicitVariableIDs.Add(kv.Key, nextVariableID);
                lastVectorStart = nextVariableID;
                nextVariableID += 4 * kv.Value;
            }

            // Add the remaining float variables.
            while (orderedFloatVariables.TryPop(out var name)) {
                explicitVariableIDs.Add(name, nextVariableID);
                nextVariableID++;
            }
        }

        /// <summary>
        /// All variables in one of the below four collections.
        /// </summary>
        readonly HashSet<string> seenVariables;

        /// <summary>
        /// All 1-float variables and strings in the vm outside methods.
        /// </summary>
        readonly Queue<string> globalFloatVariables = new();
        /// <summary>
        /// All 4<i>n</i>-float variables in the vm outside methods.
        /// The values stores <i>n</i>.
        /// </summary>
        readonly Dictionary<string, int> globalVectorVariables = new();
        /// <summary>
        /// All 1-float variables and strings in the vm inside methods.
        /// These get swapped away when context switching.
        /// </summary>
        readonly Queue<string> localFloatVariables = new();
        /// <summary>
        /// All 4<i>n</i>-float variables in the vm inside methods.
        /// These get swapped away when context switching.
        /// </summary>
        readonly Dictionary<string, int> localVectorVariables = new();
    }
}
