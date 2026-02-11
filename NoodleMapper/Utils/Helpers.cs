using System.Reflection;
using UnityEngine;

namespace NoodleMapper.Utils;

public static class Helpers
{
    public static Sprite LoadSprite(string asset) {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"NoodleMapper.Resources.{asset}");
        byte[] data = new byte[stream!.Length];
        stream.Read(data, 0, (int)stream.Length);
		
        Texture2D texture2D = new Texture2D(256, 256);
        texture2D.LoadImage(data);
		
        return Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 100.0f);
    }
}