using System;
using System.Collections.Generic;
using System.Reflection;
using Beatmap.Helper;
using NoodleMapper.UI;
using NoodleMapper.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.Utils;

public static class UnityComponentHelpers
{
    public static TComponent AddInitComponent<TComponent>(
        this RectTransform self,
        params object[] args
    ) where TComponent : Component
    {
        return self.gameObject.AddInitComponent<TComponent>(args);
    }
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
        this RectTransform self,
        params object[] args
    ) where TComponent : Component
    {
        return self.gameObject.AddInitChild<TComponent>(args);
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
    
    public static RectTransform AddChildTopLeft(this RectTransform self)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(self.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.up;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
    
    public static RectTransform AddChildTopRight(this RectTransform self)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(self.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
    
    public static RectTransform AddChildBottomLeft(this RectTransform self)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(self.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
    
    public static RectTransform AddChildBottomRight(this RectTransform self)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(self.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.right;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
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

    public static Image AddImage(this RectTransform self, Sprite sprite) => self.AddImage(sprite, Color.white);
    public static Image AddImage(this RectTransform self, Sprite sprite, Color color)
    {
        var image = self.RequireComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sprite.border.sqrMagnitude > 0.5f)
            image.type = Image.Type.Sliced;
        return image;
    }

    public static Image AddClearImage(this RectTransform self)
    {
        var image = self.RequireComponent<Image>();
        image.color = Color.clear;
        return image;
    }

    private const int FontSize = 18;

    public static TextMeshProUGUI AddLabel(
        this RectTransform self,
        string text,
        TextAlignmentOptions alignmentOptions = TextAlignmentOptions.Left,
        int fontSize = FontSize,
        Color? color = null,
        TextOverflowModes overflowMode = TextOverflowModes.Ellipsis)
    {
        var label = self.gameObject.AddComponent<TextMeshProUGUI>();
        
        label.font = PersistentUI.Instance.ButtonPrefab.Text.font;
        label.fontSize = fontSize;
        label.alignment = alignmentOptions;
        label.color = color ?? Color.white;
        label.text = text;
        label.overflowMode = overflowMode;
        label.raycastTarget = false;
        
        return label;
    }

    public static NoodleTextbox AddTextBox(
        this RectTransform self)
    {
        return NoodleTextbox.Create(self);
    }
    public static TMP_InputField AddInputFieldRaw(
        this RectTransform self,
        string placeholderText = "hi",
        string defaultText = "",
        int fontSize = FontSize
    )
    {
        var inputField = GameObject.Instantiate(PersistentUI.Instance.TextInputPrefab, self.transform);
        var rt = inputField.Transform;
        
        rt.anchorMax = Vector2.one;
        rt.anchorMin = rt.offsetMax = rt.offsetMin = Vector2.zero;

        var text = inputField.InputField.textComponent;
        var ph = (inputField.InputField.placeholder as TMP_Text)!;
        ph.text =  placeholderText;
        
        text.fontSize = ph.fontSize = fontSize;

        var bg = (inputField.InputField.targetGraphic as Image)!;

        bg.sprite = StaticAssets.RoundRectBordered;

        inputField.InputField.textViewport.Extend(4);
        inputField.InputField.SetTextWithoutNotify(defaultText);

        return inputField.InputField;
    }

    public static VerticalLayoutGroup Vertical(
        this RectTransform self,
        float padding = 0,
        float spacing = 1
    )
    {
        var layout = self.RequireComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(
            (int)padding,
            (int)padding,
            (int)padding,
            (int)padding
        );

        layout.spacing = spacing;
        
        layout.childAlignment = TextAnchor.UpperLeft;

        layout.childControlWidth = true;
        layout.childControlHeight = true;

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return layout;
    }

    public static RectTransform Item(this VerticalLayoutGroup self, int height = 20)
    {
        var parentRT = (RectTransform)self.transform;
        
        var child = parentRT.AddChild(RectTransform.Edge.Top).ExtendBottom(height);
        
        var layoutElement = child.RequireComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleHeight = 0;
        
        layoutElement.flexibleWidth = 1;

        return child;
    }

    public static RectTransform Field(this RectTransform self, string title)
    {
        const float LabelPct = 0.6f;
        
        var row = self.AddChild();
        row.name = $"{title}_Field";
        
        var labelRect = row.AddChild();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(LabelPct, 1);
        labelRect.offsetMin = Vector2.right * 4;
        labelRect.offsetMax = Vector2.zero;

        labelRect.AddLabel(title, TextAlignmentOptions.Left);
        
        var controlRect = row.AddChild();
        controlRect.anchorMin = new Vector2(LabelPct, 0);
        controlRect.anchorMax = new Vector2(1, 1);
        controlRect.offsetMin = Vector2.zero;
        controlRect.offsetMax = Vector2.zero;

        return controlRect;
    }

    public static NoodleDropdown AddDropdown(this RectTransform self, params IEnumerable<string> options)
    {
        var dropdown = self.AddInitComponent<NoodleDropdown>();
        dropdown.SetOptions(options);
        return dropdown;
    }
}