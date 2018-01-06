using System;
using System.Text;

namespace OppaiSharp
{
    public static class Helpers
    {
        /// <summary> returns a string representation of the mods, such as HDDT </summary>
        /// <returns> a string representation of the mods, such as HDDT </returns>
        public static string ModsToString(Mods mods)
        {
            var sb = new StringBuilder();

            if ((mods & Mods.NoFail) != 0)
                sb.Append("NF");

            if ((mods & Mods.Easy) != 0)
                sb.Append("EZ");

            if ((mods & Mods.TouchDevice) != 0)
                sb.Append("TD");

            if ((mods & Mods.Hidden) != 0)
                sb.Append("HD");

            if ((mods & Mods.Hardrock) != 0)
                sb.Append("HR");

            if ((mods & Mods.Nightcore) != 0)
                sb.Append("NC");
            else if ((mods & Mods.DoubleTime) != 0)
                sb.Append("DT");

            if ((mods & Mods.HalfTime) != 0)
                sb.Append("HT");

            if ((mods & Mods.Flashlight) != 0)
                sb.Append("FL");

            if ((mods & Mods.SpunOut) != 0)
                sb.Append("SO");

            return sb.ToString();
        }

        /// <summary> returns mod bitmask from the string representation </summary>
        /// <returns> mod bitmask from the string representation </returns>
        public static Mods StringToMods(string str)
        {
            var mask = Mods.NoMod;

            while (str.Length > 0) {
                if (str.StartsWith("NF")) mask |= Mods.NoFail;
                else if (str.StartsWith("EZ")) mask |= Mods.Easy;
                else if (str.StartsWith("TD")) mask |= Mods.TouchDevice;
                else if (str.StartsWith("HD")) mask |= Mods.Hidden;
                else if (str.StartsWith("HR")) mask |= Mods.Hardrock;
                else if (str.StartsWith("DT")) mask |= Mods.DoubleTime;
                else if (str.StartsWith("HT")) mask |= Mods.HalfTime;
                else if (str.StartsWith("NC")) mask |= Mods.Nightcore;
                else if (str.StartsWith("FL")) mask |= Mods.Flashlight;
                else if (str.StartsWith("SO")) mask |= Mods.SpunOut;
                else {
                    str = str.Substring(1);
                    continue;
                }
                str = str.Substring(2);
            }

            return mask;
        }

        /// <summary>
        /// applies mods to mapstats.
        ///
        /// <p><blockquote><pre>
        /// var mapstats = new MapStats();
        /// mapstats.AR = 9;
        /// Koohii.mods_apply(Mods.DoubleTime, mapstats, ModApplyFlags.ApplyAR);
        /// // mapstats.AR is now 10.33, mapstats.speed is 1.5
        /// </pre></blockquote></p>
        /// </summary>
        /// <param name="mods"></param>
        /// <param name="mapstats">the base beatmap stats</param>
        /// <param name="flags">bitmask that specifies which stats to modify. only the stats specified here need to be initialized in mapstats.</param>
        /// <returns>mapstats</returns>
        public static MapStats ModsApply(Mods mods, MapStats mapstats, ModApplyFlags flags)
        {
            mapstats.speed = 1.0f;

            if ((mods & Mods.MapChanging) == 0)
                return mapstats;

            if ((mods & (Mods.DoubleTime | Mods.Nightcore)) != 0)
                mapstats.speed = 1.5f;

            if ((mods & Mods.HalfTime) != 0)
                mapstats.speed *= 0.75f;

            float odArHpMultiplier = 1.0f;

            if ((mods & Mods.Hardrock) != 0)
                odArHpMultiplier = 1.4f;

            if ((mods & Mods.Easy) != 0)
                odArHpMultiplier *= 0.5f;

            if ((flags & ModApplyFlags.ApplyAr) != 0)
            {
                mapstats.ar *= odArHpMultiplier;

                //convert AR into milliseconds window
                double arms = mapstats.ar < 5.0f ?
                    Constants.AR0_MS - Constants.AR_MS_STEP1 * mapstats.ar
                    : Constants.AR5_MS - Constants.AR_MS_STEP2 * (mapstats.ar - 5.0f);

                //stats must be capped to 0-10 before HT/DT which brings
                //them to a range of -4.42->11.08 for OD and -5->11 for AR
                arms = Math.Min(Constants.AR0_MS, Math.Max(Constants.AR10_MS, arms));
                arms /= mapstats.speed;

                mapstats.ar = (float)(
                    arms > Constants.AR5_MS 
                    ? (Constants.AR0_MS - arms) / Constants.AR_MS_STEP1
                    : 5.0 + (Constants.AR5_MS - arms) / Constants.AR_MS_STEP2
                );
            }

            if ((flags & ModApplyFlags.ApplyOd) != 0) {
                mapstats.od *= odArHpMultiplier;
                double odms = Constants.OD0_MS - Math.Ceiling(Constants.OD_MS_STEP * mapstats.od);
                odms = Math.Min(Constants.OD0_MS, Math.Max(Constants.OD10_MS, odms));
                odms /= mapstats.speed;
                mapstats.od = (float)((Constants.OD0_MS - odms) / Constants.OD_MS_STEP);
            }

            if ((flags & ModApplyFlags.ApplyCs) != 0) {
                if ((mods & Mods.Hardrock) != 0)
                    mapstats.cs *= 1.3f;

                if ((mods & Mods.Easy) != 0)
                    mapstats.cs *= 0.5f;

                mapstats.cs = Math.Min(10.0f, mapstats.cs);
            }

            if ((flags & ModApplyFlags.ApplyHp) != 0)
                mapstats.hp = Math.Min(10.0f, mapstats.hp * odArHpMultiplier);

            return mapstats;
        }

