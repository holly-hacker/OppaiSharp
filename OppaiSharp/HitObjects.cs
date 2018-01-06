using System;
using System.Text;

namespace OppaiSharp
{
    public class HitObject
    {
        /// <summary> Start time in milliseconds. </summary>
        public double Time = 0.0;
        public HitObjects Type = HitObjects.Circle;

        /// <summary> An instance of Circle or Slider or null. </summary>
        public object Data = null;
        public Vector2 Normpos = new Vector2();
        public double[] Strains = { 0.0, 0.0 };
        public bool IsSingle = false;

        /// <summary> String representation of the type bitmask. </summary>
        public string Typestr()
        {
            StringBuilder res = new StringBuilder();

            if ((Type & HitObjects.Circle) != 0) res.Append("circle | ");
            if ((Type & HitObjects.Slider) != 0) res.Append("slider | ");
            if ((Type & HitObjects.Spinner) != 0) res.Append("spinner | ");

            string result = res.ToString();
            return result.Substring(0, result.Length - 3);
        }

        public override string ToString()
        {
            return $"{{ time={Time}, type={Typestr()}, data={Data}, normpos={Normpos}, " 
                 + $"strains=[ {Strains[0]}, {Strains[1]} ], is_single={IsSingle} }}";
        }
    }

    public class Timing
    {
        /// <summary> Start time in milliseconds. </summary>
        public double Time = 0.0;
        public double MsPerBeat = -100.0;

        /// <summary> If false, ms_per_beat is -100 * bpm_multiplier. </summary>
        public bool Change = false;
    }

    //TODO: structs?
    internal class Circle
    {
        public Vector2 Position;

        public override string ToString() => Position.ToString();
    }

    internal class Slider
    {
        public Vector2 Position;
        public double Distance;
        public int Repetitions;

        public override string ToString() => $"{{ pos={Position}, distance={Distance}, repetitions={Repetitions} }}";
    }
}
