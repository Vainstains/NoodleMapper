using NoodleMapper.UI.Components;

namespace NoodleMapper.UI;

public abstract class GenericWindow<TWindow> : Window where TWindow : GenericWindow<TWindow>
{
    private static TWindow? s_uiInstance;
    public static void ToggleUI()
    {
        if (s_uiInstance)
        {
            s_uiInstance!.Close();
            s_uiInstance = null;
            return;
        }
        
        s_uiInstance = CreateWindow<TWindow>();
    }
    public static void CloseUI()
    {
        if (!s_uiInstance)
            return;
        
        s_uiInstance!.Close();
        s_uiInstance = null;
    }
}