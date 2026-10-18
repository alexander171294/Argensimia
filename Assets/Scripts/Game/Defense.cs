using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Elemento policial colocado en un slot del carril. Todos frenan al 100% a los manifestantes de su carril
    // (solo el encapuchado tiene chance de saltar un retén) y se rompen al quedar sin vida, liberando el slot.
    public partial class Defense : MonoBehaviour
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
        // Frena a los que llegan (los que no, dejan pasar: concertina, infiltrado, desvío, estruendo y drone).
        public bool Blocks => def.defense switch
        {
            DefenseKind.Concertina or DefenseKind.Infiltrado or DefenseKind.Desvio or DefenseKind.Estruendo or DefenseKind.Drone => false,
            _ => true,
        };
        // Puede recibir tiros del mortero (el drone y el desvío sí, aunque no frenen).
        public bool Targetable => Blocks || def.defense == DefenseKind.Drone || def.defense == DefenseKind.Desvio;

        void BuildMotos()
        {
            moto = new GameObject("Motos").transform;
            moto.SetParent(model, false);
            for (int i = 0, k = level + 1; i < k; i++) MotoRun.Moto(moto, new Vector3(-(i % 2) * 2.6f + 0.6f, 0, (i - (k - 1) * 0.5f) * 1.6f));
        }
        HpBar bar;
        Transform model;

        float Mul => CardDef.LevelMul(level) * (1 + BuffFor(this)); // el comisario suma daño a las cercanas
        float Rate => CardDef.LevelRate(level);
        float Range => def.stats.range + def.LevelRange(level);
        static readonly List<Enemy> hits = new List<Enemy>();

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
            d.baseHp = def.stats.hp * CardDef.LevelHp(level);
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

        float BarHeight => def.defense == DefenseKind.Hidrante || def.defense == DefenseKind.Tanqueta ? 6.4f : def.defense == DefenseKind.Drone ? 9.2f : 4.6f;
        // Agentes un poco más grandes en cada nivel (se lee la mejora de un vistazo).
        float LevelScale => 1f + 0.05f * (level - 1);

        void Build()
        {
            BuildModel();
            if (def.defense == DefenseKind.Motorizada) motoWait = 1f;
            bar = HpBar.Create(transform, BarHeight, 3.6f, 0.55f);
            if (def.defense == DefenseKind.Motorizada)
            {
                cdBar = HpBar.Create(transform, 4.0f, 3.6f, 0.28f);
                cdBar.fixedColor = new Color(0.45f, 0.75f, 1f);
                cdBar.alwaysShow = true;
            }
            BuildBadge();
            StartCoroutine(PopIn());
            PlaceSound();
        }

        void PlaceSound()
        {
            var p = transform.position;
            Snd.At("place", p, 1, 0.05f, 0.04f); // "toc" de pieza apoyada
        }

        // Modelo según el nivel: cada nivel es más robusto (más agentes, doble vallado, blindaje).
        void BuildModel()
        {
            var a = GameAssets.I;
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            if (def.placePrefab) { Instantiate(def.placePrefab, model); return; }
            switch (def.defense)
            {
                case DefenseKind.Vallado:
                {
                    // NV1: una línea de vallas en L al fondo (oeste) del slot.
                    // NV2: doble línea + bolsas de arena adelante + reja alta antidisturbios atrás.
                    // NV3: + muro de hormigón tipo New Jersey con franjas, chapas policiales y balizas.
                    float fx = -(HalfLen - 1.35f);
                    var rows = new List<float> { fx };
                    if (level >= 2) rows.Add(fx - 1.0f);
                    foreach (float rx in rows)
                        foreach (float z in new[] { -1.2f, 1.2f })
                        {
                            var v = Inst(a.valla, new Vector3(rx, 0, z), 90, 1f);
                            if (!v) continue;
                            vallas.Add(v.transform); vallaPos.Add(v.transform.localPosition); vallaRot.Add(v.transform.localRotation);
                        }
                    faceOff = fx + 1.0f;
                    if (level >= 2)
                    {
                        // reja alta: postes, travesaños y barrotes
                        var steel = new Color(0.16f, 0.17f, 0.2f);
                        float gx = fx - 1.3f;
                        foreach (float z in new[] { -1.95f, -0.65f, 0.65f, 1.95f })
                            FxKit.Prim(PrimitiveType.Cylinder, model, new Vector3(gx, 1.15f, z), new Vector3(0.11f, 1.15f, 0.11f), steel);
                        foreach (float y in new[] { 0.25f, 1.25f, 2.22f })
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(gx, y, 0), new Vector3(0.07f, 0.07f, 3.95f), steel);
                        for (int k = 0; k < 20; k++)
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(gx, 1.2f, -1.85f + k * 0.195f), new Vector3(0.035f, 2.0f, 0.035f), steel);
                        // bolsas de arena adelante (dos hileras)
                        var sand = new Color(0.72f, 0.64f, 0.45f);
                        var sand2 = new Color(0.64f, 0.56f, 0.38f);
                        float sx = fx + 0.75f;
                        for (int k = 0; k < 7; k++)
                            FxKit.Prim(PrimitiveType.Sphere, model, new Vector3(sx, 0.17f, -1.8f + k * 0.6f), new Vector3(0.5f, 0.3f, 0.66f), k % 2 == 0 ? sand : sand2);
                        for (int k = 0; k < 6; k++)
                            FxKit.Prim(PrimitiveType.Sphere, model, new Vector3(sx - 0.04f, 0.42f, -1.5f + k * 0.6f), new Vector3(0.46f, 0.28f, 0.62f), k % 2 == 0 ? sand2 : sand);
                        faceOff = sx + 0.26f;
                    }
                    if (level >= 3)
                    {
                        var conc = new Color(0.7f, 0.68f, 0.64f);
                        var concD = new Color(0.58f, 0.56f, 0.53f);
                        var yel = new Color(0.98f, 0.78f, 0.1f);
                        var blk = new Color(0.08f, 0.08f, 0.09f);
                        float bx = fx + 1.55f;
                        foreach (float z in new[] { -1.05f, 1.05f })
                        {
                            // New Jersey: base ancha + cuerpo + tapa
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(bx, 0.2f, z), new Vector3(0.9f, 0.4f, 2.0f), concD);
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(bx, 0.68f, z), new Vector3(0.5f, 0.6f, 2.0f), conc);
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(bx, 1.02f, z), new Vector3(0.42f, 0.1f, 1.96f), concD);
                            // franjas amarillas y negras sobre la cara este
                            for (int k = 0; k < 6; k++)
                                FxKit.Prim(PrimitiveType.Cube, model, new Vector3(bx + 0.26f, 0.68f, z - 0.83f + k * 0.333f), new Vector3(0.03f, 0.22f, 0.32f), k % 2 == 0 ? yel : blk);
                        }
                        // chapas azules con franja blanca arriba de la reja
                        var navy = new Color(0.1f, 0.2f, 0.48f);
                        float gx = fx - 1.3f;
                        foreach (float z in new[] { -1.3f, 0f, 1.3f })
                        {
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(gx + 0.06f, 1.55f, z), new Vector3(0.05f, 0.9f, 1.2f), navy);
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(gx + 0.09f, 1.55f, z), new Vector3(0.02f, 0.14f, 1.2f), Color.white);
                        }
                        // balizas en las puntas del muro
                        FxKit.Prim(PrimitiveType.Sphere, model, new Vector3(bx, 1.17f, -2.0f), Vector3.one * 0.2f, new Color(1f, 0.15f, 0.1f));
                        FxKit.Prim(PrimitiveType.Sphere, model, new Vector3(bx, 1.17f, 2.0f), Vector3.one * 0.2f, new Color(0.2f, 0.4f, 1f));
                        faceOff = bx + 0.45f;
                    }
                    break;
                }
                case DefenseKind.Infanteria:
                case DefenseKind.BalasGoma:
                {
                    // misma formación; balas de goma sin escudos y con escopetas. La fila de adelante se mantiene firme.
                    bool gun = def.defense == DefenseKind.BalasGoma;
                    System.Func<Vector3, Fidget.Mode, GameObject> mk = gun ? (System.Func<Vector3, Fidget.Mode, GameObject>)GunOfficer : (lp, m) => Officer(lp, m);
                    for (int r = 0; r < 2; r++)
                        for (int i = 0; i < 3; i++) mk(new Vector3(1.6f - r * 1.5f, 0, (i - 1) * 1.35f + r * 0.45f), r == 0 ? Fidget.Mode.Firm : Fidget.Mode.Shuffle);
                    if (level >= 2) for (int i = 0; i < 2; i++) mk(new Vector3(-1.5f, 0, (i - 0.5f) * 1.4f), Fidget.Mode.Shuffle);
                    if (level >= 3) for (int i = 0; i < 2; i++) mk(new Vector3(-1.5f, 0, (i == 0 ? -1 : 1) * 2.05f), Fidget.Mode.Shuffle);
                    break;
                }
                case DefenseKind.Lanzagases:
                {
                    // NV1 un agente, NV2 dos, NV3 tres + cajones de granadas
                    var olive = new Color(0.2f, 0.25f, 0.18f);
                    for (int k = 0; k < level; k++)
                    {
                        float z = (k - (level - 1) * 0.5f) * 1.3f;
                        var o = Officer(new Vector3(0.6f - (k % 2) * 0.5f, 0, z), Fidget.Mode.Shuffle, 0.22f);
                        if (!o) continue;
                        FxKit.Prim(PrimitiveType.Cylinder, o.transform, new Vector3(0.16f, 1.25f, 0.3f), new Vector3(0.16f, 0.36f, 0.16f), olive, Quaternion.Euler(60, 0, 0));
                        FxKit.Prim(PrimitiveType.Cylinder, o.transform, new Vector3(0.16f, 1.1f, 0.12f), new Vector3(0.26f, 0.09f, 0.26f), new Color(0.12f, 0.13f, 0.12f), Quaternion.Euler(60, 0, 0)); // tambor
                        shooters.Add(o.GetComponent<Fidget>());
                    }
                    if (level >= 3)
                        foreach (float z in new[] { -0.55f, 0.55f })
                        {
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(-1.3f, 0.28f, z), new Vector3(0.8f, 0.56f, 0.6f), olive);
                            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(-1.3f, 0.57f, z), new Vector3(0.84f, 0.04f, 0.64f), new Color(0.75f, 0.6f, 0.15f));
                        }
                    break;
                }
                case DefenseKind.Detencion:
                {
                    int n = 2 + level; // 3 / 4 / 5 agentes
                    for (int i = 0; i < n; i++) Officer(new Vector3(1.1f - (i % 2) * 0.7f, 0, (i - (n - 1) * 0.5f) * 1.1f), Fidget.Mode.Shuffle, 0.25f);
                    break;
                }
                case DefenseKind.Motorizada:
                    BuildMotos();
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
                    if (level >= 3) turret.localScale = Vector3.one * 1.3f;
                    FxKit.Prim(PrimitiveType.Cylinder, turret, new Vector3(0, -0.1f, 0), new Vector3(0.6f, 0.12f, 0.6f), new Color(0.12f, 0.13f, 0.15f));
                    FxKit.Prim(PrimitiveType.Cylinder, turret, new Vector3(0, 0, 0.55f), new Vector3(0.2f, 0.55f, 0.2f), new Color(0.22f, 0.24f, 0.27f), Quaternion.Euler(90, 0, 0));
                    jet = FxKit.Particles(turret, new Vector3(0, 0, 1.1f), Quaternion.identity, a.water,
                        new Color(0.85f, 0.93f, 1f, 0.8f), WaterSpeed, 1.4f, 0.35f, 0.9f, 220, 3f, WaterGravity, 0.1f, 2.4f, 3000);
                    var em = jet.emission; em.enabled = false;
                    if (level >= 2) Armor(truck);
                    break;
                }
                default:
                    BuildExtra();
                    break;
            }
        }

        // Hidrante NV2: chapas laterales con franja y paragolpes reforzado. NV3: + jaula en el techo y cañón más grande.
        void Armor(GameObject truck)
        {
            if (!truck) return;
            var mf = truck.GetComponentInChildren<MeshFilter>();
            if (!mf || !mf.sharedMesh) return;
            var mb = mf.sharedMesh.bounds;
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var q = model.InverseTransformPoint(mf.transform.TransformPoint(c));
                min = Vector3.Min(min, q); max = Vector3.Max(max, q);
            }
            var steel = new Color(0.22f, 0.24f, 0.27f);
            var plate = new Color(0.46f, 0.49f, 0.54f);
            var hazard = new Color(0.95f, 0.75f, 0.12f);
            float len = max.x - min.x, zc = (min.z + max.z) * 0.5f, zw = max.z - min.z;
            float cx = (min.x + max.x) * 0.5f - len * 0.08f;
            foreach (float sz in new[] { -1f, 1f })
            {
                float z = sz > 0 ? max.z + 0.07f : min.z - 0.07f;
                FxKit.Prim(PrimitiveType.Cube, model, new Vector3(cx, min.y + 1.75f, z), new Vector3(len * 0.62f, 1.3f, 0.12f), plate);
                FxKit.Prim(PrimitiveType.Cube, model, new Vector3(cx, min.y + 2.42f, z + sz * 0.02f), new Vector3(len * 0.62f, 0.16f, 0.12f), hazard);
                for (int k = 0; k < 5; k++) FxKit.Prim(PrimitiveType.Cube, model, new Vector3(cx - len * 0.28f + k * len * 0.14f, min.y + 1.75f, z + sz * 0.05f), new Vector3(0.08f, 1.2f, 0.06f), steel); // remaches/costillas
            }
            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(max.x + 0.16f, min.y + 0.75f, zc), new Vector3(0.24f, 0.7f, zw * 0.92f), steel);
            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(max.x + 0.3f, min.y + 0.75f, zc), new Vector3(0.06f, 0.14f, zw * 0.9f), hazard);
            if (level < 3) return;
            float top = max.y, rx = (min.x + max.x) * 0.5f - len * 0.18f, rl = len * 0.45f;
            foreach (float sz in new[] { -1f, 1f })
            {
                float z = zc + sz * zw * 0.42f;
                FxKit.Prim(PrimitiveType.Cube, model, new Vector3(rx, top + 0.42f, z), new Vector3(rl, 0.09f, 0.09f), steel);
                for (int k = 0; k < 4; k++)
                    FxKit.Prim(PrimitiveType.Cube, model, new Vector3(rx - rl * 0.5f + k * rl / 3f, top + 0.21f, z), new Vector3(0.09f, 0.42f, 0.09f), steel);
            }
            FxKit.Prim(PrimitiveType.Cube, model, new Vector3(rx - rl * 0.5f, top + 0.42f, zc), new Vector3(0.09f, 0.09f, zw * 0.84f), steel);
        }

        // Rombos de nivel sobre la defensa (plateados en NV2, dorados en NV3), siempre mirando a cámara.
        Transform badge;
        void BuildBadge()
        {
            if (badge) Destroy(badge.gameObject);
            badge = null;
            if (level < 2) return;
            badge = new GameObject("Nivel").transform;
            badge.SetParent(transform, false);
            badge.localPosition = Vector3.up * (BarHeight + 0.85f);
            var col = level >= 3 ? new Color(1f, 0.78f, 0.18f) : new Color(0.82f, 0.86f, 0.95f);
            var rim = level >= 3 ? new Color(0.45f, 0.28f, 0.04f) : new Color(0.2f, 0.24f, 0.32f);
            for (int i = 0; i < level; i++)
            {
                var c = new Vector3((i - (level - 1) * 0.5f) * 0.78f, 0, 0);
                FxKit.Prim(PrimitiveType.Cube, badge, c + Vector3.forward * 0.04f, new Vector3(0.56f, 0.56f, 0.06f), rim, Quaternion.Euler(0, 0, 45));
                FxKit.Prim(PrimitiveType.Cube, badge, c, new Vector3(0.42f, 0.42f, 0.1f), col, Quaternion.Euler(0, 0, 45));
            }
        }

        // ------------------------------------------------------------------ mejora (soltar una carta igual encima)

        public bool CanUpgrade => !Dead && level < 3 && def.Upgradable;

        public static bool AnyUpgradable(CardDef def)
        {
            foreach (var d in All) if (d.def == def && d.CanUpgrade) return true;
            return false;
        }

        // Defensa mejorable con esta carta bajo el punto p (x, z), con r metros de tolerancia.
        public static Defense UpgradableAt(CardDef def, Vector3 p, float r)
        {
            Defense best = null;
            float bd = float.MaxValue;
            foreach (var d in All)
            {
                if (d.def != def || !d.CanUpgrade) continue;
                var q = d.transform.position;
                float dist = new Vector2(p.x - q.x, p.z - q.z).magnitude;
                if (dist > d.HalfLen + r || dist >= bd) continue;
                bd = dist; best = d;
            }
            return best;
        }

        public void Upgrade()
        {
            if (!CanUpgrade) return;
            float oldMax = maxHp;
            level++;
            baseHp = def.stats.hp * CardDef.LevelHp(level);
            maxHp = baseHp * (GameState.I ? GameState.I.DefenseHpMul : 1);
            hp = Mathf.Min(maxHp, hp + Mathf.Max(0, maxHp - oldMax));
            vallas.Clear(); vallaPos.Clear(); vallaRot.Clear(); shooters.Clear();
            turret = null; jet = null; sirenRed = sirenBlue = null; moto = null;
            if (model) Destroy(model.gameObject);
            BuildModel();
            BuildBadge();
            SetHint(false);
            bar.Set(hp / maxHp);
            StartCoroutine(PopIn());
            Burst(new Color(1f, 0.8f, 0.3f, 0.95f), 46, 7f);
            Snd.Ui("upgrade");
            if (level >= 3) Achievements.Unlock("dorado");
        }

        // Curación del cierre de mandato (o de cartas): suma frac de la vida máxima, sin pasarse. Devuelve cuántas curó.
        public static int HealAll(float frac)
        {
            int n = 0;
            foreach (var d in All.ToArray())
            {
                if (d.Dead || d.hp >= d.maxHp - 0.01f) continue;
                d.hp = Mathf.Min(d.maxHp, d.hp + d.maxHp * frac);
                d.bar.Set(d.hp / d.maxHp);
                d.Burst(new Color(0.45f, 1f, 0.45f, 0.85f), 22, 3.5f);
                n++;
            }
            if (n > 0) Snd.Ui("heal", 1, 1, 0.5f);
            return n;
        }

        void Burst(Color c, int count, float speed)
        {
            var fx = new GameObject("DefenseFx");
            fx.transform.position = transform.position + Vector3.up * 0.4f;
            var ps = FxKit.Particles(fx.transform, Vector3.zero, Quaternion.Euler(-90, 0, 0), GameAssets.I.fire, c, speed, 0.9f, 0.35f, 0.75f, 0, 40, 0.5f, HalfLen * 0.6f, 0.3f);
            var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0, count) });
            Destroy(fx, 2f);
        }

        // ------------------------------------------------------------------ marca de 'soltá acá' mientras se arrastra una carta igual

        GameObject hint;
        Transform hintDisc, hintArrow;
        readonly List<Material> hintMats = new List<Material>();
        bool hintHover;
        public static bool AnyHint { get { foreach (var d in All) if (d.hint) return true; return false; } }

        public static void ShowHints(CardDef def, bool on)
        {
            foreach (var d in All) d.SetHint(on && def && d.def == def && d.CanUpgrade);
        }

        public static void HoverHint(Defense h)
        {
            foreach (var d in All) d.hintHover = d == h;
        }

        void SetHint(bool on)
        {
            if (on == (hint != null)) return;
            if (!on)
            {
                Destroy(hint); hint = null; hintHover = false;
                foreach (var m in hintMats) if (m) Destroy(m);
                hintMats.Clear();
                return;
            }
            hint = new GameObject("UpgradeHint");
            hint.transform.SetParent(transform, false);
            var gold = new Color(1f, 0.8f, 0.25f);
            hintDisc = FxKit.Prim(PrimitiveType.Cylinder, hint.transform, new Vector3(0, 0.03f, 0), new Vector3(HalfLen * 2.1f, 0.015f, 4.4f), gold).transform;
            hintArrow = new GameObject("Arrow").transform;
            hintArrow.SetParent(hint.transform, false);
            FxKit.Prim(PrimitiveType.Cube, hintArrow, new Vector3(0, 0.75f, 0), new Vector3(0.34f, 1.0f, 0.1f), gold);
            FxKit.Prim(PrimitiveType.Cube, hintArrow, new Vector3(0, 0.2f, 0), new Vector3(0.62f, 0.62f, 0.1f), gold, Quaternion.Euler(0, 0, 45));
            // material propio con emisión para que titile
            foreach (var r in hint.GetComponentsInChildren<Renderer>())
            {
                var m = r.material;
                m.EnableKeyword("_EMISSION");
                hintMats.Add(m);
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (badge)
            {
                if (cam) badge.rotation = cam.transform.rotation;
                badge.localPosition = Vector3.up * (BarHeight + 0.85f + Mathf.Sin(Time.time * 2.2f + x) * 0.08f);
            }
            if (hint)
            {
                float w = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                float k = hintHover ? 1.12f + w * 0.06f : 0.95f + w * 0.05f;
                hintDisc.localScale = new Vector3(HalfLen * 2.1f * k, 0.015f, 4.4f * k);
                hintArrow.localPosition = Vector3.up * (BarHeight + 1.9f + w * 0.5f);
                if (cam) hintArrow.rotation = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0);
                hintArrow.localScale = Vector3.one * (hintHover ? 1.35f : 1f);
                // titila: destello dorado fuerte ↔ apagado, ~2,5 veces por segundo
                float blink = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 16f), 2f);
                var em = new Color(1f, 0.75f, 0.2f) * Mathf.Lerp(0.05f, hintHover ? 3.2f : 2.4f, blink);
                foreach (var m in hintMats) if (m) m.SetColor("_EmissionColor", em);
            }
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
            var g = Inst(GameAssets.I.officer, lp, 90 + Random.Range(-6f, 6f), PeopleScale * LevelScale);
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
                if (d.Dead || d.lane != e.lane || !d.Blocks) continue;
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
            if (def.defense != DefenseKind.Vallado && def.defense != DefenseKind.Drone) model.localPosition = Vector3.right * punch * 0.15f;
            switch (def.defense)
            {
                case DefenseKind.Vallado: Shake(dt); break;
                case DefenseKind.Infanteria: Melee(); break;
                case DefenseKind.BalasGoma: Shoot(); break;
                case DefenseKind.Lanzagases: Gas(); break;
                case DefenseKind.Detencion: Detain(); break;
                case DefenseKind.Hidrante: Water(dt); Siren(); break;
                case DefenseKind.Motorizada: Moto(dt); break;
                default: UpdateExtra(dt); break;
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
            // pega a 1 / 2 / 3 manifestantes por golpe según el nivel
            hits.Clear();
            foreach (var e in blocking) if (e && !e.Dead && hits.Count < level) hits.Add(e);
            Enemy t = hits.Count > 0 ? hits[0] : null;
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
            if (hits.Count == 0) hits.Add(t);
            cd = 1f / Mathf.Max(0.05f, def.stats.rate * Rate);
            Snd.At("baton", t.transform.position, 1, 0.07f, 0.12f);
            foreach (var e in hits.ToArray()) e.TakeDamage(def.stats.damage * Mul);
            punch = 1;
        }

        // Balas de goma: en su carril, prioriza a los que golpean un vallado (o a otra defensa) adelante; si no, al más cercano.
        void Shoot()
        {
            if (cd > 0 || shooters.Count == 0) return;
            Candidates(Range, false, tmp);
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
            cd = 1f / Mathf.Max(0.05f, def.stats.rate * Rate);
            var s = shooters[Random.Range(0, shooters.Count)];
            if (!s) return;
            RubberShot.Fire(s.transform.TransformPoint(GunMuzzle), t, def.stats.damage * Mul);
            Snd.At("shotgun", s.transform.position, 1, 0.04f, 0.1f);
            s.Kick();
        }

        // Gases: elige al azar entre su carril y los de al lado (los que tengan gente a tiro) y tira al grupo más denso.
        void Gas()
        {
            if (cd > 0) return;
            Candidates(Range, true, tmp);
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
            cd = 1f / Mathf.Max(0.05f, def.stats.rate * Rate);
            var s = shooters.Count > 0 ? shooters[Random.Range(0, shooters.Count)] : null;
            Vector3 from = s ? s.transform.TransformPoint(new Vector3(0.16f, 1.55f, 0.55f)) : transform.position + Vector3.up * 2.5f;
            GasCloud.Throw(from, t.transform.position, def.stats.area * (1 + 0.2f * (level - 1)), def.stats.damage * Mul,
                def.slow, Mathf.Max(def.slowTime, level >= 3 ? 5 : 3));
            Snd.At("gas_launch", from, 1, 0.1f);
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
            Snd.At("cuffs", t.transform.position, 1, 0.1f);
            Snd.At("whistle_short", transform.position, 0.6f, 0.6f);
            t.Capture();
            punch = 1;
        }

        const float WaterSpeed = 22f, WaterGravity = 0.55f;

        // Hidrante: el cañón apunta solo al manifestante más cercano (su carril o los de al lado) y lo moja,
        // lo empuja y lo frena. El tanque (cycleOn s) se gasta solo mientras moja; vacío, recarga cycleOff s.
        void Water(float dt)
        {
            if (reloading) { reload -= dt; if (reload <= 0) { reloading = false; tank = def.cycleOn * Rate; } }
            Candidates(Range, true, tmp);
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
            Snd.Loop(this, "loop_water", on);
            if (!on) return;
            tank -= dt;
            if (tank <= 0) { reloading = true; reload = def.cycleOff; }
            var main = jet.main;
            main.startLifetime = Mathf.Clamp(dist / WaterSpeed + 0.25f, 0.4f, 1.8f);
            t.TakeDamage(def.stats.damage * Mul * dt);
            t.Wet(1.5f);
            t.Slow(def.slow, 0.5f);
            t.Push(def.push * (1 + 0.25f * (level - 1)) * dt);
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
            Snd.At("moto_pass", runner.position, 1, 0.2f);
            BuildMotos();
            moto.localScale = Vector3.zero;
            motoAppear = 0;
            motoWait = cooldown;
        }

        public void TakeDamage(float a)
        {
            if (Dead || !Targetable) return;
            hp -= a;
            punch = Mathf.Max(punch, 0.5f);
            if (def.defense == DefenseKind.Vallado) shake = Mathf.Min(1, shake + 0.65f);
            if (a >= 1) Snd.At(def.defense == DefenseKind.Vallado ? "fence_clang" : "defense_hit", transform.position, 1, 0.12f, 0.12f);
            bar.Set(hp / maxHp);
            if (hp <= 0) Break();
        }

        void Break()
        {
            Dead = true;
            All.Remove(this);
            blocking.Clear();
            if (def.defense == DefenseKind.Vallado) Headlines.Say("fence_break", 10f); else Headlines.Say("defense_break", 4f, L.CardTitle(def).ToLowerInvariant());
            // vallado y camión se desarman; los agentes caen
            bool thing = def.defense == DefenseKind.Vallado || def.defense == DefenseKind.Hidrante || def.defense == DefenseKind.Contenedores || def.defense == DefenseKind.Tanqueta || def.defense == DefenseKind.Drone;
            Snd.At(thing ? "break_crash" : "unit_down", transform.position, 1, 0.1f, 0.08f);
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
            Snd.At("rubber_hit", aim, 1, 0.04f, 0.15f);
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
                Snd.At("gas_hiss", to, 1, 0.1f);
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
                Snd.At("baton", e.transform.position, 1, 0.06f, 0.1f, 0.85f);
            }
            if (x >= endX) Destroy(gameObject);
        }
    }
}
