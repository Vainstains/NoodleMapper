using SimpleJSON;

namespace VainLib.Data;

public abstract class JsonFallback
{
    public JSONNode Node { get; set; } = JSONNull.CreateOrGet();
}
