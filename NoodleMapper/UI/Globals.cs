using System;
using System.Reflection;
using NoodleMapper.Utils;
using NoodleMapper.Wiring;
using UnityEngine;
using UnityEngine.Events;

namespace NoodleMapper.UI;

public static class Globals
{
    private abstract class AssetAttribute : Attribute;
    private class SpriteAssetAttribute : AssetAttribute
    {
        public bool IsPercentBased { get; }
        public int BorderPx { get; }
        public float BorderPct { get; }
    
        public SpriteAssetAttribute()
        {
            BorderPx = 0;
            BorderPct = 0;
            IsPercentBased = true;
        }
        public SpriteAssetAttribute(int borderPx)
        {
            BorderPx = borderPx;
            BorderPct = 0;
            IsPercentBased = false;
        }
        public SpriteAssetAttribute(float borderPct)
        {
            BorderPx = 0;
            BorderPct = borderPct;
            IsPercentBased = true;
        }
    }
    public static class Assets
    {
        
        [SpriteAsset(12)] public static Sprite RoundRect = null!;
        [SpriteAsset(12)] public static Sprite RoundRectBordered = null!;
        [SpriteAsset(12)] public static Sprite RoundRectBorderedSharp = null!;
        [SpriteAsset(12)] public static Sprite RoundRectBorderOnly = null!;
        
        [SpriteAsset(12)] public static Sprite ButtonRaised = null!;
        [SpriteAsset(12)] public static Sprite ButtonDepressed = null!;

        [SpriteAsset] public static Sprite CloseButton = null!;
        [SpriteAsset] public static Sprite WindowCorner = null!;
        [SpriteAsset(12)] public static Sprite TitleBar = null!;
        [SpriteAsset(0.5f)] public static Sprite Shadow = null!;
        
        [SpriteAsset] public static Sprite DragHandle = null!;
        [SpriteAsset] public static Sprite Endpoint = null!;
    }

    public static class Events
    {
        public static UnityEvent ExtensionButtonClicked = new ();
    }
    
    [OnPluginInit]
    private static void OnPluginInit()
    {
        var assetFields = typeof(Assets).GetFields();
        for (int i = 0; i < assetFields.Length; i++)
        {
            var field = assetFields[i];
            var resourceName = field.Name;
            var attr = field.GetCustomAttribute<AssetAttribute>();
            if (attr is null) continue;

            if (attr is SpriteAssetAttribute spriteAttr)
            {
                resourceName += ".png";
                if (spriteAttr.IsPercentBased)
                    field.SetValue(null, Helpers.LoadSprite(resourceName, spriteAttr.BorderPct));
                else
                    field.SetValue(null, Helpers.LoadSprite(resourceName, spriteAttr.BorderPx));
            }
        }
        
        ExtensionButtons.AddButton(
            Helpers.LoadSprite("ExtensionButtonIcon.png"),
            "NoodleMapper",
            () => { Events.ExtensionButtonClicked.Invoke(); }
        );
    }
}