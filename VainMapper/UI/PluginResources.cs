using System.Reflection;
using UnityEngine;
using VainLib.IO;

namespace VainMapper.UI;

public static class PluginResources
{
    private static readonly ResourceLoader s_loader = new(
        new EmbeddedResourceLocation(Assembly.GetExecutingAssembly()),
        "Resources/meta.json"
    );

    public static bool HasResource(string path) => s_loader.HasResource(path);

    public static Sprite LoadSprite(string path) => s_loader.LoadSprite(path);

    public static bool TryLoadSprite(string path, out Sprite sprite) => s_loader.TryLoadSprite(path, out sprite);

    public static Texture2D LoadTexture(string path) => s_loader.LoadTexture(path);

    public static byte[] LoadBytes(string path) => s_loader.LoadBytes(path);

    public static string LoadText(string path) => s_loader.LoadText(path);
}
