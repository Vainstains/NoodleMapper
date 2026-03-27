using System.IO;
using Beatmap.Helper;
using VainMapper.ModMap;
using SimpleJSON;
using UnityEngine;
using VainLib.Scenes;
using VainMapper.Managers.Windows;
using VainMapper.Map;
using VainMapper.UI;
using VainMapper.Utils;
using Object = UnityEngine.Object;

namespace VainMapper.Managers;

public class EditorManager : ManagerBehaviour<EditorManager>
{
    private AudioTimeSyncController m_atsc;
    public AudioTimeSyncController Atsc => m_atsc;
    public MapData? Map { get; private set; } = null;
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
        var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        if (Helpers.TryGetMapFile(info.CustomData, out var mapFile))
        {
            Map = MapData.LoadFromFile(Helpers.GetMapDataPath(mapFile));
        }
        
        Events.ExtensionButtonClicked.AddListener(EditorMainWindow.ToggleUI);
        
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += DiffChanged;
        EditorPatches.OnSavingDiff += Save;
    }

    private void Start()
    {
        m_atsc = Object.FindObjectOfType<AudioTimeSyncController>();
        
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
        var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        
        if (Helpers.TryGetMapFile(info.CustomData, out var mapFile) && Map != null)
        {
            Map.SaveToFile(Helpers.GetMapDataPath(mapFile));
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
        var modMapFile = new VainLib.IO.JsonFile<ModMapData>(path)
        {
            Data = new ModMapData()
        };
        modMapFile.Save();
    }
}