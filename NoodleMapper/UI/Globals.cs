using NoodleMapper.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace NoodleMapper.UI;

public static class Globals
{
    public static class Assets
    {
        public static Sprite RoundRect = null!;
        public static Sprite RoundRectBordered = null!;
        public static Sprite RoundRectBorderedSharp = null!;
        public static Sprite RoundRectBorderOnly = null!;
        
        public static Sprite ButtonRaised = null!;
        public static Sprite ButtonDepressed = null!;

        public static Sprite CloseButton = null!;
        public static Sprite WindowCorner = null!;
        public static Sprite TitleBar = null!;
        public static Sprite Shadow = null!;
    }

    public static class Events
    {
        public static UnityEvent ExtensionButtonClicked = new ();
    }

    public static void Load()
    {
        Assets.RoundRect = Helpers.LoadSprite("RoundRect.png", 12);
        Assets.RoundRectBordered = Helpers.LoadSprite("RoundRectBordered.png", 12);
        Assets.RoundRectBorderedSharp = Helpers.LoadSprite("RoundRectBorderedSharp.png", 12);
        Assets.RoundRectBorderOnly = Helpers.LoadSprite("RoundRectBorderOnly.png", 12);
        
        Assets.ButtonRaised = Helpers.LoadSprite("ButtonRaised.png", 12);
        Assets.ButtonDepressed = Helpers.LoadSprite("ButtonDepressed.png", 12);
        
        Assets.CloseButton = Helpers.LoadSprite("CloseButton.png");
        Assets.WindowCorner = Helpers.LoadSprite("WindowCorner.png");
        Assets.TitleBar = Helpers.LoadSprite("TitleBar.png", 12);
        Assets.Shadow = Helpers.LoadSprite("Shadow.png", 0.5f);
        
        ExtensionButtons.AddButton(
            Helpers.LoadSprite("ExtensionButtonIcon.png"),
            "NoodleMapper",
            () => { Events.ExtensionButtonClicked.Invoke(); }
        );
    }
}