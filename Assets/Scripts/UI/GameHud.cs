using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // HUD completo generado por código (la escena se regenera, así que no hay UI serializada).
    // Unidades de referencia 1600x900, landscape. Todo es táctil (Input System UI module).
    [DefaultExecutionOrder(100)]
    public partial class GameHud : MonoBehaviour
    {
        [Header("Layout (unidades de referencia 1600x900)")]
        public float handScale = 0.43f;
        public float handSpacing = 168f;
        public float deckScale = 0.36f;
        public float zoomScale = 1.34f;
        public float tickerHeight = 46f;
        public float tickerSpeed = 110f;

        [Header("Colocar")]
        [Tooltip("Zona válida para colocar (x, z en mundo): plaza + franja policial")]
        public Rect placeBounds = Rect.MinMaxRect(-46, -26, 40, 26);
        public float groundY = 0.16f;
        [Tooltip("Radio (m) para que un tap enganche el slot libre más cercano")] public float slotSnapDistance = 4f;

        [Header("Marquesina")]
        public string[] headlines =
        {
            "El Gobierno asegura que está todo controlado",
            "Fuentes oficiales: «la Plaza está tranquila»",
            "Cortes en Av. de Mayo: recomiendan evitar la zona",
            "El dólar abre en alza y el mercado mira a la Plaza",
            "Columnas avanzan desde el Congreso hacia Casa Rosada",
            "Encuesta: la imagen del Gobierno, en caída libre",
            "El vocero desmiente renuncias en el Gabinete",
        };

        GameState gs;
        CardDeck deck;
        Canvas canvas;
        CanvasScaler scaler;
        RectTransform root, safe, hand, zoomLayer, zoomUi, fxLayer;
        RectTransform moneyPanel, mandatePanel, popPanel, deckRt, deckTop, renewRt, renewIcon;
        RectTransform tickerRt, tickerContent, tickerViewport, mandateFill, popFill, activateBtn, closeBtn, placeBanner;
        TextMeshProUGUI moneyTxt, incomeTxt, mandateTxt, popTxt, moodTxt, deckCount, renewCostTxt, tickerTxt, placeTxt, activateTxt;
        Image popFillImg, moodBg, dim, mapCatcher, activateFace, activateBase, tickerDot;
        CanvasGroup zoomUiGroup, bannerGroup;
        GameObject pauseOverlay;

        CardView[] cards;
        CardView zoomed, placing;
        bool busy, paused;
        float shownMoney, shownPop, tickerX = 1e6f;
        int lastMoney = -1, lastIncome = -1, lastPop = -1, lastMandate = -1, lastMood = -1, lastDeck = -1;
        Rect lastSafe;
        Vector2Int lastScreen;
        readonly Queue<string> urgent = new Queue<string>();
        int headlineIdx;
        bool tickerIsFiller;

        static readonly Color[] MoodColors = { UiKit.Hex("#3fb950"), UiKit.Hex("#d9a92a"), UiKit.Hex("#e8742a"), UiKit.Hex("#d73a3a") };

        // ------------------------------------------------------------------ setup

        void Start()
        {
            gs = GameState.I ? GameState.I : FindAnyObjectByType<GameState>();
            deck = FindAnyObjectByType<CardDeck>();
            if (!gs || !deck) { Debug.LogError("GameHud: falta GameState o CardDeck en la escena"); enabled = false; return; }
            Application.targetFrameRate = 60;
            cards = new CardView[deck.handSize];
            shownMoney = gs.money;
            shownPop = gs.popularity;
            EnsureEventSystem();
            Build();
            ApplySafeArea(true);
            if (GameFlow.I && !GameFlow.I.HudVisible) SetHudVisible(false, 0); // menú: el HUD aparece al terminar el paneo
            gs.Headline += OnHeadline;
            gs.Lost += OnLost;
            StartCoroutine(DealHand(0.6f));
        }

        void OnDestroy() { if (gs) { gs.Headline -= OnHeadline; gs.Lost -= OnLost; } Time.timeScale = 1; AudioListener.pause = false; }

        static void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (!es) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (!es.TryGetComponent<InputSystemUIInputModule>(out var m)) m = es.gameObject.AddComponent<InputSystemUIInputModule>();
            if (m.actionsAsset == null) m.AssignDefaultActions();
            es.pixelDragThreshold = Mathf.Max(10, Mathf.RoundToInt(Screen.dpi > 0 ? Screen.dpi * 0.06f : 10)); // tolerancia de dedo
        }

        void Build()
        {
            var go = new GameObject("HUD", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            go.AddComponent<GraphicRaycaster>();
            root = (RectTransform)go.transform;

            // Captura de taps en el mapa (solo en modo colocar). Primero = debajo de todo el HUD.
            mapCatcher = UiKit.Img(root, "MapCatcher", null, new Color(0, 0, 0, 0), 0, true);
            mapCatcher.RT().Fill();
            var mc = mapCatcher.gameObject.AddComponent<Tap>();
            mc.onTapAt = OnMapTap;
            mapCatcher.gameObject.SetActive(false);

            BuildTicker();
            safe = UiKit.Rect("Safe", root).Fill();
            BuildTopBar();
            hand = UiKit.Rect("Hand", safe).At(new Vector2(0.5f, 0), new Vector2(0, tickerHeight + 14 + CardView.H * handScale * 0.5f), new Vector2(10, 10), new Vector2(0.5f, 0.5f));
            BuildDeck();
            BuildRenew();
            BuildShop();
            BuildPlaceBanner();
            fxLayer = UiKit.Rect("FX", root).Fill();
            BuildZoomLayer();
            BuildFlow();
            BuildPause();
        }

        void BuildTicker()
        {
            var bg = UiKit.Img(root, "Ticker", null, new Color(0.04f, 0.07f, 0.16f, 0.94f));
            tickerRt = bg.RT();
            tickerRt.anchorMin = Vector2.zero; tickerRt.anchorMax = new Vector2(1, 0); tickerRt.pivot = new Vector2(0.5f, 0);
            tickerRt.sizeDelta = new Vector2(0, tickerHeight); tickerRt.anchoredPosition = Vector2.zero;
            var line = UiKit.Img(tickerRt, "Line", null, UiKit.Gold).RT();
            line.anchorMin = new Vector2(0, 1); line.anchorMax = Vector2.one; line.pivot = new Vector2(0.5f, 1);
            line.sizeDelta = new Vector2(0, 3); line.anchoredPosition = Vector2.zero;

            tickerContent = UiKit.Rect("Content", tickerRt);
            tickerContent.anchorMin = new Vector2(0, 1); tickerContent.anchorMax = Vector2.one; tickerContent.pivot = new Vector2(0.5f, 1);
            tickerContent.sizeDelta = new Vector2(0, tickerHeight); tickerContent.anchoredPosition = Vector2.zero;

            var tag = UiKit.Img(tickerContent, "Tag", null, UiKit.Red).RT();
            tag.anchorMin = Vector2.zero; tag.anchorMax = new Vector2(0, 1); tag.pivot = new Vector2(0, 0.5f);
            tag.offsetMin = Vector2.zero; tag.offsetMax = new Vector2(250, -3);
            tickerDot = UiKit.Img(tag, "Dot", UiKit.Circle, Color.white);
            tickerDot.RT().At(new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(12, 12), new Vector2(0, 0.5f));
            UiKit.Txt(tag, "T", "ÚLTIMO MOMENTO", 23, Color.white, TextAlignmentOptions.Left).RT().Fill(36, 6, 0, 0);

            tickerViewport = UiKit.Rect("Viewport", tickerContent);
            tickerViewport.anchorMin = Vector2.zero; tickerViewport.anchorMax = Vector2.one;
            tickerViewport.offsetMin = new Vector2(266, 0); tickerViewport.offsetMax = new Vector2(-8, -3);
            tickerViewport.gameObject.AddComponent<RectMask2D>();
            tickerTxt = UiKit.Txt(tickerViewport, "Text", "", 26, Color.white, TextAlignmentOptions.Left, false, UiKit.FontBody);
            var tr = tickerTxt.RT();
            tr.anchorMin = Vector2.zero; tr.anchorMax = new Vector2(0, 1); tr.pivot = new Vector2(0, 0.5f);
            tr.sizeDelta = new Vector2(2000, 0);
        }

        void BuildTopBar()
        {
            // Presupuesto + ingreso por segundo
            moneyPanel = UiKit.Panel(safe, "Money").At(new Vector2(0, 1), new Vector2(16, -12), new Vector2(430, 76));
            UiKit.Coin(moneyPanel, 58).At(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(58, 58));
            UiKit.Txt(moneyPanel, "Label", "PRESUPUESTO", 17, UiKit.Gold, TextAlignmentOptions.Left)
                .RT().At(new Vector2(0, 1), new Vector2(84, -7), new Vector2(240, 22));
            moneyTxt = UiKit.Txt(moneyPanel, "Value", "$ 0", 38, Color.white, TextAlignmentOptions.Left, true);
            moneyTxt.RT().At(Vector2.zero, new Vector2(84, 4), new Vector2(220, 46));
            incomeTxt = UiKit.Txt(moneyPanel, "Income", "", 26, UiKit.Green, TextAlignmentOptions.Right, true);
            incomeTxt.RT().At(new Vector2(1, 0), new Vector2(-14, 8), new Vector2(140, 40));

            // Mandato (= oleada)
            mandatePanel = UiKit.Panel(safe, "Mandate").At(new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(330, 76));
            UiKit.Img(mandatePanel, "Icon", UiKit.Building, Color.white).RT().At(new Vector2(0, 0.5f), new Vector2(14, 3), new Vector2(54, 54));
            UiKit.Txt(mandatePanel, "Label", "MANDATO", 26, Color.white, TextAlignmentOptions.Left, true)
                .RT().At(new Vector2(0, 1), new Vector2(80, -5), new Vector2(150, 40));
            mandateTxt = UiKit.Txt(mandatePanel, "Value", "", 34, UiKit.Gold, TextAlignmentOptions.Right, true);
            mandateTxt.RT().At(Vector2.one, new Vector2(-16, -2), new Vector2(110, 44));
            mandateFill = Bar(mandatePanel, 80, 16, 12, UiKit.Green, out _);

            // Pausa
            var pause = UiKit.Panel(safe, "Pause", 16, true).At(Vector2.one, new Vector2(-16, -12), new Vector2(76, 76));
            for (int i = -1; i <= 1; i++) UiKit.Img(pause, "Bar" + (i + 1), UiKit.RR, Color.white, 4).RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, i * 13), new Vector2(40, 8));
            var pt = pause.gameObject.AddComponent<Tap>();
            pt.visual = pause; pt.onTap = TogglePause;

            // Popularidad (= dificultad de la oleada)
            popPanel = UiKit.Panel(safe, "Popularity").At(Vector2.one, new Vector2(-16 - 76 - 12, -12), new Vector2(390, 76));
            UiKit.Img(popPanel, "Icon", UiKit.People, Color.white).RT().At(new Vector2(0, 0.5f), new Vector2(14, 3), new Vector2(54, 54));
            UiKit.Txt(popPanel, "Label", "POPULARIDAD", 24, Color.white, TextAlignmentOptions.Left, true)
                .RT().At(new Vector2(0, 1), new Vector2(80, -6), new Vector2(220, 38));
            popTxt = UiKit.Txt(popPanel, "Value", "", 34, UiKit.Gold, TextAlignmentOptions.Right, true);
            popTxt.RT().At(Vector2.one, new Vector2(-16, -2), new Vector2(100, 44));
            popFill = Bar(popPanel, 80, 16, 12, UiKit.Green, out popFillImg);
            moodBg = UiKit.Img(popPanel, "Mood", UiKit.RR, MoodColors[2], 10);
            moodBg.RT().At(new Vector2(1, 0), new Vector2(-20, -20), new Vector2(236, 34), new Vector2(1, 0.5f));
            moodTxt = UiKit.Txt(moodBg.transform, "T", "", 21, Color.white, TextAlignmentOptions.Center, true);
            moodTxt.RT().Fill();
        }

        RectTransform Bar(RectTransform parent, float left, float right, float y, Color c, out Image fillImg)
        {
            var track = UiKit.Img(parent, "Track", UiKit.RR, new Color(0, 0, 0, 0.45f), 7).RT();
            track.anchorMin = Vector2.zero; track.anchorMax = new Vector2(1, 0); track.pivot = new Vector2(0.5f, 0);
            track.offsetMin = new Vector2(left, y); track.offsetMax = new Vector2(-right, y + 14);
            fillImg = UiKit.Img(track, "Fill", UiKit.RR, c, 7);
            var fr = fillImg.RT();
            fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0.5f, 1);
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            return fr;
        }

        void BuildDeck()
        {
            deckRt = UiKit.Rect("Deck", safe).At(Vector2.zero, new Vector2(26, tickerHeight + 14), new Vector2(CardView.W * deckScale, CardView.H * deckScale));
            var stack = UiKit.Rect("Stack", deckRt).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardView.W, CardView.H));
            stack.localScale = Vector3.one * deckScale;
            for (int i = 2; i >= 0; i--)
            {
                var b = CardView.Create(stack, null);
                b.Rt.anchoredPosition = new Vector2(-i * 9, -i * 12);
                b.group.blocksRaycasts = false;
                deckTop = b.Rt;
            }
            var ring = UiKit.Img(deckRt, "Badge", UiKit.Circle, Color.white);
            ring.RT().At(Vector2.one, new Vector2(4, 4), new Vector2(50, 50), new Vector2(0.5f, 0.5f));
            UiKit.Img(ring.transform, "In", UiKit.Circle, UiKit.Red).RT().Fill(4, 4, 4, 4);
            deckCount = UiKit.Txt(ring.transform, "N", "0", 24, Color.white, TextAlignmentOptions.Center, true);
            deckCount.RT().Fill();
        }

        void BuildRenew()
        {
            renewRt = UiKit.Panel(safe, "Renew", 20, true).At(new Vector2(1, 0), new Vector2(-24, tickerHeight + 14), new Vector2(140, 172));
            renewRt.Find("Inner").GetComponent<Image>().color = UiKit.Hex("#1e56b8");
            renewIcon = UiKit.Img(renewRt, "Icon", UiKit.Refresh, Color.white).RT().At(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(74, 74), new Vector2(0.5f, 1));
            renewIcon.pivot = new Vector2(0.5f, 0.5f); renewIcon.anchoredPosition = new Vector2(0, -51);
            var rl = UiKit.Txt(renewRt, "Label", "CAMBIAR\nCARTAS", 21, Color.white, TextAlignmentOptions.Center, true);
            rl.lineSpacing = -14;
            rl.RT().At(new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(136, 46));
            var pill = UiKit.Img(renewRt, "Cost", UiKit.RR, new Color(0, 0, 0, 0.4f), 14).RT().At(new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(118, 38));
            UiKit.Coin(pill, 28).At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(28, 28));
            renewCostTxt = UiKit.Txt(pill, "T", "$" + GameState.Money(deck.renewCost), 24, Color.white, TextAlignmentOptions.Center, true);
            renewCostTxt.RT().Fill(32, 4, 0, 0);
            var t = renewRt.gameObject.AddComponent<Tap>();
            t.visual = renewRt; t.onTap = OnRenew;
        }

        void BuildPlaceBanner()
        {
            placeBanner = UiKit.Panel(safe, "PlaceBanner", 18).At(new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(720, 66)); // debajo de MANDATO + GOBERNABILIDAD
            bannerGroup = placeBanner.gameObject.AddComponent<CanvasGroup>();
            placeTxt = UiKit.Txt(placeBanner, "T", "", 26, Color.white, TextAlignmentOptions.Center, true);
            placeTxt.RT().Fill(18, 80, 0, 0);
            placeTxt.enableAutoSizing = true; placeTxt.fontSizeMin = 18; placeTxt.fontSizeMax = 26;
            var x = CloseButton(placeBanner, 52);
            x.At(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(64, 64), new Vector2(1, 0.5f));
            x.GetComponent<Tap>().onTap = CancelPlacement;
            placeBanner.gameObject.SetActive(false);
        }

        void BuildZoomLayer()
        {
            zoomLayer = UiKit.Rect("Zoom", root).Fill();
            dim = UiKit.Img(zoomLayer, "Dim", null, new Color(0, 0, 0, 0), 0, true);
            dim.RT().Fill();
            dim.gameObject.AddComponent<Tap>().onTap = CancelZoom;
            dim.gameObject.SetActive(false);

            zoomUi = UiKit.Rect("ZoomUI", zoomLayer).Fill();
            zoomUiGroup = zoomUi.gameObject.AddComponent<CanvasGroup>();
            zoomUiGroup.alpha = 0; zoomUiGroup.blocksRaycasts = false;

            activateBtn = UiKit.Rect("Activate", zoomUi).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 96));
            activateBase = UiKit.Img(activateBtn, "Base", UiKit.RR, UiKit.Hex("#1d6b2a"), 24, true);
            activateBase.RT().Fill();
            activateFace = UiKit.Img(activateBtn, "Face", UiKit.RR, UiKit.Hex("#43c157"), 24);
            activateFace.RT().Fill(0, 0, 0, 8);
            var g = UiKit.Img(activateFace.transform, "Gloss", UiKit.RR, new Color(1, 1, 1, 0.18f), 22).RT();
            g.anchorMin = new Vector2(0, 0.5f); g.anchorMax = Vector2.one; g.offsetMin = new Vector2(4, 0); g.offsetMax = new Vector2(-4, -4);
            activateTxt = UiKit.Txt(activateFace.transform, "T", "ACTIVAR", 42, Color.white, TextAlignmentOptions.Center, true);
            activateTxt.RT().Fill();
            var at = activateBtn.gameObject.AddComponent<Tap>();
            at.visual = activateBtn; at.onTap = OnActivate;

            closeBtn = CloseButton(zoomUi, 84);
            closeBtn.GetComponent<Tap>().onTap = CancelZoom;
        }

        // X circular roja con área de toque generosa (≥ 120 unidades).
        RectTransform CloseButton(Transform parent, float size)
        {
            var hit = UiKit.Img(parent, "Close", null, new Color(0, 0, 0, 0), 0, true).RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 36, size + 36));
            var o = UiKit.Img(hit, "Ring", UiKit.Circle, Color.white).RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            UiKit.Img(o, "In", UiKit.Circle, UiKit.Red).RT().Fill(size * 0.07f, size * 0.07f, size * 0.07f, size * 0.07f);
            for (int i = 0; i < 2; i++)
            {
                var bar = UiKit.Img(o, "X" + i, null, Color.white).RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.52f, size * 0.12f));
                bar.localRotation = Quaternion.Euler(0, 0, i == 0 ? 45 : -45);
            }
            var t = hit.gameObject.AddComponent<Tap>();
            t.visual = o;
            return hit;
        }

        void BuildPause()
        {
            // fondo oscuro que bloquea el juego (no cierra al tocar: para eso está CONTINUAR)
            var bg = UiKit.Img(root, "PauseOverlay", null, new Color(0.02f, 0.04f, 0.1f, 0.78f), 0, true);
            bg.RT().Fill();
            // contenedor con el título PAUSA afuera, arriba; los dos se animan juntos (hijos de un grupo)
            var group = UiKit.Rect("Group", bg.transform).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            UiKit.Txt(group, "T", "PAUSA", 104, Color.white, TextAlignmentOptions.Center, true).RT()
                .At(new Vector2(0.5f, 0.5f), new Vector2(0, 262), new Vector2(700, 130)); // título PAUSA
            var panel = UiKit.Panel(group, "Panel", 28, true).At(new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(700, 430));
            VolumeSlider.Create(panel, "MÚSICA", new Vector2(0.5f, 1), new Vector2(0, -52), 600, () => GameAudio.Music, GameAudio.SetMusic);
            VolumeSlider.Create(panel, "EFECTOS", new Vector2(0.5f, 1), new Vector2(0, -118), 600, () => GameAudio.Sfx, GameAudio.SetSfx);
            PauseButton(panel, "CONTINUAR", "#43c157", "#1d6b2a", new Vector2(0, -228), TogglePause);
            PauseButton(panel, "RENUNCIAR", "#d8452f", "#7a1f14", new Vector2(0, -344), Resign);
            pauseOverlay = bg.gameObject;
            pauseOverlay.SetActive(false);
        }

        // ------------------------------------------------------------------ loop

        void Update()
        {
            ApplySafeArea(false);

            float diff = gs.money - shownMoney;
            shownMoney = Mathf.Abs(diff) > gs.income * 0.6f + 2 ? Mathf.Lerp(shownMoney, gs.money, 1 - Mathf.Exp(-7 * Time.unscaledDeltaTime)) : gs.money;
            int m = Mathf.FloorToInt(shownMoney);
            if (m != lastMoney) { lastMoney = m; moneyTxt.text = "$ " + GameState.Money(m); }
            int inc = Mathf.RoundToInt(gs.income);
            if (inc != lastIncome) { lastIncome = inc; incomeTxt.text = "+$" + GameState.Money(inc) + "/s"; }

            if (gs.mandate != lastMandate)
            {
                if (lastMandate > 0) StartCoroutine(Pulse(mandatePanel));
                lastMandate = gs.mandate;
                mandateTxt.text = gs.mandate.ToString(); // sin límite de mandatos
            }
            mandateFill.anchorMax = new Vector2(Mathf.Max(0.03f, gs.MandateProgress), 1);

            shownPop = Mathf.Lerp(shownPop, gs.popularity, 1 - Mathf.Exp(-6 * Time.unscaledDeltaTime));
            int p = Mathf.RoundToInt(shownPop);
            if (p != lastPop) { lastPop = p; popTxt.text = p + "%"; }
            popFill.anchorMax = new Vector2(Mathf.Max(0.03f, shownPop / 100f), 1);
            if (gs.MoodIndex != lastMood)
            {
                lastMood = gs.MoodIndex;
                moodTxt.text = gs.Mood;
                moodBg.color = MoodColors[lastMood];
                popFillImg.color = MoodColors[lastMood];
            }

            int dc = deck.DrawCount;
            if (dc != lastDeck) { lastDeck = dc; deckCount.text = dc.ToString(); deckCount.color = dc == 0 ? UiKit.RedSoft : Color.white; }
            renewCostTxt.color = gs.CanAfford(deck.renewCost) && dc > 0 ? Color.white : UiKit.RedSoft;
            renewIcon.GetComponent<Image>().color = dc > 0 ? Color.white : new Color(1, 1, 1, 0.35f);

            foreach (var cv in cards)
                if (cv && cv.transform.parent == hand && cv.tap.interactable)
                    cv.SetDisabled(Blocked(cv.def));

            if (zoomed && !busy) RefreshActivate();
            UpdateShop();
            UpdateFlow();
            UpdateTicker();
        }

        void ApplySafeArea(bool force)
        {
            var sa = Screen.safeArea;
            var scr = new Vector2Int(Screen.width, Screen.height);
            if (force || sa != lastSafe || scr != lastScreen)
            {
                lastSafe = sa; lastScreen = scr;
                // Teléfonos (≥16:10) escalan por alto; tablets 4:3 por ancho para que entren las 5 cartas.
                scaler.matchWidthOrHeight = (float)scr.x / scr.y >= 1.6f ? 1f : 0f;
                safe.anchorMin = new Vector2(sa.xMin / scr.x, sa.yMin / scr.y);
                safe.anchorMax = new Vector2(sa.xMax / scr.x, sa.yMax / scr.y);
                safe.offsetMin = safe.offsetMax = Vector2.zero;
            }
            // Marquesina de lado a lado: fondo hasta el borde, contenido dentro del área segura.
            var r = root.rect;
            float bottom = sa.yMin / scr.y * r.height;
            tickerRt.sizeDelta = new Vector2(0, tickerHeight + bottom);
            tickerContent.offsetMin = new Vector2(sa.xMin / scr.x * r.width, tickerContent.offsetMin.y);
            tickerContent.offsetMax = new Vector2(-(scr.x - sa.xMax) / scr.x * r.width, tickerContent.offsetMax.y);
        }

        static bool NoFreeSlot() => PoliceSlots.I && !PoliceSlots.I.HasFree();
        // Sin slot libre y sin una defensa igual para mejorar.
        static bool Blocked(CardDef d) => d.UsesSlot && PoliceSlots.I && !PoliceSlots.I.HasFree(d.slotSize) && !Defense.AnyUpgradable(d);

        // ------------------------------------------------------------------ marquesina

        void OnHeadline(string h)
        {
            urgent.Enqueue(h);
            if (tickerIsFiller) tickerX = 1e6f; // corta el relleno: la noticia nueva entra ya
        }

        void UpdateTicker()
        {
            tickerX -= tickerSpeed * Time.unscaledDeltaTime;
            float w = tickerTxt.RT().sizeDelta.x;
            if (tickerX < -w || tickerX > 1e5f)
            {
                tickerIsFiller = urgent.Count == 0;
                string h = urgent.Count > 0 ? urgent.Dequeue() : Headlines.NextFiller();
                tickerTxt.text = h;
                tickerTxt.RT().sizeDelta = new Vector2(tickerTxt.GetPreferredValues(h).x + 60, 0);
                tickerX = tickerViewport.rect.width;
            }
            tickerTxt.RT().anchoredPosition = new Vector2(tickerX, 0);
            tickerDot.color = new Color(1, 1, 1, Mathf.PingPong(Time.unscaledTime * 1.6f, 1) > 0.35f ? 1 : 0.15f);
        }

        // ------------------------------------------------------------------ mano

        Vector3 SlotPos(int i)
        {
            float k = i - (cards.Length - 1) * 0.5f;
            return new Vector3(k * handSpacing, -k * k * 4f, 0);
        }

        float SlotZ(int i) => -(i - (cards.Length - 1) * 0.5f) * 2.2f;

        IEnumerator DealHand(float delay)
        {
            busy = true;
            yield return new WaitForSecondsRealtime(delay);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i]) continue;
                StartCoroutine(DrawInto(i));
                yield return new WaitForSecondsRealtime(0.11f);
            }
            yield return new WaitForSecondsRealtime(0.55f);
            busy = false;
        }

        // Saca una carta del mazo: sale boca abajo, describe un arco y gira 180° en Y hasta quedar en su lugar.
        IEnumerator DrawInto(int i, CardInst forced = null)
        {
            var inst = forced ?? deck.Draw();
            if (inst == null) { Headlines.Say("deck_empty", 90f); yield break; } // se pueden comprar (GameHudShop)
            var def = inst.def;
            Snd.Ui("card_draw", 1, Random.Range(0.94f, 1.08f), 0.02f);
            var cv = CardView.Create(hand, def, inst.level);
            cv.slot = i;
            cards[i] = cv;
            cv.tap.onTap = () => OnCardTap(cv);
            cv.tap.interactable = false;
            if (def.kind == CardKind.Colocar) AddDrag(cv);
            Vector3 from = hand.InverseTransformPoint(deckTop.position), to = SlotPos(i);
            float zEnd = SlotZ(i);
            yield return UiKit.Tween(0.55f, t =>
            {
                float e = Ease.OutCubic(t), arc = Mathf.Sin(t * Mathf.PI);
                cv.transform.localPosition = Vector3.Lerp(from, to, e) + Vector3.up * arc * 150f;
                cv.transform.localScale = Vector3.one * Mathf.LerpUnclamped(deckScale, handScale, Ease.OutBack(t));
                cv.transform.localRotation = Quaternion.Euler(-arc * 20f, Mathf.Lerp(180, 0, Ease.InOutCubic(t)), Mathf.Lerp(-14, zEnd, e));
            });
            if (cv) cv.tap.interactable = true;
        }

        void OnCardTap(CardView cv)
        {
            if (busy || zoomed || placing || paused) return;
            if (gs.phase != Phase.Previa && gs.phase != Phase.Oleada) return;
            if (cv.Disabled) { StartCoroutine(Shake(cv.Rt)); return; }
            StartCoroutine(Zoom(cv));
        }

        Vector3 ZoomPos => new Vector3(0, 62, 0);

        IEnumerator Zoom(CardView cv)
        {
            busy = true;
            zoomed = cv;
            Snd.Ui("card_zoom");
            // colocable: las defensas iguales que se pueden mejorar titilan; el fondo se oscurece menos para verlas
            if (cv.def.kind == CardKind.Colocar) Defense.ShowHints(cv.def, true);
            dimA = Defense.AnyHint ? 0.38f : 0.68f;
            cv.dimWhenPoor = false;
            dim.gameObject.SetActive(true);
            cv.transform.SetParent(zoomLayer, true);
            cv.transform.SetSiblingIndex(dim.transform.GetSiblingIndex() + 1);
            Vector3 from = cv.transform.localPosition, to = ZoomPos;
            float s0 = cv.transform.localScale.x, z0 = UiKit.SignedAngle(cv.transform.localEulerAngles.z);
            yield return UiKit.Tween(0.34f, t =>
            {
                float e = Ease.OutCubic(t), arc = Mathf.Sin(t * Mathf.PI);
                dim.color = new Color(0, 0, 0, dimA * e);
                cv.transform.localPosition = Vector3.Lerp(from, to, e);
                cv.transform.localScale = Vector3.one * Mathf.LerpUnclamped(s0, zoomScale, Ease.OutBack(t));
                cv.transform.localRotation = Quaternion.Euler(-arc * 10f, -arc * 22f, Mathf.Lerp(z0, 0, e));
            });
            float hw = CardView.W * zoomScale * 0.5f, hh = CardView.H * zoomScale * 0.5f;
            closeBtn.anchoredPosition = new Vector2(to.x + hw - 4, to.y + hh - 4);
            activateBtn.anchoredPosition = new Vector2(to.x, to.y - hh - 62);
            RefreshActivate();
            zoomUiGroup.blocksRaycasts = true;
            yield return UiKit.Tween(0.18f, t =>
            {
                zoomUiGroup.alpha = t;
                activateBtn.anchoredPosition = new Vector2(to.x, to.y - hh - 62 - (1 - Ease.OutCubic(t)) * 30);
            });
            busy = false;
        }

        void RefreshActivate()
        {
            if (zoomed.def.kind == CardKind.Crisis)
            {
                activateTxt.text = L.T("ACEPTAR"); activateTxt.fontSize = 42;
                activateFace.color = UiKit.Hex("#d9534a"); activateBase.color = UiKit.Hex("#7a231d");
                return;
            }
            bool locked = zoomed.def.previaOnly && gs.phase != Phase.Previa;
            bool blocked = Blocked(zoomed.def), ok = !blocked && !locked && gs.CanAfford(zoomed.def.cost);
            activateTxt.text = ok ? L.T("ACTIVAR") : locked ? L.T("SOLO EN PREVIA") : blocked ? L.T("SIN LUGAR") : L.T("FALTA $") + GameState.Money(zoomed.def.cost - gs.money + 0.999f);
            activateTxt.fontSize = ok ? 42 : 34;
            activateFace.color = ok ? UiKit.Hex("#43c157") : UiKit.Hex("#7b8494");
            activateBase.color = ok ? UiKit.Hex("#1d6b2a") : UiKit.Hex("#4a5160");
        }

        void HideZoomButtons()
        {
            zoomUiGroup.alpha = 0;
            zoomUiGroup.blocksRaycasts = false;
        }

        void CancelZoom()
        {
            if (busy || !zoomed) return;
            StartCoroutine(Unzoom(zoomed));
        }

        IEnumerator Unzoom(CardView cv)
        {
            busy = true;
            HideZoomButtons();
            Snd.Ui("card_flip", 0.7f);
            Defense.ShowHints(null, false);
            Vector3 from = cv.transform.localPosition, to = zoomLayer.InverseTransformPoint(hand.TransformPoint(SlotPos(cv.slot)));
            float zEnd = SlotZ(cv.slot);
            yield return UiKit.Tween(0.26f, t =>
            {
                float e = Ease.OutCubic(t), arc = Mathf.Sin(t * Mathf.PI);
                dim.color = new Color(0, 0, 0, dimA * (1 - e));
                cv.transform.localPosition = Vector3.Lerp(from, to, e);
                cv.transform.localScale = Vector3.one * Mathf.Lerp(zoomScale, handScale, e);
                cv.transform.localRotation = Quaternion.Euler(arc * 8f, arc * 18f, Mathf.Lerp(0, zEnd, e));
            });
            ReturnToHand(cv);
            dim.gameObject.SetActive(false);
            zoomed = null;
            busy = false;
        }

        void ReturnToHand(CardView cv)
        {
            cv.transform.SetParent(hand, false);
            cv.transform.localPosition = SlotPos(cv.slot);
            cv.transform.localRotation = Quaternion.Euler(0, 0, SlotZ(cv.slot));
            cv.transform.localScale = Vector3.one * handScale;
            cv.group.alpha = 1;
            cv.dimWhenPoor = true;
            cv.RefreshAfford();
        }

        // ------------------------------------------------------------------ activar

        void OnActivate()
        {
            if (busy || !zoomed) return;
            if (zoomed.def.kind == CardKind.Crisis) { StartCoroutine(PlayEffect(zoomed, true)); return; }
            if (!gs.CanAfford(zoomed.def.cost) || Blocked(zoomed.def) || (zoomed.def.previaOnly && gs.phase != Phase.Previa)) { StartCoroutine(Shake(activateBtn)); return; }
            StartCoroutine(zoomed.def.kind == CardKind.Colocar ? BeginPlacement(zoomed) : PlayEffect(zoomed));
        }

        IEnumerator PlayEffect(CardView cv, bool free = false)
        {
            busy = true;
            var def = cv.def;
            HideZoomButtons();
            if (!free) gs.Spend(def.cost);
            Snd.Ui("card_play", 0.8f);
            if (!free && def.cost > 0) Snd.Ui("spend", 0.8f);
            if (def.cost > 0 && !free) FloatText(moneyPanel, "-$" + GameState.Money(def.cost), UiKit.RedSoft, 0);
            yield return UiKit.Tween(0.14f, t => cv.transform.localScale = Vector3.one * zoomScale * (1 + Mathf.Sin(t * Mathf.PI) * 0.07f));

            var target = def.popularityDelta != 0 ? popPanel : moneyPanel;
            Vector3 from = cv.transform.localPosition, to = zoomLayer.InverseTransformPoint(target.TransformPoint(target.rect.center));
            yield return UiKit.Tween(0.45f, t =>
            {
                float e = Ease.InCubic(t);
                dim.color = new Color(0, 0, 0, dimA * (1 - Ease.OutCubic(t)));
                cv.transform.localPosition = Vector3.Lerp(from, to, e);
                cv.transform.localScale = Vector3.one * Mathf.Lerp(zoomScale, 0.06f, e);
                cv.transform.localRotation = Quaternion.Euler(0, e * 360f, e * 25f);
                cv.group.alpha = 1 - Mathf.Clamp01((t - 0.65f) / 0.35f);
            });
            if (!free) Achievements.Count("play_" + def.id);
            if (def.kind == CardKind.Ministro) { var gone = gs.AddMinister(def); if (gone) Headlines.Say("minister_swap", L.CardTitle(gone), L.CardTitle(def)); }
            else gs.ApplyEffect(def);
            StartCoroutine(Pulse(target));
            EffectSound(def);
            FloatDeltas(def);

            int slot = cv.slot; // la carta se consume: el mazo no se rebaraja
            cards[slot] = null;
            Destroy(cv.gameObject);
            dim.gameObject.SetActive(false);
            zoomed = null;
            yield return new WaitForSecondsRealtime(0.15f);
            yield return DrawInto(slot);
            busy = false;
        }

        // Lo que suena cuando la carta impacta en el panel (la curación suena en Defense.HealAll).
        void EffectSound(CardDef d)
        {
            if (d.kind == CardKind.Ministro) Snd.Ui("stamp", 0.85f);
            if (d.kind == CardKind.Crisis) Snd.Ui("mood_down", 0.7f, 1, 0.5f);
            if (d.moneyDelta > 0) Snd.Ui("coin");
            if (d.popularityDelta > 0.01f) Snd.Ui("pop_up");
            else if (d.popularityDelta < -0.01f) Snd.Ui("boo"); // la gente se enoja
            else if (d.moneyMultiplier > 0 && d.moneyMultiplier < 1) Snd.Ui("pop_down");
        }

        float dimA = 0.68f; // oscurecido del zoom

        IEnumerator SndLater(string id, float delay, float vol = 1)
        {
            yield return new WaitForSecondsRealtime(delay);
            Snd.Ui(id, vol, 1, 0);
        }

        void FloatDeltas(CardDef d)
        {
            if (d.moneyDelta != 0) FloatText(moneyPanel, (d.moneyDelta > 0 ? "+$" : "-$") + GameState.Money(d.moneyDelta), d.moneyDelta > 0 ? UiKit.Green : UiKit.RedSoft, 0.05f);
            if (d.incomeDelta != 0) FloatText(moneyPanel, (d.incomeDelta > 0 ? "+$" : "-$") + GameState.Money(d.incomeDelta) + "/s", d.incomeDelta > 0 ? UiKit.Green : UiKit.RedSoft, 0.35f);
            if (d.popularityDelta != 0) FloatText(popPanel, (d.popularityDelta > 0 ? "+" : "-") + Mathf.Abs(d.popularityDelta).ToString("0") + "%", d.popularityDelta > 0 ? UiKit.Green : UiKit.RedSoft, 0.05f);
        }

        // ------------------------------------------------------------------ colocar en el mapa

        IEnumerator BeginPlacement(CardView cv)
        {
            busy = true;
            HideZoomButtons();
            placing = cv;
            zoomed = null;
            Snd.Ui("card_zoom", 0.8f, 0.8f);
            Vector3 from = cv.transform.localPosition, to = zoomLayer.InverseTransformPoint(placeBanner.position);
            yield return UiKit.Tween(0.3f, t =>
            {
                float e = Ease.InCubic(t);
                dim.color = new Color(0, 0, 0, dimA * (1 - Ease.OutCubic(t)));
                cv.transform.localPosition = Vector3.Lerp(from, to, e);
                cv.transform.localScale = Vector3.one * Mathf.Lerp(zoomScale, 0.12f, e);
                cv.group.alpha = 1 - e;
            });
            cv.gameObject.SetActive(false);
            dim.gameObject.SetActive(false);
            placeTxt.text = L.T(Defense.AnyUpgradable(cv.def) ? "Elegí un lugar o tocá una igual para mejorarla: " : "Elegí un lugar en el camino para ") + "<color=#f2c14e>" + L.CardTitle(cv.def).ToUpperInvariant() + "</color>";
            if (PoliceSlots.I) PoliceSlots.I.SetHighlight(true);
            Defense.ShowHints(cv.def, true);
            placeBanner.gameObject.SetActive(true);
            mapCatcher.gameObject.SetActive(true);
            yield return UiKit.Tween(0.2f, t => bannerGroup.alpha = t);
            busy = false;
        }

        void OnMapTap(Vector2 screen)
        {
            if (!placing || busy || paused) return;
            if (!GroundPoint(screen, out var p)) return;
            if (!TryUsePlaceCard(placing, p)) { StartCoroutine(Shake(placeBanner)); return; }
            placing = null;
            HidePlacement();
        }

        bool GroundPoint(Vector2 screen, out Vector3 p)
        {
            p = default;
            var cam = Camera.main;
            if (!cam) return false;
            var ray = cam.ScreenPointToRay(screen);
            if (!new Plane(Vector3.up, new Vector3(0, groundY, 0)).Raycast(ray, out float d)) return false;
            p = ray.GetPoint(d);
            return true;
        }

        // Lo que haría una carta colocable soltada en p: mejorar una defensa igual (prioridad) o ocupar el slot libre más cercano.
        void PlaceTarget(CardDef d0, Vector3 p, out Defense up, out PoliceSlots.Slot target, out PoliceSlots.Slot second)
        {
            up = Defense.UpgradableAt(d0, p, 1.2f);
            target = second = null;
            if (up || !PoliceSlots.I) return;
            if (d0.slotSize >= 2) target = PoliceSlots.I.NearestPair(p, slotSnapDistance + 2f, out second);
            else target = PoliceSlots.I.Nearest(p, slotSnapDistance);
        }

        // Usa la carta en p (cobra, coloca o mejora, consume la carta y roba otra). false = no había dónde.
        bool TryUsePlaceCard(CardView cv, Vector3 p)
        {
            var d0 = cv.def;
            PlaceTarget(d0, p, out var up, out var target, out var second);
            if (!up && target == null) return false;
            if (!gs.Spend(d0.cost))
            {
                FloatText(moneyPanel, L.T("FALTA $") + GameState.Money(d0.cost - gs.money + 0.999f), UiKit.RedSoft, 0);
                StartCoroutine(Shake(moneyPanel));
                return false;
            }
            if (d0.cost > 0) { FloatText(moneyPanel, "-$" + GameState.Money(d0.cost), UiKit.RedSoft, 0); Snd.Ui("spend", 0.6f); }
            Achievements.Count("place_" + d0.id);
            if (d0.defense == DefenseKind.BalasGoma || d0.defense == DefenseKind.Lanzagases) gs.usedRepression = true;
            if (up)
            {
                up.Upgrade();
                WorldFloat(up.transform.position + Vector3.up * 5.5f, L.F("¡NIVEL {0}!", up.level), UiKit.Gold);
                Headlines.Say("upgrade", L.CardTitle(d0).ToLowerInvariant(), up.level);
            }
            else
            {
                var placed = Defense.Place(d0, cv.level, target, second).gameObject;
                PoliceSlots.I.Occupy(target, placed);
                if (second != null) PoliceSlots.I.Occupy(second, placed);
                if (d0.defense == DefenseKind.Puntero && Defense.Count(DefenseKind.Puntero) >= 3) Achievements.Unlock("aparato");
                if (!string.IsNullOrEmpty(d0.headline)) gs.PushHeadline(Headlines.PickVariant(L.CardHeadline(d0)));
            }
            int slot = cv.slot;
            cards[slot] = null;
            Destroy(cv.gameObject);
            StartCoroutine(DrawInto(slot));
            return true;
        }

        void HidePlacement()
        {
            placeBanner.gameObject.SetActive(false);
            mapCatcher.gameObject.SetActive(false);
            if (PoliceSlots.I) PoliceSlots.I.SetHighlight(false);
            Defense.ShowHints(null, false);
        }

        void CancelPlacement()
        {
            if (!placing || busy) return;
            StartCoroutine(ReturnFromPlacement());
        }

        IEnumerator ReturnFromPlacement()
        {
            busy = true;
            var cv = placing;
            placing = null;
            Snd.Ui("card_flip", 0.7f);
            Vector3 from = hand.InverseTransformPoint(placeBanner.position);
            HidePlacement();
            cv.gameObject.SetActive(true);
            cv.transform.SetParent(hand, false);
            float zEnd = SlotZ(cv.slot);
            yield return UiKit.Tween(0.38f, t =>
            {
                float e = Ease.OutCubic(t);
                cv.transform.localPosition = Vector3.Lerp(from, SlotPos(cv.slot), e);
                cv.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.12f, handScale, Ease.OutBack(t));
                cv.transform.localRotation = Quaternion.Euler(0, (1 - e) * 180f, Mathf.Lerp(0, zEnd, e));
                cv.group.alpha = Mathf.Clamp01(t * 3);
            });
            ReturnToHand(cv);
            busy = false;
        }

        // Placeholder hasta que existan los prefabs de defensa.
        GameObject Spawn(CardDef def, Vector3 p, float yaw)
        {
            var parentGo = GameObject.Find("Defenses");
            if (!parentGo) parentGo = new GameObject("Defenses");
            GameObject go;
            var rot = Quaternion.Euler(0, yaw, 0);
            if (def.placePrefab) go = Instantiate(def.placePrefab, p, rot);
            else
            {
                Vector3 size;
                switch (def.id)
                {
                    case "vallado": size = new Vector3(0.5f, 1.1f, 4.4f); break;
                    case "hidrante": size = new Vector3(4f, 2.4f, 2.2f); break;
                    case "caballeria": size = new Vector3(2.6f, 2.4f, 0.9f); break;
                    case "refuerzo": size = new Vector3(1.2f, 1.8f, 4f); break;
                    default: size = Vector3.one; break;
                }
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.transform.SetPositionAndRotation(p + Vector3.up * size.y * 0.5f, rot);
                go.transform.localScale = size;
                var r = go.GetComponent<Renderer>();
                r.material.color = def.color;
            }
            go.name = def.id;
            go.transform.SetParent(parentGo.transform, true);
            StartCoroutine(PopIn(go.transform));
            return go;
        }

        IEnumerator PopIn(Transform t)
        {
            var s = t.localScale;
            yield return UiKit.Tween(0.35f, k => { if (t) t.localScale = s * Mathf.Max(0, Ease.OutBack(k)); });
        }

        // ------------------------------------------------------------------ renovar

        void OnRenew()
        {
            if (busy || zoomed || placing || paused || (gs.phase != Phase.Previa && gs.phase != Phase.Oleada)) return;
            // sin cartas en el mazo te devolvería las mismas: no se cobra
            if (deck.DrawCount < 1) { StartCoroutine(Shake(renewRt)); FloatText(renewRt, L.T("MAZO VACÍO"), UiKit.RedSoft, 0); Snd.Ui("ui_error"); return; }
            if (!gs.CanAfford(deck.renewCost)) { StartCoroutine(Shake(renewRt)); return; }
            gs.Spend(deck.renewCost);
            FloatText(moneyPanel, "-$" + GameState.Money(deck.renewCost), UiKit.RedSoft, 0);
            StartCoroutine(RenewRoutine());
        }

        IEnumerator RenewRoutine()
        {
            busy = true;
            Headlines.Say("renew", 20f);
            Snd.Ui("card_shuffle");
            Snd.Ui("spend", 0.6f);
            StartCoroutine(UiKit.Tween(0.7f, t => renewIcon.localRotation = Quaternion.Euler(0, 0, -Ease.InOutCubic(t) * 360f)));
            Vector3 target = hand.InverseTransformPoint(renewRt.TransformPoint(renewRt.rect.center));
            for (int i = cards.Length - 1; i >= 0; i--)
            {
                var cv = cards[i];
                if (!cv || cv.def.kind == CardKind.Crisis) continue; // la crisis no se va renovando
                cards[i] = null;
                deck.ReturnToDeck(new CardInst(cv.def, cv.level)); // vuelve al fondo del mazo
                StartCoroutine(FlyAway(cv, target));
                yield return new WaitForSecondsRealtime(0.06f);
            }
            yield return new WaitForSecondsRealtime(0.3f);
            busy = false;
            yield return DealHand(0);
        }

        IEnumerator FlyAway(CardView cv, Vector3 to)
        {
            cv.tap.interactable = false;
            Vector3 from = cv.transform.localPosition;
            float z0 = UiKit.SignedAngle(cv.transform.localEulerAngles.z);
            yield return UiKit.Tween(0.4f, t =>
            {
                float e = Ease.InCubic(t);
                cv.transform.localPosition = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 90f;
                cv.transform.localScale = Vector3.one * Mathf.Lerp(handScale, 0.12f, e);
                cv.transform.localRotation = Quaternion.Euler(0, e * 180f, Mathf.Lerp(z0, -30, e));
                cv.group.alpha = 1 - e * e;
            });
            Destroy(cv.gameObject);
        }

        // ------------------------------------------------------------------ varios

        void TogglePause()
        {
            paused = !paused;
            Time.timeScale = paused ? 0 : 1;
            AudioListener.pause = paused; // el mapa calla; la UI y la música siguen
            pauseOverlay.SetActive(paused);
            if (paused) StartCoroutine(UiKit.Tween(0.25f, t => { if (pauseOverlay) pauseOverlay.transform.Find("Group").localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Ease.OutBack(t)); }));
        }

        void PauseButton(RectTransform parent, string label, string faceHex, string baseHex, Vector2 pos, System.Action onTap)
        {
            var b = UiKit.Rect(label, parent).At(new Vector2(0.5f, 1), pos, new Vector2(460, 96), new Vector2(0.5f, 0.5f));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex(baseHex), 24, true).RT().Fill();
            var f = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex(faceHex), 24).RT().Fill(0, 0, 0, 9);
            UiKit.Txt(f, "T", label, 40, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            var t = b.gameObject.AddComponent<Tap>();
            t.visual = b; t.onTap = onTap;
        }

        // El presidente renuncia: se va en helicóptero y vuelve al menú.
        void Resign()
        {
            if (!paused) return;
            TogglePause();
            if (zoomed) CancelZoom();
            if (placing) CancelPlacement();
            if (GameFlow.I && GameFlow.I.state == GameFlow.State.Playing) GameFlow.I.Resign();
            else if (WaveDirector.I) WaveDirector.I.Lose();
        }

        void FloatText(RectTransform anchor, string s, Color c, float delay) => StartCoroutine(FloatRoutine(anchor, s, c, delay));

        IEnumerator FloatRoutine(RectTransform anchor, string s, Color c, float delay)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            var t = UiKit.Txt(fxLayer, "Float", s, 38, c, TextAlignmentOptions.Center, true);
            t.RT().sizeDelta = new Vector2(320, 50);
            Vector3 p0 = fxLayer.InverseTransformPoint(anchor.TransformPoint(new Vector3(anchor.rect.center.x, anchor.rect.yMin, 0))) + Vector3.down * 34;
            yield return UiKit.Tween(1.2f, k =>
            {
                t.transform.localPosition = p0 + Vector3.down * Ease.OutCubic(k) * 26;
                t.transform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) * 4));
                t.alpha = 1 - Mathf.Clamp01((k - 0.6f) / 0.4f);
            });
            Destroy(t.gameObject);
        }

        IEnumerator Pulse(RectTransform rt)
        {
            yield return UiKit.Tween(0.35f, t => rt.localScale = Vector3.one * (1 + Mathf.Sin(t * Mathf.PI) * 0.1f));
            rt.localScale = Vector3.one;
        }

        IEnumerator Shake(RectTransform rt)
        {
            Snd.Ui("ui_error");
            var basePos = rt.anchoredPosition;
            yield return UiKit.Tween(0.32f, t => rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 40f) * 12f * (1 - t), 0));
            rt.anchoredPosition = basePos;
        }
    }
}
