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
        public string TitleUnicode { get; internal set; }
        public string Artist { get; internal set; }
        public string ArtistUnicode { get; internal set; }

        /// <summary>Mapper name</summary>
        public string Creator { get; internal set; }

        /// <summary>Difficulty name</summary>
        public string Version { get; internal set; }

        private int _allCountCircles;
        private int _allCountSliders;
        private int _allCountSpinners;
        private int _cutCountCircles;
        private int _cutCountSliders;
        private int _cutCountSpinners;

        public int CountCircles
        {
            get
            {
                if (Cutting)
                    return _cutCountCircles;
                return _allCountCircles;
            }
            set { _allCountCircles = value; }
        }

        public int CountSliders
        {
            get
            {
                if (Cutting)
                    return _cutCountSliders;
                return _allCountSliders;
            }
            set { _allCountSliders = value; }
        }

        public int CountSpinners
        {
            get
            {
                if (Cutting)
                    return _cutCountSpinners;
                return _allCountSpinners;
            }
            set { _allCountSpinners = value; }
        }

        public float HP { get; set; } = 5f;
        public float CS { get; set; } = 5f;
        public float OD { get; set; } = 5f;
        public float AR { get; set; } = 5f;
        public float SliderVelocity { get; set; } = 1f;
        public float TickRate { get; set; } = 1f;
        public bool Cutting { get; private set; } = false;
        private List<HitObject> _allObjects = new List<HitObject>(512);
        private List<HitObject> CutObjects { get; set; }
        public List<HitObject> Objects
        {
            get
            {
                if (Cutting)
                    return CutObjects;
                return _allObjects;
            }
            set { _allObjects = value; }
        }
        private List<Timing> AllTimingPoints { get; set; } = new List<Timing>(32);
        private List<Timing> CutTimingPoints { get; set; } = new List<Timing>(32);

        public List<Timing> TimingPoints
        {
            get
            {
                if (Cutting)
                    return CutTimingPoints;
                return AllTimingPoints;
            }
        }

        public void Cut(int endTime)
        {
            if (endTime <= 0)
                throw new ArgumentException(nameof(endTime));
            Cutting = true;
            CutObjects = new List<HitObject>();
            foreach (var o in _allObjects)
            {
                if (o.Time < endTime)
                    CutObjects.Add(o);
                else//Assuming chronological order
                    break;
            }
            CutTimingPoints = new List<Timing>();
            foreach (var t in AllTimingPoints)
            {
                if (t.Time < endTime)
                    CutTimingPoints.Add(t);
                else//Assuming chronological order
                    break;
            }
            _cutCountCircles = 0;
            _cutCountSpinners = 0;
            _cutCountSliders = 0;
            foreach (var o in CutObjects)
            {
                if (o.Type == HitObjectType.Circle)
                    _cutCountCircles++;
                else if (o.Type == HitObjectType.Slider)
                    _cutCountSliders++;
                else if (o.Type == HitObjectType.Spinner)
                    _cutCountSpinners++;
            }
        }

        public void ResetCut()
        {
            Cutting = false;
        }
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
