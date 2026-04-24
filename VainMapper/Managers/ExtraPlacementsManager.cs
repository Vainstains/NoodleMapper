using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using VainLib.Scenes;
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
        }
    }

    public int GridPlusShuffleRate
    {
        get => m_gridPlusSnap.ShuffleRate;
        set
        {
            m_gridPlusSnap.ShuffleRate = value;
        }
    }

    public float GridPlusShuffleStrength
    {
        get => m_gridPlusSnap.ShuffleStrength;
        set
        {
            m_gridPlusSnap.ShuffleStrength = value;
        }
    }

    public float GridPlusShufflePeriodOffset
    {
        get => m_gridPlusSnap.ShufflePeriodOffset;
        set
        {
            m_gridPlusSnap.ShufflePeriodOffset = value;
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
    public IEnumerable<float> EnumerateSnapPoints(float beat)
    {
        yield break;
    }
}

internal class NotesScrubSnapProvider : ScrubSnapHandler.IScrubSnapProvider
{
    public IEnumerable<float> EnumerateSnapPoints(float beat)
    {
        foreach (var note in BaseObjectHelper.Editable.ColorNotes)
            yield return note.JsonTime;
    }
}

internal class GridPlusScrubSnapProvider : ScrubSnapHandler.IScrubSnapProvider
{
    public int ShuffleRate { get; set; } = 2;
    public float ShuffleStrength { get; set; } = 0.0f;
    public float ShufflePeriodOffset { get; set; } = 0.0f;

    public IEnumerable<float> EnumerateSnapPoints(float beat)
    {
        var atsc = EditorManager.Instance.Atsc;

        var period = 1.0f / ShuffleRate;
        var startBeat = Mathf.FloorToInt((beat - 2) * ShuffleRate) * period;
        var beatStep = 1.0f / atsc.GridMeasureSnapping;
        var endBeat = beat + 1;

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
        public IEnumerable<float> EnumerateSnapPoints(float beat);
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
            foreach (var snap in SnapProvider.EnumerateSnapPoints(fromJsonTime))
            {
                if (snap >= fromJsonTime)
                    break;
                
                target = snap;
                foundTarget = true;
            }
        }
        else
        {
            foreach (var snap in SnapProvider.EnumerateSnapPoints(fromJsonTime))
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
        }
    }
}