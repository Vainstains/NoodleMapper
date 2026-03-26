using System;
using System.Collections.Generic;
using VainMapper.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using VainMapper.UI.Components;
using VainMapper.Utils.Scenes;
using VainMapper.Wiring;

namespace VainMapper.Managers;

public static class SceneManagers
{
    public interface IManagerRegistration
    {
        IManagerRegistration ForScene(CMScene scene);
    }

    private class ManagerRegistration : IManagerRegistration
    {
        public Type ManagerComponentType;
        
        public CMScene Scene = default;

        public ManagerRegistration(Type componentType)
        {
            ManagerComponentType = componentType;
        }

        public IManagerRegistration ForScene(CMScene cmScene)
        {
            Scene = cmScene;
            
            var scene = SceneManager.GetActiveScene();
            
            if (Scene == (CMScene)scene.buildIndex)
            {
                var managerObject = new GameObject(ManagerComponentType.Name);
                SceneManager.MoveGameObjectToScene(managerObject, scene);
                
                managerObject.AddInitComponent(ManagerComponentType);
            }
            
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

    [OnPluginInit]
    private static void OnPluginInit()
    {
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WindowContainer.EnsureContainerExists((CMScene)scene.buildIndex);
        foreach (var manager in s_managers)
        {
            if (manager.Scene == (CMScene)scene.buildIndex)
            {
                var managerObject = new GameObject(manager.ManagerComponentType.Name);
                SceneManager.MoveGameObjectToScene(managerObject, scene);
                
                managerObject.AddInitComponent(manager.ManagerComponentType);
            }
        }
    }
}