using Atrufulgium.BulletScript.Compiler.HighLevelOpCodes;
using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Collections.ObjectModel;

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

        public EmitWalker() => OPCodes = new ReadOnlyCollection<HLOP>(opCodes);

        protected override void VisitRoot(Root node) {
            if (node.Declarations.Count > 0)
                throw new VisitorAssumptionFailedException("The tree should be in root-level statement form.");

            foreach (var s in node.RootLevelStatements) {
                if (s is IEmittable e)
                    opCodes.AddRange(e.Emit(Model));
                else
                    throw new VisitorAssumptionFailedException($"Every root-level statement should be emittable. Problem: {s.ToCompactString()}");
            }
        }
    }
}
