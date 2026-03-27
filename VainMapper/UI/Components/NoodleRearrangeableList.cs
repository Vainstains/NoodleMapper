using System.Collections;
using System.Collections.Generic;
using VainMapper.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VainMapper.UI.Components;

/// <summary>
/// Sorta like a list, but items can be dragged on the vertical axis by a small handle on the left side,
/// and dropping the item fires a swap event (fromidx, toidx).
/// </summary>
public class NoodleRearrangeableList : MonoBehaviour
{
    public class ListItem : MonoBehaviour
    {
        private const int HandleWidth = 26;

        private RectTransform m_rt = null!;
        private RectTransform m_content = null!;
        private RectTransform m_handle = null!;
        private DragHandler m_drag = null!;

        private NoodleRearrangeableList m_parent = null!;
        public RectTransform TopHighlight = null!;
        public RectTransform BottomHighlight = null!;
        public RectTransform Rect => m_rt;
        public RectTransform Content => m_content;
        public RectTransform BG;
        private float m_height = 32f;
        public float Height => m_height;

        private void Init(NoodleRearrangeableList parent, float height)
        {
            m_parent = parent;
            m_height = height;

            m_rt = gameObject.RequireComponent<RectTransform>();
            
            m_rt.anchorMin = new Vector2(0, 1);
            m_rt.anchorMax = new Vector2(1, 1);
            m_rt.pivot = new Vector2(0.5f, 1);

            m_rt.sizeDelta = new Vector2(0, height);

            // background
            BG = m_rt.AddChild().AddImage(PluginResources.LoadSprite("Resources/RoundRectBordered.png"), new Color(0.3f, 0.3f, 0.3f))
                .DisableRaycasts().RequireComponent<RectTransform>();

            
            m_handle = BG.AddChild(RectTransform.Edge.Left).ExtendRight(HandleWidth);
            m_handle.AddClearImage();
            m_handle.AddSpriteImage(PluginResources.LoadSprite("Resources/DragHandle.png"), new Color(0, 0, 0, 0.7f));

            m_drag = m_handle.AddDragHandler()
                .SetOnBeginDrag(OnBeginDrag)
                .SetOnDrag(OnDrag)
                .SetOnEndDrag(OnEndDrag);

            m_handle.AddGetBorder(RectTransform.Edge.Right, color: new Color(0, 0, 0, 0.7f)).InsetTop(4).InsetBottom(4);

            // content area
            m_content = BG.AddChild().InsetLeft(HandleWidth).Inset(2);

            TopHighlight = BG.AddGetBorder(RectTransform.Edge.Top, 2, new Color(0.5f, 0.7f, 1.0f, 0.6f));
            BottomHighlight = BG.AddGetBorder(RectTransform.Edge.Bottom, 2, new Color(0.5f, 0.7f, 1.0f, 0.6f));
            
            TopHighlight.gameObject.SetActive(false);
            BottomHighlight.gameObject.SetActive(false);
        }

        private void OnBeginDrag(PointerEventData e)
        {
            m_parent.BeginDrag(this);
        }

        private void OnDrag(PointerEventData e)
        {
            m_parent.Drag(this, e.delta.y);
        }

        private void OnEndDrag(PointerEventData e)
        {
            m_parent.EndDrag(this);
        }
    }
    
    public delegate void SwapHandler(int fromIdx, int toIdx);
    private SwapHandler? m_swapHandler = null;

    private RectTransform m_rt = null!;
    private readonly List<ListItem> m_items = new();

    private ListItem? m_dragging;
    private float m_dragOffset;

    private const float Spacing = 1f;

    private void Init()
    {
        m_rt = gameObject.RequireComponent<RectTransform>();
        m_rt.pivot = Vector2.up;
    }

    public NoodleRearrangeableList SetOnSwap(SwapHandler onSwap)
    {
        m_swapHandler = onSwap;
        return this;
    }
    public ListItem AddItem(float height = 32)
    {
        var child = m_rt.AddChild(RectTransform.Edge.Top);
        child.name = "Item";

        var item = child.AddInitComponent<ListItem>(this, height);

        m_items.Add(item);

        RefreshLayout(0.0f);
        
        return item;
    }

