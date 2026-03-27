using SimpleJSON;
using UnityEngine;
using VainLib.Data;

namespace VainMapper.Map;

public class MapRange
{
    [JsonID("s")]
    public float StartBeat { get; set; } = 0;

    [JsonID("e")]
    public float EndBeat { get; set; } = -1;

    [JsonID("c")]
    public Color Color { get; set; } = Color.white;

    [JsonID("n")]
    public string Name { get; set; } = "";

    [OnJsonDeserialized]
    private void OnJsonDeserialized()
    {
        if (EndBeat == -1)
            EndBeat = StartBeat;
    }
}
