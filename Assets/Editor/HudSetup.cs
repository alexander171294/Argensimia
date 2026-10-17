using System.IO;
using Argensimia;
using Argensimia.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;

// Assets de juego (fuentes TMP, cartas, unidades, GameAssets, materiales de FX) + objeto "Game" en la escena.
public static class HudSetup
{
    const string ResUi = "Assets/Resources/UI/";
    const string CardsDir = "Assets/Resources/Cards/";
    const string UnitsDir = "Assets/Resources/Units/";
    const string FxDir = "Assets/Resources/FX/";
    const string PlazaMats = "Assets/Materials/Plaza/";

    [MenuItem("Argensimia/4. Create Game Assets (fuentes, cartas, unidades)")]
    public static string CreateUiAssets()
    {
        Directory.CreateDirectory(ResUi);
        Directory.CreateDirectory(CardsDir);
        Directory.CreateDirectory(UnitsDir);
        Directory.CreateDirectory(FxDir);
        string r = MakeFont("Assets/UI/Fonts/Roboto-Black.ttf", ResUi + "Roboto-Black SDF.asset")
                 + MakeFont("Assets/UI/Fonts/Roboto-Bold.ttf", ResUi + "Roboto-Bold SDF.asset");
        AssetDatabase.DeleteAsset(CardsDir + "refuerzo.asset"); // reemplazada por Infantería con escudos
        AssetDatabase.DeleteAsset(CardsDir + "caballeria.asset"); // reemplazada por Motorizada
        int n = Cards();
        int u = Units();
        Assets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "fonts: " + r + " | cartas: " + n + " | unidades: " + u;
    }

    // ------------------------------------------------------------------ cartas

