using System;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

public class UnknownFilter : JsonFallback, INoodleFilter
{
    string IModMapEditorItem.EditorLabel => "Unknown Filter";
    float IModMapEditorItem.EditorHeight => 32f;

    public bool TestAgainst(BaseObject obj) => true;

    public JSONNode ToJSON() => Node;

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        content.Field("Unknown Filter", 0.3f).AddLabel(GetType().Name);
    }
}
