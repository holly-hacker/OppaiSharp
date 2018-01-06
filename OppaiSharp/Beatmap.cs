using System;
using System.Collections.Generic;
using System.Text;

namespace OppaiSharp
{
    public class Map
    {
        public int FormatVersion;
        public int Mode;
        public string Title, TitleUnicode;
        public string Artist, ArtistUnicode;

        /// <summary> mapper name </summary>
        public string Creator;

        /// <summary> difficulty name </summary>
        public string Version;

        public int Ncircles, Nsliders, Nspinners;
        public float Hp, Cs, Od, Ar;
        public float Sv, TickRate;

        public List<HitObject> Objects = new List<HitObject>(512);

        public List<Timing> Tpoints = new List<Timing>(32);

        public Map() { Reset(); }

        /// <summary> clears the instance so that it can be reused </summary>
        public void Reset()
        {
            Title = TitleUnicode =
            Artist = ArtistUnicode =
            Creator = Version = "";

            Ncircles = Nsliders = Nspinners = 0;
            Hp = Cs = Od = Ar = 5.0f;
            Sv = TickRate = 1.0f;

            Objects.Clear();
            Tpoints.Clear();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (HitObject obj in Objects) {
                sb.Append(obj);
                sb.Append(", ");
            }

            string objsStr = sb.ToString();

            sb.Length = 0;

            foreach (Timing t in Tpoints) {
                sb.Append(t);
                sb.Append(", ");
            }

            string timingStr = sb.ToString();

            return $"beatmap {{ mode={Mode}, title={Title}, title_unicode={TitleUnicode}, " 
                    + $"artist={Artist}, artist_unicode={ArtistUnicode}, creator={Creator}, " 
                    + $"version={Version}, ncircles={Ncircles}, nsliders={Nsliders}, nspinners={Nspinners}," 
                    + $" hp={Hp}, cs={Cs}, od={Od}, ar={Ar}, sv={Sv}, tick_rate={TickRate}, " 
                    + $"tpoints=[ {timingStr} ], objects=[ {objsStr} ] }}";
        }

        public int MaxCombo()
        {
            int res = 0;
            int tindex = -1;
            double tnext = double.NegativeInfinity;
            double pxPerBeat = 0.0;

            foreach (HitObject obj in Objects)
            {
                if ((obj.Type & HitObjects.Slider) == 0)
                {
                    //non-sliders add 1 combo
                    ++res;
                    continue;
                }

                //keep track of the current timing point without
                //looping through all of them for every object
                while (obj.Time >= tnext) {
                    ++tindex;

                    tnext = Tpoints.Count > tindex + 1 
                        ? Tpoints[tindex + 1].Time 
                        : double.PositiveInfinity;

                    Timing t = Tpoints[tindex];

                    double svMultiplier = 1.0;

                    if (!t.Change && t.MsPerBeat < 0)
                        svMultiplier = -100.0 / t.MsPerBeat;

                    pxPerBeat = Sv * 100.0 * svMultiplier;
                    if (FormatVersion < 8)
                        pxPerBeat /= svMultiplier;
                }

                //slider, we need to calculate slider ticks
                var sl = (Slider)obj.Data;

                double numBeats = sl.Distance * sl.Repetitions / pxPerBeat;

                int ticks = (int)Math.Ceiling((numBeats - 0.1) / sl.Repetitions * TickRate);

                --ticks;
                ticks *= sl.Repetitions;
                ticks += sl.Repetitions + 1;

                res += Math.Max(0, ticks);
            }

            return res;
        }
    }
}
