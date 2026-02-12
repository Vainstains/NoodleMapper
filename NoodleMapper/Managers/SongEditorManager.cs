using System;
using System.IO;
using NoodleMapper.Managers.Windows;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using TMPro;
using UnityEngine;

namespace NoodleMapper.Managers;

public class SongEditorManager : ManagerBehaviour<SongEditorManager>
{
    protected override void PostInit()
    {
        ModifyUI();
        DifficultySelectEvents.OnSavingDiff += SaveDiff;
    }
    
    private void OnDisable()
    {
        DifficultySelectEvents.OnSavingDiff -= SaveDiff;
    }

    private void SaveDiff(DifficultySettings diffSettings)
    {
        var info = diffSettings.InfoDifficulty;
        Debug.Log(info.Difficulty);
        // each difficulty should have a UNIQUE map file, if it has one at all. In the event that a difficulty
        // was copied, it will not have the correct name. My solution is to just always set the json key
        // to the correct one if it needs to be, and if it needed to be set then create the correct file as well.
        
        if (info.CustomData.TryGetString(JsonKeys.MapFile, out var mapFile))
        {
            if (string.IsNullOrEmpty(mapFile))
            {
                info.CustomData.Remove(JsonKeys.MapFile);
            }
            else
            {
                var correctName = $"{info.Difficulty}{info.Characteristic}";
                Debug.Log($"Correct name: {correctName}");
                var correctPath = Helpers.GetMapDataPath(correctName);
                var storedPath = Helpers.GetMapDataPath(mapFile);

                var contents = Helpers.ReadAllTextOrEmpty(storedPath);
                Helpers.WriteAllText(correctPath, contents);
                info.CustomData[JsonKeys.MapFile] = correctName;
            }
        }
    }

    private void ModifyUI()
    {
        var container = GameObject.Find("SongInfoPanel").RequireComponent<RectTransform>();

        var buttonRect = container.AddChildBottomRight().IgnoreLayout().Move(-2, 2)
            .ExtendTop(20).ExtendLeft(90);
        buttonRect.AddInitComponent<NoodleButton>(new Color(0.4f, 0.4f, 0.4f), () =>
        {
            SongEditorWindow.ToggleUI();
        }).Content.AddLabel("NoodleMapper", TextAlignmentOptions.Center, fontSize: 16);
    }
}

