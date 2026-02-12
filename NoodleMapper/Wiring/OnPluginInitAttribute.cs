using System;

namespace NoodleMapper.Wiring;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class OnPluginInitAttribute : Attribute
{
    public OnPluginInitAttribute() { }
}