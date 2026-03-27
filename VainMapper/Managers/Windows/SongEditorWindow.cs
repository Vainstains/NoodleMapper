using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using VainLib.UI;
using VainLib.Utils;
using VainMapper.UI;
using VainMapper.Utils;

namespace VainMapper.Managers.Windows;

public class SongEditorWindow : GenericWindow<SongEditorWindow>
{
    public override string WindowName => "Map Config";
    private DifficultySelect? m_difficultySelect;

    protected override void PostInit()
    {
        m_difficultySelect = FindObjectOfType<DifficultySelect>();
        DifficultySelectEvents.OnAnythingChanged += SetUIDirty;
    }

    private void OnDisable()
    {
        DifficultySelectEvents.OnAnythingChanged -= SetUIDirty;
    }

    private void SetDiffDirty(DifficultySettings diffSettings, string characteristic, string difficulty)
    {
        if (m_difficultySelect == null)
            return;
        
        diffSettings.ForceDirty = true;
        
        if (m_difficultySelect.diffs == m_difficultySelect.Characteristics[characteristic])
        {
            var row = m_difficultySelect.rows.FirstOrDefault(r => r.Name == difficulty);
            row?.ShowDirtyObjects(diffSettings);
        }

        SetUIDirty();
    }

    protected override void BuildUI(RectTransform content)
    {
        SetupScrolling(ref content);
        var list = content.AddList();
        
        var characteristics = m_difficultySelect.Characteristics;
        
        foreach (var c in characteristics)
        {
            var characteristic = c.Key;
            foreach (var d in c.Value)
            {
                var difficulty = d.Key;
                var diffSettings = d.Value;
                var diffInfo = diffSettings.InfoDifficulty;
                var customName = diffSettings.CustomName;

                var nameText = $"{difficulty}{characteristic}";
                if (!string.IsNullOrEmpty(customName))
                {
                    nameText = $"{customName} <color=#FFFFFF50>({nameText})</color>";
                }

                var itemRect = list.AddRow();

                var (nameRect, mapRect) = itemRect.AddChild()
                    .InsetLeft(4).InsetRight(4).SplitHorizontal(0.6f);

                nameRect.AddBorder(RectTransform.Edge.Right).AddLabel(nameText);

                bool isNMEnabled = Helpers.TryGetMapFile(diffInfo.CustomData, out _);

                // if (isNMEnabled)
                //     mapRect.AddBorder(RectTransform.Edge.Right);

                mapRect.Field("Enable VM", 1, -26).AddToggle().SetValue(isNMEnabled).SetOnChange(isOn =>
                {
                    if (!isOn)
                        Helpers.RemoveMapFile(diffInfo.CustomData);
                    else
                        Helpers.SetMapFile(diffInfo.CustomData, $"{difficulty}{characteristic}");
                    SetDiffDirty(diffSettings, characteristic, difficulty);
                });

                // don't worry about the rest of the table columns, it's a placeholder for now
            }
        }
    }
}

[HarmonyPatch]
public static class DifficultySelectEvents
{
    public static event Action OnAnythingChanged;
    public static event Action<DifficultySettings> OnSavingDiff;

    static void Fire(DifficultySelect __instance)
        => OnAnythingChanged?.Invoke();

    // ---------- STRUCTURE ----------

    [HarmonyPatch(typeof(DifficultySelect), "OnChange")]
    [HarmonyPostfix]
    static void OnChange(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "HandleDeleteDifficulty")]
    [HarmonyPostfix]
    static void Delete(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "DoPaste")]
    [HarmonyPostfix]
    static void Paste(DifficultySelect __instance) => Fire(__instance);


    // ---------- DATA EDITS ----------

    [HarmonyPatch(typeof(DifficultySelect), "UpdateOffset")]
    [HarmonyPostfix]
    static void Offset(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "UpdateNJS")]
    [HarmonyPostfix]
    static void NJS(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "UpdateEnvironment")]
    [HarmonyPostfix]
    static void Env(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "UpdateLightshowFilePath")]
    [HarmonyPostfix]
    static void Light(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "UpdateMappers")]
    [HarmonyPostfix]
    static void Mappers(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "UpdateLighters")]
    [HarmonyPostfix]
    static void Lighters(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "UpdateEnvRemoval")]
    [HarmonyPostfix]
    static void EnvRemoval(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "OnValueChanged")]
    [HarmonyPostfix]
    static void NameChanged(DifficultySelect __instance) => Fire(__instance);


    // ---------- SAVE / REVERT ----------
    [HarmonyPatch(typeof(DifficultySelect), "SaveDiff")]
    [HarmonyPrefix]
    static void SavePrefix(DifficultySelect __instance, DifficultyRow row)
    {
        Debug.Log("Hooking into DifficultySelect.SaveDiff");
        if (__instance.diffs.TryGetValue(row.Name, out var diffSettings))
        {
            OnSavingDiff?.Invoke(diffSettings);
        }
    }

    [HarmonyPatch(typeof(DifficultySelect), "SaveDiff")]
    [HarmonyPostfix]
    static void Save(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "SaveAllDiffs")]
    [HarmonyPostfix]
    static void SaveAll(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "Revertdiff")]
    [HarmonyPostfix]
    static void Revert(DifficultySelect __instance) => Fire(__instance);


    // ---------- CONTEXT / LOAD ----------

    [HarmonyPatch(typeof(DifficultySelect), "SetCharacteristic")]
    [HarmonyPostfix]
    static void Characteristic(DifficultySelect __instance) => Fire(__instance);

    [HarmonyPatch(typeof(DifficultySelect), "Start")]
    [HarmonyPostfix]
    static void Start(DifficultySelect __instance) => Fire(__instance);
}
