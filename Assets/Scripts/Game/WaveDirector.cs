using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Argensimia
{
    // Mandatos (= oleadas): previa de planificación sin timer → oleada → cierre (sueldo + 1 de 3).
    // La oleada se arma gastando un presupuesto = base del mandato × Difficulty (popularidad); la Bronca
    // (estado de ánimo) multiplica las stats al spawnear.
    public class WaveDirector : MonoBehaviour
    {
        public static WaveDirector I { get; private set; }

        public int[] baseBudget = { 20, 32, 48, 68, 100 };
        [Tooltip("Endless: crecimiento del presupuesto y de la vida por mandato")] public float endlessGrowth = 1.18f, endlessHp = 1.08f; // el daño además crece por mandato (DmgPerMandate)
        [Tooltip("Multiplicador global de velocidad de los manifestantes")] public float speedScale = 1.3f;
        public float spawnInterval = 0.75f;
        [Tooltip("Pausa entre pulsos con ESTALLIDO")] public float pulseGap = 9f;

        [HideInInspector] public int extraColumns;
        [HideInInspector] public float reduction;

        public struct Entry { public UnitDef unit; public int side; } // side: 0 = este (Av. de Mayo), 1 = norte, 2 = sur

        public readonly List<Entry> plan = new List<Entry>();
        readonly List<UnitDef> units = new List<UnitDef>();
        readonly Queue<(Entry e, float delay)> queue = new Queue<(Entry, float)>();
        float spawnT, lastPlanPop = -1;
        int total, gone, arrived;
        public int ArrivedThisWave => arrived;
        public int PlannedCount => plan.Count;
        public float Progress => total <= 0 || !GameState.I || GameState.I.phase != Phase.Oleada ? 0 : (float)gone / total;
        readonly List<GameObject> previews = new List<GameObject>();

        GameState gs => GameState.I;

        void Awake()
        {
            I = this;
            units.AddRange(Resources.LoadAll<UnitDef>("Units"));
            units.Sort((a, b) => a.cost.CompareTo(b.cost));
        }

        void Start()
        {
            if (!GetComponent<LaneArrows>()) gameObject.AddComponent<LaneArrows>();
            gs.SetPhase(Phase.Previa);
            Plan();
        }

        void Update()
        {
            if (gs.phase == Phase.Previa)
            {
                // la oleada se recalcula en vivo con la popularidad hasta tocar ARRANCAR
                if (Mathf.Abs(gs.popularity - lastPlanPop) > 0.01f) Plan();
                return;
            }
            if (gs.phase != Phase.Oleada) return;
            spawnT -= Time.deltaTime;
            while (queue.Count > 0 && spawnT <= 0)
            {
                var (e, delay) = queue.Dequeue();
                SpawnOne(e);
                spawnT += delay;
            }
            if (queue.Count == 0 && Enemy.All.Count == 0 && gone >= total) EndWave();
        }

        // ------------------------------------------------------------------ armado de la oleada

        public const float DmgPerMandate = 0.08f;

        public float Budget(int m)
        {
            if (m <= baseBudget.Length) return baseBudget[Mathf.Max(0, m - 1)];
            return baseBudget[baseBudget.Length - 1] * Mathf.Pow(endlessGrowth, m - baseBudget.Length);
        }

        public bool IsFinal => gs.totalMandates > 0 && !gs.endless && gs.mandate == gs.totalMandates;

        // Carriles activos: mandato 1 = uno, mandato 2 = dos, desde el 3 los cuatro (da tiempo a armar la defensa).
        public static readonly int[] LaneOrder = { 1, 3, 0, 2 }; // interiores primero (norte, sur), después exteriores
        public int ActiveLaneCount => Mathf.Clamp(gs.mandate, 1, 4); // un carril más por mandato: 1, 2, 3 y desde el 4 los cuatro
        public bool LaneActive(int l) => System.Array.IndexOf(LaneOrder, l) < ActiveLaneCount;

        public void Plan()
        {
            lastPlanPop = gs.popularity;
            var rnd = new System.Random(gs.mandate * 7919 + 17); // misma oleada para la misma popularidad
            plan.Clear();
            int m = gs.mandate;
            float budget = Budget(m) * gs.Difficulty * (1 - reduction);
            if (Tutorial.Active)
            {
                foreach (var (id, n) in Tutorial.Wave(m))
                {
                    var u = units.Find(x => x.id == id);
                    int k = Mathf.Max(1, Mathf.RoundToInt(n * gs.Difficulty / Tutorial.BaseDifficulty)); // también crece si baja la popularidad
                    for (int i = 0; u && i < k; i++) plan.Add(new Entry { unit = u });
                }
                budget = 0;
            }
            var avail = Tutorial.Active ? new List<UnitDef>() : units.FindAll(u => u.minMandate <= m);
            UnitDef col = units.Find(u => u.kind == UnitKind.Columna);
            int cols = extraColumns + (IsFinal ? 3 : 0);
            for (int i = 0; i < cols && col; i++) { plan.Add(new Entry { unit = col }); budget -= col.cost; }
            if (gs.MoodIndex >= 2 && !Tutorial.Active)
            {
                var specials = avail.FindAll(u => u.special && u.kind != UnitKind.Columna);
                if (specials.Count > 0) { var s = specials[rnd.Next(specials.Count)]; plan.Add(new Entry { unit = s }); budget -= s.cost; }
            }
            int guard = 0;
            while (guard++ < 500)
            {
                var afford = avail.FindAll(u => u.cost <= budget);
                if (afford.Count == 0) break;
                float tw = 0;
                foreach (var u in afford) tw += u.weight;
                float r = (float)rnd.NextDouble() * tw;
                var pick = afford[afford.Count - 1];
                foreach (var u in afford) { r -= u.weight; if (r <= 0) { pick = u; break; } }
                plan.Add(new Entry { unit = pick });
                budget -= pick.cost;
            }
            // por dónde entran: mandatos 1-4 solo Av. de Mayo; desde el 5 se abren norte y sur
            for (int i = 0; i < plan.Count; i++)
            {
                var e = plan[i];
                double roll = rnd.NextDouble();
                float sideChance = m < 5 ? 0 : 0.5f;
                e.side = roll < sideChance ? (rnd.NextDouble() < 0.5 ? 1 : 2) : 0;
                plan[i] = e;
            }
            // orden: especiales más hacia el final
            var ordered = new List<(Entry e, double k)>();
            foreach (var e in plan) ordered.Add((e, rnd.NextDouble() + (e.unit.special ? 0.45 : 0)));
            ordered.Sort((a, b) => a.k.CompareTo(b.k));
            plan.Clear();
            foreach (var o in ordered) plan.Add(o.e);
            RefreshPreviews();
        }

        public string PreviewText()
        {
            var counts = new Dictionary<UnitDef, int>();
            foreach (var e in plan) { counts.TryGetValue(e.unit, out int n); counts[e.unit] = n + 1; }
            return CountText(counts, plan.Exists(e => e.side != 0));
        }

        // Durante la oleada: los que faltan entrar más los que siguen en la Plaza.
        public string RemainingText()
        {
            var counts = new Dictionary<UnitDef, int>();
            foreach (var q in queue) { counts.TryGetValue(q.e.unit, out int n); counts[q.e.unit] = n + 1; }
            foreach (var en in Enemy.All) if (en && !en.Dead && en.def) { counts.TryGetValue(en.def, out int n); counts[en.def] = n + 1; }
            return counts.Count == 0 ? "—" : CountText(counts, false);
        }

        public int RemainingCount { get { int n = queue.Count; foreach (var en in Enemy.All) if (en && !en.Dead) n++; return n; } }

        string CountText(Dictionary<UnitDef, int> counts, bool sides)
        {
            var keys = new List<UnitDef>(counts.Keys);
            keys.Sort((a, b) => a.cost.CompareTo(b.cost));
            var sb = new StringBuilder();
            foreach (var u in keys)
            {
                if (sb.Length > 0) sb.Append('\n');
                string name = L.UnitName(u, counts[u] != 1, Plural);
                sb.Append(u.special ? "<color=#ff8a70>" : "").Append(counts[u]).Append(' ').Append(name).Append(u.special ? "</color>" : "");
            }
            if (sides) sb.Append("\n<color=#f2c14e>").Append(L.T("También por norte y sur")).Append("</color>");
            return sb.ToString();
        }

        static string Plural(string s)
        {
            if (s.Contains(" ")) { var w = s.Split(' '); w[0] = Plural(w[0]); for (int i = 1; i < w.Length; i++) w[i] = Plural(w[i]); return string.Join(" ", w); }
            if (s.EndsWith("z")) return s.Substring(0, s.Length - 1) + "ces";
            if ("aeiouáéó".IndexOf(s[s.Length - 1]) >= 0) return s + "s";
            return s + "es";
        }

        // Especiales a la vista en la esquina por donde van a entrar (telegrafía).
        void RefreshPreviews()
        {
            foreach (var p in previews) if (p) Destroy(p);
            previews.Clear();
            var shown = new HashSet<UnitDef>();
            int k = 0;
            foreach (var e in plan)
            {
                if (!e.unit.special || shown.Contains(e.unit)) continue;
                shown.Add(e.unit);
                var pos = e.side == 0 ? SpawnPoint(0, LaneOrder[k++ % ActiveLaneCount]) : SpawnPoint(e.side, 0);
                if (e.side == 0) pos.x -= 1.5f;
                var en = Enemy.Spawn(e.unit, new List<Vector3> { pos }, 0, 1, 1, 0);
                Enemy.All.Remove(en);
                en.enabled = false;
                en.transform.rotation = Quaternion.Euler(0, e.side == 0 ? -90 : e.side == 1 ? 180 : 0, 0);
                en.name = "Preview_" + e.unit.id;
                previews.Add(en.gameObject);
            }
        }

        // ------------------------------------------------------------------ fases

        public void StartWave()
        {
            if (gs.phase != Phase.Previa) return;
            Plan();
            foreach (var p in previews) if (p) Destroy(p);
            previews.Clear();
            queue.Clear();
            int pulses = gs.MoodIndex >= 3 ? 3 : 1;
            int per = Mathf.CeilToInt(plan.Count / (float)pulses);
            for (int i = 0; i < plan.Count; i++)
            {
                bool endPulse = pulses > 1 && (i + 1) % per == 0 && i < plan.Count - 1;
                queue.Enqueue((plan[i], endPulse ? pulseGap : spawnInterval * Random.Range(0.6f, 1.4f)));
            }
            total = plan.Count; gone = 0; arrived = 0;
            spawnT = 1f;
            extraColumns = 0;
            reduction = 0;
            gs.SetPhase(Phase.Oleada);
            Snd.Ui("wave_start");
            // 0f explícito: con un int como 2º argumento se elegía la sobrecarga (key, cooldown) y el {0} del titular tiraba excepción
            if (IsFinal) Headlines.Say("wave_final"); else Headlines.Say("wave_start", 0f, gs.mandate);
        }

        public void OnEnemyGone(Enemy e, bool arr)
        {
            gone++;
            if (arr) arrived++;
        }

        void EndWave()
        {
            if (gs.phase != Phase.Oleada) return;
            if (IsFinal) { gs.SetPhase(Phase.Victoria); Headlines.Say("victory"); return; }
            gs.SetPhase(Phase.Cierre);
        }

        // Lo llama el HUD tras elegir la carta del cierre.
        public void NextMandate()
        {
            gs.NextMandate();
            gs.SetPhase(Phase.Previa);
            Plan();
        }

        public void StartEndless()
        {
            gs.endless = true;
            NextMandate();
        }

        public void Lose()
        {
            queue.Clear();
            if (gs.phase != Phase.Derrota) Snd.Ui("gameover");
            gs.SetPhase(Phase.Derrota);
        }

        // ------------------------------------------------------------------ spawn

        // Todo aparece sobre las líneas de los carriles donde tocan la calle (borde de la plaza):
        // extremo este de cada carril (Av. de Mayo) y, desde el mandato 3, arriba del refuerzo norte/sur (x = 0).
        Vector3 SpawnPoint(int side, int lane)
        {
            var slots = PoliceSlots.I;
            if (side == 0) return slots.LanePoint(lane, slots.EastX(lane) - 0.5f);
            float sz = side == 1 ? 1 : -1;
            return new Vector3(Random.Range(-1f, 1f), 0.16f, sz * 25.5f);
        }

        void SpawnOne(Entry e)
        {
            var slots = PoliceSlots.I;
            int m = gs.MoodIndex;
            float endlessK = gs.mandate > baseBudget.Length ? Mathf.Pow(endlessHp, gs.mandate - baseBudget.Length) : 1;
            // Curtido por mandato: el daño sube +8% lineal por mandato (×1,72 en el 10, ×2,5 en el 20) para que las defensas
            // de siempre también caigan; velocidad +1% (tope +20%). La vida sigue la curva de siempre (×1,08 desde el 6),
            // columnas incluidas, sin nada extra.
            float mk = gs.mandate - 1;
            float dmgK = 1 + DmgPerMandate * mk, spdK = Mathf.Min(1.2f, 1 + 0.01f * mk);
            float hpK = endlessK;
            var path = new List<Vector3>();
            int lane;
            if (e.side == 0)
            {
                lane = LaneOrder[Random.Range(0, ActiveLaneCount)];
                path.Add(SpawnPoint(0, lane));
            }
            else
            {
                // bajan por el refuerzo (x = 0) y toman un carril de su lado
                float sz = e.side == 1 ? 1 : -1;
                var sideLanes = new List<int>();
                for (int l = 0; l < slots.LaneCount; l++) if (Mathf.Sign(slots.LaneZ(l, 0)) == sz && LaneActive(l)) sideLanes.Add(l);
                if (sideLanes.Count == 0) { e.side = 0; SpawnOne(e); return; }
                lane = sideLanes[Random.Range(0, sideLanes.Count)];
                path.Add(SpawnPoint(e.side, lane));
                path.Add(slots.LanePoint(lane, Random.Range(-0.5f, 0.5f)));
            }
            var en = Enemy.Spawn(e.unit, path, lane, GameState.HpMul[m] * hpK, GameState.DmgMul[m] * dmgK, GameState.SpdMul[m] * spdK);
            en.PopIn();
        }
    }
}
