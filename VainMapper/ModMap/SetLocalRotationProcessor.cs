using System;
using Beatmap.Base;
using SimpleJSON;
using TMPro;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID("SetLocalRotation")]
public class SetLocalRotationProcessor : INoodleSpanProcessor
{
    string IModMapEditorItem.EditorLabel => "Set Local Rotation";
    float IModMapEditorItem.EditorHeight => 56f;

    [JsonID("x")]
    public float X { get; set; }

    [JsonID("y")]
    public float Y { get; set; }

    [JsonID("z")]
    public float Z { get; set; }

    public void Process(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote note:
                SpanProcessorUtils.EnsureCustomDataObject(note)[note.CustomKeyLocalRotation] = CreateRotationArray();
                SpanProcessorUtils.RefreshObject(note);
                break;
            case BaseSlider slider:
                SpanProcessorUtils.EnsureCustomDataObject(slider)[slider.CustomKeyLocalRotation] = CreateRotationArray();
                SpanProcessorUtils.RefreshObject(slider);
                break;
        }
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        var titleRow = content.AddChild(RectTransform.Edge.Top).ExtendBottom(24);
        titleRow.AddLabel("Set Local Rotation", TextAlignmentOptions.Left);

        var valuesRow = content.AddChild().InsetTop(26);
        var thirds = SplitThree(valuesRow);

        BuildAxisField(thirds[0].InsetRight(2), "X", X, value => X = value, onChanged);
        BuildAxisField(thirds[1].InsetLeft(2).InsetRight(2), "Y", Y, value => Y = value, onChanged);
        BuildAxisField(thirds[2].InsetLeft(2), "Z", Z, value => Z = value, onChanged);
    }

    private static RectTransform[] SplitThree(RectTransform row)
    {
        var first = row.AddChild();
        first.anchorMin = new Vector2(0f, 0f);
        first.anchorMax = new Vector2(1f / 3f, 1f);
        first.offsetMin = first.offsetMax = Vector2.zero;

        var second = row.AddChild();
        second.anchorMin = new Vector2(1f / 3f, 0f);
        second.anchorMax = new Vector2(2f / 3f, 1f);
        second.offsetMin = second.offsetMax = Vector2.zero;

        var third = row.AddChild();
        third.anchorMin = new Vector2(2f / 3f, 0f);
        third.anchorMax = new Vector2(1f, 1f);
        third.offsetMin = third.offsetMax = Vector2.zero;

        return new[] { first, second, third };
    }

    private static void BuildAxisField(RectTransform rect, string label, float currentValue, Action<float> setter, Action onChanged)
    {
        rect.Field(label, 0.28f).AddTextBox()
            .SetValue(currentValue.ToString("0.###"))
            .SetOnChange(text =>
            {
                if (!float.TryParse(text, out var value))
                    return;

                setter(value);
                onChanged();
            });
    }

    private JSONArray CreateRotationArray()
    {
        var array = new JSONArray();
        array.Add(X);
        array.Add(Y);
        array.Add(Z);
        return array;
    }
}
