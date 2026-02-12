using System;
using System.IO;
using Beatmap.Base;
using Beatmap.Helper;
using HarmonyLib;
using NoodleMapper.Managers.Windows;
using NoodleMapper.Map;
using NoodleMapper.ModMap;
using NoodleMapper.UI;
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
        
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += ResetFresh;
        SavingPatch.OnSavingDiff += Save;
    }

    private void OnDisable()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= ResetFresh;
        SavingPatch.OnSavingDiff -= Save;
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
}

[HarmonyPatch]
class SavingPatch
{
    public static event Action OnSavingDiff;
    [HarmonyPatch(typeof(BaseDifficulty), nameof(BaseDifficulty.Save))]
    [HarmonyPrefix]
    static void Save()
    {
        OnSavingDiff?.Invoke();
    }
}