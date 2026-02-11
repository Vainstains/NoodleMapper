using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Beatmap.Enums;
using Beatmap.Helper;
using NoodleMapper.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoodleMapper;

[Plugin("NoodleMapper")]
public class NoodleMapperPlugin
{
    private const int EditorSceneBuildIndex = 3;
    private Scene? m_currentScene;
        
    [Init]
    private void Init()
    {
        SceneManager.sceneLoaded += SceneLoaded;
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += LoadedDifficultyChanged;

        var iconSprite = Helpers.LoadSprite("ExtensionButtonIcon.png");
        ExtensionButtons.AddButton(iconSprite, "This is the tooltip", OnExtensionButtonClick);
    }

    private void OnExtensionButtonClick()
    {
        NoodleMapperManager.Instance?.OnExtensionButtonClicked();
    }

    private void LoadedDifficultyChanged()
    {
        if (m_currentScene != null)
            SceneLoaded(m_currentScene.Value, LoadSceneMode.Single);
    }
    private async void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        m_currentScene = scene;
        await Task.Delay(500);
            
        GameObject managerObject = new GameObject(nameof(NoodleMapperManager));
        SceneManager.MoveGameObjectToScene(managerObject, scene);

        managerObject.AddInitComponent<NoodleMapperManager>();
    }
}

public class NoodleMapperManager : MonoBehaviour
{
    private static NoodleMapperManager? s_instance;
        
    public static NoodleMapperManager? Instance => s_instance;

    private void Init()
    {
        if (s_instance)
            Destroy(s_instance!.gameObject);
            
        s_instance = this;
        
        var map = BeatSaberSongContainer.Instance.Map;
        if (!map.CustomData.HasKey("noodleMapper"))
        {
            
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