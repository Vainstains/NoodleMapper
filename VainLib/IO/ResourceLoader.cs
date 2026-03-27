using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;
using VainLib.Data;

namespace VainLib.IO;

/// <summary>
///     A location to load resources from. Use file paths here and leave it up to the implementation to
///     reformat them into the correct format for themselves.
/// </summary>
public interface IResourceLocation
{
    bool TryGetStream(string path, out Stream stream);
    bool PathExists(string path);
}

public class EmbeddedResourceLocation : IResourceLocation
{
    private readonly Assembly m_assembly;
    private readonly HashSet<string> m_paths = new();
    private readonly string? m_rootNamespace;

    public EmbeddedResourceLocation(Assembly assembly)
    {
        m_assembly = assembly;

        var allResources = m_assembly.GetManifestResourceNames();
        foreach (var res in allResources)
            m_paths.Add(res);

        if (allResources.Length > 0)
            m_rootNamespace = allResources[0].Split('.')[0];
    }

    private string ConvertFilePathToResourcePath(string path)
    {
        // ex:
        //   "Resources/RoundRect.png" -> "{RootNamespace}.Resources.RoundRect.png"
        return $"{m_rootNamespace}.{path.Replace('/', '.').Replace('\\', '.')}";
    }

    public bool TryGetStream(string path, out Stream stream)
    {
        var resourcePath = ConvertFilePathToResourcePath(path);
        Debug.Log($"Loading stream from `{resourcePath}`");
        if (!m_paths.Contains(resourcePath))
        {
            stream = null!;
            return false;
        }

        stream = m_assembly.GetManifestResourceStream(resourcePath);
        return stream != null;
    }

    public bool PathExists(string path)
    {
        var resourcePath = ConvertFilePathToResourcePath(path);
        return m_paths.Contains(resourcePath);
    }
}

public class ResourceLoader
{
    private class ResourceMetadata
    {
        public class SpriteMetadata
        {
            [JsonID("path")]
            public string Path { get; set; } = "";

            [JsonID("border")]
            public string Border { get; set; } = "";
        }

        [JsonID("sprites")]
        public SpriteMetadata[] Sprites { get; set; } = [];
    }

    private readonly IResourceLocation m_location;
    private readonly ResourceMetadata m_metadata;
    private readonly Dictionary<string, ResourceMetadata.SpriteMetadata> m_spriteMetadata = new();
    private readonly Dictionary<string, Sprite> m_spriteCache = new();
    private readonly string? m_metadataDirectory;

    public ResourceLoader(IResourceLocation location, string? metadataPath = null)
    {
        m_location = location;
        m_metadataDirectory = GetParentPath(metadataPath);
        m_metadata = LoadMetadata(metadataPath);

        foreach (var sprite in m_metadata.Sprites)
        {
            if (string.IsNullOrWhiteSpace(sprite.Path))
                continue;

            var metadataRelativePath = NormalizePath(sprite.Path);
            m_spriteMetadata[metadataRelativePath] = sprite;

            var resolvedPath = ResolveMetadataRelativePath(sprite.Path);
            m_spriteMetadata[resolvedPath] = sprite;

            var fileName = NormalizePath(Path.GetFileName(sprite.Path));
            if (!m_spriteMetadata.ContainsKey(fileName))
                m_spriteMetadata[fileName] = sprite;
        }
    }

    public void FlushCache()
    {
        foreach (var sprite in m_spriteCache.Values)
        {
            if (sprite == null)
                continue;

            var texture = sprite.texture;
            Object.Destroy(sprite);
            if (texture != null)
                Object.Destroy(texture);
        }

        m_spriteCache.Clear();
    }

    public bool HasResource(string path) => m_location.PathExists(NormalizePath(path));

    public byte[] LoadBytes(string path)
    {
        if (!TryLoadBytes(path, out var bytes))
            throw new FileNotFoundException($"Could not load resource at path '{path}'.");

        return bytes;
    }

