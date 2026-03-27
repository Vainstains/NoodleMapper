using VainMapper.Utils;
using TMPro;
using UnityEngine;
using VainLib.Scenes;
using VainLib.UI.Components;
using VainLib.Utils;
using VainMapper.Managers.Windows;

namespace VainMapper.Managers;

/// <summary>
/// Manager that lives on the Song Select screen (scene 1 — SongSelectMenu).
/// Exposes <see cref="OpenCloneWindow"/> for the Clone from Git button.
///
/// TO WIRE THE BUTTON:
///   Uncomment and adapt the code in PostInit() once you know the name of the
///   panel you want to attach it to. Or call OpenCloneWindow() from your own
///   button code.
/// </summary>
public class SongSelectManager : ManagerBehaviour<SongSelectManager>
{
    protected override void PostInit()
    {
        // ── STUB ──────────────────────────────────────────────────────────────
        // Replace "YourPanelNameHere" with the actual GameObject name in the
        // Song Select scene where you want the button to appear.
        //
        var panel = GameObject.Find("SongInfoPanel")?.RequireComponent<RectTransform>();
        if (panel == null) { Debug.LogError("[VainMapper] SongSelectManager: panel not found"); return; }
        
        panel.AddChildTopRight().IgnoreLayout().Move(-2, 2)
            .ExtendTop(20).ExtendLeft(120)
            .AddInitComponent<NoodleButton>(new Color(0.3f, 0.3f, 0.45f), OpenCloneWindow)
            .Content.AddLabel("Clone from Git", TextAlignmentOptions.Center, fontSize: 14);
        // ─────────────────────────────────────────────────────────────────────
    }

    /// <summary>Toggle the Clone Map from Git window.</summary>
    public static void OpenCloneWindow() => GitCloneWindow.ToggleUI();
}
