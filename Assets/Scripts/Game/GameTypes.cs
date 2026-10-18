using UnityEngine;

namespace Argensimia
{
    public enum CardKind { Colocar = 0, Efecto = 1, Ministro = 2, Crisis = 3 }
    public enum Rarity { Comun, PocoComun, Rara }
    public enum DefenseKind { Vallado, Infanteria, Lanzagases, Detencion, Motorizada, Hidrante, BalasGoma,
        // desbloqueables (cada 3 mandatos sobrevividos, ver Meta)
        Puntero, Contenedores, Infiltrado, Comisario, Concertina, Requisa, Desvio, Estruendo, Drone, Tanqueta,
        // utilidad: retira una defensa del mapa y devuelve parte de la plata
        Desafectar }
    public enum UnitKind { Comun, Cacerolero, Jubilado, Encapuchado, Piquetero, Mortero, Columna }
    public enum Phase { Previa, Oleada, Cierre, Victoria, Derrota }

    // Stats compartidas por policía y manifestantes.
    [System.Serializable]
    public struct UnitStats
    {
        [Tooltip("Vida")] public float hp;
        [Tooltip("Daño por golpe (o por segundo en efectos continuos)")] public float damage;
        [Tooltip("Golpes por segundo")] public float rate;
        [Tooltip("Alcance (m)")] public float range;
        [Tooltip("Radio del área (m), 0 = un solo objetivo")] public float area;
        [Tooltip("Velocidad (m/s)")] public float speed;
    }

    public static class Names
    {
        public static string Rarity(Rarity r) => r == Argensimia.Rarity.Comun ? "COMÚN" : r == Argensimia.Rarity.PocoComun ? "POCO COMÚN" : "RARA";
        public static Color RarityColor(Rarity r) => r == Argensimia.Rarity.Comun ? new Color(0.78f, 0.8f, 0.84f) : r == Argensimia.Rarity.PocoComun ? new Color(0.36f, 0.78f, 1f) : new Color(1f, 0.72f, 0.2f);
        public static string Kind(CardKind k) => k == CardKind.Colocar ? "COLOCAR" : k == CardKind.Efecto ? "EFECTO" : k == CardKind.Ministro ? "MINISTRO" : "CRISIS";
        public static string Num(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
