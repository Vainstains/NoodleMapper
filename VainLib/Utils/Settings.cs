using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VainLib.Data;
using VainLib.IO;

namespace VainLib.Utils;

public class Settings
{
    private static readonly JsonFile<Settings> s_instance = new(PersistentData.GetPath("VainLib.json"));
    public static void Reload() => s_instance.Reload();
    public static void Save() => s_instance.Save();


    public Dictionary<string, Vector4> windows = new();
    public static IDictionary<string, Vector4> Windows => s_instance.Data.windows;
}

public static class PersistentData
{
    private static readonly string s_persistentDataPath = Application.persistentDataPath;

    public static string GetPath(string path)
    {
        return Path.Combine(s_persistentDataPath, path);
    }
}
