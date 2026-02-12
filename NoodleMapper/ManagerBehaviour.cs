using System;
using System.Collections.Generic;
using System.Reflection;
using NoodleMapper.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NoodleMapper;

public static class SceneManagers
{
    public interface IManagerRegistration
    {
        IManagerRegistration ForScene(CMSceneIndex sceneIndex);
    }

    private class ManagerRegistration : IManagerRegistration
    {
        public Type ManagerComponentType;
        
        public CMSceneIndex SceneIndex = default;

        public ManagerRegistration(Type componentType)
        {
            ManagerComponentType = componentType;
        }

        public IManagerRegistration ForScene(CMSceneIndex sceneIndex)
        {
            SceneIndex = sceneIndex;
            return this;
        }
    }
    
    private static readonly List<ManagerRegistration> s_managers = new();
    
    public static IManagerRegistration Register<TManager>() where TManager : ManagerBehaviour<TManager>
    {
        var registration = new ManagerRegistration(typeof(TManager));
        s_managers.Add(registration);
        return registration;
    }

    public static void SceneLoaded(Scene scene)
    {
        var cmScene = (CMSceneIndex)scene.buildIndex;

        foreach (var manager in s_managers)
        {
            if (manager.SceneIndex == cmScene)
            {
                var managerObject = new GameObject(manager.ManagerComponentType.Name);
                SceneManager.MoveGameObjectToScene(managerObject, scene);
                
                managerObject.AddInitComponent(manager.ManagerComponentType);
            }
        }
    }
}
public abstract class ManagerBehaviour<T> : MonoBehaviour where T : ManagerBehaviour<T>
{
    private static T? s_instance;
    public static T? Instance => s_instance;
    protected abstract void PostInit();
    
    protected void Init()
    {
        if (s_instance)
            Destroy(s_instance!.gameObject);
        s_instance = GetComponent<T>();
        
        PostInit();
    }
}