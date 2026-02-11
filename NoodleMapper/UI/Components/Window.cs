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
    
    protected virtual void Init()
    {
        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f);
        
        const float titleBarThickness = 20;
        var titleBar = RT.AddChild(RectTransform.Edge.Top).ExtendBottom(titleBarThickness);
        var titleBarImg = titleBar.gameObject.AddComponent<Image>();
        titleBarImg.color = new Color(0.15f, 0.15f, 0.15f);

        ContentRect = RT.AddChild().InsetTop(titleBarThickness).Inset(2);

        var titleBarDraggerRect = titleBar.AddChild().InsetRight(titleBarThickness);
        var titleBarCloseButtonRect = titleBar.AddChild(RectTransform.Edge.Right)
            .ExtendLeft(titleBarThickness).Inset(4);

        titleBarDraggerRect.gameObject.AddInitComponent<DragHandler>((PointerEventData e) =>
        {
            RT.anchoredPosition += e.delta;
            RT.SetAsLastSibling();
        });
        titleBarDraggerRect.gameObject.AddComponent<Image>().color = Color.clear;

        titleBarCloseButtonRect.gameObject.AddInitComponent<NoodleButton>(
            new Color(0.8f, 0.1f, 0.2f),
            (Action)Close
        );
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