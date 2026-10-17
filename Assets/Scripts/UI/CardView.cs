using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Carta: se diseña a 360x500 y se escala (mano ~0,43, zoom ~1,34). El texto es SDF, así que escala nítido.
    public class CardView : MonoBehaviour
    {
        public const float W = 360, H = 500;

        public CardDef def;
        public int level = 1;
        public int slot = -1;
        public Tap tap;
        public CanvasGroup group;
        public RectTransform body, state;
        public bool Disabled { get; private set; }
        public bool dimWhenPoor = true;

        GameObject front, back;
        Image dim;
        CanvasGroup disabledFx;
        float disabledK;
        TextMeshProUGUI cost;
        UIPerspective persp;
        bool lastAfford = true;

        public RectTransform Rt => (RectTransform)transform;

        public static CardView Create(Transform parent, CardDef def, int level = 1)
        {
            var rt = UiKit.Rect(def ? "Card_" + def.id : "CardBack", parent);
            rt.sizeDelta = new Vector2(W, H);
            var cv = rt.gameObject.AddComponent<CardView>();
            cv.def = def;
            cv.level = level;
            cv.Build();
            return cv;
        }

        void Build()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            state = UiKit.Rect("State", transform).Fill();
            var sh = UiKit.Img(state, "Shadow", UiKit.Shadow, new Color(0, 0, 0, 0.55f));
            sh.pixelsPerUnitMultiplier = 1;
            sh.RT().Fill(-26, -38, -14, -52);

            body = UiKit.Rect("Body", state).Fill();
            tap = gameObject.AddComponent<Tap>();
            tap.visual = body;
            tap.pressScale = 0.95f;

            if (def) front = BuildFront();
            back = BuildBack();
            if (def) { back.transform.localRotation = Quaternion.Euler(0, 180, 0); back.SetActive(false); }

            persp = gameObject.AddComponent<UIPerspective>();
            persp.Refresh();
        }

        GameObject BuildFront()
        {
            var c = def.color;
            var f = UiKit.Rect("Front", body).Fill();
            UiKit.Img(f, "Frame", UiKit.RR, UiKit.Shade(c, 0.45f), 26, true).RT().Fill();
            UiKit.Img(f, "Face", UiKit.RR, c, 21).RT().Fill(7, 7, 7, 7);
            var gloss = UiKit.Img(f, "Gloss", UiKit.RR, new Color(1, 1, 1, 0.13f), 21).RT();
            gloss.anchorMin = new Vector2(0, 0.55f); gloss.anchorMax = Vector2.one;
            gloss.offsetMin = new Vector2(7, 0); gloss.offsetMax = new Vector2(-7, -7);

            var title = UiKit.Txt(f, "Title", def.title.ToUpperInvariant(), 40, Color.white, TextAlignmentOptions.Center, true);
            title.RT().At(new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(W - 36, 58), new Vector2(0.5f, 1));
            title.enableAutoSizing = true; title.fontSizeMin = 22; title.fontSizeMax = 40;

            // Arte (o placeholder hasta que se sumen las imágenes)
            var artFrame = UiKit.Img(f, "ArtFrame", UiKit.RR, UiKit.Shade(c, 0.35f), 16);
            artFrame.RT().At(new Vector2(0.5f, 1), new Vector2(0, -72), new Vector2(W - 32, 226), new Vector2(0.5f, 1));
            var art = UiKit.Img(artFrame.transform, "Art", UiKit.RR, UiKit.Tint(c, 0.3f), 13);
            art.RT().Fill(4, 4, 4, 4);
            art.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (def.art)
            {
                var a = UiKit.Img(art.transform, "Image", def.art, Color.white);
                var fit = a.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = def.art.rect.width / def.art.rect.height;
            }
            else
            {
                UiKit.Txt(art.transform, "Placeholder", def.title.Substring(0, 1).ToUpperInvariant(), 170, new Color(1, 1, 1, 0.45f)).RT().Fill();
                UiKit.Txt(art.transform, "Hint", "ARTE PENDIENTE", 16, new Color(1, 1, 1, 0.6f))
                    .RT().At(new Vector2(0.5f, 0), new Vector2(0, def.kind == CardKind.Colocar ? 66 : 10), new Vector2(220, 22), new Vector2(0.5f, 0));
            }
            var chip = UiKit.Img(artFrame.transform, "Kind", UiKit.RR, new Color(0, 0, 0, 0.55f), 12);
            chip.RT().At(new Vector2(0, 1), new Vector2(12, -12), new Vector2(def.kind == CardKind.Colocar ? 122 : 136, 32), new Vector2(0, 1));
            UiKit.Txt(chip.transform, "T", Names.Kind(def.kind), 17, def.kind == CardKind.Crisis ? UiKit.RedSoft : Color.white).RT().Fill();
            if (def.kind != CardKind.Crisis)
            {
                var rar = UiKit.Img(artFrame.transform, "Rarity", UiKit.RR, Names.RarityColor(def.rarity), 10);
                rar.RT().At(new Vector2(1, 1), new Vector2(-12, -12), new Vector2(def.rarity == Rarity.PocoComun ? 132 : 92, 32), new Vector2(1, 1));
                UiKit.Txt(rar.transform, "T", Names.Rarity(def.rarity), 15, UiKit.Hex("#1c1c24")).RT().Fill();
            }
            if (level > 1)
            {
                var lv = UiKit.Img(artFrame.transform, "Level", UiKit.RR, UiKit.Gold, 12);
                lv.RT().At(new Vector2(1, 1), new Vector2(-12, -50), new Vector2(90, 34), new Vector2(1, 1));
                UiKit.Txt(lv.transform, "T", "NV " + level, 22, UiKit.Hex("#5a3a06")).RT().Fill();
            }

            if (def.kind == CardKind.Colocar) StatBadges(artFrame.transform);

            // Descripción: ilegible en la mano a propósito, se lee al hacer zoom.
            var descBg = UiKit.Img(f, "DescBg", UiKit.RR, UiKit.Cream, 14).RT();
            descBg.anchorMin = Vector2.zero; descBg.anchorMax = Vector2.one;
            descBg.offsetMin = new Vector2(16, 86); descBg.offsetMax = new Vector2(-16, -306);
            var summary = def.EffectSummary(level);
            var desc = UiKit.Txt(descBg, "Desc", string.IsNullOrEmpty(summary) ? def.description : summary + "\n" + def.description,
                21, UiKit.Hex("#3a2a1a"), TextAlignmentOptions.Center, false, UiKit.FontBody);
            desc.RT().Fill(12, 12, 6, 6);
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.enableAutoSizing = true; desc.fontSizeMin = 13; desc.fontSizeMax = 21;

            dim = UiKit.Img(f, "Dim", UiKit.RR, new Color(0.05f, 0.05f, 0.1f, 0.5f), 26);
            dim.RT().Fill(); dim.enabled = false;

            // Deshabilitada (sin slots libres): gris oscuro + etiqueta
            var dis = UiKit.Img(f, "Disabled", UiKit.RR, new Color(0.22f, 0.24f, 0.29f, 0.72f), 26);
            dis.RT().Fill();
            disabledFx = dis.gameObject.AddComponent<CanvasGroup>();
            disabledFx.alpha = 0;
            var tag = UiKit.Img(dis.transform, "Tag", UiKit.RR, new Color(0, 0, 0, 0.75f), 14);
            tag.RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(250, 60));
            UiKit.Txt(tag.transform, "T", "SIN LUGAR", 34, Color.white, TextAlignmentOptions.Center, true).RT().Fill();

            // Costo
            var bar = UiKit.Img(f, "CostBar", UiKit.RR, UiKit.Shade(c, 0.5f), 16).RT();
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(0.5f, 0);
            bar.offsetMin = new Vector2(16, 14); bar.offsetMax = new Vector2(-16, 76);
            var coin = UiKit.Coin(bar, 44);
            coin.At(new Vector2(0.5f, 0.5f), new Vector2(-68, 0), new Vector2(44, 44));
            cost = UiKit.Txt(bar, "Cost", def.kind == CardKind.Crisis ? "$" + GameState.Money(def.cost) : def.cost <= 0 ? "GRATIS" : "$" + GameState.Money(def.cost), 38, Color.white, TextAlignmentOptions.Left, true);
            cost.RT().At(new Vector2(0.5f, 0.5f), new Vector2(-38, 0), new Vector2(200, 56), new Vector2(0, 0.5f));
            return f.gameObject;
        }

        // Daño, ritmo, alcance y vida como badges en el borde inferior del arte.
        void StatBadges(Transform parent)
        {
            var list = def.StatList(level);
            if (list.Count == 0) return;
            const float bw = 74, bh = 52, gap = 6;
            float total = list.Count * bw + (list.Count - 1) * gap;
            for (int i = 0; i < list.Count; i++)
            {
                var b = UiKit.Img(parent, "Stat_" + list[i].label, UiKit.RR, new Color(0.04f, 0.05f, 0.1f, 0.72f), 10);
                b.raycastTarget = false;
                b.RT().At(new Vector2(0.5f, 0), new Vector2(-total / 2 + bw / 2 + i * (bw + gap), 10), new Vector2(bw, bh), new Vector2(0.5f, 0));
                var v = UiKit.Txt(b.transform, "V", list[i].value, 24, Color.white, TextAlignmentOptions.Center, true);
                v.RT().At(new Vector2(0.5f, 1), new Vector2(0, -3), new Vector2(bw - 6, 30), new Vector2(0.5f, 1));
                v.enableAutoSizing = true; v.fontSizeMin = 13; v.fontSizeMax = 24;
                var lb = UiKit.Txt(b.transform, "L", list[i].label, 12, UiKit.Gold, TextAlignmentOptions.Center, true);
                lb.RT().At(new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(bw - 4, 16), new Vector2(0.5f, 0));
                lb.enableAutoSizing = true; lb.fontSizeMin = 9; lb.fontSizeMax = 12;
            }
        }

        GameObject BuildBack()
        {
            var b = UiKit.Rect("Back", body).Fill();
            UiKit.Img(b, "Frame", UiKit.RR, UiKit.Gold, 26, true).RT().Fill();
            UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex("#5d9fd8"), 21).RT().Fill(8, 8, 8, 8);
            var band = UiKit.Img(b, "White", null, UiKit.Hex("#f4f6fa")).RT();
            band.anchorMin = new Vector2(0, 0.36f); band.anchorMax = new Vector2(1, 0.64f);
            band.offsetMin = new Vector2(8, 0); band.offsetMax = new Vector2(-8, 0);
            var sun = UiKit.Img(b, "Sun", UiKit.Circle, UiKit.Hex("#c98a12"));
            sun.RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116, 116));
            UiKit.Img(sun.transform, "In", UiKit.Circle, UiKit.Hex("#f6b40e")).RT().Fill(9, 9, 9, 9);
            var gloss = UiKit.Img(b, "Gloss", UiKit.RR, new Color(1, 1, 1, 0.12f), 21).RT();
            gloss.anchorMin = new Vector2(0, 0.55f); gloss.anchorMax = Vector2.one;
            gloss.offsetMin = new Vector2(8, 0); gloss.offsetMax = new Vector2(-8, -8);
            UiKit.Txt(b, "Logo", "ARGENSIMIA", 34, Color.white, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(320, 44), new Vector2(0.5f, 0));
            return b.gameObject;
        }

        public void RefreshAfford() { lastAfford = !lastAfford; }

        // Sin slots: más chica, más baja y oscurecida (queda "detrás" de las demás).
        public void SetDisabled(bool on)
        {
            if (on == Disabled) return;
            Disabled = on;
            if (on) transform.SetAsFirstSibling();
        }

        void LateUpdate()
        {
            if (!def) return;
            float target = Disabled ? 1f : 0f;
            if (Mathf.Abs(disabledK - target) > 0.001f)
            {
                disabledK = Mathf.MoveTowards(disabledK, target, Time.unscaledDeltaTime * 5f);
                float e = Ease.InOutCubic(disabledK);
                state.localScale = Vector3.one * Mathf.Lerp(1f, 0.84f, e);
                state.anchoredPosition = new Vector2(0, Mathf.Lerp(0, -46f, e));
                disabledFx.alpha = e;
            }
            bool showFront = (transform.rotation * Vector3.forward).z > 0f;
            if (front.activeSelf != showFront)
            {
                front.SetActive(showFront);
                back.SetActive(!showFront);
                persp.MarkDirty();
            }
            if (GameState.I)
            {
                bool ok = !dimWhenPoor || GameState.I.CanAfford(def.cost);
                if (ok != lastAfford)
                {
                    lastAfford = ok;
                    dim.enabled = !ok;
                    cost.color = ok ? Color.white : UiKit.RedSoft;
                }
            }
        }
    }

    // Perspectiva falsa para un Canvas overlay (que es ortográfico): escala cada vértice según su profundidad
    // tras la rotación del root, como si hubiera una cámara a `distance` unidades. Así el flip/inclinación se ve 3D.
    [DisallowMultipleComponent]
    public class UIPerspective : MonoBehaviour
    {
        public float distance = 1100f;
        readonly List<Graphic> graphics = new List<Graphic>();
        readonly List<TMP_Text> texts = new List<TMP_Text>();
        Quaternion last = Quaternion.identity;
        bool dirty;
        public bool IsFlat { get; private set; } = true;

        public void Refresh()
        {
            foreach (var t in texts) if (t) t.OnPreRenderText -= OnText;
            graphics.Clear(); texts.Clear();
            foreach (var g in GetComponentsInChildren<Graphic>(true))
            {
                if (g is TMP_SubMeshUI) continue;
                if (g is TMP_Text t) { texts.Add(t); t.OnPreRenderText += OnText; continue; }
                if (!g.TryGetComponent<UIPerspectiveMesh>(out var m)) m = g.gameObject.AddComponent<UIPerspectiveMesh>();
                m.owner = this;
                graphics.Add(g);
            }
            dirty = true;
        }

        public void MarkDirty() => dirty = true;

        void OnDestroy() { foreach (var t in texts) if (t) t.OnPreRenderText -= OnText; }

        void LateUpdate()
        {
            var r = transform.rotation;
            if (!dirty && Quaternion.Angle(r, last) < 0.05f) return;
            dirty = false;
            last = r;
            IsFlat = Mathf.Abs((r * Vector3.forward).z - 1f) < 1e-5f; // sin inclinación fuera del plano
            foreach (var g in graphics) if (g && g.isActiveAndEnabled) g.SetVerticesDirty();
            foreach (var t in texts) if (t && t.isActiveAndEnabled) t.ForceMeshUpdate(true);
        }

        void OnText(TMP_TextInfo info)
        {
            if (IsFlat || info == null || info.textComponent == null) return;
            var mi = info.meshInfo;
            for (int i = 0; i < mi.Length; i++)
                if (mi[i].vertices != null) Warp(info.textComponent.transform, mi[i].vertices, mi[i].vertexCount);
        }

        public void Warp(Transform t, Vector3[] v, int count)
        {
            var toRoot = transform.worldToLocalMatrix * t.localToWorldMatrix;
            var back = toRoot.inverse;
            var rot = transform.rotation;
            for (int i = 0; i < count; i++)
            {
                var p = toRoot.MultiplyPoint3x4(v[i]);
                float z = (rot * p).z;
                float f = distance / Mathf.Max(distance * 0.25f, distance + z);
                p.x *= f; p.y *= f;
                v[i] = back.MultiplyPoint3x4(p);
            }
        }
    }

    public class UIPerspectiveMesh : BaseMeshEffect
    {
        public UIPerspective owner;
        static Vector3[] buf = new Vector3[64];

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || owner == null || owner.IsFlat) return;
            int n = vh.currentVertCount;
            if (buf.Length < n) buf = new Vector3[n * 2];
            var v = new UIVertex();
            for (int i = 0; i < n; i++) { vh.PopulateUIVertex(ref v, i); buf[i] = v.position; }
            owner.Warp(transform, buf, n);
            for (int i = 0; i < n; i++) { vh.PopulateUIVertex(ref v, i); v.position = buf[i]; vh.SetUIVertex(v, i); }
        }
    }
}
