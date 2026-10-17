using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Mazo vacío: aparece arriba del mazo un panel para comprar cartas al azar (1 x $500 o 10 x $4.200).
    // Las compradas entran a los huecos de la mano; lo que sobra queda en el mazo.
    public partial class GameHud
    {
        RectTransform shopRt, shop1Rt, shop10Rt;
        TextMeshProUGUI shop1Cost, shop10Cost;
        CanvasGroup shopGroup, deckStackGroup;
        bool shopShown;

        void BuildShop()
        {
            var stack = deckRt.Find("Stack");
            if (stack) deckStackGroup = stack.gameObject.AddComponent<CanvasGroup>();

            // encima del mazo (que vacío no se usa): no ocupa lugar extra y queda por delante
            float dw = CardView.W * deckScale, dh = CardView.H * deckScale;
            float cx = 26 + dw * 0.5f, cy = tickerHeight + 14 + dh * 0.5f;
            shopRt = UiKit.Panel(safe, "Shop", 18).At(Vector2.zero, new Vector2(Mathf.Max(8, cx - 134), Mathf.Max(tickerHeight + 6, cy - 107)), new Vector2(268, 214));
            shopGroup = shopRt.gameObject.AddComponent<CanvasGroup>();
            UiKit.Txt(shopRt, "Title", "MAZO VACÍO", 24, UiKit.Gold, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(250, 32), new Vector2(0.5f, 1));
            shop1Rt = ShopButton("Buy1", "1 CARTA", deck.buyCost, new Vector2(0, -46), "#e0a020", "#8a5a06", out shop1Cost, null);
            shop10Rt = ShopButton("Buy10", "10 CARTAS", deck.buy10Cost, new Vector2(0, -128), "#43c157", "#1d6b2a", out shop10Cost,
                "-" + Mathf.RoundToInt((1 - deck.buy10Cost / (deck.buyCost * 10f)) * 100) + "%");
            shop1Rt.GetComponent<Tap>().onTap = () => OnBuy(1, deck.buyCost, shop1Rt);
            shop10Rt.GetComponent<Tap>().onTap = () => OnBuy(10, deck.buy10Cost, shop10Rt);
            shopRt.gameObject.SetActive(false);
        }

        RectTransform ShopButton(string name, string label, int cost, Vector2 pos, string faceHex, string baseHex, out TextMeshProUGUI costTxt, string tag)
        {
            var b = UiKit.Rect(name, shopRt).At(new Vector2(0.5f, 1), pos, new Vector2(240, 72), new Vector2(0.5f, 1));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex(baseHex), 18, true).RT().Fill();
            var f = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex(faceHex), 18).RT().Fill(0, 0, 0, 7);
            UiKit.Txt(f, "T", label, 24, Color.white, TextAlignmentOptions.Center, true).RT().At(new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(230, 30), new Vector2(0.5f, 1));
            var pill = UiKit.Img(f, "Cost", UiKit.RR, new Color(0, 0, 0, 0.35f), 12).RT().At(new Vector2(0.5f, 0), new Vector2(0, 5), new Vector2(150, 30), new Vector2(0.5f, 0));
            UiKit.Coin(pill, 24).At(new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(24, 24));
            costTxt = UiKit.Txt(pill, "T", "$" + GameState.Money(cost), 22, Color.white, TextAlignmentOptions.Center, true);
            costTxt.RT().Fill(28, 4, 0, 0);
            if (tag != null)
            {
                var t = UiKit.Img(b, "Tag", UiKit.RR, UiKit.Red, 10).RT().At(Vector2.one, new Vector2(10, 12), new Vector2(64, 28), Vector2.one);
                t.localRotation = Quaternion.Euler(0, 0, -10);
                UiKit.Txt(t, "T", tag, 18, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            }
            var tap = b.gameObject.AddComponent<Tap>();
            tap.visual = b;
            return b;
        }

        void UpdateShop()
        {
            bool show = deck.DrawCount == 0 && (gs.phase == Phase.Previa || gs.phase == Phase.Oleada);
            if (deckStackGroup) deckStackGroup.alpha = deck.DrawCount == 0 ? 0.3f : 1f;
            if (show != shopShown)
            {
                shopShown = show;
                if (show)
                {
                    shopRt.gameObject.SetActive(true);
                    shopRt.SetAsLastSibling();
                    StartCoroutine(UiKit.Tween(0.3f, t => { if (shopRt) shopRt.localScale = Vector3.one * Mathf.Max(0, Ease.OutBack(t)); }));
                }
                else shopRt.gameObject.SetActive(false);
            }
            if (!show) return;
            shop1Cost.color = gs.CanAfford(deck.buyCost) ? Color.white : UiKit.RedSoft;
            shop10Cost.color = gs.CanAfford(deck.buy10Cost) ? Color.white : UiKit.RedSoft;
            shopGroup.alpha = busy ? 0.6f : 1f;
        }

        void OnBuy(int n, int cost, RectTransform btn)
        {
            if (busy || flowBusy || zoomed || placing || paused || overlay || dragging || deck.DrawCount > 0) return;
            if (!gs.Spend(cost))
            {
                StartCoroutine(Shake(btn));
                FloatText(moneyPanel, L.T("FALTA $") + GameState.Money(cost - gs.money + 0.999f), UiKit.RedSoft, 0);
                return;
            }
            FloatText(moneyPanel, "-$" + GameState.Money(cost), UiKit.RedSoft, 0);
            for (int i = 0; i < n; i++) deck.AddToDeck(deck.RollOne(gs.mandate, null));
            Headlines.Say(n > 1 ? "buy10" : "buy", 8f);
            Snd.Ui("spend");
            Snd.Ui("card_shuffle");
            StartCoroutine(BuyRoutine());
        }

        IEnumerator BuyRoutine()
        {
            StartCoroutine(Pulse(deckRt));
            yield return DealHand(0.15f); // completa los huecos de la mano; el resto queda en el mazo
        }
    }
}
