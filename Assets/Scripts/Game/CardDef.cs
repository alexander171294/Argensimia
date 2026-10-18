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
        [Tooltip("Puntero: $ por segundo que genera durante la oleada")] public float moneyRate;
        [Tooltip("Comisario: daño extra de las defensas cercanas (0,3 = +30%)")] public float buff;
        [Tooltip("Contenedores: el encapuchado no puede saltarlo")] public bool noJump;

        // De un solo uso (no se suben de nivel): el infiltrado y el estruendo se consumen al actuar.
        public bool Upgradable => kind == CardKind.Colocar && defense != DefenseKind.Infiltrado && defense != DefenseKind.Estruendo && defense != DefenseKind.Desvio && defense != DefenseKind.Desafectar;
        // Las que no frenan ni reciben golpes no muestran VIDA.
        public bool HasHp => defense != DefenseKind.Desafectar && defense != DefenseKind.Concertina && defense != DefenseKind.Infiltrado && defense != DefenseKind.Estruendo && defense != DefenseKind.Desvio;
        public float LevelBuff(int level) => buff * (1 + 0.5f * (level - 1));

        // Nivel 2 = +40% al stat principal; nivel 3 = x2 y +30% de vida. Se sube soltando la carta sobre una igual del mapa.
        // Niveles de las colocables (mejora soltando una carta igual sobre la defensa del mapa).
        // Vida ×2 / ×3,5; daño ×1,75 / ×3; ritmo ×1,25 / ×1,6. NV3 además suma 1 casillero de alcance a los de distancia
        // y la infantería pega a 2 / 3 manifestantes por golpe.
        public static float LevelHp(int level) => level >= 3 ? 3.5f : level == 2 ? 2f : 1f;
        public static float LevelMul(int level) => level >= 3 ? 3f : level == 2 ? 1.75f : 1f; // daño / potencia
        public static float LevelRate(int level) => level >= 3 ? 1.6f : level == 2 ? 1.25f : 1f;
        public float LevelRange(int level) => level >= 3 && (defense == DefenseKind.BalasGoma || defense == DefenseKind.Lanzagases || defense == DefenseKind.Hidrante) ? 6.8f : 0f;

        public bool IsPolice => kind == CardKind.Colocar;
        public bool UsesSlot => kind == CardKind.Colocar && defense != DefenseKind.Desafectar;
        // Desafectar: se suelta sobre una defensa del mapa (cualquiera) en vez de sobre un slot.
        public bool TargetsDefense => kind == CardKind.Colocar && defense == DefenseKind.Desafectar;

        // Stats de las colocables para los badges de la carta. Vida = lo que aguanta la defensa antes de romperse
        // (los golpes de los manifestantes la bajan; en 0 se rompe y libera el slot).
        public List<(string label, string value)> StatList(int level = 1)
        {
            var l = new List<(string, string)>();
            if (kind != CardKind.Colocar) return l;
            float m = LevelMul(level);
            if (stats.hp > 0 && HasHp) l.Add(("VIDA", Names.Num(Mathf.Round(stats.hp * LevelHp(level)))));
            if (stats.damage > 0) l.Add(("DAÑO", Names.Num(Mathf.Round(stats.damage * m * 10) / 10)));
            float rate = stats.rate * (defense == DefenseKind.Detencion ? m : LevelRate(level));
            if (stats.rate > 0) l.Add(("RITMO", Names.Num(Mathf.Round(rate * 100) / 100) + "/s"));
            if (moneyRate > 0) l.Add(("PLATA", "+$" + Names.Num(Mathf.Round(moneyRate * LevelRate(level))) + "/s"));
            if (buff > 0) l.Add(("REFUERZO", "+" + Mathf.RoundToInt(LevelBuff(level) * 100) + "%"));
            if (stun > 0 && defense != DefenseKind.Motorizada) l.Add(("ATURDE", Names.Num(stun) + " s"));
            float range = stats.range + LevelRange(level);
            if (stats.range >= 40) l.Add(("ALCANCE", L.T("PLAZA")));
            else if (stats.range > 0) l.Add(("ALCANCE", range >= 6 ? Mathf.RoundToInt(range / 6.8f) + L.T(" cas.") : Names.Num(range) + " m"));
            return l;
        }

        // Resumen con rich text TMP, se muestra arriba de la descripción.
        public string EffectSummary(int level = 1)
        {
            var parts = new List<string>();
            if (kind == CardKind.Colocar) return ""; // los stats van como badges sobre el arte (StatList)
            if (moneyDelta != 0) parts.Add(Tag(moneyDelta > 0, (moneyDelta > 0 ? "+$" : "-$") + GameState.Money(moneyDelta)));
            if (incomeDelta != 0) parts.Add(Tag(incomeDelta > 0, (incomeDelta > 0 ? "+$" : "-$") + GameState.Money(incomeDelta) + "/s"));
            if (popularityDelta != 0) parts.Add(Tag(popularityDelta > 0, (popularityDelta > 0 ? "+" : "-") + Mathf.Abs(popularityDelta).ToString("0") + "%" + L.T(" popularidad")));
            if (popPerMandate != 0) parts.Add(Tag(popPerMandate > 0, (popPerMandate > 0 ? "+" : "-") + Mathf.Abs(popPerMandate).ToString("0") + L.T("%/mandato")));
            if (defenseHpBonus != 0) parts.Add(Tag(true, "+" + Mathf.RoundToInt(defenseHpBonus * 100) + L.T("% vida policía")));
            if (moneyMultiplier < 1) parts.Add(Tag(false, "-" + Mathf.RoundToInt((1 - moneyMultiplier) * 100) + L.T("% del dinero")));
            if (extraColumns > 0) parts.Add(Tag(false, "+" + extraColumns + L.T(" columna")));
            if (nextWaveReduction > 0) parts.Add(Tag(true, L.T("oleada -") + Mathf.RoundToInt(nextWaveReduction * 100) + "%"));
            if (salaryDelta != 0) parts.Add(Tag(salaryDelta > 0, (salaryDelta > 0 ? "+$" : "-$") + GameState.Money(salaryDelta) + L.T(" sueldo")));
            if (maxGovDelta > 0 && govDelta == maxGovDelta) parts.Add(Tag(true, "+" + govDelta + L.T(" gobernabilidad (y máx.)")));
            else
            {
                if (govDelta != 0) parts.Add(Tag(govDelta > 0, (govDelta > 0 ? "+" : "-") + Mathf.Abs(govDelta) + L.T(" gobernabilidad")));
                if (maxGovDelta != 0) parts.Add(Tag(maxGovDelta > 0, (maxGovDelta > 0 ? "+" : "-") + Mathf.Abs(maxGovDelta) + L.T(" gob. máx.")));
            }
            if (govPerMandate != 0) parts.Add(Tag(govPerMandate > 0, "+" + govPerMandate + L.T(" gob./mandato")));
            if (healNow > 0) parts.Add(Tag(true, "policía +" + Mathf.RoundToInt(healNow * 100) + "% vida"));
            if (healBonus > 0) parts.Add(Tag(true, "recupera " + Mathf.RoundToInt((GameState.BaseCloseHeal + healBonus) * 100) + "% al cierre"));
            return string.Join("   ", parts);
        }

        static string Stat(string k, string v) => "<color=#5a4632>" + k + "</color> <b>" + v + "</b>";
        static string Tag(bool good, string s) => "<color=" + (good ? "#1f7a2e" : "#b3261e") + "><b>" + s + "</b></color>";
    }
}
