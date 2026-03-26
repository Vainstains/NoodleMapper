using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;
using VainMapper.ModMap;
using VainMapper.Utils;

namespace VainMapper.Map;

public class MapData
{
    public string? ModMapFile { get; private set; } = null;
    public ModMapData? ModMapData { get; private set; } = null;

    public List<MapRange> MapRanges { get; } = new();
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
        
        if (node.TryGetArray("ranges", out var ranges))
        {
            for (var i = 0; i < ranges.Count; i++)
            {
                var range = MapRange.FromJSON(ranges[i]);
                data.MapRanges.Add(range);
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
        
        var rangesArray = new JSONArray();
        foreach (var range in MapRanges)
        {
            rangesArray.Add(range.ToJSON());
        }
        node.Add("ranges", rangesArray);
        
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