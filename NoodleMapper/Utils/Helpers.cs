using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;

namespace NoodleMapper.Utils;

public static class Helpers
{
    public static Sprite LoadSprite(string asset) {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"NoodleMapper.Resources.{asset}");
        byte[] data = new byte[stream!.Length];
        stream.Read(data, 0, (int)stream.Length);
		
        Texture2D texture2D = new Texture2D(256, 256);
        texture2D.LoadImage(data);
		
        return Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 100.0f);
    }

    public static Sprite LoadSprite(string asset, int edgePx)
    {
        var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"NoodleMapper.Resources.{asset}");

        byte[] data = new byte[stream!.Length];
        stream.Read(data, 0, (int)stream.Length);

        Texture2D texture2D = new Texture2D(2, 2);
        texture2D.LoadImage(data);

        var border = new Vector4(edgePx, edgePx, edgePx, edgePx);

        return Sprite.Create(
            texture2D,
            new Rect(0, 0, texture2D.width, texture2D.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border
        );
    }
    
    public static Sprite LoadSprite(string asset, float edgePct)
    {
        var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"NoodleMapper.Resources.{asset}");

        byte[] data = new byte[stream!.Length];
        stream.Read(data, 0, (int)stream.Length);

        Texture2D texture2D = new Texture2D(2, 2);
        texture2D.LoadImage(data);
        
        int edgePx = (int)(edgePct * texture2D.width);
        var border = new Vector4(edgePx, edgePx, edgePx, edgePx);

        return Sprite.Create(
            texture2D,
            new Rect(0, 0, texture2D.width, texture2D.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border
        );
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
    public static string NoodleMapperDataDir => Path.Combine(MapDir, "NoodleMapper");
    public static string GetMapDataPath(string name) =>
        Path.Combine(NoodleMapperDataDir, $"{name}.map");
    public static string GetModMapDataPath(string name) =>
        Path.Combine(NoodleMapperDataDir, $"{name}.modmap");
    public static IEnumerable<string> GetMapDataNames() =>
        Directory.GetFiles(NoodleMapperDataDir, "*.map")
            .Select(path => Path.GetFileNameWithoutExtension(path));
    public static IEnumerable<string> GetModMapDataNames() =>
        Directory.GetFiles(NoodleMapperDataDir, "*.modmap")
            .Select(path => Path.GetFileNameWithoutExtension(path));

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
}