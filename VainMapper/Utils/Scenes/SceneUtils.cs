using System.Collections.Generic;
using VainMapper.Wiring;
using UnityEngine.SceneManagement;

namespace VainMapper.Utils.Scenes;

public static class SceneUtils
{
    public static Scene GetScene(this CMScene scene) => SceneManager.GetSceneByBuildIndex((int)scene);
}