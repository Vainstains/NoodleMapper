using System;
using NoodleMapper.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoodleMapper.UI.Components;

public class NoodleButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const int DepressDeltaY = 1;
    
    public Color MainColor;

    private RectTransform m_content;
    public RectTransform Content => m_content;

    private Action? m_onClick;
    private Image? m_image;

    private Sprite m_currentSprite;
    private Vector2 m_currentContentDelta;
    private Sprite? m_overrideOverrideSprite;
    public Sprite? OverrideSprite
    {
        get => m_overrideOverrideSprite;
        set => SetOverrideSprite(value);
    }
    
    private void SetOverrideSprite(Sprite? value)
    {
        m_overrideOverrideSprite = value;
        SetState(m_currentSprite, m_currentContentDelta);
    }

    private void SetState(Sprite sprite, Vector2 contentDelta)
    {
        m_currentSprite = sprite;
        m_currentContentDelta = contentDelta;
        if (m_overrideOverrideSprite)
        {
            sprite = m_overrideOverrideSprite!;
            contentDelta = Vector2.zero;
        }
        
        m_content.anchoredPosition = contentDelta;

        m_image!.sprite = sprite;
        if (sprite == null)
        {
            m_image.type = Image.Type.Simple;
            return;
        }

        m_image!.type = sprite.border.sqrMagnitude > 0.5 ? Image.Type.Sliced : Image.Type.Simple;
    }

    private void Init(Color color, Action onClick)
    {
        MainColor = color;
        m_onClick = onClick;
        m_image = gameObject.AddComponent<Image>();
        m_image.color = MainColor;
        m_content = gameObject.RequireComponent<RectTransform>().AddChild().InsetBottom(DepressDeltaY);
        
        SetState(Globals.Assets.ButtonRaised, Vector2.zero);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        m_onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_image != null)
            m_image.color = MainColor + new Color(0.08f, 0.08f, 0.1f, 0.0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_image != null)
            m_image.color = MainColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetState(Globals.Assets.ButtonDepressed, Vector2.down * DepressDeltaY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetState(Globals.Assets.ButtonRaised, Vector2.zero);
    }
}