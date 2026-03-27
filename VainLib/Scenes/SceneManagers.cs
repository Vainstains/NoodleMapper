using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VainLib.UI.Components;
using VainLib.Utils;

namespace VainLib.Scenes;

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
    
    internal static void InitializeSceneStuff()
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