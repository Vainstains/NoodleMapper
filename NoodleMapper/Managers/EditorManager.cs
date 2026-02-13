using System;
using System.Collections.Generic;
using System.IO;
using Beatmap.Base;
using Beatmap.Helper;
using HarmonyLib;
using NoodleMapper.EditorThings;
using NoodleMapper.Managers.Windows;
using NoodleMapper.Map;
using NoodleMapper.ModMap;
using NoodleMapper.UI;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using SimpleJSON;
using UnityEngine;

namespace NoodleMapper.Managers;

public class EditorManager : ManagerBehaviour<EditorManager>
{
    public MapData? Map { get; private set; } = null;
    
    protected override void PostInit()
    {
        var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        if (info.CustomData.TryGetString(JsonKeys.MapFile, out var mapFile))
        {
            var json = Helpers.LoadJSONFile(Helpers.GetMapDataPath(mapFile));
            Map = MapData.FromJSON(json);
        }
        
        Globals.Events.ExtensionButtonClicked.AddListener(EditorMainWindow.ToggleUI);
        
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += DiffChanged;
        EditorPatches.OnSavingDiff += Save;
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
        var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        
        if (info.CustomData.TryGetString(JsonKeys.MapFile, out var mapFile) && Map != null)
        {
            Helpers.WriteAllText(Helpers.GetMapDataPath(mapFile), Map.ToJSON().ToString(4));
        }
    }

    private void ExampleNoteProcessor()
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

    public static void DeleteModmap(string name)
    {
        var path = Helpers.GetModMapDataPath(name);
        if (File.Exists(path))
            File.Delete(path);

        if (Instance == null)
            return;

        if (Instance.Map != null)
        {
            MapData map = Instance.Map;

            if (map.ModMapFile == name)
            {
                map.SetModMapFile(null);
            }
        }
    }

    public static void CreateModmap(string name)
    {
        var path = Helpers.GetModMapDataPath(name);
        Helpers.WriteAllText(path, "{}");
    }
}

public class EditorGridAndTrackController : ManagerBehaviour<EditorGridAndTrackController>
{
    private RangeBarController m_rangeBarController = null!;
    public void RefreshGridStuff()
    {
        m_rangeBarController.RefreshPositions();
    }
    protected override void PostInit()
    {
        m_rangeBarController = RangeBarController.Create();
    }

    public void SetRanges(IEnumerable<RawMapRange> ranges)
    {
        m_rangeBarController.UpdateRanges(ranges);
    }

    public void ClearRanges()
    {
        m_rangeBarController.UpdateRanges([]);
    }
}

[HarmonyPatch]
class EditorPatches
{
    public static event Action OnSavingDiff;
    
    [HarmonyPatch(typeof(BaseDifficulty), nameof(BaseDifficulty.Save))]
    [HarmonyPrefix]
    static void Save()
    {
        OnSavingDiff?.Invoke();
    }

    [HarmonyPatch(typeof(BPMChangeGridContainer), nameof(BPMChangeGridContainer.RefreshModifiedBeat))]
    [HarmonyPostfix]
    static void BPMRefresh()
    {
        EditorGridAndTrackController.Instance.RefreshGridStuff();
    }
}