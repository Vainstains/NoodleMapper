using System;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;
using VainLib.Data;
using VainLib.Utils;

namespace VainMapper.ModMap;

[JsonID(FilterTypeStr)]
public class HandTypeFilter : INoodleFilter
{
    public const string FilterTypeStr = "HandType";

    string IModMapEditorItem.EditorLabel => "Hand Type";
    float IModMapEditorItem.EditorHeight => 30f;

    [JsonID("hand")]
    public HandType Hand { get; set; }

    public bool TestAgainst(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote note:
                return note.Color == (int)Hand && note.Type != (int)NoteType.Bomb;
            case BaseChain chain:
                return chain.Color == (int)Hand;
            case BaseArc arc:
                return arc.Color == (int)Hand;
        }

        return false;
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        var control = content.Field("Hand Type");
        control.AddDropdown("Both", "Left", "Right")
            .SetSelectedOption(HandTypeToDropdownIndex(Hand))
            .SetOnChange(idx =>
            {
                Hand = DropdownIndexToHandType(idx);
                onChanged();
            });
    }

    private static int HandTypeToDropdownIndex(HandType handType)
    {
        return handType switch
        {
            HandType.Left => 1,
            HandType.Right => 2,
            _ => 0
        };
    }

    private static HandType DropdownIndexToHandType(int index)
    {
        return index switch
        {
            1 => HandType.Left,
            2 => HandType.Right,
            _ => HandType.Both
        };
    }
}

[JsonID(FilterTypeStr)]
public class ObjectTypeFilter : INoodleFilter
{
    public const string FilterTypeStr = "ObjectType";

    string IModMapEditorItem.EditorLabel => "Object Type";
    float IModMapEditorItem.EditorHeight => 30f;

    [JsonID("type")]
    public ObjectType ObjectType { get; set; }

    /*
    Note,
    Event,
    Obstacle,
    CustomNote,
    CustomEvent,
    BpmChange,
    Arc,
    Chain,
    Bookmark, // don't need these vvv
    Waypoint,
    NJSEvent,
    EnvironmentEnhancement
    */

    public bool TestAgainst(BaseObject obj)
    {
        return obj.ObjectType == ObjectType;
    }

    public void BuildEditorUI(RectTransform content, Action onChanged)
    {
        var control = content.Field("Object Type");
        control.AddDropdown("Note", "Event", "Obstacle", "CustomNote", "CustomEvent", "BpmChange", "Arc", "Chain")
            .SetSelectedOption((int)ObjectType)
            .SetOnChange(idx =>
            {
                ObjectType = (ObjectType)idx;
                onChanged();
            });
    }
}
