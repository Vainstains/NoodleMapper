using System.Collections.Generic;
using NoodleMapper.Utils;
using NoodleMapper.Utils.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.UI.Components;

public class WindowContainer : MonoBehaviour
{
    public RectTransform ContainerRect { get; set; }

    public static void EnsureContainerExists(CMScene cmScene)
    {
        var windowContainer = FindObjectOfType<WindowContainer>();
        if (windowContainer != null)
            return;
        
        var containerObject = new GameObject("WindowContainer");
        var canvas = containerObject.AddComponent<Canvas>();
        canvas.sortingOrder = 0;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = containerObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        var group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        group.interactable = true;
        var raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.All;
        raycaster.blockingMask = ~0;
        
        windowContainer = containerObject.AddComponent<WindowContainer>();
        windowContainer.ContainerRect = canvas.RequireComponent<RectTransform>();
        
        if (cmScene == CMScene.Mapper)
            AddGroupToMapEditorUI(group);
    }

    private static void AddGroupToMapEditorUI(CanvasGroup group)
    {
        var mapEditorUI = FindFirstObjectByType<MapEditorUI>();
        group.transform.SetParent(mapEditorUI.transform);
        var groups = new List<CanvasGroup>(mapEditorUI.MainUIGroup);
        groups.Add(group);
        mapEditorUI.MainUIGroup = groups.ToArray();
        group.transform.SetAsFirstSibling();
    }
}