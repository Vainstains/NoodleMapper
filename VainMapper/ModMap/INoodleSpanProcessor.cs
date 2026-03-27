using Beatmap.Base;

namespace VainMapper.ModMap;

public interface INoodleSpanProcessor : IModMapEditorItem
{
    public void Process(BaseObject obj);
}
