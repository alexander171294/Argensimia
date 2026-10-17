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
            return m;
        }

        // ------------------------------------------------------------------ ranking (top 10, a la izquierda)

        void RefreshBoard(int highlight)
        {
            if (board) Destroy(board.gameObject);
            board = UiKit.Panel(root, "Ranking", 18).At(new Vector2(0, 0.5f), new Vector2(28, -20), new Vector2(370, 560), new Vector2(0, 0.5f));
            UiKit.Txt(board, "Title", "RANKING", 40, UiKit.Gold, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(340, 48), new Vector2(0.5f, 1));
            UiKit.Txt(board, "Head", "MEJORES GOBIERNOS", 15, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(340, 20), new Vector2(0.5f, 1));
            var top = Leaderboard.Top;
            if (top.Count == 0)
            {
                UiKit.Txt(board, "Empty", "Todavía no hay partidas.\n¡Sé el primero en aguantar!", 20, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Center, false, UiKit.FontBody)
                    .RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(330, 80));
                return;
            }
            for (int i = 0; i < top.Count; i++)
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
                UiKit.Txt(row, "Mand", e.mandates + (e.mandates == 1 ? " mandato" : " mandatos"), 12, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Right, false, UiKit.FontBody)
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
            UiKit.Txt(p, "Title", "¡ENTRASTE AL TOP 10!", 46, UiKit.Gold, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(580, 56), new Vector2(0.5f, 1));
            UiKit.Txt(p, "Score", "Puntaje <color=#f2c14e>" + GameState.Money(score) + "</color>  ·  " + mandates + (mandates == 1 ? " mandato" : " mandatos"), 26, Color.white, TextAlignmentOptions.Center, false, UiKit.FontBody)
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
