using System.Reflection;
using UnityEngine;
using HarmonyLib;
using UnityEngine.SceneManagement;
using VainLib.Data;

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

        Debug.Log($"Hello from {Name}!");
        Testing.Test();
    }
}

public enum CMScene
{
    FirstBoot = 0,
    SongSelectMenu = 1,
    SongEditMenu = 2,
    Mapper = 3,
    Options = 4
}

public static class CMSceneExtensions
{
    public static Scene GetUnityScene(this CMScene scene) => SceneManager.GetSceneByBuildIndex((int)scene);
}