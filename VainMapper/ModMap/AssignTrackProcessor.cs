using System;
using Beatmap.Base;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID("AssignTrack")]
public class AssignTrackProcessor : INoodleSpanProcessor
{
    string IModMapEditorItem.EditorLabel => "Assign Track";
    float IModMapEditorItem.EditorHeight => 30f;

    [JsonID("track")]
    public string TrackName { get; set; } = string.Empty;

    public void Process(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote:
            case BaseChain:
            case BaseArc:
            case BaseObstacle:
                SpanProcessorUtils.EnsureCustomDataObject(obj)["track"] = TrackName;
                break;
        }

        SpanProcessorUtils.RefreshObject(obj);
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        content.Field("Assign Track", 0.3f).AddTextBox()
            .SetValue(TrackName)
            .SetOnChange(value =>
            {
                TrackName = value ?? string.Empty;
                onChanged();
            });
    }
}
