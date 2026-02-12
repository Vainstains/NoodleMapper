using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NoodleMapper.UI.Components;

public class DragHandler : MonoBehaviour, IDragHandler
{
    private Action<PointerEventData>? m_onDrag;

    public void Init(Action<PointerEventData> onDrag)
    {
        m_onDrag = onDrag;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        m_onDrag?.Invoke(eventData);
    }
}