using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Partida tutorial: mazo y oleadas fijas, sin reloj en la previa, sin progreso (logros, Meta, ranking)
    // y sin derrota. La guía en pantalla (Don Anselmo, el presidente saliente) está en GameHudTutorial.
    // El estado es estático porque sobrevive a la recarga de la escena (GameFlow.StartTutorial).
    public static class Tutorial
    {
        public static bool Active;
        public const string DoneKey = "tutorial_done";
        public static bool Done => PlayerPrefs.GetInt(DoneKey, 0) == 1;
        public static void MarkDone() { PlayerPrefs.SetInt(DoneKey, 1); PlayerPrefs.Save(); }

        // Orden de robo (la primera sale primero): la mano inicial es Vallado ×2, Infantería, Impuestazo y Cadena nacional.
        public static readonly string[] DeckOrder =
        {
            "vallado", "vallado", "infanteria", "impuestazo", "cadena_nacional",
            "balas_goma", "maquinita", "vallado", "lanzagases", "infanteria",
            "plan_platita", "cadena_nacional", "detencion", "blanqueo", "balas_goma",
        };

        // Crisis forzada del mandato 2.
        public const string Crisis = "escandalo";
        // El tutorial termina al cerrar este mandato.
        public const int LastMandate = 2;

        // Dificultad con la popularidad inicial (78%): las cantidades de abajo valen para ese valor y se escalan
        // con GameState.Difficulty, así la oleada crece en vivo si la popularidad baja (lo muestra el Impuestazo).
        public const float BaseDifficulty = 0.908f;

        // Oleadas fijas: (unidad, cantidad con 78% de popularidad).
        public static (string id, int n)[] Wave(int mandate) => mandate <= 1
            ? new[] { ("comun", 5), ("cacerolero", 2) }
            : new[] { ("comun", 7), ("jubilado", 2), ("cacerolero", 1), ("encapuchado", 1) };

        public static List<CardDef> Deck(List<CardDef> catalog)
        {
            var res = new List<CardDef>();
            foreach (var id in DeckOrder)
            {
                var c = catalog.Find(d => d && d.id == id);
                if (c) res.Add(c);
            }
            return res;
        }
    }
}
