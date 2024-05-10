using Atrufulgium.BulletScript.Compiler.Syntax;

namespace Atrufulgium.BulletScript.Compiler.Visitors {
    /// <summary>
    /// This class performs the following rewrite:
    /// <list type="bullet">
    /// <item>
    /// Nested calls
    /// <code>
    ///     f( .. g(..) .. );
    /// </code>
    /// turn into
    /// <code>
    ///     type g_res = g(..);
    ///     f( .. g_res .. );
    /// </code>
    /// </item>
    /// of course including arbitrary nesting.
    /// </list>
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// <list type="bullet">
    /// <item><b>ASSUMPTIONS BEFORE:</b> None. </item>
    /// <item><b>ASSUMPTIONS AFTER:</b> Method calls are not nested inside eachother. </item>
    /// </list>
    /// </remarks>
    internal class FlattenNestedCallsRewriter : AbstractTreeRewriter {

        readonly Dictionary<Syntax.Type, int> varIDs = new();
        IdentifierName GetNewVar(Syntax.Type type) {
            if (!varIDs.TryGetValue(type, out int val)) {
                val = 0;
            }
            varIDs[type] = val + 1;
            return new($"invocation#result#{type}#{val}");
        }

        // Depth-first, whenever we encounter a call beyond the first, make it
        // a variable of the correct type, and replace the invocation with a
        // reference to that variable.
        readonly List<Statement> prependedStatements = new();
        // Bottom layer invocation does not need to be replaced.
        int layer = -1;
        protected override Node? VisitStatement(Statement node) {
            prependedStatements.Clear();
            varIDs.Clear();

            node = (Statement)base.VisitStatement(node)!;
            if (prependedStatements.Count == 0)
                return node;
            return new MultipleStatements(
                new MultipleStatements(prependedStatements),
                node
            );
        }

        protected override Node? VisitInvocationExpression(InvocationExpression node) {
            layer++;
            var key = node;
            node = (InvocationExpression)base.VisitInvocationExpression(node)!;
            bool bottomMostLayer = layer == 0;
            layer--;
            if (bottomMostLayer)
                // Bottom layer don't care
                return node;

            // Insert the declaration of a variable we will now make the
            // replaced argument.
            var type = Model.GetExpressionType(key);
            IdentifierName name = GetNewVar(type);
            prependedStatements.Add(
                new LocalDeclarationStatement(
                    new VariableDeclaration(
                        name,
                        type,
                        initializer: node
                    )
                )
            );
            return name;
        }
    }
}
