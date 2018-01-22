using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OppaiSharp
{
    public class Beatmap
    {
        public int FormatVersion { get; internal set; }
        public GameMode Mode { get; internal set; }
        public string Title { get; internal set; }
        public string TitleUnicode  { get; internal set; }
        public string Artist { get; internal set; }
        public string ArtistUnicode { get; internal set; }

        /// <summary>Mapper name</summary>
        public string Creator { get; internal set; }

        /// <summary>Difficulty name</summary>
        public string Version { get; internal set; }

        public int CountCircles { get; set; }
        public int CountSliders { get; set; }
        public int CountSpinners { get; set; }
        public float HP { get; set; } = 5f;
        public float CS { get; set; } = 5f;
        public float OD { get; set; } = 5f;
        public float AR { get; set; } = 5f;
        public float SliderVelocity { get; set; } = 1f;
        public float TickRate { get; set; } = 1f;

        public List<HitObject> Objects { get; } = new List<HitObject>(512);
        public List<Timing> TimingPoints { get; } = new List<Timing>(32);

        public static Beatmap Read(StreamReader reader) => Parser.Read(reader);

        internal Beatmap() { }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (HitObject obj in Objects) {
                sb.Append(obj);
                sb.Append(", ");
            }

            string objects = sb.ToString();

#if NET20
            sb = new StringBuilder();
#else
            sb.Clear();
#endif

            foreach (Timing t in TimingPoints) {
                sb.Append(t);
                sb.Append(", ");
            }

            string timingPoints = sb.ToString();

            return $"beatmap {{ mode={Mode}, title={Title}, title_unicode={TitleUnicode}, " 
                    + $"artist={Artist}, artist_unicode={ArtistUnicode}, creator={Creator}, " 
                    + $"version={Version}, ncircles={CountCircles}, nsliders={CountSliders}, nspinners={CountSpinners}," 
                    + $" hp={HP}, cs={CS}, od={OD}, ar={AR}, sv={SliderVelocity}, tick_rate={TickRate}, " 
                    + $"tpoints=[ {timingPoints} ], objects=[ {objects} ] }}";
        }

        public int GetMaxCombo()
        {
            int res = 0;
            int tIndex = -1;
            double tNext = double.NegativeInfinity;
            double pxPerBeat = 0.0;

            foreach (HitObject obj in Objects)
            {
                if ((obj.Type & HitObjectType.Slider) == 0) {
                    //non-sliders add 1 combo
                    res++;
                    continue;
                }

                //keep track of the current timing point without
                //looping through all of them for every object
                while (obj.Time >= tNext) {
                    tIndex++;

                    tNext = TimingPoints.Count > tIndex + 1 
                        ? TimingPoints[tIndex + 1].Time 
                        : double.PositiveInfinity;

                    Timing t = TimingPoints[tIndex];

                    double svMultiplier = 1.0;

                    if (!t.Change && t.MsPerBeat < 0)
                        svMultiplier = -100.0 / t.MsPerBeat;

                    pxPerBeat = SliderVelocity * 100.0 * svMultiplier;
                    if (FormatVersion < 8)
                        pxPerBeat /= svMultiplier;
                }

                //slider, we need to calculate slider ticks
                var sl = (Slider)obj.Data;

                double numBeats = sl.Distance * sl.Repetitions / pxPerBeat;

                int ticks = (int)Math.Ceiling((numBeats - 0.1) / sl.Repetitions * TickRate);

                ticks--;
                ticks *= sl.Repetitions;
                ticks += sl.Repetitions + 1;

                res += Math.Max(0, ticks);
            }

            return res;
        }
    }
}
