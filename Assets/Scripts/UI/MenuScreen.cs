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
            var titleSh = UiKit.Txt(root, "TitleShadow", "ARGENSIMIA", 124, new Color(0.08f, 0.05f, 0.12f, 0.85f), TextAlignmentOptions.Center, true);
            titleSh.RT().At(new Vector2(0.5f, 1), new Vector2(7, -36), new Vector2(1400, 140), new Vector2(0.5f, 1));
            titleSh.characterSpacing = 6;
            var title = UiKit.Txt(root, "Title", "ARGENSIMIA", 124, UiKit.Gold, TextAlignmentOptions.Center, true);
            title.RT().At(new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(1400, 140), new Vector2(0.5f, 1));
            title.characterSpacing = 6;

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
            return m;
        }

        IEnumerator Breathe(RectTransform b)
        {
            while (b)
            {
                if (group.alpha > 0.99f) b.localScale = Vector3.one * (1 + Mathf.Sin(Time.unscaledTime * 3f) * 0.03f);
                yield return null;
            }
        }

        IEnumerator RibbonPulse()
        {
            while (ribbon)
            {
                float k = Mathf.Repeat(Time.unscaledTime, 2.4f);
                float punch = k < 0.25f ? Mathf.Sin(k / 0.25f * Mathf.PI) * 0.06f : 0;
                if (!stamping) ribbon.localScale = Vector3.one * (1 + punch);
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
            yield return UiKit.Tween(0.45f, k =>
            {
                float e = Ease.OutBack(k);
                ribbon.localScale = Vector3.one * Mathf.LerpUnclamped(2.2f, 1f, e);
                ribbon.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(-14f, -2.5f, e));
            });
            stamping = false;
        }
    }
}
