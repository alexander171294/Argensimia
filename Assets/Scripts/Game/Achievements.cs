using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Logros estilo Steam. Se guardan en Meta (persisten entre partidas). Los de contador avanzan con Count(clave);
    // los de un solo paso se dan con Unlock(id). Al desbloquear uno sale un aviso (AchievementToast) y suena.
    // Los id quedan listos para mapear a Steamworks si algún día se publica ahí.
    public class AchievementDef
    {
        public string id, title, desc, titleEn, descEn;
        public string counter; // null = de un solo paso
        public int goal = 1;
        public string Title => L.En ? titleEn : title;
        public string Desc => L.En ? descEn : desc;
    }

    public static class Achievements
    {
        static AchievementDef A(string id, string t, string d, string te, string de, string counter = null, int goal = 1) =>
            new AchievementDef { id = id, title = t, desc = d, titleEn = te, descEn = de, counter = counter, goal = goal };

        public static readonly AchievementDef[] All =
        {
            A("asumio", "Asumió", "Sobreviví el primer mandato.", "Sworn In", "Survive your first term."),
            A("segundo_mandato", "Segundo mandato", "Llegá al mandato 5.", "Second Term", "Reach term 5."),
            A("reeleccion", "Reelección indefinida", "Llegá al mandato 10.", "Indefinite Reelection", "Reach term 10."),
            A("eterno", "Eterno", "Llegá al mandato 20.", "Eternal", "Reach term 20."),
            A("orden", "La casa está en orden", "Cerrá un mandato sin que nadie llegue al vallado.", "The House Is in Order", "End a term with nobody reaching the fence."),
            A("mal_bien", "Estamos mal pero vamos bien", "Sobreviví un mandato en ESTALLIDO.", "Bad but Getting Better", "Survive a term during an UPRISING."),
            A("tranqui", "Tranqui total", "Llegá al mandato 10 con 70% de popularidad o más.", "Totally Calm", "Reach term 10 with 70% popularity or more."),
            A("sin_represion", "Sin represión", "Llegá al mandato 5 sin colocar escopeteros ni lanzagases.", "No Repression", "Reach term 5 without deploying shotguns or tear gas."),
            A("que_se_vayan", "¡Que se vayan todos!", "Renunciá en el primer mandato.", "Out With Them All!", "Resign during the first term."),
            A("helicoptero", "Al helicóptero", "Perdé la gobernabilidad.", "To the Helicopter", "Lose governability."),
            A("maquinita", "Emisión sin respaldo", "Usá La maquinita 25 veces.", "Unbacked Printing", "Use the Money Printer 25 times.", "play_maquinita", 25),
            A("cadena", "En cadena", "Usá Cadena nacional 50 veces.", "On Every Channel", "Use National Address 50 times.", "play_cadena_nacional", 50),
            A("muralla", "Muralla", "Colocá 100 vallados.", "The Wall", "Deploy 100 barricades.", "place_vallado", 100),
            A("mano_dura", "Mano dura", "Detené a 500 manifestantes.", "Iron Fist", "Arrest 500 protesters.", "detenidos", 500),
            A("pagaste", "Pagaste todas", "Evitá 10 crisis pagando.", "Paid Them All", "Avoid 10 crises by paying.", "crisis_evitadas", 10),
            A("dorado", "Todo dorado", "Subí una defensa a nivel 3.", "Gold Plated", "Upgrade a defense to level 3."),
            A("gabinete", "Gabinete completo", "Tené 3 ministros a la vez.", "Full Cabinet", "Have 3 ministers at once."),
            A("aparato", "El aparato", "Tené 3 punteros a la vez en la Plaza.", "The Machine", "Have 3 brokers at once in the Plaza."),
            A("coleccionista", "Coleccionista", "Desbloqueá todas las cartas.", "Collector", "Unlock every card."),
        };

        public static event System.Action<AchievementDef> Unlocked;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Unlocked = null;

        public static AchievementDef Get(string id) => System.Array.Find(All, a => a.id == id);
        public static bool Has(string id) => Meta.HasAchievement(id);
        public static int Progress(AchievementDef a) => a.counter == null ? (Has(a.id) ? 1 : 0) : Mathf.Min(a.goal, Meta.Count(a.counter));

        public static void Unlock(string id)
        {
            var a = Get(id);
            if (a == null || !Meta.AddAchievement(id)) return;
            Unlocked?.Invoke(a);
        }

        // Suma n al contador y desbloquea los logros que lo usen y hayan llegado a la meta.
        public static void Count(string counter, int n = 1)
        {
            int v = Meta.Add(counter, n);
            foreach (var a in All)
                if (a.counter == counter && v >= a.goal) Unlock(a.id);
        }

        // Logros por llegar a un mandato (se llama al empezar cada uno).
        public static void OnMandate(int mandate, float popularity, bool usedRepression)
        {
            Meta.Max("max_mandate", mandate);
            if (mandate >= 5) Unlock("segundo_mandato");
            if (mandate >= 10) Unlock("reeleccion");
            if (mandate >= 20) Unlock("eterno");
            if (mandate >= 10 && popularity >= 70) Unlock("tranqui");
            if (mandate >= 5 && !usedRepression) Unlock("sin_represion");
        }
    }
}
