using System;
using System.Diagnostics;
using System.IO;

namespace OppaiSharp
{
    internal class Parser
    {
        /// <summary> Last line touched. </summary>
        public string Lastline;

        /// <summary> Last line number touched. </summary>
        public int Nline;

        /// <summary> Last token touched. </summary>
        public string Lastpos;

        /// <summary> True if the parsing completed successfully. </summary>
        public bool Done;

        /// <summary>
        /// The parsed beatmap will be stored in this object.
        /// Will persist throughout Reset() calls and will be reused by
        /// subsequent parse calls until changed.
        /// See <seealso cref="Reset"/>
        /// </summary>
        public Map Beatmap;

        /// <summary> Current section </summary>
        private string section;
        private bool arFound = false;

        public Parser() { Reset(); }

        private void Reset()
        {
            Lastline = Lastpos = section = "";
            Nline = 0;
            Done = false;
            Beatmap?.Reset();
        }

        public override string ToString() => $"in line {Nline}\n{Lastline}\n> {Lastpos}";

        private void Warn(string fmt, params object[] args)
        {
            //TODO: to logger
            Debug.WriteLine("W: " + fmt, args);
            Debug.WriteLine(this);
        }

        /// <summary>
        /// Trims <paramref name="v"/>, sets lastpos to it and returns trimmed <paramref name="v"/>.
        /// Should be used to access any string that can make the parser fail.
        /// </summary>
        private string Setlastpos(string v) => Lastpos = v.Trim();

        private string[] Property()
        {
            string[] split = Lastline.Split(new[] {':'}, 2);
            split[0] = Setlastpos(split[0]);
            if (split.Length > 1)
                split[1] = Setlastpos(split[1]);
            return split;
        }

        private void Metadata()
        {
            string[] p = Property();

            switch (p[0]) {
                case "Title":
                    Beatmap.Title = p[1];
                    break;
                case "TitleUnicode":
                    Beatmap.TitleUnicode = p[1];
                    break;
                case "Artist":
                    Beatmap.Artist = p[1];
                    break;
                case "ArtistUnicode":
                    Beatmap.ArtistUnicode = p[1];
                    break;
                case "Creator":
                    Beatmap.Creator = p[1];
                    break;
                case "Version":
                    Beatmap.Version = p[1];
                    break;
            }
        }

        private void General()
        {
            string[] p = Property();

            switch (p[0]) {
                case "Mode":
                    Beatmap.Mode = (GameMode)int.Parse(Setlastpos(p[1]));

                    if (Beatmap.Mode != GameMode.Standard)
                        throw new InvalidOperationException("this gamemode is not yet supported");
                    break;
            }
        }

        private void Difficulty()
        {
            string[] p = Property();

            switch (p[0]) {
                case "CircleSize":
                    Beatmap.CS = float.Parse(Setlastpos(p[1]));
                    break;
                case "OverallDifficulty":
                    Beatmap.OD = float.Parse(Setlastpos(p[1]));
                    break;
                case "ApproachRate":
                    Beatmap.AR = float.Parse(Setlastpos(p[1]));
                    arFound = true;
                    break;
                case "HPDrainRate":
                    Beatmap.HP = float.Parse(Setlastpos(p[1]));
                    break;
                case "SliderMultiplier":
                    Beatmap.SliderVelocity = float.Parse(Setlastpos(p[1]));
                    break;
                case "SliderTickRate":
                    Beatmap.TickRate = float.Parse(Setlastpos(p[1]));
                    break;
            }
        }

        private void Timing()
        {
            string[] s = Lastline.Split(',');

            if (s.Length > 8)
                Warn("timing point with trailing values");

            var t = new Timing {
                Time = double.Parse(Setlastpos(s[0])),
                MsPerBeat = double.Parse(Setlastpos(s[1]))
            };

            if (s.Length >= 7)
                t.Change = s[6].Trim() != "0";

            Beatmap.TimingPoints.Add(t);
        }

        private void Objects()
        {
            string[] s = Lastline.Split(',');

            if (s.Length > 11)
                Warn("object with trailing values");

            var obj = new HitObject {
                Time = double.Parse(Setlastpos(s[2])),
                Type = (HitObjects)int.Parse(Setlastpos(s[3]))
            };

            switch (obj.Type) {
                case HitObjects.Circle:
                    ++Beatmap.CountCircles;
                    obj.Data = new Circle {
                        Position = new Vector2 {
                            X = double.Parse(Setlastpos(s[0])),
                            Y = double.Parse(Setlastpos(s[1]))
                        }
                    };
                    break;
                case HitObjects.Spinner:
                    Beatmap.CountSpinners++;
                    break;
                case HitObjects.Slider:
                    Beatmap.CountSliders++;
                    obj.Data = new Slider {
                        Position = {
                            X = double.Parse(Setlastpos(s[0])),
                            Y = double.Parse(Setlastpos(s[1]))
                        },
                        Repetitions = int.Parse(Setlastpos(s[6])),
                        Distance = double.Parse(Setlastpos(s[7]))
                    };
                    break;
            }

            Beatmap.Objects.Add(obj);
        }

        /// <summary>
        /// Calls Reset() on beatmap and parses a osu file into it.
        /// If beatmap is null, it will be initialized to a new Map
        /// </summary>
        /// <returns><see cref="Beatmap"/></returns>
        public Map Map(StreamReader reader)
        {
            string line;

            if (Beatmap == null)
                Beatmap = new Map();

            Reset();

            while ((line = reader.ReadLine()) != null) {
                Lastline = line;
                ++Nline;

                //comments (according to lazer)
                if (line.StartsWith(" ") || line.StartsWith("_")) {
                    continue;
                }

                line = Lastline = line.Trim();
                if (line.Length <= 0) {
                    continue;
                }

                //c++ style comments
                if (line.StartsWith("//")) {
                    continue;
                }

                //[SectionName]
                if (line.StartsWith("[")) {
                    section = line.Substring(1, line.Length - 1);
                    continue;
                }

                switch (section) {
                    case "Metadata":
                        Metadata();
                        break;
                    case "General":
                        General();
                        break;
                    case "Difficulty":
                        Difficulty();
                        break;
                    case "TimingPoints":
                        Timing();
                        break;
                    case "HitObjects":
                        Objects();
                        break;
                    default:
                        int fmtIndex = line.IndexOf("file format v", StringComparison.Ordinal);
                        if (fmtIndex < 0)
                            continue;

                        Beatmap.FormatVersion = int.Parse(
                            line.Substring(fmtIndex + "file format v".Length)
                        );
                        break;
                }
            }

            if (!arFound) {
                Beatmap.AR = Beatmap.OD;
            }

            Done = true;
            return Beatmap;
        }

        /// <summary> sets beatmap and returns map(reader) </summary>
        /// <returns><see cref="Beatmap"/></returns>
        public Map Map(StreamReader reader, Map beatmap) {
            Beatmap = beatmap;
            return Map(reader);
        }
    }
}
