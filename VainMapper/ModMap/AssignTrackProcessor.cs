using Beatmap.Base;

namespace VainMapper.ModMap;

public class AssignTrackProcessor : INoodleSpanProcessor
{
    public string TrackName { get; set; } = string.Empty;

    public void Process(BaseObject obj)
    {
        switch (obj)
        {
            case BaseNote:
            case BaseChain:
            case BaseArc:
            case BaseObstacle:
                obj.CustomData["track"] = TrackName;
                break;
        };
        obj.RefreshCustom();
    }
}