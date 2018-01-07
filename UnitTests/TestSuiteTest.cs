using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OppaiSharp;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Common.Tar;
using SharpCompress.Readers;
using SharpCompress.Readers.Tar;

namespace UnitTests
{
    [TestClass]
    public class TestSuiteTest
    {
        private const string SuitePath = "test_suite_20170811.tar.xz";
        private const string SuiteUrl = "http://www.hnng.moe/stuff/" + SuitePath;
        private const string SuiteExpectedPath = "TestSuite.txt";

        private readonly List<Tuple<uint, ExpectedOutcome>> testCases = new List<Tuple<uint, ExpectedOutcome>>();

        [TestInitialize]
        public void Prepare()
        {
            //make sure that the results are downloaded
            if (!File.Exists(SuitePath))
                new WebClient().DownloadFile(SuiteUrl, SuitePath);

            //require the suite results to be here
            if (!File.Exists(SuiteExpectedPath))
                Assert.Inconclusive(SuiteExpectedPath + " not found!");

            //load expected results from file
            testCases.Clear();
            using (var stream = File.OpenRead(SuiteExpectedPath))
            using (var sr = new StreamReader(stream)) {
                while (!sr.EndOfStream) {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;

                    var c = new ExpectedOutcome(line, out uint id);
                    testCases.Add(new Tuple<uint, ExpectedOutcome>(id, c));
                }
            }
        }

        [TestMethod]
        public void TestEntireSuite()
        {
            using (var stream = new FileStream(SuitePath, FileMode.Open))
            using (var reader = ReaderFactory.Open(stream)) {
                while (reader.MoveToNextEntry()) {
                    //only check actual files
                    if (reader.Entry.IsDirectory || !(reader.Entry is TarEntry t)) continue;

                    string fileName = t.Key;  //eg: "test_suite/737100.osu"
                    uint id = uint.Parse(fileName.Split('/').Last().Split('.').First());

                    Beatmap bm;
                    using (var ms = new MemoryStream())
                    using (var str = new StreamReader(ms)) {
                        reader.WriteEntryTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        bm = new Parser().Map(str);

                        foreach (var testcase in testCases.Where(a => a.Item1 == id).Select(a => a.Item2))
                        {
                            var expected = testcase.PP;
                            var actual = CheckCase(bm, testcase, out double margin);
                            Console.WriteLine($"Testing {expected:F2}pp vs {actual:F2}pp");
                            Assert.AreEqual(expected, actual, margin);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestSinglePlay()
        {
            byte[] data = new WebClient().DownloadData("https://osu.ppy.sh/osu/774965");

            var stream = new MemoryStream(data, false);
            var reader = new StreamReader(stream);
            //read a beatmap
            var beatmap = new Parser().Map(reader);

            //calculate star ratings for HDDT
            Mods mods = Mods.Hidden | Mods.DoubleTime;
            var stars = new DiffCalc().Calc(beatmap, mods);
            Console.WriteLine($"Star rating: {stars.Total:F2} (aim stars: {stars.Aim:F2}, speed stars: {stars.Speed:F2})");

            //calculate the PP for this map
            var pp = new PPv2(new PPv2Parameters {
                Beatmap = beatmap,
                AimStars = stars.Aim,
                SpeedStars = stars.Speed,
                Mods = mods,
                Count100 = 8,
                Count50 = 0,
                CountMiss = 0,
            });
            Console.WriteLine($"Play is worth {pp.Total:F2}pp ({pp.Aim:F2} aim pp, {pp.Acc:F2} acc pp, {pp.Speed:F2} " +
                              $"speed pp) and has an accuracy of {pp.ComputedAccuracy.Value() * 100:F2}");

            Assert.AreEqual(817.0, pp.Total, 0.5);
        }

        private static double CheckCase(Beatmap bm, ExpectedOutcome outcome, out double margin)
        {
            const double errorMargin = 0.02;

            margin = errorMargin * outcome.PP;

            var diff = new DiffCalc().Calc(bm, outcome.Mods);
            var pp = new PPv2(outcome.ToParameters(bm, diff));

            if (outcome.PP < 100)
                margin *= 3;
            else if (outcome.PP < 200)
                margin *= 2;
            else if (outcome.PP < 300)
                margin *= 1.5;

            return pp.Total;
        }

        private struct ExpectedOutcome
        {
            public readonly double PP;
            public readonly Mods Mods;

            private readonly ushort maxCombo;
            private readonly ushort count300, count100, count50, countMiss;

            public ExpectedOutcome(string line, out uint id)
            {
                line = line.Trim(' ', ',', '{', '}');
                string[] s = line.Split(',');

                if (s.Length != 8) Assert.Inconclusive("Invalid test case");
                
                id = uint.Parse(s[0]);
                maxCombo = ushort.Parse(s[1]);
                count300 = ushort.Parse(s[2]);
                count100 = ushort.Parse(s[3]);
                count50 =  ushort.Parse(s[4]);
                countMiss =ushort.Parse(s[5]);

                PP = double.Parse(s[7], CultureInfo.InvariantCulture);

                string modString = s[6].Trim(' ').Replace(" | ", string.Empty).ToUpper();
                Mods = Helpers.StringToMods(modString);
                    
            }

            public PPv2Parameters ToParameters(Beatmap bm, DiffCalc d)
            {
                return new PPv2Parameters {
                    Beatmap = bm,
                    AimStars = d.Aim,
                    SpeedStars = d.Speed,
                    Mods = Mods,
                    Count300 = count300,
                    Count100 = count100,
                    Count50 = count50,
                    CountMiss = countMiss,
                    Combo = maxCombo
                };
            }
        }
    }
}
