using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VainMapper.Managers;

namespace VainMapper.EditorThings;

public class GridSnapMarkerController : MonoBehaviour
{
    private const bool DebugLogging = true;

    private class SnapMarker : MonoBehaviour
    {
        public Image Image = null!;
    }

    private const float MarkerWidth = 24f;
    private const float MarkerHeight = 0.075f;

    private AudioTimeSyncController m_atsc = null!;
    private RectTransform m_parent = null!;
    private BeatSaberSongContainer m_song = null!;
    private SnapMarker m_markerPrefab = null!;

    private readonly List<SnapMarker> m_markers = new();

    private bool m_init;
    private float m_previousAtscBeat = float.NaN;
    private int m_previousGridMeasureSnapping = -1;
    private SnapMode m_previousSnapMode = (SnapMode)(-1);
    private int m_previousShuffleRate = -1;
    private float m_previousShuffleStrength = float.NaN;
    private float m_previousShufflePeriodOffset = float.NaN;

    public static GridSnapMarkerController? Create()
    {
        var measureLines = FindObjectOfType<MeasureLinesController>();
        if (measureLines == null)
        {
            Debug.LogError("Could not find MeasureLinesController!");
            return null;
        }

        var controller = measureLines.gameObject.AddComponent<GridSnapMarkerController>();
        controller.m_atsc = FindObjectOfType<AudioTimeSyncController>();
        controller.m_parent = GameObject.Find("Measure Lines Canvas")?.transform as RectTransform;
        controller.m_song = BeatSaberSongContainer.Instance;
        controller.m_markerPrefab = controller.CreateMarkerPrefab();

        controller.Log(
            $"Create: atsc={(controller.m_atsc != null)}, parent={(controller.m_parent != null)}, songMap={(controller.m_song?.Map != null)}");

        EditorScaleController.EditorScaleChangedEvent += controller.OnEditorScaleChanged;
        return controller;
    }

    private SnapMarker CreateMarkerPrefab()
    {
        var go = new GameObject("GridSnapMarkerPrefab", typeof(Image));
        go.SetActive(false);

        var image = go.GetComponent<Image>();
        image.raycastTarget = false;

        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(MarkerWidth, MarkerHeight);

        var marker = go.AddComponent<SnapMarker>();
        marker.Image = image;
        return marker;
    }

    private void Start()
    {
        m_init = true;
        Log("Start: initialized grid snap marker controller.");
        RefreshMarkers();
    }

    private void LateUpdate()
    {
        if (!m_init || m_atsc == null || m_parent == null || m_song?.Map == null)
            return;

        var extrasManager = EditorExtrasManager.Instance;
        var currentBeat = m_atsc.CurrentSongBpmTime;

        if (Mathf.Approximately(currentBeat, m_previousAtscBeat)
            && m_previousGridMeasureSnapping == m_atsc.GridMeasureSnapping
            && extrasManager != null
            && m_previousSnapMode == extrasManager.SnapMode
            && m_previousShuffleRate == extrasManager.GridPlusShuffleRate
            && Mathf.Approximately(m_previousShuffleStrength, extrasManager.GridPlusShuffleStrength)
            && Mathf.Approximately(m_previousShufflePeriodOffset, extrasManager.GridPlusShufflePeriodOffset))
            return;

        RefreshMarkers();
    }

    private void OnDestroy()
    {
        Log("OnDestroy: disposing grid snap marker controller.");
        EditorScaleController.EditorScaleChangedEvent -= OnEditorScaleChanged;
    }

    private void OnEditorScaleChanged(float _)
    {
        Log($"Editor scale changed: scale={EditorScaleController.EditorScale}");
        RefreshMarkers();
    }

