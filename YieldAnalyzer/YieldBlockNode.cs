using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YieldAnalyzer
{
    public class YieldBlockRoute
    {
        public List<YieldBlockNode> RouteNodes { get; } = new List<YieldBlockNode>();
        public YieldBlockRoute()
        {
        }

        public YieldBlockRoute(YieldBlockNode node)
        {
            this.Add(node);
        }

        public void Add(YieldBlockNode node)
        {
            RouteNodes.Add(node);
        }

        public void Merge(YieldBlockRoute route)
        {
            this.RouteNodes.AddRange(route.RouteNodes);
        }

        public void AddParents(YieldBlockNode node)
        {
            var parentsNode = new Stack<YieldBlockNode>();
            foreach (var parent in node.GetAncestors())
            {
                if (parent.Parent == null)
                {
                    break;
                }

                parentsNode.Push(parent);
            }
            this.RouteNodes.AddRange(parentsNode);
            this.RouteNodes.Add(node);
        }

        public bool HasBreak()
        {
            return this.RouteNodes.Any(x => x.IsBranch == false && x.Yield.HasBreak == true);
        }

        public IEnumerable<YieldStatementSyntax> GetYieldReturns()
        {
            return this.RouteNodes
                    .Where(x => x.IsBranch == false && x.Yield.OpSync == false && x.Yield.AllBreak == false)
                    .SelectMany(x => x.Yield.Yields)
                    .Where(x => x.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword) == false);
        }

        public override string ToString()
        {
            if (RouteNodes.Count == 0)
            {
                return "No Routes";
            }

            int n = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var node in RouteNodes)
            {
                if (node.IsBranch == false)
                {
                    ++n;
                    sb.Append($"[{n}] >> ");
                }
                sb.AppendLine(node.ToString());
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    public class YieldBlockNode
    {
        public YieldBlockNode() : this(null, null, null)
        {
        }

        public YieldBlockNode(YieldBlockNode parent, YieldBlock yield, IfStatementSyntax branch)
        {
            this.Parent = parent;
            if (parent != null)
            {
                this.Depth = parent.Depth + 1;
            }
            this.Yield = yield;
            this.Branch = branch;
            if (this.Yield != null)
            {
                if (this.Branch != null && this.Branch.Else != null)
                {
                    this.Else = SyntaxHelper.FindParentSyntax<ElseClauseSyntax>(yield.Parent);
                }
            }
        }

        public IEnumerable<YieldBlockNode> GetAncestors()
        {
            var parent = this.Parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }

        public IEnumerable<YieldBlockNode> GetDescendants()
        {
            foreach (var child in this.Childs)
            {
                yield return child;
                foreach (var child2 in child.GetDescendants())
                {
                    yield return child2;
                }
            }
        }

        public IEnumerable<YieldBlockNode> GetTerminals()
        {
            foreach (var child in this.Childs)
            {
                if (child.IsTerminal)
                {
                    yield return child;
                }
                else
                {
                    foreach (var terminal in child.GetTerminals())
                    {
                        yield return terminal;
                    }
                }
            }
        }

        public IfStatementSyntax Branch { get; }
        public ElseClauseSyntax Else { get; }

        public YieldBlockNode Parent { get; }
        public List<YieldBlockNode> Childs { get; } = new List<YieldBlockNode>();
        public YieldBlock Yield { get; }
        public int Depth { get; }
        public bool IsTerminal => this.Childs.Count == 0;
        public bool IsBranch => this.Branch != null && this.Yield == null;

        public void AddChild(YieldBlockNode child)
        {
            this.Childs.Add(child);
        }

        public override string ToString()
        {
            if (this.IsBranch)
            {
                return "if (" + (this.Branch.Condition.ToString() ?? "None") + ")";
            }

            return this.Yield.ToString() ?? "";
        }

        public string ToDebug()
        {
            if (this.Branch != null && this.Childs.Count == 0)
            {
                return this.Branch.ToString();
            }

            int i = 0;
            IfStatementSyntax branch = null;
            StringBuilder sb = new StringBuilder(this.ToString());
            foreach (var child in GetDescendants())
            {
                if (branch != null && branch != child.Branch)
                {
                    sb.AppendLine("========== Branch ===========");
                    branch = child.Branch;
                    sb.AppendLine(branch.ToString());
                    sb.AppendLine("=============================");
                }

                sb.Append(new string('>', child.Depth));
                sb.AppendLine($"[{i}] Debug.");
                sb.AppendLine($"Parent : {child?.Yield?.Parent.GetType().Name ?? "Null"}");
                sb.AppendLine(child.ToString());
                sb.AppendLine();

                ++i;
            }
            return sb.ToString();
        }
    }
}
