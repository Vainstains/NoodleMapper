using System;
using UnityEngine;
using UnityEngine.Events;
using VainMapper.Utils;
using VainMapper.Wiring;

namespace VainMapper.UI;

public static class Events
{
    public static UnityEvent ExtensionButtonClicked = new ();
    
    [OnPluginInit]
    private static void OnPluginInit()
    {
        ExtensionButtons.AddButton(
            PluginResources.LoadSprite("Resources/ExtensionButtonIcon.png"),
            Helpers.CurrentPluginName,
            () => { ExtensionButtonClicked.Invoke(); }
        );
            
        ExtensionButtons.AddButton(
            PluginResources.LoadSprite("Resources/RebootButtonIcon.png"),
            "Reboot ChroMapper and return to where you are now",
            Rebooter.Reboot
        );
    }
}
