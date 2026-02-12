using System.Collections.Generic;
using NoodleMapper.ModMap;
using SimpleJSON;

namespace NoodleMapper.ModMap;

public class ModMapData
{
    public List<NoodleSpan> EffectSpans { get; } = new List<NoodleSpan>();

    public static ModMapData FromJSON(JSONNode node)
    {
        var data = new ModMapData();
        
        var spans = node.GetValueOrDefault("spans", new JSONArray());
        for (var i = 0; i < spans.Count; i++)
        {
            var span = NoodleSpan.FromJSON(spans[i]);
            data.EffectSpans.Add(span);
        }
        
        return data;
    }

    public JSONNode ToJSON()
    {
        var node = new JSONObject();
        
        node["spans"] = new JSONArray();
        foreach (var span in EffectSpans)
        {
            node["spans"].Add(span.ToJSON());
        }
        
        return node;
    }
}