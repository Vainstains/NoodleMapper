using UnityEngine;

namespace NoodleMapper.EditorThings;

public class RawMapRange
{
    public float StartBeat { get; set; } // This is JSON time
    public float EndBeat { get; set; }    // This is JSON time
    public Color RangeColor { get; set; }
    public string Label { get; set; }
}