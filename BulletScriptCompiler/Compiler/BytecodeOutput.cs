using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Visitors;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// Represents everything the VM needs to run this BulletScript code.
    /// See the extended documentation for more info.
    /// </summary>
    public class BytecodeOutput {
        /// <summary>
        /// All opcodes and their args.
        /// </summary>
        public readonly LLOP[] OpCodes;
        /// <summary>
        /// A sufficiently large pre-initialized memory.
        /// </summary>
        public readonly float[] Memory;
        /// <summary>
        /// All strings this bytecode references.
        /// </summary>
        public readonly string[] Strings;
        /// <summary>
        /// Exactly indices from MainMethodMemoryStart up <see cref="Memory"/>
        /// are locals to the main method (whether called directly or not).
        /// </summary>
        /// <remarks>
        /// This enables the VMs to do something you can barely call threading:
        /// between `main()` calls, store the <see cref="Memory"/> region
        /// between here and the end somewhere else, run the other function,
        /// and restore this memory after.
        /// </remarks>
        public readonly int MainMethodMemoryStart;

        internal BytecodeOutput(EmitWalker emitter) {
            // We have three passes:
            // (1) Prepare the data the HLOPs need for conversion into LLOPs
            // (2) Actually do this conversion, which is easy
            // (3) Prepare the VM memory.
            var opcodes = emitter.OPCodes;

            Dictionary<string, int> explicitGotoTargets = new();
            Dictionary<string, int> explicitVariableIDs = new(emitter.ExplicitVariableIDs);
            Dictionary<string, int> explicitStringIDs = new() {
                { "error", 0 }
            };

            // First find all goto labels and strings
            int instruction = 0;
            var gotoLabelOpcode = HLOP.GotoLabel("").OpCode;
            foreach (var op in opcodes) {
                // Goto
                // (Subtract 1 because the IP increases after every op)
                if (op.OpCode == gotoLabelOpcode)
                    explicitGotoTargets.Add(((InstructionRef)op.arg1).Label, instruction - 1);
                if (op.OpCode >= 0) // (negative HLOPs don't map to LLOPs)
                    instruction++;

                // Strings
                foreach (var arg in op) {
                    if (arg is StringRef str
                        && !explicitStringIDs.ContainsKey(str.id)) {
                        explicitStringIDs.Add(str.id, explicitStringIDs.Count);
                    }
                }
            }

            // Now go through all opcodes to create the bytecode array
            // (shut up bytecode is a state of mind, `float4code` just doesn't
            //  have the same ring to it)
            List<LLOP> llops = new();
            foreach (var op in opcodes) {
                var res = op.ToLowLevel(explicitGotoTargets, explicitVariableIDs, explicitStringIDs);
                if (res != null)
                    llops.Add(res.Value);
            }
            OpCodes = llops.ToArray();

            // Now add all memory: floats
            // Bad software design: Be sure to keep this in sync with
            /// <see cref="Semantics.IntrinsicData"/>
            /// <see cref="EmitWalker.ExplicitVariableIDs"/>
            Memory = new float[explicitVariableIDs.Values.Max()];
            Memory[2] = 1f; // `spawnspeed` inits to 1
            Memory[3] = 1f; // `spawnrelative` inits to 1

            // Now add all memory: strings
            // (When is c# getting a built-in bidict?)
            SortedDictionary<int, string> reverseStringIDs = new();
            foreach (var kv in explicitStringIDs) {
                reverseStringIDs.Add(kv.Value, kv.Key);
            }
            Strings = reverseStringIDs.Select(kv => kv.Value).ToArray();

            // The last value we can just read off directly
            MainMethodMemoryStart = emitter.MainMethodMemoryStart;
        }

        /// <summary>
        /// Returns a human-readable(??) representation of the vm. These are
        /// tables of strings, initial memory, and raw bytecode.
        /// </summary>
        public override string ToString() {
            unsafe static int Int(float f) => *(int*)&f;
            // Very reasonable heuristic as these floats are a bunch of subnormals
            static bool IsInt(float f) => Int(f) is >= 0 and < 65536;

            StringBuilder sb = new();
            sb.AppendLine($"String memory ({Strings.Length}):");
            foreach (var (s, i) in Strings.Select((s, i) => (s, i))) {
                sb.AppendLine($"{i,4} | \"{s}\"");
            }

            // Sorta annoying as I want a clean line between global/local
            sb.AppendLine($"Initial memory ({Memory.Length}):");
            StringBuilder subsb = new();
            for (int i4 = 0; i4 < Memory.Length; i4 += 4) {
                float f1 = Memory[i4];
                float? f2 = i4 + 1 < Memory.Length ? Memory[i4 + 1] : null;
                float? f3 = i4 + 2 < Memory.Length ? Memory[i4 + 2] : null;
                float? f4 = i4 + 3 < Memory.Length ? Memory[i4 + 3] : null;
                string m1 = $"{f1,6:0.0}";
                string m2 = $"{(f2 == null ? "" : f2.Value.ToString("0.0")),8}";
                string m3 = $"{(f3 == null ? "" : f3.Value.ToString("0.0")),8}";
                string m4 = $"{(f4 == null ? "" : f4.Value.ToString("0.0")),8}";
                subsb.AppendLine($"{i4,4} | {m1} {m2}   {m3}   {m4}");
            }
            // Just literally insert into the correct row into the correct
            // table col the correct offset.
            // It's brittle and bad, but I won't be updating these tables.
            int row = MainMethodMemoryStart / 4;
            int col = MainMethodMemoryStart % 4;
            switch (col) {
                case 0: subsb.Insert(46 * row, $"Local:{"",38}\n"); break;
                case 1: subsb.Insert(46 * row + 14, $"{"",30}\nLocal:{"",8}"); break;
                case 2: subsb.Insert(46 * row + 23, $"{"",21}\nLocal:{"",17}"); break;
                case 3: subsb.Insert(46 * row + 34, $"{"",10}\nLocal:{"",28}"); break;
            }

            sb.Append(subsb);

            sb.AppendLine($"Bytecode ({OpCodes.Length}):");
            foreach (var (b, i) in OpCodes.Select((b, i) => (b, i))) {
                string opcode = $"{Int(b.opcode),4}  ";
                string arg1 = IsInt(b.arg1) ? $"{Int(b.arg1),6}    " : $"{b.arg1,10:0.000}";
                string arg2 = IsInt(b.arg2) ? $"{Int(b.arg2),6}    " : $"{b.arg2,10:0.000}";
                string arg3 = IsInt(b.arg3) ? $"{Int(b.arg3),6}    " : $"{b.arg3,10:0.000}";
                sb.AppendLine($"{i,4} | {opcode} {arg1} {arg2} {arg3}");
            }

            return sb.ToString();
        }
    }
}
