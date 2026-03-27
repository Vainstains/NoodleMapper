using System;
using Beatmap.Base;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

public class UnknownSpanProcessor : JsonFallback, INoodleSpanProcessor
{
    string IModMapEditorItem.EditorLabel => "Unknown Effect";
    float IModMapEditorItem.EditorHeight => 32f;

    public void Process(BaseObject obj)
    {
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        content.Field("Unknown Effect", 0.3f).AddLabel(GetType().Name);
    }
}
