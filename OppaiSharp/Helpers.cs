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
    }
}
