using NoodleMapper.Utils;
using UnityEngine;

namespace NoodleMapper.UI;

public static class StaticAssets
{
    public static Sprite RoundRect = null!;
    public static Sprite RoundRectBordered = null!;
    public static Sprite RoundRectBorderedSharp = null!;
    
    public static Sprite CloseButton = null!;
    public static Sprite WindowCorner = null!;
    
    public static void Load()
    {
        RoundRect = Helpers.LoadSlicedSprite("RoundRect.png", 12);
        RoundRectBordered = Helpers.LoadSlicedSprite("RoundRectBordered.png", 12);
        RoundRectBorderedSharp = Helpers.LoadSlicedSprite("RoundRectBorderedSharp.png", 12);
        
        CloseButton = Helpers.LoadSprite("CloseButton.png");
        WindowCorner = Helpers.LoadSprite("WindowCorner.png");
    }
}