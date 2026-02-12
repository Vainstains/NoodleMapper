using System.Collections.Generic;
using NoodleMapper.Wiring;
using UnityEngine.SceneManagement;

namespace NoodleMapper.Utils.Scenes;

public static class SceneUtils
{
    public static Scene GetScene(this CMScene scene) => SceneManager.GetSceneByBuildIndex((int)scene);
}