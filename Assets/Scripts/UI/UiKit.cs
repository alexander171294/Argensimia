using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Argensimia.UI
{
    public static class Ease
    {
        public static float OutCubic(float t) { t = 1 - t; return 1 - t * t * t; }
        public static float InCubic(float t) => t * t * t;
        public static float InOutCubic(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        public static float OutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
    }

    // Botón táctil genérico: feedback de presión (escala) + callback de tap.
    public class Tap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action onTap;
        public Action<Vector2> onTapAt;
        public Transform visual;
        public float pressScale = 0.93f;
        public bool interactable = true;
        float cur = 1f, goal = 1f;

        public void OnPointerDown(PointerEventData e) { if (interactable) goal = pressScale; }
        public void OnPointerUp(PointerEventData e) { goal = 1f; }
        public void OnPointerExit(PointerEventData e) { goal = 1f; }
        public void OnPointerClick(PointerEventData e)
        {
            if (!interactable) return;
            onTap?.Invoke();
            onTapAt?.Invoke(e.position);
        }

        void Update()
        {
            if (!visual || Mathf.Abs(goal - cur) < 0.0005f) return;
            cur = Mathf.Lerp(cur, goal, 1f - Mathf.Exp(-30f * Time.unscaledDeltaTime));
            visual.localScale = Vector3.one * cur;
        }
    }

    public static class UiKit
    {
        public static readonly Color Navy = Hex("#13244a");
        public static readonly Color NavyDark = Hex("#0b1733");
        public static readonly Color Gold = Hex("#f2c14e");
        public static readonly Color GoldDark = Hex("#b98419");
        public static readonly Color Green = Hex("#7be36a");
        public static readonly Color Red = Hex("#d9342b");
        public static readonly Color RedSoft = Hex("#ff7a70");
        public static readonly Color Cream = Hex("#fbf1dc");

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        public static Color Tint(Color c, float k) => Color.Lerp(c, Color.white, k);

        // ---------- Fuentes ----------
        static TMP_FontAsset font, fontBody;
        static Material outlineMat;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = Resources.Load<TMP_FontAsset>("UI/Roboto-Black SDF");
                if (font == null) font = TMP_Settings.defaultFontAsset;
                return font;
            }
        }

        public static TMP_FontAsset FontBody
        {
            get
            {
                if (fontBody == null) fontBody = Resources.Load<TMP_FontAsset>("UI/Roboto-Bold SDF");
                if (fontBody == null) fontBody = Font;
                return fontBody;
            }
        }

        // Contorno oscuro + sombra: el look "juego mobile" de la referencia.
        public static Material OutlineMat
        {
            get
            {
                if (outlineMat == null)
                {
                    outlineMat = new Material(Font.material) { name = "TMP Outline (runtime)" };
                    outlineMat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                    outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.16f);
                    outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.04f, 0.07f, 0.16f, 1));
                    outlineMat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                    outlineMat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.55f));
                    outlineMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
                    outlineMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
                }
                return outlineMat;
            }
        }

        // ---------- Sprites procedurales (sin assets) ----------
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Sprite RR => Get("rr", () => Paint(72, 72, (x, y) => Cov(SdRoundBox(new Vector2(x - 36, y - 36), new Vector2(35, 35), 32)), 34));
        public static Sprite Circle => Get("circle", () => Paint(128, 128, (x, y) => Cov(new Vector2(x - 64, y - 64).magnitude - 62), 0));
        public static Sprite Shadow => Get("shadow", () => Paint(128, 128, (x, y) =>
        {
            float d = SdRoundBox(new Vector2(x - 64, y - 64), new Vector2(44, 44), 24);
            float a = 1f - Mathf.InverseLerp(-16, 18, d);
            return a * a;
        }, 62));

        public static Sprite People => Get("people", () => Paint(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float mid = Mathf.Min(SdCircle(p, new Vector2(64, 84), 19), Mathf.Max(SdCircle(p, new Vector2(64, 26), 36), 14 - y));
            float side = Mathf.Min(Mathf.Min(SdCircle(p, new Vector2(28, 78), 14), SdCircle(p, new Vector2(100, 78), 14)),
                Mathf.Max(Mathf.Min(SdCircle(p, new Vector2(28, 30), 27), SdCircle(p, new Vector2(100, 30), 27)), 14 - y));
            return Mathf.Max(Cov(mid), Cov(side) * (1 - Cov(mid - 7)));
        }, 0));

        public static Sprite Building => Get("building", () => Paint(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float d = SdBox(p, new Vector2(64, 16), new Vector2(56, 8));
            d = Mathf.Min(d, SdBox(p, new Vector2(64, 80), new Vector2(56, 6)));
            for (int i = 0; i < 5; i++) d = Mathf.Min(d, SdBox(p, new Vector2(22 + i * 21, 48), new Vector2(6, 23)));
            d = Mathf.Min(d, SdTri(p, new Vector2(8, 91), new Vector2(120, 91), new Vector2(64, 122)));
            return Cov(d);
        }, 0));

        public static Sprite Refresh => Get("refresh", () => Paint(128, 128, (x, y) =>
        {
            var p = new Vector2(x - 64, y - 64);
            float ring = Mathf.Abs(p.magnitude - 40) - 9;
            float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            float gap = Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(ang, 62)), Mathf.Abs(Mathf.DeltaAngle(ang, -118)));
            if (gap < 30) ring = Mathf.Max(ring, (30 - gap) * 0.8f);
            float d = Mathf.Min(ring, Mathf.Min(Arrow(p, 32), Arrow(p, 212)));
            return Cov(d);
        }, 0));

        static float Arrow(Vector2 p, float aDeg)
        {
            float a = aDeg * Mathf.Deg2Rad;
            var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var t = new Vector2(-n.y, n.x);
            var c = n * 40;
            return SdTri(p, c + n * 22, c - n * 22, c + t * 26);
        }

        static float Cov(float d) => Mathf.Clamp01(0.5f - d);

        static Sprite Get(string k, Func<Sprite> make)
        {
            if (!sprites.TryGetValue(k, out var s) || s == null) { s = make(); sprites[k] = s; }
            return s;
        }

        static Sprite Paint(int w, int h, Func<float, float, float> alpha, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "ui_proc" };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f)) * 255));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static float SdRoundBox(Vector2 p, Vector2 b, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }
        static float SdCircle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float SdBox(Vector2 p, Vector2 c, Vector2 half) => SdRoundBox(p - c, half, 0.01f);
        static float SdTri(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            Vector2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
            Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(Vector2.Min(
                new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        // ---------- Construcción ----------
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform RT(this Component c) => (RectTransform)c.transform;

        public static RectTransform At(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite s, Color c, float radius = 0, bool raycast = false)
        {
            var im = Rect(name, parent).gameObject.AddComponent<Image>();
            im.sprite = s; im.color = c; im.raycastTarget = raycast;
            if (s != null && s.border.sqrMagnitude > 0)
            {
                im.type = Image.Type.Sliced;
                if (radius > 0) im.pixelsPerUnitMultiplier = 32f / radius;
            }
            return im;
        }

        public static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false, TMP_FontAsset f = null)
        {
            var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = f ? f : Font;
            if (outline) t.fontSharedMaterial = OutlineMat;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        // Panel azul marino con borde dorado (estilo referencia).
        public static RectTransform Panel(Transform parent, string name, float radius = 18, bool raycast = false)
        {
            var border = Img(parent, name, RR, Gold, radius, raycast);
            Img(border.transform, "Inner", RR, Navy, radius - 3).RT().Fill(3, 3, 3, 3);
            var gloss = Img(border.transform, "Gloss", RR, new Color(1, 1, 1, 0.07f), radius - 3).RT();
            gloss.anchorMin = new Vector2(0, 0.5f); gloss.anchorMax = Vector2.one;
            gloss.offsetMin = new Vector2(3, 0); gloss.offsetMax = new Vector2(-3, -3);
            return border.RT();
        }

        public static RectTransform Coin(Transform parent, float size)
        {
            var o = Img(parent, "Coin", Circle, GoldDark).RT();
            o.sizeDelta = new Vector2(size, size);
            Img(o, "In", Circle, Gold).RT().Fill(size * 0.09f, size * 0.09f, size * 0.09f, size * 0.09f);
            Txt(o, "S", "$", size * 0.62f, Hex("#8a5306")).RT().Fill();
            return o;
        }

        public static IEnumerator Tween(float dur, Action<float> step)
        {
            float t = 0;
            step(0);
            while (t < dur)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / dur));
            }
        }

        public static float SignedAngle(float a) => Mathf.DeltaAngle(0, a);
    }
}
