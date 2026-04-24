using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VainLib.UI;
using VainLib.UI.Components;

namespace VainLib.Utils;

public static class RectTransformExtensions
{
    public static RectTransform AddChild(this RectTransform self)
    {
        var rt = self.gameObject.AddChildObject("rect").AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    public static RectTransform AddChild(this RectTransform self, RectTransform.Edge edge)
    {
        var rt = self.gameObject.AddChildObject("rect").AddComponent<RectTransform>();
        
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
        var rt = self.gameObject.AddChildObject("rect").AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.up;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
    
    public static RectTransform AddChildTopRight(this RectTransform self)
    {
        var rt = self.gameObject.AddChildObject("rect").AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
    
    public static RectTransform AddChildBottomLeft(this RectTransform self)
    {
        var rt = self.gameObject.AddChildObject("rect").AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
    
    public static RectTransform AddChildBottomRight(this RectTransform self)
    {
        var rt = self.gameObject.AddChildObject("rect").AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.right;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
    
    public static RectTransform AddChildCenter(this RectTransform self)
    {
        var rt = self.gameObject.AddChildObject("rect").AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.one * 0.5f;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    public static RectTransform Move(this RectTransform self, Vector2 delta)
    {
        self.anchoredPosition += delta;
        return self;
    }
    
    public static RectTransform Move(this RectTransform self, float deltaX, float deltaY)
    {
        self.anchoredPosition += new Vector2(deltaX, deltaY);
        return self;
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
    public static Image AddImage(this RectTransform self, Sprite? sprite, Color color)
    {
        var image = self.RequireComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sprite != null && sprite.border.sqrMagnitude > 0.5f)
            image.type = Image.Type.Sliced;
        return image;
    }
    
    public static Image AddSpriteImage(this RectTransform self, Sprite sprite) => self.AddSpriteImage(sprite, Color.white);
    public static Image AddSpriteImage(this RectTransform self, Sprite sprite, Color color)
    {
        var imgRect = self.AddChildCenter();
        imgRect.pivot = Vector2.one * 0.5f;
        imgRect.sizeDelta = new Vector2(sprite.texture.width, sprite.texture.height);
        var image = imgRect.RequireComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sprite.border.sqrMagnitude > 0.5f)
            image.type = Image.Type.Sliced;
        return image;
    }

    public static DragHandler AddDragHandler(this RectTransform self)
    {
        if (!self.GetComponents<Graphic>().Any(g => g.raycastTarget))
            self.AddClearImage();
        return self.gameObject.AddComponent<DragHandler>();
    }

    public static Image DisableRaycasts(this Image self)
    {
        self.raycastTarget = false;
        return self;
    }

    public static Image AddClearImage(this RectTransform self)
    {
        var image = self.RequireComponent<Image>();
        image.color = Color.clear;
        return image;
    }

    public static NoodleRearrangeableList AddRearrangeableList(this RectTransform self)
    {
        return self.AddInitComponent<NoodleRearrangeableList>();
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

        bg.sprite = DefaultResources.LoadSprite("Resources/RoundRectBordered.png");
        bg.color = new Color(0.30f, 0.30f, 0.30f);

        inputField.InputField.textViewport.Extend(4);
        inputField.InputField.SetTextWithoutNotify(defaultText);

        return inputField.InputField;
    }

    public static NoodleValueInput AddValueInput(this RectTransform self, float defaultValue = 0, float min = float.MinValue, float max = float.MaxValue, float step = 1)
    {
        var input = self.AddInitComponent<NoodleValueInput>();
        input.SetDefault(defaultValue).SetMinMax(min, max).SetStep(step);
        return input;
    }

    public static RectTransform AddVerticalScrollView(this RectTransform self) =>
        self.AddVerticalScrollView(out _);
    public static RectTransform AddVerticalScrollView(this RectTransform self, out ScrollRect scrollRect)
    {
        var viewport = self.AddChild();
        viewport.name = "Viewport";
    
        var mask = viewport.RequireComponent<RectMask2D>();
        mask.softness = Vector2Int.up * 2;
        scrollRect = self.RequireComponent<ScrollRect>();
    
        var content = viewport.AddChild(RectTransform.Edge.Top);
        content.name = "Content";
        content.pivot = new Vector2(0, 1);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
    
        var scrollbarRect = self.AddChild(RectTransform.Edge.Right);
        scrollbarRect.name = "Scrollbar";
        scrollbarRect.ExtendLeft(8);
        scrollbarRect.InsetTop(2).InsetBottom(2);
    
        var scrollbar = scrollbarRect.RequireComponent<Scrollbar>();
        var scrollbarImage = scrollbarRect.AddImage(null, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        scrollbarImage.raycastTarget = true;
    
        var handleRect = scrollbarRect.AddChild();
        handleRect.name = "Handle";
        handleRect.Extend(2).Inset(1);
    
        var handleImage = handleRect.AddImage(DefaultResources.LoadSprite("Resources/RoundRect.png"), new Color(0.5f, 0.5f, 0.5f, 0.8f));
        handleImage.raycastTarget = true;
        handleImage.type = Image.Type.Sliced;
    
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.size = 0.2f;
    
        var viewportImage = viewport.AddClearImage();
        viewportImage.raycastTarget = true;
    
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = 3;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
    
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        scrollRect.scrollSensitivity = 2.5f;
    
        scrollbarRect.gameObject.SetActive(false);
        scrollbar.numberOfSteps = 0;
        viewport.offsetMax = new Vector2(-8, 0);
    
        return content;
    }

    public static ContentSizeFitter AddSizeFitter(this RectTransform self,
        ContentSizeFitter.FitMode horizontal = ContentSizeFitter.FitMode.Unconstrained,
        ContentSizeFitter.FitMode vertical = ContentSizeFitter.FitMode.Unconstrained)
    {
        var fitter = self.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = horizontal;
        fitter.verticalFit = vertical;
        return fitter;
    }
    
    public static NoodleButton AddButton(this RectTransform self, Action onClick)
    {
        var btn = self.AddInitComponent<NoodleButton>(new Color(0.4f, 0.4f, 0.4f), onClick);
        return btn;
    }
    public static NoodleButton AddButton(this RectTransform self, string label, Action onClick)
    {
        var btn = self.AddInitComponent<NoodleButton>(new Color(0.4f, 0.4f, 0.4f), onClick);
        btn.Content.AddLabel(label, TextAlignmentOptions.Center);
        return btn;
    }

    public static NoodleList AddList(this RectTransform self)
    {
        return self.AddInitComponent<NoodleList>();
    }

    public static NoodleVerticalLayout AddVertical(
        this RectTransform self,
        float spacing = 1
    )
    {
        return self.AddInitComponent<NoodleVerticalLayout>(spacing);
    }
    public static VerticalLayoutGroup AddVerticalLayoutRaw(
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
        layout.childControlHeight = false;

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return layout;
    }

    public static RectTransform IgnoreLayout(this RectTransform self)
    {
        self.gameObject.RequireComponent<LayoutElement>().ignoreLayout = true;
        return self;
    }

    private const int RowHeight = 22;
    public static RectTransform Item(this VerticalLayoutGroup self, int height = RowHeight)
    {
        var parentRT = (RectTransform)self.transform;
        
        var child = parentRT.AddChild(RectTransform.Edge.Top).ExtendBottom(height);
        
        var layoutElement = child.RequireComponent<LayoutElement>();
        layoutElement.preferredHeight = height;

        return child;
    }
    
    public static (RectTransform left, RectTransform right) SplitHorizontal(this RectTransform self, float percentage, float bias = 0)
    {
        percentage = Mathf.Clamp01(percentage);

        var left = self.AddChild();
        left.anchorMin = new Vector2(0, 0);
        left.anchorMax = new Vector2(percentage, 1);

        var right = self.AddChild();
        right.anchorMin = new Vector2(percentage, 0);
        right.anchorMax = new Vector2(1, 1);
        left.offsetMin = right.offsetMax = Vector2.zero;
        left.offsetMax = right.offsetMin = new Vector2(bias, 0);

        return (left, right);
    }
    
    public static (RectTransform top, RectTransform bottom) SplitVertical(this RectTransform self, float percentage, float bias = 0)
    {
        percentage = Mathf.Clamp01(percentage);

        var top = self.AddChild();
        top.anchorMin = new Vector2(0, 1 - percentage);
        top.anchorMax = new Vector2(1, 1);

        var bottom = self.AddChild();
        bottom.anchorMin = new Vector2(0, 0);
        bottom.anchorMax = new Vector2(1, 1 - percentage);
        
        top.offsetMin = bottom.offsetMax = Vector2.zero;
        top.offsetMax = bottom.offsetMin = new Vector2(0, bias);

        return (top, bottom);
    }
    
    public static RectTransform Field(this RectTransform self, string title, float split = 0.5f, float bias = 0.0f)
    {
        var row = self.AddChild();
        row.name = $"{title}_Field";
        
        var (labelRect, controlRect) = row.SplitHorizontal(split, bias);
        
        labelRect.offsetMin = Vector2.right * 4;
        labelRect.AddLabel(title, TextAlignmentOptions.Left);
        return controlRect;
    }

    public static NoodleDropdown AddDropdown(this RectTransform self, params string[] options)
    {
        var dropdown = self.AddInitComponent<NoodleDropdown>();
        dropdown.SetOptions(options);
        return dropdown;
    }

    public static NoodleEnumDropdown AddDropdown<TEnum>(this RectTransform self) where TEnum : struct, Enum
    {
        var dropdown = self.AddInitComponent<NoodleEnumDropdown>(typeof(TEnum));
        return dropdown;
    }
    
    public static NoodleEnumDropdown AddDropdown<TEnum>(this RectTransform self, Action<TEnum> onChange) where TEnum : struct, Enum
    {
        var dropdown = self.AddDropdown<TEnum>();
        dropdown.SetOnChange(onChange);
        return dropdown;
    }

    public static NoodleToggle AddToggle(this RectTransform self)
    {
        const int toggleSize = RowHeight;
        var toggleRect = self.AddChild();
        toggleRect.anchorMin = new Vector2(0, 0.5f);
        toggleRect.anchorMax = new Vector2(0, 0.5f);
        toggleRect.offsetMin = Vector2.zero;
        toggleRect.offsetMax = Vector2.zero;
        
        toggleRect.ExtendRight(toggleSize).ExtendTop(toggleSize / 2).ExtendBottom(toggleSize / 2);
        return toggleRect.AddInitComponent<NoodleToggle>();
    }

    public static RectTransform AddBorder(this RectTransform self, RectTransform.Edge edge, int thickness = 1, Color? color = null)
    {
        var rect = self.AddChild(edge);
        rect.AddImage(null, color ?? new Color(1, 1, 1, 0.05f)).DisableRaycasts();
        Func<RectTransform, float, RectTransform> func = edge switch
        {
            RectTransform.Edge.Top => ExtendTop,
            RectTransform.Edge.Bottom => ExtendBottom,
            RectTransform.Edge.Left => ExtendLeft,
            RectTransform.Edge.Right => ExtendRight,
            _ => (r, _) => r
        };
        func(rect, thickness);
        return self;
    }
    
    public static RectTransform AddGetBorder(this RectTransform self, RectTransform.Edge edge, int thickness = 1, Color? color = null)
    {
        var rect = self.AddChild(edge);
        rect.AddImage(null, color ?? new Color(1, 1, 1, 0.05f)).DisableRaycasts();
        Func<RectTransform, float, RectTransform> func = edge switch
        {
            RectTransform.Edge.Top => ExtendTop,
            RectTransform.Edge.Bottom => ExtendBottom,
            RectTransform.Edge.Left => ExtendLeft,
            RectTransform.Edge.Right => ExtendRight,
            _ => (r, _) => r
        };
        func(rect, thickness);
        return rect;
    }
}
