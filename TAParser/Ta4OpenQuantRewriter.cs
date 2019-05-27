using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace TAParser
{
    internal class Ta4OpenQuantRewriter : CSharpSyntaxRewriter
    {
        private static readonly HashSet<string> BarDataItems = new HashSet<string> { "inHigh", "inLow", "inOpen", "inClose", "inVolume" };
        private bool _hasBarData;
        private bool _rewriteInternalCall;

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

        private static string ReplaceLastOccurrence(string source, string find, string replace)
        {
            var place = source.LastIndexOf(find, StringComparison.Ordinal);
            if (place == -1)
                return source;
            var result = source.Remove(place, find.Length).Insert(place, replace);
            return result;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            const string arrayCopy = "Array.Copy(inReal";
            const string seriesCopy = "SeriesCopy(inReal";
            if (_rewriteInternalCall) {
                if (node.ArgumentList.Arguments.Any(n => BarDataItems.Contains(n.Expression.ToString()))) {
                    var list = node.ToFullString();
                    return SyntaxFactory.ParseExpression(ReplaceLastOccurrence(list, ")", ", inBar)"));
                }
            }
            else {
                var code = node.ToString();
                if (code.StartsWith(arrayCopy)) {
                    return SyntaxFactory.ParseExpression(node.ToFullString().Replace(arrayCopy, seriesCopy));
                }
            }
            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitParameterList(ParameterListSyntax node)
        {
            if (!_rewriteInternalCall && node.Parameters.Count > 0 && IsInParameter(node.Parameters.First())) {
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
                    paramList.Append(parameter);
                }

                if (_hasBarData) {
                    paramList.Append($",SmartQuant.ISeries inBar");
                }

                return SyntaxFactory.ParseParameterList($"({paramList})\r\n");
            }
            return base.VisitParameterList(node);
        }

        public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (!_rewriteInternalCall) {
                var exp = node.Expression.ToString();
                if (BarDataItems.Contains(exp)) {
                    var args = node.ArgumentList.ToString().Replace("]", $",SmartQuant.BarData.{exp.Substring(2)}]");
                    return SyntaxFactory.ParseExpression($"inBar{args}");
                }
            }
            return base.VisitElementAccessExpression(node);
        }

        public MethodDeclarationSyntax RewriteInternalCall(MethodDeclarationSyntax method)
        {
            if (method.ParameterList.Parameters.Last().Identifier.Text != "inBar") {
                return method;
            }
            _rewriteInternalCall = true;
            try {
                return (MethodDeclarationSyntax)Visit(method);
            }
            finally {
                _rewriteInternalCall = false;
            }
        }
    }
}