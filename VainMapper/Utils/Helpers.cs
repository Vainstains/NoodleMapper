using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;
using VainLib.Data;

namespace VainMapper.Utils;

public static class Helpers
{
    public const string CurrentPluginName = "VainMapper";
    public const string LegacyPluginName = "NoodleMapper";
    
    public static string GetPersistentDataPath(string currentFileName, string legacyFileName)
    {
        var currentPath = Path.Combine(UnityEngine.Application.persistentDataPath, currentFileName);
        var legacyPath = Path.Combine(UnityEngine.Application.persistentDataPath, legacyFileName);

        if (!File.Exists(currentPath) && File.Exists(legacyPath))
        {
            WriteAllText(currentPath, ReadAllTextOrEmpty(legacyPath));
            File.Delete(legacyPath);
        }

        return currentPath;
    }

    public static string GetPersistentDataPath(string fileName)
    {
        return Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
    }
    public static JSONObject LoadJSONFile(string path)
    {
        return LoadJSONFileRaw(ReadAllTextOrEmpty(path));
    }
    public static JSONObject LoadJSONFileRaw(string contents)
    {
        try
        {
            var json = JSON.Parse(contents).AsObject ?? new JSONObject();
            return json;
        }
        catch (System.Exception) { }
        return new JSONObject();
    }

    public static string MapDir => BeatSaberSongContainer.Instance.Info.Directory;
    public static string CurrentMapDataDir => Path.Combine(MapDir, CurrentPluginName);
    public static string LegacyMapDataDir => Path.Combine(MapDir, LegacyPluginName);
    public static string MapperDataDir
    {
        get
        {
            EnsureMapDataMigrated();
            return CurrentMapDataDir;
        }
    }

    public static string GetMapDataPath(string name) =>
        Path.Combine(MapperDataDir, $"{name}.map");
    public static string GetModMapDataPath(string name) =>
        Path.Combine(MapperDataDir, $"{name}.modmap");
    public static IEnumerable<string> GetMapDataNames()
    {
        if (!Directory.Exists(MapperDataDir))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(MapperDataDir, "*.map")
            .Select(path => Path.GetFileNameWithoutExtension(path));
    }

    public static IEnumerable<string> GetModMapDataNames()
    {
        if (!Directory.Exists(MapperDataDir))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(MapperDataDir, "*.modmap")
            .Select(path => Path.GetFileNameWithoutExtension(path));
    }

    public static bool TryGetMapFile(JSONNode customData, out string mapFile)
    {
        if (customData.TryGetString(JsonKeys.MapFile, out mapFile))
            return true;

        if (!customData.TryGetString(JsonKeys.LegacyMapFile, out mapFile))
            return false;

        SetMapFile(customData, mapFile);
        return true;
    }

    public static void SetMapFile(JSONNode customData, string mapFile)
    {
        customData[JsonKeys.MapFile] = mapFile;
        customData.Remove(JsonKeys.LegacyMapFile);
    }

    public static void RemoveMapFile(JSONNode customData)
    {
        customData.Remove(JsonKeys.MapFile);
        customData.Remove(JsonKeys.LegacyMapFile);
    }
    
    public static string ReadAllTextOrEmpty(string path)
    {
        if (File.Exists(path))
            return File.ReadAllText(path);
        return string.Empty;
    }

    public static void WriteAllText(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
    }

    private static void EnsureMapDataMigrated()
    {
        if (!Directory.Exists(LegacyMapDataDir))
        {
            if (!Directory.Exists(CurrentMapDataDir))
                Directory.CreateDirectory(CurrentMapDataDir);
            return;
        }

        Directory.CreateDirectory(CurrentMapDataDir);

        foreach (var legacyPath in Directory.GetFiles(LegacyMapDataDir))
        {
            var currentPath = Path.Combine(CurrentMapDataDir, Path.GetFileName(legacyPath));
            if (File.Exists(currentPath))
                continue;

            File.Move(legacyPath, currentPath);
        }

        if (!Directory.EnumerateFileSystemEntries(LegacyMapDataDir).Any())
            Directory.Delete(LegacyMapDataDir, false);
    }
    
    public static void AskYesNo(this PersistentUI ui, string title, string message, Action onYes)
    {
        DialogBox dialog = ui.CreateNewDialogBox().WithTitle(title);
        dialog.AddComponent<TextComponent>()
            .WithInitialValue(message);
        dialog.AddFooterButton(() => { dialog.Close(); }, "Cancel");
        dialog.AddFooterButton(() =>
        {
            onYes?.Invoke();
            dialog.Close();
        }, "Confirm");
        
        dialog.Open();
    }
    
    public static void ShowMessage(this PersistentUI ui, string message)
    {
        ui.ShowDialogBox(message, _ => { }, PersistentUI.DialogBoxPresetType.Ok);
    }
    
    public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest) {
        first = list.Count > 0 ? list[0] : default(T); // or throw
        rest = list.Skip(1).ToList();
    }

    public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest) {
        first = list.Count > 0 ? list[0] : default(T); // or throw
        second = list.Count > 1 ? list[1] : default(T); // or throw
        rest = list.Skip(2).ToList();
    }
}
