using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Argensimia.UI
{
    // Arrastre de una carta colocable desde la mano al mapa: soltarla sobre una defensa igual la sube de nivel (máx. 3);
    // soltarla sobre un slot libre la coloca. Si no hay dónde (o falta plata), vuelve a la mano.
    public class CardDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<PointerEventData> onBegin, onDrag, onEnd;
        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold = true; }
        public void OnBeginDrag(PointerEventData e) => onBegin?.Invoke(e);
        public void OnDrag(PointerEventData e) => onDrag?.Invoke(e);
        public void OnEndDrag(PointerEventData e) => onEnd?.Invoke(e);
    }

    public partial class GameHud
    {
        CardView dragging;
        RectTransform sellZone;
        TMPro.TextMeshProUGUI sellZoneTxt;
        UnityEngine.UI.Image sellZoneFace;

        // Zona de venta mientras se arrastra: aparece sobre el mazo (fuera del campo, no tapa slots). Soltar ahí vende.
        void ShowSellZone(CardView cv, bool on)
        {
            if (!sellZone)
            {
                // del tamaño del mazo, justo encima de él
                sellZone = UiKit.Rect("SellZone", zoomLayer).At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardView.W * deckScale, CardView.H * deckScale));
                UiKit.Img(sellZone, "Ring", UiKit.RR, Color.white, 20).RT().Fill();
                sellZoneFace = UiKit.Img(sellZone, "Face", UiKit.RR, UiKit.Hex("#2f9a4a"), 16);
                sellZoneFace.RT().Fill(5, 5, 5, 5);
                // Midline: centra el glifo por su forma, no por la altura de línea de la fuente (si no, queda alto)
                UiKit.Txt(sellZone, "Dollar", "$", 72, Color.white, TextAlignmentOptions.Midline, false, UiKit.FontLogo).RT().At(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(110, 84), new Vector2(0.5f, 1));
                sellZoneTxt = UiKit.Txt(sellZone, "T", "", 21, Color.white, TextAlignmentOptions.Center, true);
                sellZoneTxt.RT().At(new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(120, 56), new Vector2(0.5f, 0));
                sellZoneTxt.lineSpacing = -10;
            }
            sellZone.gameObject.SetActive(on);
            if (!on) return;
            // sobre el mazo, pero entera en pantalla (sin pisar la marquesina)
            var lp = zoomLayer.InverseTransformPoint(deckRt.TransformPoint(deckRt.rect.center));
            var r = zoomLayer.rect;
            var hs = sellZone.sizeDelta * 0.5f;
            lp.x = Mathf.Max(lp.x, r.xMin + hs.x + 8);
            lp.y = Mathf.Max(lp.y, r.yMin + tickerHeight + hs.y + 8);
            sellZone.localPosition = lp;
            sellZone.localScale = Vector3.one;
            sellZoneTxt.text = L.T("VENDER") + "\n+$" + GameState.Money(SellPrice(cv.def));
            sellZone.SetAsLastSibling();
            cv.transform.SetAsLastSibling();
        }

        bool OverSellZone(Vector2 screen) => sellZone && sellZone.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(sellZone, screen, null);

        void AddDrag(CardView cv)
        {
            var d = cv.gameObject.AddComponent<CardDrag>();
            d.onBegin = e => DragBegin(cv, e);
            d.onDrag = e => DragMove(cv, e);
            d.onEnd = e => DragEnd(cv, e);
        }

        bool CanDrag(CardView cv) =>
            cv && !busy && !flowBusy && !zoomed && !placing && !paused && !overlay && !dragging &&
            cv.tap.interactable && !cv.Disabled && cv.transform.parent == hand &&
            (gs.phase == Phase.Previa || gs.phase == Phase.Oleada);

        // La zona de la mano (abajo): soltar ahí es cancelar.
        bool OverHand(Vector2 screen)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var lp)) return true;
            float bottom = -root.rect.height * 0.5f;
            return lp.y < bottom + tickerHeight + 14 + CardView.H * handScale * 0.85f;
        }

        void DragBegin(CardView cv, PointerEventData e)
        {
            if (!CanDrag(cv)) return;
            // tutorial: solo la carta que pide el paso (o cualquiera si el paso es vender)
            if (!TutAllows("card", cv.def) && !TutAllows("sell", cv.def)) { TutBlock("card", cv.def, cv.Rt); return; }
            dragging = cv;
            Snd.Ui("card_zoom", 0.6f, 1.15f);
            cv.transform.SetParent(zoomLayer, true);
            cv.transform.SetAsLastSibling();
            cv.group.blocksRaycasts = false;
            ShowSellZone(cv, TutAllows("sell", cv.def));
            if (cv.def.kind == CardKind.Colocar)
            {
                if (PoliceSlots.I && !cv.def.TargetsDefense) PoliceSlots.I.SetHighlight(true);
                Defense.ShowHints(cv.def, true);
            }
            DragMove(cv, e);
        }

        void DragMove(CardView cv, PointerEventData e)
        {
            if (dragging != cv || !cv) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zoomLayer, e.position, null, out var lp);
            bool overSell = OverSellZone(e.position);
            if (sellZone && sellZone.gameObject.activeSelf)
            {
                sellZone.localScale = Vector3.one * (overSell ? 1.1f : 1f);
                sellZoneFace.color = overSell ? UiKit.Hex("#43c157") : UiKit.Hex("#2f9a4a");
            }
            // chica y por arriba del dedo, para que se vea dónde cae
            bool overMap = !OverHand(e.position);
            cv.transform.localPosition = (Vector3)lp + Vector3.up * (overMap ? 95f : 30f);
            cv.transform.localRotation = Quaternion.identity;
            cv.transform.localScale = Vector3.one * handScale * (overMap ? 0.6f : 1f);
            Defense up = null;
            bool valid = false;
            if (cv.def.kind != CardKind.Colocar) valid = overMap && CanPlayDropped(cv.def);
            else if (overMap && GroundPoint(e.position, out var p))
            {
                PlaceTarget(cv.def, p, out up, out var t, out _);
                valid = up || t != null;
            }
            Defense.HoverHint(up);
            cv.group.alpha = valid || !overMap ? 1f : 0.7f;
        }

        void DragEnd(CardView cv, PointerEventData e)
        {
            if (dragging != cv || !cv) return;
            dragging = null;
            bool sell = OverSellZone(e.position);
            EndDragFx();
            cv.group.blocksRaycasts = true;
            cv.group.alpha = 1;
            if (sell && TutAllows("sell", cv.def) && !paused && (gs.phase == Phase.Previa || gs.phase == Phase.Oleada)) { QuickSell(cv); return; }
            if (!TutAllows("card", cv.def)) { TutBlock("card", cv.def, null); Snd.Ui("card_flip", 0.6f); StartCoroutine(DragReturn(cv)); return; }
            bool live = !paused && (gs.phase == Phase.Previa || gs.phase == Phase.Oleada) && !OverHand(e.position);
            if (cv.def.kind != CardKind.Colocar)
            {
                // efecto / ministro: soltarla en cualquier lado de la partida la activa (sin abrirla)
                if (live && CanPlayDropped(cv.def)) { StartCoroutine(PlayEffect(cv)); return; }
                if (live && !gs.CanAfford(cv.def.cost)) { FloatText(moneyPanel, L.T("FALTA $") + GameState.Money(cv.def.cost - gs.money + 0.999f), UiKit.RedSoft, 0); StartCoroutine(Shake(moneyPanel)); }
                Snd.Ui("card_flip", 0.6f); StartCoroutine(DragReturn(cv));
                return;
            }
            bool ok = live && GroundPoint(e.position, out var p) && TryUsePlaceCard(cv, p);
            if (!ok) { Snd.Ui("card_flip", 0.6f); StartCoroutine(DragReturn(cv)); }
        }

        bool CanPlayDropped(CardDef d) => gs.CanAfford(d.cost) && (!d.previaOnly || gs.phase == Phase.Previa);

        void EndDragFx()
        {
            ShowSellZone(null, false);
            if (PoliceSlots.I) PoliceSlots.I.SetHighlight(false);
            Defense.HoverHint(null);
            Defense.ShowHints(null, false);
        }

        // Cierre o fin de partida con una carta en el aire: vuelve a la mano.
        void CancelDrag()
        {
            if (!dragging) return;
            var cv = dragging;
            dragging = null;
            EndDragFx();
            cv.group.blocksRaycasts = true;
            cv.group.alpha = 1;
            StartCoroutine(DragReturn(cv));
        }

        IEnumerator DragReturn(CardView cv)
        {
            if (!cv) yield break;
            cv.transform.SetParent(hand, true);
            Vector3 from = cv.transform.localPosition;
            float s0 = cv.transform.localScale.x, zEnd = SlotZ(cv.slot);
            yield return UiKit.Tween(0.3f, t =>
            {
                if (!cv) return;
                float k = Ease.OutCubic(t);
                cv.transform.localPosition = Vector3.Lerp(from, SlotPos(cv.slot), k);
                cv.transform.localScale = Vector3.one * Mathf.LerpUnclamped(s0, handScale, Ease.OutBack(t));
                cv.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, zEnd, k));
            });
            if (cv) ReturnToHand(cv);
        }
    }
}
