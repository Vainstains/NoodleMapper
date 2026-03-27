using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace VainLib.Scenes;

public static class SceneUtils
{
    public static Scene GetScene(this CMScene scene) => SceneManager.GetSceneByBuildIndex((int)scene);
}