        //TODO: move this and below to diffcalc
        internal static double DSpacingWeight(int type, double distance)
        {
            switch (type)
            {
                case Constants.DiffAim:
                    return Math.Pow(distance, 0.99);

                case Constants.DiffSpeed:
                    if (distance > Constants.SINGLE_SPACING)
                        return 2.5;
                    else if (distance > Constants.STREAM_SPACING)
                        return 1.6 + 0.9 * (distance - Constants.STREAM_SPACING) / (Constants.SINGLE_SPACING - Constants.STREAM_SPACING);
                    else if (distance > Constants.ALMOST_DIAMETER)
                        return 1.2 + 0.4 * (distance - Constants.ALMOST_DIAMETER) / (Constants.STREAM_SPACING - Constants.ALMOST_DIAMETER);
                    else if (distance > Constants.ALMOST_DIAMETER / 2.0)
                        return 0.95 + 0.25 * (distance - Constants.ALMOST_DIAMETER / 2.0) / (Constants.ALMOST_DIAMETER / 2.0);

                    return 0.95;
            }

            throw new InvalidOperationException("this difficulty type does not exist");
        }

        /**
        * calculates the strain for one difficulty type and stores it in
        * obj. this assumes that normpos is already computed.
        * this also sets is_single if type is DIFF_SPEED
        */
        internal static void DStrain(int type, HitObject obj, HitObject prev, double speedMul)
        {
            double value = 0.0;
            double timeElapsed = (obj.Time - prev.Time) / speedMul;
            double decay =
                Math.Pow(Constants.DECAY_BASE[type], timeElapsed / 1000.0);

            /* this implementation doesn't account for sliders */
            if ((obj.Type & (HitObjects.Slider | HitObjects.Circle)) != 0)
            {
                double distance = (obj.Normpos - prev.Normpos).Length;

                if (type == Constants.DiffSpeed)
                {
                    obj.IsSingle = distance > Constants.SINGLE_SPACING;
                }

                value = DSpacingWeight(type, distance);
                value *= Constants.WEIGHT_SCALING[type];
            }

            value /= Math.Max(timeElapsed, 50.0);
            obj.Strains[type] = prev.Strains[type] * decay + value;
        }

        internal static double pp_base(double stars)
        {
            return Math.Pow(5.0 * Math.Max(1.0, stars / 0.0675) - 4.0, 3.0) / 100000.0;
        }
    }
}
