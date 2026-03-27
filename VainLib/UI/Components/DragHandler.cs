using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VainLib.UI.Components;

public class DragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Action<PointerEventData>? m_onDrag;
    private Action<PointerEventData>? m_onBeginDrag;
    private Action<PointerEventData>? m_onEndDrag;
    
    public void OnDrag(PointerEventData eventData) => m_onDrag?.Invoke(eventData);
    public void OnBeginDrag(PointerEventData eventData) => m_onBeginDrag?.Invoke(eventData);
    public void OnEndDrag(PointerEventData eventData) => m_onEndDrag?.Invoke(eventData);

    public DragHandler SetOnDrag(Action<PointerEventData> onDrag)
    {
        m_onDrag = onDrag;
        return this;
    }

    public DragHandler SetOnBeginDrag(Action<PointerEventData> onBeginDrag)
    {
        m_onBeginDrag = onBeginDrag;
        return this;
    }

    public DragHandler SetOnEndDrag(Action<PointerEventData> onEndDrag)
    {
        m_onEndDrag = onEndDrag;
        return this;
    }
}