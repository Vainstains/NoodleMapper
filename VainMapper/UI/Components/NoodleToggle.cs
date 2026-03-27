using VainMapper.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace VainMapper.UI.Components;

public class NoodleToggle : MonoBehaviour
{
    public delegate void Setter(bool isOn);

    private bool m_isOn = false;
    
    private Image m_imageToEnable = null!;
    
    private Setter? m_setter;
    public bool IsOn
    {
        get => m_isOn;
        set
        {
            m_isOn = m_imageToEnable.enabled = value;
        }
    }

    public void Init()
    {
        gameObject.AddInitComponent<NoodleButton>(new Color(0.35f, 0.35f, 0.35f), () =>
        {
            IsOn = !IsOn;
            m_setter?.Invoke(IsOn);
        }).OverrideSprite = PluginResources.LoadSprite("Resources/RoundRectBorderedSharp.png");
        var rt = gameObject.RequireComponent<RectTransform>();
        m_imageToEnable = rt.AddChildCenter().Extend(8).AddImage(PluginResources.LoadSprite("Resources/RoundRect.png"));
        IsOn = false;
    }

    public NoodleToggle SetOnChange(Setter setter)
    {
        m_setter = setter;
        return this;
    }

    public NoodleToggle SetValue(bool isOn)
    {
        IsOn = isOn;
        return this;
    }
}
