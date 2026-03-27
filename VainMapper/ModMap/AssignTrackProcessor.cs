using Beatmap.Base;
using VainLib.Data;

namespace VainMapper.ModMap;

[JsonID("AssignTrack")]
public class AssignTrackProcessor : INoodleSpanProcessor
{
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
                obj.CustomData["track"] = TrackName;
                break;
        }

        obj.RefreshCustom();
    }
}
