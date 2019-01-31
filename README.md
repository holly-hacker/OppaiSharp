[![NuGet](https://img.shields.io/nuget/v/HoLLy.osu.OppaiSharp.svg)](https://www.nuget.org/packages/HoLLy.osu.OppaiSharp)
# OppaiSharp
OppaiSharp is a C# port of [oppai-ng](//github.com/Francesco149/oppai-ng) by [Francesco149](//github.com/Francesco149), based on its Java port [Koohii](//github.com/Francesco149/koohii). It allows you to calculate star ratings and PP values for any osu!standard map, just like oppai(-ng) does.

It is "licensed" under the Unlicense, so you're free to do whatever you want with it. But giving me or Francesco149 a bit of credit doesn't hurt, and I would appreciate it if you didn't claim it as your own :)

## Warning
This repo is not up-to-date with the latest changes to the PP and SR algorithm, and has been superseded by the official implementation at [ppy/osu](https://github.com/ppy/osu). Please switch to using it.

## Example usage
v2:
```cs
//create a StreamReader for your beatmap
byte[] data = new WebClient().DownloadData("https://osu.ppy.sh/osu/774965");
var stream = new MemoryStream(data, false);
var reader = new StreamReader(stream);

//read a beatmap
var beatmap = Beatmap.Read(reader);

//calculate star ratings for HDDT
Mods mods = Mods.Hidden | Mods.DoubleTime;
var diff = new DiffCalc().Calc(beatmap, mods);
Console.WriteLine(string.Format("Star rating: {0:F2} (aim stars: {1:F2}, speed stars: {2:F2})", 
    diff.Total, diff.Aim, diff.Speed));

//calculate the PP for a play on this map
//the play has no misses or 50's, so we don't specify them
var pp = new PPv2(new PPv2Parameters(beatmap, diff, c100: 8, mods: mods));

Console.WriteLine(string.Format("Play is worth {0:F2}pp ({1:F2} aim pp, {2:F2} acc pp, {3:F2} speed pp) " +
                                "and has an accuracy of {4:F2}%", 
    pp.Total, pp.Aim, pp.Acc, pp.Speed, pp.ComputedAccuracy.Value() * 100));
```

v1 (old):
```cs
//create a StreamReader for your beatmap
byte[] data = new WebClient().DownloadData("https://osu.ppy.sh/osu/774965");
var stream = new MemoryStream(data, false);
var reader = new StreamReader(stream);

//parse the beatmap
var beatmap = new Parser().Map(reader);

//calculate star ratings for HDDT
Mods mods = Mods.Hidden | Mods.DoubleTime;
var stars = new DiffCalc().Calc(beatmap, mods);
Console.WriteLine(string.Format("Star rating: {0:F2} (aim stars: {1:F2}, speed stars: {2:F2})", 
    stars.Total, stars.Aim, stars.Speed));

//calculate the PP value for a play
var pp = new PPv2(new PPv2Parameters {
    Beatmap = beatmap,
    AimStars = stars.Aim,
    SpeedStars = stars.Speed,
    Mods = mods,
    Count100 = 8,
    Count50 = 0,
    CountMiss = 0,
});
Console.WriteLine(string.Format("Play is worth {0:F2}pp ({1:F2} aim pp, {2:F2} acc pp, {3:F2} speed pp) " +
                                "and has an accuracy of {4:F2}%", 
    pp.Total, pp.Aim, pp.Acc, pp.Speed, pp.ComputedAccuracy.Value() * 100));
```
