using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Aviso de logro desbloqueado (estilo Steam): baja desde arriba, queda 3,2 s y se va. Varios en cola.
    // Vive en su propio canvas persistente, así sobrevive a las recargas de escena (menú ↔ partida).
    public class AchievementToast : MonoBehaviour
    {
        static AchievementToast I;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => I = null;
        readonly Queue<AchievementDef> queue = new Queue<AchievementDef>();
        RectTransform root;
        bool showing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I) return;
            var go = new GameObject("AchievementToast", typeof(RectTransform)) { layer = 5 };
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            I = go.AddComponent<AchievementToast>();
            I.root = (RectTransform)go.transform;
            Achievements.Unlocked += a => { if (I) I.Enqueue(a); };
        }

        void Enqueue(AchievementDef a)
        {
            queue.Enqueue(a);
            if (!showing) StartCoroutine(Run());
        }

        void OnApplicationPause(bool p) { if (p) Meta.SaveIfDirty(); }
        void OnApplicationQuit() => Meta.SaveIfDirty();

        IEnumerator Run()
        {
            showing = true;
            while (queue.Count > 0)
            {
                var a = queue.Dequeue();
                var box = UiKit.Panel(root, "Toast", 20).At(new Vector2(1, 1), new Vector2(660, -262), new Vector2(560, 104), new Vector2(1, 1));
                // trofeo
                var cup = UiKit.Rect("Cup", box).At(new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(78, 78), new Vector2(0, 0.5f));
                UiKit.Img(cup, "Ring", UiKit.Circle, UiKit.GoldDark).RT().Fill();
                UiKit.Img(cup, "In", UiKit.Circle, UiKit.Gold).RT().Fill(6, 6, 6, 6);
                Trophy(cup);
                UiKit.Txt(box, "Head", L.En ? "ACHIEVEMENT UNLOCKED" : "LOGRO DESBLOQUEADO", 18, UiKit.Gold, TextAlignmentOptions.Left, false, UiKit.FontBody)
                    .RT().At(new Vector2(0, 1), new Vector2(112, -12), new Vector2(430, 24), new Vector2(0, 1));
                var t = UiKit.Txt(box, "Title", a.Title, 34, Color.white, TextAlignmentOptions.Left, true);
                t.RT().At(new Vector2(0, 1), new Vector2(112, -34), new Vector2(430, 38), new Vector2(0, 1));
                t.enableAutoSizing = true; t.fontSizeMin = 20; t.fontSizeMax = 34;
                var d = UiKit.Txt(box, "Desc", a.Desc, 19, UiKit.Cream, TextAlignmentOptions.Left, false, UiKit.FontBody);
                d.RT().At(new Vector2(0, 1), new Vector2(112, -72), new Vector2(430, 24), new Vector2(0, 1));
                d.enableAutoSizing = true; d.fontSizeMin = 12; d.fontSizeMax = 19;
                Snd.Ui("fanfare", 0.7f, 1.15f, 0);
                yield return UiKit.Tween(0.45f, k => { if (box) box.anchoredPosition = new Vector2(Mathf.LerpUnclamped(660, -24, Ease.OutBack(k)), -262); });
                float t0 = Time.unscaledTime;
                while (Time.unscaledTime - t0 < 3.2f)
                {
                    if (box) box.localScale = Vector3.one * (1 + Mathf.Sin((Time.unscaledTime - t0) * 5f) * 0.01f);
                    yield return null;
                }
                yield return UiKit.Tween(0.3f, k => { if (box) box.anchoredPosition = new Vector2(Mathf.Lerp(-24, 660, Ease.InCubic(k)), -262); });
                if (box) Destroy(box.gameObject);
                yield return new WaitForSecondsRealtime(0.15f);
            }
            showing = false;
        }

        // Copa con primitivas de UI: copa, asas, pie y base.
        public static void Trophy(RectTransform p, float s = 1)
        {
            var dark = UiKit.Hex("#8a5306");
            UiKit.Img(p, "Bowl", UiKit.RR, dark, 10).RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, 8 * s), new Vector2(34 * s, 28 * s));
            foreach (float x in new[] { -19f, 19f })
            {
                var h = UiKit.Img(p, "Handle", UiKit.Circle, dark).RT();
                h.At(new Vector2(0.5f, 0.5f), new Vector2(x * s, 10 * s), new Vector2(12 * s, 14 * s));
                UiKit.Img(h, "Hole", UiKit.Circle, UiKit.Gold).RT().Fill(3 * s, 3 * s, 3 * s, 3 * s);
            }
            UiKit.Img(p, "Stem", null, dark).RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, -12 * s), new Vector2(8 * s, 12 * s));
            UiKit.Img(p, "Base", UiKit.RR, dark, 4).RT().At(new Vector2(0.5f, 0.5f), new Vector2(0, -20 * s), new Vector2(26 * s, 7 * s));
        }
    }
}