    public bool TryLoadBytes(string path, out byte[] bytes)
    {
        bytes = [];
        if (!TryResolveStream(path, out var stream))
            return false;

        using (stream)
        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            bytes = memory.ToArray();
            return true;
        }
    }

    public string LoadText(string path)
    {
        if (!TryLoadText(path, out var text))
            throw new FileNotFoundException($"Could not load resource at path '{path}'.");

        return text;
    }

    public bool TryLoadText(string path, out string text)
    {
        text = string.Empty;
        if (!TryResolveStream(path, out var stream))
            return false;

        using (stream)
        using (var reader = new StreamReader(stream))
        {
            text = reader.ReadToEnd();
            return true;
        }
    }

    public JSONNode LoadJson(string path)
    {
        var text = LoadText(path);
        return JSON.Parse(text) ?? new JSONObject();
    }

    public Texture2D LoadTexture(string path)
    {
        if (!TryLoadTexture(path, out var texture))
            throw new FileNotFoundException($"Could not load texture at path '{path}'.");

        return texture;
    }

    public bool TryLoadTexture(string path, out Texture2D texture)
    {
        texture = null!;
        if (!TryLoadBytes(path, out var bytes))
            return false;

        texture = new Texture2D(2, 2);
        if (!texture.LoadImage(bytes))
        {
            Object.Destroy(texture);
            texture = null!;
            return false;
        }

        return true;
    }

    public Sprite LoadSprite(string path)
    {
        if (!TryLoadSprite(path, out var sprite))
            throw new FileNotFoundException($"Could not load sprite at path '{path}'.");

        return sprite;
    }

    public bool TryLoadSprite(string path, out Sprite sprite)
    {
        sprite = null!;

        var resolvedPath = NormalizePath(path);
        if (!m_location.PathExists(resolvedPath))
            return false;

        if (m_spriteCache.TryGetValue(resolvedPath, out sprite))
            return sprite != null;

        if (!TryLoadTexture(resolvedPath, out var texture))
            return false;

        var border = Vector4.zero;
        if (TryGetSpriteMetadata(resolvedPath, out var spriteMetadata) &&
            !string.IsNullOrWhiteSpace(spriteMetadata.Border))
        {
            border = GetSpriteBorder(texture.width, texture.height, spriteMetadata.Border);
        }

        sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border
        );

        m_spriteCache[resolvedPath] = sprite;
        return true;
    }

    public Vector4 GetSpriteBorder(int width, int height, string border)
    {
        if (string.IsNullOrWhiteSpace(border))
            return Vector4.zero;

        // 1 value: controls all sides
        // 2 values: first is top/bottom, second is left/right
        // 3 values: top, left/right, bottom
        // 4 values: goes clockwise: top, right, bottom, left

        var values = border.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length == 0)
            return Vector4.zero;

        if (values.Length == 1)
        {
            var value = ParseBorderValue(values[0], width, height);
            return new Vector4(value.x, value.y, value.x, value.y);
        }

        if (values.Length == 2)
        {
            var vertical = ParseBorderValue(values[0], width, height);
            var horizontal = ParseBorderValue(values[1], width, height);
            return new Vector4(horizontal.x, vertical.y, horizontal.x, vertical.y);
        }

        if (values.Length == 3)
        {
            var top = ParseBorderValue(values[0], width, height);
            var horizontal = ParseBorderValue(values[1], width, height);
            var bottom = ParseBorderValue(values[2], width, height);
            return new Vector4(horizontal.x, bottom.y, horizontal.x, top.y);
        }

        if (values.Length == 4)
        {
            var top = ParseBorderValue(values[0], width, height);
            var right = ParseBorderValue(values[1], width, height);
            var bottom = ParseBorderValue(values[2], width, height);
            var left = ParseBorderValue(values[3], width, height);
            return new Vector4(left.x, bottom.y, right.x, top.y);
        }

        Debug.LogWarning($"Unsupported sprite border format '{border}'.");
        return Vector4.zero;
    }

    private ResourceMetadata LoadMetadata(string? metadataPath)
    {
        if (string.IsNullOrWhiteSpace(metadataPath))
            return new ResourceMetadata();

        if (!TryLoadText(metadataPath, out var metadataText))
        {
            Debug.LogWarning($"Could not load resource metadata at path '{metadataPath}'.");
            return new ResourceMetadata();
        }

        var metadataJson = JSON.Parse(metadataText);
        return metadataJson == null
            ? new ResourceMetadata()
            : JSONReflector.ToObject<ResourceMetadata>(metadataJson) ?? new ResourceMetadata();
    }

    private bool TryGetSpriteMetadata(string path, out ResourceMetadata.SpriteMetadata metadata)
    {
        var normalizedPath = NormalizePath(path);
        if (m_spriteMetadata.TryGetValue(normalizedPath, out metadata))
            return true;

        var fileName = NormalizePath(Path.GetFileName(path));
        return m_spriteMetadata.TryGetValue(fileName, out metadata);
    }

    private bool TryResolveStream(string path, out Stream stream)
    {
        stream = null!;
        var resolvedPath = NormalizePath(path);
        if (!m_location.PathExists(resolvedPath))
            return false;

        return m_location.TryGetStream(resolvedPath, out stream);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private string ResolveMetadataRelativePath(string path)
    {
        var normalizedPath = NormalizePath(path);
        if (string.IsNullOrEmpty(m_metadataDirectory))
            return normalizedPath;

        return CombinePaths(m_metadataDirectory, normalizedPath);
    }

    private static string? GetParentPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var normalized = NormalizePath(path);
        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash < 0)
            return string.Empty;

        return normalized[..lastSlash];
    }

    private static string CombinePaths(string left, string right)
    {
        if (string.IsNullOrEmpty(left))
            return NormalizePath(right);
        if (string.IsNullOrEmpty(right))
            return NormalizePath(left);

        var segments = new List<string>();
        segments.AddRange(NormalizePath(left).Split('/').Where(s => !string.IsNullOrEmpty(s)));
        foreach (var segment in NormalizePath(right).Split('/'))
        {
            if (string.IsNullOrEmpty(segment) || segment == ".")
                continue;

            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return string.Join("/", segments);
    }

    private static Vector2 ParseBorderValue(string value, int width, int height)
    {
        value = value.Trim();
        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
            float.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var px))
        {
            return new Vector2(px, px);
        }

        if (value.EndsWith("%", StringComparison.OrdinalIgnoreCase) &&
            float.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
        {
            var scalar = percent / 100f;
            return new Vector2(width * scalar, height * scalar);
        }

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
            return new Vector2(raw, raw);

        Debug.LogWarning($"Could not parse sprite border value '{value}'.");
        return Vector2.zero;
    }
}
