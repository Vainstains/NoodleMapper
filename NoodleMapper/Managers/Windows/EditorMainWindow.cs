using System;
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
        
        // testing the rearrangeable list
        var list1 = layout.AddRow(120).AddRearrangeableList();

        list1.AddItem(32);
        list1.AddItem(32);
        list1.AddItem(50);
        list1.AddItem(50);
        list1.AddItem(80);
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