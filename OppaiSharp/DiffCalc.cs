using System;
using System.Collections.Generic;

namespace OppaiSharp
{
    internal class DiffCalc
    {
        /** star rating. */
        public double Total;

        /** aim stars. */
        public double Aim;

        /** speed stars. */
        public double Speed;

        /**
        * number of notes that are considered singletaps by the
        * difficulty calculator.
        */
        public int Nsingles;

        /**
        * number of taps slower or equal to the singletap threshold
        * value.
        */
        public int NsinglesThreshold;

        /**
        * the beatmap we want to calculate the difficulty for.
        * must be set or passed to Calc() explicitly.
        * persists across Calc() calls unless it's changed or explicity
        * passed to Calc()
        * @see Koohii.DiffCalc#Calc(Koohii.Map, int, double)
        * @see Koohii.DiffCalc#Calc(Koohii.Map, int)
        * @see Koohii.DiffCalc#Calc(Koohii.Map)
        */
        public Map Beatmap = null;

        private double speedMul;
        private List<Double> strains = new List<Double>(512);

        public DiffCalc() { Reset(); }

        /** sets up the instance for re-use by resetting fields. */
        private void Reset()
        {
            Total = Aim = Speed = 0.0;
            Nsingles = NsinglesThreshold = 0;
            speedMul = 1.0;
        }

        public override String ToString()
        {
            return $"{Total} stars ({Aim} aim, {Speed} speed)";
        }

        private double calc_individual(int type)
        {
            strains.Clear();

            double strainStep = Constants.STRAIN_STEP * speedMul;
            double intervalEnd = strainStep;
            double maxStrain = 0.0;

            /* calculate all strains */
            for (int i = 0; i < Beatmap.Objects.Count; ++i)
            {
                HitObject obj = Beatmap.Objects[i];
                HitObject prev = i > 0 
                    ? Beatmap.Objects[i - 1] 
                    : null;

                if (prev != null)
                {
                    Helpers.DStrain(type, obj, prev, speedMul);
                }

                while (obj.Time > intervalEnd)
                {
                    /* add max strain for this interval */
                    strains.Add(maxStrain);

                    if (prev != null)
                    {
                        /* decay last object's strains until the next
                        interval and use that as the initial max
                        strain */
                        double decay = Math.Pow(Constants.DECAY_BASE[type],
                            (intervalEnd - prev.Time) / 1000.0);

                        maxStrain = prev.Strains[type] * decay;
                    }
                    else
                    {
                        maxStrain = 0.0;
                    }

                    intervalEnd += strainStep;
                }

                maxStrain = Math.Max(maxStrain, obj.Strains[type]);
            }

            /* weigh the top strains sorted from highest to lowest */
            double weight = 1.0;
            double difficulty = 0.0;

            strains.Sort();
            strains.Reverse();

            foreach (Double strain in strains)
            {
                difficulty += strain * weight;
                weight *= Constants.DECAY_WEIGHT;
            }

            return difficulty;
        }

        /**
        * default value for singletap_threshold.
        * @see DiffCalc#Calc
        */
        public const double DefaultSingletapThreshold = 125.0;

