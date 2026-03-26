using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Beatmap.Enums;
using HarmonyLib;
using VainMapper.UI;
using VainMapper.UI.Components;
using VainMapper.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VainMapper.Managers;
using VainMapper.Utils.Scenes;
using VainMapper.Wiring;
using Object = UnityEngine.Object;

namespace VainMapper;

[Plugin("VainMapper")]
public class VainMapperPlugin
{
    [Init]
    private void Init()
    {
        new Harmony("com.Vainstains.VainMapper")
            .PatchAll(Assembly.GetExecutingAssembly());
        
        var allStaticInitMethods = typeof(VainMapperPlugin).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.IsStatic&& m.GetCustomAttribute(typeof(OnPluginInitAttribute), false) != null);

        foreach (var method in allStaticInitMethods)
        {
            method.Invoke(null, null);
        }
        
        SceneManagers.Register<SongSelectManager>().ForScene(CMScene.SongSelectMenu);
        SceneManagers.Register<SongEditorManager>().ForScene(CMScene.SongEditMenu);
        SceneManagers.Register<EditorGridAndTrackController>().ForScene(CMScene.Mapper);
        SceneManagers.Register<EditorManager>().ForScene(CMScene.Mapper);
        SceneManagers.Register<RebootManager>().ForScene(CMScene.SongSelectMenu);
    }
}
