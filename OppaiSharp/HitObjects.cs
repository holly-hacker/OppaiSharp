using System.Collections.Generic;
using System.Text;

namespace OppaiSharp
{
    public class HitObject
    {
        /// <summary> Start time in milliseconds. </summary>
        public double Time { get; set; } = 0.0;
        public HitObjectType Type { get; set; } = HitObjectType.Circle;

        /// <summary> An instance of Circle or Slider or null. </summary>
        public object Data { get; set; } = null;
        public Vector2 Normpos { get; set; } = new Vector2();
        public Dictionary<StrainType, double> Strains { get; } = new Dictionary<StrainType, double> { { StrainType.Speed, 0.0 }, { StrainType.Aim, 0.0 } };
        public bool IsSingle { get; internal set; } = false;

        /// <summary> String representation of the type bitmask. </summary>
        public string TypeString()
        {
            var res = new StringBuilder();

            if ((Type & HitObjectType.Circle) != 0) res.Append("circle | ");
            if ((Type & HitObjectType.Slider) != 0) res.Append("slider | ");
            if ((Type & HitObjectType.Spinner) != 0) res.Append("spinner | ");

            string result = res.ToString();
            return result.Substring(0, result.Length - 3);
        }

        public override string ToString()
        {
            return $"{{ time={Time}, type={TypeString()}, data={Data}, normpos={Normpos}, " 
                 + $"strains=[ {Strains[StrainType.Speed]}, {Strains[StrainType.Aim]} ], is_single={IsSingle} }}";
        }
    }

    public class Timing
    {
        /// <summary> Start time in milliseconds. </summary>
        public double Time = 0.0;
        public double MsPerBeat = -100.0;

        /// <summary> If false, <seealso cref="MsPerBeat"/> is -100 * BpmMultiplier. </summary>
        public bool Change = false;
    }

    public struct Circle
    {
        public Vector2 Position;

        public override string ToString() => Position.ToString();
    }

    public struct Slider
    {
        public Vector2 Position;
        public double Distance;
        public int Repetitions;

        public override string ToString() => $"{{ pos={Position}, distance={Distance}, repetitions={Repetitions} }}";
    }
}
