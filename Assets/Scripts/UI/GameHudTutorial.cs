using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Tutorial guiado por Don Anselmo, el presidente saliente (personaje ficticio). Corre sobre la partida
    // tutorial (mazo y oleadas fijas, ver Tutorial) con dos tipos de paso:
    //  Info: oscurece todo menos el elemento que explica y espera un toque.
    //  Acción: resalta el elemento sin bloquear y espera a que el jugador lo haga.
    // Va en su propio canvas (orden 25: arriba del HUD, abajo del menú).
    public partial class GameHud
    {
        // lo que miran los pasos (los suman GameHud, GameHudFlow y GameHudDrag)
        int tutSold, tutRenewed;
        bool tutAllowStart, tutFinal;
        readonly List<string> tutPlayed = new List<string>();

        RectTransform tutRoot, tutBox, tutFrame, tutArrow, tutFace, tutMouth, tutPortrait, tutSkip;
        RectTransform[] tutDims;
        Image[] tutDimImgs, tutBarImgs;
        CanvasGroup tutGroup, tutBoxGroup;
        CanvasScaler tutScaler;
        Image tutCatch;
        TextMeshProUGUI tutText, tutHint;
        Coroutine tutCo;
        Func<Rect?> tutTarget;
        bool tutTapped, tutTalking, tutBlocking, tutBoxOn;
        float tutDimA, tutDimShown, tutBoxY;
        float? tutBoxYOverride;

        const float TutBigW = 900, TutBigH = 224, TutSmallW = 620, TutSmallH = 200;

        public void BeginTutorial()
        {
            if (tutRoot) return;
            BuildTutorial();
            tutCo = StartCoroutine(TutorialRoutine());
        }

        // ------------------------------------------------------------------ guion

        IEnumerator TutorialRoutine()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            while (busy) yield return null;

            yield return Info("¡Bienvenido a la presidencia! Soy Don Anselmo, el presidente saliente. Antes de tomarme el helicóptero… digo, las vacaciones, te explico cómo se gobierna este país.",
                "Welcome to the presidency! I'm Don Anselmo, the outgoing president. Before I catch my helicopter… I mean, my vacation, let me show you how to run this country.");
            yield return Info("Cada MANDATO es una oleada de manifestantes que entra por Avenida de Mayo y marcha hacia la Casa Rosada. Tu trabajo: aguantar mandato tras mandato.",
                "Each TERM is a wave of protesters coming down Avenida de Mayo toward the Casa Rosada. Your job: hold on, term after term.", Of(mandatePanel));
            yield return Info("Esta es la GOBERNABILIDAD. Cada manifestante que llega a la reja de la Casa Rosada te saca un poco. Si llega a cero… helicóptero. Creeme, sé de lo que hablo.",
                "This is GOVERNABILITY. Every protester who reaches the Casa Rosada fence takes a bit away. If it hits zero… helicopter. Trust me, I know.", Of(govPanel));
            yield return Info("La POPULARIDAD marca cuánta gente viene y qué tan enojada: TRANQUI, TENSO, CALLE CALIENTE y ESTALLIDO. Cuanto más baja, más difícil.",
                "POPULARITY sets how many people come and how angry they are: CALM, TENSE, HEATED STREETS and UPRISING. The lower it is, the harder it gets.", Of(popPanel));
            yield return Info("Con el PRESUPUESTO pagás las cartas. Durante la oleada entra plata por segundo, y al cerrar cada mandato cobrás el sueldo, que sube con la popularidad.",
                "Your BUDGET pays for cards. Money trickles in every second during the wave, and at the end of each term you get your salary, which grows with popularity.", Of(moneyPanel));
            yield return Info("Acá ves quiénes vienen en la próxima oleada; los especiales salen en rojo. En el mapa, la flecha roja marca por qué camino van a entrar.",
                "Here you can see who's coming in the next wave; special units show in red. On the map, the red arrow marks the path they'll take.", Of(previewPanel));
            yield return Info("Estas son tus CARTAS. Cuestan plata y se gastan al jugarlas. Tocá una para verla en grande, o arrastrala directo al mapa.",
                "These are your CARDS. They cost money and are used up when played. Tap one to see it up close, or drag it straight onto the map.", HandRect);

            yield return Act("Arrastrá el VALLADO al camino de la flecha roja. Frena a todos los que llegan, hasta que lo rompen.",
                "Drag the BARRICADE onto the path with the red arrow. It stops everyone who reaches it, until they break it.",
                () => CardRect("vallado"), () => Defense.Count(DefenseKind.Vallado) >= 1, () => !InHand("vallado"));
            yield return Act("¡Así se hace! Te queda otro VALLADO: soltalo encima del que pusiste y sube de nivel (hasta el 3).",
                "That's it! You have another BARRICADE: drop it on top of the one you placed to level it up (up to 3).",
                () => CardRect("vallado"), AnyUpgraded, () => !InHand("vallado") || Defense.Count(DefenseKind.Vallado) >= 2);
            yield return Act("Ahora la INFANTERÍA: pelea cuerpo a cuerpo. Ponela adelante del vallado, más cerca de donde entran (hacia la izquierda).",
                "Now the RIOT SQUAD: it fights hand to hand. Place it in front of the barricade, closer to where they come in (to the left).",
                () => CardRect("infanteria"), () => Defense.Count(DefenseKind.Infanteria) >= 1, () => !InHand("infanteria"));

            yield return Info("Uy, la caja quedó flaca. Para eso están las medidas impopulares: dan plata, pero bajan la popularidad.",
                "Oops, the treasury is running thin. That's what unpopular measures are for: they bring in money, but popularity drops.", Of(moneyPanel));
            yield return Act("Jugá el IMPUESTAZO: soltalo en cualquier lado (o tocalo y ACTIVAR).",
                "Play the TAX HIKE: drop it anywhere (or tap it and PLAY CARD).",
                () => CardRect("impuestazo"), () => tutPlayed.Contains("impuestazo"), () => !InHand("impuestazo"));
            if (tutPlayed.Contains("impuestazo"))
            {
                yield return new WaitForSecondsRealtime(0.9f);
                yield return Info("¿Viste? Entró plata, pero la popularidad bajó y la próxima oleada creció. En este país todo tiene un precio.",
                    "See? Money came in, but popularity dropped and the next wave got bigger. In this country everything has a price.", Of(popPanel));
            }
            yield return Act("Para levantarla, jugá CADENA NACIONAL. Nada como hablarle al pueblo en horario central.",
                "To win them back, play NATIONAL ADDRESS. Nothing like talking to the people in prime time.",
                () => CardRect("cadena_nacional"), () => tutPlayed.Contains("cadena_nacional"), () => !InHand("cadena_nacional"));

            yield return Act("Las cartas que no te sirven se VENDEN: abrí una y tocá el botón $ de arriba a la izquierda, o arrastrala sobre el mazo. Probá vender una.",
                "Cards you don't need can be SOLD: open one and tap the $ button at the top left, or drag it onto the deck. Try selling one.",
                HandRect, () => tutSold >= 1);
            yield return Info(
                L.F("CAMBIAR CARTAS devuelve toda la mano al fondo del mazo y te da cinco nuevas por ${0}. Sirve cuando la mano no te cierra.", GameState.Money(deck.renewCost)),
                L.F("SWAP CARDS sends your whole hand to the bottom of the deck and deals five new ones for ${0}. Handy when your hand doesn't fit.", GameState.Money(deck.renewCost)), Of(renewRt));
            yield return Info("El MAZO no se rebaraja: lo que jugás o vendés, se gasta. Al cerrar cada mandato ganás 3 cartas nuevas, y si se vacía vas a poder comprar cartas al azar ahí arriba.",
                "The DECK is never reshuffled: whatever you play or sell is gone. Each term you earn 3 new cards, and if it runs out you can buy random cards right above it.", Of(deckRt));

            tutAllowStart = true;
            yield return Act("Cuando tengas la defensa lista, tocá ¡ARRANCAR! En una partida normal tenés 30 segundos para decidir; hoy no hay apuro.",
                "When your defense is ready, tap START! In a normal game you get 30 seconds to decide; today there's no rush.",
                Of(startBtn), () => gs.phase == Phase.Oleada);

            // primera oleada
            float wait = 0;
            while (gs.phase == Phase.Oleada && wait < 4.5f) { wait += Time.unscaledDeltaTime; yield return null; }
            if (gs.phase == Phase.Oleada)
                yield return Info("¡Ahí vienen! La barra del mandato muestra cuánto falta. Mientras dura la oleada entra plata y podés seguir jugando cartas.",
                    "Here they come! The term bar shows how much is left. While the wave lasts money keeps coming in and you can keep playing cards.", Of(mandatePanel));
            TutHide();
            while (gs.phase == Phase.Oleada) yield return null;

            // cierre del mandato 1
            while (!(overlay && rewards.Count > 0)) yield return null;
            yield return new WaitForSecondsRealtime(1.4f);
            yield return Info("¡Sobreviviste tu primer mandato! Cobraste el sueldo, la policía se curó un poco y ganaste 3 cartas para el mazo. Si alguna no te gusta, CAMBIAR te da otra (una vez por carta). Después tocá CONTINUAR.",
                "You survived your first term! You got your salary, the police healed a bit and you earned 3 cards for your deck. If you don't like one, SWAP gives you another (once per card). Then tap CONTINUE.",
                RewardsRect, boxY: 262);
            TutHide();

            // mandato 2: crisis
            while (!(gs.mandate >= 2 && gs.phase == Phase.Previa)) yield return null;
            tutAllowStart = false;
            wait = 0;
            while ((busy || flowBusy || CrisesInHand().Count == 0) && wait < 8f) { wait += Time.unscaledDeltaTime; yield return null; }
            if (CrisesInHand().Count > 0)
            {
                yield return Info("¡Uh! Una CRISIS. Desde el segundo mandato entran solas a la mano. Si arrancás con ella en la mano, se aplica: esta te baja la popularidad.",
                    "Uh-oh! A CRISIS. From the second term on they land in your hand by themselves. If you start with it in hand, it applies: this one cuts your popularity.", CrisisRect);
                yield return Act("Abrila: podés ACEPTARLA ya o EVITARLA pagando su costo. Vos decidís.",
                    "Open it: you can ACCEPT it now or AVOID it by paying its cost. Your call.",
                    CrisisRect, () => CrisesInHand().Count == 0);
            }
            yield return Info("Ahora entran por DOS caminos: fijate las flechas rojas del mapa y reforzá el nuevo con lo que tengas en la mano.",
                "Now they come in on TWO paths: check the red arrows on the map and reinforce the new one with what you have in hand.");
            tutAllowStart = true;
            yield return Act("Armá la defensa y ARRANCÁ cuando quieras.",
                "Set up your defense and START whenever you're ready.",
                Of(startBtn), () => gs.phase == Phase.Oleada);
            TutHide();

            // fin
            while (!tutFinal) yield return null;
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Final();
        }

        // Despedida con botón: vuelve al menú y Don Anselmo se va en el helicóptero.
        IEnumerator Final()
        {
            ShowStep(true, null);
            tutCatch.gameObject.SetActive(true);
            tutHint.text = "";
            Snd.Ui("fanfare");
            yield return Type(L.En
                ? "Spotless! You know the basics. Still to discover: MINISTERS, rare cards and special protesters. I'm off: the presidential sash is yours. Good luck, you'll need it!"
                : "¡Impecable! Ya sabés lo básico. Te falta descubrir los MINISTROS, las cartas raras y los manifestantes especiales. Yo me voy: la banda es tuya. ¡Suerte, la vas a necesitar!");
            var b = UiKit.Rect("Govern", tutBox).At(new Vector2(0.5f, 0), new Vector2(60, -126), new Vector2(420, 100), new Vector2(0.5f, 0));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex("#1d6b2a"), 26, true).RT().Fill();
            var f = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex("#43c157"), 26).RT().Fill(0, 0, 0, 9);
            UiKit.Txt(f, "T", "¡A GOBERNAR!", 44, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            var t = b.gameObject.AddComponent<Tap>();
            t.visual = b;
            bool go = false;
            t.onTap = () => go = true;
            b.localScale = Vector3.zero;
            yield return UiKit.Tween(0.35f, k => { if (b) b.localScale = Vector3.one * Mathf.Max(0, Ease.OutBack(k)); });
            while (!go) yield return null;
            TutClose();
            if (GameFlow.I) GameFlow.I.EndTutorial();
        }

        // ------------------------------------------------------------------ pasos

        // Paso de información: bloquea todo, explica y espera un toque. Durante la oleada congela el tiempo.
        IEnumerator Info(string es, string en, Func<Rect?> target = null, float? boxY = null)
        {
            bool freeze = gs.phase == Phase.Oleada && !paused;
            if (freeze) Time.timeScale = 0;
            tutBoxYOverride = boxY;
            ShowStep(true, target);
            tutHint.text = "";
            tutTapped = false;
            yield return Type(L.En ? en : es);
            tutTapped = false;
            tutHint.text = L.T("TOCÁ PARA SEGUIR");
            while (!tutTapped) yield return null;
            Snd.Ui("ui_tap");
            tutBoxYOverride = null;
            if (freeze && !paused) Time.timeScale = 1;
        }

        // Paso de acción: resalta sin bloquear y espera a que se cumpla done (o que skip diga que ya no aplica).
        IEnumerator Act(string es, string en, Func<Rect?> target, Func<bool> done, Func<bool> skip = null)
        {
            if (done() || (skip != null && skip())) yield break;
            ShowStep(false, target);
            tutHint.text = L.T("TU TURNO");
            var typing = StartCoroutine(Type(L.En ? en : es));
            while (!done() && (skip == null || !skip())) yield return null;
            StopCoroutine(typing);
            tutTalking = false;
            if (done()) Snd.Ui("pop_up", 0.7f);
            yield return new WaitForSecondsRealtime(0.35f);
        }

        IEnumerator Type(string s)
        {
            tutText.text = s;
            tutText.maxVisibleCharacters = 0;
            tutText.ForceMeshUpdate();
            int total = tutText.textInfo.characterCount;
            float shown = 0;
            tutTalking = true;
            while (shown < total)
            {
                if (tutBlocking && tutTapped) { tutTapped = false; break; } // el primer toque completa el texto
                shown += Time.unscaledDeltaTime * 62f;
                tutText.maxVisibleCharacters = Mathf.FloorToInt(shown);
                yield return null;
            }
            tutText.maxVisibleCharacters = 99999;
            tutTalking = false;
        }

        void ShowStep(bool blocking, Func<Rect?> target)
        {
            tutBlocking = blocking;
            tutTarget = target;
            tutDimA = blocking ? 0.62f : 0;
            tutCatch.gameObject.SetActive(blocking);
            bool big = blocking;
            tutBox.sizeDelta = big ? new Vector2(TutBigW, TutBigH) : new Vector2(TutSmallW, TutSmallH);
            tutText.fontSize = big ? 28 : 24;
            tutPortrait.localScale = Vector3.one * (big ? 1f : 0.86f);
            tutText.RT().Fill(big ? 196 : 176, 26, 50, 46);
            if (!tutBoxOn)
            {
                tutBoxOn = true;
                tutBox.gameObject.SetActive(true);
                PlaceBox(true);
                StartCoroutine(UiKit.Tween(0.3f, k => { if (tutBox) { tutBox.localScale = Vector3.one * Mathf.Max(0, Ease.OutBack(k)); tutBoxGroup.alpha = k; } }));
            }
            else StartCoroutine(UiKit.Tween(0.22f, k => { if (tutBox) tutBox.localScale = Vector3.one * (1 + Mathf.Sin(k * Mathf.PI) * 0.03f); }));
        }

        void TutHide()
        {
            tutTarget = null;
            tutDimA = 0;
            tutBlocking = false;
            if (tutCatch) tutCatch.gameObject.SetActive(false);
            if (!tutBoxOn) return;
            tutBoxOn = false;
            StartCoroutine(UiKit.Tween(0.2f, k => { if (tutBoxGroup) tutBoxGroup.alpha = 1 - k; }));
        }

        void TutClose()
        {
            if (tutCo != null) StopCoroutine(tutCo);
            if (tutRoot) Destroy(tutRoot.gameObject);
            tutRoot = null;
            if (!paused) Time.timeScale = 1;
        }

        void OnTutSkip()
        {
            if (!GameFlow.I || GameFlow.I.state != GameFlow.State.Playing) return;
            TutClose();
            if (zoomed) CancelZoom();
            if (placing) CancelPlacement();
            GameFlow.I.Resign();
        }

        // ------------------------------------------------------------------ objetivos (rect en píxeles de pantalla)

        static Rect? ScreenRect(RectTransform rt)
        {
            if (!rt || !rt.gameObject.activeInHierarchy) return null;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            float x0 = Mathf.Min(c[0].x, c[1].x, c[2].x, c[3].x), x1 = Mathf.Max(c[0].x, c[1].x, c[2].x, c[3].x);
            float y0 = Mathf.Min(c[0].y, c[1].y, c[2].y, c[3].y), y1 = Mathf.Max(c[0].y, c[1].y, c[2].y, c[3].y);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static Rect? Union(Rect? a, Rect? b)
        {
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;
            return Rect.MinMaxRect(Mathf.Min(a.Value.xMin, b.Value.xMin), Mathf.Min(a.Value.yMin, b.Value.yMin), Mathf.Max(a.Value.xMax, b.Value.xMax), Mathf.Max(a.Value.yMax, b.Value.yMax));
        }

        static Func<Rect?> Of(params RectTransform[] rts) => () =>
        {
            Rect? r = null;
            foreach (var rt in rts) r = Union(r, ScreenRect(rt));
            return r;
        };

        Rect? HandRect()
        {
            Rect? r = null;
            foreach (var cv in cards) if (cv && cv.transform.parent == hand) r = Union(r, ScreenRect(cv.Rt));
            return r;
        }

        Rect? CardRect(string id)
        {
            foreach (var cv in cards) if (cv && cv.def && cv.def.id == id && cv.transform.parent == hand) return ScreenRect(cv.Rt);
            return null;
        }

        Rect? CrisisRect()
        {
            foreach (var cv in cards) if (cv && cv.def && cv.def.kind == CardKind.Crisis && cv.transform.parent == hand) return ScreenRect(cv.Rt);
            return null;
        }

        Rect? RewardsRect()
        {
            Rect? r = null;
            foreach (var w in rewards) { if (w.cv) r = Union(r, ScreenRect(w.cv.Rt)); if (w.btn) r = Union(r, ScreenRect(w.btn)); }
            return r;
        }

        bool InHand(string id) => Array.Exists(cards, c => c && c.def && c.def.id == id);
        static bool AnyUpgraded() { foreach (var d in Defense.All) if (d && d.level >= 2) return true; return false; }

        // ------------------------------------------------------------------ cuadro por frame

        void UpdateTutorial()
        {
            if (!tutRoot) return;
            tutScaler.matchWidthOrHeight = scaler.matchWidthOrHeight;
            bool live = !paused && (!GameFlow.I || GameFlow.I.state == GameFlow.State.Playing);
            tutGroup.alpha = Mathf.MoveTowards(tutGroup.alpha, live ? 1 : 0, Time.unscaledDeltaTime * 6f);
            tutGroup.blocksRaycasts = live;

            var size = tutRoot.rect.size;
            Rect? hole = null;
            var sr = tutTarget?.Invoke();
            if (sr.HasValue)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(tutRoot, sr.Value.min, null, out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(tutRoot, sr.Value.max, null, out var b);
                hole = Rect.MinMaxRect(a.x - 10, a.y - 10, b.x + 10, b.y + 10);
            }
            tutDimShown = Mathf.MoveTowards(tutDimShown, tutDimA, Time.unscaledDeltaTime * 3f);
            LayoutDims(hole, size);

            // marco y flecha: solo con objetivo y sin una carta abierta o en el aire
            bool frame = hole.HasValue && !(zoomed || placing || dragging);
            tutFrame.gameObject.SetActive(frame);
            tutArrow.gameObject.SetActive(frame);
            if (frame)
            {
                var h = hole.Value;
                tutFrame.anchoredPosition = h.center;
                tutFrame.sizeDelta = h.size;
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                foreach (var im in tutBarImgs) im.color = new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.55f + 0.45f * pulse);
                float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.5f)) * 14f;
                bool side = Mathf.Abs(h.center.x) > size.x * 0.3f && h.width < size.x * 0.4f;
                if (side)
                {
                    // objetivo en un costado (paneles, mazo, botones): flecha del lado de adentro, apuntando hacia afuera
                    bool right = h.center.x > 0;
                    tutArrow.anchoredPosition = right ? new Vector2(h.xMin - 40 - bob, h.center.y) : new Vector2(h.xMax + 40 + bob, h.center.y);
                    tutArrow.localRotation = Quaternion.Euler(0, 0, right ? 90 : -90);
                }
                else
                {
                    bool below = h.center.y < 0; // objetivo abajo: flecha arriba apuntando hacia abajo
                    bool inHand = h.yMax < -size.y * 0.5f + 330; // arriba de las cartas está el cartel de mejora o de crisis
                    tutArrow.anchoredPosition = below ? new Vector2(h.center.x, h.yMax + (inHand ? 82 : 34) + bob) : new Vector2(h.center.x, h.yMin - 34 - bob);
                    tutArrow.localRotation = Quaternion.Euler(0, 0, below ? 0 : 180);
                }
            }
            PlaceBox(false, hole, size);

            // Don Anselmo habla: boca y un leve vaivén
            float tt = Time.unscaledTime;
            tutMouth.localScale = new Vector3(1, tutTalking ? 1 + Mathf.Abs(Mathf.Sin(tt * 17f)) * 1.8f : 1, 1);
            tutFace.anchoredPosition = new Vector2(0, Mathf.Sin(tt * 2.2f) * 2f + (tutTalking ? Mathf.Abs(Mathf.Sin(tt * 8.5f)) * 2f : 0));
            tutFace.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(tt * 1.3f) * 2f);
        }

        void LayoutDims(Rect? hole, Vector2 size)
        {
            float W = size.x * 0.5f + 4, H = size.y * 0.5f + 4;
            var c = new Color(0.02f, 0.04f, 0.1f, tutDimShown);
            foreach (var im in tutDimImgs) im.color = c;
            if (!hole.HasValue)
            {
                Box(tutDims[0], -W, -H, W, H);
                for (int i = 1; i < 4; i++) Box(tutDims[i], 0, 0, 0, 0);
                return;
            }
            var h = hole.Value;
            Box(tutDims[0], -W, h.yMax, W, H);          // arriba
            Box(tutDims[1], -W, -H, W, h.yMin);         // abajo
            Box(tutDims[2], -W, h.yMin, h.xMin, h.yMax); // izquierda
            Box(tutDims[3], h.xMax, h.yMin, W, h.yMax);  // derecha
        }

        static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(0, x1 - x0), Mathf.Max(0, y1 - y0));
        }

        // Cuadro de diálogo: los de información van al medio (arriba o abajo del objetivo); los de acción, a la
        // derecha y a media altura, para no tapar la mano ni el camino donde se coloca.
        void PlaceBox(bool snap, Rect? hole = null, Vector2 size = default)
        {
            if (size == default) size = tutRoot.rect.size;
            var bs = tutBox.sizeDelta;
            Vector2 want;
            if (tutBlocking)
            {
                float y = tutBoxYOverride ?? (!hole.HasValue ? 10 : hole.Value.center.y < 0 ? 90 : -70);
                want = new Vector2(50, y);
            }
            else want = new Vector2(size.x * 0.5f - 24 - bs.x * 0.5f, 110);
            tutBoxY = want.y;
            tutBox.anchoredPosition = snap ? want : Vector2.Lerp(tutBox.anchoredPosition, want, 1 - Mathf.Exp(-10 * Time.unscaledDeltaTime));
        }

        // ------------------------------------------------------------------ construcción

        void BuildTutorial()
        {
            var go = new GameObject("TutorialUI", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 25;
            tutScaler = go.AddComponent<CanvasScaler>();
            tutScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            tutScaler.referenceResolution = new Vector2(1600, 900);
            tutScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            tutScaler.matchWidthOrHeight = scaler.matchWidthOrHeight;
            go.AddComponent<GraphicRaycaster>();
            tutGroup = go.AddComponent<CanvasGroup>();
            tutGroup.alpha = 0;
            tutRoot = (RectTransform)go.transform;

            // oscurecido con un hueco sobre el objetivo (4 rectángulos)
            tutDims = new RectTransform[4];
            tutDimImgs = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                tutDimImgs[i] = UiKit.Img(tutRoot, "Dim" + i, null, new Color(0.02f, 0.04f, 0.1f, 0));
                tutDims[i] = tutDimImgs[i].RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            }
            // en los pasos de información, cualquier toque avanza
            tutCatch = UiKit.Img(tutRoot, "Catch", null, new Color(0, 0, 0, 0), 0, true);
            tutCatch.RT().Fill();
            var ct = tutCatch.gameObject.AddComponent<Tap>();
            ct.sound = null;
            ct.pressScale = 1;
            ct.onTap = () => tutTapped = true;
            tutCatch.gameObject.SetActive(false);

            // marco dorado pulsante alrededor del hueco
            tutFrame = UiKit.Rect("Frame", tutRoot).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10), new Vector2(0.5f, 0.5f));
            tutBarImgs = new Image[4];
            const float th = 7;
            var mins = new[] { new Vector2(0, 1), Vector2.zero, Vector2.zero, new Vector2(1, 0) };
            var maxs = new[] { Vector2.one, new Vector2(1, 0), new Vector2(0, 1), Vector2.one };
            var oMin = new[] { new Vector2(-th, 0), new Vector2(-th, -th), new Vector2(-th, 0), Vector2.zero };
            var oMax = new[] { new Vector2(th, th), new Vector2(th, 0), Vector2.zero, new Vector2(th, 0) };
            for (int i = 0; i < 4; i++)
            {
                tutBarImgs[i] = UiKit.Img(tutFrame, "B" + i, UiKit.RR, UiKit.Gold, 3);
                var r = tutBarImgs[i].RT();
                r.anchorMin = mins[i]; r.anchorMax = maxs[i]; r.pivot = new Vector2(0.5f, 0.5f);
                r.offsetMin = oMin[i]; r.offsetMax = oMax[i];
            }
            // flecha: doble chevrón dorado con sombra
            tutArrow = UiKit.Rect("Arrow", tutRoot).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60), new Vector2(0.5f, 0.5f));
            for (int k = 0; k < 2; k++)
                for (int s = 0; s < 2; s++)
                {
                    var col = s == 0 ? new Color(0.1f, 0.06f, 0, 0.55f) : UiKit.Gold;
                    var off = s == 0 ? new Vector2(3, -4) : Vector2.zero;
                    float y = 14 - k * 22;
                    UiKit.Img(tutArrow, "L", UiKit.RR, col, 6).RT().At(new Vector2(0.5f, 0.5f), off + new Vector2(-12, y), new Vector2(38, 13)).localRotation = Quaternion.Euler(0, 0, -45);
                    UiKit.Img(tutArrow, "R", UiKit.RR, col, 6).RT().At(new Vector2(0.5f, 0.5f), off + new Vector2(12, y), new Vector2(38, 13)).localRotation = Quaternion.Euler(0, 0, 45);
                }

            // cuadro de diálogo
            tutBox = UiKit.Panel(tutRoot, "Box", 26).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TutBigW, TutBigH));
            tutBoxGroup = tutBox.gameObject.AddComponent<CanvasGroup>();
            tutBoxGroup.blocksRaycasts = true;
            UiKit.Txt(tutBox, "Name", "DON ANSELMO  <color=#fbf1dc><size=16>·  PRESIDENTE SALIENTE</size></color>", 22, UiKit.Gold, TextAlignmentOptions.Left, true)
                .RT().At(new Vector2(0, 1), new Vector2(196, -10), new Vector2(560, 32), new Vector2(0, 1));
            tutText = UiKit.Txt(tutBox, "Text", "", 28, UiKit.Cream, TextAlignmentOptions.TopLeft, false, UiKit.FontBody);
            tutText.textWrappingMode = TextWrappingModes.Normal;
            tutText.lineSpacing = -4;
            tutText.RT().Fill(196, 26, 50, 46);
            tutHint = UiKit.Txt(tutBox, "Hint", "", 20, UiKit.Gold, TextAlignmentOptions.Right, true);
            tutHint.RT().At(new Vector2(1, 0), new Vector2(-24, 12), new Vector2(420, 28), new Vector2(1, 0));

            // saltear: pastilla arriba a la derecha del cuadro
            tutSkip = UiKit.Img(tutBox, "Skip", UiKit.RR, new Color(0.02f, 0.04f, 0.1f, 0.85f), 14, true).RT();
            tutSkip.At(new Vector2(1, 1), new Vector2(-12, 8), new Vector2(230, 40), new Vector2(1, 0));
            UiKit.Txt(tutSkip, "T", "SALTEAR TUTORIAL", 18, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Center, true).RT().Fill();
            var sk = tutSkip.gameObject.AddComponent<Tap>();
            sk.visual = tutSkip;
            sk.onTap = OnTutSkip;

            // retrato: medio cuerpo, asoma por la izquierda del cuadro
            tutPortrait = UiKit.Rect("Portrait", tutBox).At(new Vector2(0, 0.5f), new Vector2(-34, 8), new Vector2(214, 214), new Vector2(0, 0.5f));
            BuildAnselmo(tutPortrait);
            tutBox.gameObject.SetActive(false);
        }

        // Don Anselmo: presidente caricaturesco y ficticio (pelado con costados canosos, bigote, banda presidencial).
        void BuildAnselmo(RectTransform p)
        {
            var mid = new Vector2(0.5f, 0.5f);
            RectTransform Part(Transform parent, string n, Sprite s, Color col, Vector2 pos, Vector2 sz, float rot = 0, float rad = 0)
            {
                var r = UiKit.Img(parent, n, s, col, rad).RT().At(mid, pos, sz);
                r.localRotation = Quaternion.Euler(0, 0, rot);
                return r;
            }
            Part(p, "Shadow", UiKit.Circle, new Color(0, 0, 0, 0.35f), new Vector2(4, -6), new Vector2(214, 214));
            Part(p, "Ring", UiKit.Circle, UiKit.Gold, Vector2.zero, new Vector2(214, 214));
            var inner = Part(p, "In", UiKit.Circle, UiKit.Celeste, Vector2.zero, new Vector2(200, 200));
            inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            // fondo: bandera
            Part(inner, "White", null, new Color(0.97f, 0.97f, 0.97f), Vector2.zero, new Vector2(220, 66));
            Part(inner, "Sun", UiKit.Circle, UiKit.Hex("#f6b40e"), new Vector2(64, 0), new Vector2(26, 26));

            // cuerpo
            var skin = UiKit.Hex("#efbd96");
            Part(inner, "Suit", UiKit.Circle, UiKit.Hex("#26314f"), new Vector2(0, -122), new Vector2(220, 160));
            Part(inner, "Shirt", UiKit.RR, Color.white, new Vector2(0, -66), new Vector2(46, 56), 0, 10);
            var sash = UiKit.Rect("Sash", inner).At(mid, new Vector2(4, -86), new Vector2(10, 10));
            sash.localRotation = Quaternion.Euler(0, 0, 33);
            Part(sash, "C1", null, UiKit.Celeste, new Vector2(0, 12), new Vector2(240, 12));
            Part(sash, "W", null, Color.white, Vector2.zero, new Vector2(240, 12));
            Part(sash, "C2", null, UiKit.Celeste, new Vector2(0, -12), new Vector2(240, 12));
            Part(inner, "Medal", UiKit.Circle, UiKit.Gold, new Vector2(30, -96), new Vector2(18, 18));
            Part(inner, "Neck", UiKit.RR, UiKit.Shade(skin, 0.9f), new Vector2(0, -44), new Vector2(42, 34), 0, 10);

            // cara (se mueve aparte)
            tutFace = UiKit.Rect("Face", inner).At(mid, Vector2.zero, new Vector2(10, 10));
            var gray = UiKit.Hex("#c9ccd2");
            Part(tutFace, "EarL", UiKit.Circle, UiKit.Shade(skin, 0.94f), new Vector2(-47, 6), new Vector2(22, 30));
            Part(tutFace, "EarR", UiKit.Circle, UiKit.Shade(skin, 0.94f), new Vector2(47, 6), new Vector2(22, 30));
            Part(tutFace, "HairL", UiKit.Circle, gray, new Vector2(-40, 24), new Vector2(34, 46));
            Part(tutFace, "HairR", UiKit.Circle, gray, new Vector2(40, 24), new Vector2(34, 46));
            Part(tutFace, "Head", UiKit.Circle, skin, new Vector2(0, 10), new Vector2(92, 108));
            Part(tutFace, "Shine", UiKit.Circle, new Color(1, 1, 1, 0.35f), new Vector2(-14, 50), new Vector2(26, 12), 15);
            Part(tutFace, "Comb", UiKit.RR, gray, new Vector2(10, 55), new Vector2(46, 9), 10, 4);
            Part(tutFace, "BrowL", UiKit.RR, UiKit.Hex("#7d828c"), new Vector2(-19, 30), new Vector2(26, 8), 10, 4);
            Part(tutFace, "BrowR", UiKit.RR, UiKit.Hex("#7d828c"), new Vector2(19, 30), new Vector2(26, 8), -10, 4);
            foreach (var x in new[] { -18f, 18f })
            {
                Part(tutFace, "Eye", UiKit.Circle, UiKit.Hex("#2b2b33"), new Vector2(x, 16), new Vector2(10, 13));
                Part(tutFace, "Hi", UiKit.Circle, Color.white, new Vector2(x + 2, 19), new Vector2(4, 4));
            }
            Part(tutFace, "CheekL", UiKit.Circle, new Color(0.95f, 0.45f, 0.45f, 0.3f), new Vector2(-28, -4), new Vector2(20, 12));
            Part(tutFace, "CheekR", UiKit.Circle, new Color(0.95f, 0.45f, 0.45f, 0.3f), new Vector2(28, -4), new Vector2(20, 12));
            Part(tutFace, "Nose", UiKit.Circle, UiKit.Hex("#e2a37e"), new Vector2(0, 4), new Vector2(20, 22));
            // sonrisa: comisuras levantadas
            Part(tutFace, "SmileL", UiKit.RR, UiKit.Hex("#7a2a2a"), new Vector2(-12, -26), new Vector2(10, 5), -25, 2);
            Part(tutFace, "SmileR", UiKit.RR, UiKit.Hex("#7a2a2a"), new Vector2(12, -26), new Vector2(10, 5), 25, 2);
            tutMouth = Part(tutFace, "Mouth", UiKit.RR, UiKit.Hex("#7a2a2a"), new Vector2(0, -28), new Vector2(22, 7), 0, 3);
            Part(tutFace, "MustL", UiKit.RR, gray, new Vector2(-14, -14), new Vector2(34, 15), 14, 7);
            Part(tutFace, "MustR", UiKit.RR, gray, new Vector2(14, -14), new Vector2(34, 15), -14, 7);
        }
    }
}
