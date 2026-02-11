using System.Collections.Generic;
using System.IO;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;

namespace NoodleMapper;

public class NoodleMapperManager : MonoBehaviour
{
    private static NoodleMapperManager? s_instance;
    public static NoodleMapperManager? Instance => s_instance;
    
    public NoodleMap? Map { get; private set; }

    private void Init()
    {
        if (s_instance)
            Destroy(s_instance!.gameObject);
            
        s_instance = this;
        
        var map = BeatSaberSongContainer.Instance.Map;
        if (!map.CustomData.HasKey("noodleMap"))
            map.CustomData["noodleMap"] = "map";

        if (map.CustomData.HasKey("noodleMap"))
        {
            string noodleMap = map.CustomData["noodleMap"];
            string path = $"{BeatSaberSongContainer.Instance.Info.Directory}/{noodleMap}.noodle";
            Map = new NoodleMap(path);
        }
    }

    public void OnExtensionButtonClicked()
    {
        var map = BeatSaberSongContainer.Instance.Map;
        Debug.LogError(map.DirectoryAndFile);

        var notes = map.Notes.ToArray();
        
        foreach (var o in notes) {
            var orig = BeatmapFactory.Clone(o);
            
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(o.ObjectType);
					
            collection.DeleteObject(o, false, false, "", true, false);
            
            o.CustomData[o.CustomKeyNoteJumpMovementSpeed] = 69;
            o.RefreshCustom();
					
            collection.SpawnObject(o, false, true);
        }
        
        BeatmapObjectContainerCollection.RefreshAllPools();
    }
}

public class NoodleMap
{
    public List<NoodleEffectSpan> EffectSpans { get; } = new List<NoodleEffectSpan>();
    public NoodleMap(string noodlePath)
    {
        string fileContents = "";
        if (!File.Exists(noodlePath))
        {
            File.WriteAllText(noodlePath, fileContents);
        }
        else
        {
            fileContents = File.ReadAllText(noodlePath);
        }
        
        fileContents = fileContents.Trim();
        
        if (string.IsNullOrEmpty(fileContents))
            return;
        
        
    }
}

public class NoodleEffectSpan
{
    public float StartBeat { get; set; } = 0;
    public float Duration { get; set; } = 0;
    public List<INoodleFilter> Filters { get; } = new ();

    public static NoodleEffectSpan FromJSONNode(JSONNode node)
    {
        var span = new NoodleEffectSpan();
        span.StartBeat = node.GetValueOrDefault("b", 0);
        span.Duration = node.GetValueOrDefault("d", 0);
        var filters = node.GetValueOrDefault("filters", new JSONArray());
        for (int i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];
            var type = filter.GetValueOrDefault("type", "");
        }
    }
}

public interface INoodleFilter
{
    public bool TestAgainst(BaseObject obj);
}

public enum HandType
{
    Left = 0,
    Right = 1
}

public class HandTypeFilter : INoodleFilter
{
    public HandType Hand { get; set; }

    public bool TestAgainst(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote note:
                return note.Color == (int)Hand && note.Type != (int)NoteType.Bomb;
            case BaseChain chain:
                return chain.Color == (int)Hand;
            case BaseArc arc:
                return arc.Color == (int)Hand;
        }
        return false;
    }
}