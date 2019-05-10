using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TAParser
{
    internal class Ta4OpenQuantRewriter : CSharpSyntaxRewriter
    {
        private static readonly string[] BarDataItems = { "inHigh", "inLow", "inOpen", "inClose", "inVolume" };
        private readonly HashSet<string> _paramsAdded = new HashSet<string>();

        private static bool IsDoubleArray(ParameterSyntax p)
        {
            return p.Type.IsKind(SyntaxKind.ArrayType)
                   && ((ArrayTypeSyntax)p.Type).ElementType.IsKind(SyntaxKind.PredefinedType)
                   && ((PredefinedTypeSyntax)((ArrayTypeSyntax)p.Type).ElementType).Keyword.IsKind(SyntaxKind.DoubleKeyword);
        }

        private static bool IsBarData(ParameterSyntax p)
        {
            var name = p.Identifier.Text;
            return Array.IndexOf(BarDataItems, name) >= 0;
        }

        private static bool IsInReal(ParameterSyntax p)
        {
            var name = p.Identifier.Text;
            return name.StartsWith("inReal");
        }

        private static bool IsInParameter(ParameterSyntax p)
        {
            var name = p.Identifier.Text;
            return name.StartsWith("in") || name == "startIdx" || name == "endIdx";
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (_paramsAdded.Contains(node.Expression.ToString())) {
                //node.ArgumentList
            }
            //SyntaxFactory.ParseExpression()
            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitParameterList(ParameterListSyntax node)
        {
            if (node.Parameters.Count > 0 && IsInParameter(node.Parameters.First())) {
                var paramList = new StringBuilder();
                var hasBarData = false;

                foreach (var parameter in node.Parameters) {
                    if (paramList.Length > 0) {
                        paramList.Append(",");
                    }
                    if (IsDoubleArray(parameter) && IsInParameter(parameter)) {
                        if (IsBarData(parameter)) {
                            hasBarData = true;
                        }
                        else if (IsInReal(parameter)) {
                            paramList.Append($"SmartQuant.ISeries {parameter.Identifier.Text}");
                            continue;
                        }
                    }
                    paramList.Append(parameter.ToString());
                }

                if (hasBarData) {
                    _paramsAdded.Add(((MethodDeclarationSyntax)node.Parent).Identifier.Text);
                    paramList.Append($",SmartQuant.ISeries inBar");
                }

                return SyntaxFactory.ParseParameterList($"({paramList.ToString()})\r\n");
            }
            return base.VisitParameterList(node);
        }

        public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            return base.VisitElementAccessExpression(node);
        }

    }

    class Program
    {
        private const string CoreFilePath = @"..\..\..\TALibraryInCSharp\TACore.cs";
        private const string Ta4OqFilePath = @"..\..\..\TALibraryInCSharp\TA4OpenQuant.cs";
        private const string FuncPath = @"..\..\..\TALibraryInCSharp\TAFunc";

        static void Main(string[] args)
        {
            CreateTA4OpenQuant();
        }

        private static void CreateTA4OpenQuant()
        {
            var coreCode = File.ReadAllText(CoreFilePath);
            var parser = new TaLibCodeParser();
            parser.Scan(coreCode);
            foreach (var file in Directory.GetFiles(FuncPath, "*.cs", SearchOption.TopDirectoryOnly)) {
                parser.Scan(File.ReadAllText(file));
            }

            var rewriter = new Ta4OpenQuantRewriter();
            var list = new List<MethodDeclarationSyntax>();
            foreach (var member in parser.MethodMembers) {
                list.Add((MethodDeclarationSyntax)rewriter.Visit(member));
            }
            parser.MethodMembers.Clear();
            parser.MethodMembers.AddRange(list);
            parser.Save(Ta4OqFilePath);
        }
    }
}
