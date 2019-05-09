using System.Collections.Generic;
using System.IO;
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
        private readonly List<MethodDeclarationSyntax> _methodMembers = new List<MethodDeclarationSyntax>();

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
                        _methodMembers.Add((MethodDeclarationSyntax)node);
                    }
                    break;
                case SyntaxKind.EnumDeclaration:
                    _classes.Push((BaseTypeDeclarationSyntax)node);
                    _innerClasses.Add((BaseTypeDeclarationSyntax)node);
                    break;
            }
            base.Visit(node);
        }

        public void Scan(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            if (tree.HasCompilationUnitRoot) {
                tree.GetCompilationUnitRoot().Accept(this);
            }
        }
    }

    class Program
    {
        private const string CoreFilePath = @"..\..\..\TALibraryInCSharp\TACore.cs";
        private const string FuncPath = @"..\..\..\TALibraryInCSharp\";
        static void Main(string[] args)
        {
            var coreCode = File.ReadAllText(CoreFilePath);
            var parser = new TaLibCodeParser();
            parser.Scan(coreCode);
        }
    }
}
