using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SmartQuant;
using TaLib;
using SearchOption = System.IO.SearchOption;

namespace TAParser
{
    internal class TaAroon : SmartQuant.Indicator
    {
        private static readonly double[] InHigh = { 0 };
        private static readonly double[] InLow = { 0 };

        public int TimePeriod { get; }
        public SmartQuant.TimeSeries AroonUp { get; }

        public TaAroon(SmartQuant.ISeries input, int timePeriod) : base(input)
        {
            AroonUp = new SmartQuant.TimeSeries();
            TimePeriod = timePeriod;
            Init();
        }

        protected override void Init()
        {
            name = $"TA_Lib Aroon ({TimePeriod})";
            description = "TA_Lib Aroon";
            Clear();
            calculate = true;
        }

        public override void Calculate(int index)
        {
            if (index >= TimePeriod) {
                var outBegIdx = 0;
                var outNBElement = 0;
                var down = new double[1];
                var up = new double[1];
                var ret = TaLib.Core.Aroon(index, index, InHigh, InLow, TimePeriod,
                    ref outBegIdx, ref outNBElement, down, up, input);
                if (ret == TaLib.Core.RetCode.Success) {
                    var datatime = input.GetDateTime(index);
                    Add(datatime, down[0]);
                    AroonUp.Add(datatime, up[0]);
                }
            }
        }
    }

    class Program
    {
        private const string CoreFilePath = @"..\..\..\TALibraryInCSharp\TACore.cs";
        private const string Ta4OqFilePath = @"..\..\..\TALibraryInCSharp\TA4OpenQuant.cs";
        private const string FuncPath = @"..\..\..\TALibraryInCSharp\TAFunc";

        static Program()
        {
            SmartQuant.OpenQuantOutside.Init();
        }

        static void Main(string[] args)
        {
            //CreateTa4OpenQuant();
            //Ta4OqTest();
            CreateTaIndicator();
        }

        private static void CreateTaIndicator()
        {
            var fs = File.Open("ta_func_api.xml", FileMode.Open);
            using (var sr = new StreamReader(fs, Encoding.UTF8)) {
                var xz = new XmlSerializer(typeof(FinancialFunctions));
                var funcs = (FinancialFunctions)xz.Deserialize(sr);
            }
        }

        private static void Ta4OqTest()
        {
            var framework = Framework.Current;
            var inst = framework.InstrumentManager.Get("rb88");
            var bars = framework.DataManager.GetHistoricalBars(inst, BarType.Time, 60);
            var series = new BarSeries();
            var aroon = new TaAroon(series, 12);
            foreach (var bar in bars) {
                series.Add(bar);
            }
            //var bar
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
