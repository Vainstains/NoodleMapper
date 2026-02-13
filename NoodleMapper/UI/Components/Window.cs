using System;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using HarmonyLib;
using NoodleMapper.Managers;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using NoodleMapper.Utils;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NoodleMapper.UI.Components;

public abstract class Window : MonoBehaviour
{
    private static readonly List<Window> s_windows = new ();
    protected RectTransform WindowRect = null!;
    private RectTransform m_contentRect = null!;
    private const float TitleBarThickness = 24;
    public abstract string WindowName { get; }

    protected abstract void BuildUI(RectTransform content);

    protected void SetUIDirty()
    {
        CancelInvoke(nameof(RebuildUI));
        Invoke(nameof(RebuildUI), 0.05f);
    }
    
    private void RebuildUI()
    {
        CreateContentRect();
        BuildUI(m_contentRect);
    }
    
    protected void SetPositionDirty()
    {
        CancelInvoke(nameof(SavePosition));
        Invoke(nameof(SavePosition), 0.05f);
    }
    
    protected void LoadPosition()
    {
        Vector4 pos = Utils.Settings.Get($"(Window) {WindowName}", new Vector4(
            50, -20,
            400, 300
        ));
        
        WindowRect.anchoredPosition = new Vector2(pos.x, pos.y);
        WindowRect.sizeDelta = new Vector2(pos.z, pos.w);
        ClampToScreen(0.0f);
    }

    protected void SavePosition()
    {
        Utils.Settings.Set($"(Window) {WindowName}", new Vector4(
            WindowRect.anchoredPosition.x,
            WindowRect.anchoredPosition.y,
            WindowRect.sizeDelta.x,
            WindowRect.sizeDelta.y
        ));
    }

    protected static void SetupScrolling(ref RectTransform content)
    {
        content = content.AddVerticalScrollView(out var scrollRect);
        // give 12px of extra space on the bottom right to avoid obscuring the size handle
        scrollRect.verticalScrollbar.RequireComponent<RectTransform>().InsetBottom(12);
    }

    private void CreateContentRect()
    {
        if (m_contentRect)
            Destroy(m_contentRect.gameObject);
        m_contentRect = WindowRect.AddChild().InsetTop(TitleBarThickness).Inset(2).InsetLeft(4).InsetRight(4);
    }
    
    private void Init()
    {
        float shadowRadius = Globals.Assets.Shadow.texture.width * 0.5f;
        var shadowImg = WindowRect.AddChild().Extend(shadowRadius * 0.85f).InsetTop(shadowRadius * 0.1f).AddImage(
            Globals.Assets.Shadow,
            new Color(0, 0, 0, 0.9f));
        shadowImg.raycastTarget = false;
        
        var bg = WindowRect.AddChild().AddImage(
            Globals.Assets.RoundRect,
            new Color(0.22f, 0.22f, 0.22f));
        var titleBar = WindowRect.AddChild(RectTransform.Edge.Top).ExtendBottom(TitleBarThickness);
        var titleBarImg = titleBar.AddImage(
            Globals.Assets.TitleBar,
            new Color(0.35f, 0.35f, 0.35f));

        WindowRect.AddChild().AddImage(
            Globals.Assets.RoundRectBorderOnly,
            new Color(0.4f, 0.4f, 0.4f)).DisableRaycasts();

        var titleBarDraggerRect = titleBar.AddChild().InsetRight(TitleBarThickness);
        var titleBarCloseButtonRect = titleBar.AddChild(RectTransform.Edge.Right)
            .ExtendLeft(TitleBarThickness).Inset(4);
        
        titleBarDraggerRect.AddDragHandler().SetOnDrag((PointerEventData e) =>
        {
            WindowRect.Move(e.delta).SetAsLastSibling();
            SetPositionDirty();
        }).SetOnBeginDrag(_ => StopAllCoroutines()).SetOnEndDrag(_ => ClampToScreen());;

        var closeButton = titleBarCloseButtonRect.AddInitComponent<NoodleButton>(
            new Color(0.9f, 0.1f, 0.3f),
            (Action)Close);
        
        closeButton.Content.AddChild().AddImage(Globals.Assets.CloseButton).DisableRaycasts();

        titleBarDraggerRect.AddChild().InsetLeft(3).AddLabel(WindowName);

        var lowerCorner = WindowRect.AddChildBottomRight().ExtendLeft(16).ExtendTop(16);
        lowerCorner.AddImage(Globals.Assets.WindowCorner);
        lowerCorner.AddDragHandler().SetOnDrag((PointerEventData e) =>
        {
            var offsetMax = WindowRect.offsetMax;
            offsetMax.x += e.delta.x;
            var offsetMin = WindowRect.offsetMin;
            offsetMin.y += e.delta.y;
            
            offsetMax.x = Mathf.Max(offsetMax.x, offsetMin.x + 100);
            offsetMin.y = Mathf.Min(offsetMin.y, offsetMax.y - 100);
            
            WindowRect.offsetMax = offsetMax;
            WindowRect.offsetMin = offsetMin;
            SetPositionDirty();
        }).SetOnBeginDrag(_ => StopAllCoroutines()).SetOnEndDrag(_ => ClampToScreen());
    }
    
    private Coroutine? m_slideRoutine;
    private void ClampToScreen(float slideDuration = 0.2f)
    {
        var parent = WindowRect.parent as RectTransform;
        if (!parent) return;

        Vector2 pos = WindowRect.anchoredPosition;
        Vector2 size = WindowRect.sizeDelta;
        Rect pr = parent!.rect;

        // Because pivot = (0,1)
        float minX = 2;
        float maxX = pr.width - size.x - 2;

        float maxY = -2;
        float minY = -(pr.height - size.y - 2);

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        
        if (m_slideRoutine != null)
            StopCoroutine(m_slideRoutine);
        m_slideRoutine = StartCoroutine(SlideToPosition(pos, slideDuration));
    }

    private IEnumerator SlideToPosition(Vector2 target, float slideDuration = 0.2f)
    {
        Vector2 start = WindowRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = t * t * (3f - 2f * t);

            WindowRect.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        WindowRect.anchoredPosition = target;
        SavePosition();
        m_slideRoutine = null;
    }

    
    public void Close()
    {
        Debug.Log($"Closing window: ({GetType().Name}) {WindowName}");
        Destroy(gameObject);
    }

    protected virtual void PostInit() {}

    protected static TNewWindow CreateWindow<TNewWindow>() where TNewWindow : Window
    {
        var windowContainer = FindObjectOfType<WindowContainer>().ContainerRect;
        var windowRect = windowContainer.AddChild();
        windowRect.pivot = Vector2.up;
        windowRect.anchorMin = Vector2.up;
        windowRect.anchorMax = Vector2.up;

        var window = windowRect.gameObject.AddComponent<TNewWindow>();
        Debug.Log($"Creating window: ({window.GetType().Name}) {window.WindowName}");
        s_windows.Add(window);
        window.WindowRect = windowRect;
        window.Init();
        
        window.PostInit();
        window.LoadPosition();
        window.RebuildUI();
        
        return window;
    }

    private static IReadOnlyList<Window> GetWindows()
    {
        for (int i = s_windows.Count - 1; i >= 0; i--)
            if (s_windows[i] == null)
                s_windows.RemoveAt(i);
        return s_windows;
    }

    public static void RebuildAll()
    {
        foreach (var window in GetWindows())
            window.SetUIDirty();
    }
}