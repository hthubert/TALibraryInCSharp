using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TAParser
{
    internal class TaLibCodeParser : CSharpSyntaxWalker
    {
        private readonly Stack<BaseTypeDeclarationSyntax> _classes = new Stack<BaseTypeDeclarationSyntax>();
        private readonly List<BaseTypeDeclarationSyntax> _innerClasses = new List<BaseTypeDeclarationSyntax>();
        private readonly List<FieldDeclarationSyntax> _fieldMembers = new List<FieldDeclarationSyntax>();
        public readonly List<MethodDeclarationSyntax> MethodMembers = new List<MethodDeclarationSyntax>();

        private const string CoreClassName = "Core";

        public TaLibCodeParser() : base(SyntaxWalkerDepth.Token)
        {
        }

        public override void VisitToken(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.CloseBraceToken) {
                if (_classes.Count > 0 && token.Span.End == _classes.Peek().Span.End) {
                    _classes.Pop();
                }
            }

            base.VisitToken(token);
        }

        public override void Visit(SyntaxNode node)
        {
            switch (node.Kind()) {
                case SyntaxKind.ClassDeclaration:
                    var c = (ClassDeclarationSyntax)node;
                    _classes.Push(c);
                    if (c.Identifier.Text != CoreClassName) {
                        _innerClasses.Add(c);
                    }
                    break;
                case SyntaxKind.FieldDeclaration:
                    if (_classes.Peek().Identifier.Text == CoreClassName) {
                        _fieldMembers.Add((FieldDeclarationSyntax)node);
                    }
                    break;
                case SyntaxKind.MethodDeclaration:
                    if (_classes.Peek().Identifier.Text == CoreClassName) {
                        var method = (MethodDeclarationSyntax)node;
                        if (method.ParameterList.Parameters.Any(IsFloatArray)) {
                            break;
                        }
                        MethodMembers.Add((MethodDeclarationSyntax)node);
                    }
                    break;
                case SyntaxKind.EnumDeclaration:
                    _classes.Push((BaseTypeDeclarationSyntax)node);
                    _innerClasses.Add((BaseTypeDeclarationSyntax)node);
                    break;
            }
            base.Visit(node);

            bool IsFloatArray(ParameterSyntax p)
            {
                return p.Type.IsKind(SyntaxKind.ArrayType)
                       && ((ArrayTypeSyntax)p.Type).ElementType.IsKind(SyntaxKind.PredefinedType)
                       && ((PredefinedTypeSyntax)((ArrayTypeSyntax)p.Type).ElementType).Keyword.IsKind(SyntaxKind.FloatKeyword);
            }
        }

        public void Scan(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            if (tree.HasCompilationUnitRoot) {
                tree.GetCompilationUnitRoot().Accept(this);
            }
        }

        public void Save(string path)
        {
            using (var writer = new StreamWriter(path)) {
                writer.WriteLine("using System;");
                writer.WriteLine("namespace TALibrary");
                writer.WriteLine("{");
                writer.WriteLine("public class TA4OpenQuant");
                writer.WriteLine("{");
                writer.WriteLine("#region Private Members");
                foreach (var c in _innerClasses) {
                    if (c.Modifiers.Any(n => n.IsKind(SyntaxKind.PrivateKeyword))) {
                        c.WriteTo(writer);
                    }
                }
                foreach (var c in _fieldMembers) {
                    c.WriteTo(writer);
                }
                foreach (var c in MethodMembers) {
                    if (c.Modifiers.Any(n => n.IsKind(SyntaxKind.PrivateKeyword))) {
                        c.WriteTo(writer);
                    }
                }
                writer.WriteLine(" static TA4OpenQuant() { RestoreCandleDefaultSettings(CandleSettingType.AllCandleSettings); }");
                writer.WriteLine("#endregion");
                foreach (var c in MethodMembers) {
                    if (c.Modifiers.Any(n => n.IsKind(SyntaxKind.PublicKeyword))) {
                        c.WriteTo(writer);
                    }
                }
                writer.WriteLine("#region Public Nested Classes");
                foreach (var c in _innerClasses) {
                    if (c.Modifiers.Any(n => n.IsKind(SyntaxKind.PublicKeyword))) {
                        c.WriteTo(writer);
                    }
                }
                writer.WriteLine("#endregion");
                writer.WriteLine("}");
                writer.WriteLine("}");
            }
        }
    }
}