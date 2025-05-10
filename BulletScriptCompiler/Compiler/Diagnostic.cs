using System.Linq;
using System.Collections.Generic;

namespace Atrufulgium.BulletScript.Compiler {
    /// <summary>
    /// Represents some information about the source file.
    /// </summary>
    public class Diagnostic {
        /// <summary>
        /// What location in the source file triggered this diagnostic.
        /// </summary>
        public Location Location { get; private set; }
        /// <summary>
        /// The severity of this diagnostic.
        /// </summary>
        public DiagnosticLevel DiagnosticLevel { get; private set; }
        /// <summary>
        /// Abbreviated code for errors, of the form AB1234.
        /// </summary>
        public string ID { get; private set; }
        /// <summary>
        /// The informational content of this diagnostic.
        /// </summary>
        public string Message { get; private set; }

        internal Diagnostic(Location location, DiagnosticLevel diagnosticLevel, string code, string message) {
            Location = location;
            DiagnosticLevel = diagnosticLevel;
            ID = code;
            Message = message;
        }

        /// <summary>
        /// Human-readable representation of the diagnostic.
        /// </summary>
        public override string ToString()
            => $"[{ID}|{DiagnosticLevel} at {Location}] {Message}";
    }

    /// <summary> How critical a diagnostic is. </summary>
    public enum DiagnosticLevel {
        /// <summary> This diagnostic is informational and can be ignored. </summary>
        Info = 1,
        /// <summary> This diagnostic is not critical, but it would be smart to act on it. </summary>
        Warning = 2,
        /// <summary> This diagnostic is critical and prevents compilation. </summary>
        Error = 3
    }

    internal static class DiagnosticExtensions {
        /// <summary>
        /// Whether a collection of diagnostics contains a diagonstic of level
        /// <see cref="DiagnosticLevel.Warning"/>.
        /// </summary>
        public static bool ContainsWarnings(this IEnumerable<Diagnostic> diagnostics)
            => diagnostics.Where(d => d.DiagnosticLevel == DiagnosticLevel.Warning).Any();


        /// <summary>
        /// Whether a collection of diagnostics contains a diagonstic of level
        /// <see cref="DiagnosticLevel.Error"/>.
        /// </summary>
        public static bool ContainsErrors(this IEnumerable<Diagnostic> diagnostics)
            => diagnostics.Where(d => d.DiagnosticLevel == DiagnosticLevel.Error).Any();
    }
}
