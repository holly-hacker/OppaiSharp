using System;

namespace OppaiSharp
{
    [Flags]
    public enum Mods
    {
        NoMod = 0,
        NoFail = 1 << 0,
        Easy = 1 << 1,
        TouchDevice = 1 << 2,
        Hidden = 1 << 3,
        Hardrock = 1 << 4,
        DoubleTime = 1 << 6,
        HalfTime = 1 << 8,
        Nightcore = 1 << 9,
        Flashlight = 1 << 10,
        SpunOut = 1 << 12,
        SpeedChanging = DoubleTime | HalfTime | Nightcore,
        MapChanging = Hardrock | Easy | SpeedChanging,
    }

    [Flags]
    public enum HitObjects
    {
        Circle = 1 << 0,
        Slider = 1 << 1,
        Spinner = 1 << 3,
    }

    [Flags]
    public enum ModApplyFlags
    {
        ApplyAr = 1 << 0,
        ApplyOd = 1 << 1,
        ApplyCs = 1 << 2,
        ApplyHp = 1 << 3
    }
}
