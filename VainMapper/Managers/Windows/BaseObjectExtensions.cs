using Beatmap.Base;
using Beatmap.Enums;

namespace VainMapper.Managers.Windows;

internal static class BaseObjectExtensions
{
    public static bool IsColorNote(this BaseObject o) => o is BaseNote && !o.IsBombNote();

    public static bool IsColorNote(this BaseObject o, out BaseNote note)
    {
        var isNote = o.IsColorNote();
        note = (isNote ? o as BaseNote : null)!;
        return isNote;
    }
    public static bool IsBombNote(this BaseObject o) => o is BaseNote { Type: (int)NoteType.Bomb };
    public static bool IsBombNote(this BaseObject o, out BaseNote note)
    {
        var isNote = o.IsBombNote();
        note = (isNote ? o as BaseNote : null)!;
        return isNote;
    }
    public static bool IsArc(this BaseObject o) => o is BaseArc;
    public static bool IsArc(this BaseObject o, out BaseArc arc)
    {
        var isArc = o.IsArc();
        arc = (isArc ? o as BaseArc : null)!;
        return isArc;
    }
    public static bool IsChain(this BaseObject o) => o is BaseChain;
    public static bool IsChain(this BaseObject o, out BaseChain chain)
    {
        var isChain = o.IsChain();
        chain = (isChain ? o as BaseChain : null)!;
        return isChain;
    }
    public static bool IsWall(this BaseObject o) => o is BaseObstacle;
    public static bool IsWall(this BaseObject o, out BaseObstacle wall)
    {
        var isWall = o.IsWall();
        wall = (isWall ? o as BaseObstacle : null)!;
        return isWall;
    }
    public static bool IsEvent(this BaseObject o) => o is BaseEvent;
    public static bool IsEvent(this BaseObject o, out BaseEvent @event)
    {
        var isEvent = o.IsEvent();
        @event = (isEvent ? o as BaseEvent : null)!;
        return isEvent;
    }
}