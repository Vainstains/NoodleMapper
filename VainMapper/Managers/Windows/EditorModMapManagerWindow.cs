using System.IO;
using System.Linq;
using UnityEngine;
using VainMapper.UI;
using VainMapper.Utils;

namespace VainMapper.Managers.Windows;

public class EditorModMapManagerWindow : GenericWindow<EditorModMapManagerWindow>
{
    public override string WindowName => "Modmap files";
    
    protected override void BuildUI(RectTransform content)
    {
        SetupScrolling(ref content);
        
        var modmaps = Helpers.GetModMapDataNames().ToList();
        
        var list = content.AddList();

        foreach (var modmap in modmaps)
        {
            var itemRect = list.AddRow();
            var innerRect = itemRect.AddChild()
                .InsetLeft(4).InsetRight(4);

            var windowName = modmap;
            innerRect.AddLabel(modmap);
            innerRect.AddChild(RectTransform.Edge.Right).ExtendLeft(26).AddButton("X", () =>
            {
                PersistentUI.Instance.AskYesNo($"Delete {windowName}?", "This cannot be undone.", () =>
                {
                    EditorManager.DeleteModmap(windowName);
                    RebuildAll();
                });
            }).MainColor = new Color(0.7f, 0.1f, 0.3f);
        }

        list.AddRow().AddChild(RectTransform.Edge.Left).ExtendRight(50).AddButton("new...", AddNewModmap);
    }

    private void AddNewModmap()
    {
        PersistentUI.Instance.ShowInputBox("Name", result =>
        {
            if (string.IsNullOrEmpty(result))
            {
                PersistentUI.Instance.ShowMessage("Name cannot be empty.");
                return;
            }

            if (File.Exists(Helpers.GetModMapDataPath(result)))
            {
                PersistentUI.Instance.ShowMessage($"{result} already exists.");
                return;
            }
            
            EditorManager.CreateModmap(result);
            RebuildAll();
        });
    }
}