using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Argensimia;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Slots policiales sobre los 4 carriles horizontales (BlenderSrc/plaza_layout.json → lanes).
// Visual: "losa hundida" semitransparente sobre el pavimento: piso oscurecido, pared este en sombra,
// pared oeste iluminada (sol desde el este) y cordón claro. Más un brillo dorado para el modo colocar.
public static class SlotBuilder
{
    const string Dir = "Assets/Materials/Slots/";
    public const float SlotLen = 5.4f;   // a lo largo del carril
    public const float SlotWid = 4.0f;   // a lo ancho (carril = 4,6 m)
    public const int PerLane = 10;       // slots por carril, repartidos parejo (margen ~1,6 m)
    const float StartX = -36f, EndX = 31f; // al este queda libre la entrada de las columnas
    static readonly Vector2 SunXZ = new Vector2(0.95f, -0.31f); // hacia el sol, en XZ

    [MenuItem("Argensimia/6. Rebuild Police Slots")]
    public static string Rebuild()
    {
        int n = Build();
        var s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
        return n + " slots";
    }

    public static int Build()
    {
        var old = GameObject.Find("PoliceSlots");
        if (old) Object.DestroyImmediate(old);
        var lanes = ParseLanes(File.ReadAllText("BlenderSrc/plaza_layout.json"));
        var recess = Mat("M_SlotRecess", false);
        var glow = Mat("M_SlotGlow", true);

        var go = new GameObject("PoliceSlots");
        var ps = go.AddComponent<PoliceSlots>();
        foreach (var lp in lanes)
        {
            var sorted = new List<Vector2>(lp);
            sorted.Sort((p, q) => p.x.CompareTo(q.x));
            ps.lanes.Add(new PoliceSlots.Lane { pts = sorted.ToArray() });
        }
        for (int l = 0; l < lanes.Count; l++)
        {
            var pts = lanes[l];
            var centers = AlongArc(pts, PerLane);
            for (int i = 0; i < centers.Count; i++)
            {
                var c = centers[i];
                if (!Sample(pts, c, out var z, out var tan)) continue;
                float yaw = -Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg;
                var pos = new Vector3(c, 0.16f, z);
                var slot = new GameObject("Slot_" + l + "_" + i).transform;
                slot.SetParent(go.transform);
                slot.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                Quad(slot, recess, new Vector2(SlotLen + 0.12f, SlotWid + 0.12f), 0.006f, "Recess");
                var h = Quad(slot, glow, new Vector2(SlotLen + 0.7f, SlotWid + 0.7f), 0.02f, "Glow");
                h.enabled = false;
                ps.slots.Add(new PoliceSlots.Slot { lane = l, index = i, position = pos, yaw = yaw, marker = slot, highlight = h });
            }
        }
        return ps.slots.Count;
    }

