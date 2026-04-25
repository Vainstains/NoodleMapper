using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.UI;
using VainLib.UI;
using VainLib.Utils;

namespace VainMapper.Managers.Windows;

public class EditorExtrasWindow : GenericWindow<EditorExtrasWindow>
{
    public override string WindowName => "Extra Tools";
    protected override void BuildUI(RectTransform content)
    {
        SetupScrolling(ref content);
        var layout = content.AddVertical();
        content.AddSizeFitter(vertical: ContentSizeFitter.FitMode.PreferredSize);
        
        layout.AddRow().AddChild(RectTransform.Edge.Left).ExtendRight(100).AddButton("Make slab notes", () =>
        {
            var objectContainerCollection = FindObjectOfType<ChainGridContainer>();
            if (objectContainerCollection == null)
                return;
            
            var notes = BaseObjectHelper.Selected.ColorNotes.ToList();
            notes.Sort((a, b) => a.JsonTime.CompareTo(b.JsonTime));
            
            if (Settings.Instance.MapVersion == 2 && notes.Count > 1)
            {
                PersistentUI.Instance.ShowDialogBox("Chain placement is not supported in v2 format.\nConvert map to v3 to place chains.",
                    null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
            
            var generatedObjects = new List<BaseChain>();
            
            foreach (var note in notes)
                if (TryCreateChainData(note, out var chain))
                    generatedObjects.Add(chain);
            
            if (generatedObjects.Count > 0)
            {
                foreach (var chainData in generatedObjects)
                    objectContainerCollection.SpawnObject(chainData, false);
                
                SelectionController.SelectedObjects = new HashSet<BaseObject>(generatedObjects);
                SelectionController.SelectionChangedEvent?.Invoke();
                SelectionController.RefreshSelectionMaterial(false);
                BeatmapActionContainer.AddAction(
                    new BeatmapObjectPlacementAction(generatedObjects.ToArray(), new List<BaseNote>(), $"Placed {generatedObjects.Count} slab notes"));
            }
        });

        layout.AddRow().Field("Snap mode").AddDropdown<SnapMode>(mode =>
        {
            EditorExtrasManager.Instance.SnapMode = mode;
            SetUIDirty();
        }).SetSelectedOption(EditorExtrasManager.Instance.SnapMode);

        if (EditorExtrasManager.Instance.SnapMode == SnapMode.GridPlus)
        {
            layout.AddRow().Field("Shuffle rate").AddValueInput(2, 1, 4, 1).SetValue(EditorExtrasManager.Instance.GridPlusShuffleRate)
                .SetOnChange(val => EditorExtrasManager.Instance.GridPlusShuffleRate = Mathf.RoundToInt(val));
            layout.AddRow().Field("Shuffle strength").AddValueInput(0, 0, 2, 0.25f).SetValue(EditorExtrasManager.Instance.GridPlusShuffleStrength)
                .SetOnChange(val => EditorExtrasManager.Instance.GridPlusShuffleStrength = val);
            layout.AddRow().Field("Shuffle period offset").AddValueInput(0, 0, 1, 0.1f).SetValue(EditorExtrasManager.Instance.GridPlusShufflePeriodOffset)
                .SetOnChange(val => EditorExtrasManager.Instance.GridPlusShufflePeriodOffset = val);
            layout.AddRow().AddChild(RectTransform.Edge.Left).ExtendRight(180).AddButton("Quantize Selection To GridPlus", () =>
            {
                var changed = EditorExtrasManager.Instance.QuantizeSelectionToGridPlus();
                Debug.Log($"[VM GridPlus] Quantized {changed} selected object(s) to GridPlus.");
            });
        }
    }

    private static bool TryCreateChainData(BaseNote head, out BaseChain chain)
    {
        if (head.CutDirection == (int)NoteCutDirection.Any)
        {
            chain = null!;
            return false;
        }
        chain = new BaseChain(head, head)
        {
            SliceCount = 1
        };
        return true;
    }
}
