using System;
using Beatmap.Base;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID("SetSpawnEffect")]
public class SetSpawnEffectProcessor : INoodleSpanProcessor
{
    string IModMapEditorItem.EditorLabel => "Set Spawn Effect";
    float IModMapEditorItem.EditorHeight => 30f;

    [JsonID("value")]
    public bool Enabled { get; set; } = true;

    public void Process(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote note:
                SpanProcessorUtils.EnsureCustomDataObject(note)[note.CustomKeySpawnEffect] = Enabled;
                SpanProcessorUtils.RefreshObject(note);
                break;
            case BaseSlider slider:
                SpanProcessorUtils.EnsureCustomDataObject(slider)[slider.CustomKeySpawnEffect] = Enabled;
                SpanProcessorUtils.RefreshObject(slider);
                break;
        }
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        content.Field("Set Spawn Effect", 0.3f).AddToggle()
            .SetValue(Enabled)
            .SetOnChange(isOn =>
            {
                Enabled = isOn;
                onChanged();
            });
    }
}
