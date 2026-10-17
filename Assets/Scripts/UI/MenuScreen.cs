using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Pantalla del menú (sobre la vista del helicóptero): título, subtítulo (resultado de la partida) y NUEVA PARTIDA.
    public class MenuScreen : MonoBehaviour
    {
        CanvasGroup group;
        TextMeshProUGUI subTxt, ribbonTxt, ribbonShadowTxt;
        RectTransform ribbon;
        bool stamping;
        Tap btn;
        Coroutine fade;
        RectTransform root, board, prompt;

        public static MenuScreen Create(System.Action onNewGame)
        {
            var go = new GameObject("MenuUI", typeof(RectTransform)) { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            go.AddComponent<GraphicRaycaster>();
            var m = go.AddComponent<MenuScreen>();
            m.group = go.AddComponent<CanvasGroup>();
            var root = (RectTransform)go.transform;

            // degradé oscuro arriba y abajo para que se lea el texto sobre la escena
            var top = UiKit.Img(root, "TopShade", null, new Color(0.02f, 0.04f, 0.1f, 0.3f));
            top.raycastTarget = false;
            top.RT().At(new Vector2(0.5f, 1), Vector2.zero, new Vector2(4000, 300), new Vector2(0.5f, 1));

            // título con sombra dura desplazada
            UiKit.Logo(root, "Title", 132).RT().At(new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(1400, 150), new Vector2(0.5f, 1));

            // cinta roja inclinada con la consigna: grande, en mayúsculas, con sombra
            m.ribbon = UiKit.Rect("Ribbon", root).At(new Vector2(0.5f, 1), new Vector2(0, -218), new Vector2(820, 84), new Vector2(0.5f, 0.5f));
            m.ribbon.localRotation = Quaternion.Euler(0, 0, -2.5f);
            UiKit.Img(m.ribbon, "Shadow", UiKit.RR, new Color(0, 0, 0, 0.45f), 14).RT().Fill(-6, 6, 8, -12);
            UiKit.Img(m.ribbon, "Back", UiKit.RR, UiKit.Hex("#7a1410"), 14).RT().Fill(0, 0, 4, -6);
            UiKit.Img(m.ribbon, "Face", UiKit.RR, UiKit.Hex("#d9342b"), 14).RT().Fill();
            UiKit.Img(m.ribbon, "Stripe", UiKit.RR, UiKit.Hex("#f2c14e"), 4).RT().At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(780, 5), new Vector2(0.5f, 0));
            UiKit.Img(m.ribbon, "Stripe2", UiKit.RR, UiKit.Hex("#f2c14e"), 4).RT().At(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(780, 5), new Vector2(0.5f, 1));
            m.ribbonShadowTxt = UiKit.Txt(m.ribbon, "TSh", "", 54, new Color(0.25f, 0.02f, 0.02f, 0.9f), TextAlignmentOptions.Center, true);
            m.ribbonShadowTxt.RT().Fill(20, 16, 4, -6);
            m.ribbonTxt = UiKit.Txt(m.ribbon, "T", "", 54, Color.white, TextAlignmentOptions.Center, true);
            m.ribbonTxt.RT().Fill(16, 20, 0, 0);
            foreach (var t in new[] { m.ribbonTxt, m.ribbonShadowTxt }) { t.enableAutoSizing = true; t.fontSizeMin = 26; t.fontSizeMax = 54; t.characterSpacing = 3; }
            m.subTxt = UiKit.Txt(root, "Sub", "", 32, UiKit.Cream, TextAlignmentOptions.Center, true);
            m.subTxt.RT().At(new Vector2(0.5f, 1), new Vector2(0, -272), new Vector2(1400, 60), new Vector2(0.5f, 1));
            // sin override de contorno: un outlineWidth > 0,16 desborda el SDF y la línea se ve como bloques negros
            m.subTxt.textWrappingMode = TextWrappingModes.Normal;
            m.StartCoroutine(m.RibbonPulse());

            var b = UiKit.Rect("NewGame", root).At(new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(460, 112), new Vector2(0.5f, 0));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex("#1d6b2a"), 28, true).RT().Fill();
            var face = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex("#43c157"), 28).RT().Fill(0, 0, 0, 10);
            UiKit.Txt(face, "T", "NUEVA PARTIDA", 46, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            // volúmenes: cada control en su propio contenedor, separados, flotando arriba del botón
            const float volW = 440, volGap = 12, volBase = 56 + 112 + 18; // 18 de aire sobre NUEVA PARTIDA
            float boxH = VolumeSlider.RowH + VolumeSlider.Pad * 2;
            VolumeSlider.Create(root, "EFECTOS", new Vector2(0.5f, 0), new Vector2(0, volBase), volW, () => GameAudio.Sfx, GameAudio.SetSfx, true, new Vector2(0.5f, 0));
            VolumeSlider.Create(root, "MÚSICA", new Vector2(0.5f, 0), new Vector2(0, volBase + boxH + volGap), volW, () => GameAudio.Music, GameAudio.SetMusic, true, new Vector2(0.5f, 0));
            m.btn = b.gameObject.AddComponent<Tap>();
            m.btn.visual = b;
            m.btn.onTap = () => onNewGame?.Invoke();
            m.StartCoroutine(m.Breathe(b));
            m.root = root;
            m.RefreshBoard(-1);
            m.BuildSideButtons();
            return m;
        }

        // ------------------------------------------------------------------ botones de la derecha: GitHub e idioma

        public const string RepoUrl = "https://github.com/alexander171294/Argensimia";

        void BuildSideButtons()
        {
            // idioma (abajo) y GitHub (arriba), con el mismo margen que el ranking
            // escritorio: salir (abajo de todo) y ventana / pantalla completa (arriba)
            bool desktop = !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer;
            float y0 = 50;
            if (desktop)
            {
                var quit = SideButton("Quit", "SALIR", "#d9534a", "#7a231d", y0);
                var qi = UiKit.Rect("Icon", quit).At(new Vector2(0, 0.5f), new Vector2(22, 2), new Vector2(40, 40), new Vector2(0, 0.5f));
                UiKit.Img(qi, "Ring", UiKit.Circle, Color.white).RT().Fill();
                UiKit.Txt(qi, "X", "X", 24, UiKit.Hex("#7a231d"), TextAlignmentOptions.Center, false, UiKit.FontLogo).RT().Fill(0, 0, 2, 0);
                quit.GetComponent<Tap>().onTap = () =>
                {
                    PlayerPrefs.Save();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                };
                y0 += 98;
            }
            var lang = SideButton("Lang", L.En ? "ESPAÑOL" : "ENGLISH", "#2f6fd6", "#163a7a", y0);
            var flag = UiKit.Rect("Flag", lang).At(new Vector2(0, 0.5f), new Vector2(22, 2), new Vector2(46, 32), new Vector2(0, 0.5f));
            if (L.En)
            {
                // a español: bandera argentina
                UiKit.Img(flag, "C1", null, UiKit.Celeste).RT().Fill(0, 0, 0, 21);
                UiKit.Img(flag, "W", null, Color.white).RT().Fill(0, 0, 11, 11);
                UiKit.Img(flag, "C2", null, UiKit.Celeste).RT().Fill(0, 0, 21, 0);
                UiKit.Img(flag, "Sun", UiKit.Circle, UiKit.Hex("#f6b40e")).RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 8));
            }
            else
            {
                // a inglés: "EN" en una pastilla blanca
                var pill = UiKit.Img(flag, "Pill", UiKit.RR, Color.white, 6).RT(); pill.Fill();
                UiKit.Txt(pill, "T", "EN", 20, UiKit.Hex("#163a7a"), TextAlignmentOptions.Center).RT().Fill();
            }
            lang.GetComponent<Tap>().onTap = () =>
            {
                L.En = !L.En;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            };
            var gh = SideButton("GitHub", "GITHUB", "#2b3137", "#0d1117", y0 + 98);
            if (desktop)
            {
                var win = SideButton("Window", ScreenLabel(), "#6a4fb3", "#35246b", y0 + 196);
                var wi = UiKit.Rect("Icon", win).At(new Vector2(0, 0.5f), new Vector2(22, 2), new Vector2(40, 32), new Vector2(0, 0.5f));
                // cuatro esquinas: "expandir"
                foreach (var c in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) })
                {
                    UiKit.Img(wi, "H", null, Color.white).RT().At(c, Vector2.zero, new Vector2(14, 5), c);
                    UiKit.Img(wi, "V", null, Color.white).RT().At(c, Vector2.zero, new Vector2(5, 14), c);
                }
                var lbl = win.Find("Face/T").GetComponent<TextMeshProUGUI>();
                win.GetComponent<Tap>().onTap = () => { ToggleFullscreen(); StartCoroutine(RefreshScreenLabel(lbl)); };
            }
            // versión, debajo del botón de idioma
            UiKit.Txt(root, "Version", "v" + Application.version, 20, Color.white, TextAlignmentOptions.Center, false, UiKit.FontBody)
                .RT().At(new Vector2(1, 0), new Vector2(-28, 16), new Vector2(280, 26), new Vector2(1, 0));
            var ic = UiKit.Img(gh, "Icon", UiKit.Circle, Color.white).RT();
            ic.At(new Vector2(0, 0.5f), new Vector2(22, 2), new Vector2(40, 40), new Vector2(0, 0.5f));
            UiKit.Txt(ic, "T", "</>", 17, UiKit.Hex("#0d1117"), TextAlignmentOptions.Center).RT().Fill();
            gh.GetComponent<Tap>().onTap = () => Application.OpenURL(RepoUrl);
        }

        static bool IsFullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;
        static string ScreenLabel() => L.T(IsFullscreen ? "VENTANA" : "PANTALLA COMPLETA");

        // Pantalla completa en el monitor donde está la ventana, o ventana al 75% de ese monitor.
        static void ToggleFullscreen()
        {
            var d = Screen.mainWindowDisplayInfo;
            int w = d.width > 0 ? d.width : Display.main.systemWidth, h = d.height > 0 ? d.height : Display.main.systemHeight;
            if (IsFullscreen) Screen.SetResolution(Mathf.RoundToInt(w * 0.75f), Mathf.RoundToInt(h * 0.75f), FullScreenMode.Windowed);
            else Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
        }

        IEnumerator RefreshScreenLabel(TextMeshProUGUI lbl)
        {
            yield return null; yield return null; // el cambio de modo se aplica al cuadro siguiente
            if (lbl) lbl.text = ScreenLabel();
        }

        RectTransform SideButton(string name, string label, string faceHex, string baseHex, float y)
        {
            var b = UiKit.Rect(name, root).At(new Vector2(1, 0), new Vector2(-28, y), new Vector2(280, 84), new Vector2(1, 0));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex(baseHex), 22, true).RT().Fill();
            var face = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex(faceHex), 22).RT().Fill(0, 0, 0, 8);
            var t0 = UiKit.Txt(face, "T", label, 36, Color.white, TextAlignmentOptions.Center, true);
            t0.RT().Fill(70, 14, 0, 0);
            t0.enableAutoSizing = true; t0.fontSizeMin = 20; t0.fontSizeMax = 36;
            var t = b.gameObject.AddComponent<Tap>();
            t.visual = b;
            return b;
        }

        // ------------------------------------------------------------------ ranking (top 5, abajo a la izquierda)

        void RefreshBoard(int highlight)
        {
            if (board) Destroy(board.gameObject);
            int rows = Mathf.Min(Leaderboard.Size, Mathf.Max(1, Leaderboard.Top.Count));
            float h = Leaderboard.Top.Count == 0 ? 170 : 92 + rows * 46;
            board = UiKit.Panel(root, "Ranking", 18).At(Vector2.zero, new Vector2(28, 28), new Vector2(370, h), Vector2.zero); // abajo a la izquierda
            UiKit.Txt(board, "Title", "RANKING", 40, UiKit.Gold, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(340, 48), new Vector2(0.5f, 1));
            UiKit.Txt(board, "Head", "MEJORES GOBIERNOS", 15, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(340, 20), new Vector2(0.5f, 1));
            var top = Leaderboard.Top;
            if (top.Count == 0)
            {
                UiKit.Txt(board, "Empty", "Todavía no hay partidas.\n¡Sé el primero en aguantar!", 20, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Center, false, UiKit.FontBody)
                    .RT().At(new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(330, 60), new Vector2(0.5f, 0));
                return;
            }
            for (int i = 0; i < top.Count && i < Leaderboard.Size; i++)
            {
                var e = top[i];
                float y = -86 - i * 46;
                var row = UiKit.Img(board, "Row" + i, UiKit.RR, i == highlight ? new Color(0.95f, 0.75f, 0.2f, 0.35f) : new Color(1, 1, 1, i % 2 == 0 ? 0.06f : 0.02f), 10).RT();
                row.At(new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(344, 42), new Vector2(0.5f, 1));
                var medal = i == 0 ? UiKit.Gold : i == 1 ? UiKit.Hex("#c9d1dc") : i == 2 ? UiKit.Hex("#d08a4a") : new Color(1, 1, 1, 0.18f);
                var rk = UiKit.Img(row, "Rank", UiKit.Circle, medal).RT();
                rk.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(30, 30), new Vector2(0, 0.5f));
                UiKit.Txt(rk, "N", (i + 1).ToString(), 17, i < 3 ? UiKit.Hex("#3a2400") : Color.white, TextAlignmentOptions.Center).RT().Fill();
                var nm = UiKit.Txt(row, "Name", e.name, 20, Color.white, TextAlignmentOptions.Left, false, UiKit.FontBody);
                nm.RT().Fill(46, 120, 0, 0);
                nm.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Txt(row, "Score", GameState.Money(e.score), 22, UiKit.Gold, TextAlignmentOptions.Right).RT().Fill(200, 10, 0, 14);
                UiKit.Txt(row, "Mand", e.mandates + L.T(e.mandates == 1 ? " mandato" : " mandatos"), 12, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Right, false, UiKit.FontBody)
                    .RT().At(new Vector2(1, 0), new Vector2(-10, 2), new Vector2(140, 16), new Vector2(1, 0));
            }
        }

        // Fin de partida: si el puntaje entra al top 10, pide el nombre.
        public void OfferScore(int score, int mandates)
        {
            if (!Leaderboard.Qualifies(score)) return;
            if (prompt) Destroy(prompt.gameObject);
            var dim = UiKit.Img(root, "NamePrompt", null, new Color(0.02f, 0.04f, 0.1f, 0.72f), 0, true);
            prompt = dim.RT();
            prompt.Fill();
            var p = UiKit.Panel(prompt, "Box", 22).At(new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(620, 330));
            UiKit.Txt(p, "Title", "¡ENTRASTE AL TOP 5!", 46, UiKit.Gold, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(580, 56), new Vector2(0.5f, 1));
            UiKit.Txt(p, "Score", L.T("Puntaje") + " <color=#f2c14e>" + GameState.Money(score) + "</color>  ·  " + mandates + L.T(mandates == 1 ? " mandato" : " mandatos"), 26, Color.white, TextAlignmentOptions.Center, false, UiKit.FontBody)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(580, 34), new Vector2(0.5f, 1));
            // campo de texto
            var box = UiKit.Img(p, "Input", UiKit.RR, UiKit.Cream, 14, true);
            box.RT().At(new Vector2(0.5f, 1), new Vector2(0, -132), new Vector2(460, 70), new Vector2(0.5f, 1));
            var area = UiKit.Rect("Area", box.transform).Fill(18, 18, 8, 8);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = UiKit.Txt(area, "Placeholder", "Tu nombre", 30, new Color(0.23f, 0.16f, 0.1f, 0.45f), TextAlignmentOptions.Left, false, UiKit.FontBody);
            ph.RT().Fill();
            var tx = UiKit.Txt(area, "Text", "", 30, UiKit.Hex("#3a2a1a"), TextAlignmentOptions.Left, false, UiKit.FontBody);
            tx.RT().Fill();
            var input = box.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = tx;
            input.placeholder = ph;
            input.characterLimit = 16;
            input.caretColor = UiKit.Hex("#3a2a1a");
            input.customCaretColor = true;
            input.text = Leaderboard.LastName;
            // guardar
            var b = UiKit.Rect("Save", p).At(new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(300, 84), new Vector2(0.5f, 0));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex("#1d6b2a"), 22, true).RT().Fill();
            var face = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex("#43c157"), 22).RT().Fill(0, 0, 0, 8);
            UiKit.Txt(face, "T", "GUARDAR", 40, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            var tap = b.gameObject.AddComponent<Tap>();
            tap.visual = b;
            void Save()
            {
                if (!prompt) return;
                Leaderboard.LastName = input.text.Trim();
                int rank = Leaderboard.Add(input.text, score, mandates);
                Snd.Ui(rank == 0 ? "upgrade" : "coin");
                Destroy(prompt.gameObject);
                prompt = null;
                RefreshBoard(rank);
            }
            tap.onTap = Save;
            input.onSubmit.AddListener(_ => Save());
            p.localScale = Vector3.zero;
            StartCoroutine(UiKit.Tween(0.35f, k => { if (p) p.localScale = Vector3.one * Mathf.Max(0, Ease.OutBack(k)); }));
            Snd.Ui("fanfare", 0.8f);
            input.ActivateInputField();
        }

        IEnumerator Breathe(RectTransform b)
        {
            while (b)
            {
                if (group.alpha > 0.99f) b.localScale = Vector3.one * (1 + Mathf.Sin(Time.unscaledTime * 3f) * 0.03f);
                yield return null;
            }
        }

        // Después del sellado la cinta solo "respira": un vaivén muy leve para no competir con la ondita del logo.
        IEnumerator RibbonPulse()
        {
            while (ribbon)
            {
                if (!stamping)
                {
                    float t = Time.unscaledTime;
                    ribbon.localScale = Vector3.one * (1 + Mathf.Sin(t * 1.3f) * 0.008f);
                    ribbon.localRotation = Quaternion.Euler(0, 0, -2.5f + Mathf.Sin(t * 0.9f) * 0.5f);
                }
                yield return null;
            }
        }

        // sub: primera línea en la cinta (consigna o resultado); el resto, debajo.
        public void Show(bool on, float dur, string sub = null)
        {
            if (sub != null)
            {
                int nl = sub.IndexOf('\n');
                string head = nl < 0 ? sub : sub.Substring(0, nl);
                head = System.Text.RegularExpressions.Regex.Replace(head, "<.*?>", "").ToUpperInvariant();
                ribbonTxt.text = ribbonShadowTxt.text = head;
                subTxt.text = nl < 0 ? "" : sub.Substring(nl + 1);
                if (on && dur > 0) StartCoroutine(Stamp());
            }
            group.blocksRaycasts = on;
            group.interactable = on;
            if (fade != null) StopCoroutine(fade);
            if (dur <= 0) { group.alpha = on ? 1 : 0; return; }
            float from = group.alpha, to = on ? 1 : 0;
            fade = StartCoroutine(UiKit.Tween(dur, t => group.alpha = Mathf.Lerp(from, to, t)));
        }

        // la cinta entra "sellada": grande y cae a su lugar
        IEnumerator Stamp()
        {
            stamping = true;
            // cae grande desde arriba, golpea, rebota y tiembla un instante
            yield return UiKit.Tween(0.32f, k =>
            {
                float e = Ease.InCubic(k);
                ribbon.localScale = Vector3.one * Mathf.Lerp(3.2f, 0.92f, e);
                ribbon.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-18f, -1.5f, e));
            });
            Snd.Ui("stamp", 1, 1, 0.3f);
            yield return UiKit.Tween(0.45f, k =>
            {
                float e = Ease.OutBack(k);
                float shake = Mathf.Sin(k * 60f) * (1 - k) * 2.2f;
                ribbon.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, e);
                ribbon.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(-1.5f, -2.5f, e) + shake);
            });
            stamping = false;
        }
    }
}