    static int Cards()
    {
        int n = 0;
        // ---- Policía (Colocar). Stats: vida, daño, ritmo (golpes/s), alcance, área, velocidad
        n += Card("vallado", "Vallado", CardKind.Colocar, 620, "#1fa59a", 3, Rarity.Comun,
            "Una línea de vallas al fondo del slot. Frena a todos hasta que la derriban; los encapuchados la rompen el triple de rápido. Las balas de goma pueden disparar por detrás.",
            "Vallan la Plaza: «es preventivo», explican desde Casa Rosada",
            c => { c.defense = DefenseKind.Vallado; c.stats = S(300, 0, 0, 0); });
        n += Card("infanteria", "Infantería con escudos", CardKind.Colocar, 1140, "#3557b8", 2, Rarity.Comun,
            "Tanque barato: una línea de agentes con escudos que frena a todos los que llegan y los golpea cuerpo a cuerpo. No llega detrás de un vallado.",
            "Despliegan la Infantería frente a la Casa Rosada",
            c => { c.defense = DefenseKind.Infanteria; c.stats = S(600, 8, 1, 1.5f); });
        n += Card("balas_goma", "Infantería con balas de goma", CardKind.Colocar, 1380, "#c0612b", 1, Rarity.PocoComun,
            "Agentes sin escudo con escopetas antitumulto. Disparan balas de goma hasta 2 casilleros adelante en su carril: desde atrás de un vallado le tiran primero a los que lo están golpeando. También frenan a todos los que llegan.",
            "Balas de goma en la Plaza: hay heridos y la oposición pide un informe",
            c => { c.defense = DefenseKind.BalasGoma; c.stats = S(400, 9, 1.2f, 14); });
        n += Card("lanzagases", "Lanzagases", CardKind.Colocar, 1620, "#6d7d2e", 1, Rarity.PocoComun,
            "Un agente solo con lanzagranadas. Tira granadas de gas hasta 3 casilleros adelante, al azar en su carril o en los de al lado, al grupo más denso: daño por segundo en un área y -40% de velocidad.",
            "Gases en la Plaza: vecinos denuncian ardor en los ojos",
            c => { c.defense = DefenseKind.Lanzagases; c.stats = S(120, 4, 0.35f, 21, 3.5f); c.slow = 0.4f; c.slowTime = 3; });
        n += Card("detencion", "Brigada de detención", CardKind.Colocar, 1760, "#4b4f9e", 0, Rarity.PocoComun,
            "Cada 4 s saca de juego a un manifestante cercano. Prioriza a los especiales; no puede con columnas.",
            "Detenciones en la Plaza: la oposición pide explicaciones",
            c => { c.defense = DefenseKind.Detencion; c.stats = S(200, 0, 0.25f, 2.5f); });
        n += Card("motorizada", "Motorizada", CardKind.Colocar, 2230, "#24408f", 1, Rarity.Rara,
            "Ocupa un slot. Cada 15 s sale una pasada a los porrazos hasta Av. de Mayo: golpea, empuja 3 m y aturde 1 s a cada uno. Mientras sale, en el slot ya aparece otra recargando; si los manifestantes llegan, le bajan la gobernabilidad.",
            "Irrumpe la policía motorizada en la Plaza",
            c => { c.defense = DefenseKind.Motorizada; c.stats = S(300, 40, 0, 0); c.push = 3; c.stun = 1; c.cycleOff = 12; });
        n += Card("hidrante", "Camión hidrante", CardKind.Colocar, 3470, "#2a7fd1", 1, Rarity.Rara,
            "Cañón de agua de hasta 4 casilleros adelante. Apunta solo al manifestante más cercano, en su carril o en los de al lado: lo moja, lo empuja 2 m/s y lo frena 60%. Tanque de 8 s, recarga 6 s. Ocupa 2 slots.",
            "Hidrantes en la Plaza: denuncian represión",
            c => { c.defense = DefenseKind.Hidrante; c.stats = S(900, 3, 0, 28); c.push = 2; c.slow = 0.6f; c.cycleOn = 8; c.cycleOff = 6; c.slotSize = 2; });

        // ---- Efecto
        n += Card("impuestazo", "Impuestazo", CardKind.Efecto, 0, "#d8452f", 2, Rarity.Comun,
            "La AFIP sale a recaudar. Entra plata, pero la calle se calienta y las columnas llegan más enojadas.",
            "AFIP anuncia un nuevo impuesto «transitorio»", c => { c.moneyDelta = 3000; c.popularityDelta = -16; });
        n += Card("plan_platita", "Plan platita", CardKind.Efecto, 1850, "#8a3fc4", 2, Rarity.PocoComun,
            "Reparto de fondos. Sube la popularidad: menos gente en la calle y columnas más tranquilas.",
            "Lluvia de bonos: «es para reactivar el consumo»", c => c.popularityDelta = 10);
        n += Card("prestamo", "Préstamo FMI", CardKind.Efecto, 0, "#d9961c", 1, Rarity.Rara,
            "Entran dólares ya, a cambio de ajuste: baja el ingreso por segundo durante las oleadas.",
            "Llegó el desembolso del Fondo: «es el último», aseguran", c => { c.moneyDelta = 5000; c.incomeDelta = -15; });
        n += Card("blanqueo", "Blanqueo", CardKind.Efecto, 1000, "#2f9a4a", 2, Rarity.PocoComun,
            "Moratoria para que entre plata al sistema. Sube el ingreso por segundo, con algo de costo político.",
            "Lanzan un blanqueo: «esta vez sí», prometen", c => { c.incomeDelta = 20; c.popularityDelta = -6; });
        n += Card("cadena_nacional", "Cadena nacional", CardKind.Efecto, 520, "#c7477a", 2, Rarity.Comun,
            "Mensaje presidencial en todos los canales. Sube un poco la popularidad.",
            "Cadena nacional: el Presidente pide «calma y paciencia»", c => c.popularityDelta = 5);
        n += Card("feriado_puente", "Feriado puente", CardKind.Efecto, 1520, "#e07b2a", 0, Rarity.PocoComun,
            "Medio país se va a la costa: la próxima oleada viene 20% más chica. Solo en la previa.",
            "Decretan feriado puente: rutas colapsadas hacia la costa", c => { c.nextWaveReduction = 0.2f; c.previaOnly = true; });
        n += Card("dolar_soja", "Dólar soja", CardKind.Efecto, 0, "#9aa83a", 0, Rarity.PocoComun,
            "Tipo de cambio especial para el campo: entra mucha plata de golpe. Solo en la previa.",
            "Nuevo dólar soja: liquidan récord en una semana", c => { c.moneyDelta = 4000; c.popularityDelta = -4; c.previaOnly = true; });

        // ---- Ministros (gabinete de 3; solo salen en el 1 de 3)
        n += Card("min_economia", "Min. Economía", CardKind.Ministro, 1850, "#b8860b", 0, Rarity.PocoComun,
            "Ministro pasivo: +$25/s en las oleadas, pero cada mandato resta 2% de popularidad.",
            "Asume el nuevo ministro de Economía: «no hay plata», advierte", c => { c.incomeDelta = 25; c.popPerMandate = -2; });
        n += Card("min_seguridad", "Min. Seguridad", CardKind.Ministro, 2230, "#2b4a8a", 0, Rarity.Rara,
            "Ministro pasivo: todas las defensas policiales tienen +25% de gobernabilidad.",
            "Asume la nueva ministra de Seguridad: «el que las hace, las paga»", c => c.defenseHpBonus = 0.25f);
        n += Card("min_desarrollo", "Min. Desarrollo Social", CardKind.Ministro, 2470, "#3f9a7a", 0, Rarity.Rara,
            "Ministro pasivo: cada mandato suma 3% de popularidad.",
            "Desarrollo Social anuncia refuerzo de planes", c => c.popPerMandate = 3);

        // ---- Crisis (entran solas a la mano; el costo es lo que sale evitarlas)
        n += Card("devaluacion", "Devaluación", CardKind.Crisis, 1850, "#7a2d2d", 0, Rarity.Comun,
            "Salto del dólar: perdés el 30% de la plata. Aceptala o pagá para evitarla.",
            "Disparada del dólar: el Gobierno habla de «reacomodamiento»", c => { c.moneyMultiplier = 0.7f; c.inPool = false; });
        n += Card("paro_general", "Paro general", CardKind.Crisis, 2470, "#8a1f1f", 0, Rarity.Comun,
            "La CGT para el país: +1 columna organizada en la próxima oleada.",
            "La CGT confirma paro general y movilización a Plaza de Mayo", c => { c.extraColumns = 1; c.inPool = false; });
        n += Card("tarifazo", "Tarifazo", CardKind.Crisis, 1240, "#7a3d1f", 0, Rarity.Comun,
            "Aumentan luz, gas y transporte: baja el ingreso por segundo.",
            "Tarifazo: las boletas llegan con aumentos de tres cifras", c => { c.incomeDelta = -10; c.inPool = false; });
        n += Card("escandalo", "Escándalo", CardKind.Crisis, 1520, "#6a2a5a", 0, Rarity.Comun,
            "Se filtra un audio comprometedor: -15% de popularidad.",
            "Se filtró un audio y el Gobierno habla de «operación»", c => { c.popularityDelta = -15; c.inPool = false; });
        return n;
    }

