using System;

namespace VainMapper.Wiring;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class OnPluginInitAttribute : Attribute
{
    public OnPluginInitAttribute() { }
}