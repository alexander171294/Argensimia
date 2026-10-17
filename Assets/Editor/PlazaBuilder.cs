using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Builds the Plaza de Mayo blockout scene from the Blender-generated FBX models.
public static class PlazaBuilder
{
    const string Models = "Assets/Models/";
    const string MatDir = "Assets/Materials/Plaza/";
    const string ScenePath = "Assets/Scenes/Plaza.unity";
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly List<Material> shirts = new List<Material>();
    static readonly List<Material> pants = new List<Material>();
    static readonly List<Material> facadesWarm = new List<Material>(), facadesStone = new List<Material>();
    static readonly List<Material> facades = new List<Material>(), roofs = new List<Material>(), awnings = new List<Material>(), skins = new List<Material>(), hairs = new List<Material>();
    static System.Random rng;
    const float PeopleScale = 1.45f;

    // Queue a step on the main thread and log its result to BlenderSrc/unity_out.txt (avoids CLI 5s timeout)
    public static string Queue(string step)
    {
        File.WriteAllText("BlenderSrc/unity_out.txt", "RUNNING " + step);
        {
            string r;
            try
            {
                if (step == "mats") r = SetupMaterials();
                else if (step == "remap") r = RemapModels();
                else if (step == "build") r = Build();
                else if (step == "refresh") { AssetDatabase.Refresh(); r = "refreshed"; }
                else if (step == "render") r = SetupRendering();
                else if (step == "fx") r = SimulateFX();
                else if (step == "full") { AssetDatabase.Refresh(); r = SetupMaterials() + " | " + RemapModels() + " | " + SetupRendering() + " | " + Build() + " | " + SimulateFX(); }
                else if (step == "all") r = SetupMaterials() + " | " + RemapModels() + " | " + Build();
                else r = "unknown step";
                r = "DONE " + step + "\n" + r;
            }
            catch (System.Exception e) { r = "FAIL " + step + "\n" + e; }
            File.WriteAllText("BlenderSrc/unity_out.txt", r);
        }
        return "ran " + step;
    }

