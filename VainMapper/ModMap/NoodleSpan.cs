using System.Collections.Generic;
using SimpleJSON;

namespace VainMapper.ModMap;

public class NoodleSpan
{
    public float StartBeat { get; set; } = 0;
    public float Duration { get; set; } = 0;
    public List<INoodleFilter> Filters { get; } = new ();
    public List<INoodleSpanProcessor> Processors { get; } = new ();

    public static NoodleSpan FromJSON(JSONNode node)
    {
        var span = new NoodleSpan();
        span.StartBeat = node.GetValueOrDefault("b", 0);
        span.Duration = node.GetValueOrDefault("d", 0);
        var filters = node.GetValueOrDefault("filters", new JSONArray());
        for (var i = 0; i < filters.Count; i++)
        {
            var filterNode = filters[i];
            var filter = ParseFilter(filterNode);
            
            span.Filters.Add(filter);
        }

        return span;
    }
    
    public JSONNode ToJSON()
    {
        var node = new JSONObject();
        node["b"] = StartBeat;
        node["d"] = Duration;
        node["filters"] = new JSONArray();

        foreach (var filter in Filters)
        {
            node["filters"] = filter.ToJSON();
        }
        
        return node;
    }

    private static INoodleFilter ParseFilter(JSONNode filterNode)
    {
        string type = filterNode.GetValueOrDefault("type", "");
        
        if (string.IsNullOrEmpty(type))
            return new UnknownFilter(filterNode);

        switch (type)
        {
            case HandTypeFilter.FilterTypeStr:
                return HandTypeFilter.FromJSONNode(filterNode);
        }
        
        return new UnknownFilter(filterNode);
    }
}