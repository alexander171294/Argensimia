using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Definición de una carta. Los assets viven en Assets/Resources/Cards/ (los crea "Argensimia/4. Create UI Assets").
    [CreateAssetMenu(menuName = "Argensimia/Carta", fileName = "Card")]
    public class CardDef : ScriptableObject
    {
        public string id;
        public string title;
        public CardKind kind;
        [Min(0)] public int cost;
        [TextArea(2, 6)] public string description;
        [Tooltip("Arte de la carta (opcional). Sin arte se dibuja un placeholder.")] public Sprite art;
        public Color color = new Color(0.2f, 0.4f, 0.8f);
        [Tooltip("Copias en el mazo inicial. 0 = solo sale en el 1 de 3")] [Min(0)] public int copies = 2;
        [Tooltip("Titular que entra a la marquesina al activarla (opcional)")] public string headline;

        [Header("Roguelike")]
        public Rarity rarity;
        [Tooltip("Puede salir en el 1 de 3 al cerrar un mandato")] public bool inPool = true;

        [Header("Efecto / Ministro / Crisis")]
        [Tooltip("Puntos de popularidad (%). Negativo = columnas más enojadas")] public float popularityDelta;
        public int moneyDelta;
        [Tooltip("$ por segundo")] public float incomeDelta;
        [Tooltip("Ministro: popularidad que suma o resta al cerrar cada mandato")] public float popPerMandate;
        [Tooltip("Ministro: bonus de vida de las defensas (0,25 = +25%)")] public float defenseHpBonus;
        [Tooltip("Crisis: multiplica el dinero actual (0,7 = devaluación del 30%)")] public float moneyMultiplier = 1;
        [Tooltip("Crisis: columnas organizadas extra en la próxima oleada")] public int extraColumns;
        [Tooltip("Efecto: la próxima oleada viene más chica (0,2 = -20%)")] public float nextWaveReduction;
        [Tooltip("Solo se puede jugar en la previa")] public bool previaOnly;
        [Tooltip("Ministro: $ extra en el sueldo de cada cierre")] public int salaryDelta;
        [Tooltip("Gobernabilidad que suma (o resta) al jugarla")] public int govDelta;
        [Tooltip("Gobernabilidad máxima extra")] public int maxGovDelta;
        [Tooltip("Ministro: gobernabilidad que recupera al cerrar cada mandato")] public int govPerMandate;
        [Tooltip("Efecto: la policía recupera ya esta fracción de su vida (0,4 = 40%)")] public float healNow;
        [Tooltip("Ministro: suma a la recuperación de la policía al cerrar el mandato (0,05 = +5%)")] public float healBonus;

        [Header("Policía (kind = Colocar)")]
        public DefenseKind defense;
        public UnitStats stats;
        [Tooltip("Máximo de manifestantes que frena a la vez (0 = todos)")] public int maxBlock;
        [Tooltip("Ralentiza (0-1)")] public float slow;
        public float slowTime;
        [Tooltip("Aturde (s)")] public float stun;
        [Tooltip("Empuje: metros (golpe) o m/s (chorro)")] public float push;
        [Tooltip("Hidrante: segundos de chorro / de recarga")] public float cycleOn, cycleOff;
        [Tooltip("Prefab a instanciar en el mapa. Vacío = se arma con los modelos de policía")] public GameObject placePrefab;
        [Tooltip("Slots que ocupa en el camino")] [Min(1)] public int slotSize = 1;

        // Nivel 2 = +40% al stat principal; nivel 3 = x2 y +30% de vida. Se sube soltando la carta sobre una igual del mapa.
        public static float LevelMul(int level) => level >= 3 ? 2f : level == 2 ? 1.4f : 1f;
        public static float LevelHp(int level) => level >= 3 ? 1.3f : 1f;

        public bool IsPolice => kind == CardKind.Colocar;
        public bool UsesSlot => kind == CardKind.Colocar;

        // Resumen con rich text TMP, se muestra arriba de la descripción.
        public string EffectSummary(int level = 1)
        {
            var parts = new List<string>();
            if (kind == CardKind.Colocar)
            {
                float m = LevelMul(level);
                if (stats.hp > 0) parts.Add(Stat("Gobernabilidad", Names.Num(stats.hp * LevelHp(level) * (defense == DefenseKind.Vallado || defense == DefenseKind.Infanteria ? m : 1))));
                if (stats.damage > 0) parts.Add(Stat("Daño", Names.Num(stats.damage * (defense == DefenseKind.Vallado || defense == DefenseKind.Infanteria ? 1 : m))));
                if (stats.rate > 0) parts.Add(Stat("Ritmo", Names.Num(stats.rate) + "/s"));
                if (stats.range > 0) parts.Add(Stat("Alcance", stats.range >= 6 ? Mathf.RoundToInt(stats.range / 6.8f) + " casilleros" : Names.Num(stats.range) + " m"));
                return string.Join("  ", parts);
            }
            if (moneyDelta != 0) parts.Add(Tag(moneyDelta > 0, (moneyDelta > 0 ? "+$" : "-$") + GameState.Money(moneyDelta)));
            if (incomeDelta != 0) parts.Add(Tag(incomeDelta > 0, (incomeDelta > 0 ? "+$" : "-$") + GameState.Money(incomeDelta) + "/s"));
            if (popularityDelta != 0) parts.Add(Tag(popularityDelta > 0, (popularityDelta > 0 ? "+" : "-") + Mathf.Abs(popularityDelta).ToString("0") + "% popularidad"));
            if (popPerMandate != 0) parts.Add(Tag(popPerMandate > 0, (popPerMandate > 0 ? "+" : "-") + Mathf.Abs(popPerMandate).ToString("0") + "%/mandato"));
            if (defenseHpBonus != 0) parts.Add(Tag(true, "+" + Mathf.RoundToInt(defenseHpBonus * 100) + "% gobernabilidad policía"));
            if (moneyMultiplier < 1) parts.Add(Tag(false, "-" + Mathf.RoundToInt((1 - moneyMultiplier) * 100) + "% del dinero"));
            if (extraColumns > 0) parts.Add(Tag(false, "+" + extraColumns + " columna"));
            if (nextWaveReduction > 0) parts.Add(Tag(true, "oleada -" + Mathf.RoundToInt(nextWaveReduction * 100) + "%"));
            if (salaryDelta != 0) parts.Add(Tag(salaryDelta > 0, (salaryDelta > 0 ? "+$" : "-$") + GameState.Money(salaryDelta) + " sueldo"));
            if (maxGovDelta > 0 && govDelta == maxGovDelta) parts.Add(Tag(true, "+" + govDelta + " gobernabilidad (y máx.)"));
            else
            {
                if (govDelta != 0) parts.Add(Tag(govDelta > 0, (govDelta > 0 ? "+" : "-") + Mathf.Abs(govDelta) + " gobernabilidad"));
                if (maxGovDelta != 0) parts.Add(Tag(maxGovDelta > 0, (maxGovDelta > 0 ? "+" : "-") + Mathf.Abs(maxGovDelta) + " gob. máx."));
            }
            if (govPerMandate != 0) parts.Add(Tag(govPerMandate > 0, "+" + govPerMandate + " gob./mandato"));
            if (healNow > 0) parts.Add(Tag(true, "policía +" + Mathf.RoundToInt(healNow * 100) + "% vida"));
            if (healBonus > 0) parts.Add(Tag(true, "recupera " + Mathf.RoundToInt((GameState.BaseCloseHeal + healBonus) * 100) + "% al cierre"));
            return string.Join("   ", parts);
        }

        static string Stat(string k, string v) => "<color=#5a4632>" + k + "</color> <b>" + v + "</b>";
        static string Tag(bool good, string s) => "<color=" + (good ? "#1f7a2e" : "#b3261e") + "><b>" + s + "</b></color>";
    }
}
