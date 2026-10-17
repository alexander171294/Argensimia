using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Pantalla de carga del arranque: fondo azul noche, ARGENSIMIA y "CARGANDO..." con un sol que gira.
    // Se va con fundido cuando los cuadros salen fluidos (WaitStable), así el menú no arranca "tildado".
    public class LoadingScreen : MonoBehaviour
    {
        CanvasGroup group;
        TextMeshProUGUI label;
        RectTransform sun;

        public static LoadingScreen Create()
        {
            var go = new GameObject("LoadingUI", typeof(RectTransform)) { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 1;
            go.AddComponent<GraphicRaycaster>(); // bloquea toques mientras carga
            var l = go.AddComponent<LoadingScreen>();
            l.group = go.AddComponent<CanvasGroup>();
            var root = (RectTransform)go.transform;
            UiKit.Img(root, "Bg", null, UiKit.NavyDark).RT().Fill();
            UiKit.Logo(root, "Title", 132).RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1400, 150));
            l.sun = UiKit.Img(root, "Sun", UiKit.Circle, UiKit.Hex("#c98a12")).RT();
            l.sun.At(new Vector2(0.5f, 0.5f), new Vector2(-162, -70), new Vector2(46, 46));
            UiKit.Img(l.sun, "In", UiKit.Circle, UiKit.Hex("#f6b40e")).RT().Fill(6, 6, 6, 6);
            for (int i = 0; i < 8; i++)
            {
                var ray = UiKit.Img(l.sun, "Ray", null, UiKit.Hex("#f6b40e")).RT();
                ray.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 18), new Vector2(0.5f, -1.5f));
                ray.localRotation = Quaternion.Euler(0, 0, i * 45);
            }
            l.label = UiKit.Txt(root, "Label", "CARGANDO", 36, UiKit.Cream, TextAlignmentOptions.Left, true);
            l.label.RT().At(new Vector2(0.5f, 0.5f), new Vector2(-92, -70), new Vector2(320, 50), new Vector2(0, 0.5f));
            return l;
        }

        void Update()
        {
            if (sun) sun.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 90f);
            if (label) label.text = L.T("CARGANDO") + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
        }

        // Espera hasta que haya ~15 cuadros seguidos fluidos (mínimo minT, máximo maxT segundos).
        public static IEnumerator WaitStable(float minT = 0.6f, float maxT = 8f)
        {
            float t = 0;
            int good = 0;
            while (t < maxT)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                t += dt;
                good = dt < 1f / 24f ? good + 1 : 0;
                if (t >= minT && good >= 15) break;
            }
        }

        public IEnumerator FadeOut(float d)
        {
            yield return UiKit.Tween(d, k => { if (group) group.alpha = 1 - k; });
            Destroy(gameObject);
        }
    }
}