    static Renderer Quad(Transform parent, Material m, Vector2 size, float y, string name)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.name = name;
        q.transform.SetParent(parent, false);
        q.transform.localPosition = new Vector3(0, y, 0);
        q.transform.localRotation = Quaternion.Euler(90, 0, 0);
        q.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return r;
    }

    // X de 'count' centros de slot repartidos parejo sobre la longitud de arco del carril entre StartX y EndX.
    static List<float> AlongArc(List<Vector2> pts, int count)
    {
        var sorted = new List<Vector2>(pts);
        sorted.Sort((a, b) => a.x.CompareTo(b.x));
        var xsArc = new List<Vector2>(); // (x, arco acumulado)
        float acc = 0;
        Vector2 prev = Vector2.zero;
        bool started = false;
        for (float x = StartX; x <= EndX; x += 0.05f)
        {
            if (!Sample(sorted, x, out var z, out _)) continue;
            var cur = new Vector2(x, z);
            if (started) acc += (cur - prev).magnitude;
            started = true;
            prev = cur;
            xsArc.Add(new Vector2(x, acc));
        }
        var xs = new List<float>();
        if (xsArc.Count == 0) return xs;
        float total = acc, pitch = count > 1 ? (total - SlotLen) / (count - 1) : 0;
        int j = 0;
        for (int i = 0; i < count; i++)
        {
            float target = SlotLen * 0.5f + i * pitch;
            while (j < xsArc.Count - 1 && xsArc[j].y < target) j++;
            xs.Add(xsArc[j].x);
        }
        return xs;
    }

    static bool Sample(List<Vector2> pts, float x, out float z, out Vector2 tan)
    {
        for (int i = 0; i < pts.Count - 1; i++)
        {
            var a = pts[i]; var b = pts[i + 1];
            if ((x >= a.x && x <= b.x) || (x <= a.x && x >= b.x))
            {
                float t = Mathf.Approximately(a.x, b.x) ? 0 : (x - a.x) / (b.x - a.x);
                z = Mathf.Lerp(a.y, b.y, t);
                var a2 = pts[Mathf.Max(0, i - 2)]; var b2 = pts[Mathf.Min(pts.Count - 1, i + 3)];
                tan = (b2 - a2).normalized;
                if (tan.x < 0) tan = -tan;
                return true;
            }
        }
        z = 0; tan = Vector2.right;
        return false;
    }

    static List<List<Vector2>> ParseLanes(string json)
    {
        var lanes = new List<List<Vector2>>();
        int i = json.IndexOf('[', json.IndexOf("\"lanes\""));
        int depth = 0;
        var num = new System.Text.StringBuilder();
        var vals = new List<float>();
        void Flush()
        {
            var t = num.ToString().Trim();
            if (t.Length > 0) vals.Add(float.Parse(t, CultureInfo.InvariantCulture));
            num.Clear();
        }
        for (; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '[')
            {
                depth++;
                if (depth == 2) lanes.Add(new List<Vector2>());
                if (depth == 3) vals.Clear();
            }
            else if (c == ']')
            {
                if (depth == 3) { Flush(); if (vals.Count >= 2) lanes[lanes.Count - 1].Add(new Vector2(vals[0], vals[1])); }
                depth--;
                if (depth == 0) break;
            }
            else if (depth == 3)
            {
                if (c == ',') Flush(); else num.Append(c);
            }
        }
        return lanes;
    }

    // ---------------------------------------------------------------- texturas y materiales

    static Material Mat(string name, bool glow)
    {
        Directory.CreateDirectory(Dir);
        var tex = Tex(name.Replace("M_", "T_"), glow);
        string path = Dir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(glow ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", glow ? new Color(1f, 0.74f, 0.16f, 0.95f) : Color.white);
        m.SetFloat("_Surface", 1);
        m.SetFloat("_Blend", 0);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = glow ? 3010 : 3000;
        if (!glow)
        {
            // Sin especular: en transparente URP suma brillo y la losa se ve "elevada" en vez de hundida
            m.SetFloat("_Smoothness", 0f); m.SetFloat("_SpecularHighlights", 0); m.SetFloat("_EnvironmentReflections", 0);
            m.SetFloat("_BlendModePreserveSpecular", 0); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.SetFloat("_ReceiveShadows", 1); m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static float SdRB(Vector2 p, Vector2 b, float r)
    {
        var q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
        return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
    }

    static Texture2D Tex(string name, bool glow)
    {
        const float ppm = 64f;
        float w = glow ? SlotLen + 0.7f : SlotLen + 0.12f, d = glow ? SlotWid + 0.7f : SlotWid + 0.12f;
        int tw = Mathf.RoundToInt(w * ppm), th = Mathf.RoundToInt(d * ppm);
        var half = new Vector2(SlotLen, SlotWid) * 0.5f;
        const float rim = 0.16f, rOut = 0.42f;
        var inHalf = half - Vector2.one * rim;
        var dark = new Color(0.22f, 0.12f, 0.07f);
        var light = new Color(1f, 0.95f, 0.86f);
        var px = new Color[tw * th];
        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                var p = new Vector2((x + 0.5f) / ppm - w * 0.5f, (y + 0.5f) / ppm - d * 0.5f);
                float dOut = SdRB(p, half, rOut);
                Color c;
                if (glow)
                {
                    float a = Mathf.Exp(-Mathf.Abs(dOut) / 0.2f) * 0.95f + (dOut < 0 ? 0.32f : 0);
                    c = new Color(1, 1, 1, Mathf.Clamp01(a));
                }
                else
                {
                    float cov = Mathf.Clamp01(0.5f - dOut * ppm);
                    float dIn = SdRB(p, inHalf, rOut - rim);
                    float net = 0.16f; // cordón claro
                    if (dIn <= 0)
                    {
                        float band = Mathf.Exp(dIn / 0.28f);
                        const float e = 0.01f;
                        var n = new Vector2(SdRB(p + Vector2.right * e, inHalf, rOut - rim) - SdRB(p - Vector2.right * e, inHalf, rOut - rim),
                                            SdRB(p + Vector2.up * e, inHalf, rOut - rim) - SdRB(p - Vector2.up * e, inHalf, rOut - rim)).normalized;
                        float facing = Vector2.Dot(-n, SunXZ.normalized); // pared que mira al sol = iluminada
                        float lit = Mathf.Max(0, facing) * band * 0.38f;
                        float shade = 0.3f + Mathf.Max(0, -facing) * band * 0.6f + band * 0.14f;
                        net = Mathf.Lerp(lit - shade, 0.16f, Mathf.Clamp01(0.5f + dIn * ppm));
                    }
                    c = net >= 0 ? new Color(light.r, light.g, light.b, net * cov) : new Color(dark.r, dark.g, dark.b, -net * cov);
                }
                px[y * tw + x] = c;
            }
        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        string path = Dir + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 16;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
