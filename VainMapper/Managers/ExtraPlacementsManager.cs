using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using VainLib.Scenes;
using VainMapper.ModMap;
using VainMapper.Managers.Windows;
using VainMapper.UI;

namespace VainMapper.Managers;

public class EditorExtrasManager : ManagerBehaviour<EditorExtrasManager>
{
    private SnapMode m_snapMode = SnapMode.Vanilla;
    private VanillaScrubSnapProvider m_vanillaSnap = new ();
    private NotesScrubSnapProvider m_notesSnap = new ();
    private GridPlusScrubSnapProvider m_gridPlusSnap = new ();

    private ScrubSnapHandler m_snapHandler = null!;

    internal ScrubSnapHandler.IScrubSnapProvider ActiveSnapProvider => m_snapHandler.SnapProvider ?? m_vanillaSnap;

    public SnapMode SnapMode
    {
        get => m_snapMode;
        set
        {
            m_snapMode = value;
            
            m_snapHandler.SnapProvider = value switch
            {
                SnapMode.Vanilla => m_vanillaSnap,
                SnapMode.Notes => m_notesSnap,
                SnapMode.GridPlus => m_gridPlusSnap,
                _ => throw new ArgumentOutOfRangeException()
            };

            EditorManager.Instance?.Atsc.SnapToGrid();
            EditorGridAndTrackController.Instance?.RefreshGridStuff();
        }
    }

    public int GridPlusShuffleRate
    {
        get => m_gridPlusSnap.ShuffleRate;
        set
        {
            m_gridPlusSnap.ShuffleRate = value;
            EditorGridAndTrackController.Instance?.RefreshGridStuff();
        }
    }

    public float GridPlusShuffleStrength
    {
        get => m_gridPlusSnap.ShuffleStrength;
        set
        {
            m_gridPlusSnap.ShuffleStrength = value;
            EditorGridAndTrackController.Instance?.RefreshGridStuff();
        }
    }

    public float GridPlusShufflePeriodOffset
    {
        get => m_gridPlusSnap.ShufflePeriodOffset;
        set
        {
            m_gridPlusSnap.ShufflePeriodOffset = value;
            EditorGridAndTrackController.Instance?.RefreshGridStuff();
        }
    }

    protected override void PostInit()
    {
        m_snapHandler = new ScrubSnapHandler();
        ATSCScrubPatches.SetScrubHandler(m_snapHandler);
        Events.ExtrasClicked.AddListener(EditorExtrasWindow.ToggleUI);
    }

    private void OnDestroy()
    {
        Events.ExtrasClicked.RemoveListener(EditorExtrasWindow.ToggleUI);
    }

    public int QuantizeSelectionToGridPlus()
    {
        var selection = SelectionController.SelectedObjects?.ToArray() ?? Array.Empty<BaseObject>();
        if (selection.Length == 0)
            return 0;

        var addedObjects = new List<BaseObject>();
        var removedObjects = new List<BaseObject>();

        foreach (var obj in selection)
        {
            if (obj == null)
                continue;

            var quantizedObject = BeatmapFactory.Clone(obj);
            var objectChanged = TryQuantizeObjectToGridPlus(quantizedObject);

            if (!objectChanged)
                continue;

            var originalObject = BeatmapFactory.Clone(obj);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);

            collection.DeleteObject(obj, false, false, "", true, false);
            collection.SpawnObject(quantizedObject, false, true);

            removedObjects.Add(originalObject);
            addedObjects.Add(quantizedObject);
        }

        if (addedObjects.Count == 0)
            return 0;

