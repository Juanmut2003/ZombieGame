#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// Builds the HUD face: a 5x7 pixel font.
    /// </summary>
    /// <remarks>
    /// The face is held here as bitmaps rather than imported as a font file, because that is the
    /// form it was designed in — seven rows of five bits per character. Everything is derived
    /// from <see cref="Glyphs"/>, so a changed letter is a menu click.
    /// The glyphs are drawn white and carry no colour of their own, which leaves a label's own
    /// colour in charge of the whole appearance: a <see cref="UnityEngine.UI.Text"/> multiplies
    /// the glyph by one vertex colour, so white here means a label is exactly the colour it is
    /// set to. Anything baked in instead would fight every tint the presenters apply.
    /// </remarks>
    public static class NeonbyteFont
    {
        /// <summary>Where the generated face and its texture are kept.</summary>
        private const string Folder = "Assets/_Project/Art/Fonts";

        /// <summary>The font asset other code loads.</summary>
        public const string FontPath = Folder + "/Neonbyte.fontsettings";

        private const string TexturePath = Folder + "/Neonbyte.png";
        private const string MaterialPath = Folder + "/Neonbyte.mat";

        /// <summary>Columns of pixels in one character.</summary>
        private const int GlyphWidth = 5;

        /// <summary>Rows of pixels in one character, above the baseline.</summary>
        private const int GlyphHeight = 7;

        /// <summary>Gap to the next character, in the same pixels.</summary>
        private const int Tracking = 1;

        /// <summary>
        /// Baseline-to-baseline distance.
        /// </summary>
        /// <remarks>
        /// Three pixels of air under a seven-pixel letter. Less and the stacked lines of the
        /// orders list read as one block; much more and a four-line readout drifts apart.
        /// </remarks>
        private const int LineSpacing = GlyphHeight + 3;

        /// <summary>First character in the sheet.</summary>
        private const int FirstChar = 32;

        /// <summary>Last character in the sheet.</summary>
        private const int LastChar = 126;

        /// <summary>How many characters sit side by side in the texture.</summary>
        private const int Columns = 16;

        /// <summary>The texture is square and a power of two, which keeps every importer happy.</summary>
        private const int AtlasSize = 128;

        /// <summary>
        /// The letters.
        /// </summary>
        /// <remarks>
        /// White on purpose. The sheet is a stencil, not artwork: each label multiplies it by its
        /// own colour, so white is the only value that lets a label be the colour it asks for.
        /// </remarks>
        private static readonly Color32 Ink = new(255, 255, 255, 255);

        /// <summary>Rebuilds the font, its texture and its material from the bitmaps below.</summary>
        [MenuItem("ZombieGame/Content/Rebuild Neonbyte Font")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Fonts");
            }

            Texture2D texture = PaintAtlas();
            System.IO.File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            ApplyTextureImport();

            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            Material material = EnsureMaterial(imported);
            WriteFont(imported, material);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Art] built the Neonbyte font at {FontPath}");
        }

        /// <summary>Draws every character into one texture.</summary>
        private static Texture2D PaintAtlas()
        {
            var texture = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, mipChain: false);

            var clear = new Color32[AtlasSize * AtlasSize];
            texture.SetPixels32(clear);

            for (int code = FirstChar; code <= LastChar; code++)
            {
                int[] rows = RowsFor((char)code);
                int index = code - FirstChar;
                int left = (index % Columns) * (GlyphWidth + Tracking);
                int top = (index / Columns) * LineSpacing;

                for (int row = 0; row < GlyphHeight; row++)
                {
                    for (int column = 0; column < GlyphWidth; column++)
                    {
                        if ((rows[row] & (1 << (GlyphWidth - 1 - column))) == 0)
                        {
                            continue;
                        }

                        Plot(texture, left + column, top + row, Ink);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Sets one pixel, counting rows from the top of the sheet.
        /// </summary>
        /// <remarks>
        /// The bitmaps read downwards and a texture reads upwards, so this is the one place the
        /// two meet. Pixels off the bottom of the sheet are dropped rather than wrapped.
        /// </remarks>
        private static void Plot(Texture2D texture, int x, int rowFromTop, Color32 colour)
        {
            int y = AtlasSize - 1 - rowFromTop;
            if (x < 0 || x >= AtlasSize || y < 0 || y >= AtlasSize)
            {
                return;
            }

            texture.SetPixel(x, y, colour);
        }

        /// <summary>Point filtering, no compression: anything else smears a five-pixel letter.</summary>
        private static void ApplyTextureImport()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = AtlasSize;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// The material the text is drawn with.
        /// </summary>
        /// <remarks>
        /// <c>UI/Default</c> rather than Unity's own text shader, which reads the sheet as a mask
        /// and would throw away the colour that is the whole point of baking it in.
        /// </remarks>
        private static Material EnsureMaterial(Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("UI/Default"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Writes the glyph metrics into the font asset.</summary>
        /// <remarks>
        /// Line spacing, point size and ascent have no setters on <see cref="Font"/>, so they go
        /// in through the serialised object the same way the scene builders set private fields.
        /// </remarks>
        private static void WriteFont(Texture2D texture, Material material)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                font = new Font("Neonbyte");
                AssetDatabase.CreateAsset(font, FontPath);
            }

            font.material = material;
            font.characterInfo = Metrics();

            var serialized = new SerializedObject(font);
            SetFloat(serialized, "m_LineSpacing", LineSpacing);
            SetFloat(serialized, "m_FontSize", GlyphHeight);
            SetFloat(serialized, "m_Ascent", GlyphHeight);
            SetReference(serialized, "m_Texture", texture);
            SetReference(serialized, "m_DefaultMaterial", material);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(font);
        }

        /// <summary>
        /// Writes one of the font's own fields, saying so if it is not there.
        /// </summary>
        /// <remarks>
        /// These are the asset's private fields rather than a public interface, so a Unity
        /// version that spells one differently is a real possibility. Naming the field beats the
        /// null reference that writing straight through would throw, which says only that
        /// something inside this method went wrong.
        /// </remarks>
        private static void SetFloat(SerializedObject font, string field, float value)
        {
            SerializedProperty? property = font.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Art] the font asset has no '{field}'; its metrics are unset.");
                return;
            }

            property.floatValue = value;
        }

        private static void SetReference(SerializedObject font, string field, Object value)
        {
            SerializedProperty? property = font.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Art] the font asset has no '{field}'; it has no texture.");
                return;
            }

            property.objectReferenceValue = value;
        }

        /// <summary>Where each character sits in the sheet, and how much room it takes on a line.</summary>
        private static CharacterInfo[] Metrics()
        {
            var infos = new List<CharacterInfo>(LastChar - FirstChar + 1);

            for (int code = FirstChar; code <= LastChar; code++)
            {
                int index = code - FirstChar;
                float left = (index % Columns) * (GlyphWidth + Tracking);
                float top = (index / Columns) * LineSpacing;

                // The quad is the letter and nothing else: it sits on the baseline and reaches
                // seven pixels up, so a line of text takes exactly the room it looks like it does.
                float boxTop = (AtlasSize - top) / (float)AtlasSize;
                float boxBottom = (AtlasSize - top - GlyphHeight) / (float)AtlasSize;
                float boxLeft = left / AtlasSize;
                float boxRight = (left + GlyphWidth) / AtlasSize;

                infos.Add(new CharacterInfo
                {
                    index = code,
                    uvBottomLeft = new Vector2(boxLeft, boxBottom),
                    uvBottomRight = new Vector2(boxRight, boxBottom),
                    uvTopLeft = new Vector2(boxLeft, boxTop),
                    uvTopRight = new Vector2(boxRight, boxTop),
                    minX = 0,
                    maxX = GlyphWidth,
                    minY = 0,
                    maxY = GlyphHeight,
                    advance = GlyphWidth + Tracking,
                });
            }

            return infos.ToArray();
        }

        private static int[] RowsFor(char glyph) =>
            Glyphs.TryGetValue(glyph, out int[] rows) ? rows : Glyphs['?'];

        /// <summary>
        /// The face itself: seven rows of five bits per character, most significant bit on the
        /// left.
        /// </summary>
        private static readonly Dictionary<char, int[]> Glyphs = new()
        {
            [' '] = new[] { 0, 0, 0, 0, 0, 0, 0 },
            ['!'] = new[] { 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00000, 0b00100 },
            ['"'] = new[] { 0b01010, 0b01010, 0b01010, 0b00000, 0b00000, 0b00000, 0b00000 },
            ['#'] = new[] { 0b01010, 0b01010, 0b11111, 0b01010, 0b11111, 0b01010, 0b01010 },
            ['$'] = new[] { 0b00100, 0b01111, 0b10100, 0b01110, 0b00101, 0b11110, 0b00100 },
            ['%'] = new[] { 0b11001, 0b11001, 0b00010, 0b00100, 0b01000, 0b10011, 0b10011 },
            ['&'] = new[] { 0b01100, 0b10010, 0b10100, 0b01000, 0b10101, 0b10010, 0b01101 },
            ['\''] = new[] { 0b00100, 0b00100, 0b01000, 0b00000, 0b00000, 0b00000, 0b00000 },
            ['('] = new[] { 0b00010, 0b00100, 0b01000, 0b01000, 0b01000, 0b00100, 0b00010 },
            [')'] = new[] { 0b01000, 0b00100, 0b00010, 0b00010, 0b00010, 0b00100, 0b01000 },
            ['*'] = new[] { 0b00000, 0b00100, 0b10101, 0b01110, 0b10101, 0b00100, 0b00000 },
            ['+'] = new[] { 0b00000, 0b00100, 0b00100, 0b11111, 0b00100, 0b00100, 0b00000 },
            [','] = new[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00100, 0b01000 },
            ['-'] = new[] { 0b00000, 0b00000, 0b00000, 0b11111, 0b00000, 0b00000, 0b00000 },
            ['.'] = new[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b01100, 0b01100 },
            ['/'] = new[] { 0b00001, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b10000 },
            ['0'] = new[] { 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110 },
            ['1'] = new[] { 0b00100, 0b01100, 0b10100, 0b00100, 0b00100, 0b00100, 0b11111 },
            ['2'] = new[] { 0b01110, 0b10001, 0b00001, 0b00110, 0b01000, 0b10000, 0b11111 },
            ['3'] = new[] { 0b01110, 0b10001, 0b00001, 0b00110, 0b00001, 0b10001, 0b01110 },
            ['4'] = new[] { 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010 },
            ['5'] = new[] { 0b11111, 0b10000, 0b11110, 0b00001, 0b00001, 0b10001, 0b01110 },
            ['6'] = new[] { 0b00110, 0b01000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110 },
            ['7'] = new[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000 },
            ['8'] = new[] { 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 },
            ['9'] = new[] { 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00010, 0b01100 },
            [':'] = new[] { 0b00000, 0b01100, 0b01100, 0b00000, 0b01100, 0b01100, 0b00000 },
            [';'] = new[] { 0b00000, 0b01100, 0b01100, 0b00000, 0b01100, 0b00100, 0b01000 },
            ['<'] = new[] { 0b00010, 0b00100, 0b01000, 0b10000, 0b01000, 0b00100, 0b00010 },
            ['='] = new[] { 0b00000, 0b00000, 0b11111, 0b00000, 0b11111, 0b00000, 0b00000 },
            ['>'] = new[] { 0b01000, 0b00100, 0b00010, 0b00001, 0b00010, 0b00100, 0b01000 },
            ['?'] = new[] { 0b01110, 0b10001, 0b00001, 0b00110, 0b00100, 0b00000, 0b00100 },
            ['@'] = new[] { 0b01110, 0b10001, 0b10111, 0b10101, 0b10110, 0b10000, 0b01110 },
            ['A'] = new[] { 0b00100, 0b01010, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001 },
            ['B'] = new[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 },
            ['C'] = new[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 },
            ['D'] = new[] { 0b11100, 0b10010, 0b10001, 0b10001, 0b10001, 0b10010, 0b11100 },
            ['E'] = new[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 },
            ['F'] = new[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 },
            ['G'] = new[] { 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01111 },
            ['H'] = new[] { 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
            ['I'] = new[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b11111 },
            ['J'] = new[] { 0b00111, 0b00010, 0b00010, 0b00010, 0b00010, 0b10010, 0b01100 },
            ['K'] = new[] { 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001 },
            ['L'] = new[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 },
            ['M'] = new[] { 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001 },
            ['N'] = new[] { 0b10001, 0b11001, 0b10101, 0b10101, 0b10011, 0b10001, 0b10001 },
            ['O'] = new[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
            ['P'] = new[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 },
            ['Q'] = new[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101 },
            ['R'] = new[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 },
            ['S'] = new[] { 0b01110, 0b10001, 0b10000, 0b01110, 0b00001, 0b10001, 0b01110 },
            ['T'] = new[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
            ['U'] = new[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
            ['V'] = new[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b01010, 0b00100 },
            ['W'] = new[] { 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b11011, 0b10001 },
            ['X'] = new[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001 },
            ['Y'] = new[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100 },
            ['Z'] = new[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111 },
            ['['] = new[] { 0b01110, 0b01000, 0b01000, 0b01000, 0b01000, 0b01000, 0b01110 },
            ['\\'] = new[] { 0b10000, 0b10000, 0b01000, 0b00100, 0b00010, 0b00001, 0b00001 },
            [']'] = new[] { 0b01110, 0b00010, 0b00010, 0b00010, 0b00010, 0b00010, 0b01110 },
            ['^'] = new[] { 0b00100, 0b01010, 0b10001, 0b00000, 0b00000, 0b00000, 0b00000 },
            ['_'] = new[] { 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b11111 },
            ['`'] = new[] { 0b01000, 0b00100, 0b00010, 0b00000, 0b00000, 0b00000, 0b00000 },
            ['a'] = new[] { 0b00000, 0b00000, 0b01110, 0b00001, 0b01111, 0b10001, 0b01111 },
            ['b'] = new[] { 0b10000, 0b10000, 0b10110, 0b11001, 0b10001, 0b11001, 0b10110 },
            ['c'] = new[] { 0b00000, 0b00000, 0b01110, 0b10000, 0b10000, 0b10001, 0b01110 },
            ['d'] = new[] { 0b00001, 0b00001, 0b01101, 0b10011, 0b10001, 0b10011, 0b01101 },
            ['e'] = new[] { 0b00000, 0b00000, 0b01110, 0b10001, 0b11111, 0b10000, 0b01110 },
            ['f'] = new[] { 0b00110, 0b01001, 0b01000, 0b11100, 0b01000, 0b01000, 0b01000 },
            ['g'] = new[] { 0b00000, 0b00000, 0b01111, 0b10001, 0b01111, 0b00001, 0b01110 },
            ['h'] = new[] { 0b10000, 0b10000, 0b10110, 0b11001, 0b10001, 0b10001, 0b10001 },
            ['i'] = new[] { 0b00100, 0b00000, 0b01100, 0b00100, 0b00100, 0b00100, 0b01110 },
            ['j'] = new[] { 0b00010, 0b00000, 0b00110, 0b00010, 0b00010, 0b10010, 0b01100 },
            ['k'] = new[] { 0b10000, 0b10000, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010 },
            ['l'] = new[] { 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
            ['m'] = new[] { 0b00000, 0b00000, 0b11010, 0b10101, 0b10101, 0b10101, 0b10001 },
            ['n'] = new[] { 0b00000, 0b00000, 0b10110, 0b11001, 0b10001, 0b10001, 0b10001 },
            ['o'] = new[] { 0b00000, 0b00000, 0b01110, 0b10001, 0b10001, 0b10001, 0b01110 },
            ['p'] = new[] { 0b00000, 0b00000, 0b10110, 0b11001, 0b11110, 0b10000, 0b10000 },
            ['q'] = new[] { 0b00000, 0b00000, 0b01101, 0b10011, 0b01111, 0b00001, 0b00001 },
            ['r'] = new[] { 0b00000, 0b00000, 0b10110, 0b11001, 0b10000, 0b10000, 0b10000 },
            ['s'] = new[] { 0b00000, 0b00000, 0b01110, 0b10000, 0b01110, 0b00001, 0b11110 },
            ['t'] = new[] { 0b01000, 0b01000, 0b11100, 0b01000, 0b01000, 0b01001, 0b00110 },
            ['u'] = new[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b10001, 0b10011, 0b01101 },
            ['v'] = new[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 },
            ['w'] = new[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b10101, 0b10101, 0b01010 },
            ['x'] = new[] { 0b00000, 0b00000, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001 },
            ['y'] = new[] { 0b00000, 0b00000, 0b10001, 0b10001, 0b01111, 0b00001, 0b01110 },
            ['z'] = new[] { 0b00000, 0b00000, 0b11111, 0b00010, 0b00100, 0b01000, 0b11111 },
            ['{'] = new[] { 0b00010, 0b00100, 0b00100, 0b01000, 0b00100, 0b00100, 0b00010 },
            ['|'] = new[] { 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
            ['}'] = new[] { 0b01000, 0b00100, 0b00100, 0b00010, 0b00100, 0b00100, 0b01000 },
            ['~'] = new[] { 0b00000, 0b00000, 0b01000, 0b10101, 0b00010, 0b00000, 0b00000 },
        };
    }
}
