using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine.InputSystem;
using VainMapper.UI;
using VainMapper.Utils;
using VainLib.Scenes;
using VainMapper.Managers;
using VainLib.UI;
using VainMapper.Managers.Windows;
using Object = UnityEngine.Object;

namespace VainMapper;

[Plugin("VainMapper")]
public class VainMapperPlugin
{
    public const string Author = "Vainstains";
    public const string Name = "VainMapper";
    public const string ID = $"com.{Author}.{Name}";
    
    [Init]
    private void Init()
    {
        new Harmony(ID)
            .PatchAll(Assembly.GetExecutingAssembly());
        
        ExtensionButtons.AddButton(
            DefaultResources.LoadSprite("Resources/ExtensionButtonIcon.png"),
            Helpers.CurrentPluginName,
            () => { Events.ExtensionButtonClicked.Invoke(); }
        );
            
        ExtensionButtons.AddButton(
            DefaultResources.LoadSprite("Resources/RebootButtonIcon.png"),
            "Reboot ChroMapper and return to where you are now",
            Rebooter.Reboot
        );
        
        SceneManagers.Register<SongSelectManager>().ForScene(CMScene.SongSelectMenu);
        SceneManagers.Register<SongEditorManager>().ForScene(CMScene.SongEditMenu);
        SceneManagers.Register<EditorGridAndTrackController>().ForScene(CMScene.Mapper);
        SceneManagers.Register<EditorManager>().ForScene(CMScene.Mapper);
        SceneManagers.Register<RebootManager>().ForScene(CMScene.SongSelectMenu);
        
        InstallInput();
    }

    void InstallInput()
    {
        var map = CMInputCallbackInstaller.InputInstance.asset.actionMaps
            .Where(x => x.name == "Node Editor")
            .FirstOrDefault();
        CMInputCallbackInstaller.InputInstance.Disable();
			
        var toggleWindow = map.AddAction("VainMapper Window", type: InputActionType.Button);
        toggleWindow.AddCompositeBinding("ButtonWithOneModifier")
            .With("Modifier", "<Keyboard>/ctrl")
            .With("Button", "<Keyboard>/n");
			
        CMInputCallbackInstaller.InputInstance.Enable();
        
        
        toggleWindow!.performed += EditorMainWindow.OnToggleWindow;
    }
}
