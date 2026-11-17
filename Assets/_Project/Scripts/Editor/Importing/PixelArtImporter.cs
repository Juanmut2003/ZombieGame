#nullable enable
using UnityEditor;
using UnityEngine;

namespace ZombieGame.Editor.Importing
{
    /// <summary>
    /// Forces the project's pixel-art import settings onto every sprite under
    /// <see cref="PixelArtRules.SpriteRoot"/>, so nobody has to remember them per file.
    /// </summary>
    /// <remarks>
    /// A single sprite imported with bilinear filtering or compression is enough to make a
    /// pixel-art scene look mushy, and the mistake is invisible until it ships. Enforcing it here
    /// means the rule cannot be forgotten, and dropping new art into the folder just works.
    /// </remarks>
    public sealed class PixelArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!PixelArtRules.AppliesTo(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = PixelArtRules.PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;

            // Full Rect keeps every sprite a plain quad. Tight meshes save a few pixels of overdraw
            // but shift the visible bounds per sprite, which breaks pixel-perfect alignment.
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)PixelArtRules.PivotFor(assetPath);
            importer.SetTextureSettings(settings);
        }
    }
}