        BeatmapObjectContainerCollection.RefreshAllPools();
        SelectionController.SelectedObjects = new HashSet<BaseObject>(addedObjects);
        SelectionController.SelectionChangedEvent?.Invoke();
        SelectionController.RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(
            addedObjects,
            removedObjects,
            $"Quantized {addedObjects.Count} object(s) to GridPlus"));
        EditorGridAndTrackController.Instance?.RefreshGridStuff();
        return addedObjects.Count;
    }

    public float QuantizeBeatToGridPlus(float beat)
    {
        var atsc = EditorManager.Instance?.Atsc;
        if (atsc == null)
            return beat;

        var beatStep = 1.0f / atsc.GridMeasureSnapping;
        var searchRadius = Mathf.Max(beatStep * 2f, 1.0f / Mathf.Max(1, m_gridPlusSnap.ShuffleRate));
        var startBeat = beat - searchRadius;
        var endBeat = beat + searchRadius;

        var bestBeat = beat;
        var bestDistance = float.MaxValue;

        foreach (var snap in m_gridPlusSnap.EnumerateSnapPoints(beat, startBeat, endBeat))
        {
            var distance = Mathf.Abs(snap - beat);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestBeat = snap;
        }

        return bestBeat;
    }

    private bool TryQuantizeObjectToGridPlus(BaseObject obj)
    {
        var objectChanged = false;

        var snappedTime = QuantizeBeatToGridPlus(obj.JsonTime);
        if (!Mathf.Approximately(snappedTime, obj.JsonTime))
        {
            obj.JsonTime = snappedTime;
            objectChanged = true;
        }

        if (obj is BaseArc arc)
        {
            var snappedTailTime = QuantizeBeatToGridPlus(arc.TailJsonTime);
            if (!Mathf.Approximately(snappedTailTime, arc.TailJsonTime))
            {
                arc.TailJsonTime = snappedTailTime;
                objectChanged = true;
            }
        }

        if (objectChanged)
            SpanProcessorUtils.RefreshObject(obj);

        return objectChanged;
    }
}

// TODO
//   - generic scrub snapping
//   - snap handler for notes
//   - snap handler for shuffled grid

public enum SnapMode
{
    Vanilla,
    Notes,
    GridPlus
}

internal class VanillaScrubSnapProvider : ScrubSnapHandler.IScrubSnapProvider
{
    public IEnumerable<float> EnumerateSnapPoints(float aroundBeat, float startBeat, float endBeat)
    {
        yield break;
    }
}

internal class NotesScrubSnapProvider : ScrubSnapHandler.IScrubSnapProvider
{
    public IEnumerable<float> EnumerateSnapPoints(float aroundBeat, float startBeat, float endBeat)
    {
        if (startBeat > endBeat)
            (startBeat, endBeat) = (endBeat, startBeat);

        foreach (var note in BaseObjectHelper.Editable.ColorNotes
                     .Where(note => note.JsonTime >= startBeat && note.JsonTime <= endBeat)
                     .OrderBy(note => note.JsonTime))
            yield return note.JsonTime;
    }
}

internal class GridPlusScrubSnapProvider : ScrubSnapHandler.IScrubSnapProvider
{
    public int ShuffleRate { get; set; } = 2;
    public float ShuffleStrength { get; set; } = 0.0f;
    public float ShufflePeriodOffset { get; set; } = 0.0f;

    public IEnumerable<float> EnumerateSnapPoints(float aroundBeat, float startBeat, float endBeat)
    {
        var atsc = EditorManager.Instance.Atsc;

        if (startBeat > endBeat)
            (startBeat, endBeat) = (endBeat, startBeat);

        var period = 1.0f / ShuffleRate;
        var beatStep = 1.0f / atsc.GridMeasureSnapping;
        startBeat = Mathf.FloorToInt(startBeat * atsc.GridMeasureSnapping) * beatStep;

        var numSteps = Mathf.CeilToInt((endBeat - startBeat) / beatStep);
        for (var i = 0; i < numSteps; i++)
        {
            var currentBeat = startBeat + i * beatStep;
            var t = Mathf.Repeat(ShuffleRate * currentBeat - ShufflePeriodOffset, 1.0f);
            var shift = period * (1.0f - Mathf.Exp(-t * ShuffleStrength)) * (1.0f - t);

            yield return currentBeat + shift;
        }
    }
}

