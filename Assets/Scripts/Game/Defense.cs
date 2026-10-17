using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Elemento policial colocado en un slot del carril. Todos frenan al 100% a los manifestantes de su carril
    // (solo el encapuchado tiene chance de saltar un retén) y se rompen al quedar sin vida, liberando el slot.
    public class Defense : MonoBehaviour
    {
        public static readonly List<Defense> All = new List<Defense>();

        public const float SlotStep = 6.8f; // distancia entre centros de slots (1 casillero)

        public CardDef def;
        public int level = 1;
        public int lane;
        public float x;
        public float HalfLen { get; private set; } = 2.7f;
        public float hp, maxHp;
        public bool Dead { get; private set; }
        float faceOff = 2.7f; // del centro a la cara este (el vallado queda al fondo del slot)
        public float EastFace => x + faceOff;
        public float WestFace => x - HalfLen;

        readonly List<Enemy> blocking = new List<Enemy>();
        float cd, cycleT, punch, baseHp;
        bool reloading;
        float tank = 999, reload; // hidrante: segundos de agua que quedan / de recarga
        ParticleSystem jet;
        List<int> nearLanes;
        // balas de goma / gases: agentes que disparan (retroceso)
        readonly List<Fidget> shooters = new List<Fidget>();
        // vallado: vallas que se sacuden al recibir golpes
        readonly List<Transform> vallas = new List<Transform>();
        readonly List<Vector3> vallaPos = new List<Vector3>();
        readonly List<Quaternion> vallaRot = new List<Quaternion>();
        float shake;
        // hidrante: cañón giratorio y luces de sirena
        Transform turret;
        Material sirenRed, sirenBlue;
        Color sirenRedEm, sirenBlueEm;
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
            d.faceOff = d.HalfLen;
            bool hpMain = def.defense == DefenseKind.Vallado || def.defense == DefenseKind.Infanteria;
            d.baseHp = def.stats.hp * CardDef.LevelHp(level) * (hpMain ? d.Mul : 1);
            d.maxHp = d.hp = d.baseHp * (GameState.I ? GameState.I.DefenseHpMul : 1);
            d.cycleT = def.cycleOn;
            d.tank = def.cycleOn;
            d.nearLanes = PoliceSlots.I ? PoliceSlots.I.NeighborLanes(d.lane, c.x) : new List<int> { d.lane };
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
                {
                    // una sola línea de vallas en L, lo más al fondo (oeste) del slot; el pie mira al este
                    float fx = -(HalfLen - 1.35f);
                    foreach (float z in new[] { -1.2f, 1.2f })
                    {
                        var v = Inst(a.valla, new Vector3(fx, 0, z), 90, 1f);
                        if (!v) continue;
                        vallas.Add(v.transform); vallaPos.Add(v.transform.localPosition); vallaRot.Add(v.transform.localRotation);
                    }
                    faceOff = fx + 1.0f;
                    break;
                }
                case DefenseKind.Infanteria:
                    // la fila de adelante se mantiene firme; las de atrás se reacomodan
                    for (int r = 0; r < 2; r++)
                        for (int i = 0; i < 3; i++) Officer(new Vector3(1.6f - r * 1.5f, 0, (i - 1) * 1.35f + r * 0.45f), r == 0 ? Fidget.Mode.Firm : Fidget.Mode.Shuffle);
                    if (level >= 2) for (int i = 0; i < 2; i++) Officer(new Vector3(-1.5f, 0, (i - 0.5f) * 1.4f), Fidget.Mode.Shuffle);
                    break;
                case DefenseKind.BalasGoma:
                    // misma formación que la infantería, sin escudos y con escopetas antitumulto
                    for (int r = 0; r < 2; r++)
                        for (int i = 0; i < 3; i++) GunOfficer(new Vector3(1.6f - r * 1.5f, 0, (i - 1) * 1.35f + r * 0.45f), r == 0 ? Fidget.Mode.Firm : Fidget.Mode.Shuffle);
                    if (level >= 2) for (int i = 0; i < 2; i++) GunOfficer(new Vector3(-1.5f, 0, (i - 0.5f) * 1.4f), Fidget.Mode.Shuffle);
                    break;
                case DefenseKind.Lanzagases:
                {
                    // un solo agente con lanzagranadas de gas
                    var o = Officer(new Vector3(0.6f, 0, 0), Fidget.Mode.Shuffle, 0.22f);
                    if (o)
                    {
                        var olive = new Color(0.2f, 0.25f, 0.18f);
                        FxKit.Prim(PrimitiveType.Cylinder, o.transform, new Vector3(0.16f, 1.25f, 0.3f), new Vector3(0.16f, 0.36f, 0.16f), olive, Quaternion.Euler(60, 0, 0));
                        FxKit.Prim(PrimitiveType.Cylinder, o.transform, new Vector3(0.16f, 1.1f, 0.12f), new Vector3(0.26f, 0.09f, 0.26f), new Color(0.12f, 0.13f, 0.12f), Quaternion.Euler(60, 0, 0)); // tambor
                        shooters.Add(o.GetComponent<Fidget>());
                    }
                    break;
                }
                case DefenseKind.Detencion:
                    for (int i = 0; i < 3; i++) Officer(new Vector3(1.1f - (i % 2) * 0.7f, 0, (i - 1) * 1.4f), Fidget.Mode.Shuffle, 0.25f);
                    break;
                case DefenseKind.Motorizada:
                    BuildMotos();
                    motoWait = 1f;
                    break;
                case DefenseKind.Hidrante:
                {
                    var truck = Inst(a.truck, Vector3.zero, 90, 1.25f);
                    Sirens(truck);
                    // cañón de agua en el techo: gira hacia el manifestante más cercano
                    turret = new GameObject("Cannon").transform;
                    turret.SetParent(model, false);
                    turret.localPosition = new Vector3(0.9f, 4.9f, 0);
                    turret.localRotation = Quaternion.Euler(-6, 90, 0);
                    FxKit.Prim(PrimitiveType.Cylinder, turret, new Vector3(0, -0.1f, 0), new Vector3(0.6f, 0.12f, 0.6f), new Color(0.12f, 0.13f, 0.15f));
                    FxKit.Prim(PrimitiveType.Cylinder, turret, new Vector3(0, 0, 0.55f), new Vector3(0.2f, 0.55f, 0.2f), new Color(0.22f, 0.24f, 0.27f), Quaternion.Euler(90, 0, 0));
                    jet = FxKit.Particles(turret, new Vector3(0, 0, 1.1f), Quaternion.identity, a.water,
                        new Color(0.85f, 0.93f, 1f, 0.8f), WaterSpeed, 1.4f, 0.35f, 0.9f, 220, 3f, WaterGravity, 0.1f, 2.4f, 3000);
                    var em = jet.emission; em.enabled = false;
                    break;
                }
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

        GameObject Officer(Vector3 lp, Fidget.Mode mode = Fidget.Mode.Shuffle, float radius = 0.32f)
        {
            var g = Inst(GameAssets.I.officer, lp, 90 + Random.Range(-6f, 6f), PeopleScale);
            if (g) Fidget.Add(g, mode, radius);
            return g;
        }

        // Punta del caño de la escopeta (espacio local del agente; +Z = adelante).
        static readonly Vector3 GunMuzzle = new Vector3(0.12f, 1.14f, 0.78f);

        GameObject GunOfficer(Vector3 lp, Fidget.Mode mode)
        {
            var o = Officer(lp, mode);
            if (!o) return null;
            NoShield(o);
            var t = o.transform;
            var dark = new Color(0.1f, 0.1f, 0.11f);
            FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.12f, 1.14f, 0.48f), new Vector3(0.07f, 0.3f, 0.07f), dark, Quaternion.Euler(90, 0, 0)); // caño
            FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0.12f, 1.1f, 0.38f), new Vector3(0.1f, 0.09f, 0.22f), new Color(0.95f, 0.45f, 0.1f)); // guardamano naranja (antitumulto)
            FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0.12f, 1.08f, 0.1f), new Vector3(0.09f, 0.14f, 0.34f), new Color(0.3f, 0.2f, 0.12f)); // culata
            shooters.Add(o.GetComponent<Fidget>());
            return o;
        }

        // Agente sin escudo: misma malla con el submesh del escudo vacío (la malla del FBX es legible).
        static Mesh noShieldMesh;
        static void NoShield(GameObject o)
        {
            var mf = o.GetComponent<MeshFilter>();
            var r = o.GetComponent<Renderer>();
            if (!mf || !r) return;
            if (!noShieldMesh)
            {
                var src = mf.sharedMesh;
                int idx = System.Array.FindIndex(r.sharedMaterials, m => m && m.name.StartsWith("M_Shield"));
                if (!src || !src.isReadable || idx < 0) return;
                noShieldMesh = Instantiate(src);
                noShieldMesh.name = src.name + "_noshield";
                noShieldMesh.SetTriangles(new int[0], idx);
            }
            mf.sharedMesh = noShieldMesh;
        }

        // Luces de la sirena del hidrante: instancias de M_LightRed / M_LightBlue que titilan alternadas.
        void Sirens(GameObject truck)
        {
            if (!truck) return;
            var r = truck.GetComponentInChildren<Renderer>();
            if (!r) return;
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m.name.StartsWith("M_LightRed")) { sirenRed = m; sirenRedEm = m.GetColor("_EmissionColor"); }
                else if (m.name.StartsWith("M_LightBlue")) { sirenBlue = m; sirenBlueEm = m.GetColor("_EmissionColor"); }
            }
            foreach (var m in new[] { sirenRed, sirenBlue }) if (m) m.EnableKeyword("_EMISSION");
        }

        void Siren()
        {
            float p = Mathf.Repeat(Time.time * 1.6f, 1f);
            bool red = p < 0.1f || (p > 0.18f && p < 0.28f);
            bool blue = (p > 0.5f && p < 0.6f) || (p > 0.68f && p < 0.78f);
            if (sirenRed) sirenRed.SetColor("_EmissionColor", sirenRedEm * (red ? 2.6f : 0.06f));
            if (sirenBlue) sirenBlue.SetColor("_EmissionColor", sirenBlueEm * (blue ? 2.6f : 0.06f));
        }

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
                if (!best || d.EastFace > best.EastFace) best = d;
            }
            if (best && !best.blocking.Contains(e)) best.blocking.Add(e);
            return best;
        }

        public void Release(Enemy e) => blocking.Remove(e);

        // Manifestantes adelante (al este) dentro del alcance, en el carril propio y, si sides, en los vecinos.
        void Candidates(float range, bool sides, List<Enemy> into)
        {
            into.Clear();
            foreach (var e in Enemy.All)
            {
                if (e.Dead || !e.onLane) continue;
                if (sides ? !nearLanes.Contains(e.lane) : e.lane != lane) continue;
                float gap = e.x - EastFace;
                if (gap < -1f || gap > range) continue;
                into.Add(e);
            }
        }
        static readonly List<Enemy> tmp = new List<Enemy>();

        // ------------------------------------------------------------------ comportamiento

        void Update()
        {
            if (Dead) return;
            blocking.RemoveAll(e => e == null || e.Dead);
            float dt = Time.deltaTime;
            cd -= dt;
            punch = Mathf.MoveTowards(punch, 0, dt * 5);
            if (def.defense != DefenseKind.Vallado) model.localPosition = Vector3.right * punch * 0.15f;
            switch (def.defense)
            {
                case DefenseKind.Vallado: Shake(dt); break;
                case DefenseKind.Infanteria: Melee(); break;
                case DefenseKind.BalasGoma: Shoot(); break;
                case DefenseKind.Lanzagases: Gas(); break;
                case DefenseKind.Detencion: Detain(); break;
                case DefenseKind.Hidrante: Water(dt); Siren(); break;
                case DefenseKind.Motorizada: Moto(dt); break;
            }
        }

        // Vallado golpeado: las vallas se inclinan hacia atrás y vibran, y vuelven a su lugar.
        void Shake(float dt)
        {
            if (shake <= 0 && vallas.Count > 0 && vallas[0].localRotation == vallaRot[0]) return;
            shake = Mathf.MoveTowards(shake, 0, dt * 2.4f);
            float t = Time.time;
            for (int i = 0; i < vallas.Count; i++)
            {
                if (!vallas[i]) continue;
                float k = shake;
                float lean = k * 8f + Mathf.Sin(t * 38f + i * 1.7f) * k * 3.5f;
                float roll = Mathf.Sin(t * 29f + i * 2.3f) * k * 3f;
                vallas[i].localRotation = Quaternion.Euler(roll, 0, lean) * vallaRot[i];
                vallas[i].localPosition = vallaPos[i] + Vector3.left * k * 0.14f;
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

        // Balas de goma: en su carril, prioriza a los que golpean un vallado (o a otra defensa) adelante; si no, al más cercano.
        void Shoot()
        {
            if (cd > 0 || shooters.Count == 0) return;
            Candidates(def.stats.range, false, tmp);
            Enemy t = null;
            float best = float.MaxValue;
            foreach (var e in tmp)
            {
                var b = e.Blocker;
                int pri = b && b != this ? (b.def.defense == DefenseKind.Vallado ? 0 : 1) : 2;
                float score = pri * 1000 + (e.x - EastFace);
                if (score < best) { best = score; t = e; }
            }
            if (!t) return;
            cd = 1f / Mathf.Max(0.05f, def.stats.rate);
            var s = shooters[Random.Range(0, shooters.Count)];
            if (!s) return;
            RubberShot.Fire(s.transform.TransformPoint(GunMuzzle), t, def.stats.damage * Mul);
            s.Kick();
        }

        // Gases: elige al azar entre su carril y los de al lado (los que tengan gente a tiro) y tira al grupo más denso.
        void Gas()
        {
            if (cd > 0) return;
            Candidates(def.stats.range, true, tmp);
            if (tmp.Count == 0) return;
            var lanesWith = new List<int>();
            foreach (var e in tmp) if (!lanesWith.Contains(e.lane)) lanesWith.Add(e.lane);
            int pick = lanesWith[Random.Range(0, lanesWith.Count)];
            Enemy t = null;
            int bestN = 0;
            float a2 = def.stats.area * def.stats.area;
            foreach (var e in tmp)
            {
                if (e.lane != pick) continue;
                int n = 0;
                foreach (var o in tmp) if ((o.transform.position - e.transform.position).sqrMagnitude < a2) n++;
                if (n > bestN) { bestN = n; t = e; }
            }
            if (!t) return;
            cd = 1f / Mathf.Max(0.05f, def.stats.rate);
            var s = shooters.Count > 0 ? shooters[0] : null;
            Vector3 from = s ? s.transform.TransformPoint(new Vector3(0.16f, 1.55f, 0.55f)) : transform.position + Vector3.up * 2.5f;
            GasCloud.Throw(from, t.transform.position, def.stats.area, def.stats.damage * Mul,
                def.slow, Mathf.Max(def.slowTime, level >= 3 ? 5 : 3));
            if (s) s.Kick();
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

        const float WaterSpeed = 22f, WaterGravity = 0.55f;

        // Hidrante: el cañón apunta solo al manifestante más cercano (su carril o los de al lado) y lo moja,
        // lo empuja y lo frena. El tanque (cycleOn s) se gasta solo mientras moja; vacío, recarga cycleOff s.
        void Water(float dt)
        {
            if (reloading) { reload -= dt; if (reload <= 0) { reloading = false; tank = def.cycleOn; } }
            Candidates(def.stats.range, true, tmp);
            Enemy t = null;
            float bd = float.MaxValue;
            foreach (var e in tmp)
            {
                float d = (e.transform.position - turret.position).sqrMagnitude;
                if (d < bd) { bd = d; t = e; }
            }
            Quaternion want;
            float dist = 0;
            if (t)
            {
                var flat = t.transform.position - turret.position;
                flat.y = 0;
                dist = flat.magnitude;
                float g = 9.81f * WaterGravity;
                float s = Mathf.Clamp(dist * 0.85f * g / (WaterSpeed * WaterSpeed), 0, 1);
                float ang = 0.5f * Mathf.Asin(s) * Mathf.Rad2Deg;
                want = Quaternion.LookRotation(flat.normalized) * Quaternion.Euler(-ang, 0, 0);
            }
            else want = Quaternion.LookRotation(transform.right) * Quaternion.Euler(-6, 0, 0);
            turret.rotation = Quaternion.RotateTowards(turret.rotation, want, 140 * dt);
            bool aimed = t && Quaternion.Angle(turret.rotation, want) < 8;
            bool on = !reloading && aimed;
            var em = jet.emission;
            if (em.enabled != on) em.enabled = on;
            if (!on) return;
            tank -= dt;
            if (tank <= 0) { reloading = true; reload = def.cycleOff; }
            var main = jet.main;
            main.startLifetime = Mathf.Clamp(dist / WaterSpeed + 0.25f, 0.4f, 1.8f);
            t.TakeDamage(def.stats.damage * Mul * dt);
            t.Wet(1.5f);
            t.Slow(def.slow, 0.5f);
            t.Push(def.push * dt);
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
            if (def.defense == DefenseKind.Vallado) shake = Mathf.Min(1, shake + 0.65f);
            bar.Set(hp / maxHp);
            if (hp <= 0) Break();
        }

        void Break()
        {
            Dead = true;
            All.Remove(this);
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

    // Bala de goma: proyectil rápido en línea recta (trazador) que sigue al blanco y le pega al llegar.
    public class RubberShot : MonoBehaviour
    {
        Enemy target;
        Vector3 aim;
        float dmg;

        public static void Fire(Vector3 from, Enemy t, float dmg)
        {
            var go = new GameObject("BalaGoma");
            go.transform.position = from;
            FxKit.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(0.14f, 0.14f, 1.0f), new Color(1f, 0.82f, 0.45f));
            var s = go.AddComponent<RubberShot>();
            s.target = t; s.dmg = dmg; s.aim = t.transform.position + Vector3.up * 2.2f;
            var flash = FxKit.Prim(PrimitiveType.Sphere, null, from, Vector3.one * 0.5f, new Color(1f, 0.9f, 0.5f));
            Destroy(flash, 0.06f);
        }

        void Update()
        {
            if (target && !target.Dead) aim = target.transform.position + Vector3.up * 2.2f;
            var dir = aim - transform.position;
            if (dir.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(dir);
            transform.position = Vector3.MoveTowards(transform.position, aim, 55f * Time.deltaTime);
            if ((transform.position - aim).sqrMagnitude > 0.05f) return;
            if (target && !target.Dead) target.TakeDamage(dmg);
            var fx = new GameObject("GomaHit");
            fx.transform.position = aim;
            var ps = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke, new Color(0.9f, 0.85f, 0.75f, 0.7f), 2, 0.35f, 0.25f, 0.5f, 0, 70, 0, 0.1f, 1.6f);
            var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 6) });
            Destroy(fx, 1f);
            Destroy(gameObject);
        }
    }

    // Granada de gas lacrimógeno: vuela en arco largo dejando estela y al caer abre una nube que daña y ralentiza.
    public class GasCloud : MonoBehaviour
    {
        float radius, dps, slow, life, tick;
        Vector3 from, to;
        float travel, dur, arc;
        bool landed;
        Transform can;
        ParticleSystem ps, trail;

        public static void Throw(Vector3 from, Vector3 to, float radius, float dps, float slow, float life)
        {
            var go = new GameObject("Gas");
            go.transform.position = from;
            var g = go.AddComponent<GasCloud>();
            g.from = from; g.to = to; g.radius = Mathf.Max(1, radius); g.dps = dps; g.slow = slow; g.life = life;
            float dist = Vector3.Distance(new Vector3(from.x, 0, from.z), new Vector3(to.x, 0, to.z));
            g.dur = 0.55f + dist / 28f;
            g.arc = 3f + dist * 0.2f;
            g.can = FxKit.Prim(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(0.22f, 0.2f, 0.22f), new Color(0.28f, 0.33f, 0.22f)).transform;
            FxKit.Prim(PrimitiveType.Cylinder, g.can, new Vector3(0, 0.9f, 0), new Vector3(1f, 0.15f, 1f), new Color(0.75f, 0.6f, 0.15f)); // franja
            g.trail = FxKit.Particles(go.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke,
                new Color(0.88f, 0.9f, 0.82f, 0.55f), 0.2f, 0.7f, 0.3f, 0.55f, 45, 30, -0.02f, 0.05f, 2.2f);
        }

        void Update()
        {
            if (!landed)
            {
                travel += Time.deltaTime / dur;
                transform.position = Vector3.Lerp(from, to, travel) + Vector3.up * Mathf.Sin(Mathf.Clamp01(travel) * Mathf.PI) * arc;
                can.Rotate(500 * Time.deltaTime, 0, 260 * Time.deltaTime, Space.Self);
                if (travel < 1) return;
                landed = true;
                transform.position = to;
                if (can) Destroy(can.gameObject);
                if (trail)
                {
                    trail.transform.SetParent(null, true);
                    var te = trail.emission; te.enabled = false;
                    Destroy(trail.gameObject, 1.2f);
                }
                ps = FxKit.Particles(transform, Vector3.up * 0.3f, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke,
                    new Color(0.86f, 0.88f, 0.78f, 0.55f), 0.6f, 2.4f, radius * 0.9f, radius * 1.5f, 14, 70, -0.01f, radius * 0.6f, 1.6f);
                var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 10) });
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
        float x, homeX, endX, wait, appear;
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
        // Las ruedas quedan fijas; el resto (cuerpo + agente) vibra con el motor en marcha.
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
            var body = new GameObject("Body").transform;
            body.SetParent(b, false);
            FxKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.68f, 0), new Vector3(1.15f, 0.32f, 0.32f), navy);
            FxKit.Prim(PrimitiveType.Capsule, body, new Vector3(0.25f, 0.9f, 0), new Vector3(0.36f, 0.32f, 0.36f), Color.white, Quaternion.Euler(0, 0, 90));
            FxKit.Prim(PrimitiveType.Cube, body, new Vector3(-0.3f, 0.92f, 0), new Vector3(0.62f, 0.12f, 0.34f), dark);
            FxKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0.62f, 0.72f, 0), new Vector3(0.07f, 0.36f, 0.07f), new Color(0.6f, 0.6f, 0.62f), Quaternion.Euler(0, 0, 25));
            FxKit.Prim(PrimitiveType.Cube, body, new Vector3(0.52f, 1.12f, 0), new Vector3(0.08f, 0.06f, 0.75f), dark);
            FxKit.Prim(PrimitiveType.Cube, body, new Vector3(0.62f, 1.28f, 0), new Vector3(0.04f, 0.3f, 0.46f), new Color(0.7f, 0.85f, 0.95f));
            FxKit.Prim(PrimitiveType.Sphere, body, new Vector3(0.82f, 0.95f, 0), Vector3.one * 0.2f, new Color(1f, 0.97f, 0.8f));
            FxKit.Prim(PrimitiveType.Cube, body, new Vector3(-0.62f, 1.18f, 0), new Vector3(0.12f, 0.1f, 0.22f), new Color(0.15f, 0.35f, 1f));
            var o = Object.Instantiate(GameAssets.I.officer, body);
            o.transform.localPosition = new Vector3(-0.15f, 0.45f, 0);
            o.transform.localRotation = Quaternion.Euler(0, 90, 0);
            o.transform.localScale = Vector3.one * 1.05f;
            Fidget.Add(body.gameObject, Fidget.Mode.Engine);
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
                // estacionada al pie del carril esperando el próximo turno (el motor vibra solo)
                wait -= dt;
                model.localScale = Vector3.one * appear;
                cdBar.Set(1f - Mathf.Clamp01(wait / Cooldown));
                if (wait <= 0) { running = true; hit.Clear(); cdBar.Set(1); }
                return;
            }
            x += Speed * dt;
            Place();
            model.localScale = Vector3.one * Mathf.Min(appear, Mathf.Clamp01((endX - x) / 2.5f));
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
        float x;
        readonly HashSet<Enemy> hit = new HashSet<Enemy>();

        public static void Go(GameObject go, CardDef def, int level, int lane, float x)
        {
            var r = go.AddComponent<MotoRunner>();
            r.def = def; r.level = level; r.lane = lane; r.x = x;
        }

        void Update()
        {
            var slots = PoliceSlots.I;
            if (!slots) return;
            float dt = Time.deltaTime;
            x += 12f * dt;
            float endX = slots.EastX(lane) - 1.5f;
            var p = slots.LanePoint(lane, x);
            transform.position = p;
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
