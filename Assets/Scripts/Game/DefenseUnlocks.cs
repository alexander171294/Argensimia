using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Defensas desbloqueables (una cada 3 mandatos sobrevividos, ver Meta). Inspiradas en Plants vs Zombies:
    //  Puntero (girasol)      genera $/s durante la oleada; frágil.
    //  Contenedores (nuez)    mucha vida y el encapuchado no lo puede saltar.
    //  Infiltrado (papa mina) se arma en 10 s; nadie lo ve. Detiene al primero que lo pisa y aturde alrededor. Un solo uso.
    //  Comisario (torchwood)  +30% de daño a las defensas a 1 casillero (no se acumula entre comisarios).
    //  Concertina (pinchos)   no frena ni recibe golpes: daño por segundo y freno leve a los que pasan.
    //  Requisa (seta imán)    cada 8 s desarma a un especial cercano (pasa a ser un manifestante común).
    //  Desvío (ajo)           manda a los que llegan al carril vecino mejor defendido. No sirve con columnas.
    //  Estruendo (cereza)     explota al segundo: daño y aturdimiento en área. Un solo uso.
    //  Drone (cattail)        vuela; le tira a cualquier carril a 3 casilleros, primero a los especiales.
    //  Tanqueta (cob cannon)  2 slots; cada 16 s dispara espuma al grupo más denso de toda la Plaza.
    public partial class Defense
    {
        float armT, coinT, auraT, fuse;
        bool armed;
        Transform drone, droneBody, blinker;
        readonly List<Transform> rotors = new List<Transform>();
        readonly HashSet<Enemy> diverted = new HashSet<Enemy>();
        Vector3 aimPoint;
        static UnitDef comun;

        // Refuerzo del comisario sobre d (el mayor de los que lo alcanzan; no se suman).
        static float BuffFor(Defense d)
        {
            float b = 0;
            foreach (var c in All)
            {
                if (c == d || c.Dead || !c.def || c.def.defense != DefenseKind.Comisario) continue;
                float r = c.def.stats.range + c.HalfLen;
                var dp = c.transform.position - d.transform.position;
                if (dp.x * dp.x + dp.z * dp.z > r * r) continue;
                b = Mathf.Max(b, c.def.LevelBuff(c.level));
            }
            return b;
        }

        public static int Count(DefenseKind k)
        {
            int n = 0;
            foreach (var d in All) if (!d.Dead && d.def.defense == k) n++;
            return n;
        }

        // ------------------------------------------------------------------ modelos

        void BuildExtra()
        {
            var a = GameAssets.I;
            drone = droneBody = blinker = null;
            rotors.Clear();
            switch (def.defense)
            {
                case DefenseKind.Puntero:
                {
                    var g = Inst(a.puntero, new Vector3(0.7f, 0, 0), 90, PeopleScale * LevelScale);
                    if (g) Fidget.Add(g, Fidget.Mode.Shuffle, 0.15f);
                    if (level >= 2)
                    {
                        // sombrilla a rayas
                        FxKit.Prim(PrimitiveType.Cylinder, model, new Vector3(-0.9f, 1.7f, 1.5f), new Vector3(0.08f, 1.7f, 0.08f), new Color(0.85f, 0.85f, 0.85f));
                        for (int i = 0; i < 6; i++)
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(-0.9f, 3.35f, 1.5f), new Vector3(2.6f, 0.08f, 0.42f), i % 2 == 0 ? new Color(0.85f, 0.15f, 0.15f) : Color.white, Quaternion.Euler(0, i * 30f, 0));
                    }
                    if (level >= 3)
                    {
                        // bafle y bandera: el acto ya es un acto
                        FxKit.Prim(PrimitiveType.Cube, model, new Vector3(-1.4f, 0.55f, -1.5f), new Vector3(0.7f, 1.1f, 0.6f), new Color(0.1f, 0.1f, 0.11f));
                        FxKit.Prim(PrimitiveType.Cylinder, model, new Vector3(-1.08f, 0.75f, -1.5f), new Vector3(0.45f, 0.02f, 0.45f), new Color(0.3f, 0.3f, 0.32f), Quaternion.Euler(0, 0, 90));
                        if (a.flagArg) Inst(a.flagArg, new Vector3(-1.6f, 0, 1.9f), 90, 1.4f);
                    }
                    coinT = 2f;
                    break;
                }
                case DefenseKind.Contenedores:
                {
                    float fx = -0.3f;
                    foreach (float z in new[] { -1.05f, 1.05f }) Inst(a.contenedor, new Vector3(fx, 0, z), 90 + Random.Range(-7f, 7f), 1);
                    faceOff = fx + 0.75f;
                    if (level >= 2)
                        foreach (float z in new[] { -1.05f, 1.05f }) Inst(a.contenedor, new Vector3(fx - 1.45f, 0, z), 90 + Random.Range(-7f, 7f), 1);
                    if (level >= 3)
                    {
                        // gomas apiladas y chapas con franja adelante
                        var tire = new Color(0.07f, 0.07f, 0.07f);
                        foreach (float z in new[] { -1.4f, 1.4f })
                            for (int k = 0; k < 3; k++) FxKit.Prim(PrimitiveType.Cylinder, model, new Vector3(fx + 1.15f, 0.12f + k * 0.22f, z), new Vector3(0.9f, 0.1f, 0.9f), tire);
                        for (int k = 0; k < 6; k++)
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(fx + 0.82f, 1.0f, -1.9f + k * 0.76f), new Vector3(0.05f, 0.3f, 0.76f), k % 2 == 0 ? new Color(0.98f, 0.78f, 0.1f) : new Color(0.08f, 0.08f, 0.09f));
                        faceOff = fx + 1.6f;
                    }
                    break;
                }
                case DefenseKind.Infiltrado:
                {
                    // civil con buzo gris, gorra y mochila: un manifestante más
                    var p = Inst(a.people[0], new Vector3(0.3f, 0, 0), 90, PeopleScale);
                    if (p)
                    {
                        foreach (var r in p.GetComponentsInChildren<Renderer>())
                        {
                            var sm = r.sharedMaterials;
                            for (int k = 0; k < sm.Length; k++)
                            {
                                if (!sm[k]) continue;
                                if (sm[k].name.StartsWith("M_Shirt")) sm[k] = FxKit.Mat(new Color(0.46f, 0.48f, 0.52f));
                                else if (sm[k].name.StartsWith("M_Pants")) sm[k] = FxKit.Mat(new Color(0.22f, 0.3f, 0.46f));
                            }
                            r.sharedMaterials = sm;
                        }
                        var t = p.transform;
                        FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.74f, 0.04f), new Vector3(0.36f, 0.12f, 0.42f), new Color(0.12f, 0.12f, 0.14f));
                        FxKit.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.18f, -0.22f), new Vector3(0.34f, 0.4f, 0.16f), new Color(0.2f, 0.32f, 0.22f));
                        blinker = FxKit.Prim(PrimitiveType.Sphere, t, new Vector3(0.14f, 1.64f, 0.02f), Vector3.one * 0.05f, new Color(0.2f, 1f, 0.3f)).transform;
                        Fidget.Add(p, Fidget.Mode.Crowd);
                    }
                    cdBar = HpBar.Create(transform, 4.0f, 3.0f, 0.28f);
                    cdBar.fixedColor = new Color(0.75f, 0.78f, 0.85f);
                    cdBar.alwaysShow = true;
                    armT = Mathf.Max(0.5f, def.cycleOn);
                    break;
                }
                case DefenseKind.Comisario:
                {
                    var c = Inst(a.comisario, new Vector3(0.6f, 0, 0), 90, PeopleScale * LevelScale);
                    if (c) Fidget.Add(c, Fidget.Mode.Firm, 0.1f);
                    if (level >= 2) Officer(new Vector3(-0.9f, 0, -1.3f), Fidget.Mode.Shuffle);
                    if (level >= 3) Officer(new Vector3(-0.9f, 0, 1.3f), Fidget.Mode.Shuffle);
                    auraT = 1.2f;
                    break;
                }
                case DefenseKind.Concertina:
                    for (int i = 0; i < level; i++) Inst(a.concertina, new Vector3(0.9f - i * 1.3f, 0, 0), 90, 1);
                    break;
                case DefenseKind.Requisa:
                {
                    Inst(a.requisa, new Vector3(0.6f, 0, 0), 90, 1);
                    Officer(new Vector3(-0.8f, 0, -1.2f), Fidget.Mode.Shuffle);
                    Officer(new Vector3(-0.8f, 0, 1.2f), Fidget.Mode.Shuffle);
                    if (level >= 2) Officer(new Vector3(-1.8f, 0, 0), Fidget.Mode.Shuffle);
                    if (level >= 3) Officer(new Vector3(1.9f, 0, 1.8f), Fidget.Mode.Firm);
                    cd = 1.5f;
                    break;
                }
                case DefenseKind.Desvio:
                {
                    Inst(a.desvio, new Vector3(0.4f, 0, 0), 90 + 26, 1);
                    // agente de tránsito con chaleco flúor
                    var o = Officer(new Vector3(-1.2f, 0, -1.3f), Fidget.Mode.Shuffle);
                    if (o)
                    {
                        NoShield(o);
                        FxKit.Prim(PrimitiveType.Cube, o.transform, new Vector3(0, 1.22f, 0), new Vector3(0.44f, 0.5f, 0.32f), new Color(0.85f, 0.95f, 0.25f));
                        FxKit.Prim(PrimitiveType.Cube, o.transform, new Vector3(0, 1.22f, 0.165f), new Vector3(0.42f, 0.06f, 0.01f), new Color(0.9f, 0.9f, 0.9f));
                    }
                    break;
                }
                case DefenseKind.Estruendo:
                {
                    var olive = new Color(0.25f, 0.3f, 0.2f);
                    FxKit.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.55f, 0), new Vector3(0.55f, 0.45f, 0.55f), olive);
                    FxKit.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.55f, 0), new Vector3(0.58f, 0.06f, 0.58f), new Color(0.85f, 0.7f, 0.15f));
                    FxKit.Prim(PrimitiveType.Cube, model, new Vector3(0.18f, 1.05f, 0), new Vector3(0.12f, 0.3f, 0.32f), new Color(0.6f, 0.62f, 0.65f), Quaternion.Euler(0, 0, -20));
                    blinker = FxKit.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.12f, 0), Vector3.one * 0.26f, new Color(1f, 0.15f, 0.1f)).transform;
                    fuse = Mathf.Max(0.3f, def.cycleOn);
                    break;
                }
                case DefenseKind.Drone:
                {
                    drone = new GameObject("DroneRig").transform;
                    drone.SetParent(model, false);
                    drone.localPosition = Vector3.up * 6.5f;
                    var g = Inst(a.drone, Vector3.zero, 90, 1.2f * (1 + 0.12f * (level - 1)));
                    if (g)
                    {
                        droneBody = g.transform;
                        droneBody.SetParent(drone, false);
                        droneBody.localPosition = Vector3.zero;
                        foreach (var sx in new[] { -1f, 1f })
                            foreach (var sz in new[] { -1f, 1f })
                            {
                                var rr = FxKit.Prim(PrimitiveType.Cylinder, droneBody, new Vector3(sx * 0.81f, 0.36f, sz * 0.81f), new Vector3(0.95f, 0.012f, 0.18f), new Color(0.15f, 0.16f, 0.18f)).transform;
                                rotors.Add(rr);
                            }
                    }
                    // sombra de contacto: un disco oscuro en el piso para leer la altura
                    FxKit.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.03f, 0), new Vector3(1.6f, 0.01f, 1.6f), new Color(0.35f, 0.3f, 0.28f));
                    break;
                }
                case DefenseKind.Tanqueta:
                {
                    var tq = Inst(a.tanqueta, Vector3.zero, 90, 1f);
                    Sirens(tq);
                    var tt = Inst(a.tanquetaTurret, new Vector3(0, 2.82f, 0), 90, 1f + 0.15f * (level - 1));
                    turret = tt ? tt.transform : null;
                    if (level >= 2)
                        foreach (float z in new[] { -1.52f, 1.52f })
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(0, 1.0f, z), new Vector3(6.4f, 0.12f, 0.06f), new Color(0.95f, 0.75f, 0.12f));
                    cd = Mathf.Max(cd, 3f);
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ comportamiento

        void UpdateExtra(float dt)
        {
            switch (def.defense)
            {
                case DefenseKind.Puntero: Broker(dt); break;
                case DefenseKind.Infiltrado: Undercover(dt); break;
                case DefenseKind.Comisario: Commissioner(dt); break;
                case DefenseKind.Concertina: Wire(dt); break;
                case DefenseKind.Requisa: Search(); break;
                case DefenseKind.Desvio: Divert(); break;
                case DefenseKind.Estruendo: Fuse(dt); break;
                case DefenseKind.Drone: Fly(dt); break;
                case DefenseKind.Tanqueta: Foam(dt); Siren(); break;
            }
        }

        // Puntero: plata por segundo durante la oleada; cada tanto suelta monedas (feedback visual).
        void Broker(float dt)
        {
            var gs = GameState.I;
            if (!gs || gs.phase != Phase.Oleada) return;
            gs.money += def.moneyRate * CardDef.LevelRate(level) * dt;
            coinT -= dt;
            if (coinT > 0) return;
            coinT = 3.5f;
            var fx = new GameObject("PunteroCoins");
            fx.transform.position = transform.position + Vector3.up * 3.2f;
            var ps = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.fire, new Color(1f, 0.82f, 0.25f, 0.95f), 3.5f, 0.9f, 0.25f, 0.45f, 0, 25, 0.9f, 0.2f, 0.6f);
            var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 8) });
            Destroy(fx, 2f);
            Snd.At("coin", transform.position, 0.35f, 1f, 0.1f, 1.2f);
        }

        // Infiltrado: se arma; armado, el primero que lo pisa queda detenido (a una columna le pega) y los de alrededor, aturdidos.
        void Undercover(float dt)
        {
            if (!armed)
            {
                armT -= dt;
                if (cdBar) cdBar.Set(Mathf.Max(0.001f, 1 - armT / Mathf.Max(0.5f, def.cycleOn)));
                if (armT > 0) return;
                armed = true;
                if (cdBar) { Destroy(cdBar.gameObject); cdBar = null; }
                Snd.At("cuffs", transform.position, 0.4f, 0.2f);
            }
            if (blinker) blinker.localScale = Vector3.one * (Mathf.Repeat(Time.time * 1.5f, 1f) < 0.15f ? 0.09f : 0.04f);
            Enemy t = null;
            foreach (var e in Enemy.All)
            {
                if (e.Dead || !e.onLane || e.lane != lane || e.Jumping) continue;
                if (Mathf.Abs(e.x - x) < 1.6f) { t = e; break; }
            }
            if (!t) return;
            float r2 = def.stats.area * def.stats.area;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e.Dead || e == t) continue;
                var dp = e.transform.position - t.transform.position;
                if (dp.x * dp.x + dp.z * dp.z < r2) e.Stun(def.stun);
            }
            if (t.def.members > 1) { t.TakeDamage(def.stats.damage); t.Stun(def.stun); }
            else t.Capture();
            Snd.At("cuffs", t.transform.position, 1, 0.1f);
            Snd.At("whistle", transform.position, 0.8f, 0.3f);
            Burst(new Color(0.6f, 0.75f, 1f, 0.9f), 30, 5f);
            Headlines.Say("infiltrado", 12f);
            Consume();
        }

        // Comisario: cada tanto arenga por megáfono y las defensas que refuerza destellan en dorado.
        void Commissioner(float dt)
        {
            auraT -= dt;
            if (auraT > 0) return;
            auraT = 4.5f;
            float r = def.stats.range + HalfLen;
            int n = 0;
            foreach (var d in All)
            {
                if (d == this || d.Dead) continue;
                var dp = d.transform.position - transform.position;
                if (dp.x * dp.x + dp.z * dp.z > r * r) continue;
                d.Burst(new Color(1f, 0.82f, 0.3f, 0.9f), 10, 2.2f);
                n++;
            }
            if (n > 0) { Snd.At("whistle", transform.position, 0.3f, 2f, 0.05f, 0.85f); punch = 1; }
        }

        // Concertina: daño continuo y freno leve a los que la cruzan.
        void Wire(float dt)
        {
            auraT -= dt;
            if (auraT > 0) return;
            auraT = 0.25f;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e.Dead || !e.onLane || e.lane != lane || e.Jumping) continue;
                if (Mathf.Abs(e.x - x) > HalfLen) continue;
                e.TakeDamage(def.stats.damage * Mul * 0.25f);
                e.Slow(def.slow, 0.5f);
            }
        }

        // Requisa: desarma al especial más caro a alcance (encapuchado, piquetero o mortero → manifestante común).
        void Search()
        {
            if (cd > 0) return;
            comun ??= Resources.Load<UnitDef>("Units/comun");
            if (!comun) return;
            Enemy t = null;
            float r = def.stats.range + HalfLen;
            foreach (var e in Enemy.All)
            {
                if (e.Dead || !e.onLane || !e.def.special || e.def.members > 1) continue;
                var dp = e.transform.position - transform.position;
                if (dp.x * dp.x + dp.z * dp.z > r * r) continue;
                if (!t || e.Cost > t.Cost) t = e;
            }
            if (!t) return;
            cd = 1f / Mathf.Max(0.02f, def.stats.rate * Rate);
            if (!t.Disarm(comun)) return;
            Snd.At("stamp", t.transform.position, 0.7f, 0.2f);
            Snd.At("whistle_short", transform.position, 0.5f, 0.4f);
            Headlines.Say("requisa", 14f);
            punch = 1;
        }

        // Desvío: a los que llegan (de a uno; las columnas no se desvían) los manda al carril vecino mejor defendido.
        void Divert()
        {
            if (nearLanes == null || nearLanes.Count < 2) return;
            foreach (var e in Enemy.All)
            {
                if (e.Dead || !e.onLane || e.lane != lane || e.Jumping || e.def.members > 1 || diverted.Contains(e)) continue;
                float gap = e.x - EastFace;
                if (gap < -0.8f || gap > 1.4f) continue;
                int best = -1;
                float bestHp = -1;
                foreach (int l in nearLanes)
                {
                    if (l == lane) continue;
                    float hp = Random.value; // desempate
                    foreach (var d in All) if (!d.Dead && d.lane == l && d.Blocks && d.x < e.x) hp += d.hp;
                    if (hp > bestHp) { bestHp = hp; best = l; }
                }
                if (best < 0) return;
                diverted.Add(e);
                e.ShiftLane(best);
                Snd.At("whistle_short", transform.position, 0.6f, 0.35f);
                Headlines.Say("desvio", 20f);
                punch = 1;
            }
        }

        // Estruendo: mecha corta (titila cada vez más rápido) y explota.
        void Fuse(float dt)
        {
            fuse -= dt;
            if (blinker) blinker.localScale = Vector3.one * (Mathf.Repeat(Time.time * (3f + 6f / Mathf.Max(0.2f, fuse)), 1f) < 0.5f ? 0.34f : 0.18f);
            if (fuse > 0) return;
            float r2 = def.stats.area * def.stats.area;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e.Dead) continue;
                var dp = e.transform.position - transform.position;
                if (dp.x * dp.x + dp.z * dp.z > r2) continue;
                e.TakeDamage(def.stats.damage * Mul);
                e.Stun(def.stun);
            }
            var fx = new GameObject("Estruendo");
            fx.transform.position = transform.position + Vector3.up * 0.8f;
            var flash = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.fire, new Color(1f, 0.97f, 0.85f, 1f), 9f, 0.35f, 1.4f, 2.6f, 0, 85, 0, 0.3f, 2.5f);
            var em = flash.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 40) });
            var smoke = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke, new Color(0.85f, 0.85f, 0.82f, 0.7f), 3.5f, 1.6f, 1.2f, 2.4f, 0, 80, -0.05f, 0.6f, 2.2f);
            var em2 = smoke.emission; em2.SetBursts(new[] { new ParticleSystem.Burst(0, 22) });
            Destroy(fx, 3f);
            Snd.At("explosion", transform.position, 1, 0.1f, 0.05f, 1.25f);
            Headlines.Say("estruendo", 10f);
            Consume();
        }

        // Drone: flota sobre su slot y le tira a cualquier carril; primero a los especiales, después al más cercano.
        void Fly(float dt)
        {
            if (!drone) return;
            float t = Time.time;
            drone.localPosition = Vector3.up * (6.5f + Mathf.Sin(t * 1.7f + x) * 0.3f);
            foreach (var r in rotors) if (r) r.Rotate(0, 1500 * dt, 0, Space.Self);
            if (droneBody)
            {
                var want = Quaternion.Euler(0, 90, 0);
                if (aimPoint != Vector3.zero)
                {
                    var to = drone.InverseTransformPoint(aimPoint); to.y = 0;
                    want = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(Mathf.Clamp(to.z, -1, 1) * 8f, 0, Mathf.Clamp(-to.x, -1, 1) * 8f);
                }
                droneBody.localRotation = Quaternion.Slerp(droneBody.localRotation, want, dt * 3f);
            }
            if (cd > 0) return;
            Enemy best = null;
            float bs = float.MaxValue, r2 = (Range + HalfLen) * (Range + HalfLen);
            var p = transform.position;
            foreach (var e in Enemy.All)
            {
                if (e.Dead || !e.onLane) continue;
                var dp = e.transform.position - p;
                float d2 = dp.x * dp.x + dp.z * dp.z;
                if (d2 > r2) continue;
                float score = (e.def.special ? 0 : 1e5f) + d2;
                if (score < bs) { bs = score; best = e; }
            }
            if (!best) { aimPoint = Vector3.zero; return; }
            aimPoint = best.transform.position;
            cd = 1f / Mathf.Max(0.05f, def.stats.rate * Rate);
            var from = droneBody ? droneBody.position + Vector3.down * 0.7f : p + Vector3.up * 6f;
            RubberShot.Fire(from, best, def.stats.damage * Mul);
            Snd.At("shotgun", from, 0.55f, 0.05f, 0.1f, 1.6f);
        }

        // Tanqueta: cada cycleOff s dispara una bola de espuma al grupo más denso de la Plaza (moja, frena y daña en área).
        void Foam(float dt)
        {
            if (turret && aimPoint != Vector3.zero)
            {
                var flat = aimPoint - turret.position; flat.y = 0;
                if (flat.sqrMagnitude > 0.1f) turret.rotation = Quaternion.RotateTowards(turret.rotation, Quaternion.LookRotation(flat), 90 * dt);
            }
            if (cd > 0) return;
            Enemy t = null;
            int bestN = 0;
            float a2 = def.stats.area * def.stats.area;
            foreach (var e in Enemy.All)
            {
                if (e.Dead || !e.onLane) continue;
                int n = 0;
                foreach (var o in Enemy.All)
                {
                    if (o.Dead || !o.onLane) continue;
                    var dp = o.transform.position - e.transform.position;
                    if (dp.x * dp.x + dp.z * dp.z < a2) n += Mathf.Max(1, o.def.members);
                }
                if (n > bestN) { bestN = n; t = e; }
            }
            if (!t) return;
            aimPoint = t.transform.position;
            if (turret)
            {
                var flat = aimPoint - turret.position; flat.y = 0;
                if (Vector3.Angle(turret.forward, flat) > 12) { cd = 0.1f; return; } // termina de apuntar
            }
            cd = Mathf.Max(2f, def.cycleOff) / Rate;
            var from = turret ? turret.TransformPoint(new Vector3(0, 0.55f, 1.8f)) : transform.position + Vector3.up * 3.5f;
            FoamShell.Fire(from, aimPoint, def.stats.area, def.stats.damage * Mul, def.slow, def.slowTime);
            Snd.At("mortar_launch", from, 1, 0.1f, 0.05f, 0.7f);
            punch = 1;
            Headlines.Say("tanqueta", 25f);
        }

        // Defensas de un solo uso: desaparecen y liberan el slot sin titular de "cae la línea".
        void Consume()
        {
            if (Dead) return;
            Dead = true;
            All.Remove(this);
            blocking.Clear();
            StartCoroutine(Fall());
        }
    }

    // Bola de espuma de la tanqueta: arco largo; al caer moja, frena, empuja un poco y daña en área.
    public class FoamShell : MonoBehaviour
    {
        Vector3 a, b;
        float t, dmg, area, slow, slowTime, dur;

        public static void Fire(Vector3 from, Vector3 to, float area, float dmg, float slow, float slowTime)
        {
            var go = FxKit.Prim(PrimitiveType.Sphere, null, from, Vector3.one * 0.9f, new Color(0.95f, 0.97f, 1f));
            var s = go.AddComponent<FoamShell>();
            s.a = from; s.b = to + Vector3.up * 0.3f; s.dmg = dmg; s.area = Mathf.Max(1, area); s.slow = slow; s.slowTime = slowTime;
            float dist = Vector3.Distance(new Vector3(from.x, 0, from.z), new Vector3(to.x, 0, to.z));
            s.dur = 0.8f + dist / 40f;
        }

        void Update()
        {
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (6f + Vector3.Distance(a, b) * 0.15f);
            transform.localScale = Vector3.one * (0.9f + Mathf.Sin(Time.time * 20f) * 0.08f);
            if (t < 1) return;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e.Dead) continue;
                var dp = e.transform.position - b;
                if (dp.x * dp.x + dp.z * dp.z > area * area) continue;
                e.TakeDamage(dmg);
                e.Wet(slowTime);
                e.Slow(slow, slowTime);
                e.Push(1.2f);
            }
            Snd.At("gas_hiss", b, 1, 0.1f, 0.05f, 0.8f);
            var fx = new GameObject("Espuma");
            fx.transform.position = b;
            var ps = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke, new Color(0.97f, 0.98f, 1f, 0.9f), 2.5f, 2.6f, 0.8f, 1.6f, 0, 85, 0.05f, area * 0.5f, 1.3f);
            var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, 45) });
            Destroy(fx, 3.5f);
            Destroy(gameObject);
        }
    }
}
