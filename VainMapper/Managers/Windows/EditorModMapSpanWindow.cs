using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VainLib.Data;
using VainLib.UI;
using VainLib.UI.Components;
using VainLib.Utils;
using VainMapper.ModMap;

namespace VainMapper.Managers.Windows;

public class EditorModMapSpanWindow : GenericWindow<EditorModMapSpanWindow>
{
    private static readonly IReadOnlyList<EditorChoice<INoodleFilter>> s_filterChoices = DiscoverChoices<INoodleFilter>();
    private static readonly IReadOnlyList<EditorChoice<INoodleSpanProcessor>> s_processorChoices = DiscoverChoices<INoodleSpanProcessor>();
    private static int s_selectedFilterChoice;
    private static int s_selectedProcessorChoice;

    public static bool IsOpen { get; private set; }
    public override string WindowName => "Span Filters & Effects";

    protected override void PostInit()
    {
        IsOpen = true;
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += DifficultyChanged;
        EditorManager.Instance?.PreviewSelectedModMapSpan();
    }

    private void OnDisable()
    {
        IsOpen = false;
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= DifficultyChanged;
        EditorManager.Instance?.PreviewSelectedModMapSpan();
    }

    private void DifficultyChanged()
    {
        ModMapEditorContext.ClearSelection();
        CloseUI();
    }

    protected override void BuildUI(RectTransform content)
    {
        if (!ModMapEditorContext.TryGetSelectedSpan(out var span))
        {
            content.AddLabel("Choose an effect range first.",
                overflowMode: TextOverflowModes.Overflow,
                alignmentOptions: TextAlignmentOptions.Center).enableWordWrapping = true;
            return;
        }

        SetupScrolling(ref content);
        var layout = content.AddVertical();
        content.AddSizeFitter(vertical: ContentSizeFitter.FitMode.PreferredSize);

        var summary = layout.AddRow();
        summary.AddLabel($"Editing {span.StartBeat:0.###} - {span.StartBeat + span.Duration:0.###}",
            TextAlignmentOptions.Center);

        layout.AddRow(2).AddGetBorder(RectTransform.Edge.Bottom).Move(0, 1);
        BuildFiltersSection(layout, span);
        layout.AddRow(2).AddGetBorder(RectTransform.Edge.Bottom).Move(0, 1);
        BuildProcessorsSection(layout, span);
    }

    private void BuildFiltersSection(NoodleVerticalLayout layout, NoodleSpan span)
    {
        layout.AddRow().AddLabel("Filters", TextAlignmentOptions.Center);
        BuildEditorList(
            layout,
            span.Filters,
            s_filterChoices,
            () => s_selectedFilterChoice,
            idx => s_selectedFilterChoice = idx);
    }

    private void BuildProcessorsSection(NoodleVerticalLayout layout, NoodleSpan span)
    {
        layout.AddRow().AddLabel("Effects", TextAlignmentOptions.Center);
        BuildEditorList(
            layout,
            span.Processors,
            s_processorChoices,
            () => s_selectedProcessorChoice,
            idx => s_selectedProcessorChoice = idx);
    }

    private void BuildEditorList<TItem>(
        NoodleVerticalLayout layout,
        List<TItem> items,
        IReadOnlyList<EditorChoice<TItem>> choices,
        Func<int> getSelectedIndex,
        Action<int> setSelectedIndex)
        where TItem : class, IModMapEditorItem
    {
        var list = layout.AddRow().AddRearrangeableList();
        list.SetOnSwap((from, to) =>
        {
            var item = items[from];
            items.RemoveAt(from);
            items.Insert(to, item);
            EditorManager.Instance?.ApplyActiveModMap();
            Window.RebuildAll();
        });

        for (var i = 0; i < items.Count; i++)
        {
            var itemIndex = i;
            var item = items[i];
            var itemHeight = Mathf.Max(32f, item.EditorHeight);
            var container = list.AddItem(itemHeight + 4f);

            var row = container.Content.AddChild();
            row.anchorMin = new Vector2(0, 0.5f);
            row.anchorMax = new Vector2(1, 0.5f);
            row.pivot = new Vector2(0, 0.5f);
            row.sizeDelta = new Vector2(0, itemHeight);

            var (contentRect, deleteRect) = row.SplitHorizontal(1.0f, -54);
            item.BuildEditorUI(contentRect.InsetRight(2), () =>
            {
                EditorManager.Instance?.ApplyActiveModMap();
                Window.RebuildAll();
            });

            deleteRect.AddButton("remove", () =>
            {
                items.RemoveAt(itemIndex);
                EditorManager.Instance?.ApplyActiveModMap();
                Window.RebuildAll();
            }).MainColor = new Color(0.7f, 0.1f, 0.3f);
        }

        var addRow = layout.AddRow();
        var (dropdownRect, buttonRect) = addRow.SplitHorizontal(1.0f, -74);

        var selectedIndex = choices.Count == 0
            ? 0
            : Mathf.Clamp(getSelectedIndex(), 0, choices.Count - 1);

        dropdownRect.InsetRight(2).AddDropdown(GetChoiceNames(choices))
            .SetSelectedOption(selectedIndex)
            .SetOnChange(setSelectedIndex);

        var addButton = buttonRect.AddButton("add", () =>
        {
            if (choices.Count == 0)
                return;

            var idx = Mathf.Clamp(getSelectedIndex(), 0, choices.Count - 1);
            items.Add(choices[idx].Factory());
            EditorManager.Instance?.ApplyActiveModMap();
            Window.RebuildAll();
        });
        addButton.MainColor = new Color(0.25f, 0.45f, 0.55f);
    }

    private static string[] GetChoiceNames<TItem>(IReadOnlyList<EditorChoice<TItem>> choices)
    {
        if (choices.Count == 0)
            return new[] { "No options" };

        return choices.Select(it => it.Name).ToArray();
    }

    private static IReadOnlyList<EditorChoice<TItem>> DiscoverChoices<TItem>()
        where TItem : class, IModMapEditorItem
    {
        return typeof(TItem).Assembly.GetTypes()
            .Where(type => typeof(TItem).IsAssignableFrom(type))
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
            .Where(type => !typeof(JsonFallback).IsAssignableFrom(type))
            .Select(type => new EditorChoice<TItem>(GetChoiceName(type), () => (TItem)Activator.CreateInstance(type)!))
            .OrderBy(choice => choice.Name)
            .ToArray();
    }

    private static string GetChoiceName(Type type)
    {
        if (Activator.CreateInstance(type) is INoodleFilter filter)
            return filter.EditorLabel;
        if (Activator.CreateInstance(type) is INoodleSpanProcessor processor)
            return processor.EditorLabel;

        var jsonId = type.GetCustomAttribute<JsonIDAttribute>();
        return jsonId?.ID ?? type.Name;
    }

    private readonly struct EditorChoice<TItem>
    {
        public EditorChoice(string name, Func<TItem> factory)
        {
            Name = name;
            Factory = factory;
        }

        public string Name { get; }
        public Func<TItem> Factory { get; }
    }
}