        /**
        * calculates beatmap difficulty and stores it in total, aim,
        * speed, nsingles, nsingles_speed fields.
        * @param singletap_threshold the smallest milliseconds interval
        *        that will be considered singletappable. for example,
        *        125ms is 240 1/2 singletaps ((60000 / 240) / 2)
        * @return self
        */
        public DiffCalc Calc(Mods mods, double singletapThreshold)
        {
            Reset();

            MapStats mapstats = new MapStats();
            mapstats.cs = Beatmap.Cs;
            Helpers.ModsApply(mods, mapstats, ModApplyFlags.ApplyCs);
            speedMul = mapstats.speed;

            double radius = (Constants.PLAYFIELD_WIDTH / 16.0) *
                (1.0 - 0.7 * (mapstats.cs - 5.0) / 5.0);

            /* positions are normalized on circle radius so that we can
            Calc as if everything was the same circlesize */
            double scalingFactor = 52.0 / radius;

            if (radius < Constants.CIRCLESIZE_BUFF_THRESHOLD)
            {
                scalingFactor *= 1.0 +
                    Math.Min(Constants.CIRCLESIZE_BUFF_THRESHOLD - radius, 5.0)
                    / 50.0;
            }

            Vector2 normalizedCenter = new Vector2(Constants.PLAYFIELD_CENTER) * scalingFactor;

            /* calculate normalized positions */
            foreach (HitObject obj in Beatmap.Objects)
            {
                if ((obj.Type & HitObjects.Spinner) != 0)
                {
                    obj.Normpos = new Vector2(normalizedCenter);
                }

                else
                {
                    Vector2 pos;

                    switch (obj.Type) {
                        case HitObjects.Slider:
                            pos = ((Slider)obj.Data).Position;
                            break;
                        case HitObjects.Circle:
                            pos = ((Circle)obj.Data).Position;
                            break;
                        default:
                            //TODO: warn $"W: unknown object type {obj.Type:X8}\n"
                            pos = new Vector2();
                            break;
                    }

                    obj.Normpos = new Vector2(pos) * scalingFactor;
                }
            }

            /* speed and aim stars */
            Speed = calc_individual(Constants.DiffSpeed);
            Aim = calc_individual(Constants.DiffAim);

            Speed = Math.Sqrt(Speed) * Constants.STAR_SCALING_FACTOR;
            Aim = Math.Sqrt(Aim) * Constants.STAR_SCALING_FACTOR;
            if ((mods & Mods.TouchDevice) != 0)
            {
                Aim = Math.Pow(Aim, 0.8);
            }

            /* total stars */
            Total = Aim + Speed +
                Math.Abs(Speed - Aim) * Constants.EXTREME_SCALING_FACTOR;

            /* singletap stats */
            for (int i = 1; i < Beatmap.Objects.Count; ++i)
            {
                HitObject prev = Beatmap.Objects[i - 1];
                HitObject obj = Beatmap.Objects[i];

                if (obj.IsSingle)
                    ++Nsingles;

                if ((obj.Type & (HitObjects.Circle | HitObjects.Slider)) == 0)
                    continue;

                double interval = (obj.Time - prev.Time) / speedMul;

                if (interval >= singletapThreshold)
                {
                    ++NsinglesThreshold;
                }
            }

            return this;
        }

        /**
        * @return Calc(mods, DEFAULT_SINGLETAP_THRESHOLD)
        * @see DiffCalc#Calc(int, double)
        * @see DiffCalc#DEFAULT_SINGLETAP_THRESHOLD
        */
        public DiffCalc Calc(Mods mods)
        {
            return Calc(mods, DefaultSingletapThreshold);
        }

        /**
        * @return Calc(MODS_NOMOD, DEFAULT_SINGLETAP_THRESHOLD)
        * @see DiffCalc#Calc(int, double)
        * @see DiffCalc#DEFAULT_SINGLETAP_THRESHOLD
        */
        public DiffCalc Calc()
        {
            return Calc(Mods.NoMod, DefaultSingletapThreshold);
        }

        /**
        * sets beatmap field and calls
        * Calc(mods, singletap_threshold).
        * @see DiffCalc#Calc(int, double)
        */
        public DiffCalc Calc(Map beatmap, Mods mods, double singletapThreshold)
        {
            this.Beatmap = beatmap;
            return Calc(mods, singletapThreshold);
        }

        /**
        * sets beatmap field and calls
        * Calc(mods, DEFAULT_SINGLETAP_THRESHOLD).
        * @see DiffCalc#Calc(int, double)
        * @see DiffCalc#DEFAULT_SINGLETAP_THRESHOLD
        */
        public DiffCalc Calc(Map beatmap, Mods mods)
        {
            return Calc(beatmap, mods, DefaultSingletapThreshold);
        }

        /**
        * sets beatmap field and calls
        * Calc(MODS_NOMOD, DEFAULT_SINGLETAP_THRESHOLD).
        * @see DiffCalc#Calc(int, double)
        * @see DiffCalc#DEFAULT_SINGLETAP_THRESHOLD
        */
        public DiffCalc Calc(Map beatmap)
        {
            return Calc(beatmap, Mods.NoMod,
                DefaultSingletapThreshold);
        }
    }
}
