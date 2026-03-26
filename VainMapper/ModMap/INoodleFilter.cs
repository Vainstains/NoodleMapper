using Beatmap.Base;
using SimpleJSON;

namespace VainMapper.ModMap;

public interface INoodleFilter
{
    public bool TestAgainst(BaseObject obj);
    public JSONNode ToJSON();
}