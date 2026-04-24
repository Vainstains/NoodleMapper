using System;
using System.Collections.Generic;
using System.IO;
using Beatmap.Base;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;
using VainLib.Scenes;
using VainMapper.Managers.Windows;
using VainMapper.Map;
using VainMapper.ModMap;
using VainMapper.UI;
using VainMapper.Utils;
using Object = UnityEngine.Object;

namespace VainMapper.Managers;

public class EditorManager : ManagerBehaviour<EditorManager>
{
    private AudioTimeSyncController m_atsc;

    private NoteGridContainer m_noteGridContainer;
    private ArcGridContainer m_arcGridContainer;
    private ChainGridContainer m_chainGridContainer;
    private ObstacleGridContainer m_obstacleGridContainer;
    private EventGridContainer m_eventGridContainer;
    private CustomEventGridContainer m_customEventGridContainer;
    private SelectionController m_selectionController;

    public AudioTimeSyncController Atsc => m_atsc;

    public NoteGridContainer NoteGridContainer => m_noteGridContainer;
    public ArcGridContainer ArcGridContainer => m_arcGridContainer;
    public ChainGridContainer ChainGridContainer => m_chainGridContainer;
    public ObstacleGridContainer ObstacleGridContainer => m_obstacleGridContainer;
    public EventGridContainer EventGridContainer => m_eventGridContainer;
    public CustomEventGridContainer CustomEventGridContainer => m_customEventGridContainer;
    public SelectionController SelectionController => m_selectionController;

    public MapData? Map { get; private set; }
    public static bool NMEnabled
    {
        get
        {
            var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
            return Helpers.TryGetMapFile(info.CustomData, out _);
        }
    }

    protected override void PostInit()
    {
        Map = TryLoadCurrentMapData();

        Events.ExtensionButtonClicked.AddListener(EditorMainWindow.ToggleUI);

        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += DiffChanged;
        EditorPatches.OnSavingDiff += Save;
    }

    public void EnsureMapLoaded()
    {
        Map ??= TryLoadCurrentMapData();
    }

    public static MapData? TryLoadCurrentMapData()
    {
        var info = BeatSaberSongContainer.Instance?.MapDifficultyInfo;
        if (info == null)
            return null;

        if (!Helpers.TryGetMapFile(info.CustomData, out var mapFile))
            return null;

        return MapData.LoadFromFile(Helpers.GetMapDataPath(mapFile));
    }

    private void Start()
    {
        m_atsc = Object.FindObjectOfType<AudioTimeSyncController>();

        m_noteGridContainer = Object.FindObjectOfType<NoteGridContainer>();
        m_arcGridContainer = Object.FindObjectOfType<ArcGridContainer>();
        m_chainGridContainer = Object.FindObjectOfType<ChainGridContainer>();
        m_obstacleGridContainer = Object.FindObjectOfType<ObstacleGridContainer>();
        m_eventGridContainer = Object.FindObjectOfType<EventGridContainer>();
        m_customEventGridContainer = Object.FindObjectOfType<CustomEventGridContainer>();

        m_selectionController = Object.FindObjectOfType<SelectionController>();

        ApplyActiveModMap();
        EditorGridAndTrackController.Instance.RefreshGridStuff();
    }

