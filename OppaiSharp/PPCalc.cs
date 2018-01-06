using System;

namespace OppaiSharp
{
    public class PPv2Parameters
    {
        /// <summary> 
        /// If not null, max_combo, nsliders, ncircles, nobjects, base_ar, base_od will be obtained from this beatmap.
        /// </summary>
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
        public GameMode Mode = GameMode.Standard;

        /// <summary> the mods bitmask, same as osu! api, see MODS_* constants </summary>
        public Mods Mods = Mods.NoMod;

        /// <summary> the maximum combo achieved, if -1 it will default to max_combo - nmiss </summary>
        public int Combo = -1;

        /// <summary> number of 300s, if -1 it will default to nobjects - Count100 - Count50 - nmiss </summary>
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
            int maxCombo, int countSliders, int countCircles, int countObjects,
            float baseAR, float baseOD, GameMode mode, Mods mods,
            int combo, int count300, int count100, int count50, int countMiss,
            int scoreVersion, Map beatmap)
        {
            if (beatmap != null) {
                mode = beatmap.Mode;
                baseAR = beatmap.AR;
                baseOD = beatmap.OD;
                maxCombo = beatmap.MaxCombo();
                countSliders = beatmap.CountSliders;
                countCircles = beatmap.CountCircles;
                countObjects = beatmap.Objects.Count;
            }

            if (mode != GameMode.Standard)
                throw new InvalidOperationException("this gamemode is not yet supported");

            if (maxCombo <= 0) {
                //TODO: warn "W: max_combo <= 0, changing to 1\n"
                maxCombo = 1;
            }

            if (combo < 0)
                combo = maxCombo - countMiss;

            if (count300 < 0)
                count300 = countObjects - count100 - count50 - countMiss;

            /* accuracy -------------------------------------------- */
            ComputedAccuracy = new Accuracy(count300, count100, count50, countMiss);
            double accuracy = ComputedAccuracy.Value();
            double realAcc = accuracy;

            switch (scoreVersion)
            {
                case 1:
                    /* scorev1 ignores sliders since they are free 300s
                    and for some reason also ignores spinners */
                    int countSpinners = countObjects - countSliders - countCircles;

                    realAcc = new Accuracy(count300 - countSliders - countSpinners, count100, count50, countMiss).Value();

                    realAcc = Math.Max(0.0, realAcc);
                    break;

                case 2:
                    countCircles = countObjects;
                    break;

                default:
                    throw new InvalidOperationException($"unsupported scorev{scoreVersion}");
            }

            //global values ---------------------------------------
            double countObjectsOver2K = countObjects / 2000.0;

            double lengthBonus = 0.95 + 0.4 * Math.Min(1.0, countObjectsOver2K);

            if (countObjects > 2000)
                lengthBonus += Math.Log10(countObjectsOver2K) * 0.5;

            double missPenality = Math.Pow(0.97, countMiss);
            double comboBreak = Math.Pow(combo, 0.8) / Math.Pow(maxCombo, 0.8);

            /* calculate stats with mods */
            var mapstats = new MapStats {
                AR = baseAR,
                OD = baseOD
            };
            MapStats.ModsApply(mods, mapstats, ModApplyFlags.ApplyAR | ModApplyFlags.ApplyOD);

            /* ar bonus -------------------------------------------- */
            double arBonus = 1.0;

            if (mapstats.AR > 10.33)
                arBonus += 0.45 * (mapstats.AR - 10.33);
            else if (mapstats.AR < 8.0) {
                double lowArBonus = 0.01 * (8.0 - mapstats.AR);

                if ((mods & Mods.Hidden) != 0)
                    lowArBonus *= 2.0;

                arBonus += lowArBonus;
            }

            /* aim pp ---------------------------------------------- */
            Aim = Helpers.GetPPBase(aimStars);
            Aim *= lengthBonus;
            Aim *= missPenality;
            Aim *= comboBreak;
            Aim *= arBonus;

            if ((mods & Mods.Hidden) != 0)
                Aim *= 1.18;

            if ((mods & Mods.Flashlight) != 0)
                Aim *= 1.45 * lengthBonus;

            double accBonus = 0.5 + accuracy / 2.0;
            double odBonus =
                0.98 + (mapstats.OD * mapstats.OD) / 2500.0;

            Aim *= accBonus;
            Aim *= odBonus;

            /* speed pp -------------------------------------------- */
            Speed = Helpers.GetPPBase(speedStars);
            Speed *= lengthBonus;
            Speed *= missPenality;
            Speed *= comboBreak;
            Speed *= accBonus;
            Speed *= odBonus;

            /* acc pp ---------------------------------------------- */
            Acc = Math.Pow(1.52163, mapstats.OD) * Math.Pow(realAcc, 24.0) * 2.83;

            Acc *= Math.Min(1.15, Math.Pow(countCircles / 1000.0, 0.3));

            if ((mods & Mods.Hidden) != 0)
                Acc *= 1.02;

            if ((mods & Mods.Flashlight) != 0)
                Acc *= 1.02;

            /* total pp -------------------------------------------- */
            double finalMultiplier = 1.12;

            if ((mods & Mods.NoFail) != 0)
                finalMultiplier *= 0.90;

            if ((mods & Mods.SpunOut) != 0)
                finalMultiplier *= 0.95;

            Total = Math.Pow(Math.Pow(Aim, 1.1) + Math.Pow(Speed, 1.1) + Math.Pow(Acc, 1.1), 1.0 / 1.1) * finalMultiplier;
        }

        /// <inheritdoc />
        /// <summary> See <see cref="PPv2Parameters" /> </summary>
        public PPv2(PPv2Parameters p) : 
            this(p.AimStars, p.SpeedStars, p.MaxCombo, p.Nsliders,
            p.Ncircles, p.Nobjects, p.BaseAr, p.BaseOd, p.Mode,
            p.Mods, p.Combo, p.N300, p.N100, p.N50, p.Nmiss,
            p.ScoreVersion, p.Beatmap)
        { }

        /// <inheritdoc />
        /// <summary>
        /// Simplest possible call, calculates ppv2 for SS scorev1
        /// </summary>
        public PPv2(double aimStars, double speedStars, Map map) 
            : this(aimStars, speedStars, -1, map.CountSliders, map.CountCircles, map.Objects.Count, 
                  map.AR, map.OD, map.Mode, Mods.NoMod, -1, -1, 0, 0, 0, 1, map)
        { }
    }
}