internal class ScrubSnapHandler : ATSCScrubPatches.IScrubHandler
{
    public interface IScrubSnapProvider
    {
        public IEnumerable<float> EnumerateSnapPoints(float aroundBeat, float startBeat, float endBeat);
    }
    public IScrubSnapProvider? SnapProvider { get; set; } = null;
    public void OnScrubbed(float fromJsonTime, float toJsonTime, ref float currentJsonTime)
    {
        if (SnapProvider == null)
            return;
        
        var scrubbingBackwards = fromJsonTime > toJsonTime;

        float target = 0;

        var foundTarget = false;

        if (scrubbingBackwards)
        {
            foreach (var snap in SnapProvider.EnumerateSnapPoints(fromJsonTime, fromJsonTime - 2f, fromJsonTime))
            {
                if (snap >= fromJsonTime)
                    break;
                
                target = snap;
                foundTarget = true;
            }
        }
        else
        {
            foreach (var snap in SnapProvider.EnumerateSnapPoints(fromJsonTime, fromJsonTime, fromJsonTime + 2f))
            {
                if (snap <= fromJsonTime)
                    continue;
                
                target = snap;
                foundTarget = true;
                break;
            }
        }

        if (!foundTarget)
            return;
        
        currentJsonTime = target;
    }
}

static class ATSCScrubPatches
{
    public interface IScrubHandler
    {
        void OnScrubbed(float fromJsonTime, float toJsonTime, ref float currentJsonTime);
    }

    private static IScrubHandler? s_scrubHandler = null;
    private static float s_jsonTimeAfterPrefixExecution = 0;

    public static void SetScrubHandler(IScrubHandler? handler)
    {
        s_scrubHandler = handler;
    }

    private static void CommonPrefix(AudioTimeSyncController __instance)
    {
        s_jsonTimeAfterPrefixExecution = __instance.CurrentJsonTime;
    }

    private static void CommonPostfix(AudioTimeSyncController __instance)
    {
        if (s_scrubHandler != null)
        {
            var jsonTime = __instance.CurrentJsonTime;
            var prevTime = s_jsonTimeAfterPrefixExecution;

            Debug.Log($"Scrub detected: prevTime={prevTime}, jsonTime={jsonTime}");

            if (Mathf.Abs(prevTime - jsonTime) < 0.0001f)
                return;
            
            var newTime = jsonTime;
            s_scrubHandler.OnScrubbed(prevTime, jsonTime, ref newTime);
            if (!Mathf.Approximately(jsonTime, newTime))
                __instance.MoveToJsonTime(newTime);
        }
    }
    
    // these will never cancel the original method, just monitor before and after.
    [HarmonyPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.OnMoveCursorForward))]
    class MoveCursorForwardPatch
    {
        static bool s_willCancel = false;
        static bool Prefix(AudioTimeSyncController __instance, InputAction.CallbackContext context)
        {
            s_willCancel = !context.performed;
            CommonPrefix(__instance);
            return true;
        }

        static void Postfix(AudioTimeSyncController __instance)
        {
            if (s_willCancel)
                return;
            CommonPostfix(__instance);
        }
    }

    [HarmonyPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.OnMoveCursorBackward))]
    class MoveCursorBackwardPatch
    {
        static bool s_willCancel = false;
        static bool Prefix(AudioTimeSyncController __instance, InputAction.CallbackContext context)
        {
            s_willCancel = !context.performed;
            CommonPrefix(__instance);
            return true;
        }

        static void Postfix(AudioTimeSyncController __instance)
        {
            if (s_willCancel)
                return;
            CommonPostfix(__instance);
        }
    }

    [HarmonyPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.OnChangeTimeandPrecision))]
    class ChangeTimeandPrecisionPatch
    {
        static bool s_willCancel = false;
        static bool Prefix(AudioTimeSyncController __instance, InputAction.CallbackContext context)
        {
            s_willCancel = !context.performed || __instance.controlSnap;
            CommonPrefix(__instance);
            return true;
        }

        static void Postfix(AudioTimeSyncController __instance)
        {
            if (s_willCancel)
                return;
            CommonPostfix(__instance);
            EditorGridAndTrackController.Instance?.RefreshGridStuff();
        }
    }
}
