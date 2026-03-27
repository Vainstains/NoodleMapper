using System;
using VainMapper.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VainMapper.UI.Components;

public class NoodleButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const int DepressDeltaY = 1;

    private Color m_mainColor;
    private Color m_tintColor;
    public Color MainColor
    {
        get => m_mainColor;
        set
        {
            m_mainColor = value;
            SetState(m_currentSprite, m_currentContentDelta);
        }
    }

    private RectTransform m_content;
    public RectTransform Content => m_content;

    private Action? m_onClick;
    private Image? m_image;

    private Sprite m_currentSprite;
    private Vector2 m_currentContentDelta;
    private Sprite? m_overrideOverrideSprite;
    
    
    private TMP_Text? m_tmpText;
    public TMP_Text? Text => this.GetComponentInChildren(ref m_tmpText);

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
        m_image!.color = m_mainColor + m_tintColor;
    }

    private void Init(Color color, Action onClick)
    {
        m_mainColor = color;
        m_onClick = onClick;
        m_image = gameObject.AddComponent<Image>();
        m_image.color = MainColor;
        m_content = gameObject.RequireComponent<RectTransform>().AddChild().InsetBottom(DepressDeltaY);
        
        SetState(PluginResources.LoadSprite("Resources/ButtonRaised.png"), Vector2.zero);
    }

    public void SetOnClick(Action onClick)
    {
        m_onClick = onClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        m_onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_tintColor = new Color(0.08f, 0.08f, 0.1f, 0.0f);
        SetState(m_currentSprite, m_currentContentDelta);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_tintColor = new Color(0, 0, 0, 0);
        SetState(m_currentSprite, m_currentContentDelta);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetState(PluginResources.LoadSprite("Resources/ButtonDepressed.png"), Vector2.down * DepressDeltaY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetState(PluginResources.LoadSprite("Resources/ButtonRaised.png"), Vector2.zero);
    }
}
