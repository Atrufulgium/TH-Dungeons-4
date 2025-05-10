using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// When you want to return multiple declarations when doing rewrites,
    /// return this and the declarations get flattened into the surrounding
    /// list of declarations.
    /// <br/>
    /// This node is only valid when it is returned from one of:
    /// <list type="bullet">
    /// <item>
    /// <see cref="Visitors.AbstractTreeRewriter.VisitMethodDeclaration(MethodDeclaration)"/>; and
    /// </item>
    /// <item>
    /// <see cref="Visitors.AbstractTreeRewriter.VisitVariableDeclaration(VariableDeclaration)"/>
    /// </item>
    /// </list>
    /// when they are directly parented to a <see cref="Root"/>.
    /// </summary>
    internal class MultipleDeclarations : Declaration, ITransientNode {
        public ReadOnlyCollection<Declaration> Declarations { get; private set; }

        public MultipleDeclarations(IList<Declaration> declarations)
            : base(new IdentifierName("(not a real declaration)"), Type.Error, Location.CompilerIntroduced) {
            Declarations = new(declarations);
        }

        public MultipleDeclarations(params Declaration[] declarations)
            : this(declarations.ToList()) { }

        public override string ToString()
            => $"[multiple declarations]\ndeclarations:\n{Indent(Declarations)}";

        public override string ToCompactString()
            => $"[multiple declarations]\n{CompactIndent(Declarations)}";

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => throw new PersistentTransientException(this);

        public MultipleDeclarations WithDeclarations(IList<Declaration> Declarations)
            => new(Declarations);
        public MultipleDeclarations WithDeclarations(params Declaration[] Declarations)
            => new(Declarations);

        public MultipleDeclarations WithPrependedDeclarations(IList<Declaration> Declarations)
            => new(Declarations.Concat(Declarations).ToList());
        public MultipleDeclarations WithPrependedDeclarations(params Declaration[] Declarations)
            => new(Declarations.Concat(Declarations).ToList());

        public MultipleDeclarations WithAppendedDeclarations(IList<Declaration> Declarations)
            => new(Declarations.Concat(Declarations).ToList());
        public MultipleDeclarations WithAppendedDeclarations(params Declaration[] Declarations)
            => new(Declarations.Concat(Declarations).ToList());

        /// <summary>
        /// Removes nested <see cref="MultipleDeclarations"/> into one list.
        /// </summary>
        public IEnumerable<Declaration> Flatten() {
            foreach(var s in Declarations) {
                if (s is MultipleDeclarations m) {
                    foreach (var s2 in m.Flatten())
                        yield return s2;
                } else {
                    yield return s;
                }
            }
        }
    }
}
