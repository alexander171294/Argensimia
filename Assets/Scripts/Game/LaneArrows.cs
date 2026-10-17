using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Flechas en el extremo izquierdo (Av. de Mayo) de los carriles por donde van a venir manifestantes este mandato.
    // En la previa los carriles nuevos titilan más fuerte para avisar que se abrió una fila.
    public class LaneArrows : MonoBehaviour
    {
        public Color color = new Color(1f, 0.74f, 0.16f, 1f);
        readonly List<(Transform t, Material m, int lane)> arrows = new List<(Transform, Material, int)>();
        int lastCount = -1;
        readonly HashSet<int> fresh = new HashSet<int>();

        void Start()
        {
            var slots = PoliceSlots.I;
            if (!slots) { enabled = false; return; }
            var mesh = ArrowMesh(1.7f, 0.02f); var outline = ArrowMesh(1.9f, 0f);
            var dark = new Material(GameAssets.I.unlit); dark.SetColor("_BaseColor", new Color(0.16f, 0.08f, 0.02f));
            for (int l = 0; l < slots.LaneCount; l++)
            {
                var go = new GameObject("LaneArrow_" + l);
                go.transform.SetParent(transform, false);
                float x = slots.EastX(l) - 5.5f;
                go.transform.position = new Vector3(x, 0.22f, slots.LaneZ(l, x));
                var dir = slots.LanePoint(l, x - 1) - slots.LanePoint(l, x);
                go.transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, -90, 0);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                var m = new Material(GameAssets.I.unlit);
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var ol = new GameObject("Outline"); ol.transform.SetParent(go.transform, false);
                ol.AddComponent<MeshFilter>().sharedMesh = outline;
                var or = ol.AddComponent<MeshRenderer>(); or.sharedMaterial = dark; or.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                arrows.Add((go.transform, m, l));
            }
        }

        // flecha plana apuntando a +X local
        static Mesh ArrowMesh(float k, float y)
        {
            Vector2[] p = { new Vector2(-1.7f, -0.42f), new Vector2(0.15f, -0.42f), new Vector2(0.15f, -1.15f), new Vector2(1.75f, 0), new Vector2(0.15f, 1.15f), new Vector2(0.15f, 0.42f), new Vector2(-1.7f, 0.42f) };
            var v = new List<Vector3>(); foreach (var q in p) v.Add(new Vector3(q.x * k, y, q.y * k));
            int[] tri = { 0, 1, 5, 0, 5, 6, 2, 3, 4 };
            var t = new List<int>();
            for (int i = 0; i < tri.Length; i += 3) { t.Add(tri[i]); t.Add(tri[i + 2]); t.Add(tri[i + 1]); t.Add(tri[i]); t.Add(tri[i + 1]); t.Add(tri[i + 2]); }
            var mesh = new Mesh { name = "LaneArrow" };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        void Update()
        {
            var gs = GameState.I; var wd = WaveDirector.I;
            if (!gs || !wd) return;
            int n = wd.ActiveLaneCount;
            if (n != lastCount)
            {
                fresh.Clear();
                if (lastCount >= 0) for (int i = lastCount; i < n; i++) fresh.Add(WaveDirector.LaneOrder[i]);
                lastCount = n;
            }
            bool show = gs.phase == Phase.Previa || gs.phase == Phase.Oleada;
            float tm = Time.unscaledTime;
            foreach (var (t, m, l) in arrows)
            {
                bool on = show && wd.LaneActive(l);
                if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
                if (!on) continue;
                bool isNew = fresh.Contains(l) && gs.phase == Phase.Previa;
                float w = 0.5f + 0.5f * Mathf.Sin(tm * (isNew ? 7f : 3.2f));
                float s = (isNew ? 1.25f : 1f) * (1f + w * 0.08f);
                t.localScale = new Vector3(s, 1, s);
                // se desliza hacia la Casa Rosada
                m.SetColor("_BaseColor", Color.Lerp(color * 1.6f, color * 3.2f, isNew ? w : w * 0.5f)); // HDR: se lee sobre el pavimento y toma el bloom
            }
        }
    }
}
