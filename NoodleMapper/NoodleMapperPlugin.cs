using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Beatmap.Enums;
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