    static UnitStats S(float hp, float dmg, float rate, float range, float area = 0, float speed = 0) =>
        new UnitStats { hp = hp, damage = dmg, rate = rate, range = range, area = area, speed = speed };

    // Crea o actualiza (sin pisar el arte asignado a mano).
    static int Card(string id, string title, CardKind kind, int cost, string hex, int copies, Rarity rarity, string desc, string headline,
        System.Action<CardDef> setup)
    {
        string path = CardsDir + id + ".asset";
        var c = AssetDatabase.LoadAssetAtPath<CardDef>(path);
        bool isNew = c == null;
        if (isNew) c = ScriptableObject.CreateInstance<CardDef>();
        var art = c.art;
        c.id = id; c.title = title; c.kind = kind; c.cost = cost; c.copies = copies; c.description = desc; c.headline = headline;
        c.rarity = rarity; c.inPool = true;
        c.popularityDelta = 0; c.moneyDelta = 0; c.incomeDelta = 0; c.popPerMandate = 0; c.defenseHpBonus = 0;
        c.moneyMultiplier = 1; c.extraColumns = 0; c.nextWaveReduction = 0; c.previaOnly = false;
        c.stats = default; c.maxBlock = 0; c.slow = 0; c.slowTime = 0; c.stun = 0; c.push = 0; c.cycleOn = 0; c.cycleOff = 0; c.slotSize = 1;
        ColorUtility.TryParseHtmlString(hex, out c.color);
        setup(c);
        c.art = art;
        if (isNew) AssetDatabase.CreateAsset(c, path);
        EditorUtility.SetDirty(c);
        return 1;
    }

