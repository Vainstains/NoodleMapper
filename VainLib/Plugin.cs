using System.Reflection;
using UnityEngine;
using HarmonyLib;
using UnityEngine.SceneManagement;
using VainLib.Data;
using VainLib.Scenes;

namespace VainLib;

[Plugin(Name)]
public class Plugin
{
    public const string Author = "Vainstains";
    public const string Name = "VainLib";
    public const string ID = $"com.{Author}.{Name}";

    [Init]
    private void Init()
    {
        new Harmony(ID)
            .PatchAll(Assembly.GetExecutingAssembly());

        SceneManagers.InitializeSceneStuff();
        Testing.Test();
    }
}