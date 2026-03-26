using Beatmap.Base;
using SimpleJSON;

namespace VainMapper.ModMap;

public class UnknownFilter : INoodleFilter
{
    public JSONNode Node { get; }
    public bool TestAgainst(BaseObject obj) => true;

    public UnknownFilter(JSONNode node)
    {
        Node = node;
    }
    
    public JSONNode ToJSON() => Node;
}