using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using VainLib.Data;

namespace VainMapper.ModMap;

[JsonID(FilterTypeStr)]
public class HandTypeFilter : INoodleFilter
{
    public const string FilterTypeStr = "HandType";

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
}
