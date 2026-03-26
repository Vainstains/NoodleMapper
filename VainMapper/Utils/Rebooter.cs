using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Beatmap.Info;
using VainMapper.Wiring;
using SimpleJSON;
using UnityEngine;
using VainMapper.Managers;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace VainMapper.Utils;

public static class Rebooter
{
    private static readonly string RebootFile = Helpers.GetPersistentDataPath("VainMapperReboot.json", "NoodleMapperReboot.json");
    
    static bool DidJustReboot(out JSONNode rebootJson)
    {
        if (!File.Exists(RebootFile))
        {
            rebootJson = new JSONObject();
            return false;
        }
        
        rebootJson = Helpers.LoadJSONFile(RebootFile);
        return true;
    }
    
    public static void HandleReboot()
    {
        var didReboot = DidJustReboot(out var rebootJson);
        if (!didReboot)
        {
            Debug.Log("Reboot: Not rebooting");
            return;
        }

        string? location = rebootJson.GetValueOrDefault("rebootingFrom", "none");
        
        if (location == null || location == "none")
        {
            FinishReboot();
            return;
        }

        Debug.Log($"Reboot: Attempting reboot into {location}");

        if (location == "song")
        {
            var dir = rebootJson["mapDirectory"];
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            if (!dirInfo.Exists)
            {
                Debug.LogError($"Reboot: Map directory does not exist! {dir}");
                return;
            }
            Debug.Log($"Reboot: loading {dirInfo.FullName}");
            var mapInfo = BeatSaberSongUtils.GetInfoFromFolder(dirInfo.FullName);
            BeatSaberSongContainer.Instance.SelectSongForEditing(mapInfo);
        }
        else if (location == "mapper")
        {
            var dir = rebootJson["mapDirectory"];
            var targetDiff = rebootJson["difficulty"];
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            if (!dirInfo.Exists)
            {
                Debug.LogError($"Reboot: Map directory does not exist! {dir}");
                return;
            }
            Debug.Log($"Reboot: loading {dirInfo.FullName}");
            var mapInfo = BeatSaberSongUtils.GetInfoFromFolder(dirInfo.FullName);
            new GameObject("Mapper Reboot Handler").AddComponent<MapperRebootHandler>().time =
                rebootJson["time"];
            BeatSaberSongContainer.Instance.SelectSongForEditing(mapInfo);
        }
        else
        {
            FinishReboot();
        }
    }

    static JSONNode BuildRebootJson()
    {
        var rebootJson = new JSONObject();
        
        var songContainer = BeatSaberSongContainer.Instance;
        var editorManager = EditorManager.Instance;

        if (SongSelectManager.Instance != null)
        {
            rebootJson["rebootingFrom"] = "song";
            rebootJson["mapDirectory"] = Helpers.MapDir;
            return rebootJson;
        }
        
        if (editorManager != null)
        {
            var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
            rebootJson["rebootingFrom"] = "mapper";
            rebootJson["mapDirectory"] = Helpers.MapDir;
            var fullName = $"{info.Difficulty}{info.Characteristic}";
            rebootJson["time"] = editorManager.Atsc.CurrentJsonTime;
            return rebootJson;
        }
        
        rebootJson["rebootingFrom"] = "none";
        return rebootJson;
    }

    public static void Reboot()
    {
        var json = BuildRebootJson();
        File.WriteAllText(RebootFile, json.ToString(4));

        var saver = Object.FindObjectOfType<AutoSaveController>();
        if (saver != null)
            saver.Save();

        SpawnChromapper();
        Application.Quit();
    }

    public static void FinishReboot()
    {
        if (File.Exists(RebootFile))
            File.Delete(RebootFile);
    }

    static void SpawnChromapper()
    {
        Process.Start(Path.GetDirectoryName(Application.dataPath) + "/" + 
                      Path.GetFileName(Application.dataPath).Replace("_Data", ".exe"));
    }
    
    private class MapperRebootHandler : MonoBehaviour
    {
        public float time;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(RebootIntoMapper());
        }

        private IEnumerator RebootIntoMapper()
        {
            SongInfoEditUI? songInfoEditUI = null;
            bool waiting = true;
            yield return new WaitForSeconds(0.2f);
            while (waiting)
            {
                songInfoEditUI = FindObjectOfType<SongInfoEditUI>();
                if (songInfoEditUI != null)
                    waiting = false;
                yield return new WaitForSeconds(0.15f);
            }
            waiting = true;
            while (waiting)
            {
                if (songInfoEditUI.previewAudio.clip != null && !(BeatSaberSongContainer.Instance.MapDifficultyInfo == null || PersistentUI.Instance.DialogBoxIsEnabled))
                    waiting = false;
                yield return new WaitForSeconds(0.15f);
            }
            yield return new WaitForSeconds(0.1f);
            
            songInfoEditUI.EditMapButtonPressed();

            waiting = true;
            while (waiting)
            {
                if (EditorManager.Instance != null)
                    waiting = false;
                yield return new WaitForSeconds(0.1f);
            }
            
            EditorManager.Instance.Atsc.MoveToJsonTime(time);
            
            FinishReboot();
            
            Destroy(gameObject);
        }
    }
}
