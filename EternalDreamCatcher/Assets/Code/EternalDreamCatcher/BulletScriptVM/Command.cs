using System;

namespace Atrufulgium.EternalDreamCatcher.BulletScriptVM {
    /// <summary>
    /// Represents a low-level command that a burst-compiled VM can output.
    /// <br/>
    /// Low-level commands mirror opcodes, but have all values filled in.
    /// </summary>
    internal readonly struct Command : IEquatable<Command> {
        public readonly CommandEnum command;
        public readonly float arg1;
        public readonly float arg2;
        public readonly float arg3;

        public Command(
            CommandEnum command,
            float arg1 = float.NaN,
            float arg2 = float.NaN,
            float arg3 = float.NaN
        ) {
            this.command = command;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }

        public bool Equals(Command other)
            => command == other.command
            && (arg1 == other.arg1 || (float.IsNaN(arg1) && float.IsNaN(other.arg1)))
            && (arg2 == other.arg2 || (float.IsNaN(arg2) && float.IsNaN(other.arg2)))
            && (arg3 == other.arg3 || (float.IsNaN(arg3) && float.IsNaN(other.arg3)));

        public override bool Equals(object obj)
            => obj is Command other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(command, arg1, arg2, arg3);

        public static bool operator ==(Command a, Command b) => a.Equals(b);
        public static bool operator !=(Command a, Command b) => !a.Equals(b);

        public override string ToString()
            => $"{command,24} | {(float.IsNaN(arg1) ? new string('-', 12) : arg1),12} | {(float.IsNaN(arg2) ? new string('-', 12) : arg2), 12} | {(float.IsNaN(arg3) ? new string('-', 12) : arg3), 12}";
    }

    internal enum CommandEnum {
        InvalidCommand = 0,
        ContinueAfterTime,
        SendMessage,
        Spawn, // TODO: Snapshot relevant variables somehow.
        Destroy,
        LoadBackground,
        AddScriptBarrier,
        AddScript,
        AddScriptWithValue,
        StartScript,
        StartScriptWithValue,
        StartScriptMany,
        StartScriptManyWithValue,
        Depivot,
        AddRotation,
        SetRotation,
        AddSpeed,
        SetSpeed,
        FacePlayer,
        AngleToPlayer, // TODO: Not appropriate as command.
        Gimmick
    }
}
