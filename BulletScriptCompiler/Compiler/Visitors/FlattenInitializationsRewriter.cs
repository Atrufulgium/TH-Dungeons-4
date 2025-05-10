using Atrufulgium.BulletScript.Compiler.Syntax;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Declarations
    /// <code>
    ///     type name [= value];
    /// </code>
    /// turn into
    /// <code>
    ///     type name;
    ///     [name = value];
    /// </code>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> The root is in statements form and all variable declarations are at root- or block-level. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> Initializations are empty. </item>
    /// </list>
    /// </remarks>
    internal class FlattenInitializationsRewriter : AbstractTreeRewriter {

        bool variableDeclInLocalDecl = false;
        protected override Node? VisitLocalDeclarationStatement(LocalDeclarationStatement node) {
            variableDeclInLocalDecl = true;
            node = (LocalDeclarationStatement)base.VisitLocalDeclarationStatement(node)!;
            variableDeclInLocalDecl = false;

            var decl = new LocalDeclarationStatementWithoutInit(
                node.Declaration.Identifier,
                node.Declaration.Type
            );

            if (node.Declaration.Initializer == null)
                return decl;
            return new MultipleStatements(
                decl,
                new ExpressionStatement(
                    new AssignmentExpression(
                        node.Declaration.Identifier,
                        AssignmentOp.Set,
                        node.Declaration.Initializer
                    )
                )
            );
        }

        protected override Node? VisitVariableDeclaration(VariableDeclaration node) {
            if (!variableDeclInLocalDecl)
                throw new VisitorAssumptionFailedException("Assume intializations can be extracted directly into the surrounding context.");
            return base.VisitVariableDeclaration(node);
        }

        protected override Node? VisitForStatement(ForStatement node) {
            if (node.Initializer == null || node.Initializer is not LocalDeclarationStatement ldecl || ldecl.Declaration.Initializer == null)
                return base.VisitForStatement(node);
            throw new VisitorAssumptionFailedException("Assume intializations can be extracted directly into the surrounding context.");
        }

        protected override Node? VisitRoot(Root node) {
            if (node.Declarations.Count == 0 || !node.Declarations.OfType<VariableDeclaration>().Any())
                return base.VisitRoot(node);
            throw new VisitorAssumptionFailedException("Assume intializations can be extracted directly into the surrounding context.");
        }
    }
}
