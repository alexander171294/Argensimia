using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Elemento policial colocado en un slot del carril. Todos frenan a los manifestantes de su carril
    // (la infantería, hasta maxBlock a la vez) y se rompen al quedar sin vida, liberando el slot.
    public class Defense : MonoBehaviour
    {
        public static readonly List<Defense> All = new List<Defense>();

        public CardDef def;
        public int level = 1;
        public int lane;
        public float x;
        public float HalfLen { get; private set; } = 2.7f;
        public float hp, maxHp;
        public bool Dead { get; private set; }
        public float EastFace => x + HalfLen;
        public float WestFace => x - HalfLen;

        readonly List<Enemy> blocking = new List<Enemy>();
        float cd, cycleT, punch, baseHp;
        bool spraying;
        ParticleSystem jet;
        // motorizada: espera en su slot (bloquea y puede recibir daño) y cada cierto tiempo sale a los porrazos hacia Av. de Mayo
        Transform moto;
        HpBar cdBar;
        float motoWait, motoAppear = 1;
        public bool Targetable => true;

        void BuildMotos()
        {
            moto = new GameObject("Motos").transform;
            moto.SetParent(model, false);
            for (int i = 0, k = level >= 2 ? 3 : 2; i < k; i++) MotoRun.Moto(moto, new Vector3(-(i % 2) * 2.6f + 0.6f, 0, (i - (k - 1) * 0.5f) * 1.6f));
        }
        HpBar bar;
        Transform model;

        float Mul => CardDef.LevelMul(level);

        public static Defense Place(CardDef def, int level, PoliceSlots.Slot s, PoliceSlots.Slot s2)
        {
            Vector3 c = s2 != null ? (s.position + s2.position) * 0.5f : s.position;
            float yaw = s2 != null ? Mathf.LerpAngle(s.yaw, s2.yaw, 0.5f) : s.yaw;
            var go = new GameObject("Def_" + def.id);
            go.transform.SetPositionAndRotation(c, Quaternion.Euler(0, yaw, 0));
            var parent = GameObject.Find("Defenses");
            if (!parent) parent = new GameObject("Defenses");
            go.transform.SetParent(parent.transform, true);
            var d = go.AddComponent<Defense>();
            d.def = def; d.level = level; d.lane = s.lane; d.x = c.x;
            d.HalfLen = s2 != null ? Mathf.Abs(s2.position.x - s.position.x) * 0.5f + 2.7f : 2.7f;
            bool hpMain = def.defense == DefenseKind.Vallado || def.defense == DefenseKind.Infanteria;
            d.baseHp = def.stats.hp * CardDef.LevelHp(level) * (hpMain ? d.Mul : 1);
            d.maxHp = d.hp = d.baseHp * (GameState.I ? GameState.I.DefenseHpMul : 1);
            d.cycleT = def.cycleOn;
            d.Build();
            All.Add(d);
            return d;
        }

        void OnDestroy() { All.Remove(this); }

        public void RefreshHp()
        {
            float k = hp / Mathf.Max(1, maxHp);
            maxHp = baseHp * (GameState.I ? GameState.I.DefenseHpMul : 1);
            hp = maxHp * k;
        }

        // ------------------------------------------------------------------ visual (modelos de la policía de la escena)

        void Build()
        {
            var a = GameAssets.I;
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            if (def.placePrefab) { Instantiate(def.placePrefab, model); }
            else switch (def.defense)
            {
                case DefenseKind.Vallado:
                    // vallas en L (valla_l): pie hacia el este, del lado de los manifestantes
                    foreach (float z in new[] { -1.2f, 1.2f }) Inst(a.valla, new Vector3(1.2f, 0, z), 90, 1f);
                    if (level >= 2) foreach (float z in new[] { -1.2f, 1.2f }) Inst(a.valla, new Vector3(-1.0f, 0, z), 90, 1f);
                    break;
                case DefenseKind.Infanteria:
                    for (int r = 0; r < 2; r++)
                        for (int i = 0; i < 3; i++) Officer(new Vector3(1.6f - r * 1.5f, 0, (i - 1) * 1.35f + r * 0.45f));
                    if (level >= 2) for (int i = 0; i < 2; i++) Officer(new Vector3(-1.5f, 0, (i - 0.5f) * 1.4f));
                    break;
                case DefenseKind.Lanzagases:
                    Officer(new Vector3(0.4f, 0, -0.85f));
                    var o = Officer(new Vector3(0.4f, 0, 0.85f));
                    FxKit.Prim(PrimitiveType.Cylinder, o.transform, new Vector3(0.22f, 1.25f, 0.25f), new Vector3(0.14f, 0.38f, 0.14f), new Color(0.18f, 0.22f, 0.18f), Quaternion.Euler(55, 0, 0));
                    FxKit.Prim(PrimitiveType.Cube, model, new Vector3(1.7f, 0.3f, 0), new Vector3(0.7f, 0.6f, 2.6f), new Color(0.62f, 0.56f, 0.42f)); // bolsas
                    break;
                case DefenseKind.Detencion:
                    for (int i = 0; i < 3; i++) Officer(new Vector3(1.1f - (i % 2) * 0.7f, 0, (i - 1) * 1.4f));
                    break;
                case DefenseKind.Motorizada:
                    BuildMotos();
                    motoWait = 1f;
                    break;
                case DefenseKind.Hidrante:
                    Inst(a.truck, Vector3.zero, 90, 1.25f);
                    jet = FxKit.Particles(model, new Vector3(0.9f, 4.9f, 0), Quaternion.Euler(-6, 90, 0), a.water,
                        new Color(0.85f, 0.93f, 1f, 0.8f), 16f, 0.75f, 0.35f, 0.9f, 180, 4f, 1.2f, 0.1f, 2.4f, 1200);
                    break;
            }
            bar = HpBar.Create(transform, def.defense == DefenseKind.Hidrante ? 6.4f : 4.6f, 3.6f, 0.55f);
            if (def.defense == DefenseKind.Motorizada)
            {
                cdBar = HpBar.Create(transform, 4.0f, 3.6f, 0.28f);
                cdBar.fixedColor = new Color(0.45f, 0.75f, 1f);
                cdBar.alwaysShow = true;
            }
            StartCoroutine(PopIn());
        }

        GameObject Inst(GameObject src, Vector3 lp, float yaw, float s)
        {
            if (!src) return null;
            var g = Instantiate(src, model);
            g.transform.localPosition = lp;
            g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            g.transform.localScale = Vector3.one * s;
            return g;
        }

        public const float PeopleScale = 1.45f * 1.4f; // personajes del área de juego x1.4 para que se lea qué unidad es

        GameObject Officer(Vector3 lp) => Inst(GameAssets.I.officer, lp, 90 + Random.Range(-6f, 6f), PeopleScale);

        System.Collections.IEnumerator PopIn()
        {
            float t = 0;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = t / 0.35f, c1 = 1.70158f;
                model.localScale = Vector3.one * Mathf.Max(0, 1 + (c1 + 1) * Mathf.Pow(k - 1, 3) + c1 * Mathf.Pow(k - 1, 2));
                yield return null;
            }
            model.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ bloqueo

        // Defensa que frena a este manifestante (mismo carril, en contacto con su cara este).
        public static Defense BlockerFor(Enemy e)
        {
            Defense best = null;
            foreach (var d in All)
            {
                if (d.Dead || d.lane != e.lane || !d.Targetable) continue;
                float gap = e.x - d.EastFace;
                if (gap < -0.6f || gap > 0.9f) continue;
                if (d.def.maxBlock > 0 && !d.blocking.Contains(e) && d.blocking.Count >= d.def.maxBlock + (d.level >= 2 ? 1 : 0)) continue;
                if (!best || d.EastFace > best.EastFace) best = d;
            }
            if (best && !best.blocking.Contains(e)) best.blocking.Add(e);
            return best;
        }

        public void Release(Enemy e) => blocking.Remove(e);

        // ------------------------------------------------------------------ comportamiento

        void Update()
        {
            if (Dead) return;
            blocking.RemoveAll(e => e == null || e.Dead);
            float dt = Time.deltaTime;
            cd -= dt;
            punch = Mathf.MoveTowards(punch, 0, dt * 5);
            model.localPosition = Vector3.right * punch * 0.15f;
            switch (def.defense)
            {
                case DefenseKind.Infanteria: Melee(); break;
                case DefenseKind.Lanzagases: Gas(); break;
                case DefenseKind.Detencion: Detain(); break;
                case DefenseKind.Hidrante: Water(dt); break;
                case DefenseKind.Motorizada: Moto(dt); break;
            }
        }

        void Melee()
        {
            if (cd > 0) return;
            Enemy t = null;
            foreach (var e in blocking) if (e && !e.Dead) { t = e; break; }
            if (!t)
            {
                float best = def.stats.range + 0.6f;
                foreach (var e in Enemy.All)
                {
                    if (!e.onLane || e.lane != lane || e.Dead) continue;
                    float gap = e.x - EastFace;
                    if (gap >= -0.6f && gap < best) { best = gap; t = e; }
                }
            }
            if (!t) return;
            cd = 1f / Mathf.Max(0.05f, def.stats.rate);
            t.TakeDamage(def.stats.damage);
            punch = 1;
        }

        void Gas()
        {
            if (cd > 0) return;
            // al grupo más denso dentro del alcance
            Enemy t = null;
            int bestN = 0;
            float r2 = (def.stats.range + HalfLen) * (def.stats.range + HalfLen), a2 = def.stats.area * def.stats.area;
            foreach (var e in Enemy.All)
            {
                if (e.Dead || (e.transform.position - transform.position).sqrMagnitude > r2) continue;
                int n = 0;
                foreach (var o in Enemy.All) if ((o.transform.position - e.transform.position).sqrMagnitude < a2) n++;
                if (n > bestN) { bestN = n; t = e; }
            }
            if (!t) return;
            cd = 1f / Mathf.Max(0.05f, def.stats.rate);
            GasCloud.Throw(transform.position + Vector3.up * 1.6f, t.transform.position, def.stats.area, def.stats.damage * Mul,
                def.slow, Mathf.Max(def.slowTime, level >= 3 ? 5 : 3));
            punch = 1;
        }

        void Detain()
        {
            if (cd > 0) return;
            Enemy t = null;
            float r2 = (def.stats.range + HalfLen) * (def.stats.range + HalfLen);
            foreach (var e in Enemy.All)
            {
                if (e.Dead || e.def.members > 1 || (e.transform.position - transform.position).sqrMagnitude > r2) continue;
                if (!t || e.Cost > t.Cost) t = e;
            }
            if (!t) return;
            cd = 1f / Mathf.Max(0.05f, def.stats.rate * Mul);
            t.Capture();
            punch = 1;
        }

        void Water(float dt)
        {
            cycleT -= dt;
            if (cycleT <= 0)
            {
                spraying = !spraying;
                cycleT = spraying ? def.cycleOn : def.cycleOff;
                var em = jet.emission;
                em.enabled = spraying;
            }
            if (!spraying) return;
            float reach = def.stats.range;
            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.onLane || e.lane != lane || e.Dead) continue;
                float gap = e.x - EastFace;
                if (gap < -0.8f || gap > reach) continue;
                e.TakeDamage(def.stats.damage * Mul * dt);
                e.Wet(1.5f);
                e.Slow(def.slow, 0.5f);
                e.Push(def.push * dt);
            }
        }

        // La motorizada nunca deja el slot vacío: cuando una sale a los porrazos, en el slot ya aparece otra recargando
        // (la recarga es un poco más lenta para mantener el ritmo). La que espera bloquea y puede perder gobernabilidad.
        void Moto(float dt)
        {
            float cooldown = (def.cycleOff > 0 ? def.cycleOff : 12f) * 1.25f / Mul;
            motoAppear = Mathf.Min(1, motoAppear + dt / 0.6f);
            moto.localScale = Vector3.one * (1 - (1 - motoAppear) * (1 - motoAppear));
            motoWait -= dt;
            cdBar.Set(Mathf.Max(0.001f, 1f - Mathf.Clamp01(motoWait / cooldown)));
            if (motoWait > 0) return;
            var runner = moto;
            runner.SetParent(null, true);
            runner.localScale = Vector3.one;
            MotoRunner.Go(runner.gameObject, def, level, lane, x);
            BuildMotos();
            moto.localScale = Vector3.zero;
            motoAppear = 0;
            motoWait = cooldown;
        }

        public void TakeDamage(float a)
        {
            if (Dead) return;
            hp -= a;
            punch = Mathf.Max(punch, 0.5f);
            bar.Set(hp / maxHp);
            if (hp <= 0) Break();
        }

        void Break()
        {
            Dead = true;
            All.Remove(this);
            foreach (var e in blocking) if (e) { }
            blocking.Clear();
            if (GameState.I && def.defense != DefenseKind.Vallado) GameState.I.PushHeadline("Retroceden las fuerzas de seguridad: cae la línea de " + def.title.ToLowerInvariant());
            StartCoroutine(Fall());
        }

        System.Collections.IEnumerator Fall()
        {
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                model.localScale = Vector3.one * (1 - t / 0.5f);
                yield return null;
            }
            Destroy(gameObject); // el slot queda libre (detección por objeto nulo)
        }
    }

    // Nube de gas lacrimógeno: daño por segundo y ralentiza en un radio.
    public class GasCloud : MonoBehaviour
    {
        float radius, dps, slow, life, tick;
        Vector3 from, to;
        float travel;
        bool landed;
        ParticleSystem ps;

        public static void Throw(Vector3 from, Vector3 to, float radius, float dps, float slow, float life)
        {
            var go = new GameObject("Gas");
            go.transform.position = from;
            var g = go.AddComponent<GasCloud>();
            g.from = from; g.to = to; g.radius = Mathf.Max(1, radius); g.dps = dps; g.slow = slow; g.life = life;
            FxKit.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.25f, new Color(0.3f, 0.32f, 0.3f));
        }

        void Update()
        {
            if (!landed)
            {
                travel += Time.deltaTime / 0.7f;
                transform.position = Vector3.Lerp(from, to, travel) + Vector3.up * Mathf.Sin(Mathf.Clamp01(travel) * Mathf.PI) * 5f;
                if (travel < 1) return;
                landed = true;
                foreach (Transform c in transform) Destroy(c.gameObject);
                ps = FxKit.Particles(transform, Vector3.up * 0.3f, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke,
                    new Color(0.86f, 0.88f, 0.78f, 0.55f), 0.6f, 2.4f, radius * 0.9f, radius * 1.5f, 14, 70, -0.01f, radius * 0.6f, 1.6f);
            }
            life -= Time.deltaTime;
            tick -= Time.deltaTime;
            if (tick <= 0)
            {
                tick = 0.25f;
                foreach (var e in Enemy.All.ToArray())
                    if (!e.Dead && (e.transform.position - transform.position).sqrMagnitude < radius * radius)
                    {
                        e.TakeDamage(dps * 0.25f);
                        e.Slow(slow, 0.6f);
                    }
            }
            if (life <= 0)
            {
                if (ps) { var em = ps.emission; em.enabled = false; }
                Destroy(gameObject, 2.5f);
                enabled = false;
            }
        }
    }

    // Motorizada: queda estacionada al pie de su carril, frente a la Casa Rosada. Cada cierto tiempo sale a los porrazos
    // hacia Av. de Mayo (izquierda de la pantalla) sin salir del área de juego, desaparece en el borde y vuelve a aparecer
    // estacionada esperando el próximo turno. Golpea, empuja hacia atrás y aturde a cada manifestante una vez por pasada.
    public class MotoRun : MonoBehaviour
    {
        public static readonly List<MotoRun> All = new List<MotoRun>();
        CardDef def;
        int level, lane;
        float x, homeX, endX, phase, wait, appear;
        bool running;
        readonly HashSet<Enemy> hit = new HashSet<Enemy>();
        Transform model;
        HpBar cdBar;
        const float Speed = 12f;
        float Cooldown => (def.cycleOff > 0 ? def.cycleOff : 12f) / CardDef.LevelMul(level);

        public static void Launch(CardDef def, int level, int lane)
        {
            var slots = PoliceSlots.I;
            var go = new GameObject("Motorizada");
            var parent = GameObject.Find("Defenses");
            if (!parent) parent = new GameObject("Defenses");
            go.transform.SetParent(parent.transform, true);
            var m = go.AddComponent<MotoRun>();
            m.def = def; m.level = level; m.lane = lane;
            // varias motos en el mismo carril se estacionan una detrás de otra
            int n = 0; foreach (var o in All) if (o.lane == lane) n++;
            m.homeX = slots.WestX(lane) + 3.5f + n * 3.2f;
            m.endX = slots.EastX(lane) - 1.5f;
            m.x = m.homeX;
            m.wait = 0.8f;
            m.model = new GameObject("Model").transform;
            m.model.SetParent(go.transform, false);
            int k = level >= 2 ? 3 : 2;
            for (int i = 0; i < k; i++) Moto(m.model, new Vector3(-(i % 2) * 2.6f, 0, (i - (k - 1) * 0.5f) * 1.6f));
            m.model.localScale = Vector3.zero;
            m.cdBar = HpBar.Create(go.transform, 4.2f, 2.6f, 0.35f);
            All.Add(m);
            m.Place();
        }

        void OnDestroy() { All.Remove(this); }

        // Moto policial con primitivas (placeholder hasta tener el modelo de Blender). Local +X = adelante.
        internal static void Moto(Transform p, Vector3 lp)
        {
            var b = new GameObject("Moto").transform;
            b.SetParent(p, false);
            b.localPosition = lp;
            b.localScale = Vector3.one * 1.4f;
            var dark = new Color(0.08f, 0.08f, 0.09f);
            var navy = new Color(0.12f, 0.22f, 0.5f);
            foreach (var wx in new[] { -0.75f, 0.75f })
            {
                FxKit.Prim(PrimitiveType.Cylinder, b, new Vector3(wx, 0.36f, 0), new Vector3(0.72f, 0.08f, 0.72f), dark, Quaternion.Euler(90, 0, 0));
                FxKit.Prim(PrimitiveType.Cylinder, b, new Vector3(wx, 0.36f, 0), new Vector3(0.3f, 0.1f, 0.3f), new Color(0.7f, 0.72f, 0.75f), Quaternion.Euler(90, 0, 0));
            }
            FxKit.Prim(PrimitiveType.Cube, b, new Vector3(0, 0.68f, 0), new Vector3(1.15f, 0.32f, 0.32f), navy);
            FxKit.Prim(PrimitiveType.Capsule, b, new Vector3(0.25f, 0.9f, 0), new Vector3(0.36f, 0.32f, 0.36f), Color.white, Quaternion.Euler(0, 0, 90));
            FxKit.Prim(PrimitiveType.Cube, b, new Vector3(-0.3f, 0.92f, 0), new Vector3(0.62f, 0.12f, 0.34f), dark);
            FxKit.Prim(PrimitiveType.Cylinder, b, new Vector3(0.62f, 0.72f, 0), new Vector3(0.07f, 0.36f, 0.07f), new Color(0.6f, 0.6f, 0.62f), Quaternion.Euler(0, 0, 25));
            FxKit.Prim(PrimitiveType.Cube, b, new Vector3(0.52f, 1.12f, 0), new Vector3(0.08f, 0.06f, 0.75f), dark);
            FxKit.Prim(PrimitiveType.Cube, b, new Vector3(0.62f, 1.28f, 0), new Vector3(0.04f, 0.3f, 0.46f), new Color(0.7f, 0.85f, 0.95f));
            FxKit.Prim(PrimitiveType.Sphere, b, new Vector3(0.82f, 0.95f, 0), Vector3.one * 0.2f, new Color(1f, 0.97f, 0.8f));
            FxKit.Prim(PrimitiveType.Cube, b, new Vector3(-0.62f, 1.18f, 0), new Vector3(0.12f, 0.1f, 0.22f), new Color(0.15f, 0.35f, 1f));
            var o = Object.Instantiate(GameAssets.I.officer, b);
            o.transform.localPosition = new Vector3(-0.15f, 0.45f, 0);
            o.transform.localRotation = Quaternion.Euler(0, 90, 0);
            o.transform.localScale = Vector3.one * 1.05f;
        }

        void Place()
        {
            var slots = PoliceSlots.I;
            var p = slots.LanePoint(lane, x);
            var ahead = slots.LanePoint(lane, x + 1);
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(ahead - p) * Quaternion.Euler(0, -90, 0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            appear = Mathf.Min(1, appear + dt / 0.35f);
            if (!running)
            {
                // estacionada al pie del carril esperando el próximo turno
                wait -= dt;
                phase += dt * 3;
                model.localScale = Vector3.one * appear;
                model.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * 0.015f;
                cdBar.Set(1f - Mathf.Clamp01(wait / Cooldown));
                if (wait <= 0) { running = true; hit.Clear(); cdBar.Set(1); }
                return;
            }
            x += Speed * dt;
            phase += dt * 22;
            Place();
            model.localScale = Vector3.one * Mathf.Min(appear, Mathf.Clamp01((endX - x) / 2.5f));
            model.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * 0.05f;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e.Dead || !e.onLane || e.lane != lane || hit.Contains(e)) continue;
                if (Mathf.Abs(e.x - x) > 2f) continue;
                hit.Add(e);
                e.TakeDamage(def.stats.damage * CardDef.LevelMul(level));
                e.Push(def.push);
                e.Stun(def.stun);
            }
            if (x >= endX)
            {
                // vuelve a aparecer estacionada frente a la Casa Rosada
                running = false; x = homeX; wait = Cooldown; appear = 0;
                Place();
            }
        }
    }

    // Pasada de la motorizada que salió del slot: corre por su carril hasta Av. de Mayo y desaparece en el borde.
    public class MotoRunner : MonoBehaviour
    {
        CardDef def;
        int level, lane;
        float x, phase;
        readonly HashSet<Enemy> hit = new HashSet<Enemy>();

        public static void Go(GameObject go, CardDef def, int level, int lane, float x)
        {
            var r = go.AddComponent<MotoRunner>();
            r.def = def; r.level = level; r.lane = lane; r.x = x;
        }

        void Update()
        {
            var slots = PoliceSlots.I;
            float dt = Time.deltaTime;
            x += 12f * dt;
            phase += dt * 22;
            float endX = slots.EastX(lane) - 1.5f;
            var p = slots.LanePoint(lane, x);
            transform.position = p + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * 0.05f;
            transform.rotation = Quaternion.LookRotation(slots.LanePoint(lane, x + 1) - p) * Quaternion.Euler(0, -90, 0);
            transform.localScale = Vector3.one * Mathf.Clamp01((endX - x) / 2.5f);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e.Dead || !e.onLane || e.lane != lane || hit.Contains(e) || Mathf.Abs(e.x - x) > 2f) continue;
                hit.Add(e);
                e.TakeDamage(def.stats.damage * CardDef.LevelMul(level));
                e.Push(def.push);
                e.Stun(def.stun);
            }
            if (x >= endX) Destroy(gameObject);
        }
    }
}
