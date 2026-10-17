using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Control de volumen tipo barra de progreso: se toca o se arrastra sobre la barra. Etiqueta + barra (sin %).
    // boxed = true: el control va en su propio contenedor (tipo botón flotante) con poco padding.
    public class VolumeSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public const float RowH = 50, LabelW = 128, Gap = 8, Pad = 10, PadX = 20;

        RectTransform track, fill, knob;
        System.Func<float> get;
        System.Action<float> set;
        float shown = -1;

        // width = ancho del control (etiqueta + barra). Con boxed el contenedor suma Pad a cada lado.
        public static RectTransform Create(Transform parent, string label, Vector2 anchor, Vector2 pos, float width,
            System.Func<float> get, System.Action<float> set, bool boxed = false, Vector2? pivot = null)
        {
            RectTransform row;
            RectTransform outer;
            if (boxed)
            {
                outer = UiKit.Panel(parent, "Vol_" + label, 18, true).At(anchor, pos, new Vector2(width + PadX * 2, RowH + Pad * 2), pivot);
                row = UiKit.Rect("Row", outer).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, RowH));
            }
            else
            {
                row = outer = UiKit.Rect("Vol_" + label, parent).At(anchor, pos, new Vector2(width, RowH), pivot);
            }
            UiKit.Txt(row, "Label", label, 28, Color.white, TextAlignmentOptions.Left, true)
                .RT().At(new Vector2(0, 0.5f), Vector2.zero, new Vector2(LabelW, RowH), new Vector2(0, 0.5f));

            // barra: la zona táctil es más alta que la barra visible (dedo)
            float bw = width - LabelW - Gap;
            var hit = UiKit.Img(row, "Hit", null, new Color(0, 0, 0, 0), 0, true);
            hit.RT().At(new Vector2(0, 0.5f), new Vector2(LabelW + Gap, 0), new Vector2(bw, RowH), new Vector2(0, 0.5f));
            var trk = UiKit.Img(hit.transform, "Track", UiKit.RR, new Color(0.03f, 0.05f, 0.12f, 0.95f), 12);
            trk.RT().At(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(bw - 28, 26), new Vector2(0, 0.5f));
            var fl = UiKit.Img(trk.transform, "Fill", UiKit.RR, UiKit.Hex("#43c157"), 10);
            fl.RT().Fill(3, 3, 3, 3);
            fl.RT().anchorMax = new Vector2(0.5f, 1);
            // marcas de 10 en 10, como un progressbar segmentado
            for (int i = 1; i < 10; i++)
                UiKit.Img(trk.transform, "Tick", null, new Color(0, 0, 0, 0.35f)).RT()
                    .At(new Vector2(i / 10f, 0.5f), Vector2.zero, new Vector2(3, 18));
            var kn = UiKit.Img(trk.transform, "Knob", UiKit.Circle, Color.white);
            kn.RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            UiKit.Img(kn.transform, "Dot", UiKit.Circle, UiKit.Hex("#1d6b2a")).RT().At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));

            var s = hit.gameObject.AddComponent<VolumeSlider>();
            s.track = trk.RT(); s.fill = fl.RT(); s.knob = kn.RT(); s.get = get; s.set = set;
            s.Refresh();
            return outer;
        }

        public void OnPointerDown(PointerEventData e) => SetFrom(e);
        public void OnDrag(PointerEventData e) => SetFrom(e);

        void SetFrom(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var lp)) return;
            float v = Mathf.Clamp01((lp.x - track.rect.xMin) / Mathf.Max(1, track.rect.width));
            v = Mathf.Round(v * 20) / 20; // pasos de 5%
            set?.Invoke(v);
            Refresh();
        }

        void Update() { if (get != null && !Mathf.Approximately(get(), shown)) Refresh(); }

        void Refresh()
        {
            if (get == null) return;
            shown = get();
            fill.anchorMax = new Vector2(Mathf.Max(0.001f, shown), 1);
            fill.gameObject.SetActive(shown > 0.01f);
            knob.anchorMin = knob.anchorMax = new Vector2(shown, 0.5f);
            knob.anchoredPosition = Vector2.zero;
        }
    }
}