    private void OnDisable()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= DiffChanged;
        EditorPatches.OnSavingDiff -= Save;
    }

    private void DiffChanged()
    {
        ResetFresh();
    }

    private void Save()
    {
        ApplyActiveModMap();
        
        var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        if (Helpers.TryGetMapFile(info.CustomData, out var mapFile) && Map != null)
            Map.SaveToFile(Helpers.GetMapDataPath(mapFile));
    }
    
    
    private HashSet<BaseObject> m_alreadyHitWithProcessing = new();

    public void ApplyActiveModMap()
    {
        if (Map?.ModMapData == null || m_noteGridContainer == null || m_arcGridContainer == null
            || m_chainGridContainer == null || m_obstacleGridContainer == null || m_eventGridContainer == null)
            return;

        foreach (var obj in EnumerateEditableObjects())
        {
            obj.SetCustomData(null);
            obj.RefreshCustom();
        }
        foreach (var span in Map.ModMapData.EffectSpans)
        {
            foreach (var obj in GetObjectsMatchingSpan(span))
            {
                EnsureCustomDataObject(obj);
                
                foreach (var processor in span.Processors)
                {
                    try
                    {
                        processor.Process(obj);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"ModMap processor {processor.GetType().Name} failed on {obj.GetType().Name}: {ex}");
                    }
                }
                
                obj.RefreshCustom();
            }
        }

        if (EditorModMapSpanWindow.IsOpen)
            PreviewSelectedModMapSpan();
    }

    private static JSONObject EnsureCustomDataObject(BaseObject obj)
    {
        if (obj.CustomData is JSONObject objectNode)
            return objectNode;

        objectNode = new JSONObject();
        obj.CustomData = objectNode;
        return objectNode;
    }

    public void PreviewSelectedModMapSpan()
    {
        if (OutlineManager.Instance == null || m_noteGridContainer == null || m_arcGridContainer == null
            || m_chainGridContainer == null || m_obstacleGridContainer == null || m_eventGridContainer == null)
            return;

        OutlineManager.Instance.ClearAllOutlines();

        if (!EditorModMapSpanWindow.IsOpen || !ModMapEditorContext.TryGetSelectedSpan(out var span))
        {
            OutlineManager.Instance.RefreshAllOutlines();
            return;
        }

        foreach (var obj in GetObjectsMatchingSpan(span))
            OutlineManager.Instance.SetOutline(obj, new Color(0.95f, 0.65f, 0.25f, 1f));

        OutlineManager.Instance.RefreshAllOutlines();
    }

    private IEnumerable<BaseObject> GetObjectsMatchingSpan(NoodleSpan span)
    {
        foreach (var obj in EnumerateEditableObjects())
        {
            if (!IsWithinSpan(obj, span))
                continue;

            var passesFilters = true;
            foreach (var filter in span.Filters)
            {
                if (filter == null || !filter.TestAgainst(obj))
                {
                    passesFilters = false;
                    break;
                }
            }

            if (passesFilters)
                yield return obj;
        }
    }

    public IEnumerable<BaseObject> EnumerateEditableObjects()
    {
        foreach (var obj in m_noteGridContainer.MapObjects)
            yield return obj;
        foreach (var obj in m_arcGridContainer.MapObjects)
            yield return obj;
        foreach (var obj in m_chainGridContainer.MapObjects)
            yield return obj;
        foreach (var obj in m_obstacleGridContainer.MapObjects)
            yield return obj;
        foreach (var obj in m_eventGridContainer.MapObjects)
            yield return obj;
    }

    private static bool IsWithinSpan(BaseObject obj, NoodleSpan span)
    {
        var endBeat = span.StartBeat + span.Duration;
        return obj.JsonTime >= span.StartBeat && obj.JsonTime <= endBeat;
    }

    private void ExampleNoteProcessor()
    {
        var map = BeatSaberSongContainer.Instance.Map;
        Debug.LogError(map.DirectoryAndFile);

        var notes = map.Notes.ToArray();

        foreach (var o in notes)
        {
            var orig = BeatmapFactory.Clone(o);

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(o.ObjectType);

            collection.DeleteObject(o, false, false, "", true, false);

            o.CustomData[o.CustomKeyNoteJumpMovementSpeed] = 69;
            o.RefreshCustom();

            collection.SpawnObject(o, false, true);
        }

        BeatmapObjectContainerCollection.RefreshAllPools();
    }

    public static void DeleteModmap(string name)
    {
        var path = Helpers.GetModMapDataPath(name);
        if (File.Exists(path))
            File.Delete(path);

        if (Instance == null)
            return;

        if (Instance.Map != null)
        {
            var map = Instance.Map;

            if (map.ModMapFile == name)
                map.SetModMapFile(null);
        }
    }

    public static void CreateModmap(string name)
    {
        var path = Helpers.GetModMapDataPath(name);
        var modMapFile = new VainLib.IO.JsonFile<ModMapData>(path)
        {
            Data = new ModMapData()
        };
        modMapFile.Save();
    }
}
