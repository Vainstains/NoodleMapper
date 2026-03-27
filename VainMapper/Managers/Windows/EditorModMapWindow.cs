using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VainLib.UI;
using VainLib.UI.Components;
using VainLib.Utils;
using VainMapper.ModMap;
using VainMapper.Utils;

namespace VainMapper.Managers.Windows;

internal static class ModMapEditorContext
{
    public static int SelectedSpanIndex { get; private set; } = -1;

    public static bool TryGetActiveModMap(out ModMapData modMap)
    {
        modMap = null!;

        if (!EditorManager.NMEnabled || EditorManager.Instance?.Map?.ModMapData == null)
            return false;

        modMap = EditorManager.Instance.Map.ModMapData;
        return true;
    }

    public static bool TryGetSelectedSpan(out NoodleSpan span)
    {
        span = null!;

        if (!TryGetActiveModMap(out var modMap))
            return false;

        if (SelectedSpanIndex < 0 || SelectedSpanIndex >= modMap.EffectSpans.Count)
            return false;

        span = modMap.EffectSpans[SelectedSpanIndex];
        return true;
    }

    public static void SelectSpan(int index)
    {
        SelectedSpanIndex = index;
    }

    public static void ClearSelection()
    {
        SelectedSpanIndex = -1;
    }
}

