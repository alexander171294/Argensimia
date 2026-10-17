using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Flujo del mandato en el HUD: previa (ARRANCAR + próxima oleada), gobernabilidad, gabinete,
    // cierre con sueldo y 1 de 3, crisis forzadas, fusión de cartas, victoria/derrota.
    public partial class GameHud
    {
        RectTransform govPanel, govFill, startBtn, previewPanel, cabinetRow, evadeBtn, overlay;
        TextMeshProUGUI govTxt, previewTxt, startSub, evadeTxt;
        Image govFillImg;
        Phase lastPhase = (Phase)(-1);
        int lastGov = -1, lastCabinet = -1;
        string lastPreview;
        bool flowBusy;

        void BuildFlow()
        {
            // Gobernabilidad (debajo del mandato)
            govPanel = UiKit.Panel(safe, "Governability", 14).At(new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(330, 54));
            UiKit.Txt(govPanel, "Label", "GOBERNABILIDAD", 18, Color.white, TextAlignmentOptions.Left, true).RT().At(new Vector2(0, 1), new Vector2(16, -4), new Vector2(220, 26));
            govTxt = UiKit.Txt(govPanel, "Value", "", 22, UiKit.Gold, TextAlignmentOptions.Right, true);
            govTxt.RT().At(Vector2.one, new Vector2(-14, -2), new Vector2(110, 28));
            govFill = Bar(govPanel, 16, 16, 9, UiKit.Hex("#5aa9ff"), out govFillImg);

            // Próxima oleada (previa)
            previewPanel = UiKit.Panel(safe, "NextWave", 16).At(new Vector2(0, 1), new Vector2(16, -100), new Vector2(330, 120));
            UiKit.Txt(previewPanel, "Title", "PRÓXIMA OLEADA", 20, UiKit.Gold, TextAlignmentOptions.Left, true).RT().At(new Vector2(0, 1), new Vector2(16, -8), new Vector2(300, 28));
            previewTxt = UiKit.Txt(previewPanel, "List", "", 21, Color.white, TextAlignmentOptions.TopLeft, false, UiKit.FontBody);
            previewTxt.RT().Fill(16, 12, 40, 10);
            previewTxt.lineSpacing = -6;

            // Gabinete (debajo de la popularidad)
            cabinetRow = UiKit.Rect("Cabinet", safe).At(Vector2.one, new Vector2(-16 - 76 - 12, -100), new Vector2(390, 40));

            // ARRANCAR MANDATO
            startBtn = UiKit.Rect("Start", safe).At(new Vector2(1, 0), new Vector2(-24, tickerHeight + 14 + 172 + 16), new Vector2(330, 108), new Vector2(1, 0));
            UiKit.Img(startBtn, "Base", UiKit.RR, UiKit.Hex("#1d6b2a"), 26, true).RT().Fill();
            var face = UiKit.Img(startBtn, "Face", UiKit.RR, UiKit.Hex("#43c157"), 26).RT().Fill(0, 0, 0, 9);
            var g = UiKit.Img(face, "Gloss", UiKit.RR, new Color(1, 1, 1, 0.18f), 22).RT();
            g.anchorMin = new Vector2(0, 0.5f); g.anchorMax = Vector2.one; g.offsetMin = new Vector2(4, 0); g.offsetMax = new Vector2(-4, -4);
            UiKit.Txt(face, "T", "¡ARRANCAR!", 44, Color.white, TextAlignmentOptions.Center, true).RT().Fill(0, 0, 4, 34);
            startSub = UiKit.Txt(face, "S", "", 20, UiKit.Hex("#eaffdf"), TextAlignmentOptions.Center, true);
            startSub.RT().Fill(0, 0, 62, 6);
            var st = startBtn.gameObject.AddComponent<Tap>();
            st.visual = startBtn; st.onTap = OnStart;

            // EVITAR (crisis) en el zoom
            evadeBtn = UiKit.Panel(zoomUi, "Evade", 24, true).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 96));
            evadeBtn.Find("Inner").GetComponent<Image>().color = UiKit.Hex("#1e56b8");
            evadeTxt = UiKit.Txt(evadeBtn, "T", "", 36, Color.white, TextAlignmentOptions.Center, true);
            evadeTxt.RT().Fill();
            var et = evadeBtn.gameObject.AddComponent<Tap>();
            et.visual = evadeBtn; et.onTap = OnEvade;
            evadeBtn.gameObject.SetActive(false);
        }

        void UpdateFlow()
        {
            UpdateAlerts();
            if (gs.governability != lastGov)
            {
                if (lastGov > gs.governability) { StartCoroutine(Shake(govPanel)); FloatText(govPanel, "-" + (lastGov - gs.governability), UiKit.RedSoft, 0); }
                lastGov = gs.governability;
                govTxt.text = gs.governability + "/" + gs.maxGovernability;
                govFill.anchorMax = new Vector2(Mathf.Max(0.03f, gs.governability / (float)gs.maxGovernability), 1);
                govFillImg.color = gs.governability > gs.maxGovernability * 0.5f ? UiKit.Hex("#5aa9ff") : gs.governability > gs.maxGovernability * 0.25f ? UiKit.Hex("#e8a52a") : UiKit.Red;
            }

            bool previa = gs.phase == Phase.Previa;
            startBtn.gameObject.SetActive(previa);
            previewPanel.gameObject.SetActive(previa);
            if (previa)
            {
                startSub.text = "MANDATO " + gs.mandate;
                var t = WaveDirector.I ? WaveDirector.I.PreviewText() : "";
                if (t != lastPreview)
                {
                    lastPreview = t;
                    previewTxt.text = t;
                    int lines = t.Split('\n').Length;
                    previewPanel.sizeDelta = new Vector2(330, 52 + lines * 25);
                }
            }
            incomeTxt.color = gs.phase == Phase.Oleada ? UiKit.Green : new Color(0.7f, 0.74f, 0.8f);

            if (gs.cabinet.Count != lastCabinet) RebuildCabinet();

            if (zoomed && !busy)
            {
                bool crisis = zoomed.def.kind == CardKind.Crisis;
                if (evadeBtn.gameObject.activeSelf != crisis) evadeBtn.gameObject.SetActive(crisis);
                var a = activateBtn.anchoredPosition;
                if (crisis)
                {
                    activateBtn.anchoredPosition = new Vector2(-200, a.y);
                    evadeBtn.anchoredPosition = new Vector2(200, a.y);
                    bool ok = gs.CanAfford(zoomed.def.cost);
                    evadeTxt.text = ok ? "EVITAR $" + GameState.Money(zoomed.def.cost) : "FALTA $" + GameState.Money(zoomed.def.cost - gs.money + 0.999f);
                    evadeTxt.color = ok ? Color.white : UiKit.RedSoft;
                }
                else if (Mathf.Abs(a.x) > 1) activateBtn.anchoredPosition = new Vector2(0, a.y);
            }
            else if (!zoomed && evadeBtn.gameObject.activeSelf) evadeBtn.gameObject.SetActive(false);

            if (gs.phase != lastPhase)
            {
                var prev = lastPhase;
                lastPhase = gs.phase;
                if (gs.phase == Phase.Cierre) StartCoroutine(ShowClose());
                else if (gs.phase == Phase.Victoria) StartCoroutine(ShowEnd(true));
                else if (gs.phase == Phase.Derrota) { if (GameFlow.I) { if (GameFlow.I.state == GameFlow.State.Playing) GameFlow.I.ToMenu(false); } else StartCoroutine(ShowEnd(false)); }
            }
        }

        void RebuildCabinet()
        {
            lastCabinet = gs.cabinet.Count;
            foreach (Transform c in cabinetRow) Destroy(c.gameObject);
            for (int i = 0; i < gs.cabinet.Count; i++)
            {
                var m = gs.cabinet[i];
                var chip = UiKit.Img(cabinetRow, "Min", UiKit.RR, UiKit.Shade(m.color, 0.85f), 12);
                chip.RT().At(Vector2.one, new Vector2(-i * 130, 0), new Vector2(124, 38));
                var t = UiKit.Txt(chip.transform, "T", m.title.Replace("Min. ", "").ToUpperInvariant(), 15, Color.white, TextAlignmentOptions.Center, true);
                t.RT().Fill(6, 6, 0, 0);
                t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = 15;
                StartCoroutine(Pulse(chip.RT()));
            }
        }

        // ------------------------------------------------------------------ previa → oleada

        void OnStart()
        {
            if (busy || flowBusy || zoomed || placing || paused || overlay || gs.phase != Phase.Previa) return;
            ConfirmStart();
        }

        IEnumerator StartRoutine()
        {
            flowBusy = true;
            // las crisis que quedaron en la mano se aplican solas
            for (int i = 0; i < cards.Length; i++)
            {
                var cv = cards[i];
                if (!cv || cv.def.kind != CardKind.Crisis) continue;
                gs.ApplyEffect(cv.def);
                FloatDeltas(cv.def);
                cards[i] = null;
                StartCoroutine(FlyAway(cv, hand.InverseTransformPoint(popPanel.position)));
                yield return new WaitForSecondsRealtime(0.25f);
                StartCoroutine(DrawInto(i));
            }
            WaveDirector.I.StartWave();
            StartCoroutine(Pulse(mandatePanel));
            flowBusy = false;
        }

        void OnEvade()
        {
            if (busy || !zoomed || zoomed.def.kind != CardKind.Crisis) return;
            if (!gs.Spend(zoomed.def.cost)) { StartCoroutine(Shake(evadeBtn)); return; }
            FloatText(moneyPanel, "-$" + GameState.Money(zoomed.def.cost), UiKit.RedSoft, 0);
            StartCoroutine(EvadeRoutine(zoomed));
        }

        IEnumerator EvadeRoutine(CardView cv)
        {
            busy = true;
            HideZoomButtons();
            evadeBtn.gameObject.SetActive(false);
            Vector3 from = cv.transform.localPosition, to = zoomLayer.InverseTransformPoint(renewRt.position);
            yield return UiKit.Tween(0.4f, t =>
            {
                float e = Ease.InCubic(t);
                dim.color = new Color(0, 0, 0, 0.68f * (1 - Ease.OutCubic(t)));
                cv.transform.localPosition = Vector3.Lerp(from, to, e);
                cv.transform.localScale = Vector3.one * Mathf.Lerp(zoomScale, 0.08f, e);
                cv.transform.localRotation = Quaternion.Euler(0, 0, -e * 200f);
                cv.group.alpha = 1 - e * e;
            });
            int slot = cv.slot;
            cards[slot] = null;
            Destroy(cv.gameObject);
            dim.gameObject.SetActive(false);
            zoomed = null;
            gs.PushHeadline("El Gobierno logra desactivar la crisis: «la situación está bajo control»");
            yield return DrawInto(slot);
            busy = false;
        }

        // Una crisis entra sola a la mano: reemplaza una carta al azar.
        IEnumerator InjectCrisis(CardDef crisis)
        {
            while (busy) yield return null;
            busy = true;
            var options = new List<int>();
            for (int i = 0; i < cards.Length; i++) if (cards[i] && cards[i].def.kind != CardKind.Crisis) options.Add(i);
            int slot = options.Count > 0 ? options[Random.Range(0, options.Count)] : -1;
            if (slot < 0) { busy = false; yield break; }
            var old = cards[slot];
            cards[slot] = null;
            deck.Discard(new CardInst(old.def, old.level));
            StartCoroutine(FlyAway(old, hand.InverseTransformPoint(deckTop.position)));
            yield return new WaitForSecondsRealtime(0.3f);
            if (!string.IsNullOrEmpty(crisis.headline)) gs.PushHeadline(crisis.headline);
            yield return DrawInto(slot, new CardInst(crisis));
            busy = false;
        }

        // ------------------------------------------------------------------ fusión

        IEnumerator TryFuse(CardView cv)
        {
            if (!cv || cv.def.kind == CardKind.Crisis || cv.def.kind == CardKind.Ministro || cv.level >= deck.maxLevel) yield break;
            CardView other = null;
            foreach (var o in cards)
                if (o && o != cv && o.def == cv.def && o.level == cv.level && o.transform.parent == hand && o.tap.interactable) { other = o; break; }
            if (!other) yield break;
            cv.tap.interactable = false;
            other.tap.interactable = false;
            yield return new WaitForSecondsRealtime(0.15f);
            Vector3 from = cv.transform.localPosition, to = other.transform.localPosition;
            yield return UiKit.Tween(0.4f, t =>
            {
                float e = Ease.InCubic(t);
                if (cv) { cv.transform.localPosition = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 80; cv.transform.localScale = Vector3.one * handScale * (1 - e * 0.4f); }
            });
            int slot = cv.slot, os = other.slot;
            cards[slot] = null;
            Destroy(cv.gameObject);
            var nv = CardView.Create(hand, other.def, other.level + 1);
            nv.slot = os;
            cards[os] = nv;
            nv.tap.onTap = () => OnCardTap(nv);
            Destroy(other.gameObject);
            ReturnToHand(nv);
            FloatText(nv.Rt, "¡FUSIÓN! NV " + nv.level, UiKit.Gold, 0);
            yield return UiKit.Tween(0.4f, t => { if (nv) nv.transform.localScale = Vector3.one * handScale * (1 + Mathf.Sin(t * Mathf.PI) * 0.25f); });
            if (nv) nv.transform.localScale = Vector3.one * handScale;
            yield return DrawInto(slot);
        }

        // ------------------------------------------------------------------ cierre: sueldo + 1 de 3

        RectTransform MakeOverlay()
        {
            if (overlay) Destroy(overlay.gameObject);
            var bg = UiKit.Img(root, "Overlay", null, new Color(0.02f, 0.04f, 0.1f, 0), 0, true);
            bg.RT().Fill();
            overlay = bg.RT();
            StartCoroutine(UiKit.Tween(0.3f, t => { if (bg) bg.color = new Color(0.02f, 0.04f, 0.1f, 0.78f * t); }));
            return overlay;
        }

        IEnumerator ShowClose()
        {
            if (zoomed) CancelZoom();
            if (placing) CancelPlacement();
            while (busy || flowBusy) yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
            int salary = gs.CloseMandate();
            var o = MakeOverlay();
            UiKit.Txt(o, "Title", "FIN DEL MANDATO " + gs.mandate, 64, Color.white, TextAlignmentOptions.Center, true).RT().At(new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1200, 80), new Vector2(0.5f, 1));
            int arr = WaveDirector.I ? WaveDirector.I.ArrivedThisWave : 0;
            UiKit.Txt(o, "Sub", "Sueldo <color=#7be36a>+$" + GameState.Money(salary) + "</color>    ·    Llegaron al vallado: <color=#ff7a70>" + arr + "</color>    ·    Gobernabilidad " + gs.governability + "/" + gs.maxGovernability,
                28, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody).RT().At(new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(1400, 40), new Vector2(0.5f, 1));
            UiKit.Txt(o, "Pick", "ELEGÍ UNA CARTA PARA TU MAZO", 34, UiKit.Gold, TextAlignmentOptions.Center, true).RT().At(new Vector2(0.5f, 1), new Vector2(0, -186), new Vector2(1200, 48), new Vector2(0.5f, 1));
            FloatText(moneyPanel, "+$" + GameState.Money(salary), UiKit.Green, 0.2f);
            var picks = deck.RollPicks(3, gs.mandate);
            bool chosen = false;
            const float s = 0.92f;
            for (int i = 0; i < picks.Count; i++)
            {
                var cv = CardView.Create(o, picks[i]);
                cv.dimWhenPoor = false;
                cv.Rt.anchoredPosition = new Vector2((i - (picks.Count - 1) * 0.5f) * 400, -70);
                cv.transform.localScale = Vector3.zero;
                var target = cv;
                cv.tap.onTap = () =>
                {
                    if (chosen) return;
                    chosen = true;
                    StartCoroutine(Choose(target));
                };
                int k = i;
                StartCoroutine(Delayed(0.15f + k * 0.12f, UiKit.Tween(0.4f, t => { if (target) target.transform.localScale = Vector3.one * s * Mathf.Max(0, Ease.OutBack(t)); })));
            }
        }

        IEnumerator Delayed(float d, IEnumerator r) { yield return new WaitForSecondsRealtime(d); yield return r; }

        IEnumerator Choose(CardView cv)
        {
            deck.AddToDeck(cv.def);
            foreach (Transform c in overlay)
            {
                var other = c.GetComponent<CardView>();
                if (other && other != cv) StartCoroutine(UiKit.Tween(0.25f, t => { if (other) other.group.alpha = 1 - t; }));
            }
            Vector3 from = cv.transform.localPosition, to = overlay.InverseTransformPoint(deckTop.position);
            float s0 = cv.transform.localScale.x;
            yield return UiKit.Tween(0.55f, t =>
            {
                float e = Ease.InCubic(t);
                cv.transform.localPosition = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 120;
                cv.transform.localScale = Vector3.one * Mathf.Lerp(s0, deckScale, e);
                cv.transform.localRotation = Quaternion.Euler(0, e * 180f, 0);
            });
            gs.PushHeadline("Nueva medida en carpeta: " + cv.def.title);
            var bg = overlay.GetComponent<Image>();
            var ov = overlay;
            yield return UiKit.Tween(0.25f, t => { if (bg) bg.color = new Color(0.02f, 0.04f, 0.1f, 0.78f * (1 - t)); });
            Destroy(ov.gameObject);
            overlay = null;
            WaveDirector.I.NextMandate();
            // crisis forzadas desde el mandato 2 (dos si la calle está muy caliente)
            var crises = deck.Crises();
            if (gs.mandate >= 2 && crises.Count > 0)
            {
                int n = gs.popularity < 30 ? 2 : 1;
                for (int i = 0; i < n; i++) yield return InjectCrisis(crises[Random.Range(0, crises.Count)]);
            }
        }

        // ------------------------------------------------------------------ victoria / derrota

        IEnumerator ShowEnd(bool win)
        {
            if (zoomed) CancelZoom();
            if (placing) CancelPlacement();
            while (busy || flowBusy) yield return null;
            yield return new WaitForSecondsRealtime(win ? 1f : 0.6f);
            var o = MakeOverlay();
            UiKit.Txt(o, "Title", win ? "¡COMPLETASTE EL PERÍODO!" : "PERDISTE LA GOBERNABILIDAD", 72, win ? UiKit.Gold : UiKit.RedSoft, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1400, 90));
            UiKit.Txt(o, "Sub", win ? "Cinco mandatos en la Casa Rosada. Popularidad final: " + Mathf.RoundToInt(gs.popularity) + "%"
                : "La Plaza quedó en manos de la calle en el mandato " + gs.mandate + (gs.endless ? " (endless)" : ""),
                30, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody).RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1400, 44));
            if (win) Button(o, "MODO ENDLESS", "#43c157", "#1d6b2a", new Vector2(-230, -40), () =>
            {
                Destroy(overlay.gameObject);
                overlay = null;
                int s = gs.CloseMandate();
                FloatText(moneyPanel, "+$" + GameState.Money(s), UiKit.Green, 0);
                WaveDirector.I.StartEndless();
            });
            Button(o, win ? "JUGAR DE NUEVO" : "REINTENTAR", "#2f6fd6", "#163a7a", new Vector2(win ? 230 : 0, -40), () =>
            {
                if (GameFlow.I && GameFlow.I.state == GameFlow.State.Playing) { Destroy(overlay.gameObject); overlay = null; GameFlow.I.ToMenu(win); return; }
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }

        void Button(RectTransform parent, string label, string faceHex, string baseHex, Vector2 pos, System.Action onTap)
        {
            var b = UiKit.Rect(label, parent).At(new Vector2(0.5f, 0.5f), pos, new Vector2(400, 104));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex(baseHex), 26, true).RT().Fill();
            var f = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex(faceHex), 26).RT().Fill(0, 0, 0, 9);
            UiKit.Txt(f, "T", label, 38, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            var t = b.gameObject.AddComponent<Tap>();
            t.visual = b; t.onTap = onTap;
        }
    }
}
