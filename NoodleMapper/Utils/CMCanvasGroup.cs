namespace NoodleMapper.Utils;

public enum CMCanvasGroup
{
    SongTimeline = 0,
    CountersPlus = 1,
    NodeEditor = 2,
    ChromaColorSelector = 3,
    RightBar = 4,
    TopBar = 5,
}

public enum CMSceneIndex
{
    /*
  From ChroMapper:
  m_Scenes:
  - enabled: 1
    path: Assets/__Scenes/00_FirstBoot.unity
    guid: debe4ad1201f95a438652aeca403e7ba
  - enabled: 1
    path: Assets/__Scenes/01_SongSelectMenu.unity
    guid: 8cc6b08691b8a884e91842e2d48bce7d
  - enabled: 1
    path: Assets/__Scenes/02_SongEditMenu.unity
    guid: dc0585e4d7a6f3644a4e4448159e89c8
  - enabled: 1
    path: Assets/__Scenes/03_Mapper.unity
    guid: 98c1965433f244b4f9796434654abb65
  - enabled: 1
    path: Assets/__Scenes/04_Options.unity
    guid: 56ec1747ec4bdfa4396104eda05c448a
  - enabled: 0
    path: Assets/__Scenes/99_EditorHelper.unity
    guid: 26fc50e4208da4544bf2ab89e91ccdd3
  - enabled: 0
    path: Assets/__Scenes/999_PrefabBuilding.unity
    guid: 34ae2ae2fc6102c4e8b74df89340ed7f
     */
    FirstBoot = 0,
    SongSelectMenu = 1,
    SongEditMenu = 2,
    Mapper = 3,
    Options = 4
}