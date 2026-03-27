using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using TMPro;
using UnityEngine;
using VainLib.Data;
using VainLib.UI.Components;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID("AssignIndividualPathAnimation")]
public class AssignIndividualPathAnimationProcessor : INoodleSpanProcessor
{
    public class AnimationKeyframe
    {
        [JsonID("t")]
        public float Time { get; set; }

        [JsonID("x")]
        public float X { get; set; }

        [JsonID("y")]
        public float Y { get; set; }

        [JsonID("z")]
        public float Z { get; set; }

        [JsonID("w")]
        public float W { get; set; } = 1f;
    }

    public enum AnimationProperty
    {
        OffsetPosition,
        LocalRotation,
        OffsetWorldRotation,
        Scale,
        Dissolve,
        DissolveArrow,
        Interactable,
        Color
    }

    private readonly struct PropertyDefinition
    {
        public PropertyDefinition(AnimationProperty property, string label, string key, int valueCount, string[] axisLabels)
        {
            Property = property;
            Label = label;
            Key = key;
            ValueCount = valueCount;
            AxisLabels = axisLabels;
        }

        public AnimationProperty Property { get; }
        public string Label { get; }
        public string Key { get; }
        public int ValueCount { get; }
        public string[] AxisLabels { get; }
    }

    private static readonly PropertyDefinition[] s_propertyDefinitions =
    {
        new(AnimationProperty.OffsetPosition, "offsetPosition", "offsetPosition", 3, new[] { "X", "Y", "Z" }),
        new(AnimationProperty.LocalRotation, "localRotation", "localRotation", 3, new[] { "Pitch", "Yaw", "Roll" }),
        new(AnimationProperty.OffsetWorldRotation, "offsetWorldRotation", "offsetWorldRotation", 3, new[] { "Pitch", "Yaw", "Roll" }),
        new(AnimationProperty.Scale, "scale", "scale", 3, new[] { "X", "Y", "Z" }),
        new(AnimationProperty.Dissolve, "dissolve", "dissolve", 1, new[] { "Value" }),
        new(AnimationProperty.DissolveArrow, "dissolveArrow", "dissolveArrow", 1, new[] { "Value" }),
        new(AnimationProperty.Interactable, "interactable", "interactable", 1, new[] { "Value" }),
        new(AnimationProperty.Color, "color", "color", 3, new[] { "R", "G", "B" })
    };

    string IModMapEditorItem.EditorLabel => "Individual Path Animation";
    float IModMapEditorItem.EditorHeight => 92f + Math.Max(1, Keyframes.Count) * 34f;

    [JsonID("property")]
    public AnimationProperty Property { get; set; } = AnimationProperty.OffsetPosition;

    [JsonID("keys")]
    public List<AnimationKeyframe> Keyframes { get; set; } = new()
    {
        new AnimationKeyframe()
    };

    public void Process(BaseObject obj)
    {
        if (Keyframes.Count == 0)
            return;

        var customData = SpanProcessorUtils.EnsureCustomDataObject(obj);
        var animationData = SpanProcessorUtils.EnsureChildObject(customData, "animation");
        animationData[GetDefinition().Key] = BuildAnimationArray();
        SpanProcessorUtils.RefreshObject(obj);
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        var definition = GetDefinition();

        var propertyRow = content.AddChild(RectTransform.Edge.Top).ExtendBottom(26);
        propertyRow.Field("Property", 0.28f).AddDropdown(s_propertyDefinitions.Select(it => it.Label).ToArray())
            .SetSelectedOption(Array.FindIndex(s_propertyDefinitions, it => it.Property == Property))
            .SetOnChange(idx =>
            {
                Property = s_propertyDefinitions[Mathf.Clamp(idx, 0, s_propertyDefinitions.Length - 1)].Property;
                onChanged();
            });

        var listRect = content.AddChild().InsetTop(36).InsetBottom(30);
        var keyframeList = listRect.AddRearrangeableList();
        keyframeList.SetOnSwap((from, to) =>
        {
            var frame = Keyframes[from];
            Keyframes.RemoveAt(from);
            Keyframes.Insert(to, frame);
            onChanged();
        });

        for (var i = 0; i < Keyframes.Count; i++)
        {
            var frameIndex = i;
            var frame = Keyframes[i];
            var item = keyframeList.AddItem(32);
            var row = item.Content.AddChild();
            row.anchorMin = new Vector2(0, 0.5f);
            row.anchorMax = new Vector2(1, 0.5f);
            row.pivot = new Vector2(0, 0.5f);
            row.sizeDelta = new Vector2(0, 28);

            BuildKeyframeRow(row, definition, frame, () => onChanged(), () =>
            {
                Keyframes.RemoveAt(frameIndex);
                onChanged();
            });
        }

        var addRow = content.AddChild(RectTransform.Edge.Bottom).ExtendTop(26);
        addRow.AddChild(RectTransform.Edge.Left).ExtendRight(72).AddButton("add key", () =>
        {
            var newFrame = new AnimationKeyframe();
            if (Keyframes.Count > 0)
                newFrame.Time = Mathf.Clamp01(Keyframes.Last().Time);
            Keyframes.Add(newFrame);
            onChanged();
        }).MainColor = new Color(0.25f, 0.45f, 0.55f);
    }

