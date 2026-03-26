using Beatmap.Helper;
using UnityEngine;

namespace VainMapper.Utils;

public static class UnityComponentHelpers
{
    public static GameObject AddChildObject(this GameObject parent, string name = "GameObject")
    {
        GameObject childObject = new GameObject(name);
        childObject.transform.SetParent(parent.transform, worldPositionStays: false);
        return childObject;
    }

    public static TComponent RequireComponent<TComponent>(this Component component) where TComponent : Component => 
        component.gameObject.RequireComponent<TComponent>();

    public static TComponent RequireComponent<TComponent>(this GameObject gameObject) where TComponent : Component
    {
        if (gameObject.TryGetComponent(out TComponent component))
            return component;
        return gameObject.AddComponent<TComponent>();
    }
    
    public static TComponent? GetComponent<TComponent>(this Component component, ref TComponent? cache)
        where TComponent : Component => component.gameObject.GetComponent(ref cache);
    public static TComponent? GetComponent<TComponent>(this GameObject gameObject, ref TComponent? cache)
        where TComponent : Component
    {
        if (cache != null)
            return cache;
       return cache = gameObject.GetComponent<TComponent>();
    }
    
    public static TComponent? GetComponentInChildren<TComponent>(this Component component, ref TComponent? cache)
        where TComponent : Component => component.gameObject.GetComponentInChildren(ref cache);
    public static TComponent? GetComponentInChildren<TComponent>(this GameObject gameObject, ref TComponent? cache)
        where TComponent : Component
    {
        if (cache != null)
            return cache;
        return cache = gameObject.GetComponentInChildren<TComponent>();
    }
}