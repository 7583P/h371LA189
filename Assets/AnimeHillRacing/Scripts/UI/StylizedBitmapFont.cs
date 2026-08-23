using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public static class StylizedBitmapFont
    {
        private const string AtlasResourcePath = "UI/StylizedGlyphAtlas";
        private const string ShaderResourcePath = "UI/StylizedBitmapFont";
        private const int AtlasWidth = 1774;
        private const int AtlasHeight = 887;

        private static readonly Dictionary<char, Rect> GlyphUvs = BuildGlyphUvs();
        private static Material material;

        public static Material Material
        {
            get
            {
                if (material != null) return material;
                Texture2D atlas = Resources.Load<Texture2D>(AtlasResourcePath);
                Shader shader = Resources.Load<Shader>(ShaderResourcePath);
                if (atlas == null || shader == null)
                {
                    Debug.LogWarning($"Stylized bitmap font could not load {(atlas == null ? AtlasResourcePath : ShaderResourcePath)}.");
                    return null;
                }

                material = new Material(shader)
                {
                    name = "Stylized Glyph Font Material",
                    mainTexture = atlas,
                    hideFlags = HideFlags.DontSave
                };
                return material;
            }
        }

        public static bool ContainsSupportedGlyph(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (GlyphUvs.ContainsKey(char.ToUpperInvariant(character))) return true;
            }
            return false;
        }

        public static bool SupportsAllVisibleGlyphs(string value, bool parseRichText)
        {
            if (string.IsNullOrEmpty(value)) return false;
            bool foundVisibleGlyph = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (parseRichText && character == '<')
                {
                    int tagEnd = value.IndexOf('>', i + 1);
                    if (tagEnd >= 0)
                    {
                        i = tagEnd;
                        continue;
                    }
                }
                if (char.IsWhiteSpace(character)) continue;
                foundVisibleGlyph = true;
                if (!GlyphUvs.ContainsKey(char.ToUpperInvariant(character))) return false;
            }
            return foundVisibleGlyph;
        }

        public static bool TryGetUv(char character, out Rect uv) =>
            GlyphUvs.TryGetValue(char.ToUpperInvariant(character), out uv);

        public static float GetGlyphAspect(Rect uv) =>
            uv.width * AtlasWidth / Mathf.Max(0.0001f, uv.height * AtlasHeight);

        private static Dictionary<char, Rect> BuildGlyphUvs()
        {
            Dictionary<char, Rect> result = new Dictionary<char, Rect>(40);
            AddGlyph(result, '0', new RectInt(48, 63, 157, 168));
            AddGlyph(result, '1', new RectInt(239, 63, 110, 166));
            AddGlyph(result, '2', new RectInt(383, 63, 155, 166));
            AddGlyph(result, '3', new RectInt(554, 63, 156, 166));
            AddGlyph(result, '4', new RectInt(727, 63, 153, 166));
            AddGlyph(result, '5', new RectInt(908, 63, 153, 167));
            AddGlyph(result, '6', new RectInt(1081, 63, 154, 167));
            AddGlyph(result, '7', new RectInt(1256, 63, 146, 166));
            AddGlyph(result, '8', new RectInt(1419, 63, 157, 167));
            AddGlyph(result, '9', new RectInt(1596, 63, 151, 167));

            AddGlyph(result, 'A', new RectInt(25, 289, 132, 124));
            AddGlyph(result, 'B', new RectInt(180, 288, 126, 125));
            AddGlyph(result, 'C', new RectInt(322, 288, 129, 125));
            AddGlyph(result, 'D', new RectInt(463, 289, 126, 124));
            AddGlyph(result, 'E', new RectInt(605, 289, 123, 124));
            AddGlyph(result, 'F', new RectInt(741, 288, 122, 125));
            AddGlyph(result, 'G', new RectInt(874, 288, 125, 126));
            AddGlyph(result, 'H', new RectInt(1016, 289, 131, 125));
            AddGlyph(result, 'I', new RectInt(1163, 289, 70, 124));
            AddGlyph(result, 'J', new RectInt(1247, 289, 120, 125));
            AddGlyph(result, 'K', new RectInt(1382, 291, 133, 123));
            AddGlyph(result, 'L', new RectInt(1525, 289, 98, 124));
            AddGlyph(result, 'M', new RectInt(1633, 289, 130, 124));

            AddGlyph(result, 'N', new RectInt(15, 480, 127, 120));
            AddGlyph(result, 'O', new RectInt(152, 478, 124, 123));
            AddGlyph(result, 'P', new RectInt(290, 478, 122, 122));
            AddGlyph(result, 'Q', new RectInt(424, 478, 122, 128));
            AddGlyph(result, 'R', new RectInt(556, 478, 126, 123));
            AddGlyph(result, 'S', new RectInt(695, 477, 118, 124));
            AddGlyph(result, 'T', new RectInt(828, 478, 117, 123));
            AddGlyph(result, 'U', new RectInt(953, 478, 124, 123));
            AddGlyph(result, 'V', new RectInt(1092, 477, 123, 123));
            AddGlyph(result, 'W', new RectInt(1227, 477, 140, 123));
            AddGlyph(result, 'X', new RectInt(1382, 476, 139, 124));
            // Y and Z shadows meet in the generated sheet; do not let padding pull pixels from the neighbour.
            AddGlyph(result, 'Y', new RectInt(1531, 479, 114, 121), 0);
            AddGlyph(result, 'Z', new RectInt(1645, 478, 117, 122), 0);

            AddGlyph(result, '+', new RectInt(553, 650, 159, 157));
            AddGlyph(result, '-', new RectInt(767, 696, 148, 63));
            AddGlyph(result, ',', new RectInt(958, 711, 62, 88));
            AddGlyph(result, '/', new RectInt(1066, 650, 126, 158));
            return result;
        }

        private static void AddGlyph(Dictionary<char, Rect> destination, char character, RectInt topLeftRect,
            int padding = 3)
        {
            topLeftRect.xMin = Mathf.Max(0, topLeftRect.xMin - padding);
            topLeftRect.yMin = Mathf.Max(0, topLeftRect.yMin - padding);
            topLeftRect.xMax = Mathf.Min(AtlasWidth, topLeftRect.xMax + padding);
            topLeftRect.yMax = Mathf.Min(AtlasHeight, topLeftRect.yMax + padding);
            float uMin = topLeftRect.xMin / (float)AtlasWidth;
            float uMax = topLeftRect.xMax / (float)AtlasWidth;
            float vMax = 1f - topLeftRect.yMin / (float)AtlasHeight;
            float vMin = 1f - topLeftRect.yMax / (float)AtlasHeight;
            destination[character] = Rect.MinMaxRect(uMin, vMin, uMax, vMax);
        }
    }

    internal sealed class StylizedBitmapTextGraphic : MaskableGraphic
    {
        private sealed class GlyphLayout
        {
            public UIVertex[] quad;
            public Rect uv;
            public float sourceMinX;
            public float sourceMaxX;
            public float sourceMinY;
            public float sourceMaxY;
            public float targetWidth;
        }

        private sealed class LineLayout
        {
            public readonly List<GlyphLayout> glyphs = new List<GlyphLayout>(24);
            public float sourceMinX = float.PositiveInfinity;
            public float sourceMaxX = float.NegativeInfinity;
            public float centerY;
            public float referenceHeight;
        }

        private Text source;
        private Color sourceVisibleColor = Color.white;
        private string lastValue;
        private Vector2 lastRectSize;
        private int lastFontSize;

        public override Texture mainTexture => StylizedBitmapFont.Material != null
            ? StylizedBitmapFont.Material.mainTexture
            : s_WhiteTexture;

        public void Configure(Text target, Color visibleColor)
        {
            source = target;
            sourceVisibleColor = visibleColor;
            raycastTarget = false;
            color = Color.white;
            material = StylizedBitmapFont.Material;
            SetAllDirty();
        }

        public void SetVisibleColor(Color value)
        {
            if (sourceVisibleColor == value) return;
            sourceVisibleColor = value;
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            if (source == null) return;
            Vector2 size = source.rectTransform.rect.size;
            if (lastValue == source.text && lastRectSize == size && lastFontSize == source.fontSize) return;
            lastValue = source.text;
            lastRectSize = size;
            lastFontSize = source.fontSize;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (source == null || !source.enabled ||
                !StylizedBitmapFont.SupportsAllVisibleGlyphs(source.text, source.supportRichText) ||
                StylizedBitmapFont.Material == null)
                return;

            Vector2 extents = source.rectTransform.rect.size;
            TextGenerationSettings settings = source.GetGenerationSettings(extents);
            settings.color = Color.white;
            source.cachedTextGenerator.PopulateWithErrors(source.text, settings, source.gameObject);

            IList<UIVertex> generatedVertices = source.cachedTextGenerator.verts;
            if (generatedVertices == null || generatedVertices.Count < 4) return;

            float unitsPerPixel = 1f / Mathf.Max(0.0001f, source.pixelsPerUnit);
            Vector2 roundingOffset = new Vector2(generatedVertices[0].position.x, generatedVertices[0].position.y) * unitsPerPixel;
            roundingOffset = source.PixelAdjustPoint(roundingOffset) - roundingOffset;

            List<LineLayout> lines = new List<LineLayout>(4);
            LineLayout currentLine = null;
            int quadIndex = 0;
            for (int characterIndex = 0; characterIndex < source.text.Length; characterIndex++)
            {
                char character = source.text[characterIndex];
                if (source.supportRichText && character == '<')
                {
                    int tagEnd = source.text.IndexOf('>', characterIndex + 1);
                    if (tagEnd >= 0)
                    {
                        characterIndex = tagEnd;
                        continue;
                    }
                }
                if (char.IsWhiteSpace(character)) continue;
                if (!TryTakeVisibleQuad(generatedVertices, ref quadIndex, unitsPerPixel, roundingOffset,
                    out UIVertex[] quad))
                    break;
                if (!StylizedBitmapFont.TryGetUv(character, out Rect uv)) continue;

                GetQuadBounds(quad, out float minX, out float maxX, out float minY, out float maxY);
                float height = Mathf.Max(.0001f, maxY - minY);
                float centerY = (minY + maxY) * .5f;
                if (currentLine == null || Mathf.Abs(currentLine.centerY - centerY) >
                    Mathf.Max(currentLine.referenceHeight, height) * .45f)
                {
                    currentLine = new LineLayout { centerY = centerY, referenceHeight = height };
                    lines.Add(currentLine);
                }

                GlyphLayout glyph = new GlyphLayout
                {
                    quad = quad,
                    uv = uv,
                    sourceMinX = minX,
                    sourceMaxX = maxX,
                    sourceMinY = minY,
                    sourceMaxY = maxY,
                    targetWidth = height * StylizedBitmapFont.GetGlyphAspect(uv)
                };
                currentLine.glyphs.Add(glyph);
                currentLine.sourceMinX = Mathf.Min(currentLine.sourceMinX, minX);
                currentLine.sourceMaxX = Mathf.Max(currentLine.sourceMaxX, maxX);
                currentLine.referenceHeight = Mathf.Max(currentLine.referenceHeight, height);
            }

            for (int i = 0; i < lines.Count; i++)
                AddReflowedLine(vertexHelper, lines[i], extents.x);
        }

        private static Color32 Multiply(Color32 left, Color right)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(left.r * right.r),
                (byte)Mathf.RoundToInt(left.g * right.g),
                (byte)Mathf.RoundToInt(left.b * right.b),
                (byte)Mathf.RoundToInt(left.a * right.a));
        }

        private static bool TryTakeVisibleQuad(IList<UIVertex> vertices, ref int quadIndex, float unitsPerPixel,
            Vector2 roundingOffset, out UIVertex[] quad)
        {
            quad = null;
            int quadCount = vertices.Count / 4;
            while (quadIndex < quadCount)
            {
                int start = quadIndex++ * 4;
                UIVertex[] candidate = new UIVertex[4];
                float minX = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                float minY = float.PositiveInfinity;
                float maxY = float.NegativeInfinity;
                for (int i = 0; i < 4; i++)
                {
                    candidate[i] = vertices[start + i];
                    Vector3 position = candidate[i].position;
                    position *= unitsPerPixel;
                    position.x += roundingOffset.x;
                    position.y += roundingOffset.y;
                    candidate[i].position = position;
                    minX = Mathf.Min(minX, position.x);
                    maxX = Mathf.Max(maxX, position.x);
                    minY = Mathf.Min(minY, position.y);
                    maxY = Mathf.Max(maxY, position.y);
                }

                if (maxX - minX < 0.01f || maxY - minY < 0.01f) continue;
                quad = candidate;
                return true;
            }
            return false;
        }

        private static void ApplyAtlasUv(UIVertex[] quad, Rect uv)
        {
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < quad.Length; i++)
            {
                Vector3 position = quad[i].position;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
            }

            float width = Mathf.Max(0.0001f, maxX - minX);
            float height = Mathf.Max(0.0001f, maxY - minY);
            for (int i = 0; i < quad.Length; i++)
            {
                Vector3 position = quad[i].position;
                float horizontal = (position.x - minX) / width;
                float vertical = (position.y - minY) / height;
                quad[i].uv0 = new Vector2(Mathf.Lerp(uv.xMin, uv.xMax, horizontal),
                    Mathf.Lerp(uv.yMin, uv.yMax, vertical));
            }
        }

        private void AddReflowedLine(VertexHelper vertexHelper, LineLayout line, float availableWidth)
        {
            if (line == null || line.glyphs.Count == 0) return;
            float naturalWidth = 0f;
            for (int i = 0; i < line.glyphs.Count; i++)
            {
                GlyphLayout glyph = line.glyphs[i];
                naturalWidth += glyph.targetWidth;
                if (i > 0)
                {
                    GlyphLayout previous = line.glyphs[i - 1];
                    naturalWidth += Mathf.Max(line.referenceHeight * .025f,
                        glyph.sourceMinX - previous.sourceMaxX);
                }
            }

            float widthLimit = Mathf.Max(.0001f, availableWidth);
            float uniformScale = naturalWidth > widthLimit ? widthLimit / naturalWidth : 1f;
            float fittedWidth = naturalWidth * uniformScale;
            float cursor;
            TextAnchor alignment = source != null ? source.alignment : TextAnchor.MiddleCenter;
            bool rightAligned = alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight ||
                                alignment == TextAnchor.LowerRight;
            bool leftAligned = alignment == TextAnchor.UpperLeft || alignment == TextAnchor.MiddleLeft ||
                               alignment == TextAnchor.LowerLeft;
            if (rightAligned) cursor = line.sourceMaxX - fittedWidth;
            else if (leftAligned) cursor = line.sourceMinX;
            else cursor = (line.sourceMinX + line.sourceMaxX - fittedWidth) * .5f;

            for (int i = 0; i < line.glyphs.Count; i++)
            {
                GlyphLayout glyph = line.glyphs[i];
                if (i > 0)
                {
                    GlyphLayout previous = line.glyphs[i - 1];
                    float gap = Mathf.Max(line.referenceHeight * .025f,
                        glyph.sourceMinX - previous.sourceMaxX);
                    cursor += gap * uniformScale;
                }
                float targetWidth = glyph.targetWidth * uniformScale;
                float sourceHeight = glyph.sourceMaxY - glyph.sourceMinY;
                float targetHeight = sourceHeight * uniformScale;
                float centerY = (glyph.sourceMinY + glyph.sourceMaxY) * .5f;
                FitQuadToBounds(glyph.quad, cursor, cursor + targetWidth,
                    centerY - targetHeight * .5f, centerY + targetHeight * .5f);
                ApplyAtlasUv(glyph.quad, glyph.uv);
                for (int vertex = 0; vertex < glyph.quad.Length; vertex++)
                    glyph.quad[vertex].color = Multiply(glyph.quad[vertex].color, sourceVisibleColor);
                vertexHelper.AddUIVertexQuad(glyph.quad);
                cursor += targetWidth;
            }
        }

        private static void GetQuadBounds(UIVertex[] quad, out float minX, out float maxX,
            out float minY, out float maxY)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
            for (int i = 0; i < quad.Length; i++)
            {
                Vector3 position = quad[i].position;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
            }
        }

        private static void FitQuadToBounds(UIVertex[] quad, float targetMinX, float targetMaxX,
            float targetMinY, float targetMaxY)
        {
            GetQuadBounds(quad, out float minX, out float maxX, out float minY, out float maxY);
            for (int i = 0; i < quad.Length; i++)
            {
                Vector3 position = quad[i].position;
                position.x = Mathf.Abs(position.x - minX) < Mathf.Abs(position.x - maxX) ? targetMinX : targetMaxX;
                position.y = Mathf.Abs(position.y - minY) < Mathf.Abs(position.y - maxY) ? targetMinY : targetMaxY;
                quad[i].position = position;
            }
        }
    }

    internal sealed class StylizedBitmapFallbackMarker : MonoBehaviour { }

    [DefaultExecutionOrder(10000)]
    internal sealed class StylizedBitmapFontInstaller : MonoBehaviour
    {
        private sealed class TextState
        {
            public Text text;
            public Color visibleColor;
            public StylizedBitmapTextGraphic overlay;
            public Text fallback;
            public string fallbackSourceValue;
            public bool fallbackSourceRichText;
        }

        private readonly List<TextState> trackedTexts = new List<TextState>(128);
        private float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<StylizedBitmapFontInstaller>() != null) return;
            GameObject host = new GameObject("Stylized Bitmap Font Installer");
            DontDestroyOnLoad(host);
            host.AddComponent<StylizedBitmapFontInstaller>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ScanScene();
        }

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ScanScene();

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextScanTime) ScanScene();

            for (int i = trackedTexts.Count - 1; i >= 0; i--)
            {
                TextState state = trackedTexts[i];
                if (state.text == null || state.overlay == null || state.fallback == null)
                {
                    trackedTexts.RemoveAt(i);
                    continue;
                }

                bool compatible = CanProcess(state.text);
                Color current = state.text.color;
                if (current.a > 0.001f && current != state.visibleColor)
                    state.visibleColor = current;

                if (compatible)
                {
                    state.overlay.enabled = state.text.enabled;
                    state.overlay.SetVisibleColor(state.visibleColor);
                    SyncFallback(state);
                    state.fallback.enabled = state.text.enabled;
                    if (state.text.color.a > 0f)
                    {
                        Color hidden = state.visibleColor;
                        hidden.a = 0f;
                        state.text.color = hidden;
                    }
                }
                else
                {
                    state.overlay.enabled = false;
                    state.fallback.enabled = false;
                    if (state.text.color != state.visibleColor) state.text.color = state.visibleColor;
                }
            }
        }

        private void ScanScene()
        {
            nextScanTime = Time.unscaledTime + 0.5f;
            Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || text.GetComponent<StylizedBitmapFallbackMarker>() != null || IsTracked(text)) continue;
                trackedTexts.Add(CreateState(text));
            }
        }

        private static TextState CreateState(Text text)
        {
            GameObject fallbackObject = new GameObject("Original Glyph Fallback", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text), typeof(StylizedBitmapFallbackMarker));
            fallbackObject.transform.SetParent(text.transform, false);
            RectTransform fallbackRect = fallbackObject.GetComponent<RectTransform>();
            ConfigureOverlayRect(fallbackRect, text.rectTransform);
            Text fallback = fallbackObject.GetComponent<Text>();
            fallback.raycastTarget = false;

            GameObject overlayObject = new GameObject("Stylized Glyph Overlay", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(StylizedBitmapTextGraphic));
            overlayObject.transform.SetParent(text.transform, false);
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            ConfigureOverlayRect(rect, text.rectTransform);
            rect.SetAsLastSibling();

            Color visibleColor = text.color;
            StylizedBitmapTextGraphic overlay = overlayObject.GetComponent<StylizedBitmapTextGraphic>();
            overlay.Configure(text, visibleColor);
            TextState state = new TextState
            {
                text = text,
                visibleColor = visibleColor,
                overlay = overlay,
                fallback = fallback
            };
            SyncFallback(state);
            return state;
        }

        private static void ConfigureOverlayRect(RectTransform target, RectTransform source)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = source.pivot;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static bool CanProcess(Text text)
        {
            return text != null && StylizedBitmapFont.SupportsAllVisibleGlyphs(text.text, text.supportRichText);
        }

        private static void SyncFallback(TextState state)
        {
            Text source = state.text;
            Text fallback = state.fallback;
            fallback.font = source.font;
            fallback.fontSize = source.fontSize;
            fallback.fontStyle = source.fontStyle;
            fallback.lineSpacing = source.lineSpacing;
            fallback.supportRichText = true;
            fallback.alignment = source.alignment;
            fallback.alignByGeometry = source.alignByGeometry;
            fallback.horizontalOverflow = source.horizontalOverflow;
            fallback.verticalOverflow = source.verticalOverflow;
            fallback.resizeTextForBestFit = source.resizeTextForBestFit;
            fallback.resizeTextMinSize = source.resizeTextMinSize;
            fallback.resizeTextMaxSize = source.resizeTextMaxSize;
            if (fallback.color != state.visibleColor) fallback.color = state.visibleColor;
            if (state.fallbackSourceValue != source.text || state.fallbackSourceRichText != source.supportRichText)
            {
                state.fallbackSourceValue = source.text;
                state.fallbackSourceRichText = source.supportRichText;
                fallback.text = BuildFallbackText(source.text, source.supportRichText);
            }
        }

        private static string BuildFallbackText(string value, bool parseRichText)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length + 32);
            bool hiding = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (parseRichText && character == '<')
                {
                    int tagEnd = value.IndexOf('>', i + 1);
                    if (tagEnd >= 0)
                    {
                        if (hiding)
                        {
                            result.Append("</color>");
                            hiding = false;
                        }
                        result.Append(value, i, tagEnd - i + 1);
                        i = tagEnd;
                        continue;
                    }
                }
                bool supported = StylizedBitmapFont.TryGetUv(character, out _);
                if (supported && !hiding)
                {
                    result.Append("<color=#FFFFFF00>");
                    hiding = true;
                }
                else if (!supported && hiding)
                {
                    result.Append("</color>");
                    hiding = false;
                }
                result.Append(character);
            }
            if (hiding) result.Append("</color>");
            return result.ToString();
        }

        private bool IsTracked(Text target)
        {
            for (int i = 0; i < trackedTexts.Count; i++)
                if (trackedTexts[i].text == target) return true;
            return false;
        }
    }
}
