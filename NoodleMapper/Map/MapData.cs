using System.IO;
using NoodleMapper.ModMap;
using NoodleMapper.Utils;
using SimpleJSON;
using UnityEngine;

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
            var path = Helpers.GetModMapDataPath(file);
            if (File.Exists(path))
            {
                data.ModMapFile = file;
                var modMapJson = Helpers.LoadJSONFile(path);
                data.ModMapData = ModMapData.FromJSON(modMapJson);
            }
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

    public void SetModMapFile(string? file)
    {
        Debug.Log($"Setting active modmap: {file ?? "null"}");
        ModMapFile = file;
        if (ModMapFile == null)
        {
            ModMapData = null;
            return;
        }
        var modMapJson = Helpers.LoadJSONFile(Helpers.GetModMapDataPath(file!));
        ModMapData = ModMapData.FromJSON(modMapJson);
    }
}