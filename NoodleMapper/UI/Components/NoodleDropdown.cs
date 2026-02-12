using System;
using System.Collections.Generic;
using NoodleMapper.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.UI.Components;

public class NoodleDropdown : MonoBehaviour
{
    private TMP_Dropdown m_dropdown = null!;
    private Action<int>? m_onChange;
    private void Init()
    {
        m_dropdown = gameObject.AddComponent<TMP_Dropdown>();
        var rt = gameObject.RequireComponent<RectTransform>();
        var img = rt.AddImage(Globals.Assets.RoundRectBordered, new Color(0.35f, 0.35f, 0.35f));

        var label = rt.AddChild().InsetLeft(4).AddLabel("option");

        var template = rt.AddChild(RectTransform.Edge.Bottom).ExtendBottom(100);

        template.AddImage(Globals.Assets.RoundRectBordered, new Color(0.39f, 0.39f, 0.39f));

        var templateItem = template.AddChild(RectTransform.Edge.Top).ExtendBottom(20);
        var itemLabel = templateItem.AddChild().InsetLeft(4).AddLabel("item");
        var itemBg = templateItem.AddImage(Globals.Assets.RoundRect, new Color(0.25f, 0.4f, 0.8f, 1.0f));
        var toggle = templateItem.RequireComponent<Toggle>();
        toggle.graphic = itemBg;
        toggle.isOn = true;
        toggle.transition = Selectable.Transition.None;
        
        template.gameObject.SetActive(false);

        m_dropdown.template = template;
        m_dropdown.targetGraphic = img;
        m_dropdown.captionText = label;
        m_dropdown.itemText = itemLabel;
        
        m_dropdown.onValueChanged.AddListener(v => m_onChange?.Invoke(v));
    }
    public NoodleDropdown SetOptions(IEnumerable<string> options)
    {
        m_dropdown.options.Clear();
        foreach (var option in options)
        {
            m_dropdown.options.Add(new TMP_Dropdown.OptionData(option));
        }

        return this;
    }

    public NoodleDropdown SetSelectedOption(int index)
    {
        m_dropdown.SetValueWithoutNotify(index);
        return this;
    }

    public NoodleDropdown SetOnChange(Action<int> onChange)
    {
        m_onChange = onChange;
        return this;
    }
}