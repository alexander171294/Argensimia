using UnityEngine;

namespace Argensimia
{
    // Manifestante. Mismas stats que las cartas policiales; la Bronca (popularidad) las multiplica al spawnear.
    [CreateAssetMenu(menuName = "Argensimia/Unidad", fileName = "Unit")]
    public class UnitDef : ScriptableObject
    {
        public string id;
        public string title;
        public UnitKind kind;
        [Tooltip("Puntos del presupuesto de oleada")] public int cost = 1;
        public UnitStats stats;
        [Tooltip("Gobernabilidad que resta al tocar el vallado")] public int govDamage = 1;
        [Tooltip("Mandato desde el que aparece")] public int minMandate = 1;
        [Tooltip("Peso al armar la oleada")] public float weight = 1;
        public bool special;

        [Header("Rasgos")]
        [Tooltip("Popularidad que se pierde cuando la policía lo daña")] public float popOnHit;
        [Tooltip("Popularidad que se pierde si llega al vallado")] public float popOnArrive;
        [Tooltip("Multiplicador de daño contra vallas")] public float fenceMultiplier = 1;
        [Tooltip("Resistencia al empuje (0-1)")] public float pushResist;
        [Tooltip("Aura: +velocidad a los cercanos (0,2 = +20%)")] public float auraSpeed;
        public float auraRadius = 4;
        [Tooltip("Segundos entre gomas quemadas (0 = no tira)")] public float tireEvery;
        [Tooltip("Chance (0-1) de saltar cada retén policial que lo frena. Solo el encapuchado")] [Range(0, 1)] public float jumpChance;

        [Header("Visual")]
        [Tooltip("Índice en GameAssets.shirts (-1 = al azar)")] public int shirt = -1;
        [Tooltip("Índice en GameAssets.hairs (-1 = al azar)")] public int hair = -1;
        public float scale = 1;
        [Tooltip("Personas que forman la unidad (columna organizada)")] public int members = 1;
        [Tooltip("Accesorio: pot, mortar, flag, banner, hood, none")] public string prop = "none";
    }
}
