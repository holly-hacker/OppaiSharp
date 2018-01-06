using System;
using System.Collections.Generic;
using System.Text;

namespace OppaiSharp
{
    public class Map
    {
        public int FormatVersion;
        public GameMode Mode;
        public string Title, TitleUnicode;
        public string Artist, ArtistUnicode;

        /// <summary>Mapper name</summary>
        public string Creator;

        /// <summary>Difficulty name</summary>
        public string Version;

        public int CountCircles, CountSliders, CountSpinners;
        public float HP, CS, OD, AR;
        public float SliderVelocity, TickRate;

        public List<HitObject> Objects = new List<HitObject>(512);

        public List<Timing> TimingPoints = new List<Timing>(32);

        public Map() { Reset(); }

        /// <summary>Clears the instance so that it can be reused</summary>
        public void Reset()
        {
            Title = TitleUnicode = Artist = ArtistUnicode = Creator = Version = "";

            CountCircles = CountSliders = CountSpinners = 0;
            HP = CS = OD = AR = 5.0f;
            SliderVelocity = TickRate = 1.0f;

            Objects.Clear();
            TimingPoints.Clear();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (HitObject obj in Objects) {
                sb.Append(obj);
                sb.Append(", ");
            }

            string objects = sb.ToString();

            sb.Clear();

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

        public int MaxCombo()
        {
            int res = 0;
            int tIndex = -1;
            double tNext = double.NegativeInfinity;
            double pxPerBeat = 0.0;

            foreach (HitObject obj in Objects)
            {
                if ((obj.Type & HitObjects.Slider) == 0) {
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
