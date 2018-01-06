namespace OppaiSharp
{
    internal static class Constants
    {
        public const int VersionMajor = 1;
        public const int VersionMinor = 0;
        public const int VersionPatch = 11;
        public const int ModeStd = 0;   //TODO: enum


        /** strain index for speed */
        public const int DiffSpeed = 0;

        /** strain index for aim */
        public const int DiffAim = 1;

        public const double OD0_MS = 79.5;
        public const double OD10_MS = 19.5;
        public const double AR0_MS = 1800.0;
        public const double AR5_MS = 1200.0;
        public const double AR10_MS = 450.0;

        public const double OD_MS_STEP = (OD0_MS - OD10_MS) / 10.0;
        public const double AR_MS_STEP1 = (AR0_MS - AR5_MS) / 5.0;
        public const double AR_MS_STEP2 = (AR5_MS - AR10_MS) / 5.0;


        /** almost the normalized circle diameter. */
        public const double ALMOST_DIAMETER = 90.0;

        /**
        * arbitrary thresholds to determine when a stream is spaced
        * enough that it becomes hard to alternate. */
        public const double STREAM_SPACING = 110.0, SINGLE_SPACING = 125.0;

        /** strain decay per interval. */
        public static readonly double[] DECAY_BASE = {0.3, 0.15};

        /** balances speed and aim. */
        public static readonly double[] WEIGHT_SCALING = {1400.0, 26.25};

        /**
        * max strains are weighted from highest to lowest, this is how
        * much the weight decays. */
        public const double DECAY_WEIGHT = 0.9;

        /**
        * strains are calculated by analyzing the map in chunks and taking
        * the peak strains in each chunk. this is the length of a strain
        * interval in milliseconds */
        public const double STRAIN_STEP = 400.0;

        /** non-normalized diameter where the small circle buff starts. */
        public const double CIRCLESIZE_BUFF_THRESHOLD = 30.0;

        /** global stars multiplier. */
        public const double STAR_SCALING_FACTOR = 0.0675;

        /** in osu! pixels */
        public const double PLAYFIELD_WIDTH = 512.0, PLAYFIELD_HEIGHT = 384.0;

        public static readonly Vector2 PLAYFIELD_CENTER = new Vector2(
            PLAYFIELD_WIDTH / 2.0, PLAYFIELD_HEIGHT / 2.0
        );

        /**
        * 50% of the difference between aim and speed is added to total
        * star rating to compensate for aim/speed only maps
        */
        public const double EXTREME_SCALING_FACTOR = 0.5;
    }
}