    public void RemoveItem(int index)
    {
        if (index < 0 || index >= m_items.Count) return;

        Destroy(m_items[index].gameObject);
        m_items.RemoveAt(index);

        RefreshLayout(0.0f);
    }

    private int m_oldItemIdx;
    private int m_newItemIdx;
    internal void BeginDrag(ListItem item)
    {
        m_dragging = item;
        m_dragOffset = item.Rect.anchoredPosition.y;
        m_dragging.BG.offsetMin += new Vector2(10, 0);
        m_oldItemIdx = m_items.IndexOf(item);

        item.Rect.SetAsLastSibling();
    }

    internal void Drag(ListItem item, float deltaY)
    {
        if (m_dragging != item) return;

        var pos = item.Rect.anchoredPosition;
        pos.y += deltaY;
        item.Rect.anchoredPosition = pos;
        
        m_items.Sort((a, b) => GetMidY(a).CompareTo(GetMidY(b)));
        m_newItemIdx = m_items.IndexOf(item);

        for (var i = 0; i < m_items.Count; i++)
        {
            m_items[i].BottomHighlight.gameObject.SetActive(i == m_newItemIdx - 1);
            m_items[i].TopHighlight.gameObject.SetActive(i == m_newItemIdx + 1);
        }
    }
    
    private float GetMidY(ListItem item)
    {
        // anchoredPosition.y is negative going downward
        // convert to "distance from top"
        float top = -item.Rect.anchoredPosition.y;
        return top + item.Height * 0.5f;
    }

    internal void EndDrag(ListItem item)
    {
        if (m_dragging != item) return;
        
        m_dragging = null;
        
        for (var i = 0; i < m_items.Count; i++)
        {
            m_items[i].BottomHighlight.gameObject.SetActive(false);
            m_items[i].TopHighlight.gameObject.SetActive(false);
        }
        
        item.BG.offsetMin = new Vector2(0, 0);

        RefreshLayout(0.08f);

        if (m_newItemIdx != m_oldItemIdx)
            m_swapHandler?.Invoke(m_oldItemIdx, m_newItemIdx);
    }

    // -----------------------------
    // Layout
    // -----------------------------
    
    private Coroutine? m_slidingCoroutine = null;
    private float[] m_oldYPositions = null!;
    private float[] m_newYPositions = null!;
    private void RefreshLayout(float moveTime)
    {
        if (m_slidingCoroutine != null)
        {
            StopCoroutine(m_slidingCoroutine);
            m_slidingCoroutine = null;
        }
        
        m_oldYPositions = new float[m_items.Count];
        m_newYPositions = new float[m_items.Count];
        
        float y = 0;

        for (var i = 0; i < m_items.Count; i++)
        {
            var item = m_items[i];
            var rt = item.Rect;
            
            m_oldYPositions[i] = rt.anchoredPosition.y;
            m_newYPositions[i] = -y;

            y += item.Height + Spacing;
        }

        var size = m_rt.sizeDelta;
        size.y = Mathf.Max(0, y - Spacing);
        m_rt.sizeDelta = size;

        m_slidingCoroutine = StartCoroutine(SlideElementsToNewPositions(moveTime));
    }

    private void SetSlideInterpolation(float t)
    {
        for (var i = 0; i < m_items.Count; i++)
        {
            var item = m_items[i];
            var rt = item.Rect;
            
            rt.anchoredPosition = new Vector2(0, Mathf.Lerp(
                m_oldYPositions[i], m_newYPositions[i], t));
        }
    }

    private IEnumerator SlideElementsToNewPositions(float moveTime)
    {
        float time = 0;

        while (time < moveTime)
        {
            float t = time / moveTime;
            
            t = Mathf.SmoothStep(0, 1, t);
            
            SetSlideInterpolation(t);
            
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        
        SetSlideInterpolation(1);
    }
}