    // ------------------------------------------------------------------ unidades

    static int Units()
    {
        int n = 0;
        n += Unit("comun", "Manifestante", UnitKind.Comun, 1, S(40, 5, 1, 1, 0, 1.4f), 1, 1, 6, false, u => u.prop = "flag");
        n += Unit("cacerolero", "Cacerolero", UnitKind.Cacerolero, 1, S(30, 0, 0, 0, 0, 1.2f), 1, 1, 2.2f, false,
            u => { u.shirt = 1; u.prop = "pot"; });
        n += Unit("jubilado", "Jubilado", UnitKind.Jubilado, 1, S(25, 2, 0.5f, 1, 0, 0.8f), 1, 2, 1.5f, false,
            u => { u.popOnHit = 0.1f; u.hair = 5; u.shirt = 6; u.scale = 0.95f; u.prop = "cane"; });
        n += Unit("encapuchado", "Encapuchado", UnitKind.Encapuchado, 2, S(50, 15, 1.2f, 1, 0, 2.0f), 2, 2, 1.6f, true,
            u => { u.fenceMultiplier = 3; u.jumpChance = 0.3f; u.shirt = 7; u.hair = 3; u.prop = "hood"; });
        n += Unit("piquetero", "Piquetero", UnitKind.Piquetero, 3, S(80, 6, 1, 1, 0, 1.2f), 2, 3, 1.1f, true,
            u => { u.tireEvery = 10; u.shirt = 5; u.prop = "tire"; });
        n += Unit("mortero", "Mortero", UnitKind.Mortero, 4, S(35, 25, 0.25f, 14, 2, 1.0f), 3, 3, 0.8f, true,
            u => { u.shirt = 0; u.hair = 2; u.prop = "mortar"; u.scale = 1.12f; });
        n += Unit("columna", "Columna organizada", UnitKind.Columna, 6, S(600, 10, 1, 1, 0, 0.7f), 5, 4, 0.5f, true,
            u => { u.auraSpeed = 0.2f; u.auraRadius = 4; u.pushResist = 0.5f; u.members = 6; u.prop = "banner"; });
        return n;
    }

    static int Unit(string id, string title, UnitKind kind, int cost, UnitStats st, int gov, int minMandate, float weight, bool special, System.Action<UnitDef> setup)
    {
        string path = UnitsDir + id + ".asset";
        var u = AssetDatabase.LoadAssetAtPath<UnitDef>(path);
        bool isNew = u == null;
        if (isNew) u = ScriptableObject.CreateInstance<UnitDef>();
        u.id = id; u.title = title; u.kind = kind; u.cost = cost; u.stats = st; u.govDamage = gov; u.minMandate = minMandate;
        u.weight = weight; u.special = special;
        u.popOnHit = 0; u.popOnArrive = 0; u.fenceMultiplier = 1; u.pushResist = 0; u.auraSpeed = 0; u.auraRadius = 4; u.tireEvery = 0; u.jumpChance = 0;
        u.shirt = -1; u.hair = -1; u.scale = 1; u.members = 1; u.prop = "none";
        setup(u);
        if (isNew) AssetDatabase.CreateAsset(u, path);
        EditorUtility.SetDirty(u);
        return 1;
    }

