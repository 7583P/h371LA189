using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>Builds eight small reusable planet sprites once; no external package or per-frame allocation.</summary>
    public static class SpacePlanetSpriteFactory
    {
        private const int TextureSize = 256;
        private static readonly Sprite[] Sprites = new Sprite[8];

        public static Sprite GetSprite(SpacePlanetType type)
        {
            int index = Mathf.Clamp((int)type, 0, Sprites.Length - 1);
            if (Sprites[index] == null) Sprites[index] = BuildSprite((SpacePlanetType)index);
            return Sprites[index];
        }

        private static Sprite BuildSprite(SpacePlanetType type)
        {
            int bodyPixels = type == SpacePlanetType.Saturn ? 68 : 96;
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, false)
            {
                name = $"Runtime {type} Planet",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[TextureSize * TextureSize];
            Vector2 light = new Vector2(-.48f, .72f).normalized;
            for (int py = 0; py < TextureSize; py++)
            {
                for (int px = 0; px < TextureSize; px++)
                {
                    float x = (px + .5f - TextureSize * .5f) / bodyPixels;
                    float y = (py + .5f - TextureSize * .5f) / bodyPixels;
                    float radius = Mathf.Sqrt(x * x + y * y);
                    bool body = radius <= 1f;
                    bool ring = false;
                    float ringRadius = 0f;
                    if (type == SpacePlanetType.Saturn)
                    {
                        ringRadius = Mathf.Sqrt(x * x + y * y / (.255f * .255f));
                        ring = ringRadius >= 1.12f && ringRadius <= 1.68f;
                    }

                    Color color = Color.clear;
                    if (ring)
                    {
                        float stripe = .5f + .5f * Mathf.Sin(ringRadius * 54f);
                        color = Color.Lerp(new Color(.47f, .36f, .22f, .86f),
                            new Color(.91f, .79f, .52f, .94f), stripe);
                    }
                    if (body && !(type == SpacePlanetType.Saturn && ring && y < -.045f))
                    {
                        float z = Mathf.Sqrt(Mathf.Max(0f, 1f - radius * radius));
                        float lighting = Mathf.Clamp01(.24f + (x * light.x + y * light.y + z * .62f) * .66f);
                        float limb = Mathf.Lerp(.64f, 1f, Mathf.Pow(z, .42f));
                        color = SurfaceColor(type, x, y) * (lighting * limb);
                        color.a = Mathf.Clamp01((1f - radius) * bodyPixels * .9f);
                    }
                    else if (ring)
                    {
                        float edge = Mathf.Min(ringRadius - 1.12f, 1.68f - ringRadius);
                        color.a *= Mathf.Clamp01(edge * bodyPixels * .18f);
                    }
                    pixels[py * TextureSize + px] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(.5f, .5f), bodyPixels, 0, SpriteMeshType.FullRect);
            sprite.name = type.ToString();
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Color SurfaceColor(SpacePlanetType type, float x, float y)
        {
            float fine = ValueNoise(x * 5.1f + (int)type * 7.3f, y * 5.1f - (int)type * 3.7f);
            float broad = ValueNoise(x * 2.2f - (int)type * 4.1f, y * 2.2f + (int)type * 6.9f);
            float bands = .5f + .5f * Mathf.Sin(y * 28f + fine * 2.6f);
            switch (type)
            {
                case SpacePlanetType.Mercury:
                {
                    Color rock = Color.Lerp(new Color(.31f, .30f, .29f), new Color(.68f, .66f, .61f), fine);
                    float crater = Mathf.SmoothStep(.73f, .93f, broad) * .34f;
                    return rock * (1f - crater);
                }
                case SpacePlanetType.Venus:
                    return Color.Lerp(new Color(.66f, .35f, .12f), new Color(.98f, .76f, .35f),
                        Mathf.Clamp01(bands * .74f + broad * .26f));
                case SpacePlanetType.Earth:
                {
                    bool continent = broad + Mathf.Sin(x * 7f - y * 3f) * .13f > .54f;
                    Color baseColor = continent
                        ? Color.Lerp(new Color(.08f, .28f, .10f), new Color(.38f, .58f, .18f), fine)
                        : Color.Lerp(new Color(.015f, .12f, .42f), new Color(.04f, .38f, .72f), fine);
                    float clouds = Mathf.SmoothStep(.77f, .92f,
                        ValueNoise(x * 8.3f + 18f, y * 3.1f - 9f));
                    return Color.Lerp(baseColor, new Color(.91f, .95f, .96f), clouds * .72f);
                }
                case SpacePlanetType.Mars:
                {
                    Color dust = Color.Lerp(new Color(.38f, .09f, .035f), new Color(.82f, .31f, .12f), fine);
                    return Color.Lerp(dust, new Color(.17f, .055f, .03f), Mathf.SmoothStep(.68f, .9f, broad));
                }
                case SpacePlanetType.Jupiter:
                {
                    Color gas = Color.Lerp(new Color(.55f, .28f, .13f), new Color(.94f, .78f, .54f), bands);
                    float spot = Mathf.Clamp01(1f - new Vector2((x - .38f) / .31f, (y + .23f) / .15f).sqrMagnitude);
                    return Color.Lerp(gas, new Color(.68f, .16f, .075f), Smooth01(spot) * .86f);
                }
                case SpacePlanetType.Saturn:
                    return Color.Lerp(new Color(.66f, .51f, .27f), new Color(.96f, .86f, .59f),
                        bands * .76f + fine * .24f);
                case SpacePlanetType.Uranus:
                    return Color.Lerp(new Color(.24f, .66f, .71f), new Color(.68f, .91f, .88f),
                        bands * .26f + fine * .22f);
                case SpacePlanetType.Neptune:
                {
                    Color blue = Color.Lerp(new Color(.035f, .10f, .48f), new Color(.12f, .39f, .86f),
                        bands * .48f + fine * .32f);
                    float storm = Mathf.Clamp01(1f - new Vector2((x + .31f) / .27f, (y - .13f) / .12f).sqrMagnitude);
                    return Color.Lerp(blue, new Color(.035f, .035f, .18f), Smooth01(storm) * .76f);
                }
                default:
                    return Color.gray;
            }
        }

        private static float ValueNoise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float tx = Smooth01(x - ix);
            float ty = Smooth01(y - iy);
            float a = Hash(ix, iy);
            float b = Hash(ix + 1, iy);
            float c = Hash(ix, iy + 1);
            float d = Hash(ix + 1, iy + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                uint value = (uint)(x * 374761393 + y * 668265263);
                value = (value ^ (value >> 13)) * 1274126177u;
                return (value ^ (value >> 16)) / 4294967295f;
            }
        }

        private static float Smooth01(float value) => value * value * (3f - 2f * value);
    }
}
