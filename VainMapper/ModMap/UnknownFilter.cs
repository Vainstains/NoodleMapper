using Beatmap.Base;
using SimpleJSON;
using VainLib.Data;

namespace VainMapper.ModMap;

public class UnknownFilter : JsonFallback, INoodleFilter
{
    public bool TestAgainst(BaseObject obj) => true;

    public JSONNode ToJSON() => Node;
}
