using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Beatmap.Enums;
using HarmonyLib;
using NoodleMapper.UI;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using NoodleMapper.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NoodleMapper;

[Plugin("NoodleMapper")]
public class NoodleMapperPlugin
{
    private CMSceneIndex m_currentScene;
    [Init]
    private void Init()
    {
        new Harmony("com.Vainstains.NoodleMapper")
            .PatchAll(Assembly.GetExecutingAssembly());
        
        Globals.Load();

        SceneManagers.Register<SongEditorManager>().ForScene(CMSceneIndex.SongEditMenu);
        SceneManagers.Register<EditorManager>().ForScene(CMSceneIndex.Mapper);
        
        SceneManager.sceneLoaded += SceneLoaded;
    }
    
    private async void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var cmScene = (CMSceneIndex)scene.buildIndex;
        
        if (mode == LoadSceneMode.Additive && cmScene == CMSceneIndex.Options)
            return;
        
        m_currentScene = cmScene;
        // await Task.Delay(500);
        
        WindowContainer.EnsureContainerExists(cmScene);
        SceneManagers.SceneLoaded(scene);
    }
}