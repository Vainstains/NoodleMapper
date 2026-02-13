using System;
using System.Collections.Generic;
using System.Linq;
using NoodleMapper.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoodleMapper.EditorThings;

public class RangeBarController : MonoBehaviour
{
    private class RangeBar : MonoBehaviour
    {
        public Image BarImage;
        public TextMeshProUGUI Text;

        public Image StartMarker;
        public Image EndMarker;
    }
        
    private AudioTimeSyncController m_atsc;
    private RectTransform m_parent;
    private Transform m_frontNoteGridScaling;
    private BeatSaberSongContainer m_song;
        
    private RangeBar m_rangeBarPrefab;
    private readonly List<CachedRangeBar> m_rangeBars = new List<CachedRangeBar>();
        
    private bool m_init;
    private float m_previousAtscBeat = -1;
        
    private class CachedRangeBar
    {
        public RangeBar RangeBar;
        public float Start; // Store as SongBpmTime consistently
        public float End;    // Store as SongBpmTime consistently
            
        public CachedRangeBar(RangeBar rangeBar, float start, float end)
        {
            RangeBar = rangeBar;
            Start = start;
            End = end;
        }
    }
        
    public static RangeBarController Create()
    {
        var measureLines = GameObject.FindObjectOfType<MeasureLinesController>();
        if (measureLines == null)
        {
            Debug.LogError("Could not find MeasureLinesController!");
            return null;
        }
            
        var controller = measureLines.gameObject.AddComponent<RangeBarController>();
            
        controller.m_atsc = GameObject.FindObjectOfType<AudioTimeSyncController>();
        controller.m_frontNoteGridScaling = GameObject.Find("FrontNoteGrid")?.transform;
        controller.m_parent = GameObject.Find("Measure Lines Canvas")?.transform as RectTransform;
        controller.m_song = BeatSaberSongContainer.Instance;
            
        controller.m_rangeBarPrefab = controller.CreateRangeBarPrefab();
            
        EditorScaleController.EditorScaleChangedEvent += controller.OnEditorScaleChanged;
            
        return controller;
    }
        