    // ------------------------------------------------------------------ GameAssets + FX

    static GameObject Model(string n) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/" + n + ".fbx");

    static Material[] Mats(string prefix, int count)
    {
        var l = new System.Collections.Generic.List<Material>();
        for (int i = 0; i < count; i++)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(PlazaMats + prefix + i + ".mat");
            if (m) l.Add(m);
        }
        return l.ToArray();
    }

    static void Assets()
    {
        const string path = "Assets/Resources/GameAssets.asset";
        var a = AssetDatabase.LoadAssetAtPath<GameAssets>(path);
        if (!a) { a = ScriptableObject.CreateInstance<GameAssets>(); AssetDatabase.CreateAsset(a, path); }
        a.people = new[] { Model("person_a"), Model("person_b"), Model("person_c"), Model("person_d") };
        a.officer = Model("police_officer"); a.truck = Model("police_truck"); a.valla = Model("valla_l") ? Model("valla_l") : Model("valla");
        a.flagArg = Model("flag_arg"); a.flagRed = Model("flag_red"); a.banner = Model("banner");
        a.shirts = Mats("M_Shirt_", 10); a.pants = Mats("M_Pants_", 5); a.skins = Mats("M_SkinV_", 5); a.hairs = Mats("M_HairV_", 6);

        var lit = FxMat("M_PropLit", Shader.Find("Universal Render Pipeline/Lit"));
        lit.SetColor("_BaseColor", Color.white); lit.SetFloat("_Smoothness", 0.15f); lit.enableInstancing = true;
        a.lit = lit;
        var unlit = FxMat("M_BarUnlit", Shader.Find("Universal Render Pipeline/Unlit"));
        unlit.SetColor("_BaseColor", Color.white);
        a.unlit = unlit;
        var soft = SoftTexture();
        a.smoke = Particle("M_FxSmoke", soft, false);
        a.fire = Particle("M_FxFire", soft, true);
        a.water = Particle("M_FxWater", soft, false);
        EditorUtility.SetDirty(a);
    }

    static Material FxMat(string name, Shader sh)
    {
        string p = FxDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Particle(string name, Texture2D tex, bool additive)
    {
        var m = FxMat(name, Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1);
        m.SetFloat("_Blend", additive ? 2 : 0);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D SoftTexture()
    {
        string p = FxDir + "T_FxSoft.png";
        if (!File.Exists(p))
        {
            const int s = 64;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = new Vector2(x + 0.5f - s / 2f, y + 0.5f - s / 2f).magnitude / (s / 2f);
                    float a = Mathf.Clamp01(1 - d);
                    t.SetPixel(x, y, new Color(1, 1, 1, a * a * (3 - 2 * a)));
                }
            t.Apply();
            File.WriteAllBytes(p, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(p);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static string MakeFont(string ttf, string path)
    {
        if (File.Exists(path)) return "(existe) ";
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        if (!font) return "FALTA " + ttf + " ";
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 12, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(fa, path);
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        return path + " ";
    }

    // ------------------------------------------------------------------ escena

    // Lo llama PlazaBuilder.Build() antes de guardar la escena.
    public static void AddGameObjects()
    {
        var old = GameObject.Find("Game");
        if (old) Object.DestroyImmediate(old);
        var go = new GameObject("Game");
        go.AddComponent<GameState>();
        go.AddComponent<CardDeck>();
        go.AddComponent<WaveDirector>();
        go.AddComponent<StreetMood>();
        go.AddComponent<GameHud>();
        go.AddComponent<GameFlow>();
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.transform.SetParent(go.transform);
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        SlotBuilder.Build();
    }

    [MenuItem("Argensimia/5. Add Game + HUD to Scene")]
    public static string AddToScene()
    {
        AddGameObjects();
        var s = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        return "Game + HUD agregados a " + s.path;
    }
}
