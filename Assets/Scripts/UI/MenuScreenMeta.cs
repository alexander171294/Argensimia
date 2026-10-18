using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Argensimia.UI
{
    // Menú: recompensas ganadas en la partida (cartas desbloqueadas y mejoras permanentes) y panel de LOGROS.
    public partial class MenuScreen
    {
        RectTransform rewardLayer, achPanel;

        // ------------------------------------------------------------------ recompensas al volver al menú

        // Se muestran una por una, después del cartel de ranking si lo hay.
        public void ShowRewards(List<string> list)
        {
            if (list == null || list.Count == 0) return;
            StartCoroutine(RewardsRoutine(list));
        }

        IEnumerator RewardsRoutine(List<string> list)
        {
            yield return new WaitForSecondsRealtime(1.0f);
            foreach (var id in list)
            {
                while (prompt) yield return null;
                bool done = false;
                ShowReward(id, () => done = true);
                while (!done) yield return null;
            }
        }

        void ShowReward(string id, System.Action onClose)
        {
            if (rewardLayer) Destroy(rewardLayer.gameObject);
            var dim = UiKit.Img(root, "Reward", null, new Color(0.02f, 0.04f, 0.1f, 0), 0, true);
            rewardLayer = dim.RT();
            rewardLayer.Fill();
            StartCoroutine(UiKit.Tween(0.3f, t => { if (dim) dim.color = new Color(0.02f, 0.04f, 0.1f, 0.9f * t); }));
            bool perk = id.StartsWith("perk:");
            var title = UiKit.Txt(rewardLayer, "Title", perk ? "¡MEJORA PERMANENTE!" : "¡NUEVA CARTA DESBLOQUEADA!", 60, UiKit.Gold, TextAlignmentOptions.Center, true);
            title.RT().At(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1400, 76), new Vector2(0.5f, 1));
            string sub = L.F("Sobreviviste {0} mandatos en total.", Meta.TotalMandates);
            string detail;
            RectTransform hero;
            if (perk)
            {
                int n = int.Parse(id.Substring(5));
                bool money = n % 2 == 1;
                hero = UiKit.Panel(rewardLayer, "Perk", 30).At(new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(540, 340));
                if (money) { var c = UiKit.Coin(hero, 170); c.anchoredPosition = new Vector2(0, 40); }
                else
                {
                    var cup = UiKit.Rect("Cup", hero).At(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(170, 170));
                    UiKit.Img(cup, "Ring", UiKit.Circle, UiKit.GoldDark).RT().Fill();
                    UiKit.Img(cup, "In", UiKit.Circle, UiKit.Gold).RT().Fill(12, 12, 12, 12);
                    AchievementToast.Trophy(cup, 2.2f);
                }
                UiKit.Txt(hero, "T", money ? "+$250 AL EMPEZAR" : "+1 GOBERNABILIDAD MÁX.", 36, Color.white, TextAlignmentOptions.Center, true)
                    .RT().At(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(520, 50), new Vector2(0.5f, 0));
                detail = L.T(money ? "Cada partida nueva arranca con más plata." : "Cada partida nueva arranca con más gobernabilidad.");
            }
            else
            {
                var def = Resources.Load<CardDef>("Cards/" + id);
                var cv = def ? CardView.Create(rewardLayer, def) : null;
                hero = cv ? cv.Rt : UiKit.Rect("Empty", rewardLayer);
                if (cv) { cv.dimWhenPoor = false; cv.tap.interactable = false; }
                hero.anchorMin = hero.anchorMax = new Vector2(0.5f, 0.5f);
                hero.anchoredPosition = new Vector2(0, 52);
                detail = L.T("Desde tu próxima partida puede salir en las cartas del cierre de cada mandato.");
            }
            string next = Meta.AllUnlocked
                ? L.F("Ya tenés todas las cartas: cada {0} mandatos ganás una mejora permanente.", Meta.MandatesPerUnlock)
                : L.F("Próxima carta en {0} mandatos.", Meta.ToNextReward);
            var info = UiKit.Txt(rewardLayer, "Info", sub + "  " + detail + "\n<color=#f2c14e>" + next + "</color>", 26, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody);
            info.RT().At(new Vector2(0.5f, 0), new Vector2(0, 156), new Vector2(1300, 76), new Vector2(0.5f, 0));
            info.textWrappingMode = TextWrappingModes.Normal;
            // entrada: la carta gira y cae con rebote
            hero.localScale = Vector3.zero;
            StartCoroutine(UiKit.Tween(0.6f, k =>
            {
                if (!hero) return;
                hero.localScale = Vector3.one * Mathf.Max(0, Ease.OutBack(k)) * (perk ? 1f : 0.86f);
                hero.localRotation = Quaternion.Euler(0, (1 - Ease.OutCubic(k)) * 360f, 0);
            }));
            Snd.Ui("fanfare");
            StartCoroutine(SndLater("card_flip", 0.25f));
            // botón
            var b = UiKit.Rect("Ok", rewardLayer).At(new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(360, 96), new Vector2(0.5f, 0));
            UiKit.Img(b, "Base", UiKit.RR, UiKit.Hex("#1d6b2a"), 26, true).RT().Fill();
            var face = UiKit.Img(b, "Face", UiKit.RR, UiKit.Hex("#43c157"), 26).RT().Fill(0, 0, 0, 9);
            UiKit.Txt(face, "T", "¡BIEN!", 42, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            var tap = b.gameObject.AddComponent<Tap>();
            tap.visual = b;
            var layer = rewardLayer;
            tap.onTap = () =>
            {
                tap.interactable = false;
                StartCoroutine(UiKit.Tween(0.25f, k => { if (layer) layer.localScale = Vector3.one * (1 - 0.05f * k); if (dim) dim.color = new Color(0.02f, 0.04f, 0.1f, 0.8f * (1 - k)); }));
                Destroy(layer.gameObject, 0.26f);
                onClose();
            };
        }

        IEnumerator SndLater(string id, float d) { yield return new WaitForSecondsRealtime(d); Snd.Ui(id, 0.9f, 1, 0); }

        // ------------------------------------------------------------------ LOGROS

        void BuildAchievementsButton(float y)
        {
            var b = SideButton("Logros", "LOGROS", "#c9952a", "#7a5410", y);
            var cup = UiKit.Rect("Icon", b).At(new Vector2(0, 0.5f), new Vector2(18, 2), new Vector2(48, 48), new Vector2(0, 0.5f));
            UiKit.Img(cup, "Ring", UiKit.Circle, Color.white).RT().Fill();
            UiKit.Img(cup, "In", UiKit.Circle, UiKit.Gold).RT().Fill(4, 4, 4, 4);
            AchievementToast.Trophy(cup, 0.62f);
            // contador
            var badge = UiKit.Img(b, "Count", UiKit.RR, UiKit.Hex("#d9342b"), 12).RT();
            badge.At(new Vector2(1, 1), new Vector2(10, 12), new Vector2(72, 32), new Vector2(1, 1));
            UiKit.Txt(badge, "T", Meta.AchievementCount + "/" + Achievements.All.Length, 18, Color.white, TextAlignmentOptions.Center, true).RT().Fill();
            b.GetComponent<Tap>().onTap = ShowAchievements;
        }

        void ShowAchievements()
        {
            if (achPanel) return;
            var dim = UiKit.Img(root, "Achievements", null, new Color(0.02f, 0.04f, 0.1f, 0.8f), 0, true);
            achPanel = dim.RT();
            achPanel.Fill();
            var p = UiKit.Panel(achPanel, "Box", 24, true).At(new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1440, 830));
            UiKit.Txt(p, "Title", "LOGROS", 54, UiKit.Gold, TextAlignmentOptions.Center, true)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(600, 64), new Vector2(0.5f, 1));
            UiKit.Txt(p, "Count", Meta.AchievementCount + " / " + Achievements.All.Length, 28, Color.white, TextAlignmentOptions.Right, true)
                .RT().At(new Vector2(1, 1), new Vector2(-90, -28), new Vector2(240, 40), new Vector2(1, 1));

            // progreso de cartas desbloqueables
            string head = L.F("Mandatos sobrevividos en total: <color=#f2c14e>{0}</color>", Meta.TotalMandates) + "    ·    "
                + (Meta.AllUnlocked ? L.F("Mejoras permanentes: <color=#f2c14e>{0}</color> (+${1} al empezar, +{2} gob. máx.)", Meta.Perks, Meta.StartMoneyBonus, Meta.MaxGovBonus)
                                    : L.F("Próxima carta en <color=#f2c14e>{0}</color> mandatos", Meta.ToNextReward));
            UiKit.Txt(p, "Head", head, 22, UiKit.Cream, TextAlignmentOptions.Center, false, UiKit.FontBody)
                .RT().At(new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(1360, 30), new Vector2(0.5f, 1));
            var order = Meta.UnlockOrder;
            const float cw = 128, cgap = 8;
            float total = order.Length * cw + (order.Length - 1) * cgap;
            for (int i = 0; i < order.Length; i++)
            {
                bool got = Meta.IsUnlocked(order[i]);
                var def = got ? Resources.Load<CardDef>("Cards/" + order[i]) : null;
                var chip = UiKit.Img(p, "Card_" + order[i], UiKit.RR, got && def ? def.color : new Color(1, 1, 1, 0.08f), 12).RT();
                chip.At(new Vector2(0.5f, 1), new Vector2(-total / 2 + cw / 2 + i * (cw + cgap), -122), new Vector2(cw, 56), new Vector2(0.5f, 1));
                var t = UiKit.Txt(chip, "T", got && def ? L.CardTitle(def).ToUpperInvariant() : "?", 18, got ? Color.white : new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Center, true);
                t.RT().Fill(6, 6, 2, 18);
                t.enableAutoSizing = true; t.fontSizeMin = 11; t.fontSizeMax = 18;
                UiKit.Txt(chip, "M", L.F("{0} mand.", (i + 1) * Meta.MandatesPerUnlock), 12, new Color(1, 1, 1, got ? 0.85f : 0.45f), TextAlignmentOptions.Center, false, UiKit.FontBody)
                    .RT().At(new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(cw, 16), new Vector2(0.5f, 0));
            }

            // grilla de logros: 2 columnas
            var list = Achievements.All;
            int rows = (list.Length + 1) / 2;
            const float rw = 680, rh = 56, rgap = 6, top = -196;
            for (int i = 0; i < list.Length; i++)
            {
                var a = list[i];
                int col = i / rows, row = i % rows;
                bool got = Achievements.Has(a.id);
                var r = UiKit.Img(p, "Ach_" + a.id, UiKit.RR, got ? new Color(0.95f, 0.75f, 0.2f, 0.22f) : new Color(1, 1, 1, 0.05f), 12).RT();
                r.At(new Vector2(0.5f, 1), new Vector2((col == 0 ? -1 : 1) * (rw / 2 + 8), top - row * (rh + rgap)), new Vector2(rw, rh), new Vector2(0.5f, 1));
                var ic = UiKit.Rect("Icon", r).At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(44, 44), new Vector2(0, 0.5f));
                UiKit.Img(ic, "Ring", UiKit.Circle, got ? UiKit.GoldDark : new Color(1, 1, 1, 0.15f)).RT().Fill();
                UiKit.Img(ic, "In", UiKit.Circle, got ? UiKit.Gold : new Color(0.2f, 0.24f, 0.34f)).RT().Fill(3, 3, 3, 3);
                if (got) AchievementToast.Trophy(ic, 0.55f);
                else UiKit.Txt(ic, "Lock", "?", 22, new Color(1, 1, 1, 0.45f), TextAlignmentOptions.Center, true).RT().Fill();
                var tt = UiKit.Txt(r, "T", a.Title, 22, got ? Color.white : new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Left, true);
                tt.RT().At(new Vector2(0, 1), new Vector2(62, -4), new Vector2(480, 28), new Vector2(0, 1));
                UiKit.Txt(r, "D", a.Desc, 16, got ? UiKit.Cream : new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Left, false, UiKit.FontBody)
                    .RT().At(new Vector2(0, 0), new Vector2(62, 5), new Vector2(520, 22), new Vector2(0, 0));
                if (a.counter != null && !got)
                {
                    int v = Achievements.Progress(a);
                    var bar = UiKit.Img(r, "Bar", UiKit.RR, new Color(0, 0, 0, 0.35f), 6).RT();
                    bar.At(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(110, 14), new Vector2(1, 0.5f));
                    var fill = UiKit.Img(bar, "Fill", UiKit.RR, UiKit.Gold, 6).RT();
                    fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01((float)v / a.goal), 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
                    UiKit.Txt(r, "P", v + "/" + a.goal, 14, Color.white, TextAlignmentOptions.Right, false, UiKit.FontBody)
                        .RT().At(new Vector2(1, 1), new Vector2(-12, -3), new Vector2(110, 18), new Vector2(1, 1));
                }
                else if (got)
                {
                    var ok = UiKit.Img(r, "Ok", UiKit.Circle, UiKit.Hex("#43c157")).RT();
                    ok.At(new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(32, 32), new Vector2(1, 0.5f));
                    // tilde con dos barras
                    UiKit.Img(ok, "A", null, Color.white).RT().At(new Vector2(0.5f, 0.5f), new Vector2(-5, -3), new Vector2(5, 11)).localRotation = Quaternion.Euler(0, 0, 45);
                    UiKit.Img(ok, "B", null, Color.white).RT().At(new Vector2(0.5f, 0.5f), new Vector2(3, 1), new Vector2(5, 19)).localRotation = Quaternion.Euler(0, 0, -40);
                }
            }

            // cerrar
            var x = UiKit.Rect("Close", p).At(new Vector2(1, 1), new Vector2(-18, -18), new Vector2(60, 60), new Vector2(1, 1));
            UiKit.Img(x, "Bg", UiKit.Circle, UiKit.Hex("#d9534a")).RT().Fill();
            UiKit.Txt(x, "X", "X", 30, Color.white, TextAlignmentOptions.Center, false, UiKit.FontLogo).RT().Fill(0, 0, 2, 0);
            var tap = x.gameObject.AddComponent<Tap>();
            tap.visual = x;
            tap.onTap = () => { if (achPanel) Destroy(achPanel.gameObject); achPanel = null; };
            p.localScale = Vector3.zero;
            StartCoroutine(UiKit.Tween(0.3f, k => { if (p) p.localScale = Vector3.one * Mathf.Max(0, Ease.OutBack(k)); }));
        }
    }
}
