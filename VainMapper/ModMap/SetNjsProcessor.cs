using System;
using Beatmap.Base;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID("SetNJS")]
public class SetNjsProcessor : INoodleSpanProcessor
{
    string IModMapEditorItem.EditorLabel => "Set NJS";
    float IModMapEditorItem.EditorHeight => 30f;

    [JsonID("value")]
    public float Value { get; set; } = 16f;

    public void Process(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote note:
                SpanProcessorUtils.EnsureCustomDataObject(note)[note.CustomKeyNoteJumpMovementSpeed] = Value;
                SpanProcessorUtils.RefreshObject(note);
                break;
            case BaseSlider slider:
                SpanProcessorUtils.EnsureCustomDataObject(slider)[slider.CustomKeyNoteJumpMovementSpeed] = Value;
                SpanProcessorUtils.RefreshObject(slider);
                break;
        }
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        content.Field("Set NJS", 0.3f).AddTextBox()
            .SetValue(Value.ToString("0.###"))
            .SetOnChange(text =>
            {
                if (!float.TryParse(text, out var value))
                    return;

                Value = value;
                onChanged();
            });
    }
}
