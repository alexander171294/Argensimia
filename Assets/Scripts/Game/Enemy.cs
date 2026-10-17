using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Manifestante (o columna organizada). Llega por la calle, toma un carril y avanza hacia el oeste hasta el
    // vallado de la Casa Rosada. Las defensas en el carril lo frenan; si llega, resta gobernabilidad.
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public UnitDef def;
        public float hp, maxHp, damage, rate, speed, range;
        public int lane = -1;
        public bool onLane;
        public float x;
        public bool Dead { get; private set; }
        public float Cost => def ? def.cost : 1;

        float offset, slowK, slowT, stunT, atkCd, tireT, popCd, punch, phase;
        public bool wet;
        float wetT;
        readonly List<Vector3> path = new List<Vector3>();
        int pathIdx;
        Transform model;
        HpBar bar;
        Defense blocker;

        PoliceSlots Slots => PoliceSlots.I;
        float SpeedScale => WaveDirector.I ? WaveDirector.I.speedScale : 1.3f;

        public static Enemy Spawn(UnitDef d, List<Vector3> approach, int lane, float hpMul, float dmgMul, float spdMul)
        {
            var go = new GameObject("Enemy_" + d.id);
            var e = go.AddComponent<Enemy>();
            e.def = d;
            e.maxHp = e.hp = d.stats.hp * hpMul;
            e.damage = d.stats.damage * dmgMul;
            e.rate = Mathf.Max(0.01f, d.stats.rate);
            e.speed = d.stats.speed * spdMul;
            e.range = d.stats.range;
            e.lane = lane;
            e.path.AddRange(approach);
            e.offset = d.members > 1 ? Random.Range(-0.4f, 0.4f) : Random.Range(-1.5f, 1.5f);
            e.phase = Random.value * 10;
            e.tireT = d.tireEvery * Random.Range(0.5f, 1f);
            go.transform.position = approach[0];
            e.BuildModel();
            All.Add(e);
            return e;
        }

        void OnDestroy() { All.Remove(this); }

        // ------------------------------------------------------------------ visual

        void BuildModel()
        {
            var a = GameAssets.I;
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            int n = Mathf.Max(1, def.members);
            for (int i = 0; i < n; i++)
            {
                Vector3 lp = Vector3.zero;
                if (n > 1) lp = new Vector3((i / 3 - 0.5f) * 1.5f + Random.Range(-0.15f, 0.15f), 0, ((i % 3) - 1) * 1.35f + Random.Range(-0.15f, 0.15f));
                Person(a, lp, i);
            }
            if (def.prop == "banner" && a.banner)
            {
                var b = Instantiate(a.banner, model);
                b.transform.localPosition = new Vector3(0, 0, 2.4f);
                b.transform.localRotation = Quaternion.identity;
                b.transform.localScale = Vector3.one * 0.85f;
            }
            float h = (n > 1 ? 5.2f : 4.3f) * def.scale;
            bar = HpBar.Create(transform, h, n > 1 ? 4.2f : def.special ? 3f : 2.4f, n > 1 ? 0.55f : 0.45f);
        }

        void Person(GameAssets a, Vector3 lp, int i)
        {
            var src = a.people[Random.Range(0, a.people.Length)];
            var p = Instantiate(src, model);
            p.transform.localPosition = lp;
            p.transform.localRotation = Quaternion.Euler(0, Random.Range(-14f, 14f), 0);
            p.transform.localScale = Vector3.one * Defense.PeopleScale * def.scale * Random.Range(0.93f, 1.07f);
            foreach (var r in p.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                var sm = r.sharedMaterials;
                for (int k = 0; k < sm.Length; k++)
                {
                    if (sm[k] == null) continue;
                    string nm = sm[k].name;
                    if (nm.StartsWith("M_Shirt")) sm[k] = a.shirts[def.shirt >= 0 ? def.shirt : Random.Range(0, a.shirts.Length)];
                    else if (nm.StartsWith("M_Pants")) sm[k] = a.pants[Random.Range(0, a.pants.Length)];
                    else if (nm.StartsWith("M_Skin")) sm[k] = a.skins[Random.Range(0, a.skins.Length)];
                    else if (nm.StartsWith("M_Hair")) sm[k] = a.hairs[def.hair >= 0 ? def.hair : Random.Range(0, a.hairs.Length)];
                }
                r.sharedMaterials = sm;
            }
            var t = p.transform;
            switch (def.prop)
            {
                case "pot":
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.32f, 0.95f, 0.22f), new Vector3(0.24f, 0.09f, 0.24f), new Color(0.72f, 0.72f, 0.75f));
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.3f, 1.05f, 0.25f), new Vector3(0.03f, 0.16f, 0.03f), new Color(0.5f, 0.32f, 0.18f), Quaternion.Euler(40, 0, 0));
                    break;
                case "mortar":
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.18f, 1.25f, 0.05f), new Vector3(0.14f, 0.42f, 0.14f), new Color(0.24f, 0.3f, 0.2f), Quaternion.Euler(-35, 0, -10));
                    FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.62f, -0.06f), new Vector3(0.36f, 0.3f, 0.38f), new Color(0.3f, 0.2f, 0.12f)); // rastas
                    break;
                case "hood":
                    FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.6f, -0.02f), new Vector3(0.34f, 0.36f, 0.34f), new Color(0.08f, 0.08f, 0.09f));
                    break;
                case "tire":
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.38f, 0.75f, 0), new Vector3(0.5f, 0.07f, 0.5f), new Color(0.07f, 0.07f, 0.07f), Quaternion.Euler(0, 0, 90));
                    break;
                case "flag":
                    if (i % 2 == 0 && a.flagArg)
                    {
                        var f = Instantiate(Random.value < 0.5f ? a.flagArg : a.flagRed, t);
                        f.transform.localPosition = new Vector3(-0.3f, 0, 0.1f) / 1.45f;
                        f.transform.localRotation = Quaternion.Euler(0, Random.Range(-30f, 30f), 0);
                        f.transform.localScale = Vector3.one / 1.45f;
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ lógica

        void Update()
        {
            if (Dead) return;
            float dt = Time.deltaTime;
            atkCd -= dt; popCd -= dt;
            if (slowT > 0) slowT -= dt;
            if (wetT > 0) { wetT -= dt; if (wetT <= 0) wet = false; }
            punch = Mathf.MoveTowards(punch, 0, dt * 4);
            bool moving = false;

            if (stunT > 0)
            {
                stunT -= dt;
                model.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 30) * 8);
                Place(false);
                return;
            }

            float spd = speed * SpeedScale * (slowT > 0 ? 1 - slowK : 1) * AuraMul();

            if (!onLane)
            {
                var target = path[Mathf.Min(pathIdx + 1, path.Count - 1)];
                var p = transform.position;
                var to = target - p; to.y = 0;
                if (to.magnitude < 0.4f || pathIdx + 1 >= path.Count)
                {
                    pathIdx++;
                    if (pathIdx + 1 >= path.Count) { onLane = true; x = Mathf.Min(p.x, Slots.EastX(lane)); }
                }
                else
                {
                    p += to.normalized * Mathf.Min(spd * dt, to.magnitude);
                    p.y = Ground(p.x, p.z);
                    transform.position = p;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 6);
                    moving = true;
                }
            }
            else
            {
                if (def.kind == UnitKind.Mortero && Mortar()) { }
                else
                {
                    var b = Defense.BlockerFor(this);
                    if (b != blocker) { if (blocker) blocker.Release(this); blocker = b; }
                    if (blocker)
                    {
                        if (damage > 0 && atkCd <= 0)
                        {
                            atkCd = 1f / rate;
                            blocker.TakeDamage(damage * (blocker.def.defense == DefenseKind.Vallado ? def.fenceMultiplier : 1));
                            punch = 1;
                        }
                    }
                    else
                    {
                        x -= spd * dt;
                        moving = true;
                        if (x <= Slots.WestX(lane) + 0.6f) { Arrive(); return; }
                    }
                }
                if (def.tireEvery > 0 && x < 34)
                {
                    tireT -= dt;
                    if (tireT <= 0) { tireT = def.tireEvery; TireFire.Drop(transform.position); }
                }
                Place(true);
            }
            Animate(moving);
        }

        void Place(bool face)
        {
            if (!onLane) return;
            float z = Slots.LaneZ(lane, x) + offset;
            transform.position = new Vector3(x, Ground(x, z), z);
            if (face) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, -90, 0), Time.deltaTime * 6);
        }

        static float Ground(float x, float z) => Mathf.Abs(x) <= 42.6f && Mathf.Abs(z) <= 26.6f ? 0.16f : 0.02f;

        // Aparece en el borde del área de juego (sin caminar por la calle).
        public void PopIn() { appear = 0; if (model) model.localScale = Vector3.zero; }
        float appear = 1;

        void Animate(bool moving)
        {
            if (appear < 1) { appear = Mathf.Min(1, appear + Time.deltaTime / 0.35f); model.localScale = Vector3.one * (1 - (1 - appear) * (1 - appear)); }
            phase += Time.deltaTime * (moving ? 9f : 3f);
            float bob = moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.07f : 0;
            model.localPosition = new Vector3(0, bob, 0) + Vector3.left * punch * 0.25f;
            model.localRotation = Quaternion.Euler(0, 0, moving ? Mathf.Sin(phase) * 3.5f : Mathf.Sin(phase) * 1.5f);
        }

        float AuraMul()
        {
            float m = 1;
            foreach (var o in All)
            {
                if (o == this || o.def.auraSpeed <= 0 || o.Dead) continue;
                if ((o.transform.position - transform.position).sqrMagnitude < o.def.auraRadius * o.def.auraRadius) m = Mathf.Max(m, 1 + o.def.auraSpeed);
            }
            return m;
        }

        // Mortero: se planta y tira a la defensa más cercana dentro de su alcance (más lejos que el gas).
        bool Mortar()
        {
            Defense best = null;
            float bd = range * range;
            foreach (var d in Defense.All)
            {
                if (d.Dead || !d.Targetable) continue;
                var dp = d.transform.position;
                if (dp.x > x + 1) continue; // solo hacia adelante (oeste)
                float dd = (dp - transform.position).sqrMagnitude;
                if (dd < bd) { bd = dd; best = d; }
            }
            if (!best) return false;
            if (atkCd <= 0)
            {
                atkCd = 1f / rate;
                MortarShell.Fire(transform.position + Vector3.up * 2f, best.transform.position, damage, def.stats.area);
                punch = 1;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(best.transform.position - transform.position), Time.deltaTime * 5);
            return true;
        }

        // ------------------------------------------------------------------ daño y efectos

        public void TakeDamage(float a)
        {
            if (Dead) return;
            hp -= a;
            punch = Mathf.Max(punch, 0.6f);
            if (def.popOnHit > 0 && popCd <= 0 && GameState.I)
            {
                popCd = 1f;
                GameState.I.AddPopularity(-def.popOnHit);
            }
            if (bar) bar.Set(hp / maxHp);
            if (hp <= 0) Die(false);
        }

        public void Slow(float k, float t) { slowK = Mathf.Max(wet && k > 0 ? Mathf.Max(k, 0.7f) : k, slowT > 0 ? slowK : 0); slowT = Mathf.Max(slowT, t); }
        public void Wet(float t) { wet = true; wetT = Mathf.Max(wetT, t); }
        public void Stun(float t) { stunT = Mathf.Max(stunT, t); }
        public bool Stunned => stunT > 0;

        public void Push(float m)
        {
            if (!onLane || Dead) return;
            x = Mathf.Min(x + m * (1 - def.pushResist), Slots.EastX(lane));
            if (blocker) { blocker.Release(this); blocker = null; }
        }

        public void Capture()
        {
            if (Dead) return;
            if (def.popOnHit > 0 && GameState.I) GameState.I.AddPopularity(-def.popOnHit);
            Die(true);
        }

        void Arrive()
        {
            if (GameState.I)
            {
                GameState.I.LoseGovernability(def.govDamage);
                if (def.popOnArrive > 0) GameState.I.AddPopularity(-def.popOnArrive);
            }
            Die(false, true);
        }

        void Die(bool captured, bool arrived = false)
        {
            if (Dead) return;
            Dead = true;
            if (blocker) blocker.Release(this);
            All.Remove(this);
            if (WaveDirector.I) WaveDirector.I.OnEnemyGone(this, arrived);
            StartCoroutine(Vanish(captured));
        }

        System.Collections.IEnumerator Vanish(bool captured)
        {
            float t = 0;
            var s = transform.localScale;
            var p = transform.position;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float k = t / 0.45f;
                transform.localScale = s * (1 - k);
                transform.position = p + Vector3.up * (captured ? k * 1.5f : -k * 0.3f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    // Proyectil del mortero casero: arco y daño en área a las defensas.
    public class MortarShell : MonoBehaviour
    {
        Vector3 a, b;
        float t, dmg, area;

        public static void Fire(Vector3 from, Vector3 to, float dmg, float area)
        {
            var go = FxKit.Prim(PrimitiveType.Sphere, null, from, Vector3.one * 0.35f, new Color(0.25f, 0.22f, 0.2f));
            var s = go.AddComponent<MortarShell>();
            s.a = from; s.b = to + Vector3.up * 0.3f; s.dmg = dmg; s.area = Mathf.Max(1, area);
        }

        void Update()
        {
            t += Time.deltaTime / 1.1f;
            var p = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 9f;
            transform.position = p;
            if (t < 1) return;
            foreach (var d in Defense.All.ToArray())
                if (!d.Dead && (d.transform.position - b).sqrMagnitude < (area + d.HalfLen) * (area + d.HalfLen)) d.TakeDamage(dmg);
            var fx = new GameObject("MortarHit");
            fx.transform.position = b;
            var ps = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke, new Color(0.55f, 0.5f, 0.45f, 0.8f), 4, 0.9f, 0.8f, 1.6f, 0, 60, 0.3f, 0.6f, 2.2f);
            var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            Destroy(fx, 2f);
            Destroy(gameObject);
        }
    }

    // Goma quemada del piquetero: bloquea el slot libre más cercano unos segundos.
    public class TireFire : MonoBehaviour
    {
        public static void Drop(Vector3 near)
        {
            var slots = PoliceSlots.I;
            if (!slots) return;
            var s = slots.Nearest(near, 7f);
            if (s == null) return;
            var go = new GameObject("TireFire");
            go.transform.position = s.position;
            FxKit.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.12f, 0), new Vector3(0.9f, 0.1f, 0.9f), new Color(0.06f, 0.06f, 0.06f));
            FxKit.Fire(go.transform, new Vector3(0, 0.2f, 0), 0.7f, 18);
            FxKit.Smoke(go.transform, new Vector3(0, 0.6f, 0), 0.7f, new Color(0.18f, 0.17f, 0.17f, 0.7f), 5);
            go.AddComponent<TireFire>();
            slots.Occupy(s, go);
            Destroy(go, 8f);
        }
    }
}
