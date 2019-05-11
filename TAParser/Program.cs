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
        private static readonly HashSet<string> BarDataItems = new HashSet<string> { "inHigh", "inLow", "inOpen", "inClose", "inVolume" };
        private readonly HashSet<string> _paramsAdded = new HashSet<string>();
        private bool _hasBarData;

        private static bool IsDoubleArray(ParameterSyntax p)
        {
            return p.Type.IsKind(SyntaxKind.ArrayType)
                   && ((ArrayTypeSyntax)p.Type).ElementType.IsKind(SyntaxKind.PredefinedType)
                   && ((PredefinedTypeSyntax)((ArrayTypeSyntax)p.Type).ElementType).Keyword.IsKind(SyntaxKind.DoubleKeyword);
        }

        private static bool IsBarData(ParameterSyntax p)
        {
            var name = p.Identifier.Text;
            return BarDataItems.Contains(name);
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
            const string ArrayCopy = "Array.Copy(inReal";
            const string SeriesCopy = "SeriesCopy(inReal";

            var code = node.ToString();
            if (code.StartsWith(ArrayCopy)) {
                return SyntaxFactory.ParseExpression(node.ToFullString().Replace(ArrayCopy, SeriesCopy));
            }
            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitParameterList(ParameterListSyntax node)
        {
            if (node.Parameters.Count > 0 && IsInParameter(node.Parameters.First())) {
                var paramList = new StringBuilder();
                _hasBarData = false;

                foreach (var parameter in node.Parameters) {
                    if (paramList.Length > 0) {
                        paramList.Append(",");
                    }
                    if (IsDoubleArray(parameter) && IsInParameter(parameter)) {
                        if (IsBarData(parameter)) {
                            _hasBarData = true;
                        }
                        else if (IsInReal(parameter)) {
                            paramList.Append($"SmartQuant.ISeries {parameter.Identifier.Text}");
                            continue;
                        }
                    }
                    paramList.Append(parameter.ToString());
                }

                if (_hasBarData) {
                    paramList.Append($",SmartQuant.ISeries inBar");
                }

                return SyntaxFactory.ParseParameterList($"({paramList.ToString()})\r\n");
            }
            return base.VisitParameterList(node);
        }

        public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            var exp = node.Expression.ToString();
            if (BarDataItems.Contains(exp)) {
                var args = node.ArgumentList.ToString().Replace("]", $",SmartQuant.BarData.{exp.Substring(2)}]");
                return SyntaxFactory.ParseExpression($"inBar{args}");
            }
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
