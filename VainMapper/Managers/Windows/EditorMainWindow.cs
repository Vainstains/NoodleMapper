using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VainMapper.Map;
using VainMapper.UI;
using VainMapper.UI.Components;
using VainMapper.Utils;
using Random = UnityEngine.Random;

namespace VainMapper.Managers.Windows;

public class EditorMainWindow : GenericWindow<EditorMainWindow>
{
    public override string WindowName => "VainMapper";
    
    protected override void PostInit()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += DifficultyChanged;
    }
    
    public static void OnToggleWindow(InputAction.CallbackContext _) {
        if (   (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
               && !CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.INodeEditorActions))
               && !NodeEditorController.IsActive) {
            ToggleUI();
        }
        else {
            Debug.LogError("Bullshit still required ;-;");
        }
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
        if (!EditorManager.NMEnabled)
        {
            EditorModMapManagerWindow.CloseUI();
            content.AddLabel("VainMapper isn't enabled in this difficulty", 
                overflowMode: TextOverflowModes.Overflow,
                alignmentOptions: TextAlignmentOptions.Center).enableWordWrapping = true;
            return;
        }
        
        SetupScrolling(ref content);
        var layout = content.AddVertical();
        content.AddSizeFitter(vertical: ContentSizeFitter.FitMode.PreferredSize);
        var map = EditorManager.Instance.Map;
        
        BuildModMapSelector(layout, map);
        
        // testing the rearrangeable list
        
        // var list1 = layout.AddRow(120).AddRearrangeableList();
        //
        // list1.AddItem(32);
        // list1.AddItem(32);
        // list1.AddItem(50);
        // list1.AddItem(50);
        // list1.AddItem(80);
        
        BuildMapRangeEditor(layout, map);
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

    private void BuildMapRangeEditor(NoodleVerticalLayout layout, MapData map)
    {
        var atsc = EditorManager.Instance.Atsc;
        var list = layout.AddRow().AddRearrangeableList();
        list.SetOnSwap((from, to) =>
        {
            var range = map.MapRanges[from];
            map.MapRanges.RemoveAt(from);
            map.MapRanges.Insert(to, range);
        });
        for (var i = 0; i < map.MapRanges.Count; i++)
        {
            var range = map.MapRanges[i];
            var itemContainer = list.AddItem();
            
            const int DesiredContentHeight = 26;
            
            var itemRect = itemContainer.Content.AddChild();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.pivot = new Vector2(0, 0.5f);
            itemRect.sizeDelta = new Vector2(0, DesiredContentHeight);
            
            var (innerItemRect, deleteRect) = itemRect.SplitHorizontal(1.0f, bias: -26);
            var (nameRect, (colorRect, (rangeRect, _))) = SplitRow(innerItemRect.InsetRight(4),
                0.6f, 0.0f, 0.4f);
            
            var colorButton = colorRect.ExtendLeft(24).Move(-2, 0).AddButton(() => {});
            colorButton.MainColor = new Color(range.Color.r, range.Color.g, range.Color.b);
            colorButton.SetOnClick(() =>
            {
                PersistentUI.Instance.DoShowColorInputBox("Range color", col =>
                {
                    if (!col.HasValue)
                        return;
                    var color = col.Value;
                    colorButton.MainColor = color;
                    range.Color = color;
                    EditorGridAndTrackController.Instance.RefreshGridStuff();
                }, colorButton.MainColor);
            });
            
            nameRect.InsetRight(8 + 26).AddTextBox().SetValue(range.Name).SetOnChange(n =>
            {
                if (n == null)
                    return;
                
                range.Name = n;
                
                // ranges can really be anything but a common use for ranges is to annotate sections of the map.
                // if the mapper is annotating *semantically*, it might help to sync the colors of like-named ranges.
                // this kinda promotes color-coding your map and makes it nicer at a glance :)
                var candidate = map.MapRanges.First(it => it.Name == n);
                if (candidate != null)
                {
                    range.Color = candidate.Color;
                    var c = candidate.Color;
                    c.a = 1.0f;
                    colorButton.MainColor = c;
                }

                EditorGridAndTrackController.Instance.RefreshGridStuff();
            });
            nameRect.AddGetBorder(RectTransform.Edge.Right).Move(4, 0);
            
            var label = rangeRect.AddLabel("-", alignmentOptions: TextAlignmentOptions.Center);
            label.text = $"{range.StartBeat} - {range.EndBeat}";

            rangeRect.AddChild(RectTransform.Edge.Left).ExtendRight(26).AddButton("=", () =>
            {
                var beat = atsc.CurrentJsonTime;
                range.StartBeat = beat;
                label.text = $"{range.StartBeat} - {range.EndBeat}";
                EditorGridAndTrackController.Instance.RefreshGridStuff();
            }).MainColor = new Color(0.3f, 0.4f, 0.6f);
            rangeRect.AddChild(RectTransform.Edge.Right).ExtendLeft(26).AddButton("=", () =>
            {
                var beat = atsc.CurrentJsonTime;
                range.EndBeat = beat;
                label.text = $"{range.StartBeat} - {range.EndBeat}";
                EditorGridAndTrackController.Instance.RefreshGridStuff();
            }).MainColor = new Color(0.3f, 0.4f, 0.6f);

            deleteRect.AddButton("X", () =>
            {
                PersistentUI.Instance.AskYesNo($"Delete {range.Name}?", "This cannot be undone.", () =>
                {
                    map.MapRanges.Remove(range);
                    EditorGridAndTrackController.Instance.RefreshGridStuff();
                    RebuildAll();
                });
            }).MainColor = new Color(0.7f, 0.1f, 0.3f);
        }
        layout.AddRow(2).AddGetBorder(RectTransform.Edge.Bottom).Move(0, 1);
        var endRow = layout.AddRow();
        endRow.AddChild(RectTransform.Edge.Left).ExtendRight(50).AddButton("new...", AddNewMapRange);
        endRow.AddChild(RectTransform.Edge.Left).ExtendRight(50).Move(52, 0).AddButton("sort", SortMapRanges);
    }

    private RectTransform[] SplitRow(RectTransform row, params float[] splitWidthsPct)
    {
        var rects = new RectTransform[splitWidthsPct.Length + 1];

        float currentX = 0;

        for (int i = 0; i < rects.Length; i++)
        {
            float width;
            if (i < splitWidthsPct.Length)
                width = splitWidthsPct[i];
            else
                width = 1.0f - currentX;
            
            var rect = row.AddChild();
            rect.anchorMin = new Vector2(currentX, 0);
            currentX += width;
            rect.anchorMax = new Vector2(currentX, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rects[i] = rect;
        }
        
        return rects;
    }
    
    private static System.Random s_colorRandom = new System.Random();
    
    private void AddNewMapRange()
    {
        if (!EditorManager.NMEnabled)
            return;
        
        var map = EditorManager.Instance.Map;
        var atsc = EditorManager.Instance.Atsc;
        
        int seed = Environment.TickCount + map.MapRanges.Count;
        s_colorRandom = new System.Random(seed);
    
        float hue = (float)s_colorRandom.NextDouble();
        float saturation = 0.2f + (float)s_colorRandom.NextDouble() * 0.5f;
    
        Color color = Color.HSVToRGB(hue, saturation, 1);
        
        var newRange = new MapRange
        {
            Name = $"Range {map.MapRanges.Count + 1}",
            StartBeat = atsc.CurrentJsonTime,
            EndBeat = atsc.CurrentJsonTime + 1,
            Color = color
        };
    
        map.MapRanges.Add(newRange);
        EditorGridAndTrackController.Instance.RefreshGridStuff();
        RebuildAll();
    }

    private void SortMapRanges()
    {
        if (!EditorManager.NMEnabled)
            return;
        var map = EditorManager.Instance.Map;
        
        map.MapRanges.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        EditorGridAndTrackController.Instance.RefreshGridStuff();
        RebuildAll();
    }
}
