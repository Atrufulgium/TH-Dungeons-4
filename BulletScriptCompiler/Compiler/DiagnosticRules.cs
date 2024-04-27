using Atrufulgium.BulletScript.Compiler.Parsing;

namespace Atrufulgium.BulletScript.Compiler {
    internal static class DiagnosticRules {
        public static Diagnostic UnterminatedString(Location location, string token)
            => new(location, DiagnosticLevel.Error, "BS0001",
                $"Encountered unterminated string:\n  {token}\nAll strings must be enclosed with \" on both sides."
            );

        public static Diagnostic UnknownToken(Location location, string token)
            => new(location, DiagnosticLevel.Error, "BS0002",
                $"Encountered unexpected token:\n  {token}\n(If you started a name with a \"_\", that is not allowed.)"
            );
    }
}