    private RangeBar CreateRangeBarPrefab()
    {
        var go = new GameObject("RangeBarPrefab", typeof(Image));
            
        Image CreateMarker(string name)
        {
            var markerGo = new GameObject(name, typeof(Image));
            markerGo.SetActive(false);

            var img = markerGo.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite = Globals.Assets.Endpoint;

            var rect = img.rectTransform;
            rect.sizeDelta = new Vector2(0.45f, 0.45f);
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.SetParent(go.transform, false);

            return img;
        }

            
            
        go.SetActive(false);
            
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.type = Image.Type.Sliced;
            
        var textGo = new GameObject("Label", typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
            
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.font = FindObjectOfType<DevConsole>().logRow.TextMesh.font;
        tmp.fontSize = 0.4f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(1f, 1f, 1f, 0.9f);
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = false;
        tmp.alignment = TextAlignmentOptions.TopLeft;
            
        var textRect = textGo.GetComponent<RectTransform>();
            
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(0, 0);
        textRect.pivot = new Vector2(0, 1);
        textRect.anchoredPosition = new Vector2(0, 0.28f);
        textRect.sizeDelta = new Vector2(40f, 2f);
        textRect.localEulerAngles = new Vector3(0, 0, 90);

        var bar = go.AddComponent<RangeBar>();
        bar.BarImage = img;
        bar.Text = tmp;
            
        bar.StartMarker = CreateMarker("StartMarker");
        bar.EndMarker = CreateMarker("EndMarker");
            
        return bar;
    }
        
    private void Start()
    {
        m_init = true;
    }
        
    private void LateUpdate()
    {
        if (m_atsc == null) return;
            
        // Use SongBpmTime consistently, just like MeasureLinesController
        if (Mathf.Approximately(m_atsc.CurrentSongBpmTime, m_previousAtscBeat) || !m_init) 
            return;
                
        m_previousAtscBeat = m_atsc.CurrentSongBpmTime;
        RefreshVisibility();
    }
        
    private void OnDestroy()
    {
        EditorScaleController.EditorScaleChangedEvent -= OnEditorScaleChanged;
    }
        
    private void OnEditorScaleChanged(float obj)
    {
        RefreshPositions();
    }
        
    public void UpdateRanges(IEnumerable<RawMapRange> ranges)
    {
        m_init = false;
    
        var existingBars = new Queue<CachedRangeBar>(m_rangeBars);
        m_rangeBars.Clear();
    
        foreach (var range in ranges)
        {
            float start = range.StartBeat;
            float end = range.EndBeat;

            if (end < start)
                (end, start) = (start, end);
        
            CachedRangeBar cachedBar;
            RangeBar rb;
            TextMeshProUGUI label;
        
            if (existingBars.Count > 0)
            {
                cachedBar = existingBars.Dequeue();
                rb = cachedBar.RangeBar;
                label = cachedBar.RangeBar.Text;
                rb.gameObject.SetActive(true);
            }
            else
            {
                rb = Instantiate(m_rangeBarPrefab, m_parent);
                rb.gameObject.SetActive(true);
                rb.StartMarker.transform.SetParent(m_parent, false);
                rb.EndMarker.transform.SetParent(m_parent, false);

                rb.StartMarker.gameObject.SetActive(true);
                rb.EndMarker.gameObject.SetActive(true);
                    
                label = rb.GetComponentInChildren<TextMeshProUGUI>();
                    
                cachedBar = new CachedRangeBar(rb, start, end);
            }
        
            // Update cached times
            cachedBar.Start = start;
            cachedBar.End = end;
        
            // Configure visuals
            rb.BarImage.color = new Color(range.RangeColor.r, range.RangeColor.g, range.RangeColor.b, 0.2f);
            label.text = range.Label;
        
            // Position using SongBpmTime
            PositionBar(cachedBar);
        
            m_rangeBars.Add(cachedBar);
        }
    
        foreach (var leftover in existingBars)
            Destroy(leftover.RangeBar.gameObject);
    
        m_init = true;
        RefreshVisibility();
    }
        
    private void PositionBar(CachedRangeBar bar)
    {
        float scale = EditorScaleController.EditorScale;
        var rect = bar.RangeBar.BarImage.rectTransform;
        var label = bar.RangeBar.Text;
        float start = (float)m_song.Map.JsonTimeToSongBpmTime(bar.Start);
        float end = (float)m_song.Map.JsonTimeToSongBpmTime(bar.End);
        float duration = end - start;
            
        // CRITICAL: Match MeasureLinesController EXACTLY
        // Measure numbers are at X = -4.5 with pivot at center
        // Let's put our bars at X = -2.0 (between measure numbers and notes)
            
        // Use the same positioning system as measure lines
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0.5f, 0); // Pivot at bottom center
            
        // Position: X offset from left edge, Y = startTime * scale
        rect.anchoredPosition = new Vector2(-0.27f, start * scale);

        float height = duration * scale;
            
        label.ForceMeshUpdate();
            
        float lblWidth = label.preferredWidth;

        const float padding = 0.45f;
        float minimumHeight = lblWidth + padding;

        float finalHeight = Mathf.Max(height, minimumHeight);
            
        rect.sizeDelta = new Vector2(0.43f, finalHeight);
            
        float markerX = -0.27f;
            
        float startY = start * scale;
        float endY = end * scale;

        var startRect = bar.RangeBar.StartMarker.rectTransform;
        var endRect = bar.RangeBar.EndMarker.rectTransform;
        startRect.SetParent(bar.RangeBar.BarImage.transform.parent);
        endRect.SetParent(bar.RangeBar.BarImage.transform.parent);

        startRect.anchoredPosition = new Vector2(markerX, startY);
        endRect.anchoredPosition = new Vector2(markerX, endY);
            
        var color = Color.Lerp(bar.RangeBar.BarImage.color, Color.white, 0.5f);
        color.a = 0.99f;
            
        startRect.SetParent(bar.RangeBar.BarImage.transform);
        endRect.SetParent(bar.RangeBar.BarImage.transform);
            
        bar.RangeBar.StartMarker.color = color;
        bar.RangeBar.EndMarker.color = color;
    }
        
    private void RefreshVisibility()
    {
        if (m_atsc == null || m_frontNoteGridScaling == null) return;
            
        float currentTime = m_atsc.CurrentJsonTime;
            
        float beatsAhead = m_frontNoteGridScaling.localScale.z / EditorScaleController.EditorScale;
            
        float secondsAhead = (float)m_song.Map.JsonTimeToSongBpmTime(currentTime + beatsAhead) - currentTime;
        float secondsBehind = currentTime - (float)m_song.Map.JsonTimeToSongBpmTime(currentTime - beatsAhead / 4f);
            
        foreach (var bar in m_rangeBars)
        {
            float start = (float)m_song.Map.JsonTimeToSongBpmTime(bar.Start);
            float end = (float)m_song.Map.JsonTimeToSongBpmTime(bar.End);
            // Check if bar is in visible range using SongBpmTime
            bool enabled = end >= currentTime - secondsBehind && 
                           start <= currentTime + secondsAhead;
            bar.RangeBar.gameObject.SetActive(enabled);
        }
    }
        
    public void RefreshPositions()
    {
        Debug.Log("RefreshPositions");
        foreach (var bar in m_rangeBars)
        {
            PositionBar(bar);
        }
    }
        
    public void ClearAllRanges()
    {
        foreach (var bar in m_rangeBars)
        {
            if (bar.RangeBar != null)
                Destroy(bar.RangeBar.gameObject);
        }
        m_rangeBars.Clear();
    }
}