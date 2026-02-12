using NoodleMapper.ModMap;
using NoodleMapper.Utils;
using SimpleJSON;

namespace NoodleMapper.Map;

public class MapData
{
    public string? ModMapFile { get; private set; } = null;
    public ModMapData? ModMapData { get; private set; } = null;
    public static MapData FromJSON(JSONNode node)
    {
        var data = new MapData();
        
        if (!node.IsObject)
            return data;

        if (node.TryGetString("modMap", out var file))
        {
            data.ModMapFile = file;
            var modMapJson = Helpers.LoadJSONFile(Helpers.GetModMapDataPath(file));
            data.ModMapData = ModMapData.FromJSON(modMapJson);
        }
        
        return data;
    }

    public JSONNode ToJSON()
    {
        var node = new JSONObject();
        
        if (ModMapFile != null && ModMapData != null)
        {
            node.Add("modMap", ModMapFile);
            Helpers.WriteAllText(Helpers.GetModMapDataPath(ModMapFile), ModMapData.ToJSON().ToString(4));
        }
        
        return node;
    }
}