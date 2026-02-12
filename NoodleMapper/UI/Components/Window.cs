using System;
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
    
    public abstract string WindowName { get; }

    protected abstract void BuildUI(RectTransform content);

    protected void SetUIDirty()
    {
        CancelInvoke(nameof(RebuildUI));
        Invoke(nameof(RebuildUI), 0.05f);
    }
    
    private void RebuildUI()
    {
        for (int i = m_contentRect.transform.childCount - 1; i >= 0; i--)
        {
            var child = m_contentRect.transform.GetChild(i);
            Destroy(child.gameObject);
        }

        BuildUI(m_contentRect);
    }
    
    protected void SetPositionDirty()
    {
        CancelInvoke(nameof(SavePosition));
        Invoke(nameof(SavePosition), 0.05f);
    }
    
    protected void LoadPosition()
    {
        Vector4 pos = Utils.Settings.Get($"Window_{WindowName}", new Vector4(
            50, -20,
            400, 300
        ));
        
        WindowRect.anchoredPosition = new Vector2(pos.x, pos.y);
        WindowRect.sizeDelta = new Vector2(pos.z, pos.w);
    }

    protected void SavePosition()
    {
        Utils.Settings.Set($"Window_{WindowName}", new Vector4(
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

    private void Init()
    {
        float shadowRadius = Globals.Assets.Shadow.texture.width * 0.5f;
        var shadowImg = WindowRect.AddChild().Extend(shadowRadius * 0.85f).InsetTop(shadowRadius * 0.1f).AddImage(
            Globals.Assets.Shadow,
            new Color(0, 0, 0, 0.9f));
        shadowImg.raycastTarget = false;
        
        var windowRect = WindowRect.AddChild();
        var bg = windowRect.AddChild().AddImage(
            Globals.Assets.RoundRect,
            new Color(0.22f, 0.22f, 0.22f));
        
        const float titleBarThickness = 24;
        var titleBar = windowRect.AddChild(RectTransform.Edge.Top).ExtendBottom(titleBarThickness);
        var titleBarImg = titleBar.AddImage(
            Globals.Assets.TitleBar,
            new Color(0.35f, 0.35f, 0.35f));

        windowRect.AddChild().AddImage(
            Globals.Assets.RoundRectBorderOnly,
            new Color(0.4f, 0.4f, 0.4f)).DisableRaycasts();

        m_contentRect = windowRect.AddChild().InsetTop(titleBarThickness).Inset(2).InsetLeft(4).InsetRight(4);

        var titleBarDraggerRect = titleBar.AddChild().InsetRight(titleBarThickness);
        var titleBarCloseButtonRect = titleBar.AddChild(RectTransform.Edge.Right)
            .ExtendLeft(titleBarThickness).Inset(4);

        titleBarDraggerRect.AddClearImage();
        titleBarDraggerRect.AddInitComponent<DragHandler>((PointerEventData e) =>
        {
            WindowRect.Move(e.delta).SetAsLastSibling();
            SetPositionDirty();
        });

        var closeButton = titleBarCloseButtonRect.AddInitComponent<NoodleButton>(
            new Color(0.9f, 0.1f, 0.3f),
            (Action)Close);
        
        closeButton.Content.AddChild().AddImage(Globals.Assets.CloseButton).DisableRaycasts();

        titleBarDraggerRect.AddChild().InsetLeft(3).AddLabel(WindowName);

        var lowerCorner = windowRect.AddChildBottomRight().ExtendLeft(16).ExtendTop(16);
        lowerCorner.AddImage(Globals.Assets.WindowCorner);
        lowerCorner.AddInitComponent<DragHandler>((PointerEventData e) =>
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
        });
    }
    
    public void Close()
    {
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