using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YieldAnalyzer
{
    public static class SyntaxHelper
    {
        public static ObjectCreationExpressionSyntax FindLocalVarCreation(YieldStatementSyntax yieldStatement, MethodDeclarationSyntax method)
        {
            if (yieldStatement.ReturnOrBreakKeyword.IsKind(SyntaxKind.ReturnKeyword) == false)
            {
                return null;
            }

            ObjectCreationExpressionSyntax creationSyntax = yieldStatement.Expression as ObjectCreationExpressionSyntax;
            if (creationSyntax != null)
            {
                return creationSyntax;
            }

            var parentNode = yieldStatement.Parent;
            while (parentNode != null)
            {
                foreach (var variable in parentNode
                .DescendantNodes()
                .OfType<VariableDeclarationSyntax>()
                .Where(x => x.Variables.Any(y => y.Identifier.Text == yieldStatement.Expression.ToString())))
                {
                    return variable.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
                }

                if (parentNode == method.Body)
                {
                    break;
                }
                parentNode = parentNode.Parent;
            }

            return null;
        }

        public static bool IsAbstractClass(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.AbstractKeyword));
        }

        public static bool IsOverrideMethod(MethodDeclarationSyntax method)
        {
            return method.Modifiers.Any(x => x.IsKind(SyntaxKind.OverrideKeyword));
        }

        public static IEnumerable<MethodDeclarationSyntax> FindMethods(ClassDeclarationSyntax classDeclaration, string methodName)
        {
            foreach (var member in classDeclaration
                .Members
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.Identifier.Text == methodName))
            {
                yield return member;
            }
        }

        public static IdentifierNameSyntax FindObjectCreationReturnType(MethodDeclarationSyntax methodSyntax)
        {
            var returnTypeSyntax = methodSyntax.DescendantNodes()
                        .OfType<ReturnStatementSyntax>()
                        .Select(x => x.Expression)
                        .OfType<ObjectCreationExpressionSyntax>()
                        .Select(x => x.Type)
                        .OfType<IdentifierNameSyntax>()
                        .FirstOrDefault();
            return returnTypeSyntax;
        }

        public static IEnumerable<TypeSyntax> GetBaseTypes(ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.BaseList == null)
            {
                yield break;
            }

            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                yield return baseType.Type;
            }
        }

        public static T FindParentSyntax<T>(CSharpSyntaxNode statement) where T : CSharpSyntaxNode
        {
            SyntaxNode parent = statement;
            do
            {
                parent = parent?.Parent;
                if (parent is T type)
                {
                    return type;
                }
            } while (parent != null);
            return null;
        }

        public static string GetName(TypeSyntax type)
        {
            if (type is IdentifierNameSyntax identifierNameSyntax)
            {
                return identifierNameSyntax.Identifier.Text;
            }
            else if (type is QualifiedNameSyntax qualifiedNameSyntax)
            {
                return qualifiedNameSyntax.Right.Identifier.Text;
            }
            else if (type is GenericNameSyntax genericName)
            {
                return genericName.Identifier.Text;
            }
            return null;
        }

        public static string GetSourceCodeLocation(CSharpSyntaxNode node, string ifNull = null)
        {
            var location = node?.GetLocation();
            if (location == null)
            {
                return ifNull ?? "Unknwon Location";
            }

            return location + "\n" + "line : " + location.GetLineSpan().StartLinePosition.Line;
        }


        public static string GetSourceCodeLocationWithNode(CSharpSyntaxNode node, string ifNull = null)
        {
            var location = node?.GetLocation();
            if (location == null)
            {
                return ifNull ?? "Unknwon Location";
            }

            return $"{node}\n{location}\nline : {location.GetLineSpan().StartLinePosition.Line}";
        }
    }
}
