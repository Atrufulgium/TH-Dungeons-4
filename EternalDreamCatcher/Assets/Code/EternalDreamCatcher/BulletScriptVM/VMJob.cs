using Atrufulgium.EternalDreamCatcher.Base;
using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Atrufulgium.EternalDreamCatcher.BulletScriptVM {

    public partial struct VM {

        /// <summary>
        /// Starts or resumes execution of the VM directly on the same thread.
        /// <br/>
        /// The output behaviour can be read and further processed with
        /// <see cref="ConsumeCommands"/>.
        /// </summary>
        public unsafe void RunMain() {
            RunJob job = new() {
                instructions = instructions,
                op = op,
                floatMemory = floatMemory,
                rng = rng,
                commands = outputCommands
            };
            job.Execute();
            // Outputcommands may resize, moving the underlying pointer around...
            outputCommands = job.commands;
        }

        /// <summary>
        /// This job runs the VM until it either halts, or reaches a maximum
        /// number of iterations.
        /// </summary>
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
        private unsafe struct RunJob : IJob {

            public UnsafeList<float4> instructions;

            [NativeDisableUnsafePtrRestriction]
            public uint* op;
            public UnsafeList<float> floatMemory;

            [NativeDisableUnsafePtrRestriction]
            public Unity.Mathematics.Random* rng;

            public UnsafeList<Command> commands;

            const int MAX_ITERS = 100000;

            public unsafe void Execute() {
                var ops = (float4*)instructions.GetUnsafeTypedPtr();
                var opsi = (int4*)instructions.GetUnsafeTypedPtr();
                var mem = (float*)floatMemory.GetUnsafeTypedPtr();
                var mem4 = (float4*)floatMemory.GetUnsafeTypedPtr();
                var op = (int*)this.op;
                var maxOP = instructions.Length;

                for (int i = 0; i < MAX_ITERS && *op < maxOP; i++, *op += 1) {
                    ref float a = ref ops[*op].y; // first arg, as float
                    ref float b = ref ops[*op].z; // 2nd arg, as float
                    ref float c = ref ops[*op].w; // 3rd arg, as float
                    ref int ai = ref opsi[*op].y; // first arg, as int
                    ref int bi = ref opsi[*op].z; // 2nd arg, as int
                    ref int ci = ref opsi[*op].w; // 3rd arg, as int

                    // TODO: Perhaps reorder this to put the most common ones on top.
                    // (If only I could write inline assembly, then I could
                    //  (1) redefine the opcodes to be offsets from a base
                    //      instruction so you jumped to base+variable;
                    //  (2) not loop back to the start of the for loop each
                    //      iter and directly at the end of every OP go to the
                    //      next
                    //  Alas, I don't have the luxury...)
                    // OTOH, when leaving it to Burst, they get reordered
                    // in arbitrary order _anyway_.
                    switch (opsi[*op].x) {
                        // General ops
                        case   1: mem[ai] = b == mem[ci] ? 1 : 0; break;
                        case   2: // (floats and strings handled the same)
                        case   3: mem[ai] = mem[bi] == mem[ci] ? 1 : 0; break;
                        case   4: mem[ai] = b < mem[ci] ? 1 : 0; break;
                        case   5: mem[ai] = mem[bi] < c ? 1 : 0; break;
                        case   6: mem[ai] = mem[bi] < mem[ci] ? 1 : 0; break;
                        case   7: mem[ai] = b <= mem[ci] ? 1 : 0; break;
                        case   8: mem[ai] = mem[bi] <= c ? 1 : 0; break;
                        case   9: mem[ai] = mem[bi] <= mem[ci] ? 1 : 0; break;
                        case  10: mem[ai] = b; break;
                        case  11: // (floats and strings handled the same)
                        case  12: mem[ai] = mem[bi]; break;
                        case  13: // (indexed stuff)
                        case  14:
                        case  15:
                        case  16:
                        case  17:
                        case  18: throw new NotImplementedException();
                        case  19: *op = ai; break; // TODO: Don't forget to make the generated assembly set the label to 1 instruction before, due to the *op++.
                        case  20: if (mem[bi] != 0) *op = ai; break;
                        case  21: Command(CommandEnum.ContinueAfterTime, b); goto pause;
                        case  22: Command(CommandEnum.ContinueAfterTime, mem[bi]); goto pause;

                        // Misc intrinsics
                        case  32: Command(CommandEnum.SendMessage, mem[ai]); break;
                        case  33: Command(CommandEnum.SendMessage, a); break;
                        case  34: Command(CommandEnum.Spawn); break;
                        case  35: Command(CommandEnum.Destroy); break;
                        case  36: Command(CommandEnum.LoadBackground, mem[ai]); break;
                        case  37: Command(CommandEnum.AddScriptBarrier); break;
                        case  38: Command(CommandEnum.AddScript, mem[ai]); break;
                        case  39: Command(CommandEnum.AddScriptWithValue, mem[ai], b); break;
                        case  40: Command(CommandEnum.AddScriptWithValue, mem[ai], mem[bi]); break;
                        case  41: Command(CommandEnum.StartScript, mem[ai]); break;
                        case  42: Command(CommandEnum.StartScriptWithValue, mem[ai], b); break;
                        case  43: Command(CommandEnum.StartScriptWithValue, mem[ai], mem[bi]); break;
                        case  44: Command(CommandEnum.StartScriptMany, mem[ai]); break;
                        case  45: Command(CommandEnum.StartScriptManyWithValue, mem[ai], bi); break;
                        case  46: Command(CommandEnum.StartScriptManyWithValue, mem[ai], mem[bi]); break;
                        case  47: Command(CommandEnum.Depivot); break;
                        case  48: Command(CommandEnum.AddRotation, a); break;
                        case  49: Command(CommandEnum.AddRotation, mem[ai]); break;
                        case  50: Command(CommandEnum.SetRotation, a); break;
                        case  51: Command(CommandEnum.SetRotation, mem[ai]); break;
                        case  52: Command(CommandEnum.AddSpeed, a); break;
                        case  53: Command(CommandEnum.AddSpeed, mem[ai]); break;
                        case  54: Command(CommandEnum.SetSpeed, a); break;
                        case  55: Command(CommandEnum.SetSpeed, mem[ai]); break;
                        case  56: Command(CommandEnum.FacePlayer); break;
                        case  57: Command(CommandEnum.AngleToPlayer); break; // No don't
                        case  58: Command(CommandEnum.Gimmick, mem[ai]); break;
                        case  59: Command(CommandEnum.Gimmick, mem[ai], b); break;
                        case  60: Command(CommandEnum.Gimmick, mem[ai], mem[bi]); break;
                        case  61: Command(CommandEnum.Gimmick, mem[ai], b, c); break;
                        case  62: Command(CommandEnum.Gimmick, mem[ai], b, mem[ci]); break;
                        case  63: Command(CommandEnum.Gimmick, mem[ai], mem[bi], c); break;
                        case  64: Command(CommandEnum.Gimmick, mem[ai], mem[bi], mem[ci]); break;

                        // Float math
                        case  80: mem[ai] = mem[bi] == 0 ? 1 : 0; break;
                        case  81: mem[ai] = b + mem[ci]; break;
                        case  82: mem[ai] = mem[bi] + mem[ci]; break;
                        case  83: mem[ai] = b - mem[ci]; break;
                        case  84: mem[ai] = mem[bi] - c; break;
                        case  85: mem[ai] = mem[bi] - mem[ci]; break;
                        case  86: mem[ai] = b * mem[ci]; break;
                        case  87: mem[ai] = mem[bi] * mem[ci]; break;
                        case  88: mem[ai] = b / mem[ci]; break;
                        case  89: mem[ai] = mem[bi] / c; break;
                        case  90: mem[ai] = mem[bi] / mem[ci]; break;
                        case  91: mem[ai] = b % mem[ci]; break;
                        case  92: mem[ai] = mem[bi] % c; break;
                        case  93: mem[ai] = mem[bi] % mem[ci]; break;
                        case  94: mem[ai] = pow(b, mem[ci]); break;
                        case  95: mem[ai] = pow(mem[bi], c); break;
                        case  96: mem[ai] = pow(mem[bi], mem[ci]); break;
                        case  97: mem[ai] = mem[bi] * mem[bi]; break;
                        case  98: mem[ai] = sin(mem[bi]); break;
                        case  99: mem[ai] = cos(mem[bi]); break;
                        case 100: mem[ai] = tan(mem[bi]); break;
                        case 101: mem[ai] = asin(mem[bi]); break;
                        case 102: mem[ai] = acos(mem[bi]); break;
                        case 103: mem[ai] = atan(mem[bi]); break;
                        case 104: mem[ai] = atan2(b, mem[ci]); break; // note the order!
                        case 105: mem[ai] = atan2(mem[bi], c); break;
                        case 106: mem[ai] = atan2(mem[bi], mem[ci]); break;
                        case 107: mem[ai] = ceil(mem[bi]); break;
                        case 108: mem[ai] = floor(mem[bi]); break;
                        case 109: mem[ai] = round(mem[bi]); break;
                        case 110: mem[ai] = abs(mem[bi]); break;
                        case 111: mem[ai] = rng->NextFloat(b, c); break;
                        case 112: mem[ai] = rng->NextFloat(b, mem[ci]); break;
                        case 113: mem[ai] = rng->NextFloat(mem[bi], c); break;
                        case 114: mem[ai] = rng->NextFloat(mem[bi], mem[ci]); break;
                        case 115: mem[ai] = distance(b, mem[ci]); break;
                        case 116: mem[ai] = distance(mem[bi], c); break;
                        case 117: mem[ai] = distance(mem[bi], mem[ci]); break;

                        // Vector math (excluding matrix-matrix multiplication)
                        case 128: mem4[ai] = mem4[bi]; break;
                        case 129: mem4[ai] = (float4)(mem4[bi] == mem4[ci]); break;
                        case 130: mem4[ai] = (float4)(mem4[bi] < mem4[ci]); break;
                        case 131: mem4[ai] = (float4)(mem4[bi] <= mem4[ci]); break;
                        case 132: mem4[ai] = -mem4[bi]; break;
                        case 133: mem4[ai] = 1 - mem4[bi]; break;
                        case 134: mem4[ai] = mem4[bi] + mem4[ci]; break;
                        case 135: mem4[ai] = mem4[bi] - mem4[ci]; break;
                        case 136: mem4[ai] = mem4[bi] * mem4[ci]; break;
                        case 137: mem4[ai] = mem4[bi] / mem4[ci]; break;
                        case 138: mem4[ai] = mem4[bi] % mem4[ci]; break;
                        case 139: mem4[ai] = pow(mem4[bi], mem4[ci]); break;
                        case 140: mem4[ai] = mem4[bi] * mem4[bi]; break;
                        case 141: mem4[ai] = sin(mem4[bi]); break;
                        case 142: mem4[ai] = cos(mem4[bi]); break;
                        case 143: mem4[ai] = tan(mem4[bi]); break;
                        case 144: mem4[ai] = asin(mem4[bi]); break;
                        case 145: mem4[ai] = acos(mem4[bi]); break;
                        case 146: mem4[ai] = atan(mem4[bi]); break;
                        case 147: mem4[ai] = atan2(mem4[bi], mem4[ci]); break; // arg order, again
                        case 148: mem4[ai] = ceil(mem4[bi]); break;
                        case 149: mem4[ai] = floor(mem4[bi]); break;
                        case 150: mem4[ai] = round(mem4[bi]); break;
                        case 151: mem4[ai] = abs(mem4[bi]); break;
                        case 152: mem4[ai] = rng->NextFloat4(mem4[bi], mem4[ci]); break;
                        case 153: mem[ai] = length(mem4[bi]); break;
                        case 154: mem[ai] = distance(mem4[bi], mem4[ci]); break;
                        case 155: mem4[ai] = polar(b, c); break;
                        case 156: mem4[ai] = polar(b, mem[ci]); break;
                        case 157: mem4[ai] = polar(mem[bi], c); break;
                        case 158: mem4[ai] = polar(mem[bi], mem[ci]); break;

                        // Matrix multiplication

                        // Not a valid opcode, which should've been filtered ages ago
                        default: throw new InvalidOperationException();
                    }
                }
                pause:;
            }
        
            private void Command(
                CommandEnum command,
                float arg1 = float.NaN,
                float arg2 = float.NaN,
                float arg3 = float.NaN
            ) {
                commands.Add(new(command, arg1, arg2, arg3));
            }

            private static float4 polar(float angle, float radius) {
                sincos(angle, out float s, out float c);
                return radius * float4(c, s, 0, 0);
            }
        }
    }
}
