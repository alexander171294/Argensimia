using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Flujo del mandato en el HUD: previa (ARRANCAR + próxima oleada), gobernabilidad, gabinete,
    // cierre con sueldo, curación y 3 cartas al mazo (con CAMBIAR), crisis forzadas, victoria/derrota.
    public partial class GameHud
    {
        RectTransform govPanel, govFill, startBtn, previewPanel, cabinetRow, evadeBtn, overlay;
        TextMeshProUGUI govTxt, previewTxt, startSub, evadeTxt;
        Image govFillImg;
        Phase lastPhase = (Phase)(-1);
        int lastGov = -1, lastCabinet = -1;
        string lastPreview;
        bool flowBusy;
        TextMeshProUGUI previewTitle;
        // reloj de la previa: 30 s para decidir; al agotarse arranca solo (no corre en el cierre con las 3 cartas)
        const float StartTime = 30f;
        RectTransform clockRt;
        Image clockFill;
        TextMeshProUGUI clockTxt;
        float startLeft = -1;
        int lastTick = -1;
        bool autoStarting;

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
            cabinetRow = UiKit.Rect("Cabinet", safe).At(Vector2.one, new Vector2(-16 - 76 - 12, -140), new Vector2(390, 40)); // debajo de la cinta del estado

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

            // reloj a la izquierda de ¡ARRANCAR!
            clockRt = UiKit.Rect("StartClock", safe).At(new Vector2(1, 0), new Vector2(-24 - 330 - 14, tickerHeight + 14 + 172 + 16), new Vector2(108, 108), new Vector2(1, 0));
            var ring = UiKit.Img(clockRt, "Ring", UiKit.Circle, UiKit.Gold);
            ring.RT().Fill();
            UiKit.Img(ring.transform, "Bg", UiKit.Circle, UiKit.NavyDark).RT().Fill(5, 5, 5, 5);
            clockFill = UiKit.Img(ring.transform, "Fill", UiKit.Circle, UiKit.Green);
            clockFill.RT().Fill(9, 9, 9, 9);
            clockFill.type = Image.Type.Filled;
            clockFill.fillMethod = Image.FillMethod.Radial360;
            clockFill.fillOrigin = (int)Image.Origin360.Top;
            clockFill.fillClockwise = false;
            UiKit.Img(ring.transform, "In", UiKit.Circle, UiKit.NavyDark).RT().Fill(24, 24, 24, 24);
            clockTxt = UiKit.Txt(ring.transform, "T", "30", 40, Color.white, TextAlignmentOptions.Center, true);
            clockTxt.RT().Fill();

            // EVITAR (crisis) en el zoom
            evadeBtn = UiKit.Panel(zoomUi, "Evade", 24, true).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 96));
            evadeBtn.Find("Inner").GetComponent<Image>().color = UiKit.Hex("#1e56b8");
            evadeTxt = UiKit.Txt(evadeBtn, "T", "", 36, Color.white, TextAlignmentOptions.Center, true);
            evadeTxt.RT().Fill();
            var et = evadeBtn.gameObject.AddComponent<Tap>();
            et.visual = evadeBtn; et.onTap = OnEvade;
            evadeBtn.gameObject.SetActive(false);

            // VENDER (tacho) en el zoom: descarta la carta a cambio de SellPrice
            sellBtn = UiKit.Rect("Sell", zoomUi).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 120));
            var circ = UiKit.Img(sellBtn, "Btn", UiKit.Circle, UiKit.Hex("#7a231d"), 0, true);
            circ.RT().At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(84, 84), new Vector2(0.5f, 1));
            UiKit.Img(circ.transform, "Face", UiKit.Circle, UiKit.Hex("#d9534a")).RT().Fill(4, 4, 4, 8);
            var ic = UiKit.Rect("Trash", circ.transform).At(new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(44, 50));
            UiKit.Img(ic, "Lid", UiKit.RR, Color.white, 3).RT().At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(44, 8), new Vector2(0.5f, 1));
            UiKit.Img(ic, "Handle", UiKit.RR, Color.white, 3).RT().At(new Vector2(0.5f, 1), new Vector2(0, 5), new Vector2(16, 8), new Vector2(0.5f, 1));
            var body = UiKit.Img(ic, "Body", UiKit.RR, Color.white, 5).RT();
            body.At(new Vector2(0.5f, 0), Vector2.zero, new Vector2(34, 38), new Vector2(0.5f, 0));
            for (int i = -1; i <= 1; i++) UiKit.Img(body, "L", UiKit.RR, UiKit.Hex("#d9534a"), 2).RT().At(new Vector2(0.5f, 0.5f), new Vector2(i * 10, 0), new Vector2(4, 26));
            var pill = UiKit.Img(sellBtn, "Price", UiKit.RR, new Color(0, 0, 0, 0.6f), 12).RT();
            pill.At(new Vector2(0.5f, 0), Vector2.zero, new Vector2(96, 30), new Vector2(0.5f, 0));
            UiKit.Txt(pill, "T", "+$" + SellPrice, 22, UiKit.Green, TextAlignmentOptions.Center, true).RT().Fill();
            var stp = sellBtn.gameObject.AddComponent<Tap>();
            stp.visual = sellBtn; stp.onTap = OnSell;
            sellBtn.gameObject.SetActive(false);
        }

        const int SellPrice = 100;
        RectTransform sellBtn;

        void OnSell()
        {
            if (busy || !zoomed || zoomed.def.kind == CardKind.Crisis) return;
            StartCoroutine(SellRoutine(zoomed));
        }

        // La carta se achica, gira y cae en el tacho; entra la plata y se roba otra.
        IEnumerator SellRoutine(CardView cv)
        {
            busy = true;
            HideZoomButtons();
            Snd.Ui("card_flip");
            Vector3 from = cv.transform.localPosition, to = sellBtn.anchoredPosition;
            yield return UiKit.Tween(0.38f, t =>
            {
                float e = Ease.InCubic(t);
                dim.color = new Color(0, 0, 0, dimA * (1 - Ease.OutCubic(t)));
                cv.transform.localPosition = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 60f;
                cv.transform.localScale = Vector3.one * Mathf.Lerp(zoomScale, 0.05f, e);
                cv.transform.localRotation = Quaternion.Euler(0, 0, e * 160f);
                cv.group.alpha = 1 - Mathf.Clamp01((t - 0.6f) / 0.4f);
            });
            gs.money += SellPrice;
            Snd.Ui("coin");
            FloatText(moneyPanel, "+$" + SellPrice, UiKit.Green, 0);
            StartCoroutine(Pulse(moneyPanel));
            int slot = cv.slot; // vendida: sale del juego (no vuelve al mazo)
            cards[slot] = null;
            Destroy(cv.gameObject);
            dim.gameObject.SetActive(false);
            zoomed = null;
            Defense.ShowHints(null, false);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return DrawInto(slot);
            busy = false;
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
            UpdateClock(previa);
            // previa: qué viene; oleada: cuántos quedan (por entrar + en la Plaza)
            bool wave = gs.phase == Phase.Oleada;
            previewPanel.gameObject.SetActive(previa || wave);
            if (previa) startSub.text = L.F("MANDATO {0}", gs.mandate);
            if (previa || wave)
            {
                if (!previewTitle) previewTitle = previewPanel.Find("Title").GetComponent<TextMeshProUGUI>();
                string title = L.T(previa ? "PRÓXIMA OLEADA" : "MANIFESTANTES RESTANTES");
                if (wave && WaveDirector.I) title += "  <color=#ffffff>" + WaveDirector.I.RemainingCount + "</color>";
                if (previewTitle.text != title) previewTitle.text = title;
                var t = WaveDirector.I ? (previa ? WaveDirector.I.PreviewText() : WaveDirector.I.RemainingText()) : "";
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
                    evadeTxt.text = ok ? L.T("EVITAR $") + GameState.Money(zoomed.def.cost) : L.T("FALTA $") + GameState.Money(zoomed.def.cost - gs.money + 0.999f);
                    evadeTxt.color = ok ? Color.white : UiKit.RedSoft;
                }
                else if (Mathf.Abs(a.x) > 1) activateBtn.anchoredPosition = new Vector2(0, a.y);
            }
            else if (!zoomed && evadeBtn.gameObject.activeSelf) evadeBtn.gameObject.SetActive(false);
            bool sell = zoomed && !busy && zoomed.def.kind != CardKind.Crisis && (gs.phase == Phase.Previa || gs.phase == Phase.Oleada);
            if (sellBtn.gameObject.activeSelf != sell) sellBtn.gameObject.SetActive(sell);
            if (sell) sellBtn.anchoredPosition = new Vector2(-closeBtn.anchoredPosition.x, closeBtn.anchoredPosition.y - 30); // arriba a la izquierda de la carta

            if (gs.phase != lastPhase)
            {
                var prev = lastPhase;
                lastPhase = gs.phase;
                if (gs.phase == Phase.Cierre) StartCoroutine(ShowClose());
                else if (gs.phase == Phase.Victoria) StartCoroutine(ShowEnd(true));
                else if (gs.phase == Phase.Derrota) { if (GameFlow.I) { if (GameFlow.I.state == GameFlow.State.Playing) GameFlow.I.ToMenu(false); } else StartCoroutine(ShowEnd(false)); }
            }
        }

        void UpdateClock(bool previa)
        {
            clockRt.gameObject.SetActive(previa);
            if (!previa) { startLeft = -1; lastTick = -1; return; }
            if (startLeft < 0) startLeft = StartTime;
            bool live = !GameFlow.I || GameFlow.I.state == GameFlow.State.Playing;
            if (live && !autoStarting) startLeft = Mathf.Max(0, startLeft - Time.deltaTime); // en pausa deltaTime = 0
            float k = startLeft / StartTime;
            clockFill.fillAmount = k;
            clockFill.color = k > 0.5f ? Color.Lerp(UiKit.Hex("#e8c22a"), UiKit.Green, (k - 0.5f) * 2) : Color.Lerp(UiKit.Red, UiKit.Hex("#e8c22a"), k * 2);
            int secs = Mathf.CeilToInt(startLeft);
            clockTxt.text = secs.ToString();
            clockTxt.color = secs <= 5 ? UiKit.RedSoft : Color.white;
            float pulse = secs <= 5 && secs > 0 ? 1 + Mathf.Max(0, Mathf.Sin((startLeft % 1f) * Mathf.PI)) * 0.08f : 1;
            clockRt.localScale = Vector3.one * pulse;
            if (live && secs != lastTick)
            {
                if (secs <= 5 && secs > 0) Snd.Ui("clock_tick", 1, secs <= 3 ? 1.25f : 1f, 0);
                lastTick = secs;
            }
            if (live && startLeft <= 0 && !autoStarting) StartCoroutine(AutoStart());
        }

        // Se acabó el tiempo: cierra lo que haya abierto y arranca (las crisis de la mano se aplican igual).
        IEnumerator AutoStart()
        {
            autoStarting = true;
            if (overlay) CloseOverlay();
            if (zoomed) CancelZoom();
            if (placing) CancelPlacement();
            CancelDrag();
            while (busy || flowBusy) yield return null;
            if (gs.phase == Phase.Previa) yield return StartRoutine();
            autoStarting = false;
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
                var t = UiKit.Txt(chip.transform, "T", L.CardTitle(m).Replace("Min. ", "").Replace(" Min.", "").ToUpperInvariant(), 15, Color.white, TextAlignmentOptions.Center, true);
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
                Snd.Ui("mood_down", 0.8f, 1, 0.4f);
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
            Snd.Ui("spend");
            StartCoroutine(EvadeRoutine(zoomed));
        }

        IEnumerator EvadeRoutine(CardView cv)
        {
            busy = true;
            HideZoomButtons();
            evadeBtn.gameObject.SetActive(false);
            Snd.Ui("evade");
            Vector3 from = cv.transform.localPosition, to = zoomLayer.InverseTransformPoint(renewRt.position);
            yield return UiKit.Tween(0.4f, t =>
            {
                float e = Ease.InCubic(t);
                dim.color = new Color(0, 0, 0, dimA * (1 - Ease.OutCubic(t)));
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
            Headlines.Say("crisis_evaded");
            yield return DrawInto(slot);
            busy = false;
        }

        // Una crisis entra sola a la mano: reemplaza una carta al azar.
        IEnumerator InjectCrisis(CardDef crisis)
        {
            while (busy) yield return null;
            busy = true;
            var empty = new List<int>();
            for (int i = 0; i < cards.Length; i++) if (!cards[i]) empty.Add(i);
            if (empty.Count > 0)
            {
                if (!string.IsNullOrEmpty(crisis.headline)) gs.PushHeadline(Headlines.PickVariant(L.CardHeadline(crisis)));
                Snd.Ui("crisis", 1, 1, 0.8f);
                yield return DrawInto(empty[Random.Range(0, empty.Count)], new CardInst(crisis));
                busy = false;
                yield break;
            }
            var options = new List<int>();
            for (int i = 0; i < cards.Length; i++) if (cards[i] && cards[i].def.kind != CardKind.Crisis) options.Add(i);
            int slot = options.Count > 0 ? options[Random.Range(0, options.Count)] : -1;
            if (slot < 0) { busy = false; yield break; }
            var old = cards[slot];
            cards[slot] = null;
            deck.ReturnToDeck(new CardInst(old.def, old.level)); // la carta pisada vuelve al fondo del mazo
            StartCoroutine(FlyAway(old, hand.InverseTransformPoint(deckTop.position)));
            yield return new WaitForSecondsRealtime(0.3f);
            if (!string.IsNullOrEmpty(crisis.headline)) gs.PushHeadline(Headlines.PickVariant(L.CardHeadline(crisis)));
                Snd.Ui("crisis", 1, 1, 0.8f);
            yield return DrawInto(slot, new CardInst(crisis));
            busy = false;
        }

        // ------------------------------------------------------------------ cierre: sueldo + curación + 3 cartas al mazo

        RectTransform MakeOverlay()
        {
            if (overlay) Destroy(overlay.gameObject);
            var bg = UiKit.Img(root, "Overlay", null, new Color(0.02f, 0.04f, 0.1f, 0), 0, true);
            bg.RT().Fill();
            overlay = bg.RT();
            StartCoroutine(UiKit.Tween(0.3f, t => { if (bg) bg.color = new Color(0.02f, 0.04f, 0.1f, 0.78f * t); }));
            return overlay;
        }

        class Reward
        {
            public CardView cv;
            public bool rerolled;
            public Image face, bas;
            public TextMeshProUGUI label;
            public RectTransform btn;
            public float x;
        }

        readonly List<Reward> rewards = new List<Reward>();
        bool rewardBusy;
        const float RewardScale = 0.78f, RewardY = -20, RewardGap = 380;

        IEnumerator ShowClose()
        {
            if (zoomed) CancelZoom();
            if (placing) CancelPlacement();
            CancelDrag();
            while (busy || flowBusy) yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
            Snd.Ui("fanfare");
            int salary = gs.CloseMandate();
            StartCoroutine(SndLater("coin", 0.9f));
            var o = MakeOverlay();
            UiKit.Txt(o, "Title", L.F("FIN DEL MANDATO {0}", gs.mandate), 64, Color.white, TextAlignmentOptions.Center, true).RT().At(new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1200, 80), new Vector2(0.5f, 1));
            int arr = WaveDirector.I ? WaveDirector.I.ArrivedThisWave : 0;
            string heal = gs.LastHealed > 0 ? L.F("    ·    Policía <color=#7be36a>+{0}% vida</color>", Mathf.RoundToInt(gs.CloseHeal * 100)) : "";
            UiKit.Txt(o, "Sub", "Sueldo <color=#7be36a>+$" + GameState.Money(salary) + "</color>    ·    Llegaron al vallado: <color=#ff7a70>" + arr + "</color>    ·    Gobernabilidad " + gs.governability + "/" + gs.maxGovernability + heal,
                28, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody).RT().At(new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(1500, 40), new Vector2(0.5f, 1));
            var picks = deck.RollPicks(deck.rewardCount, gs.mandate);
            UiKit.Txt(o, "Pick", L.F("OBTUVISTE {0} CARTAS PARA TU MAZO", picks.Count), 34, UiKit.Gold, TextAlignmentOptions.Center, true).RT().At(new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(1200, 48), new Vector2(0.5f, 1));
            if (gs.LastHealed > 0) Headlines.Say("close");
            FloatText(moneyPanel, "+$" + GameState.Money(salary), UiKit.Green, 0.2f);
            rewards.Clear();
            rewardBusy = false;
            for (int i = 0; i < picks.Count; i++)
            {
                var r = new Reward { x = (i - (picks.Count - 1) * 0.5f) * RewardGap };
                rewards.Add(r);
                r.cv = RewardCard(o, picks[i], r.x);
                var target = r.cv;
                target.transform.localScale = Vector3.zero;
                StartCoroutine(Delayed(0.15f + i * 0.12f, UiKit.Tween(0.4f, t => { if (target) target.transform.localScale = Vector3.one * RewardScale * Mathf.Max(0, Ease.OutBack(t)); })));
                RerollButton(o, r);
                StartCoroutine(SndLater("card_flip", 1.0f + i * 0.12f, 0.8f));
            }
            Button(o, "CONTINUAR", "#43c157", "#1d6b2a", new Vector2(0, -372), () => { if (!rewardBusy) StartCoroutine(ConfirmRewards()); });
        }

        CardView RewardCard(RectTransform parent, CardDef def, float x)
        {
            var cv = CardView.Create(parent, def);
            cv.dimWhenPoor = false;
            cv.tap.interactable = false;
            cv.Rt.anchoredPosition = new Vector2(x, RewardY);
            cv.transform.localScale = Vector3.one * RewardScale;
            return cv;
        }

        void RerollButton(RectTransform parent, Reward r)
        {
            var b = UiKit.Rect("Reroll", parent).At(new Vector2(0.5f, 0.5f), new Vector2(r.x, -262), new Vector2(250, 62));
            r.bas = UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex("#163a7a"), 18, true);
            r.bas.RT().Fill();
            r.face = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex("#2f6fd6"), 18);
            r.face.RT().Fill(0, 0, 0, 6);
            UiKit.Img(r.face.transform, "Icon", UiKit.Refresh, Color.white).RT().At(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(34, 34), new Vector2(0, 0.5f));
            r.label = UiKit.Txt(r.face.transform, "T", L.T("CAMBIAR $") + GameState.Money(deck.rerollCost), 26, Color.white, TextAlignmentOptions.Center, true);
            r.label.RT().Fill(44, 8, 0, 0);
            r.btn = b;
            var t = b.gameObject.AddComponent<Tap>();
            t.visual = b;
            t.onTap = () => OnReroll(r);
        }

        void OnReroll(Reward r)
        {
            if (rewardBusy || r.rerolled || !r.cv) return;
            if (!gs.Spend(deck.rerollCost))
            {
                StartCoroutine(Shake(r.btn));
                FloatText(r.btn, L.T("FALTA $") + GameState.Money(deck.rerollCost - gs.money + 0.999f), UiKit.RedSoft, 0);
                return;
            }
            FloatText(moneyPanel, "-$" + GameState.Money(deck.rerollCost), UiKit.RedSoft, 0);
            r.rerolled = true;
            r.label.text = L.T("CAMBIADA");
            r.face.color = UiKit.Hex("#7b8494");
            r.bas.color = UiKit.Hex("#4a5160");
            r.btn.GetComponent<Tap>().interactable = false;
            Headlines.Say("reroll", 10f);
            Snd.Ui("spend", 0.7f);
            Snd.Ui("card_flip");
            StartCoroutine(RerollRoutine(r));
        }

        // La carta gira, y del otro lado aparece una nueva (distinta de las que están en pantalla).
        IEnumerator RerollRoutine(Reward r)
        {
            rewardBusy = true;
            var shown = new List<CardDef>();
            foreach (var o in rewards) if (o.cv) shown.Add(o.cv.def);
            var nd = deck.RollOne(gs.mandate, shown) ?? deck.RollOne(gs.mandate, null);
            var old = r.cv;
            yield return UiKit.Tween(0.18f, t => { if (old) old.transform.localRotation = Quaternion.Euler(0, t * 90f, 0); });
            var nv = RewardCard(overlay, nd, r.x);
            nv.transform.localRotation = Quaternion.Euler(0, -90f, 0);
            r.cv = nv;
            Destroy(old.gameObject);
            yield return UiKit.Tween(0.22f, t => { if (nv) nv.transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(-90f, 0, Ease.OutCubic(t)), 0); });
            yield return UiKit.Tween(0.25f, t => { if (nv) nv.transform.localScale = Vector3.one * RewardScale * (1 + Mathf.Sin(t * Mathf.PI) * 0.08f); });
            rewardBusy = false;
        }

        IEnumerator Delayed(float d, IEnumerator r) { yield return new WaitForSecondsRealtime(d); yield return r; }

        IEnumerator ConfirmRewards()
        {
            rewardBusy = true;
            foreach (Transform c in overlay)
                if (c.name == "Reroll" || c.name == "CONTINUAR") c.gameObject.SetActive(false);
            var names = new List<string>();
            Vector3 to = overlay.InverseTransformPoint(deckTop.position);
            for (int i = 0; i < rewards.Count; i++)
            {
                var cv = rewards[i].cv;
                if (!cv) continue;
                deck.AddToDeck(cv.def);
                Snd.Ui("card_draw", 0.9f, 1.1f, 0);
                names.Add(L.CardTitle(cv.def));
                Vector3 from = cv.transform.localPosition;
                float s0 = cv.transform.localScale.x;
                StartCoroutine(UiKit.Tween(0.55f, t =>
                {
                    if (!cv) return;
                    float e = Ease.InCubic(t);
                    cv.transform.localPosition = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 120;
                    cv.transform.localScale = Vector3.one * Mathf.Lerp(s0, deckScale, e);
                    cv.transform.localRotation = Quaternion.Euler(0, e * 180f, 0);
                }));
                yield return new WaitForSecondsRealtime(0.12f);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            StartCoroutine(Pulse(deckRt));
            if (names.Count > 0)
                Headlines.Say("new_cards", (names.Count == 1 ? names[0] : string.Join(", ", names.GetRange(0, names.Count - 1)) + (L.En ? " and " : " y ") + names[names.Count - 1]));
            var bg = overlay.GetComponent<Image>();
            var ov = overlay;
            yield return UiKit.Tween(0.25f, t => { if (bg) bg.color = new Color(0.02f, 0.04f, 0.1f, 0.78f * (1 - t)); });
            Destroy(ov.gameObject);
            overlay = null;
            rewards.Clear();
            rewardBusy = false;
            WaveDirector.I.NextMandate();
            // primero se completa la mano con lo que haya en el mazo; después entran las crisis forzadas
            yield return DealHand(0.1f);
            while (busy) yield return null;
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
            CancelDrag();
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
