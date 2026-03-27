using System;
using Beatmap.Base;
using HarmonyLib;

namespace VainMapper.Managers;

[HarmonyPatch]
class EditorPatches
{
    public static event Action OnSavingDiff;
    
    [HarmonyPatch(typeof(BaseDifficulty), nameof(BaseDifficulty.Save))]
    [HarmonyPrefix]
    static void Save()
    {
        OnSavingDiff?.Invoke();
    }

    [HarmonyPatch(typeof(BPMChangeGridContainer), nameof(BPMChangeGridContainer.RefreshModifiedBeat))]
    [HarmonyPostfix]
    static void BPMRefresh()
    {
        EditorGridAndTrackController.Instance.RefreshGridStuff();
    }
}