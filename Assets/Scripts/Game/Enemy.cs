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
        public Defense Blocker => blocker; // defensa que lo está frenando (ej. el vallado que golpea)
        readonly HashSet<Defense> rolled = new HashSet<Defense>(); // retenes contra los que ya tiró la chance de salto
        bool jumping;
        float jumpFrom, jumpTo, jumpT;
        public bool Jumping => jumping;

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

        // Formación fija de la columna organizada: triángulo 3-2-1 (la punta de 3 adelante, el abanderado atrás).
        static readonly Vector3[] Triangle =
        {
            new Vector3(-1.4f, 0, 1.2f), new Vector3(0, 0, 1.2f), new Vector3(1.4f, 0, 1.2f),
            new Vector3(-0.7f, 0, 0), new Vector3(0.7f, 0, 0),
        };

        int uniformShirt = -1; // columna: todos con la misma remera

        void BuildModel()
        {
            var a = GameAssets.I;
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            int n = Mathf.Max(1, def.members);
            if (def.kind == UnitKind.Columna) uniformShirt = def.shirt >= 0 ? def.shirt : Random.Range(0, a.shirts.Length);
            for (int i = 0; i < n; i++)
            {
                Vector3 lp = Vector3.zero;
                if (n > 1) lp = def.kind == UnitKind.Columna && i < Triangle.Length ? Triangle[i]
                    : new Vector3((i / 3 - 0.5f) * 1.5f + Random.Range(-0.15f, 0.15f), 0, ((i % 3) - 1) * 1.35f + Random.Range(-0.15f, 0.15f));
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

        static readonly Color Black = new Color(0.05f, 0.05f, 0.06f);

        void Person(GameAssets a, Vector3 lp, int i)
        {
            // person_c (cartel) y person_d (bombo) solo para el manifestante común; el resto usa cuerpos limpios para leer su silueta
            int pick = def.kind == UnitKind.Comun || a.people.Length < 2 ? Random.Range(0, a.people.Length) : Random.Range(0, 2);
            var p = Instantiate(a.people[pick], model);
            // silueta por tipo: ancho (x, z) y alto (y), e inclinación hacia adelante
            Vector3 body = Vector3.one;
            float lean = 0;
            switch (def.kind)
            {
                case UnitKind.Jubilado: body = new Vector3(0.95f, 0.88f, 0.95f); lean = 14; break;
                case UnitKind.Piquetero: body = new Vector3(0.66f, 1.1f, 0.66f); break;
                case UnitKind.Mortero: body = new Vector3(1.32f, 0.84f, 1.25f); lean = 6; break; // grandote y agazapado, piernas abiertas
            }
            bool column = def.kind == UnitKind.Columna;
            p.transform.localPosition = lp;
            p.transform.localRotation = Quaternion.Euler(lean, column ? 0 : Random.Range(-14f, 14f), 0);
            p.transform.localScale = Vector3.Scale(body, Vector3.one * Defense.PeopleScale * def.scale * (column ? 1 : Random.Range(0.93f, 1.07f)));
            bool allBlack = def.kind == UnitKind.Encapuchado;
            foreach (var r in p.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                var sm = r.sharedMaterials;
                for (int k = 0; k < sm.Length; k++)
                {
                    if (sm[k] == null) continue;
                    if (allBlack) { sm[k] = FxKit.Mat(Black); continue; }
                    string nm = sm[k].name;
                    if (def.kind == UnitKind.Columna)
                    {
                        // columna militante: todos de rojo, jean oscuro
                        if (nm.StartsWith("M_Shirt")) { sm[k] = FxKit.Mat(ColumnRed); continue; }
                        if (nm.StartsWith("M_Pants")) { sm[k] = FxKit.Mat(new Color(0.16f, 0.17f, 0.22f)); continue; }
                    }
                    if (def.kind == UnitKind.Mortero)
                    {
                        // remera roja, jogging celeste, pelo castaño oscuro
                        if (nm.StartsWith("M_Shirt")) { sm[k] = FxKit.Mat(new Color(0.82f, 0.16f, 0.12f)); continue; }
                        if (nm.StartsWith("M_Pants")) { sm[k] = FxKit.Mat(new Color(0.52f, 0.62f, 0.78f)); continue; }
                        if (nm.StartsWith("M_Hair")) { sm[k] = FxKit.Mat(MortarHair); continue; }
                    }
                    if (nm.StartsWith("M_Shirt")) sm[k] = a.shirts[uniformShirt >= 0 ? uniformShirt : def.shirt >= 0 ? def.shirt : Random.Range(0, a.shirts.Length)];
                    else if (nm.StartsWith("M_Pants")) sm[k] = column ? a.pants[0] : a.pants[Random.Range(0, a.pants.Length)];
                    else if (nm.StartsWith("M_Skin")) sm[k] = a.skins[Random.Range(0, a.skins.Length)];
                    else if (nm.StartsWith("M_Hair")) sm[k] = a.hairs[def.hair >= 0 ? def.hair : Random.Range(0, a.hairs.Length)];
                }
                r.sharedMaterials = sm;
            }
            Fidget.Add(p, column ? Fidget.Mode.Firm : Fidget.Mode.Crowd);
            var t = p.transform;
            var wood = new Color(0.45f, 0.3f, 0.17f);
            switch (def.kind)
            {
                case UnitKind.Cacerolero:
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.32f, 0.95f, 0.22f), new Vector3(0.24f, 0.09f, 0.24f), new Color(0.72f, 0.72f, 0.75f));
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.3f, 1.05f, 0.25f), new Vector3(0.03f, 0.16f, 0.03f), new Color(0.5f, 0.32f, 0.18f), Quaternion.Euler(40, 0, 0));
                    break;
                case UnitKind.Jubilado:
                    // boina, anteojos, bastón y bolsa de los mandados
                    FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.74f, -0.01f), new Vector3(0.36f, 0.1f, 0.36f), new Color(0.2f, 0.22f, 0.3f));
                    FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.6f, 0.15f), new Vector3(0.26f, 0.06f, 0.04f), new Color(0.1f, 0.1f, 0.1f));
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.3f, 0.48f, 0.22f), new Vector3(0.045f, 0.48f, 0.045f), wood, Quaternion.Euler(-12, 0, 0));
                    FxKit.Prim(PrimitiveType.Cube, t, new Vector3(-0.3f, 0.75f, 0.05f), new Vector3(0.12f, 0.3f, 0.26f), new Color(0.85f, 0.75f, 0.45f));
                    break;
                case UnitKind.Encapuchado:
                    // pasamontañas negro con ranura para los ojos y un palo
                    FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.62f, 0), new Vector3(0.38f, 0.42f, 0.38f), Black);
                    FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.66f, 0.17f), new Vector3(0.2f, 0.045f, 0.06f), new Color(0.85f, 0.62f, 0.48f));
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.32f, 1.15f, 0.2f), new Vector3(0.05f, 0.55f, 0.05f), new Color(0.3f, 0.2f, 0.12f), Quaternion.Euler(35, 0, 0));
                    break;
                case UnitKind.Piquetero:
                    // flaco, gorra, pañuelo en la cara, palo largo y goma
                    FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.74f, 0.04f), new Vector3(0.36f, 0.12f, 0.42f), new Color(0.15f, 0.2f, 0.45f));
                    FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.48f, 0.14f), new Vector3(0.3f, 0.14f, 0.12f), new Color(0.8f, 0.12f, 0.12f));
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.3f, 1.1f, 0.12f), new Vector3(0.05f, 1.0f, 0.05f), wood);
                    FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.38f, 0.75f, 0), new Vector3(0.55f, 0.1f, 0.55f), new Color(0.07f, 0.07f, 0.07f), Quaternion.Euler(0, 0, 90));
                    break;
                case UnitKind.Mortero:
                    MortarGuy(t);
                    break;
                case UnitKind.Columna:
                    // gorra roja igual para todos: se lee como grupo organizado
                    FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.74f, 0.04f), new Vector3(0.36f, 0.12f, 0.42f), new Color(0.62f, 0.04f, 0.05f));
                    break;
                default:
                    if (def.prop == "flag" && i % 2 == 0 && a.flagArg)
                    {
                        var fl = Instantiate(Random.value < 0.5f ? a.flagArg : a.flagRed, t);
                        fl.transform.localPosition = new Vector3(-0.3f, 0, 0.1f) / 1.45f;
                        fl.transform.localRotation = Quaternion.Euler(0, Random.Range(-30f, 30f), 0);
                        fl.transform.localScale = Vector3.one / 1.45f;
                    }
                    break;
            }
        }

        static readonly Color ColumnRed = new Color(0.8f, 0.06f, 0.08f);
        static readonly Color MortarHair = new Color(0.17f, 0.11f, 0.07f);
        // caño cruzado al cuerpo: sale de la cadera izquierda hacia adelante, a la derecha y arriba
        static readonly Quaternion MortarRot = Quaternion.Euler(62, 18, 0);
        static readonly Vector3 MortarBase = new Vector3(-0.14f, 1.0f, 0.1f);
        public static Vector3 MortarMuzzle => MortarBase + MortarRot * Vector3.up * 1.32f; // punta del caño (espacio de la persona)

        // El "mortero" de la calle: rastas largas, anteojos, barba, mochila y un caño casero envuelto en cinta
        // que sostiene con las dos manos a la altura de la cadera, apuntando adelante y arriba.
        static void MortarGuy(Transform t)
        {
            var dark = new Color(0.07f, 0.07f, 0.08f);
            // rastas: cuelgan de la nuca y los costados hasta los hombros
            for (int k = 0; k < 11; k++)
            {
                float ang = Mathf.Lerp(80f, 280f, k / 10f) * Mathf.Deg2Rad;
                var head = new Vector3(Mathf.Sin(ang) * 0.15f, 1.5f, Mathf.Cos(ang) * 0.15f);
                FxKit.Prim(PrimitiveType.Capsule, t, head + new Vector3(Mathf.Sin(ang) * 0.03f, -0.1f, Mathf.Cos(ang) * 0.03f),
                    new Vector3(0.055f, 0.2f + (k % 3) * 0.035f, 0.055f), MortarHair,
                    Quaternion.Euler(Mathf.Cos(ang) * -14f, 0, Mathf.Sin(ang) * 14f));
            }
            // anteojos
            FxKit.Prim(PrimitiveType.Cube, t, new Vector3(-0.065f, 1.63f, 0.165f), new Vector3(0.1f, 0.06f, 0.02f), dark);
            FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0.065f, 1.63f, 0.165f), new Vector3(0.1f, 0.06f, 0.02f), dark);
            FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.64f, 0.168f), new Vector3(0.05f, 0.015f, 0.015f), dark);
            // barba
            FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.48f, 0.13f), new Vector3(0.17f, 0.09f, 0.07f), MortarHair);
            // mochila negra
            FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.18f, -0.22f), new Vector3(0.34f, 0.4f, 0.16f), dark);
            // el caño: tubo de madera/cartón en diagonal, cinta ocre en la boca y en la empuñadura
            var tube = new Color(0.58f, 0.5f, 0.4f);
            var tape = new Color(0.82f, 0.66f, 0.3f);
            var rot = MortarRot;
            var a = MortarBase;
            var dir = rot * Vector3.up;
            FxKit.Prim(PrimitiveType.Cylinder, t, a + dir * 0.66f, new Vector3(0.075f, 0.66f, 0.075f), tube, rot);
            FxKit.Prim(PrimitiveType.Cylinder, t, a + dir * 1.18f, new Vector3(0.11f, 0.12f, 0.11f), tape, rot);   // boca encintada
            FxKit.Prim(PrimitiveType.Cylinder, t, a + dir * 0.86f, new Vector3(0.095f, 0.04f, 0.095f), tape, rot);  // vuelta de cinta
            FxKit.Prim(PrimitiveType.Cylinder, t, a + dir * 0.14f, new Vector3(0.105f, 0.1f, 0.105f), tape, rot);  // culata encintada
            FxKit.Prim(PrimitiveType.Cylinder, t, a + dir * 1.31f, new Vector3(0.065f, 0.02f, 0.065f), dark, rot);  // boca
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
                if (jumping) { Jump(dt); return; }
                if (def.kind == UnitKind.Mortero && MortarBlocked()) { }
                else
                {
                    var b = Defense.BlockerFor(this);
                    // encapuchado: la primera vez que un retén lo frena, tira la chance de saltarlo
                    if (b && def.jumpChance > 0 && !b.def.noJump && !rolled.Contains(b))
                    {
                        rolled.Add(b);
                        if (Random.value < def.jumpChance) { b.Release(this); StartJump(b); return; }
                    }
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
                    if (tireT <= 0) { tireT = def.tireEvery; TireFire.Drop(transform.position); Headlines.Say("tire", 15f); }
                }
                Place(true);
            }
            Animate(moving);
        }

        void StartJump(Defense d)
        {
            if (blocker) { blocker.Release(this); blocker = null; }
            jumping = true;
            jumpT = 0;
            jumpFrom = x;
            jumpTo = d.WestFace - 1.2f;
            Headlines.Say("jump", 6f);
            Snd.At("boing", transform.position, 1, 0.1f, 0.1f);
        }

        // Salto en arco por encima del retén (con giro), sin poder ser frenado en el aire.
        void Jump(float dt)
        {
            jumpT = Mathf.Min(1, jumpT + dt / 0.75f);
            x = Mathf.Lerp(jumpFrom, jumpTo, jumpT);
            Place(true);
            float h = Mathf.Sin(jumpT * Mathf.PI);
            model.localPosition = Vector3.up * h * 3.2f;
            model.localRotation = Quaternion.Euler(0, 0, -jumpT * 360f) ;
            if (jumpT >= 1)
            {
                jumping = false;
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
                if (x <= Slots.WestX(lane) + 0.6f) Arrive();
            }
        }

        void Place(bool face)
        {
            if (!onLane) return;
            laneBlend = Mathf.MoveTowards(laneBlend, 0, Time.deltaTime * 4.5f);
            float z = Slots.LaneZ(lane, x) + offset + laneBlend;
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

        // Mortero: con la línea limpia avanza como cualquiera. Solo cuando un retén lo frena se planta y dispara:
        // a veces al obstáculo que tiene adelante y a veces a la defensa más cercana a tiro en los carriles de al lado.
        bool MortarBlocked()
        {
            var b = Defense.BlockerFor(this);
            if (!b) return false;
            if (b != blocker) { if (blocker) blocker.Release(this); blocker = b; }
            Defense side = null;
            float bd = range * range;
            foreach (var d in Defense.All)
            {
                if (d.Dead || d == b || d.lane == lane || !d.Targetable) continue;
                var dp = d.transform.position;
                if (dp.x > x + 1) continue; // solo hacia adelante (oeste)
                float dd = (dp - transform.position).sqrMagnitude;
                if (dd < bd) { bd = dd; side = d; }
            }
            if (!aimAt || aimAt.Dead || (aimAt != b && aimAt != side)) aimAt = side && Random.value < 0.5f ? side : b;
            if (atkCd <= 0)
            {
                atkCd = 1f / rate;
                var person = model.childCount > 0 ? model.GetChild(0) : model;
                var muzzle = person.TransformPoint(MortarMuzzle);
                MortarShell.Fire(muzzle, aimAt.transform.position, damage, def.stats.area);
                var puff = new GameObject("MortarPuff");
                puff.transform.position = muzzle;
                var ps = FxKit.Particles(puff.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke, new Color(0.9f, 0.9f, 0.88f, 0.75f), 1.5f, 0.6f, 0.5f, 1.1f, 0, 50, -0.05f, 0.15f, 1.5f);
                var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 10) });
                Destroy(puff, 2f);
                Snd.At("mortar_launch", transform.position, 1, 0.1f);
                Headlines.Say("mortar", 15f);
                punch = 1;
                aimAt = side && Random.value < 0.5f ? side : b; // el próximo tiro vuelve a decidir
            }
            var to = aimAt.transform.position - transform.position; to.y = 0;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 5);
            return true;
        }
        Defense aimAt;

        // ------------------------------------------------------------------ daño y efectos

        // Agua del hidrante: si en el último segundo nadie más le pegó, crítico ×5 (así no queda lejos y empapado
        // eternamente mientras se junta plata gratis).
        float lastHit = -99;
        public const float WaterCrit = 5f;
        public void TakeWater(float a)
        {
            if (Dead) return;
            bool crit = Time.time - lastHit > 1f;
            TakeDamage(a * (crit ? WaterCrit : 1f), false);
        }

        public void TakeDamage(float a) => TakeDamage(a, true);

        void TakeDamage(float a, bool other)
        {
            if (Dead) return;
            if (other) lastHit = Time.time;
            hp -= a;
            punch = Mathf.Max(punch, 0.6f);
            if (def.popOnHit > 0 && popCd <= 0 && GameState.I)
            {
                popCd = 1f;
                GameState.I.LosePopularity(def.popOnHit, transform.position + Vector3.up * 4.5f);
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

        // Desvío: pasa al carril vecino (se desliza de costado, no salta).
        float laneBlend;
        public void ShiftLane(int to)
        {
            if (!onLane || Dead || to == lane || to < 0) return;
            float z0 = Slots.LaneZ(lane, x) + offset + laneBlend;
            if (blocker) { blocker.Release(this); blocker = null; }
            lane = to;
            x = Mathf.Clamp(x, Slots.WestX(lane) + 1f, Slots.EastX(lane));
            laneBlend = z0 - (Slots.LaneZ(lane, x) + offset);
            rolled.Clear();
        }

        // Requisa: un especial queda desarmado y pasa a ser un manifestante común (conserva vida y bronca relativas).
        public bool Disarm(UnitDef to)
        {
            if (Dead || !to || !def.special || def.members > 1 || jumping) return false;
            float k = hp / Mathf.Max(1, maxHp);
            float hpMul = maxHp / Mathf.Max(1, def.stats.hp);
            float dmgMul = def.stats.damage > 0 ? damage / def.stats.damage : 1;
            float spdMul = def.stats.speed > 0 ? speed / def.stats.speed : 1;
            if (blocker) { blocker.Release(this); blocker = null; }
            def = to;
            maxHp = to.stats.hp * hpMul; hp = maxHp * k;
            damage = to.stats.damage * dmgMul;
            rate = Mathf.Max(0.01f, to.stats.rate);
            speed = to.stats.speed * spdMul;
            range = to.stats.range;
            if (model) Destroy(model.gameObject);
            if (bar) Destroy(bar.gameObject);
            BuildModel();
            var fx = new GameObject("Requisa");
            fx.transform.position = transform.position + Vector3.up * 1.5f;
            var ps = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke, new Color(0.9f, 0.9f, 0.95f, 0.8f), 2, 0.6f, 0.5f, 1f, 0, 60, 0, 0.3f, 1.6f);
            var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 12) });
            Destroy(fx, 1.5f);
            return true;
        }

        public void Capture()
        {
            if (Dead) return;
            Achievements.Count("detenidos");
            if (def.popOnHit > 0 && GameState.I) GameState.I.LosePopularity(def.popOnHit, transform.position + Vector3.up * 4.5f);
            if (def.kind == UnitKind.Jubilado) Headlines.Say("capture_jubilado", 12f); else Headlines.Say("capture", 20f);
            Die(true);
        }

        void Arrive()
        {
            if (GameState.I)
            {
                GameState.I.LoseGovernability(def.govDamage, transform.position + Vector3.up * 4.5f);
                if (def.popOnArrive > 0) GameState.I.LosePopularity(def.popOnArrive, transform.position + Vector3.up * 4.5f);
                if (def.kind == UnitKind.Columna) Headlines.Say("arrive_column", 8f); else Headlines.Say("arrive", 10f);
                bool col = def.kind == UnitKind.Columna;
                Snd.At("gate_bang", transform.position, col ? 1f : 0.75f, 0.15f, 0.08f, col ? 0.8f : 1f);
            }
            Die(false, true);
        }

        void Die(bool captured, bool arrived = false)
        {
            if (Dead) return;
            Dead = true;
            if (!arrived && GameState.I) GameState.I.AddKill(Cost); // eliminado o detenido: suma al puntaje
            if (blocker) blocker.Release(this);
            All.Remove(this);
            if (WaveDirector.I) WaveDirector.I.OnEnemyGone(this, arrived);
            if (!captured && !arrived) Snd.At("poof", transform.position, 1, 0.05f, 0.15f);
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
                if (!d.Dead && d.Targetable && (d.transform.position - b).sqrMagnitude < (area + d.HalfLen) * (area + d.HalfLen)) d.TakeDamage(dmg);
            Snd.At("explosion", b, 1, 0.1f);
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
            Snd.At("fire_ignite", s.position, 1, 0.2f);
            Destroy(go, 8f);
        }
    }
}
