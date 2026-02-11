using System;
using System.Collections.Generic;
using System.Reflection;
using Beatmap.Helper;
using UnityEngine;

namespace NoodleMapper.Utils;

public static class UnityComponentHelpers
{
    public static TComponent AddInitComponent<TComponent>(
        this GameObject self, 
        params object[] args
    ) where TComponent : Component
    {
        var comp = self.AddComponent<TComponent>();
        
        var method = typeof(TComponent).GetMethod(
            "Init",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method != null)
        {
            try
            {
                method.Invoke(comp, args);
            }
            catch (TargetParameterCountException)
            {
                Debug.LogError(
                    $"Init(...) on {typeof(TComponent).Name} expects {method?.GetParameters().Length} parameters, " +
                    $"but {args.Length} were provided."
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"Init(...) invocation on {typeof(TComponent).Name} failed: {ex}");
            }
        }

        return comp;
    }
    
    public static TComponent AddInitChild<TComponent>(
        this GameObject self, 
        params object[] args
    ) where TComponent : Component
    {
        var childGo = new GameObject(typeof(TComponent).Name);
        childGo.transform.SetParent(self.transform, false);

        return childGo.AddInitComponent<TComponent>(args);
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
}