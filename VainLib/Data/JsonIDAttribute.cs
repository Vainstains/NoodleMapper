using System;

namespace VainLib.Data;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
public class JsonIDAttribute : Attribute
{
    public string ID { get; }

    public JsonIDAttribute(string id)
    {
        ID = id;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class OnJsonDeserializedAttribute : Attribute { }
