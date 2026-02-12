using Beatmap.Base;
using SimpleJSON;

namespace NoodleMapper.ModMap;

public interface INoodleFilter
{
    public bool TestAgainst(BaseObject obj);
    public JSONNode ToJSON();
}