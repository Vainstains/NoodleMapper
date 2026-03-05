using System;
using System.Collections.Generic;
using System.Linq;
using NoodleMapper.EditorThings;
using NoodleMapper.Map;
using NoodleMapper.UI;
using NoodleMapper.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoodleMapper.Managers;

public class EditorGridAndTrackController : ManagerBehaviour<EditorGridAndTrackController>
{
    private class TimelineRangeBar : MonoBehaviour, IPointerClickHandler
    {
        private RectTransform m_rt;
        private Image m_barImage;
        private Tooltip m_tooltip;
        private Action<TimelineRangeBar, PointerEventData> m_callback;
        
        private RectTransform m_borderL, m_borderR;
        private Image m_singleDiamond;
        
        public float StartBeat;
        public float EndBeat;
        public string Name;
        public Color Color;
        
        private void Init(Action<TimelineRangeBar, PointerEventData> callback)
        {
            m_callback = callback;
            
            m_rt = gameObject.RequireComponent<RectTransform>();

            m_barImage = m_rt.AddImage(null);
            m_borderR = m_rt.AddGetBorder(RectTransform.Edge.Right, 1, new Color(0.7f, 0.7f, 0.7f, 0.57f))
                .Move(-1, 0).ExtendTop(1).ExtendBottom(1);
            m_borderL = m_rt.AddGetBorder(RectTransform.Edge.Left, 1, new Color(1f, 1f, 1f, 0.57f))
                .Move(1, 0).ExtendTop(1).ExtendBottom(1);
            
            m_tooltip = gameObject.RequireComponent<Tooltip>();

            m_singleDiamond = m_borderL.AddChildCenter().Extend(3f).AddImage(Globals.Assets.TimelineEndpointSingle);
            m_singleDiamond.rectTransform.eulerAngles = new Vector3(0f, 0f, 45f);
            
            gameObject.SetActive(false);
        }

        public void UpdateRange(AudioTimeSyncController atsc)
        {
            gameObject.SetActive(true);
            Debug.Log(atsc);
            Debug.Log(BeatSaberSongContainer.Instance.LoadedSong);
            var song = BeatSaberSongContainer.Instance.LoadedSong;
            var map = BeatSaberSongContainer.Instance.Map;
            var songLen = song.length;
            
            float startRatio = atsc.GetSecondsFromBeat((float)map.JsonTimeToSongBpmTime(StartBeat)) / songLen;
            float endRatio = atsc.GetSecondsFromBeat((float)map.JsonTimeToSongBpmTime(EndBeat)) / songLen;

            m_rt.sizeDelta = Vector2.zero;
            m_rt.anchoredPosition = Vector2.zero;
            
            m_rt.anchorMin = new Vector2(startRatio, 0);
            m_rt.anchorMax = new Vector2(endRatio, 0);

            m_rt.ExtendTop(4).Move(0, 1);
            
            m_barImage.color = Color * new Color(1.1f, 1.1f, 1.1f, 0.6f);
            var color = Color;
            color.a = 0.9f;
            m_singleDiamond.color = color;
            
            m_tooltip.TooltipOverride = Name;

            bool single = Mathf.Abs(startRatio - endRatio) < 0.0005f;
            
            m_borderR.gameObject.SetActive(!single);
            m_singleDiamond.gameObject.SetActive(single);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            m_callback.Invoke(this, eventData);
        }
    }
    
    private RangeBarController m_rangeBarController = null!;
    private AudioTimeSyncController m_atsc = null!;
    private TimelineInputPlaybackController m_tipc = null!;
    private RectTransform m_timelineRangeBarRect = null!;
    private List<TimelineRangeBar> m_timelineRangebars = new ();
    private const int MaxCachedBars = 70;
    protected override void PostInit()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += DiffChanged;
        
        if (!EditorManager.NMEnabled)
            return;
        
        m_rangeBarController = RangeBarController.Create();
        m_atsc = FindObjectOfType<AudioTimeSyncController>();
        m_tipc = FindObjectOfType<TimelineInputPlaybackController>();
        var mapEditorUI = FindFirstObjectByType<MapEditorUI>();
        var songTimeline = mapEditorUI.MainUIGroup[(int)CMCanvasGroup.SongTimeline];
        m_timelineRangeBarRect = ((RectTransform)songTimeline.transform).AddChild(RectTransform.Edge.Bottom)
            .InsetLeft(10).InsetRight(10).ExtendTop(6).Move(0, 25).AddImage(null,
                new (0, 0, 0, 0.7f)).rectTransform;
    }
    
    private void Start()
    {
        EditorGridAndTrackController.Instance.RefreshGridStuff();
    }

    private void OnDisable()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= DiffChanged;
    }
    
    private void DiffChanged()
    {
        if (m_rangeBarController != null)
            Destroy(m_rangeBarController.gameObject);
        if (m_timelineRangeBarRect != null)
            Destroy(m_timelineRangeBarRect.gameObject);
        ResetFresh();
    }
    
    private void SetRanges(IEnumerable<MapRange> ranges)
    {
        m_rangeBarController.UpdateRanges(ranges);
    }
    
    public void RefreshGridStuff()
    {
        if (!EditorManager.NMEnabled)
            return;
        
        var map = EditorManager.Instance.Map;
        
        SetRanges(map.MapRanges);
        
        m_rangeBarController.RefreshPositions();
        
        while (m_timelineRangebars.Count < Math.Max(MaxCachedBars, map.MapRanges.Count))
            m_timelineRangebars.Add(m_timelineRangeBarRect.AddChildCenter().AddInitChild<TimelineRangeBar>(GoToRange));
        while (m_timelineRangebars.Count > Math.Max(MaxCachedBars, map.MapRanges.Count))
        {
            var bar = m_timelineRangebars[m_timelineRangebars.Count - 1];
            Destroy(bar.gameObject);
            m_timelineRangebars.RemoveAt(m_timelineRangebars.Count - 1);
        }
        for (int i = map.MapRanges.Count; i < m_timelineRangebars.Count; i++)
            m_timelineRangebars[i].Hide();
        if (m_atsc)
        {
            for (int i = 0; i < map.MapRanges.Count; i++)
            {
                var mapRange = map.MapRanges[i];
                var rangeBar = m_timelineRangebars[i];
                rangeBar.Name = mapRange.Name;
                rangeBar.Color = mapRange.Color;
                rangeBar.StartBeat = mapRange.StartBeat;
                rangeBar.EndBeat = mapRange.EndBeat;
                rangeBar.UpdateRange(m_atsc);
            }
        }
    }

    private void GoToRange(TimelineRangeBar rangeBar, PointerEventData eventData)
    {
        float goalBeat = rangeBar.StartBeat;
        if (eventData.button == PointerEventData.InputButton.Right)
            goalBeat = rangeBar.EndBeat;
        
        m_tipc.PointerDown();
        m_atsc.MoveToJsonTime(goalBeat);
        m_tipc.PointerUp();
    }
}