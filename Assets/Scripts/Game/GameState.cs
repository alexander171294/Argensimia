using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Argensimia
{
    // Estado global de la partida: dinero, popularidad (Bronca), gobernabilidad, gabinete y fase del mandato.
    public class GameState : MonoBehaviour
    {
        public static GameState I { get; private set; }

        [Header("Dinero")]
        public float money = 2500;
        [Tooltip("Goteo $ por segundo, solo durante la oleada")] public float income = 40;
        public int salaryBase = 2000;
        public int salaryPerPop = 40;

        [Header("Popularidad (0-100). Baja = más gente y más picante")]
        [Range(0, 100)] public float popularity = 78; // arranca alta: hay aire para medidas impopulares

        [Header("Gobernabilidad (se pierde si llega a 0)")]
        public int maxGovernability = 20;
        public int governability = 20;

        [Header("Mandatos (= oleadas)")]
        public int mandate = 1;
        [Tooltip("0 = sin límite (por ahora el juego es infinito)")] public int totalMandates = 0;
        public bool endless;
        public Phase phase = Phase.Previa;

        [Header("Puntaje")]
        [Tooltip("Puntos por cada punto de costo de una unidad sacada de juego (manifestante 1, columna 6)")] public int pointsPerCost = 25;
        [Tooltip("Pesos por punto del dinero que quedó")] public int moneyPerPoint = 10;
        public int kills;
        public float killPoints;

        public void AddKill(float cost) { kills++; killPoints += pointsPerCost * Mathf.Max(1, cost); }
        public int MandatesSurvived => Mathf.Max(0, mandate - 1); // el mandato en el que caíste no cuenta
        // (eliminados + dinero/10) × mandatos sobrevividos (mín. 1) × popularidad (mín. 10%)
        public int Score => Mathf.RoundToInt((killPoints + money / moneyPerPoint) * Mathf.Max(1, MandatesSurvived) * Mathf.Max(0.1f, popularity / 100f));

        [Header("Gabinete")]
        public int cabinetSize = 3;
        public readonly List<CardDef> cabinet = new List<CardDef>();

        public event Action<string> Headline;
        public event Action<int> MandateChanged;
        public event Action<Phase> PhaseChanged;

        public static readonly string[] MoodNames = { "TRANQUI", "TENSO", "CALLE CALIENTE", "ESTALLIDO" };
        // Bronca: multiplicadores de stats enemigas por estado de ánimo
        public static readonly float[] HpMul = { 1f, 1.15f, 1.35f, 1.6f };
        public static readonly float[] DmgMul = { 1f, 1.1f, 1.25f, 1.5f };
        public static readonly float[] SpdMul = { 1f, 1.05f, 1.1f, 1.2f };

        public float MandateProgress => WaveDirector.I ? WaveDirector.I.Progress : 0;
        // Multiplicador de cantidad para las oleadas: 0,6 con 100% de popularidad, 2 con 0%.
        public float Difficulty => Mathf.Lerp(0.6f, 2f, 1f - popularity / 100f);
        public int MoodIndex => popularity >= 70 ? 0 : popularity >= 50 ? 1 : popularity >= 30 ? 2 : 3;
        public string Mood => L.T(MoodNames[MoodIndex]);
        public int Salary
        {
            get { int s = salaryBase + Mathf.RoundToInt(salaryPerPop * popularity); foreach (var c in cabinet) s += c.salaryDelta; return s; }
        }
        // Al cerrar cada mandato la policía recupera hasta este % de su vida máxima (Min. Seguridad lo sube).
        public const float BaseCloseHeal = 0.25f;
        public float CloseHeal
        {
            get { float h = BaseCloseHeal; foreach (var c in cabinet) h += c.healBonus; return h; }
        }
        public float DefenseHpMul
        {
            get { float m = 1; foreach (var c in cabinet) m += c.defenseHpBonus; return m; }
        }

        void Awake() { I = this; governability = maxGovernability; }

        void Update()
        {
            if (phase == Phase.Oleada) money += income * Time.deltaTime;
        }

        public void SetPhase(Phase p)
        {
            phase = p;
            PhaseChanged?.Invoke(p);
        }

        public void NextMandate()
        {
            mandate++;
            MandateChanged?.Invoke(mandate);
        }

        public bool CanAfford(int cost) => money + 0.001f >= cost;

        public bool Spend(int cost)
        {
            if (!CanAfford(cost)) return false;
            money -= cost;
            return true;
        }

        public void ApplyEffect(CardDef c)
        {
            int before = MoodIndex;
            if (c.moneyMultiplier > 0 && c.moneyMultiplier < 1) { float before0 = money; money *= c.moneyMultiplier; Lost?.Invoke("money", before0 - money, null); }
            money = Mathf.Max(0, money + c.moneyDelta);
            income = Mathf.Max(0, income + c.incomeDelta);
            AddPopularity(c.popularityDelta, false);
            if (c.maxGovDelta != 0) { maxGovernability = Mathf.Max(1, maxGovernability + c.maxGovDelta); governability = Mathf.Min(governability, maxGovernability); }
            if (c.govDelta > 0) governability = Mathf.Min(maxGovernability, governability + c.govDelta);
            else if (c.govDelta < 0) { int lost = Mathf.Min(-c.govDelta, governability - 1); governability -= lost; if (lost > 0) Lost?.Invoke("gov", lost, null); } // una carta nunca te hace perder
            if (c.healNow > 0) Defense.HealAll(c.healNow);
            if (WaveDirector.I)
            {
                WaveDirector.I.extraColumns += c.extraColumns;
                WaveDirector.I.reduction = Mathf.Max(WaveDirector.I.reduction, c.nextWaveReduction);
            }
            if (!string.IsNullOrEmpty(c.headline)) Headline?.Invoke(Headlines.PickVariant(L.CardHeadline(c)));
            MoodHeadline(before);
        }

        // Pérdidas que el HUD muestra como números rojos flotantes (kind: pop, gov, money). at = posición en el mundo (opcional).
        public event System.Action<string, float, Vector3?> Lost;

        public void LosePopularity(float amount, Vector3? at = null)
        {
            if (amount <= 0) return;
            AddPopularity(-amount);
            Lost?.Invoke("pop", amount, at);
        }

        public void AddPopularity(float d, bool headline = true)
        {
            int before = MoodIndex;
            popularity = Mathf.Clamp(popularity + d, 0, 100);
            if (headline) MoodHeadline(before);
        }

        void MoodHeadline(int before)
        {
            if (MoodIndex != before)
            {
                Headline?.Invoke(Headlines.Pick(MoodIndex > before ? "mood_up" : "mood_down"));
                Snd.Ui(MoodIndex > before ? "mood_down" : "pop_up", 1, 1, 0.5f); // índice más alto = más bronca
            }
        }

        // Ministro nuevo: si el gabinete está lleno, reemplaza al más viejo.
        public CardDef AddMinister(CardDef c)
        {
            CardDef removed = null;
            if (cabinet.Count >= cabinetSize) { removed = cabinet[0]; cabinet.RemoveAt(0); income = Mathf.Max(0, income - removed.incomeDelta); if (removed.defenseHpBonus > 0) foreach (var d in Defense.All) d.RefreshHp(); }
            cabinet.Add(c);
            money = Mathf.Max(0, money + c.moneyDelta);
            income = Mathf.Max(0, income + c.incomeDelta);
            AddPopularity(c.popularityDelta);
            if (!string.IsNullOrEmpty(c.headline)) Headline?.Invoke(Headlines.PickVariant(L.CardHeadline(c)));
            if (c.defenseHpBonus > 0) foreach (var d in Defense.All) d.RefreshHp();
            return removed;
        }

        // Cierre de mandato: sueldo + efecto de los ministros + la policía recupera hasta CloseHeal de su vida.
        public int LastHealed { get; private set; }
        public int CloseMandate()
        {
            int s = Salary;
            money += s;
            foreach (var c in cabinet)
            {
                if (c.popPerMandate < 0) LosePopularity(-c.popPerMandate); else AddPopularity(c.popPerMandate);
                if (c.govPerMandate > 0) governability = Mathf.Min(maxGovernability, governability + c.govPerMandate);
            }
            LastHealed = Defense.HealAll(CloseHeal);
            return s;
        }

        public void LoseGovernability(int n, Vector3? at = null)
        {
            if (phase != Phase.Oleada) return;
            governability = Mathf.Max(0, governability - n);
            Lost?.Invoke("gov", n, at);
            if (governability <= 0)
            {
                Headline?.Invoke(Headlines.Pick("gov_lost"));
                if (WaveDirector.I) WaveDirector.I.Lose();
            }
        }

        public void PushHeadline(string h) => Headline?.Invoke(h);

        // 12450 -> "12.450"
        public static string Money(float v) => Mathf.FloorToInt(Mathf.Abs(v)).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');
    }
}