public class EditorModMapWindow : GenericWindow<EditorModMapWindow>
{
    public override string WindowName => "Effect Ranges";

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
        EditorModMapSpanWindow.CloseUI();
        ModMapEditorContext.ClearSelection();
        CloseUI();
    }

    protected override void BuildUI(RectTransform content)
    {
        if (!ModMapEditorContext.TryGetActiveModMap(out var modMap))
        {
            EditorModMapSpanWindow.CloseUI();
            ModMapEditorContext.ClearSelection();
            content.AddLabel("Choose an active modmap first.",
                overflowMode: TextOverflowModes.Overflow,
                alignmentOptions: TextAlignmentOptions.Center).enableWordWrapping = true;
            return;
        }

        SetupScrolling(ref content);
        var layout = content.AddVertical();
        content.AddSizeFitter(vertical: ContentSizeFitter.FitMode.PreferredSize);

        var list = layout.AddRow().AddRearrangeableList();
        list.SetOnSwap((from, to) =>
        {
            var selectedSpan = ModMapEditorContext.TryGetSelectedSpan(out var currentSelectedSpan)
                ? currentSelectedSpan
                : null;
            var span = modMap.EffectSpans[from];
            modMap.EffectSpans.RemoveAt(from);
            modMap.EffectSpans.Insert(to, span);

            if (selectedSpan != null)
                ModMapEditorContext.SelectSpan(modMap.EffectSpans.IndexOf(selectedSpan));

            EditorManager.Instance?.PreviewSelectedModMapSpan();
            Window.RebuildAll();
        });

        for (var i = 0; i < modMap.EffectSpans.Count; i++)
        {
            var span = modMap.EffectSpans[i];
            BuildSpanRow(list, modMap, span, i);
        }

        layout.AddRow(2).AddGetBorder(RectTransform.Edge.Bottom).Move(0, 1);
        var endRow = layout.AddRow();
        endRow.AddChild(RectTransform.Edge.Left).ExtendRight(50).AddButton("new...", AddNewSpan);
        endRow.AddChild(RectTransform.Edge.Left).ExtendRight(50).Move(52, 0).AddButton("sort", SortSpans);
    }

    private void BuildSpanRow(NoodleRearrangeableList list, ModMapData modMap, NoodleSpan span, int index)
    {
        var itemContainer = list.AddItem();

        const int DesiredContentHeight = 26;
        var itemRect = itemContainer.Content.AddChild();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.pivot = new Vector2(0, 0.5f);
        itemRect.sizeDelta = new Vector2(0, DesiredContentHeight);

        var columns = SplitRow(itemRect, 0.42f, 0.20f, 0.12f, 0.10f, 0.10f);
        var rangeRect = columns[0].InsetRight(4);
        var countsRect = columns[1].InsetLeft(4).InsetRight(4);
        var startRect = columns[2].InsetLeft(2).InsetRight(2);
        var endRect = columns[3].InsetLeft(2).InsetRight(2);
        var editRect = columns[4].InsetLeft(2).InsetRight(2);
        var deleteRect = columns[5].InsetLeft(2);

        var rangeLabel = rangeRect.AddLabel(GetBeatLabel(span), TextAlignmentOptions.Center);
        countsRect.AddLabel(GetCountsLabel(span), TextAlignmentOptions.Center, fontSize: 16);

        startRect.AddButton("S", () =>
        {
            span.StartBeat = EditorManager.Instance.Atsc.CurrentJsonTime;
            rangeLabel.text = GetBeatLabel(span);
            EditorManager.Instance?.ApplyActiveModMap();
        }).MainColor = new Color(0.3f, 0.4f, 0.6f);

        endRect.AddButton("E", () =>
        {
            var currentBeat = EditorManager.Instance.Atsc.CurrentJsonTime;
            span.Duration = Mathf.Max(0, currentBeat - span.StartBeat);
            rangeLabel.text = GetBeatLabel(span);
            EditorManager.Instance?.ApplyActiveModMap();
        }).MainColor = new Color(0.3f, 0.4f, 0.6f);

        editRect.AddButton("edit", () =>
        {
            ModMapEditorContext.SelectSpan(index);
            EditorModMapSpanWindow.CloseUI();
            EditorModMapSpanWindow.ToggleUI();
        }).MainColor = new Color(0.25f, 0.45f, 0.55f);

        deleteRect.AddButton("X", () =>
        {
            PersistentUI.Instance.AskYesNo($"Delete effect range {index + 1}?", "This cannot be undone.", () =>
            {
                modMap.EffectSpans.Remove(span);

                if (ModMapEditorContext.SelectedSpanIndex == index)
                {
                    EditorModMapSpanWindow.CloseUI();
                    ModMapEditorContext.ClearSelection();
                }
                else if (ModMapEditorContext.SelectedSpanIndex > index)
                {
                    ModMapEditorContext.SelectSpan(ModMapEditorContext.SelectedSpanIndex - 1);
                }

                EditorManager.Instance?.ApplyActiveModMap();
                Window.RebuildAll();
            });
        }).MainColor = new Color(0.7f, 0.1f, 0.3f);
    }

    private static RectTransform[] SplitRow(RectTransform row, params float[] splitWidthsPct)
    {
        var rects = new RectTransform[splitWidthsPct.Length + 1];
        float currentX = 0;

        for (var i = 0; i < rects.Length; i++)
        {
            float width = i < splitWidthsPct.Length ? splitWidthsPct[i] : 1.0f - currentX;

            var rect = row.AddChild();
            rect.anchorMin = new Vector2(currentX, 0);
            currentX += width;
            rect.anchorMax = new Vector2(currentX, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rects[i] = rect;
        }

        return rects;
    }

    private static string GetBeatLabel(NoodleSpan span)
    {
        var endBeat = span.StartBeat + span.Duration;
        return $"{span.StartBeat:0.###} - {endBeat:0.###}";
    }

    private static string GetCountsLabel(NoodleSpan span)
    {
        return $"F:{span.Filters.Count} E:{span.Processors.Count}";
    }

    private void AddNewSpan()
    {
        if (!ModMapEditorContext.TryGetActiveModMap(out var modMap))
            return;

        var beat = EditorManager.Instance.Atsc.CurrentJsonTime;
        modMap.EffectSpans.Add(new NoodleSpan
        {
            StartBeat = beat,
            Duration = 1
        });

        EditorManager.Instance?.ApplyActiveModMap();
        Window.RebuildAll();
    }

    private void SortSpans()
    {
        if (!ModMapEditorContext.TryGetActiveModMap(out var modMap))
            return;

        var selectedSpan = ModMapEditorContext.TryGetSelectedSpan(out var currentSelectedSpan)
            ? currentSelectedSpan
            : null;
        modMap.EffectSpans.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        if (selectedSpan != null)
            ModMapEditorContext.SelectSpan(modMap.EffectSpans.IndexOf(selectedSpan));
        EditorManager.Instance?.PreviewSelectedModMapSpan();
        Window.RebuildAll();
    }
}
