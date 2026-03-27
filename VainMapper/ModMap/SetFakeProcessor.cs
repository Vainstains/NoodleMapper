using System;
using Beatmap.Base;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID("SetFake")]
public class SetFakeProcessor : INoodleSpanProcessor
{
    string IModMapEditorItem.EditorLabel => "Set Fake";
    float IModMapEditorItem.EditorHeight => 30f;

    [JsonID("value")]
    public bool Enabled { get; set; } = true;

    public void Process(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote:
            case BaseSlider:
                SpanProcessorUtils.EnsureCustomDataObject(obj)["fake"] = Enabled;
                SpanProcessorUtils.RefreshObject(obj);
                break;
        }
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        content.Field("Set Fake", 0.3f).AddToggle()
            .SetValue(Enabled)
            .SetOnChange(isOn =>
            {
                Enabled = isOn;
                onChanged();
            });
    }
}
