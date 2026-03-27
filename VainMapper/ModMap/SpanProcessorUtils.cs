using Beatmap.Base;
using SimpleJSON;

namespace VainMapper.ModMap;

internal static class SpanProcessorUtils
{
    public static JSONObject EnsureCustomDataObject(BaseObject obj)
    {
        if (obj.CustomData is JSONObject objectNode)
            return objectNode;

        objectNode = new JSONObject();
        obj.CustomData = objectNode;
        return objectNode;
    }

    public static JSONObject EnsureChildObject(JSONObject parent, string key)
    {
        if (parent[key] is JSONObject objectNode)
            return objectNode;

        objectNode = new JSONObject();
        parent[key] = objectNode;
        return objectNode;
    }

    public static void RefreshObject(BaseObject obj)
    {
        obj.RefreshCustom();
    }
}
