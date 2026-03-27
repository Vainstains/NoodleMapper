using System;
using Beatmap.Base;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID("SetJDOffset")]
public class SetNoteJumpOffsetProcessor : INoodleSpanProcessor
{
    string IModMapEditorItem.EditorLabel => "Set JD Offset";
    float IModMapEditorItem.EditorHeight => 30f;

    [JsonID("value")]
    public float Value { get; set; }

    public void Process(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote note:
                SpanProcessorUtils.EnsureCustomDataObject(note)[note.CustomKeyNoteJumpStartBeatOffset] = Value;
                SpanProcessorUtils.RefreshObject(note);
                break;
            case BaseSlider slider:
                SpanProcessorUtils.EnsureCustomDataObject(slider)[slider.CustomKeyNoteJumpStartBeatOffset] = Value;
                SpanProcessorUtils.RefreshObject(slider);
                break;
        }
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        content.Field("Set JD Offset", 0.3f).AddTextBox()
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
