using System;
using UnityEngine;

namespace VainMapper.ModMap;

public interface IModMapEditorItem
{
    public string EditorLabel { get; }
    public float EditorHeight { get; }
    public void BuildEditorUI(RectTransform content, Action onChanged);
}
