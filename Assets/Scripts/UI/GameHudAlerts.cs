using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Pérdidas en rojo (HUD y sobre el mapa), crisis forzadas resaltadas en la mano y aviso antes de arrancar.
    public partial class GameHud
    {
        float pendingPop, popFlushT;
        RectTransform crisisWarn;
        TextMeshProUGUI crisisWarnTxt;
        readonly Dictionary<CardView, Image> crisisGlow = new Dictionary<CardView, Image>();

        // ------------------------------------------------------------------ números rojos

        void OnLost(string kind, float amount, Vector3? at)
        {
            if (amount <= 0) return;
            switch (kind)
            {
                case "pop":
                    pendingPop += amount;
                    if (at.HasValue) WorldFloat(at.Value, "-" + Pct(amount) + "%", UiKit.RedSoft);
                    break;
                case "gov":
                    if (at.HasValue) WorldFloat(at.Value, "-" + Mathf.RoundToInt(amount) + " GOB", UiKit.RedSoft);
                    break;
                case "money":
                    FloatText(moneyPanel, "-$" + GameState.Money(amount), UiKit.RedSoft, 0.1f);
                    break;
            }
        }

        static string Pct(float v) => (v < 1 ? v.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture) : v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)).Replace('.', ',');

        // Las pérdidas chicas de popularidad (jubilados) se agrupan para no llenar la pantalla.
        void FlushLoss()
        {
            popFlushT -= Time.unscaledDeltaTime;
            if (pendingPop <= 0 || popFlushT > 0) return;
            FloatText(popPanel, "-" + Pct(pendingPop) + "%", UiKit.RedSoft, 0);
            if (pendingPop >= 1) StartCoroutine(Shake(popPanel));
            pendingPop = 0;
            popFlushT = 0.6f;
        }

        void WorldFloat(Vector3 world, string s, Color c)
        {
            var cam = Camera.main;
            if (!cam || !fxLayer) return;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(fxLayer, sp, null, out var local)) return;
            StartCoroutine(WorldFloatRoutine(local, s, c));
        }

        IEnumerator WorldFloatRoutine(Vector2 p0, string s, Color c)
        {
            var t = UiKit.Txt(fxLayer, "WFloat", s, 30, c, TextAlignmentOptions.Center, true);
            t.RT().sizeDelta = new Vector2(200, 40);
            yield return UiKit.Tween(1.1f, k =>
            {
                t.transform.localPosition = (Vector3)p0 + Vector3.up * Ease.OutCubic(k) * 46;
                t.transform.localScale = Vector3.one * (k < 0.12f ? Mathf.Lerp(0.5f, 1.15f, k / 0.12f) : Mathf.Lerp(1.15f, 1f, (k - 0.12f) * 3));
                t.alpha = 1 - Mathf.Clamp01((k - 0.55f) / 0.45f);
            });
            Destroy(t.gameObject);
        }

        // ------------------------------------------------------------------ crisis forzadas

        List<CardView> CrisesInHand()
        {
            var l = new List<CardView>();
            if (cards == null) return l;
            foreach (var cv in cards) if (cv && cv.def && cv.def.kind == CardKind.Crisis) l.Add(cv);
            return l;
        }

        readonly Dictionary<CardView, Image> upgradeGlow = new Dictionary<CardView, Image>();

        void UpdateAlerts()
        {
            FlushLoss();
            var crises = CrisesInHand();
            float w = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);

            // halo rojo pulsante detrás de cada crisis de la mano
            foreach (var cv in crises)
            {
                if (!crisisGlow.TryGetValue(cv, out var g) || !g)
                {
                    g = UiKit.Img(cv.Rt, "CrisisGlow", UiKit.RR, UiKit.Red, 34);
                    g.raycastTarget = false;
                    g.RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardView.W + 56, CardView.H + 56));
                    g.transform.SetAsFirstSibling();
                    var tag = UiKit.Img(g.transform, "Tag", UiKit.RR, UiKit.Hex("#8a1414"), 18);
                    tag.raycastTarget = false;
                    tag.RT().At(new Vector2(0.5f, 1), new Vector2(0, 46), new Vector2(CardView.W + 40, 76), new Vector2(0.5f, 0));
                    UiKit.Txt(tag.transform, "T", "CRISIS: SE APLICA\nAL ARRANCAR", 30, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
                    crisisGlow[cv] = g;
                }
                var col = UiKit.Red; col.a = Mathf.Lerp(0.35f, 0.95f, w);
                g.color = col;
            }
            var dead = new List<CardView>();
            foreach (var kv in crisisGlow) if (!kv.Key || !crises.Contains(kv.Key)) { if (kv.Value) Destroy(kv.Value.gameObject); dead.Add(kv.Key); }
            foreach (var k in dead) crisisGlow.Remove(k);

            // halo dorado + cartel en las colocables que pueden mejorar una defensa igual del mapa
            var ups = new List<CardView>();
            if (gs.phase == Phase.Previa || gs.phase == Phase.Oleada)
                foreach (var cv in cards)
                    if (cv && cv.def.kind == CardKind.Colocar && Defense.AnyUpgradable(cv.def)) ups.Add(cv);
            float wf = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f), 1.5f);
            foreach (var cv in ups)
            {
                if (!upgradeGlow.TryGetValue(cv, out var g) || !g)
                {
                    g = UiKit.Img(cv.Rt, "UpgradeGlow", UiKit.RR, UiKit.Gold, 34);
                    g.raycastTarget = false;
                    g.RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardView.W + 48, CardView.H + 48));
                    g.transform.SetAsFirstSibling();
                    var tag = UiKit.Img(g.transform, "Tag", UiKit.RR, UiKit.Hex("#8a5a06"), 18);
                    tag.raycastTarget = false;
                    tag.RT().At(new Vector2(0.5f, 1), new Vector2(0, 42), new Vector2(CardView.W + 30, 76), new Vector2(0.5f, 0));
                    UiKit.Txt(tag.transform, "T", "¡SUBILE EL NIVEL\nA UNA DEL MAPA!", 30, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
                    upgradeGlow[cv] = g;
                }
                var col = UiKit.Gold; col.a = Mathf.Lerp(0.2f, 1f, wf);
                g.color = col;
                var tg = g.transform.Find("Tag");
                if (tg && tg.gameObject.activeSelf != (cv != zoomed)) tg.gameObject.SetActive(cv != zoomed); // en zoom no entra arriba
            }
            var goneUp = new List<CardView>();
            foreach (var kv in upgradeGlow) if (!kv.Key || !ups.Contains(kv.Key)) { if (kv.Value) Destroy(kv.Value.gameObject); goneUp.Add(kv.Key); }
            foreach (var k in goneUp) upgradeGlow.Remove(k);

            // cartel sobre ¡ARRANCAR!
            if (!crisisWarn && startBtn)
            {
                var bg = UiKit.Img(startBtn, "CrisisWarn", UiKit.RR, UiKit.Hex("#b3261e"), 16);
                bg.raycastTarget = false;
                crisisWarn = bg.RT();
                crisisWarn.At(new Vector2(0.5f, 1), new Vector2(0, 12), new Vector2(330, 52), new Vector2(0.5f, 0));
                crisisWarnTxt = UiKit.Txt(crisisWarn, "T", "", 22, Color.white, TextAlignmentOptions.Center, true);
                crisisWarnTxt.RT().Fill(10, 10, 0, 0);
                crisisWarnTxt.enableAutoSizing = true; crisisWarnTxt.fontSizeMin = 14; crisisWarnTxt.fontSizeMax = 22;
            }
            if (crisisWarn)
            {
                bool show = crises.Count > 0 && gs.phase == Phase.Previa;
                if (crisisWarn.gameObject.activeSelf != show) crisisWarn.gameObject.SetActive(show);
                if (show)
                {
                    crisisWarnTxt.text = crises.Count == 1 ? "1 CRISIS SE APLICA AL ARRANCAR" : crises.Count + " CRISIS SE APLICAN AL ARRANCAR";
                    crisisWarn.localScale = Vector3.one * (1 + w * 0.05f);
                }
            }
        }

        // Si hay crisis en la mano, pregunta antes de arrancar.
        void ConfirmStart()
        {
            var crises = CrisesInHand();
            if (crises.Count == 0) { StartCoroutine(StartRoutine()); return; }
            var o = MakeOverlay();
            UiKit.Txt(o, "Title", "CRISIS FORZADAS", 64, UiKit.RedSoft, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 210), new Vector2(1400, 80));
            var lines = new System.Text.StringBuilder();
            foreach (var cv in crises)
                lines.Append("<b>").Append(cv.def.title.ToUpperInvariant()).Append("</b>   ").Append(cv.def.EffectSummary()).Append('\n');
            UiKit.Txt(o, "List", lines.ToString(), 32, Color.white, TextAlignmentOptions.Center, false, UiKit.FontBody)
                .RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1400, 110));
            UiKit.Txt(o, "Ask", "Se van a aplicar las cartas de crisis forzadas. ¿Seguro que querés comenzar?\n<size=24>Podés evitarlas pagando desde la carta.</size>", 30, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody)
                .RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1400, 90));
            Button(o, "VOLVER", "#2f6fd6", "#163a7a", new Vector2(-230, -120), CloseOverlay);
            Button(o, "COMENZAR", "#d8452f", "#7a1f14", new Vector2(230, -120), () => { CloseOverlay(); StartCoroutine(StartRoutine()); });
        }

        void CloseOverlay()
        {
            if (overlay) Destroy(overlay.gameObject);
            overlay = null;
        }
    }
}
