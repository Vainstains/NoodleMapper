using System;
using System.IO;
using System.Linq;
using NoodleMapper.Map;
using NoodleMapper.UI;
using NoodleMapper.UI.Components;
using NoodleMapper.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.Managers.Windows;

public class EditorMainWindow : GenericWindow<EditorMainWindow>
{
    public override string WindowName => "NoodleMapper";

    protected override void PostInit()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += DifficultyChanged;
    }

    private void OnDisable()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= DifficultyChanged;
    }

    private void DifficultyChanged()
    {
        SetUIDirty();
        EditorModMapManagerWindow.CloseUI();
    }

    protected override void BuildUI(RectTransform content)
    {
        if (EditorManager.Instance.Map == null)
        {
            EditorModMapManagerWindow.CloseUI();
            content.AddLabel("NM isn't enabled in this difficulty", 
                overflowMode: TextOverflowModes.Overflow,
                alignmentOptions: TextAlignmentOptions.Center).enableWordWrapping = true;
            return;
        }
        
        SetupScrolling(ref content);
        var layout = content.AddVertical();
        var map = EditorManager.Instance.Map;
        
        BuildModMapSelector(layout, map);
    }
    
    private void BuildModMapSelector(NoodleVerticalLayout layout, MapData map)
    {
        var modmaps = Helpers.GetModMapDataNames().ToList();
        modmaps.Insert(0, "-none-");

        var currentIdx = 0;
        if (map.ModMapFile != null)
            currentIdx = modmaps.IndexOf(map.ModMapFile);
        if (currentIdx < 0)
            currentIdx = 0;

        var (dropdownRect, moreRect) = layout.AddRow().Field("Active modmap").SplitHorizontal(1, -30);
            
        dropdownRect.InsetRight(2).AddDropdown(modmaps.ToArray())
            .SetSelectedOption(currentIdx).SetOnChange(newIdx =>
            {
                if (newIdx > 0)
                    map.SetModMapFile(modmaps[newIdx]);
                else
                    map.SetModMapFile(null);
                RebuildAll();
            });

        moreRect.AddButton("...", () =>
        {
            EditorModMapManagerWindow.ToggleUI();
        });
    }
}

public class EditorModMapManagerWindow : GenericWindow<EditorModMapManagerWindow>
{
    public override string WindowName => "Modmap files";
    
    protected override void BuildUI(RectTransform content)
    {
        SetupScrolling(ref content);
        
        var modmaps = Helpers.GetModMapDataNames().ToList();
        
        var list = content.AddList();

        foreach (var modmap in modmaps)
        {
            var itemRect = list.AddRow();
            var innerRect = itemRect.AddChild()
                .InsetLeft(4).InsetRight(4);

            var windowName = modmap;
            innerRect.AddLabel(modmap);
            innerRect.AddChild(RectTransform.Edge.Right).ExtendLeft(26).AddButton("X", () =>
            {
                PersistentUI.Instance.AskYesNo($"Delete {windowName}?", "This cannot be undone.", () =>
                {
                    EditorManager.DeleteModmap(windowName);
                    RebuildAll();
                });
            }).MainColor = new Color(0.7f, 0.1f, 0.3f);
        }

        list.AddRow().AddChild(RectTransform.Edge.Left).ExtendRight(50).AddButton("new...", AddNewModmap);
    }

    private void AddNewModmap()
    {
        PersistentUI.Instance.ShowInputBox("Name", result =>
        {
            if (string.IsNullOrEmpty(result))
            {
                PersistentUI.Instance.ShowMessage("Name cannot be empty.");
                return;
            }

            if (File.Exists(Helpers.GetModMapDataPath(result)))
            {
                PersistentUI.Instance.ShowMessage($"{result} already exists.");
                return;
            }
            
            EditorManager.CreateModmap(result);
            RebuildAll();
        });
    }
}