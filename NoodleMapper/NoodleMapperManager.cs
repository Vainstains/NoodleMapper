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
            var filterNode = filters[i];
            
            var filter = ParseFilter(filterNode);
        }

        return span;
    }

    private static INoodleFilter ParseFilter(JSONNode filterNode)
    {
        string type = filterNode.GetValueOrDefault("type", "");
        
        if (string.IsNullOrEmpty(type))
            return new UnknownFilter(filterNode);

        switch (type)
        {
            case "HandType":
                return HandTypeFilter.FromJSONNode(filterNode);
        }
        
        return new UnknownFilter(filterNode);
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

public class UnknownFilter : INoodleFilter
{
    public JSONNode Node { get; }
    public bool TestAgainst(BaseObject obj) => true;

    public UnknownFilter(JSONNode node)
    {
        Node = node;
    }

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

    public static HandTypeFilter FromJSONNode(JSONNode node)
    {
        var filter = new HandTypeFilter();
        filter.Hand = (HandType)(int)node.GetValueOrDefault("hand", 0);
        return filter;
    }
}