using SimpleJSON;
using System.Collections.Generic;
using VainLib.Data;

namespace VainMapper.ModMap;

public class NoodleSpan
{
    [JsonID("b")]
    public float StartBeat { get; set; } = 0;

    [JsonID("d")]
    public float Duration { get; set; } = 0;

    [JsonID("filters")]
    public List<INoodleFilter> Filters { get; set; } = new();

    [JsonID("processors")]
    public List<INoodleSpanProcessor> Processors { get; set; } = new();
}
