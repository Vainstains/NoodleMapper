using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

public class NoodleButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler,
    IPointerExitHandler
{
    public Color MainColor;
    
    private Action? m_onClick;
    private Image? m_image;

    private void Init(Color color, Action onClick)
    {
        MainColor = color;
        m_onClick = onClick;
        
        m_image = gameObject.AddComponent<Image>();
        m_image.color = MainColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        m_onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_image != null)
            m_image.color = MainColor + new Color(0.15f, 0.15f, 0.15f, 0.0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_image != null)
            m_image.color = MainColor;
    }
}