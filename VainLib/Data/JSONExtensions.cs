using SimpleJSON;

namespace VainLib.Data;

public static class JSONExtensions
{
    public static bool TryGetNode(this JSONNode json, string key, out JSONNode node)
    {
        node = null!;
        if (!json.HasKey(key))
            return false;
        node = json[key];
        return true;
    }

    public static bool TryGetArray(this JSONNode json, string key, out JSONArray arr)
    {
        arr = null!;
        if (!json.TryGetNode(key, out var node))
            return false;
        if (!node.IsArray)
            return false;
        
        arr = (node as JSONArray)!;
        return true;
    }
    
    public static bool TryGetObject(this JSONNode json, string key, out JSONObject arr)
    {
        arr = null!;
        if (!json.TryGetNode(key, out var node))
            return false;
        if (!node.IsObject)
            return false;
        
        arr = (node as JSONObject)!;
        return true;
    }
    
    public static bool TryGetString(this JSONNode json, string key, out string str)
    {
        str = string.Empty;
        if (!json.TryGetNode(key, out var node))
            return false;
        if (!node.IsString)
            return false;

        str = node.Value;
        return true;
    }
    
    public static bool TryGetBool(this JSONNode json, string key, out bool value)
    {
        value = false;
        if (!json.TryGetNode(key, out var node))
            return false;
        if (!node.IsBoolean)
            return false;

        value = node.AsBool;
        return true;
    }
    
    public static bool TryGetInt(this JSONNode json, string key, out int value)
    {
        value = 0;
        if (!json.TryGetNode(key, out var node))
            return false;
        if (!node.IsNumber)
            return false;
        
        value = node.AsInt;
        return true;
    }
    
    public static bool TryGetFloat(this JSONNode json, string key, out float value)
    {
        value = 0;
        if (!json.TryGetNode(key, out var node))
            return false;
        if (!node.IsNumber)
            return false;
        
        value = node.AsFloat;
        return true;
    }
}