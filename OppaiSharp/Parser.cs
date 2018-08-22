using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace OppaiSharp
{
    internal static class Parser
    {
        /// <summary>
        /// Reads a beatmap from a file.
        /// </summary>
        /// <returns><see cref="Beatmap"/></returns>
        public static Beatmap Read(StreamReader reader)
        {
            var bm = new Beatmap();

            string line, section = null;
            while ((line = reader.ReadLine()?.Trim()) != null) {
                //any comments
                if (line.StartsWith("_") || line.StartsWith("//"))
                    continue;
                
                whileLoopStart:
                //don't continue here, the read methods will start reading at the next line
                if (line.StartsWith("["))
                    section = line.Substring(1, line.Length - 2);

                if (line.Length <= 0)
                    continue;

                switch (section) {
                    case "Metadata":
                        foreach (var s in ReadSectionPairs(reader, out line)) {
                            var val = s.Value;
                            switch (s.Key) {
                                case "Title":
                                    bm.Title = val;
                                    break;
                                case "TitleUnicode":
                                    bm.TitleUnicode = val;
                                    break;
                                case "Artist":
                                    bm.Artist = val;
                                    break;
                                case "ArtistUnicode":
                                    bm.ArtistUnicode = val;
                                    break;
                                case "Creator":
                                    bm.Creator = val;
                                    break;
                                case "Version":
                                    bm.Version = val;
                                    break;
                            }
                        }
                        break;
                    case "General":
                        foreach (var pair in ReadSectionPairs(reader, out line))
                            if (pair.Key == "Mode")
                                bm.Mode = (GameMode)int.Parse(pair.Value);
                        break;
                    case "Difficulty":
                        bool arFound = false;
                        foreach (var s in ReadSectionPairs(reader, out line)) {
                            var val = s.Value;
                            switch (s.Key) {
                                case "CircleSize":
                                    bm.CS = float.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "OverallDifficulty":
                                    bm.OD = float.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "ApproachRate":
                                    bm.AR = float.Parse(val, CultureInfo.InvariantCulture);
                                    arFound = true;
                                    break;
                                case "HPDrainRate":
                                    bm.HP = float.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "SliderMultiplier":
                                    bm.SliderVelocity = float.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                                case "SliderTickRate":
                                    bm.TickRate = float.Parse(val, CultureInfo.InvariantCulture);
                                    break;
                            }
                        }
                        if (!arFound)
                            bm.AR = bm.OD;
                        break;
                    case "TimingPoints":
                        foreach (var ptLine in ReadSectionLines(reader, out line)) {
                            string[] splitted = ptLine.Split(',');

                            if (splitted.Length > 8)
                                Warn("timing point with trailing values");
                            else if (splitted.Length < 2) {
                                Warn("timing point with too little values");
                                continue;
                            }


                            var t = new Timing {
                                Time = double.Parse(splitted[0], CultureInfo.InvariantCulture),
                                MsPerBeat = double.Parse(splitted[1], CultureInfo.InvariantCulture)
                            };

                            if (splitted.Length >= 7)
                                t.Change = splitted[6].Trim() != "0";

                            bm.TimingPoints.Add(t);
                        }
                        break;
                    case "HitObjects":
                        foreach (var objLine in ReadSectionLines(reader, out line)) {
                            string[] s = objLine.Split(',');

                            if (s.Length > 11)
                                Warn("object with trailing values");
                            else if (s.Length < 5) {
                                Warn("object with too little values");
                                continue;
                            }

                            var obj = new HitObject {
                                Time = double.Parse(s[2], CultureInfo.InvariantCulture),
                                Type = (HitObjectType)int.Parse(s[3])
                            };

                            if ((obj.Type & HitObjectType.Circle) != 0)
                            {
                                bm.CountCircles++;
                                obj.Data = new Circle {
                                    Position = new Vector2 {
                                        X = double.Parse(s[0], CultureInfo.InvariantCulture),
                                        Y = double.Parse(s[1], CultureInfo.InvariantCulture)
                                    }
                                };
                            }
                            if ((obj.Type & HitObjectType.Spinner) != 0)
                            {
                                bm.CountSpinners++;
                            }
                            if ((obj.Type & HitObjectType.Slider) != 0)
                            {
                                bm.CountSliders++;
                                obj.Data = new Slider {
                                    Position = {
                                        X = double.Parse(s[0], CultureInfo.InvariantCulture),
                                        Y = double.Parse(s[1], CultureInfo.InvariantCulture)
                                    },
                                    Repetitions = int.Parse(s[6]),
                                    Distance = double.Parse(s[7], CultureInfo.InvariantCulture)
                                };
                            }

                            bm.Objects.Add(obj);
                        }
                        break;
                    default:
                        int fmtIndex = line.IndexOf("file format v", StringComparison.Ordinal);
                        if (fmtIndex < 0)
                            continue;

                        bm.FormatVersion = int.Parse(line.Substring(fmtIndex + "file format v".Length));
                        break;
                }

                //in hand-edited beatmap, it's possible that the section header doesn't come after a newline
                //if that is the case, this check stops us from skipping this line
                if (line?.StartsWith("[") == true)
                    goto whileLoopStart;
            }
            return bm;
        }

        private static Dictionary<string, string> ReadSectionPairs(StreamReader sr, out string line)
        {
            var dic = new Dictionary<string, string>();

            while (!string.IsNullOrEmpty(line = sr.ReadLine().Trim()) && !line.StartsWith("["))
            {
                int i = line.IndexOf(':');

                if (i == -1)
#if DEBUG
                    throw new Exception("Invalid key/value line: " + line);
#else
                    continue;
#endif

                string key = line.Substring(0, i);
                string value = line.Substring(i + 1);

                dic.Add(key.TrimEnd(), value.TrimStart());
            }

            return dic;
        }

        private static List<string> ReadSectionLines(StreamReader sr, out string line)
        {
            var list = new List<string>();

            while (!string.IsNullOrEmpty(line = sr.ReadLine()?.Trim()) && !line.StartsWith("["))
                list.Add(line);

            return list;
        }

        [Conditional("DEBUG")]
        private static void Warn(string fmt, params object[] args)
        {
            Debug.WriteLine(string.Format("W: " + fmt, args));
        }
    }
}
