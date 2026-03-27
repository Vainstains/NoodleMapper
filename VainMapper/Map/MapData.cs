using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using VainLib.Data;
using VainLib.IO;
using VainMapper.ModMap;
using VainMapper.Utils;

namespace VainMapper.Map;

public class MapData
{
    private class MapFileData
    {
        [JsonID("modMap")]
        public string? ModMapFile { get; set; }

        [JsonID("ranges")]
        public List<MapRange> MapRanges { get; set; } = new();
    }

    public string? ModMapFile { get; private set; }
    public ModMapData? ModMapData { get; private set; }
    public List<MapRange> MapRanges { get; private set; } = new();

    public static MapData LoadFromFile(string path)
    {
        var file = new JsonFile<MapFileData>(path);
        return FromFileData(file.Data);
    }

    private static MapData FromFileData(MapFileData fileData)
    {
        var data = new MapData
        {
            MapRanges = fileData.MapRanges ?? new List<MapRange>()
        };

        if (!string.IsNullOrEmpty(fileData.ModMapFile))
            data.SetModMapFile(fileData.ModMapFile);

        return data;
    }

    private MapFileData ToFileData()
    {
        return new MapFileData
        {
            ModMapFile = ModMapFile,
            MapRanges = MapRanges
        };
    }

    public void SaveToFile(string path)
    {
        if (ModMapFile != null && ModMapData != null)
        {
            var modMapFile = new JsonFile<ModMapData>(Helpers.GetModMapDataPath(ModMapFile))
            {
                Data = ModMapData
            };
            modMapFile.Save();
        }

        var mapFile = new JsonFile<MapFileData>(path)
        {
            Data = ToFileData()
        };
        mapFile.Save();
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

        var modMapFile = new JsonFile<ModMapData>(Helpers.GetModMapDataPath(file!));
        ModMapData = modMapFile.Data;
    }
}
