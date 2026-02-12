using Beatmap.Helper;
using UnityEngine;

namespace NoodleMapper.Utils;

public static class UnityComponentHelpers
{
    public static GameObject AddChildObject(this GameObject parent, string name = "GameObject")
    {
        GameObject childObject = new GameObject(name);
        childObject.transform.SetParent(parent.transform, worldPositionStays: false);
        return childObject;
    }
    
    /// <summary>
    /// When this component dies, so does the target.
    /// </summary>
    private class LifetimeLink : MonoBehaviour
    {
        private UnityEngine.Object m_target = null!;

        private void Init(UnityEngine.Object target)
        {
            m_target = target;
        }

        private void OnDestroy()
        {
            Destroy(m_target);
        }
    }

    public static void LinkLifetime(this UnityEngine.Object target, GameObject indirectParent)
    {
        indirectParent.AddInitComponent<LifetimeLink>(target);
    }

    public static TComponent RequireComponent<TComponent>(this Component component) where TComponent : Component
    {
        return component.gameObject.RequireComponent<TComponent>();
    }

    public static TComponent RequireComponent<TComponent>(this GameObject gameObject) where TComponent : Component
    {
        if (gameObject.TryGetComponent(out TComponent component))
            return component;
        return gameObject.AddComponent<TComponent>();
    }
}