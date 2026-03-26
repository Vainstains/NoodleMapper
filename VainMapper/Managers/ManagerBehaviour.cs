using VainMapper.Utils;
using UnityEngine;
using VainMapper.UI.Components;

namespace VainMapper.Managers;

public abstract class ManagerBehaviour<T> : MonoBehaviour where T : ManagerBehaviour<T>
{
    private static T? s_instance;
    public static T? Instance => s_instance;
    protected abstract void PostInit();
    
    protected void Init()
    {
        if (s_instance)
        {
            return;
        }
        
        s_instance = GetComponent<T>();
        Window.RebuildAll();
        
        PostInit();
    }

    /// <summary>
    /// Whenever it's time to nuke yourself and reset all the state.
    /// As long as they interface correctly, outside observers won't know a thing.
    /// </summary>
    protected void ResetFresh()
    {
        if (s_instance)
            Destroy(s_instance!.gameObject);
        s_instance = null;
        
        Debug.Log($"Nuking {typeof(T).Name}.Instance and resetting...");
        
        var managerObject = new GameObject(typeof(T).Name);
                
        managerObject.AddInitComponent<T>();
    }
}