using System;
using NoodleMapper.UI;
using NoodleMapper.Utils;
using TMPro;

namespace NoodleMapper.Managers.Windows;

public class EditorMainWindow : GenericWindow<EditorMainWindow>
{
    public override string WindowName => "NoodleMapper";

    protected override void PostInit()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += SetUIDirty;
    }

    private void OnDestroy()
    {
        LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= SetUIDirty;
    }

    protected override void BuildUI()
    {
        if (EditorManager.Instance.Map == null)
        {
            ContentRect.Vertical().Item().AddLabel("NM isn't enabled in this difficulty", 
                overflowMode: TextOverflowModes.Overflow).enableWordWrapping = true;
            return;
        }
    }
}