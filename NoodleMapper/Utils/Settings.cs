using System.IO;
using SimpleJSON;

namespace NoodleMapper.Utils;

public class Settings {
    public static JSONNode? Get(string name, JSONNode? defaultValue = null) {
        var o = Data.GetNode(Instance.m_json, name);
        if (o == null && defaultValue != null) {
            o = Set(name, defaultValue);
        }
        return o;
    }
	
    public static JSONNode Set(string name, JSONNode value) {
        Data.SetNode(Instance.m_json, name, value);
        Instance.Save();
        return value;
    }
	
    public static void Reload() {
        s_instance = new Settings();
    }
	
    public readonly string SettingsFile = UnityEngine.Application.persistentDataPath + "/NoodleMapper.json";
	
    private static Settings? s_instance = null;
    private static Settings Instance {
        get {
            if (s_instance == null) {
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
	
    private void Save() {
        File.WriteAllText(SettingsFile, m_json.ToString(4));
    }
}