    private void BuildKeyframeRow(RectTransform row, PropertyDefinition definition, AnimationKeyframe frame, Action onChanged, Action onDelete)
    {
        var valueColumnCount = definition.ValueCount;
        var timeWidth = 0.18f;
        var deleteWidth = 0.14f;
        var valueWidth = (1f - timeWidth - deleteWidth) / valueColumnCount;

        var currentX = 0f;
        var columns = new List<RectTransform>();
        for (var i = 0; i < valueColumnCount; i++)
        {
            var rect = row.AddChild();
            rect.anchorMin = new Vector2(currentX, 0);
            currentX += valueWidth;
            rect.anchorMax = new Vector2(currentX, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            columns.Add(rect);
        }

        var timeRect = row.AddChild();
        timeRect.anchorMin = new Vector2(currentX, 0);
        currentX += timeWidth;
        timeRect.anchorMax = new Vector2(currentX, 1);
        timeRect.offsetMin = timeRect.offsetMax = Vector2.zero;

        var deleteRect = row.AddChild();
        deleteRect.anchorMin = new Vector2(currentX, 0);
        deleteRect.anchorMax = Vector2.one;
        deleteRect.offsetMin = deleteRect.offsetMax = Vector2.zero;

        for (var i = 0; i < valueColumnCount; i++)
        {
            var axisIndex = i;
            var axisRect = columns[i];
            if (i > 0)
                axisRect = axisRect.InsetLeft(2);
            if (i < valueColumnCount - 1)
                axisRect = axisRect.InsetRight(2);

            BuildFloatField(axisRect, definition.AxisLabels[i], GetAxisValue(frame, axisIndex), value =>
            {
                SetAxisValue(frame, axisIndex, value);
                onChanged();
            });
        }

        BuildFloatField(timeRect.InsetLeft(2).InsetRight(2), "T", frame.Time, value =>
        {
            frame.Time = Mathf.Clamp01(value);
            onChanged();
        });

        deleteRect.InsetLeft(2).AddButton("X", onDelete).MainColor = new Color(0.7f, 0.1f, 0.3f);
    }

    private static void BuildFloatField(RectTransform rect, string label, float currentValue, Action<float> setter)
    {
        rect.Field(label, 0.24f).AddTextBox()
            .SetValue(currentValue.ToString("0.###"))
            .SetOnChange(text =>
            {
                if (!float.TryParse(text, out var value))
                    return;

                setter(value);
            });
    }

    private JSONArray BuildAnimationArray()
    {
        var definition = GetDefinition();
        var array = new JSONArray();
        foreach (var frame in Keyframes.OrderBy(it => it.Time))
        {
            var point = new JSONArray();
            switch (definition.ValueCount)
            {
                case 1:
                    point.Add(frame.X);
                    break;
                case 3:
                    point.Add(frame.X);
                    point.Add(frame.Y);
                    point.Add(frame.Z);
                    break;
            }

            point.Add(frame.Time);
            array.Add(point);
        }

        return array;
    }

    private PropertyDefinition GetDefinition()
    {
        return s_propertyDefinitions.First(it => it.Property == Property);
    }

    private static float GetAxisValue(AnimationKeyframe frame, int index)
    {
        return index switch
        {
            0 => frame.X,
            1 => frame.Y,
            2 => frame.Z,
            3 => frame.W,
            _ => 0f
        };
    }

    private static void SetAxisValue(AnimationKeyframe frame, int index, float value)
    {
        switch (index)
        {
            case 0:
                frame.X = value;
                break;
            case 1:
                frame.Y = value;
                break;
            case 2:
                frame.Z = value;
                break;
            case 3:
                frame.W = value;
                break;
        }
    }
}
