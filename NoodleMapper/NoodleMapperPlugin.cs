using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Beatmap.Enums;
using HarmonyLib;
using NoodleMapper.UI;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using NoodleMapper.Managers;
using NoodleMapper.Utils.Scenes;
using NoodleMapper.Wiring;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NoodleMapper;

[Plugin("NoodleMapper")]
public class NoodleMapperPlugin
{
    [Init]
    private void Init()
    {
        new Harmony("com.Vainstains.NoodleMapper")
            .PatchAll(Assembly.GetExecutingAssembly());
        
        var allStaticInitMethods = typeof(NoodleMapperPlugin).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.IsStatic&& m.GetCustomAttribute(typeof(OnPluginInitAttribute), false) != null);

        foreach (var method in allStaticInitMethods)
        {
            method.Invoke(null, null);
        }

        SceneManagers.Register<SongEditorManager>().ForScene(CMScene.SongEditMenu);
        SceneManagers.Register<EditorManager>().ForScene(CMScene.Mapper);
    }
}