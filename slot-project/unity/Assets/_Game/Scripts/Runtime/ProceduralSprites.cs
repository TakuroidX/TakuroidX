using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Generates placeholder sprites in code (no image assets needed).
    /// Everything is white; tint it with Image.color.
    /// </summary>
    public static class ProceduralSprites
    {
        private const int Size = 128;
        private const int Radius = 28;

        private static Sprite _rounded, _outline, _star, _diamond, _circle, _white;

        /// <summary>Rounded rect (9-slice)</summary>
        public static Sprite RoundedRect => _rounded ? _rounded : (_rounded = Sliced("rounded", RoundedAlpha(false)));

        /// <summary>Rounded rect outline (9-slice)</summary>
        public static Sprite RoundedOutline => _outline ? _outline : (_outline = Sliced("outline", RoundedAlpha(true)));

        public static Sprite Star => _star ? _star : (_star = Simple("star", Supersample(StarInside)));

        public static Sprite Diamond => _diamond ? _diamond : (_diamond = Simple("diamond", Supersample(DiamondInside)));

        public static Sprite Circle => _circle ? _circle : (_circle = Simple("circle", Supersample((x, y) => x * x + y * y <= 0.92f * 0.92f)));

        public static Sprite White => _white ? _white : (_white = Simple("white", Fill(1f)));

        private static float[] Fill(float a)
        {
            var px = new float[Size * Size];
            for (var i = 0; i < px.Length; i++) px[i] = a;
            return px;
        }

        private static float[] RoundedAlpha(bool outlineOnly)
        {
            const float thickness = 7f;
            var px = new float[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    // Signed distance of a rounded rect (negative inside)
                    var qx = Mathf.Abs(x + 0.5f - Size / 2f) - (Size / 2f - Radius);
                    var qy = Mathf.Abs(y + 0.5f - Size / 2f) - (Size / 2f - Radius);
                    var outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
                    var d = outside + Mathf.Min(Mathf.Max(qx, qy), 0) - Radius;
                    var a = Mathf.Clamp01(0.5f - d);
                    if (outlineOnly) a *= Mathf.Clamp01(d + thickness + 0.5f);
                    px[y * Size + x] = a;
                }
            }
            return px;
        }

        /// <summary>Anti-aliases a shape given by an inside test with 4x4 supersampling. Coordinates are -1..1.</summary>
        private static float[] Supersample(Func<float, float, bool> inside)
        {
            const int ss = 4;
            var px = new float[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var hits = 0;
                    for (var sy = 0; sy < ss; sy++)
                        for (var sx = 0; sx < ss; sx++)
                        {
                            var u = (x + (sx + 0.5f) / ss) / Size * 2f - 1f;
                            var v = (y + (sy + 0.5f) / ss) / Size * 2f - 1f;
                            if (inside(u, v)) hits++;
                        }
                    px[y * Size + x] = hits / (float)(ss * ss);
                }
            }
            return px;
        }

        private static bool DiamondInside(float x, float y) => Mathf.Abs(x) / 0.78f + Mathf.Abs(y) / 0.95f <= 1f;

        private static readonly Vector2[] StarPoly = BuildStar();

        private static Vector2[] BuildStar()
        {
            var pts = new Vector2[10];
            for (var i = 0; i < 10; i++)
            {
                var r = i % 2 == 0 ? 0.95f : 0.40f;
                var ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                pts[i] = new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r - 0.05f);
            }
            return pts;
        }

        private static bool StarInside(float x, float y)
        {
            // Even-odd rule point-in-polygon test
            var inside = false;
            for (int i = 0, j = StarPoly.Length - 1; i < StarPoly.Length; j = i++)
            {
                var a = StarPoly[i];
                var b = StarPoly[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        private static Texture2D ToTexture(string name, float[] alpha)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "proc_" + name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var colors = new Color32[alpha.Length];
            for (var i = 0; i < alpha.Length; i++) colors[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha[i] * 255));
            tex.SetPixels32(colors);
            tex.Apply(false, true);
            return tex;
        }

        private static Sprite Simple(string name, float[] alpha)
        {
            var s = Sprite.Create(ToTexture(name, alpha), new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            s.name = name;
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        private static Sprite Sliced(string name, float[] alpha)
        {
            var border = new Vector4(Radius + 2, Radius + 2, Radius + 2, Radius + 2);
            var s = Sprite.Create(ToTexture(name, alpha), new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            s.name = name;
            s.hideFlags = HideFlags.DontSave;
            return s;
        }
    }
}
