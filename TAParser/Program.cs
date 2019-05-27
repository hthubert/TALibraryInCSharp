using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SearchOption = System.IO.SearchOption;

namespace TAParser
{
    class Program
    {
        private const string CoreFilePath = @"..\..\..\TALibraryInCSharp\TACore.cs";
        private const string Ta4OqFilePath = @"..\..\..\TALibraryInCSharp\TA4OpenQuant.cs";
        private const string FuncPath = @"..\..\..\TALibraryInCSharp\TAFunc";

        static void Main(string[] args)
        {
            //CreateTa4OpenQuant();
            //Ta4OqTest();
            //CreateTaIndicator();
            RewriteInternalCall();
        }

        private static void RewriteInternalCall()
        {
            var coreCode = File.ReadAllText(Ta4OqFilePath);
            var parser = new TaLibCodeParser();
            parser.Scan(coreCode);
            var rewriter = new Ta4OpenQuantRewriter();
            var list = new List<MethodDeclarationSyntax>();
            foreach (var member in parser.MethodMembers) {
                list.Add(rewriter.RewriteInternalCall(member));
            }
            parser.MethodMembers.Clear();
            parser.MethodMembers.AddRange(list);
            parser.Save(Ta4OqFilePath);
        }

        private static void CreateTaIndicator()
        {
            var fs = File.Open("ta_func_api.xml", FileMode.Open);
            //using (var sr = new StreamReader(fs, Encoding.UTF8)) {
            //    var xz = new XmlSerializer(typeof(FinancialFunctions));
            //    var funcs = (FinancialFunctions)xz.Deserialize(sr);
            //}
        }

        private static void CreateTa4OpenQuant()
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
