using System;

namespace OppaiSharp
{
    public class PPv2Parameters
    {
        /// <summary> if not null, max_combo, nsliders, ncircles, nobjects, base_ar, base_od will be obtained from this beatmap. </summary>
        public Map Beatmap = null;

        public double AimStars = 0.0;
        public double SpeedStars = 0.0;
        public int MaxCombo = 0;
        public int Nsliders = 0, Ncircles = 0, Nobjects = 0;

        /// <summary> the base AR (before applying mods). </summary>
        public float BaseAr = 5.0f;

        /// <summary> the base OD (before applying mods) </summary>
        public float BaseOd = 5.0f;

        /// <summary> gamemode </summary>
        public int Mode = Constants.ModeStd;

        /// <summary> the mods bitmask, same as osu! api, see MODS_* constants </summary>
        public Mods Mods = Mods.NoMod;

        /// <summary> the maximum combo achieved, if -1 it will default to max_combo - nmiss </summary>
        public int Combo = -1;

        /// <summary> number of 300s, if -1 it will default to nobjects - n100 - n50 - nmiss </summary>
        public int N300 = -1;
        public int N100 = 0, N50 = 0, Nmiss = 0;

        /// <summary> scorev1 (1) or scorev2 (2) </summary>
        public int ScoreVersion = 1;
    }

    public class PPv2
    {
        public double Total, Aim, Speed, Acc;
        public Accuracy ComputedAccuracy;

        /// <summary>
        /// calculates ppv2, results are stored in total, aim, speed, acc, acc_percent.
        /// See: <seealso cref="PPv2Parameters"/>
        /// </summary>
        private PPv2(double aimStars, double speedStars,
            int maxCombo, int nsliders, int ncircles, int nobjects,
            float baseAr, float baseOd, int mode, Mods mods,
            int combo, int n300, int n100, int n50, int nmiss,
            int scoreVersion, Map beatmap)
        {
            if (beatmap != null) {
                mode = beatmap.Mode;
                baseAr = beatmap.Ar;
                baseOd = beatmap.Od;
                maxCombo = beatmap.MaxCombo();
                nsliders = beatmap.Nsliders;
                ncircles = beatmap.Ncircles;
                nobjects = beatmap.Objects.Count;
            }

            if (mode != Constants.ModeStd)
                throw new InvalidOperationException("this gamemode is not yet supported");

            if (maxCombo <= 0)
            {
                //TODO: warn "W: max_combo <= 0, changing to 1\n"
                maxCombo = 1;
            }

            if (combo < 0)
            {
                combo = maxCombo - nmiss;
            }

            if (n300 < 0)
            {
                n300 = nobjects - n100 - n50 - nmiss;
            }

            /* accuracy -------------------------------------------- */
            ComputedAccuracy = new Accuracy(n300, n100, n50, nmiss);
            double accuracy = ComputedAccuracy.value();
            double realAcc = accuracy;

            switch (scoreVersion)
            {
                case 1:
                    /* scorev1 ignores sliders since they are free 300s
                    and for some reason also ignores spinners */
                    int nspinners = nobjects - nsliders - ncircles;

                    realAcc = new Accuracy(n300 - nsliders - nspinners,
                        n100, n50, nmiss).value();

                    realAcc = Math.Max(0.0, realAcc);
                    break;

                case 2:
                    ncircles = nobjects;
                    break;

                default:
                    throw new InvalidOperationException(string.Format($"unsupported scorev{0:D}",scoreVersion)
                    );
            }

            //global values ---------------------------------------
            double nobjectsOver2K = nobjects / 2000.0;

            double lengthBonus = 0.95 + 0.4 *
                Math.Min(1.0, nobjectsOver2K);

            if (nobjects > 2000)
            {
                lengthBonus += Math.Log10(nobjectsOver2K) * 0.5;
            }

            double missPenality = Math.Pow(0.97, nmiss);
            double comboBreak = Math.Pow(combo, 0.8) /
                Math.Pow(maxCombo, 0.8);

            /* calculate stats with mods */
            MapStats mapstats = new MapStats();
            mapstats.ar = baseAr;
            mapstats.od = baseOd;
            Helpers.ModsApply(mods, mapstats, ModApplyFlags.ApplyAr | ModApplyFlags.ApplyOd);

            /* ar bonus -------------------------------------------- */
            double arBonus = 1.0;

            if (mapstats.ar > 10.33)
            {
                arBonus += 0.45 * (mapstats.ar - 10.33);
            }

            else if (mapstats.ar < 8.0)
            {
                double lowArBonus = 0.01 * (8.0 - mapstats.ar);

                if ((mods & Mods.Hidden) != 0)
                {
                    lowArBonus *= 2.0;
                }

                arBonus += lowArBonus;
            }

            /* aim pp ---------------------------------------------- */
            Aim = Helpers.pp_base(aimStars);
            Aim *= lengthBonus;
            Aim *= missPenality;
            Aim *= comboBreak;
            Aim *= arBonus;

            if ((mods & Mods.Hidden) != 0)
            {
                Aim *= 1.18;
            }

            if ((mods & Mods.Flashlight) != 0)
            {
                Aim *= 1.45 * lengthBonus;
            }

            double accBonus = 0.5 + accuracy / 2.0;
            double odBonus =
                0.98 + (mapstats.od * mapstats.od) / 2500.0;

            Aim *= accBonus;
            Aim *= odBonus;

            /* speed pp -------------------------------------------- */
            Speed = Helpers.pp_base(speedStars);
            Speed *= lengthBonus;
            Speed *= missPenality;
            Speed *= comboBreak;
            Speed *= accBonus;
            Speed *= odBonus;

            /* acc pp ---------------------------------------------- */
            Acc = Math.Pow(1.52163, mapstats.od) *
                Math.Pow(realAcc, 24.0) * 2.83;

            Acc *= Math.Min(1.15, Math.Pow(ncircles / 1000.0, 0.3));

            if ((mods & Mods.Hidden) != 0)
            {
                Acc *= 1.02;
            }

            if ((mods & Mods.Flashlight) != 0)
            {
                Acc *= 1.02;
            }

            /* total pp -------------------------------------------- */
            double finalMultiplier = 1.12;

            if ((mods & Mods.NoFail) != 0)
            {
                finalMultiplier *= 0.90;
            }

            if ((mods & Mods.SpunOut) != 0)
            {
                finalMultiplier *= 0.95;
            }

            Total = Math.Pow(
                Math.Pow(Aim, 1.1) + Math.Pow(Speed, 1.1) +
                Math.Pow(Acc, 1.1),
                1.0 / 1.1
            ) * finalMultiplier;
        }

        /** @see PPv2Parameters */
        public PPv2(PPv2Parameters p) : 
            this(p.AimStars, p.SpeedStars, p.MaxCombo, p.Nsliders,
            p.Ncircles, p.Nobjects, p.BaseAr, p.BaseOd, p.Mode,
            p.Mods, p.Combo, p.N300, p.N100, p.N50, p.Nmiss,
            p.ScoreVersion, p.Beatmap)
        { }

        /**
        * simplest possible call, calculates ppv2 for SS scorev1.
        * @see Koohii.PPv2#Koohii.PPv2(Koohii.PPv2Parameters)
        */
        public PPv2(double aimStars, double speedStars, Map b) 
            : this(aimStars, speedStars, -1, b.Nsliders, b.Ncircles, b.Objects.Count, b.Ar, b.Od, b.Mode, Mods.NoMod, -1, -1, 0, 0, 0, 1, b)
        { }
    }
}
