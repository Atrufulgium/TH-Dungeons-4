using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.BulletScript.Compiler.Syntax {
    /// <summary>
    /// Represents an <c>if</c>-statement, optionally with an <c>else</c> clause.
    /// </summary>
    internal class IfStatement : Statement {
        public Expression Condition { get; private set; }
        public Block TrueBranch { get; private set; }
        public Block? FalseBranch { get; private set; }

        public IfStatement(
            Expression condition,
            Block trueBranch,
            Location location = default,
            Block? falseBranch = null
        ) : base(location) {
            Condition = condition;
            TrueBranch = trueBranch;
            FalseBranch = falseBranch;
        }

        public override string ToString()
            => $"[if]\ncondition:\n{Indent(Condition)}\ntrue:\n{Indent(TrueBranch)}\nfalse:\n{Indent(FalseBranch)}";

        public override string ToCompactString() {
            string res = $"[if]                     if ({Condition.ToCompactString()})\n{CompactIndent(TrueBranch)}";
            if (FalseBranch != null)
                res += $"\nelse\n{CompactIndent(FalseBranch)}";
            return res;
        }

        public override IEnumerable<Diagnostic> ValidateTree(IEnumerable<Node> path)
            => Condition.ValidateTree(path.Append(this))
            .Concat(TrueBranch.ValidateTree(path.Append(this)))
            .Concat(FalseBranch?.ValidateTree(path.Append(this)) ?? new List<Diagnostic>());

        public IfStatement WithCondition(Expression condition)
            => new(condition, TrueBranch, Location, FalseBranch);
        public IfStatement WithTrueBranch(Block trueBranch)
            => new(Condition, trueBranch, Location, FalseBranch);
        public IfStatement WithFalseBranch(Block? falseBranch)
            => new(Condition, TrueBranch, Location, falseBranch);
    }
}
