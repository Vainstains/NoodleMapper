using System;
using System.Collections.Generic;
using Beatmap.Base;
using HarmonyLib;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NoodleMapper.Utils;
using Object = UnityEngine.Object;

namespace NoodleMapper.UI.Components;

public abstract class Window : MonoBehaviour
{
    protected RectTransform RT = null!;
    protected RectTransform ContentRect = null!;
    
    public abstract string WindowName { get; }
    
    protected virtual void Init()
    {
        var bg = RT.AddImage(
            StaticAssets.RoundRectBorderedSharp,
            new Color(0.25f, 0.25f, 0.25f)
        );
        
        const float titleBarThickness = 24;
        var titleBar = RT.AddChild(RectTransform.Edge.Top).ExtendBottom(titleBarThickness);
        var titleBarImg = titleBar.AddImage(
            StaticAssets.RoundRectBordered,
            new Color(0.25f, 0.25f, 0.25f)
        );

        ContentRect = RT.AddChild().InsetTop(titleBarThickness).Inset(2);

        var titleBarDraggerRect = titleBar.AddChild().InsetRight(titleBarThickness);
        var titleBarCloseButtonRect = titleBar.AddChild(RectTransform.Edge.Right)
            .ExtendLeft(titleBarThickness).Inset(4);

        titleBarDraggerRect.AddClearImage();
        titleBarDraggerRect.AddInitComponent<DragHandler>((PointerEventData e) =>
        {
            RT.anchoredPosition += e.delta;
            RT.SetAsLastSibling();
        });

        titleBarCloseButtonRect.AddInitComponent<NoodleButton>(
            new Color(0.9f, 0.1f, 0.3f),
            (Action)Close
        );
        var closeButtonImage = titleBarCloseButtonRect.AddChild().AddImage(StaticAssets.CloseButton);
        closeButtonImage.raycastTarget = false;
        closeButtonImage.sprite = StaticAssets.CloseButton;

        titleBarDraggerRect.AddChild().InsetLeft(3).AddLabel(WindowName);

        var lowerCorner = RT.AddChildBottomRight().ExtendLeft(16).ExtendTop(16);
        lowerCorner.AddImage(StaticAssets.WindowCorner);
        lowerCorner.AddInitComponent<DragHandler>((PointerEventData e) =>
        {
            var offsetMax = RT.offsetMax;
            offsetMax.x += e.delta.x;
            var offsetMin = RT.offsetMin;
            offsetMin.y += e.delta.y;
            
            offsetMax.x = Mathf.Max(offsetMax.x, offsetMin.x + 100);
            offsetMin.y = Mathf.Min(offsetMin.y, offsetMax.y - 100);
            
            RT.offsetMax = offsetMax;
            RT.offsetMin = offsetMin;
        });
    }
    
    public void Close()
    {
        Destroy(gameObject);
    }

    protected static TWindow CreateWindow<TWindow>() where TWindow : Window
    {
        var windowContainer = FindObjectOfType<WindowContainer>().ContainerRect;
        var windowRect = windowContainer.AddChild();
        windowRect.pivot = Vector2.up;
        windowRect.anchorMin = Vector2.up;
        windowRect.anchorMax = Vector2.up;
        windowRect.sizeDelta = new Vector2(200, 300);
        windowRect.anchoredPosition = new Vector2(50, -20);

        var window = windowRect.gameObject.AddComponent<TWindow>();
        window.RT = windowRect;
        window.Init();
        
        return window;
    }
}

public class WindowContainer : MonoBehaviour
{
    public RectTransform ContainerRect { get; set; }

    public static void EnsureContainerExists()
    {
        var windowContainer = FindObjectOfType<WindowContainer>();
        if (windowContainer != null)
            return;
        var mapEditorUI = FindFirstObjectByType<MapEditorUI>();
        var groups = new List<CanvasGroup>(mapEditorUI.MainUIGroup);
        var newGroupGO = new GameObject("WindowContainer");
        newGroupGO.transform.SetParent(mapEditorUI.transform);
        var canvas = newGroupGO.AddComponent<Canvas>();
        canvas.sortingOrder = 9999;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = newGroupGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        var group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        group.interactable = true;
        var raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.All;
        raycaster.blockingMask = ~0;
        groups.Add(group);
        windowContainer = newGroupGO.AddComponent<WindowContainer>();
        windowContainer.ContainerRect = canvas.RequireComponent<RectTransform>();
    }
}