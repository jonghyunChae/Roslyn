using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace YieldAnalyzer
{
    public static class YieldAnalyzer
    {
        public static YieldBlockNode Analyze(MethodDeclarationSyntax method)
        {
            var blockList = new List<YieldBlock>();

            var body = method.Body;

            int seqID = 0;
            YieldBlock currentYield = null;
            foreach (var yieldStatement in body.DescendantNodes().OfType<YieldStatementSyntax>())
            {
                // 스위치는 아직 미지원
                if (yieldStatement.Parent.Ancestors().OfType<SwitchStatementSyntax>().Any())
                {
                    return null;
                }

                if (currentYield == null || currentYield.Parent != yieldStatement.Parent)
                {
                    currentYield = new YieldBlock(yieldStatement.Parent as CSharpSyntaxNode, seqID++);
                    blockList.Add(currentYield);
                }

                currentYield.Add(yieldStatement);

                if (currentYield.OpSync == false)
                {
                    var creationSyntax = SyntaxHelper.FindLocalVarCreation(yieldStatement, method);
                    if (creationSyntax != null)
                    {
                        currentYield.OpSync = true;
                    }
                }
            }

            YieldBlockNode root = new YieldBlockNode();
            YieldBlockNode lastNode = root;
            foreach (var block in blockList)
            {
                //if (block.HasBreak && block.Yields.All(x => x.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword)))
                //{
                //    continue;
                //}

                YieldBlockNode parentNode = null;

                var branches = new Stack<IfStatementSyntax>();
                foreach (var ifSyntax in block.Parent.AncestorsAndSelf().OfType<IfStatementSyntax>())
                {
                    branches.Push(ifSyntax);
                }

                foreach (var ifSyntax in branches)
                {
                    if (parentNode == null)
                    {
                        parentNode = root;
                    }

                    var targetBranch = root.GetDescendants().FirstOrDefault(x => x.Branch == ifSyntax);
                    if (targetBranch == null)
                    {
                        var branchNode = new YieldBlockNode(parentNode, null, ifSyntax);
                        parentNode.AddChild(branchNode);
                        parentNode = branchNode;
                    }
                    else
                    {
                        parentNode = targetBranch;
                    }
                }

                var currentBranch = block.Parent as IfStatementSyntax ?? SyntaxHelper.FindParentSyntax<IfStatementSyntax>(block.Parent);
                if (currentBranch != null)
                {
                    parentNode = root.GetDescendants().FirstOrDefault(x => x.Branch == currentBranch);
                }

                if (parentNode == null)
                {
                    parentNode = root.GetDescendants().FirstOrDefault(x => x.Yield != null && x.Yield.Parent == block.Parent)?.Parent;
                    if (parentNode == null)
                    {
                        parentNode = root;
                    }
                }

                var blockNode = new YieldBlockNode(parentNode, block, null);
                parentNode.AddChild(blockNode);

                lastNode = blockNode;
            }
            return root;
        }

        public static List<YieldBlockRoute> MakeRoutes(YieldBlockNode rootNode)
        {
            // 순서 보장을 위해 일단 리스트로
            var newRoutes = new List<YieldBlockRoute>();
            var routes = new List<YieldBlockRoute>();
            var lastNode = rootNode.Childs.Last();
            foreach (var node in rootNode.Childs.Where(x => x.Yield == null || x.Yield.OpSync == false))
            {
                newRoutes.Clear();

                if (node.IsTerminal)
                {
                    if (routes.Count == 0 || routes.All(x => x.HasBreak()))
                    {
                        routes.Add(new YieldBlockRoute(node));
                        continue;
                    }

                    foreach (var route in routes.Where(x => x.HasBreak() == false))
                    {
                        var newRoute = new YieldBlockRoute();
                        newRoute.Merge(route);
                        newRoute.AddParents(node);

                        newRoutes.Add(newRoute);
                    }
                }
                else
                {
                    if (routes.Count == 0 || routes.All(x => x.HasBreak()))
                    {
                        routes.Add(new YieldBlockRoute(node));
                    }

                    foreach (var terminal in node.GetTerminals())
                    {
                        foreach (var route in routes.Where(x => x.HasBreak() == false))
                        {
                            var newRoute = new YieldBlockRoute();
                            newRoute.Merge(route);
                            newRoute.AddParents(terminal);

                            newRoutes.Add(newRoute);
                        }

                        /*
                         * if ()
                         * {
                         *   yield return a -> a block
                         * }
                         * else
                         * {
                         *   yield return b -> b block
                         * }
                         * yield break
                         * 
                         * root
                         * ㄴ if () ㅡ a block
                         *          ㄴ b block
                         * ㄴ yield break
                         * 이러한 구조에서 yield break일 경우 a block과 b block에 yield block을 이어준다.
                         */
                        if (terminal.IsBranch == false && terminal.Yield.AllBreak)
                        {
                            foreach (var route in newRoutes.Where(x => x.HasBreak() == false))
                            {
                                if (route.RouteNodes.Last().Parent?.Parent == terminal.Parent)
                                {
                                    route.Add(terminal);
                                }
                            }
                        }
                    }
                }

                routes.AddRange(newRoutes);
            }
            return routes;
        }

    }
}
