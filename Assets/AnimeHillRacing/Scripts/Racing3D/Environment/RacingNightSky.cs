using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Baked once when Night City loads. One opaque cubemap lookup per pixel;
    // no sky Update, particles, lights, transparent cards or procedural fragment noise.
    public static class RacingNightSky
    {
        public const int FaceSize = 256;
        public const int StarsPerFace = 900;

        public static Material Create(RacingEnvironmentScope scope)
        {
            var texture = new Cubemap(FaceSize, TextureFormat.RGB24, true)
            {
                name = "Night City / static dense stars / 256px faces",
                filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[FaceSize * FaceSize];
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < FaceSize; y++) for (int x = 0; x < FaceSize; x++)
                {
                    Vector3 d = Direction(face, (x + .5f) * 2 / FaceSize - 1, (y + .5f) * 2 / FaceSize - 1).normalized;
                    float height = Mathf.Abs(d.y);
                    Color sky = Color.Lerp(new Color(.035f, .075f, .14f),
                        new Color(.002f, .004f, .012f), Mathf.SmoothStep(0, 1, height / .62f));
                    // Faint tilted galactic band, baked once: violet core with a blue rim.
                    float band = Mathf.Exp(-Mathf.Pow(Vector3.Dot(d, BandNormal) / .16f, 2)) * Mathf.Clamp01(d.y * 3 + .2f);
                    pixels[y * FaceSize + x] = sky + new Color(.030f, .020f, .060f) * band + new Color(.006f, .014f, .030f) * band * band;
                }
                var rng = new DeterministicRng(91873 + face * 193);
                for (int star = 0; star < StarsPerFace; star++)
                {
                    int x = rng.Next(2, FaceSize - 2), y = rng.Next(2, FaceSize - 2);
                    Vector3 d = Direction(face, (x + .5f) * 2 / FaceSize - 1, (y + .5f) * 2 / FaceSize - 1).normalized;
                    if (d.y < -.04f) continue;
                    float brightness = rng.Range(.55f, 1f) * Mathf.Lerp(.6f, 1f, Mathf.Clamp01(d.y * 5));
                    Color color = Color.Lerp(new Color(.61f, .78f, 1f), new Color(1f, .91f, .76f), rng.NextFloat()) * brightness;
                    pixels[y * FaceSize + x] = color;
                    if (star % 5 != 0) continue;
                    pixels[y * FaceSize + x - 1] += color * .18f;
                    pixels[y * FaceSize + x + 1] += color * .18f;
                    pixels[(y - 1) * FaceSize + x] += color * .18f;
                    pixels[(y + 1) * FaceSize + x] += color * .18f;
                }
                texture.SetPixels(pixels, (CubemapFace)face);
            }
            texture.Apply(true, true);
            var material = new Material(Resources.Load<Shader>("RacingSkyTrackSky")) { name = "Night City / dark starry sky" };
            // Retain the existing shader's texture slot so other skies stay compatible.
            material.SetTexture("_SunsetSky", texture);
            scope?.Own(texture);scope?.Own(material);
            return material;
        }

        static readonly Vector3 BandNormal = new Vector3(.55f, .45f, -.70f).normalized;

        static Vector3 Direction(int face, float u, float v)
        {
            switch (face)
            {
                case 0: return new Vector3(1, -v, -u);
                case 1: return new Vector3(-1, -v, u);
                case 2: return new Vector3(u, 1, v);
                case 3: return new Vector3(u, -1, -v);
                case 4: return new Vector3(u, -v, 1);
                default: return new Vector3(-u, -v, -1);
            }
        }
    }
}
