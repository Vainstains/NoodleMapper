using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace VainMapper.Map;

public class MapRange
{
    public float StartBeat { get; set; } = 0;
    public float EndBeat { get; set; } = 0;
    public Color Color { get; set; } = Color.white;
    public string Name { get; set; } = "";

    public static MapRange FromJSON(JSONNode node)
    {
        var span = new MapRange();
        span.StartBeat = node.GetValueOrDefault("s", 0);
        span.EndBeat = node.GetValueOrDefault("e", span.StartBeat);
        span.Color = node.GetValueOrDefault("c", Color.white);
        span.Name = node.GetValueOrDefault("n", "");
        return span;
    }
    
    public JSONNode ToJSON()
    {
        var s = StartBeat;
        var e = EndBeat;

        var node = new JSONObject();
        if (e > s + 0.01f)
        {
            node["s"] = StartBeat;
            node["e"] = EndBeat;
        }
        else
        {
            node["s"] = StartBeat;
        }
        node["c"] = Color;
        node["n"] = Name;
        
        return node;
    }
}