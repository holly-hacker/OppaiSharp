using System.Collections.Generic;

namespace OppaiSharp
{
    internal partial class DiffCalc
    {
        /// <summary>
        /// arbitrary thresholds to determine when a stream is spaced enough that it becomes hard to alternate
        /// </summary>
        public const double StreamSpacing = 110.0;

        /// <summary>
        /// arbitrary thresholds to determine when a stream is spaced enough that it becomes hard to alternate
        /// </summary>
        public const double SingleSpacing = 125.0;

        /// <summary> Non-normalized diameter where the small circle buff starts. </summary>
        public const double CirclesizeBuffThreshold = 30.0;

        /// <summary> global stars multiplier </summary>
        public const double StarScalingFactor = 0.0675;

        /// <summary> in osu! pixels </summary>
        public const double PlayfieldWidth = 512.0;

        /// <summary> in osu! pixels </summary>
        public const double PlayfieldHeight = 384.0;

        public static readonly Vector2 PlayfieldCenter = new Vector2(PlayfieldWidth / 2.0, PlayfieldHeight / 2.0);

        /// <summary>
        /// 50% of the difference between aim and speed is added to total star rating to compensate for aim/speed only maps
        /// </summary>
        public const double ExtremeScalingFactor = 0.5;

        /// <summary>
        /// Strains are calculated by analyzing the map in chunks and taking the peak strains in each chunk. This is 
        /// the length of a strain interval in milliseconds.
        /// </summary>
        public const double StrainStep = 400.0;

        /// <summary> max strains are weighted from highest to lowest, this is how much the weight decays </summary>
        public const double DecayWeight = 0.9;

        /// <summary> almost the normalized circle diameter.</summary>
        public const double AlmostDiameter = 90.0;

        /// <summary>strain decay per interval</summary>
        private static readonly Dictionary<StrainType, double> DecayBase = new Dictionary<StrainType, double> { { StrainType.Speed, 0.3 }, { StrainType.Aim, 0.15 } };

        /// <summary>balances speed and aim</summary>
        private static readonly Dictionary<StrainType, double> WeightScaling = new Dictionary<StrainType, double> { { StrainType.Speed, 1400.0 }, { StrainType.Aim, 26.25 } };
    }
}
