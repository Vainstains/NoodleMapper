using UnityEngine;

namespace VainMapper.EditorThings;

public class RawMapRange
{
    public float StartBeat { get; set; } // This is JSON time
    public float EndBeat { get; set; }    // This is JSON time
    
    public bool EnableEndingEndpoint { get; set; }
    public Color RangeColor { get; set; }
    public string Label { get; set; }
}