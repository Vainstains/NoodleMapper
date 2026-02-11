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
public static class RectTransformExtensions
{
    public static RectTransform AddChild(this RectTransform self)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(self.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    public static RectTransform AddChild(this RectTransform self, RectTransform.Edge edge)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(self.transform, false);

        var rt = go.AddComponent<RectTransform>();
        
        switch (edge)
        {
            case RectTransform.Edge.Left:
            case RectTransform.Edge.Right:
                rt.anchorMin = new Vector2(edge == RectTransform.Edge.Left ? 0 : 1, 0);
                rt.anchorMax = new Vector2(edge == RectTransform.Edge.Left ? 0 : 1, 1);
                rt.sizeDelta = new Vector2(0, 0);
                rt.anchoredPosition = Vector2.zero;
                break;

            case RectTransform.Edge.Top:
            case RectTransform.Edge.Bottom:
                rt.anchorMin = new Vector2(0, edge == RectTransform.Edge.Bottom ? 0 : 1);
                rt.anchorMax = new Vector2(1, edge == RectTransform.Edge.Bottom ? 0 : 1);
                rt.sizeDelta = new Vector2(0, 0);
                rt.anchoredPosition = Vector2.zero;
                break;
        }

        return rt;
    }

    public static RectTransform ExtendTop(this RectTransform self, float px)
    {
        var offset = self.offsetMax;
        offset.y += px;
        self.offsetMax = offset;
        return self;
    }
    
    public static RectTransform ExtendBottom(this RectTransform self, float px)
    {
        var offset = self.offsetMin;
        offset.y -= px;
        self.offsetMin = offset;
        return self;
    }
    
    public static RectTransform ExtendRight(this RectTransform self, float px)
    {
        var offset = self.offsetMax;
        offset.x += px;
        self.offsetMax = offset;
        return self;
    }
    
    public static RectTransform ExtendLeft(this RectTransform self, float px)
    {
        var offset = self.offsetMin;
        offset.x -= px;
        self.offsetMin = offset;
        return self;
    }

    public static RectTransform InsetTop(this RectTransform self, float px) => ExtendTop(self, -px);
    public static RectTransform InsetBottom(this RectTransform self, float px) => ExtendBottom(self, -px);
    public static RectTransform InsetRight(this RectTransform self, float px) => ExtendRight(self, -px);
    public static RectTransform InsetLeft(this RectTransform self, float px) => ExtendLeft(self, -px);
    
    public static RectTransform Extend(this RectTransform self, float px) =>
        self.ExtendTop(px).ExtendBottom(px).ExtendRight(px).ExtendLeft(px);

    public static RectTransform Inset(this RectTransform self, float px) => Extend(self, -px);
}