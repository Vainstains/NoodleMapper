using Beatmap.Base;
using SimpleJSON;

namespace VainMapper.ModMap;

public interface INoodleFilter : IModMapEditorItem
{
    public bool TestAgainst(BaseObject obj);
}
