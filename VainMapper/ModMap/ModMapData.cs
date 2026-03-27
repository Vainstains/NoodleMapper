using SimpleJSON;
using System.Collections.Generic;
using VainLib.Data;

namespace VainMapper.ModMap;

public class ModMapData
{
    [JsonID("spans")]
    public List<NoodleSpan> EffectSpans { get; set; } = new();
}
