using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YieldAnalyzer
{

    // swtich, case에 대한 건 나중에 고민해보자. 분기지만 block 안에 없을 수 있음
    public class YieldBlock
    {
        public int SeqID { get; }
        public CSharpSyntaxNode Parent { get; }
        public List<YieldStatementSyntax> Yields { get; } = new List<YieldStatementSyntax>();
        public bool HasBreak { get; private set; }
        public bool OpSync { get; set; }
        public bool AllBreak => this.Yields.All(x => x.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword));

        internal YieldBlock(CSharpSyntaxNode parent, int seqID)
        {
            this.Parent = parent;
            this.SeqID = seqID;
            this.HasBreak = false;
        }

        public void Add(YieldStatementSyntax yield)
        {
            this.Yields.Add(yield);
            this.HasBreak = yield.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword);
        }

        public bool Contains(YieldStatementSyntax yield)
        {
            return Yields.Contains(yield);
        }

        public override string ToString()
        {
            string parent = "";
            if (Parent is BlockSyntax)
            {

            }
            else
            {
                parent = Parent?.ToString();
            }
            return $"{parent}{string.Join("\n", Yields)}\n{SyntaxHelper.GetSourceCodeLocation(Yields.FirstOrDefault(), "")}";
        }

        public BlockSyntax GetBlock()
        {
            return SyntaxHelper.FindParentSyntax<BlockSyntax>(Parent);
        }
    }
}
