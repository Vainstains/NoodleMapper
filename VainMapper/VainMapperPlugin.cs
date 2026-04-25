using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using VainLib.IO;
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
            PluginResources.LoadSprite("Resources/ExtensionButtonIcon.png"),
            Helpers.CurrentPluginName,
            () => { Events.ExtensionButtonClicked.Invoke(); }
        );
        
        ExtensionButtons.AddButton(
            PluginResources.LoadSprite("Resources/Extras.png"),
            Helpers.CurrentPluginName,
            () => { Events.ExtrasClicked.Invoke(); }
        );
            
        ExtensionButtons.AddButton(
            PluginResources.LoadSprite("Resources/RebootButtonIcon.png"),
            "Reboot ChroMapper and return to where you are now",
            Rebooter.Reboot
        );
        
        SceneManagers.Register<SongSelectManager>().ForScene(CMScene.SongSelectMenu);
        SceneManagers.Register<SongEditorManager>().ForScene(CMScene.SongEditMenu);
        SceneManagers.Register<EditorGridAndTrackController>().ForScene(CMScene.Mapper);
        SceneManagers.Register<EditorManager>().ForScene(CMScene.Mapper);
        // SceneManagers.Register<OutlineManager>().ForScene(CMScene.Mapper);
        SceneManagers.Register<EditorExtrasManager>().ForScene(CMScene.Mapper);
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

        var testLog = map.AddAction("VainMapper Test Log", type: InputActionType.Button);
        testLog.AddBinding("<Keyboard>/k");
			
        CMInputCallbackInstaller.InputInstance.Enable();
        
        
        toggleWindow!.performed += EditorMainWindow.OnToggleWindow;
        testLog.performed += _ =>
        {
            if (OutlineManager.Instance != null && EditorManager.Instance != null)
            {
                var selection = SelectionController.SelectedObjects.ToArray();
                SelectionController.DeselectAll();
                
                OutlineManager.Instance.ClearAllOutlines();
                foreach (var obj in selection)
                {
                    var c = Random.ColorHSV();
                    OutlineManager.Instance.SetOutline(obj, c);
                }
                OutlineManager.Instance.RefreshAllOutlines();
            }
        };
    }
}

public static class PluginResources
{
    private static readonly ResourceLoader s_loader = new(
        new EmbeddedResourceLocation(Assembly.GetExecutingAssembly()),
        "Resources/meta.json"
    );

    public static bool HasResource(string path) => s_loader.HasResource(path);

    public static Sprite LoadSprite(string path) => s_loader.LoadSprite(path);

    public static bool TryLoadSprite(string path, out Sprite sprite) => s_loader.TryLoadSprite(path, out sprite);

    public static Texture2D LoadTexture(string path) => s_loader.LoadTexture(path);

    public static byte[] LoadBytes(string path) => s_loader.LoadBytes(path);

    public static string LoadText(string path) => s_loader.LoadText(path);
}