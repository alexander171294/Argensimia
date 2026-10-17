using System.Collections;
using UnityEngine;

namespace Argensimia.UI
{
    // Visibilidad del HUD para el loop menú ↔ partida (GameFlow).
    public partial class GameHud
    {
        CanvasGroup hudGroup;
        Coroutine hudFade;

        public void SetHudVisible(bool on, float dur = 0.4f)
        {
            if (!canvas) return;
            if (!hudGroup) hudGroup = canvas.GetComponent<CanvasGroup>() ? canvas.GetComponent<CanvasGroup>() : canvas.gameObject.AddComponent<CanvasGroup>();
            hudGroup.blocksRaycasts = on;
            hudGroup.interactable = on;
            if (hudFade != null) StopCoroutine(hudFade);
            if (dur <= 0) { hudGroup.alpha = on ? 1 : 0; return; }
            float from = hudGroup.alpha, to = on ? 1 : 0;
            hudFade = StartCoroutine(UiKit.Tween(dur, t => { if (hudGroup) hudGroup.alpha = Mathf.Lerp(from, to, t); }));
        }
    }
}