    public void RefreshMarkers()
    {
        if (!m_init || m_atsc == null || m_parent == null || m_song?.Map == null)
        {
            Log(
                $"Refresh skipped: init={m_init}, atsc={(m_atsc != null)}, parent={(m_parent != null)}, songMap={(m_song?.Map != null)}");
            return;
        }

        var extrasManager = EditorExtrasManager.Instance;
        var provider = extrasManager?.ActiveSnapProvider;
        if (provider == null)
        {
            Log("Refresh: no active snap provider, hiding all markers.");
            SetMarkerCount(0);
            return;
        }

        var currentJsonTime = m_atsc.CurrentJsonTime;
        var beatsAhead = 6;
        var startBeat = currentJsonTime - beatsAhead / 4f;
        var endBeat = currentJsonTime + beatsAhead;

        var snapPoints = provider
            .EnumerateSnapPoints(currentJsonTime, startBeat, endBeat)
            .Where(beat => beat >= startBeat - 0.001f && beat <= endBeat + 0.001f)
            .Distinct()
            .Take(256)
            .ToList();

        Log(
            $"Refresh: mode={extrasManager.SnapMode}, currentJson={currentJsonTime:F3}, currentSongBeat={m_atsc.CurrentSongBpmTime:F3}, window=[{startBeat:F3}, {endBeat:F3}], scale={EditorScaleController.EditorScale:F3}, gridSnap={m_atsc.GridMeasureSnapping}, markers={snapPoints.Count}");
        if (snapPoints.Count > 0)
            Log($"Refresh: first markers={string.Join(", ", snapPoints.Take(8).Select(beat => beat.ToString("F3")).ToArray())}");
        else
            Log("Refresh: provider returned no visible snap points.");

        SetMarkerCount(snapPoints.Count);

        var color = GetMarkerColor(extrasManager.SnapMode);
        for (var i = 0; i < snapPoints.Count; i++)
        {
            var marker = m_markers[i];
            marker.gameObject.SetActive(true);
            PositionMarker(marker, snapPoints[i], color);
        }

        m_previousAtscBeat = m_atsc.CurrentSongBpmTime;
        m_previousGridMeasureSnapping = m_atsc.GridMeasureSnapping;
        if (extrasManager != null)
        {
            m_previousSnapMode = extrasManager.SnapMode;
            m_previousShuffleRate = extrasManager.GridPlusShuffleRate;
            m_previousShuffleStrength = extrasManager.GridPlusShuffleStrength;
            m_previousShufflePeriodOffset = extrasManager.GridPlusShufflePeriodOffset;
        }
    }

    private void SetMarkerCount(int targetCount)
    {
        var previousCount = m_markers.Count;
        while (m_markers.Count < targetCount)
            m_markers.Add(Instantiate(m_markerPrefab, m_parent));

        for (var i = 0; i < m_markers.Count; i++)
            m_markers[i].gameObject.SetActive(i < targetCount);

        if (previousCount != m_markers.Count || targetCount == 0)
            Log($"SetMarkerCount: pool={m_markers.Count}, active={targetCount}");
    }

    private void PositionMarker(SnapMarker marker, float beat, Color color)
    {
        var rect = marker.Image.rectTransform;
        var mappedBeat = (float)m_song.Map.JsonTimeToSongBpmTime(beat);

        rect.anchoredPosition = new Vector2(0f, mappedBeat * EditorScaleController.EditorScale);
        rect.sizeDelta = new Vector2(MarkerWidth, MarkerHeight);

        marker.Image.color = color;

        if (DebugLogging)
            Log($"PositionMarker: beat={beat:F3}, mappedBeat={mappedBeat:F3}, anchoredY={rect.anchoredPosition.y:F3}, color={color}");
    }

    private static Color GetMarkerColor(SnapMode snapMode)
    {
        return snapMode switch
        {
            SnapMode.Notes => new Color(1f, 0.75f, 0.30f, 0.4f),
            SnapMode.GridPlus => new Color(0.35f, 0.95f, 1f, 0.4f),
            _ => new Color(1f, 1f, 1f, 0f)
        };
    }

    private void Log(string message)
    {
        if (!DebugLogging)
            return;

        Debug.Log($"[VM GridSnapMarkers] {message}");
    }
}
