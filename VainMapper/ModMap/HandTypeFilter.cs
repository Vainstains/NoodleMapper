using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace VainMapper.ModMap;

public class HandTypeFilter : INoodleFilter
{
    public const string FilterTypeStr = "HandType";
    public HandType Hand { get; set; }

    public bool TestAgainst(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote note:
                return note.Color == (int)Hand && note.Type != (int)NoteType.Bomb;
            case BaseChain chain:
                return chain.Color == (int)Hand;
            case BaseArc arc:
                return arc.Color == (int)Hand;
        }
        return false;
    }

    public static HandTypeFilter FromJSONNode(JSONNode node)
    {
        var filter = new HandTypeFilter();
        filter.Hand = (HandType)(int)node.GetValueOrDefault("hand", 0);
        return filter;
    }

    public JSONNode ToJSON()
    {
        var node = new JSONObject();
        
        node["type"] = FilterTypeStr;
        node["hand"] = (int)Hand;
        
        return node;
    }
}