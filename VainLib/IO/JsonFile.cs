using System;
using System.IO;
using SimpleJSON;
using UnityEngine;
using VainLib.Data;

namespace VainLib.IO;

public class JsonFile<T>
{
    public string FilePath { get; }
    public T Data { get; set; }

    public JsonFile(string filePath)
    {
        FilePath = filePath;
        Reload();
        if (Data == null)
        {
            Debug.LogWarning($"File {FilePath} does not exist, loading a blank object.");
            Data = Activator.CreateInstance<T>();
        }
    }

    public void Reload()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogWarning($"File {FilePath} does not exist, loading a blank object.");
            Data = Activator.CreateInstance<T>();
            return;
        }
        var text = File.ReadAllText(FilePath);
        var json = JSON.Parse(text);
        Data = JSONReflector.ToObject<T>(json) ?? Activator.CreateInstance<T>();
    }

    public void SaveAs(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JSONReflector.ToJSON(Data);
        var text = json.ToString(4);
        File.WriteAllText(filePath, text);
    }

    public void Save()
    {
        SaveAs(FilePath);
    }
}
