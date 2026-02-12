using System.Collections.Generic;
using NoodleMapper.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.UI.Components;

public class NoodleVerticalLayout : MonoBehaviour
{
    private RectTransform m_rt = null!;
    private VerticalLayoutGroup m_layout = null!;
    private List<RectTransform> m_itemRects = null!;
    public void Init(float spacing)
    {
        m_rt = gameObject.RequireComponent<RectTransform>();
        m_rt.pivot = Vector2.up;
        
        m_layout = m_rt.AddVerticalLayoutRaw(0, spacing);
        m_itemRects = new List<RectTransform>();
    }

    public RectTransform AddRow(int height = 26)
    {
        var itemRect = m_layout.Item(height: height);
        
        m_itemRects.Add(itemRect);

        RefreshSize();
        
        return itemRect;
    }

    public void RemoveRow(int index)
    {
        if (index < 0 || index >= m_itemRects.Count)
            return;
        
        var itemRect = m_itemRects[index];
        m_itemRects.RemoveAt(index);
        Destroy(itemRect.gameObject);
        RefreshSize();
    }

    private void RefreshSize()
    {
        // start out with the height contributed by any spacing
        float height = m_layout.spacing * (m_itemRects.Count - 1);
        
        // then add the rest
        foreach (var itemRect in m_itemRects)
            height += itemRect.sizeDelta.y;
        
        var sd = m_rt.sizeDelta;
        sd.y = height;
        m_rt.sizeDelta = sd;
    }
}