    // ---------- materials ----------
    [MenuItem("Argensimia/1. Setup Materials")]
    public static string SetupMaterials()
    {
        Directory.CreateDirectory(MatDir);
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        mats.Clear();
        foreach (var line in File.ReadAllLines("BlenderSrc/palette.txt"))
        {
            var p = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 3) continue;
            ColorUtility.TryParseHtmlString(p[1], out var c);
            float em = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
            var m = MakeMat(p[0], c, em, lit);
            if (p.Length >= 5) ApplyTexture(m, p[3], float.Parse(p[4], System.Globalization.CultureInfo.InvariantCulture));
            mats[p[0]] = m;
        }
        string[] shirtHex = { "#c0392b", "#f4f4f2", "#75b8e8", "#2e7d4f", "#e6b631", "#e07b2a", "#7d7f86", "#2b2d33", "#8e44ad", "#1f5fa8" };
        shirts.Clear();
        for (int i = 0; i < shirtHex.Length; i++) { ColorUtility.TryParseHtmlString(shirtHex[i], out var c); shirts.Add(MakeMat("M_Shirt_" + i, c, 0, lit)); }
        string[] pantHex = { "#33405a", "#2b2b2b", "#5a4a3a", "#3d5a7a", "#6b6b6b" };
        pants.Clear();
        for (int i = 0; i < pantHex.Length; i++) { ColorUtility.TryParseHtmlString(pantHex[i], out var c); pants.Add(MakeMat("M_Pants_" + i, c, 0, lit)); }
        Variants(facades, "M_FacadeV_", new[] { "#f1e2c6", "#ecd3ad", "#f3e6d6", "#ecccb8", "#f2dcb2", "#e8d8c4" }, lit);
        foreach (var f in facades) ApplyTexture(f, "T_plaster", 3);
        Variants(facadesWarm, "M_FacadeW_", new[] { "#e9b98f", "#e2a48c", "#d9a676", "#efc8a0", "#e4b0a2", "#dfb48a" }, lit);
        foreach (var f in facadesWarm) ApplyTexture(f, "T_plaster", 3);
        Variants(facadesStone, "M_FacadeS_", new[] { "#e6dfd2", "#d9d0c1", "#ede7dc", "#d2c6b3" }, lit);
        foreach (var f in facadesStone) ApplyTexture(f, "T_stone", 4);
        Variants(roofs, "M_RoofV_", new[] { "#4e5566", "#475062", "#555a68" }, lit);
        foreach (var r in roofs) ApplyTexture(r, "T_roof", 2);
        Variants(awnings, "M_AwningV_", new[] { "#b8322b", "#2f6b47", "#1f4f8a", "#c7862a", "#7a2d4f" }, lit);
        Variants(skins, "M_SkinV_", new[] { "#e2b08c", "#c88d68", "#a8714f", "#f0c7a6", "#8a5a3c" }, lit);
        Variants(hairs, "M_HairV_", new[] { "#2a1d15", "#3a2a1f", "#5b3d24", "#141414", "#8a6b45", "#9a9a9a" }, lit);
        foreach (var leaf in new[] { "M_PalmLeaf", "M_Leaf2", "M_Leaf" }) { var m = mats[leaf]; m.SetFloat("_Cull", 0); m.doubleSidedGI = true; EditorUtility.SetDirty(m); }
        AssetDatabase.SaveAssets();
        return "materials: " + mats.Count;
    }

    static void ApplyTexture(Material m, string tex, float metersPerRepeat)
    {
        string a = "Assets/Textures/" + tex + ".png", n = "Assets/Textures/" + tex + "_n.png";
        foreach (var path in new[] { a, n })
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
            bool isN = path == n; bool dirty = false;
            if (isN && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
            if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (ti.anisoLevel != 8) { ti.anisoLevel = 8; dirty = true; }
            if (dirty) ti.SaveAndReimport();
        }
        var at = AssetDatabase.LoadAssetAtPath<Texture2D>(a); var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(n);
        var tiling = new Vector2(1f / metersPerRepeat, 1f / metersPerRepeat);
        if (at != null) { m.SetTexture("_BaseMap", at); m.SetTextureScale("_BaseMap", tiling); }
        if (nt != null) { m.SetTexture("_BumpMap", nt); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
        EditorUtility.SetDirty(m);
    }

    static void Variants(List<Material> list, string prefix, string[] hex, Shader lit)
    {
        list.Clear();
        for (int i = 0; i < hex.Length; i++) { ColorUtility.TryParseHtmlString(hex[i], out var c); list.Add(MakeMat(prefix + i, c, 0, lit)); }
    }

    static void Swap(GameObject go, Dictionary<string, Material> map)
    {
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var sm = r.sharedMaterials;
            for (int i = 0; i < sm.Length; i++) if (sm[i] != null && map.TryGetValue(sm[i].name, out var m)) sm[i] = m;
            r.sharedMaterials = sm;
        }
    }
    static T Pick<T>(List<T> l) => l[rng.Next(l.Count)];

    // ---------- rendering quality: shadows + color grading ----------
    public static string SetupRendering()
    {
        foreach (var path in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (a == null) continue;
            var so = new SerializedObject(a);
            so.FindProperty("m_ShadowDistance").floatValue = 380;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = 4096;
            so.FindProperty("m_ShadowCascadeCount").intValue = 2;
            var sp = so.FindProperty("m_Cascade2Split"); if (sp != null) sp.floatValue = 0.55f;
            var db = so.FindProperty("m_ShadowDepthBias"); if (db != null) db.floatValue = 1.0f;
            var nb = so.FindProperty("m_ShadowNormalBias"); if (nb != null) nb.floatValue = 0.6f;
            var hdr = so.FindProperty("m_SupportsHDR"); if (hdr != null) hdr.boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(a);
        }
        foreach (var rp in new[] { "Assets/Settings/PC_Renderer.asset" })
        {
            var rd = AssetDatabase.LoadAssetAtPath<ScriptableObject>(rp);
            if (rd == null) continue;
            var so = new SerializedObject(rd); var mode = so.FindProperty("m_RenderingMode");
            if (mode != null) { mode.intValue = 2; so.ApplyModifiedProperties(); EditorUtility.SetDirty(rd); }
        }
        SetupTiltShift();
        SetupSSAO();
        const string profPath = "Assets/Settings/PlazaProfile.asset";
        AssetDatabase.DeleteAsset(profPath);
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, profPath);
        // dirección de color: luz cálida / sombras frías (split toning), verdes y rosas vivos sin quemar el piso
        var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
        var bl = prof.Add<Bloom>(true); bl.threshold.Override(1.0f); bl.intensity.Override(0.55f); bl.scatter.Override(0.72f); bl.tint.Override(new Color(1f, 0.86f, 0.7f));
        var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.3f); ca.contrast.Override(10f); ca.saturation.Override(20f);
        var wb = prof.Add<WhiteBalance>(true); wb.temperature.Override(6f); wb.tint.Override(3f);
        var st = prof.Add<SplitToning>(true); st.shadows.Override(new Color(0.5f, 0.47f, 0.64f)); st.highlights.Override(new Color(1f, 0.8f, 0.58f)); st.balance.Override(0f);
        var smh = prof.Add<ShadowsMidtonesHighlights>(true); smh.shadows.Override(new Vector4(0.94f, 0.95f, 1.1f, -0.02f)); smh.midtones.Override(new Vector4(1.02f, 1.0f, 0.98f, 0f)); smh.highlights.Override(new Vector4(1.04f, 1.0f, 0.93f, -0.03f));
        var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.24f); vg.smoothness.Override(0.5f); vg.color.Override(new Color(0.22f, 0.12f, 0.08f));
        foreach (var c in prof.components) { c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(c, prof); }
        EditorUtility.SetDirty(prof);
        AssetDatabase.SaveAssets();
        return "rendering configured";
    }

    // Tilt-shift (desenfoque en bordes) como Full Screen Pass del renderer de PC
    static void SetupTiltShift()
    {
        const string matPath = "Assets/Materials/TiltShift.mat";
        var sh = Shader.Find("Argensimia/TiltShift");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, matPath); }
        mat.shader = sh;
        mat.SetFloat("_Focus", 0.46f); mat.SetFloat("_Band", 0.31f); mat.SetFloat("_Soft", 0.24f); mat.SetFloat("_Strength", 0.55f); mat.SetFloat("_Radius", 9f); mat.SetFloat("_Edge", 1.0f); mat.SetFloat("_EdgeStart", 0.78f);
        EditorUtility.SetDirty(mat);
        var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        if (rd == null) return;
        var f = rd.rendererFeatures.Find(x => x != null && x.name == "TiltShift") as FullScreenPassRendererFeature;
        if (f == null)
        {
            f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>(); f.name = "TiltShift";
            AssetDatabase.AddObjectToAsset(f, rd);
            var so = new SerializedObject(rd);
            var feats = so.FindProperty("m_RendererFeatures"); var map = so.FindProperty("m_RendererFeatureMap");
            feats.arraySize++; feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = f;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long lid);
            map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = lid;
            so.ApplyModifiedProperties();
        }
        f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        f.fetchColorBuffer = true;
        f.requirements = ScriptableRenderPassInput.Color;
        f.passMaterial = mat; f.passIndex = 0;
        f.SetActive(true);
        EditorUtility.SetDirty(f); EditorUtility.SetDirty(rd);
        AssetDatabase.SaveAssets();
    }

    // Oclusión ambiental en pantalla: contacto y profundidad en molduras, balcones, bordes de canteros
    static void SetupSSAO()
    {
        var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        if (rd == null) return;
        var t = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
        var f = rd.rendererFeatures.Find(x => x != null && x.GetType() == t);
        if (f == null)
        {
            f = (ScriptableRendererFeature)ScriptableObject.CreateInstance(t); f.name = "SSAO";
            AssetDatabase.AddObjectToAsset(f, rd);
            var so0 = new SerializedObject(rd);
            var feats = so0.FindProperty("m_RendererFeatures"); var map = so0.FindProperty("m_RendererFeatureMap");
            feats.InsertArrayElementAtIndex(0); feats.GetArrayElementAtIndex(0).objectReferenceValue = f;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long lid);
            map.InsertArrayElementAtIndex(0); map.GetArrayElementAtIndex(0).longValue = lid;
            so0.ApplyModifiedProperties();
        }
        var so = new SerializedObject(f);
        void F(string n, float v) { var q = so.FindProperty("m_Settings." + n); if (q != null) q.floatValue = v; }
        void I(string n, int v) { var q = so.FindProperty("m_Settings." + n); if (q != null) q.intValue = v; }
        F("Intensity", 1.6f); F("Radius", 0.6f); F("DirectLightingStrength", 0.35f); F("Falloff", 60f);
        I("Samples", 1); I("Source", 1); I("NormalSamples", 1); I("AOMethod", 1);
        var dn = so.FindProperty("m_Settings.Downsample"); if (dn != null) dn.boolValue = false;
        so.ApplyModifiedProperties(); f.SetActive(true);
        EditorUtility.SetDirty(f); EditorUtility.SetDirty(rd); AssetDatabase.SaveAssets();
    }

    public static string SimulateFX()
    {
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>()) { ps.Simulate(4f, true, true); }
        SceneView.RepaintAll();
        return "fx simulated";
    }

    static Material MakeMat(string name, Color c, float em, Shader lit)
    {
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
        m.shader = lit;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", name.Contains("Glass") || name.Contains("Window") ? 0.75f : 0.12f);
        m.SetFloat("_Metallic", name.Contains("Iron") || name.Contains("Metal") ? 0.4f : 0f);
        if (em > 0)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * em);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else { m.SetColor("_EmissionColor", Color.black); m.DisableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack; }
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    [MenuItem("Argensimia/2. Remap Model Materials")]
    public static string RemapModels()
    {
        if (mats.Count == 0) SetupMaterials();
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("_axis_test")) continue;
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.importAnimation = false;
            imp.animationType = ModelImporterAnimationType.None;
            imp.importCameras = false; imp.importLights = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            if (path.EndsWith("police_officer.fbx")) imp.isReadable = true; // Defense saca el escudo (balas de goma)
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var sm in r.sharedMaterials)
                {
                    if (sm == null) continue;
                    var key = sm.name.Split('.')[0];
                    if (mats.TryGetValue(key, out var target))
                        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sm.name), target);
                }
            imp.SaveAndReimport();
            n++;
        }
        return "remapped models: " + n;
    }

    // ---------- scene ----------
    static Transform root, city, plaza, props, crowd, police, traffic, fx;

    [MenuItem("Argensimia/3. Build Plaza Scene")]
    public static string Build()
    {
        SetupMaterials();
        rng = new System.Random(1810);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("PlazaDeMayo").transform;
        city = Group("City"); plaza = Group("Plaza"); props = Group("PlazaProps");
        crowd = Group("Protesters"); police = Group("Police"); traffic = Group("Traffic"); fx = Group("FX");

        Place("ground", Vector3.zero, 0, root, true);
        BuildCity();
        BuildCasaRosada();
        BuildHelipad();
        BuildPlaza();
        // Sin policía ni patrulleros fijos: toda la policía la coloca el jugador con cartas (Defense.cs)
        BuildCrowds();
        BuildTraffic();
        BuildLightingAndCamera();
        HudSetup.AddGameObjects(); // Game: estado, mazo y HUD (UI generada en runtime)

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == ScenePath)) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        return "scene built: " + root.GetComponentsInChildren<Transform>().Length + " transforms";
    }

    static Transform Group(string n) { var t = new GameObject(n).transform; t.SetParent(root); return t; }

    static GameObject Place(string model, Vector3 pos, float yaw, Transform parent, bool isStatic = true, float scale = 1f)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
        if (asset == null) { Debug.LogWarning("Missing model " + model); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        go.transform.localScale = Vector3.one * scale;
        if (model == "lamp")
        {
            var lgo = new GameObject("LampLight"); lgo.transform.SetParent(go.transform, false); lgo.transform.localPosition = new Vector3(0, 4.1f, 0);
            var l = lgo.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 9f; l.color = new Color(1f, 0.74f, 0.42f); l.shadows = LightShadows.None; l.enabled = false; // mediodía: faroles apagados
        }
        if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    // blocks: x0,x1,z0,z1 (outer, incl. 3m sidewalk) — must match gen_assets.layout_blocks()
    static List<Vector4> Blocks()
    {
        var o = new List<Vector4>();
        float[,] xs = { { -130, -110 }, { -100, -5 }, { 5, 40 }, { 50, 110 }, { 120, 170 } };
        float[,] zs = { { 36, 70 }, { 80, 110 }, { -70, -36 }, { -110, -80 } };
        for (int i = 0; i < 5; i++) for (int j = 0; j < 4; j++) o.Add(new Vector4(xs[i, 0], xs[i, 1], zs[j, 0], zs[j, 1]));
        o.Add(new Vector4(-130, -110, -26, 26));
        o.Add(new Vector4(50, 110, 6, 26)); o.Add(new Vector4(50, 110, -26, -6));
        o.Add(new Vector4(120, 170, 6, 26)); o.Add(new Vector4(120, 170, -26, -6));
        return o;
    }

    static void BuildCity()
    {
        string[] kinds = { "building_a", "building_b", "building_c", "building_it_a", "building_it_b", "building_it_c", "building_nv_a", "building_nv_b", "building_dc_a", "building_dc_b", "building_pt_a", "building_pt_b", "building_pt_c", "building_pt_a", "building_pt_c" };
        string last = "";
        // fila de enfrente (lado sur, mirando a la plaza): secuencia curada, nunca dos estilos iguales seguidos
        string[] frontKinds = { "building_pt_a", "building_nv_b", "building_pt_c", "building_it_b", "building_pt_b", "building_nv_a", "building_pt_a", "building_dc_b", "building_pt_c", "building_it_c" };
        int frontIdx = 0;
        foreach (var b in Blocks())
        {
            float x0 = b.x + 3, x1 = b.y - 3, z0 = b.z + 3, z1 = b.w - 3;
            int nx = Mathf.Max(1, Mathf.FloorToInt((x1 - x0) / 14f)), nz = Mathf.Max(1, Mathf.FloorToInt((z1 - z0) / 14f));
            float cw = (x1 - x0) / nx, ch = (z1 - z0) / nz;
            var blk = new GameObject($"Block_{b.x}_{b.z}").transform; blk.SetParent(city);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    bool edge = i == 0 || j == 0 || i == nx - 1 || j == nz - 1;
                    if (!edge) continue;
                    float cx = x0 + cw * (i + 0.5f), cz = z0 + ch * (j + 0.5f);
                    // face nearest street
                    float dN = z1 - cz, dS = cz - z0, dE = x1 - cx, dW = cx - x0;
                    float yaw = 0; float m = dN;
                    if (dS < m) { m = dS; yaw = 180; }
                    if (dE < m) { m = dE; yaw = 90; }
                    if (dW < m) { yaw = -90; }
                    if (nz == 1) yaw = 180; // single-row blocks face south (camera side)
                    bool near = b.z >= 36 && b.z < 80; // fila norte (bajo la cámara): más alta para que asome desenfocada en el borde inferior
                    bool corner = (i == 0 || i == nx - 1) && (j == 0 || j == nz - 1) && new Vector2(cx, cz).magnitude < 95 && nx > 1 && nz > 1 && Vector2.Distance(new Vector2(cx, cz), Vector2.zero) <= MinCornerDist(x0, x1, z0, z1, cw, ch) + 0.1f;
                    var k = corner ? (near ? (cx < 0 ? "building_terrace" : "building_corner") : "building_otto") : kinds[rng.Next(kinds.Length)]; // bajo la cámara, esquina oeste: terraza con vista a la plaza // la esquina Otto (alta, con agujas) nunca bajo la cámara
                    while (!corner && k == last) k = kinds[rng.Next(kinds.Length)];
                    last = k;
                    bool front = b.z == -70 && j == nz - 1;
                    if (front && !corner) k = frontKinds[frontIdx++ % frontKinds.Length];
                    bool round = b.x == 5 && b.z == -70 && i == nx - 1 && j == nz - 1; // esquina de enfrente a la izquierda: torre circular
                    if (round) { k = "building_round"; corner = false; }
                    // al lado de la terraza: edificio adelantado sobre la vereda apoyado en una recova (encuadre a mano en la Game view)
                    bool recova = near && Mathf.Abs(cx + 30.25f) < 1f && cz < 50;
                    if (recova) { k = "building_dc_b"; corner = false; }
                    // corner building: rotate so its rounded corner (local front-left) points at the plaza
                    if (corner) { float ang = Mathf.Atan2(-cx, -cz) * Mathf.Rad2Deg; yaw = Mathf.Round((ang + 45f) / 90f) * 90f; }
                    if (k == "building_terrace") yaw = 180; // el voladizo de la terraza avanza sobre la calle hacia la plaza
                    var go = Place(k, new Vector3(cx, 0.15f, cz), yaw, blk);
                    var fac = k.Contains("_it_") ? Pick(facadesWarm) : (k.Contains("_dc_") || k.Contains("_pt_") || k == "building_otto" || k == "building_terrace" || k == "building_round") ? Pick(facadesStone) : Pick(facades); var rf = Pick(roofs); var aw = Pick(awnings);
                    Swap(go, new Dictionary<string, Material> { { "M_Facade", fac }, { "M_Facade2", fac }, { "M_Facade3", fac }, { "M_Roof", rf }, { "M_AwningRed", aw }, { "M_AwningGreen", aw }, { "M_AwningBlue", aw } });
                    float s = Mathf.Min(cw, ch) / (k == "building_corner" || k == "building_otto" ? 18f : 14f);
                    go.transform.localScale = new Vector3(Mathf.Min(1.05f, s * 1.02f), near ? R(1.35f, 1.6f) : R(0.95f, 1.08f), Mathf.Min(1.05f, s * 1.02f));
                    if (round) go.transform.localScale = Vector3.one * (Mathf.Min(cw, ch) / 15f);
                    if (k == "building_terrace") { go.transform.localScale = Vector3.one; go.transform.position = new Vector3(-15.91f, 0.15f, 47.55f); TerracePeople(go.transform); } // posición ajustada a mano para que la terraza entre en cámara
                    if (recova)
                    {
                        go.transform.SetPositionAndRotation(new Vector3(cx, 0.15f + RecovaH, RecovaZ), Quaternion.Euler(0, 180, 0));
                        go.transform.localScale = new Vector3(1.02f, 1.41f, 1.02f);
                        var rb = Place("recova_base", new Vector3(cx, 0.15f, RecovaZ), 180, blk);
                        rb.transform.localScale = new Vector3(1.02f, 1f, 1.02f);
                        // lado que da al roof-top: medianera ciega, sin ventanas (cuerpo bajo y escalonamiento)
                        foreach (var (model, wx, y0) in new[] { ("medianera_a", -22.98f, 21.9f), ("medianera_b", -24.88f, 31.0f) }) // a medida (porteno_kit.medianera), sin escalar: textura en metros
                        {
                            var wall = Place(model, new Vector3(wx, y0, RecovaZ), 0, blk);
                            Swap(wall, new Dictionary<string, Material> { { "M_Facade3", fac } });
                        }
                    }
                }
            // street furniture on sidewalks
            if (b.x > 140 || b.w < -80 || b.z > 80) continue;
            var furn = new GameObject("Furniture").transform; furn.SetParent(blk);
            float in_ = 1.1f;
            for (float x = b.x + 5; x < b.y - 4; x += 12)
            {
                if (b.x == -100 && b.z == 36 && x > -44 && x < -18) continue; // vereda ocupada por la recova
                Place("lamp", new Vector3(x, 0.15f, b.z + in_), 0, furn);
                Place("lamp", new Vector3(x, 0.15f, b.w - in_), 0, furn);
                Place(rng.NextDouble() < 0.2 ? "tree_jacaranda" : (rng.NextDouble() < 0.5 ? "tree" : "tree_b"), new Vector3(x + 6, 0.15f, b.z + in_ + 0.4f), R(0, 360), furn, true, R(0.8f, 1.0f));
                Place(rng.NextDouble() < 0.2 ? "tree_jacaranda" : (rng.NextDouble() < 0.5 ? "tree" : "tree_b"), new Vector3(x + 6, 0.15f, b.w - in_ - 0.4f), R(0, 360), furn, true, R(0.8f, 1.0f));
            }
        }
    }

    // Gente en la terraza: prensa con trípodes en el borde que mira a la plaza y clientes del bar.
    // Coordenadas de Blender (x, y) del modelo building_terrace; local de Unity = (-x, z, -y).
    static void TerracePeople(Transform t)
    {
        const float deck = 4.4f + 3.8f + 4 * 3.3f + 0.38f; // = porteno_kit.terrace (4 pisos)
        var g = new GameObject("TerracePeople").transform; g.SetParent(t.parent);
        GameObject P(float bx, float by, float worldYaw, Material shirt, float lean = 0)
        {
            var pos = t.TransformPoint(new Vector3(-bx, deck, -by));
            var go = Place(rng.NextDouble() < 0.5 ? "person_a" : "person_b", pos, worldYaw, g, false, PeopleScale);
            go.transform.rotation = Quaternion.Euler(lean, worldYaw, 0);
            Swap(go, new Dictionary<string, Material> { { "M_Skin", Pick(skins) }, { "M_Hair", Pick(hairs) } });
            var rend = go.GetComponentInChildren<MeshRenderer>(); var sm = rend.sharedMaterials;
            for (int i = 0; i < sm.Length; i++) { if (sm[i] == null) continue; if (sm[i].name == "M_Shirt") sm[i] = shirt ?? shirts[rng.Next(shirts.Count)]; else if (sm[i].name == "M_Pants") sm[i] = pants[rng.Next(pants.Count)]; }
            rend.sharedMaterials = sm;
            return go;
        }
        // camarógrafo detrás del trípode (porteno_kit.TERRACE_CAM: x -4.94, y -13.43, rumbo 221°), mirando a la Casa Rosada
        float ry = 221f * Mathf.Deg2Rad;
        P(-4.94f - Mathf.Cos(ry) * 1.25f, -13.43f - Mathf.Sin(ry) * 1.25f, Mathf.Atan2(Mathf.Cos(ry), Mathf.Sin(ry)) * Mathf.Rad2Deg, shirts[7]);
        // persona apoyada en la baranda del frente, mirando hacia abajo a la plaza
        P(5.37f, -14.37f, 180, null, 14);
    }

    const float RecovaH = 6.66f, RecovaZ = 40.54f; // = porteno_kit.recova (alto de la arcada) y frente sobre la vereda

    static float MinCornerDist(float x0, float x1, float z0, float z1, float cw, float ch)
    {
        float m = float.MaxValue;
        foreach (var cx in new[] { x0 + cw / 2, x1 - cw / 2 }) foreach (var cz in new[] { z0 + ch / 2, z1 - ch / 2 }) m = Mathf.Min(m, new Vector2(cx, cz).magnitude);
        return m;
    }

    // Casa Rosada: escalada para entrar en el ancho de la plaza; un único vallado negro con entradas alineadas a los senderos
    const float CasaScale = 0.98f, FenceX = -46f, FenceHalf = 25.6f, CasaBackX = -74f;
    static readonly float[] GateZ = { -16.22f, -5.17f, 5.17f, 16.22f };   // = casa_gates_z de plaza_layout.json (fin de los 4 carriles)
    static readonly float[] GateW = { 4.4f, 4.4f, 4.4f, 4.4f };
    static bool InGate(float z) { for (int i = 0; i < GateZ.Length; i++) if (Mathf.Abs(z - GateZ[i]) < GateW[i] / 2 + 0.6f) return true; return false; }
    static void BuildCasaRosada()
    {
        var t = new GameObject("CasaRosada").transform; t.SetParent(root);
        Place("casa_rosada", new Vector3(FenceX - 1.5f - 14f * CasaScale, 0.155f, 0), 90, t, true, CasaScale);
        for (float z = -FenceHalf + 1; z <= FenceHalf - 0.9f; z += 2)
        {
            if (InGate(z)) continue;
            var f = Place("iron_fence", new Vector3(FenceX, 0.155f, z), 90, t); f.transform.localScale = new Vector3(1, 1.3f, 1);
        }
        for (float x = FenceX - 1; x >= CasaBackX; x -= 2)
            foreach (var z in new[] { FenceHalf, -FenceHalf }) { var f = Place("iron_fence", new Vector3(x, 0.155f, z), 0, t); f.transform.localScale = new Vector3(1, 1.3f, 1); }
        for (int i = 0; i < GateZ.Length; i++)
            foreach (var sd in new[] { -1f, 1f }) Place("lamp", new Vector3(FenceX + 0.4f, 0.155f, GateZ[i] + sd * (GateW[i] / 2 + 0.5f)), 0, t);
    }

    // Helipuerto en la explanada detrás de la Casa Rosada (heli_kit.py) con el helicóptero presidencial listo para despegar.
    static readonly Vector3 HelipadPos = new Vector3(-87f, 0.155f, 0f);
    static void BuildHelipad()
    {
        var t = new GameObject("Helipuerto").transform; t.SetParent(root);
        Place("helipad", HelipadPos, 0, t);
        var heli = Place("helicopter", HelipadPos + Vector3.up * 0.35f, -35, t, false, 1.15f);
        if (!heli) return;
        var rotor = Place("heli_rotor", Vector3.zero, 0, heli.transform, false);
        if (!rotor) return;
        rotor.transform.localPosition = new Vector3(0, 3.95f, -0.3f);
        rotor.transform.localRotation = Quaternion.Euler(0, 20, 0);
        rotor.transform.localScale = Vector3.one;
        rotor.AddComponent<Argensimia.Spin>().speed = 90f; // motor en marcha: por si hay que evacuar

        // Escena del menú: hidrante y patrulleros en barricada frente a la multitud (GameFlow los anima en el menú)
        var hid = Place("police_truck", new Vector3(-70.66f, 0f, -30.06f), -90, t, false, 1.05f);
        if (hid) hid.name = "MenuHidrante";
        var c1 = Place("patrol_car", new Vector3(-73.3f, 0f, 30.9f), 62, t, true, 1.15f);
        if (c1) c1.name = "MenuPatrullero1";
        var c2 = Place("patrol_car", new Vector3(-68.1f, 0f, 32.0f), 118, t, true, 1.15f);
        if (c2) c2.name = "MenuPatrullero2";
    }

    // lawn test, mirrors gen_assets lawn geometry
    static bool InLawn(float x, float z, float m)
    {
        float ax = Mathf.Abs(x), az = Mathf.Abs(z);
        if (ax < 3 + m || ax > 37 - m || az < 3 + m || az > 23 - m) return false;
        if (Mathf.Sqrt(x * x + z * z) < 10.5f + m) return false;
        float d = Mathf.Abs(ax * 23 - az * 37) / Mathf.Sqrt(37 * 37 + 23 * 23);
        if (d < 1.7f + m) return false;
        if ((ax > 34 - m || ax < 6 + m) && (az > 20 - m || az < 6 + m)) return false; // bevelled corners
        return true;
    }

    static void BuildPlaza()
    {
        var groups = new Dictionary<string, Transform>();
        foreach (var line in File.ReadAllLines("BlenderSrc/plaza_props.txt"))
        {
            var p = line.Split(' '); if (p.Length < 6) continue;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string model = p[0];
            if (!groups.TryGetValue(model, out var g)) { g = new GameObject(model + "s").transform; g.SetParent(model == "monument" ? plaza : props); groups[model] = g; }
            var pos = new Vector3(float.Parse(p[1], ci), float.Parse(p[2], ci), float.Parse(p[3], ci));
            var go = Place(model, pos, float.Parse(p[4], ci), g, true, float.Parse(p[5], ci));
            if (model == "iron_fence" && float.Parse(p[5], ci) < 0.99f) go.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    static void Scatter(string model, int count, float minDist, float minBig, List<Vector2> placed, float y, float s0, float s1)
    {
        int tries = 0, n = 0;
        while (n < count && tries++ < 4000)
        {
            float x = R(-37, 37), z = R(-23, 23);
            if (!InLawn(x, z, minDist * 0.6f)) continue;
            var p = new Vector2(x, z); bool ok = true;
            foreach (var q in placed) if ((q - p).sqrMagnitude < minBig * minBig) { ok = false; break; }
            if (!ok) continue;
            placed.Add(p); n++;
            Place(model, new Vector3(x, y, z), R(0, 360), props, true, R(s0, s1));
        }
    }

    static void BuildPolice()
    {
        // franja policial angosta entre la plaza y el vallado; las vallas las coloca el jugador
        const float Y = 0.16f;
        foreach (float zc in new[] { -19f, -8f, 8f, 19f })
            for (int r = 0; r < 2; r++)
                for (int i = 0; i < 3; i++)
                    Place("police_officer", new Vector3(-41.8f - r * 1.4f, Y, zc + (i - 1f) * 1.3f + r * 0.6f), 90 + R(-6, 6), police, false, PeopleScale);
        var truck = Place("police_truck", new Vector3(-43.2f, Y, 3.5f), 0, police, false, 1.1f);
        var jet = new GameObject("WaterJet");
        jet.transform.SetParent(truck.transform, false);
        jet.transform.localPosition = new Vector3(0, 3.9f, 0.3f);
        jet.transform.localRotation = Quaternion.Euler(-8, 90, 0);
        MakeParticles(jet, new Color(0.85f, 0.93f, 1f, 0.75f), 18f, 1.4f, 0.4f, 1.1f, 160, 3f, 1.2f);
        Place("patrol_car", new Vector3(-43.3f, Y, -13), 180, police, false);
        Place("patrol_car", new Vector3(-43.3f, Y, 13.5f), 0, police, false);
        Place("patrol_car", new Vector3(-36, 0, 33.5f), -70, police, false);
    }

    static void MakeParticles(GameObject go, Color col, float speed, float life, float s0, float s1, float rate, float angle, float gravity)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main; main.startSpeed = speed; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1);
        main.startColor = col; main.gravityModifier = gravity; main.maxParticles = 2000; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = angle; sh.radius = 0.1f;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.8f));
        var ca = ps.colorOverLifetime; ca.enabled = true;
        var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0, 1) });
        ca.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var m = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        if (m == null) m = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        r.sharedMaterial = m;
    }

    static void Crowd(string name, Vector3 c, Vector2 size, int count, float yaw, float flagChance)
    {
        var t = new GameObject(name).transform; t.SetParent(crowd);
        var pts = new List<Vector2>();
        int tries = 0;
        while (pts.Count < count && tries++ < count * 30)
        {
            var p = new Vector2(R(-size.x / 2, size.x / 2), R(-size.y / 2, size.y / 2));
            bool ok = true; foreach (var q in pts) if ((q - p).sqrMagnitude < 0.55f * PeopleScale * PeopleScale) { ok = false; break; }
            if (!ok) continue;
            pts.Add(p);
            var pos = c + new Vector3(p.x, 0, p.y);
            float y = yaw + R(-25, 25);
            double roll = rng.NextDouble();
            string model = roll < 0.5 ? "person_a" : roll < 0.75 ? "person_b" : roll < 0.9 ? "person_c" : "person_d";
            var go = Place(model, pos, y, t, false, R(0.92f, 1.08f) * PeopleScale);
            Swap(go, new Dictionary<string, Material> { { "M_Skin", Pick(skins) }, { "M_Hair", Pick(hairs) } });
            var rend = go.GetComponentInChildren<MeshRenderer>();
            var sm = rend.sharedMaterials;
            for (int i = 0; i < sm.Length; i++)
            {
                if (sm[i] == null) continue;
                if (sm[i].name == "M_Shirt") sm[i] = shirts[rng.Next(shirts.Count)];
                else if (sm[i].name == "M_Pants") sm[i] = pants[rng.Next(pants.Count)];
            }
            rend.sharedMaterials = sm;
            if ((model == "person_a" || model == "person_b") && rng.NextDouble() < flagChance * 1.6)
            {
                var f = Place(rng.NextDouble() < 0.55 ? "flag_arg" : "flag_red", pos, 0, go.transform, false);
                f.transform.localPosition = new Vector3(-0.3f, 0, 0.1f); f.transform.localRotation = Quaternion.Euler(0, R(-30, 30), 0); f.transform.localScale = Vector3.one;
            }
        }
    }

    // Decorado de manifestantes en las calles (fuera del área de juego). StreetMood los va prendiendo según la popularidad:
    // Crowd_Bocacalles = bocacalles (sueltos arriba de 70%, llenas, sin amontonarse, desde ~65%);
    // Crowd_Calles = calles que rodean la plaza (se llenan de 70% a 30%). El orden de los hijos es el orden de aparición.
    static readonly Vector2[] CrowdAvoid = { new Vector2(-15, -28.5f) }; // taxi estacionado

    static void BuildCrowds()
    {
        var boca = new GameObject("Crowd_Bocacalles").transform; boca.SetParent(crowd);
        var calles = new GameObject("Crowd_Calles").transform; calles.SetParent(crowd);
        var mouths = new List<Vector2> { new Vector2(1.5f, 31), new Vector2(0, -40), new Vector2(45, -40), new Vector2(57, -30.6f), new Vector2(53, 0) };
        var a = new List<(Vector3 p, float yaw, float key)>();
        // bocacalles: la de x=0 norte queda abajo de la cámara principal (el montoncito en primer plano)
        Area(a, -6, 9, 27.5f, 35, 1.55f, 180, null);           // montoncito en primer plano de la cámara (calle norte, frente a x=0)
        Area(a, -4.5f, 4.5f, -60, -37.5f, 1.55f, 0, null);     // sur x=0
        // (las bocacalles norte quedan fuera de cámara: esa gente pasó a la calle sur, al este de x=50)
        Area(a, 50.8f, 65, -34.2f, -27, 1.55f, 0, null);       // calle sur pasando la esquina de x=45 (arriba a la izquierda en cámara)
        Area(a, 41, 49, -55, -37.5f, 1.55f, 0, null);          // sur x=45
        Area(a, 51, 70.5f, -5, 5, 1.55f, -90, null);           // Av. de Mayo (lo que entra en cámara)
        Spawn(boca, a, 0.14f);
        var b = new List<(Vector3 p, float yaw, float key)>();
        Area(b, -14, -6.5f, 27, 35, 1.2f, 180, mouths);        // calle norte (sin el montoncito de la cámara; al oeste queda tapada por la terraza)
        Area(b, -56, -40.4f, -35, -26.2f, 1.2f, 0, mouths);    // calle sur frente a la Casa Rosada (gente reubicada desde la calle norte)
        Area(b, 9.5f, 40, 27, 35, 1.2f, 180, mouths);
        Area(b, -40, 40, -35, -27, 1.2f, 0, mouths);           // calle sur
        Area(b, 41, 49, -35.5f, 35.5f, 1.2f, -90, mouths);     // calle este
        // continuaciones que se ven al fondo desde la cámara (arriba a la izquierda y arriba): también se llenan al 30%
        Area(b, -4.5f, 4.5f, -79, -60, 1.2f, 0, mouths);      // sur x=0 hasta la calle de atrás
        Area(b, 41, 49, -79, -55, 1.2f, 0, mouths);            // sur x=45

        Spawn(calles, b, 0.1f);
        foreach (var (pos, yaw) in new[] { (new Vector3(0, 0.02f, 38.5f), 180f), (new Vector3(0, 0.02f, -38.5f), 0f), (new Vector3(45, 0.02f, 38.5f), 180f), (new Vector3(45, 0.02f, -38.5f), 0f), (new Vector3(51.5f, 0.02f, 0), -90f), (new Vector3(20, 0.02f, 30.5f), 180f), (new Vector3(-22, 0.02f, -30.5f), 0f), (new Vector3(45, 0.02f, 16), -90f) })
            Place("banner", pos, yaw, crowd, false);
        foreach (var p in new[] { new Vector3(52, 0, 6), new Vector3(30, 0, -26.5f), new Vector3(42, 0, 28) })
        {
            var s = new GameObject("Smoke"); s.transform.SetParent(fx); s.transform.position = p; s.transform.rotation = Quaternion.Euler(-90, 0, 0);
            MakeParticles(s, new Color(0.92f, 0.9f, 0.88f, 0.35f), 1.2f, 6f, 2.5f, 4.5f, 6, 25f, -0.02f);
        }
    }

    // Grilla con jitter (sin amontonar). key = orden de aparición: en las calles, primero cerca de las bocacalles.
    static void Area(List<(Vector3 p, float yaw, float key)> o, float x0, float x1, float z0, float z1, float sp, float yaw, List<Vector2> mouths)
    {
        for (float x = x0 + sp * 0.5f; x < x1; x += sp)
            for (float z = z0 + sp * 0.5f; z < z1; z += sp)
            {
                var q = new Vector2(x + R(-0.3f, 0.3f) * sp, z + R(-0.3f, 0.3f) * sp);
                bool ok = true; foreach (var v in CrowdAvoid) if ((v - q).sqrMagnitude < 9) ok = false;
                if (!ok) continue;
                float key = (float)rng.NextDouble();
                if (mouths != null) { float d = 1e9f; foreach (var m in mouths) d = Mathf.Min(d, (m - q).magnitude); key = d + R(0, 14); }
                o.Add((new Vector3(q.x, 0.02f, q.y), yaw, key));
            }
    }

    static void Spawn(Transform parent, List<(Vector3 p, float yaw, float key)> pts, float flagChance)
    {
        pts.Sort((x, y) => x.key.CompareTo(y.key));
        foreach (var e in pts) CrowdPerson(parent, e.p, e.yaw + R(-25, 25), flagChance);
    }

    static void CrowdPerson(Transform t, Vector3 pos, float y, float flagChance)
    {
        double roll = rng.NextDouble();
        string model = roll < 0.5 ? "person_a" : roll < 0.75 ? "person_b" : roll < 0.9 ? "person_c" : "person_d";
        var go = Place(model, pos, y, t, false, R(0.92f, 1.08f) * PeopleScale);
        Swap(go, new Dictionary<string, Material> { { "M_Skin", Pick(skins) }, { "M_Hair", Pick(hairs) } });
        var rend = go.GetComponentInChildren<MeshRenderer>();
        var sm = rend.sharedMaterials;
        for (int i = 0; i < sm.Length; i++)
        {
            if (sm[i] == null) continue;
            if (sm[i].name == "M_Shirt") sm[i] = shirts[rng.Next(shirts.Count)];
            else if (sm[i].name == "M_Pants") sm[i] = pants[rng.Next(pants.Count)];
        }
        rend.sharedMaterials = sm;
        if ((model == "person_a" || model == "person_b") && rng.NextDouble() < flagChance * 1.6)
        {
            var f = Place(rng.NextDouble() < 0.55 ? "flag_arg" : "flag_red", pos, 0, go.transform, false);
            f.transform.localPosition = new Vector3(-0.3f, 0, 0.1f); f.transform.localRotation = Quaternion.Euler(0, R(-30, 30), 0); f.transform.localScale = Vector3.one;
        }
    }

    static void BuildTraffic()
    {
        (string m, Vector3 p, float y)[] v =
        {
            ("taxi", new Vector3(75, 0, 28.5f), 90), ("taxi", new Vector3(98, 0, 33.5f), -90), ("taxi", new Vector3(130, 0, 28.5f), 90),
            ("taxi", new Vector3(42.5f, 0, 60), 180), ("taxi", new Vector3(47.5f, 0, 85), 0), ("taxi", new Vector3(47.5f, 0, -60), 0),
            ("taxi", new Vector3(-55, 0, 28.5f), 90), ("taxi", new Vector3(-50, 0, -33.5f), 90), ("taxi", new Vector3(-15, 0, -28.5f), -90),
            ("taxi", new Vector3(100, 0, -3), 90), ("taxi", new Vector3(135, 0, 3), -90), ("taxi", new Vector3(-105 + 2.5f, 0, 50), 0),
            ("bus", new Vector3(-82, 0, 33.5f), -90), ("bus", new Vector3(85, 0, -3), 90), ("taxi", new Vector3(80, 0, -33.5f), 90),
        };
        foreach (var e in v) Place(e.m, e.p, e.y, traffic, false);
        var tl = new GameObject("TrafficLights").transform; tl.SetParent(traffic);
        foreach (var c in new[] { new Vector2(45, 31), new Vector2(45, -31), new Vector2(0, 31), new Vector2(0, -31), new Vector2(45, 75), new Vector2(115, 31), new Vector2(115, -31), new Vector2(-105, 31), new Vector2(45, 0) })
        {
            Place("traffic_light", new Vector3(c.x + 6.3f, 0.15f, c.y - 6.3f), 0, tl);
            Place("traffic_light", new Vector3(c.x - 6.3f, 0.15f, c.y + 6.3f), 180, tl);
        }
    }

    static void BuildLightingAndCamera()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.87f, 0.68f); sun.intensity = 2.5f; // tarde soleada: cálida pero con sol alto
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.82f;
        sun.transform.rotation = Quaternion.Euler(52, -72, 0); // desde el este: ilumina la Casa Rosada, sombras hacia el oeste (no cruzan la plaza) // atardecer rasante desde el frente-izquierda: ilumina la Casa Rosada y las fachadas norte
        sun.transform.SetParent(root);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.64f, 0.68f, 0.84f);
        RenderSettings.ambientEquatorColor = new Color(0.70f, 0.64f, 0.64f);
        RenderSettings.ambientGroundColor = new Color(0.36f, 0.28f, 0.26f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.93f, 0.82f, 0.70f); RenderSettings.fogStartDistance = 170; RenderSettings.fogEndDistance = 420;
        RenderSettings.skybox = null;

        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; camGo.transform.SetParent(root);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.93f, 0.82f, 0.70f);
        cam.fieldOfView = 50; cam.nearClipPlane = 1f; cam.farClipPlane = 700;
        var rot = Quaternion.Euler(50, 180, 0); // vista desde el norte: Casa Rosada a la derecha
        var target = new Vector3(-6, 0, 3.9f); // corrida 2,9 m hacia atrás: se ve mejor el terreno
        camGo.transform.SetPositionAndRotation(target - rot * Vector3.forward * 72f, rot);
        var data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camGo.AddComponent<AudioListener>();

        var vol = new GameObject("Global Volume").AddComponent<Volume>(); vol.transform.SetParent(root);
        vol.isGlobal = true;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/PlazaProfile.asset");
    }
}
