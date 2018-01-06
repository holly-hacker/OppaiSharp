# OppaiSharp


## Example usage

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
Console.WriteLine($"Star rating: {stars.Total:F2} (aim stars: {stars.Aim:F2}, speed stars: {stars.Speed:F2})");

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
Console.WriteLine($"Play is worth {pp.Total:F2}pp ({pp.Aim:F2} aim pp, {pp.Acc:F2} acc pp, {pp.Speed:F2} speed pp) and has an accuracy of {pp.ComputedAccuracy.Value() * 100:F2}");
```