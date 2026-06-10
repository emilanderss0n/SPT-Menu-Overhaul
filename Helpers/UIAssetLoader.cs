using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class UIAssetLoader
    {
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static Texture2D LoadTextureFromDirectory(string directory, string baseFileName)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseFileName))
            {
                return null;
            }

            string cacheKey = BuildCacheKey(directory, baseFileName);
            if (TextureCache.TryGetValue(cacheKey, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            string filePath = ResolveAssetPath(directory, baseFileName);
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(fileBytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Layout, $"Failed to decode image file '{filePath}'.");
                    return null;
                }

                texture.name = Path.GetFileNameWithoutExtension(filePath);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                TextureCache[cacheKey] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Layout, $"Failed to read texture from '{filePath}': {ex.Message}");
                return null;
            }
        }

        public static Sprite LoadSpriteFromDirectory(string directory, string baseFileName, float pixelsPerUnit = 100f)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseFileName))
            {
                return null;
            }

            string cacheKey = BuildCacheKey(directory, baseFileName + "|" + pixelsPerUnit);
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D texture = LoadTextureFromDirectory(directory, baseFileName);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        public static void ClearCaches()
        {
            foreach (Sprite sprite in SpriteCache.Values)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
            }
            SpriteCache.Clear();

            foreach (Texture2D texture in TextureCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }
            TextureCache.Clear();
        }

        private static string ResolveAssetPath(string directory, string baseFileName)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseFileName) || !Directory.Exists(directory))
            {
                return null;
            }

            string[] extensions = new[] { ".png", ".jpg", ".jpeg", ".tga" };

            if (Path.HasExtension(baseFileName))
            {
                string explicitPath = Path.Combine(directory, baseFileName);
                if (File.Exists(explicitPath))
                {
                    return explicitPath;
                }
            }

            foreach (string extension in extensions)
            {
                string candidatePath = Path.Combine(directory, baseFileName + extension);
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            string[] matchingFiles = Directory.GetFiles(directory, baseFileName + ".*");
            if (matchingFiles.Length > 0)
            {
                return matchingFiles[0];
            }

            return null;
        }

        private static string BuildCacheKey(string directory, string baseFileName)
        {
            return string.Concat(directory, "|", baseFileName).ToLowerInvariant();
        }
    }
}
