using System;
using System.IO;
using TMPro;
using UnityEngine;
using VainLib.Scenes;
using VainLib.UI.Components;
using VainLib.Utils;
using VainMapper.Managers.Windows;
using VainMapper.Utils;

namespace VainMapper.Managers;

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
        
        if (Helpers.TryGetMapFile(info.CustomData, out var mapFile))
        {
            if (string.IsNullOrEmpty(mapFile))
            {
                Helpers.RemoveMapFile(info.CustomData);
            }
            else
            {
                var correctName = $"{info.Difficulty}{info.Characteristic}";
                Debug.Log($"Correct name: {correctName}");
                var correctPath = Helpers.GetMapDataPath(correctName);
                var storedPath = Helpers.GetMapDataPath(mapFile);

                var contents = Helpers.ReadAllTextOrEmpty(storedPath);
                Helpers.WriteAllText(correctPath, contents);
                Helpers.SetMapFile(info.CustomData, correctName);
            }
        }
    }

    private void ModifyUI()
    {
        var container = GameObject.Find("SongInfoPanel").RequireComponent<RectTransform>();

        // ── VainMapper button (rightmost) ───────────────────────────────────
        var nmButtonRect = container.AddChildBottomRight().IgnoreLayout().Move(-0.5f, 0.5f)
            .ExtendTop(17).ExtendLeft(90);
        nmButtonRect.AddInitComponent<NoodleButton>(new Color(0.4f, 0.4f, 0.4f), () =>
        {
            SongEditorWindow.ToggleUI();
        }).Content.AddLabel("VainMapper", TextAlignmentOptions.Center, fontSize: 14);

        // ── Git button (immediately to the left, 4 px gap) ──────────────────
        // NM button occupies x: [-92 .. -2], so Git button starts at -96 and extends left by 46.
        var gitButtonRect = container.AddChildBottomRight().IgnoreLayout().Move(-0.5f, 17.5f)
            .ExtendTop(17).ExtendLeft(46);
        gitButtonRect.AddInitComponent<NoodleButton>(new Color(0.3f, 0.35f, 0.5f), () =>
        {
            GitWindow.ToggleUI();
        }).Content.AddLabel("Git", TextAlignmentOptions.Center, fontSize: 14);
    }
}
