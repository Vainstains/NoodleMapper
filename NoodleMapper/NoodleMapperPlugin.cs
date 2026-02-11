using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Beatmap.Enums;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
        MainWindow.ToggleUI();
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
        WindowContainer.EnsureContainerExists();
            
        var managerObject = new GameObject(nameof(NoodleMapperManager));
        SceneManager.MoveGameObjectToScene(managerObject, scene);

        managerObject.AddInitComponent<NoodleMapperManager>();
    }
}

public class MainWindow : Window
{
    private static MainWindow? s_uiInstance;

    public static void ToggleUI()
    {
        if (s_uiInstance)
        {
            s_uiInstance.Close();
            return;
        }
        
        s_uiInstance = Window.CreateWindow<MainWindow>();
    }
}

