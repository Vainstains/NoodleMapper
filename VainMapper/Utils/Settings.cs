using System.Collections.Generic;
using UnityEngine;
using VainLib.Data;
using VainLib.IO;

namespace VainMapper.Utils;

public class Settings
{
    /*
    public static JSONNode? Get(string name, JSONNode? defaultValue = null)
    {
        if (Instance.m_json.HasKey(name))
            return Instance.m_json[name];
        if (defaultValue != null)
            return Set(name, defaultValue);
        return defaultValue;
    }
	
    public static JSONNode Set(string name, JSONNode value)
    {
        Instance.m_json[name] = value;
        Instance.Save();
        return value;
    }
	
    public static void Reload()
    {
        s_instance = new Settings();
    }
	
    public readonly string SettingsFile = Helpers.GetPersistentDataPath("VainMapper.json", "NoodleMapper.json");
	
    private static Settings? s_instance = null;
    private static Settings Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = new Settings();
            }
            return s_instance;
        }
    }
	
    private JSONObject m_json;
	
    private Settings()
    {
        m_json = Helpers.LoadJSONFile(SettingsFile);
    }
	
    private void Save()
    {
        File.WriteAllText(SettingsFile, m_json.ToString(4));
    }
    */

    private static JsonFile<Settings> s_instance = new(Helpers.GetPersistentDataPath("VainMapper.json", "NoodleMapper.json"));
    public static void Reload() => s_instance.Reload();
    public static void Save() => s_instance.Save();


    public Dictionary<string, Vector4> windows = new();
    public static IDictionary<string, Vector4> Windows => s_instance.Data.windows;
}
