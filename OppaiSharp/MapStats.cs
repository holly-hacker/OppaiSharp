using System;

namespace OppaiSharp
{
    internal struct MapStats
    {
        private const double OD0Ms = 79.5;
        private const double OD10Ms = 19.5;
        private const double AR0Ms = 1800.0;
        private const double AR5Ms = 1200.0;
        private const double AR10Ms = 450.0;

        private const double ODMsStep = (OD0Ms - OD10Ms) / 10.0;
        private const double ARMsStep1 = (AR0Ms - AR5Ms) / 5.0;
        private const double ARMsStep2 = (AR5Ms - AR10Ms) / 5.0;

        public float AR, OD, CS, HP;

        /// <summary>
        /// Speed multiplier / music rate. this doesn't need to be initialized before calling <seealso cref="ModsApply"/>
        /// </summary>
        public float Speed;

        /// <summary>
        /// applies mods to mapstats.
        /// <p><blockquote><pre>
        /// var mapstats = new MapStats();
        /// mapstats.AR = 9;
        /// Koohii.mods_apply(Mods.DoubleTime, mapstats, ModApplyFlags.ApplyAR);
        /// // mapstats.AR is now 10.33, mapstats.speed is 1.5
        /// </pre></blockquote></p>
        /// </summary>
        /// <param name="mods"></param>
        /// <param name="mapstats">the base beatmap stats</param>
        /// <param name="flags">
        /// enum that specifies which stats to modify. only the stats specified here need to be initialized in mapstats
        /// </param>
        /// <returns>mapstats</returns>
        public static MapStats ModsApply(Mods mods, MapStats mapstats, 
            ModApplyFlags flags = ModApplyFlags.ApplyAR | ModApplyFlags.ApplyCS | ModApplyFlags.ApplyHP | ModApplyFlags.ApplyOD)
        {
            mapstats.Speed = 1.0f;

            if ((mods & Mods.MapChanging) == 0)
                return mapstats;

            if ((mods & (Mods.DoubleTime | Mods.Nightcore)) != 0)
                mapstats.Speed = 1.5f;

            if ((mods & Mods.HalfTime) != 0)
                mapstats.Speed *= 0.75f;

            float odArHpMultiplier = 1.0f;

            if ((mods & Mods.Hardrock) != 0)
                odArHpMultiplier = 1.4f;

            if ((mods & Mods.Easy) != 0)
                odArHpMultiplier *= 0.5f;

            if ((flags & ModApplyFlags.ApplyAR) != 0) {
                mapstats.AR *= odArHpMultiplier;

                //convert AR into milliseconds window
                double arms = mapstats.AR < 5.0f ?
                    AR0Ms - ARMsStep1 * mapstats.AR
                    : AR5Ms - ARMsStep2 * (mapstats.AR - 5.0f);

                //stats must be capped to 0-10 before HT/DT which brings
                //them to a range of -4.42->11.08 for OD and -5->11 for AR
                arms = Math.Min(AR0Ms, Math.Max(AR10Ms, arms));
                arms /= mapstats.Speed;

                mapstats.AR = (float)(
                    arms > AR5Ms
                    ? (AR0Ms - arms) / ARMsStep1
                    : 5.0 + (AR5Ms - arms) / ARMsStep2
                );
            }

            if ((flags & ModApplyFlags.ApplyOD) != 0) {
                mapstats.OD *= odArHpMultiplier;
                double odms = OD0Ms - Math.Ceiling(ODMsStep * mapstats.OD);
                odms = Math.Min(OD0Ms, Math.Max(OD10Ms, odms));
                odms /= mapstats.Speed;
                mapstats.OD = (float)((OD0Ms - odms) / ODMsStep);
            }

            if ((flags & ModApplyFlags.ApplyCS) != 0) {
                if ((mods & Mods.Hardrock) != 0)
                    mapstats.CS *= 1.3f;

                if ((mods & Mods.Easy) != 0)
                    mapstats.CS *= 0.5f;

                mapstats.CS = Math.Min(10.0f, mapstats.CS);
            }

            if ((flags & ModApplyFlags.ApplyHP) != 0)
                mapstats.HP = Math.Min(10.0f, mapstats.HP * odArHpMultiplier);

            return mapstats;
        }
    }
}
