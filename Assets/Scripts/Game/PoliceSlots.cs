using System;
using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Slots sobre los 4 carriles donde se colocan las cartas policiales, y los carriles mismos (para el recorrido de
    // los manifestantes). Los genera SlotBuilder desde BlenderSrc/plaza_layout.json (lanes).
    public class PoliceSlots : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            public int lane;
            public int index;
            public Vector3 position;
            [Tooltip("Yaw del carril (grados). Local +X = sentido del carril (hacia el este)")] public float yaw;
            public Transform marker;
            public Renderer highlight;
            [NonSerialized] public GameObject occupant;
            public bool Free => occupant == null;
        }

        [Serializable]
        public class Lane
        {
            [Tooltip("Polilínea (x, z) ordenada de oeste a este")] public Vector2[] pts;
        }

        public static PoliceSlots I { get; private set; }

        public List<Slot> slots = new List<Slot>();
        public List<Lane> lanes = new List<Lane>();
        public Color highlightColor = new Color(1f, 0.74f, 0.16f, 0.95f);
        [Tooltip("Brillo de los slots libres fuera del modo colocar (0-1)")] public float idleGlow = 0.55f;
        public float pulseSpeed = 3.2f;

        bool showing;
        Material[] mats; // instancia por slot (MaterialPropertyBlock no aplica con GPU Resident Drawer)
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            I = this;
            mats = new Material[slots.Count];
            for (int i = 0; i < slots.Count; i++) if (slots[i].highlight) mats[i] = slots[i].highlight.material;
            SetHighlight(false);
        }

        // ---------------------------------------------------------------- carriles

        public int LaneCount => lanes.Count;
        public float WestX(int lane) => lanes[lane].pts[0].x;
        public float EastX(int lane) => lanes[lane].pts[lanes[lane].pts.Length - 1].x;

        public float LaneZ(int lane, float x)
        {
            var p = lanes[lane].pts;
            if (x <= p[0].x) return p[0].y;
            if (x >= p[p.Length - 1].x) return p[p.Length - 1].y;
            int lo = 0, hi = p.Length - 1;
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (p[mid].x <= x) lo = mid; else hi = mid; }
            float t = (x - p[lo].x) / Mathf.Max(1e-4f, p[hi].x - p[lo].x);
            return Mathf.Lerp(p[lo].y, p[hi].y, t);
        }

        public Vector3 LanePoint(int lane, float x, float y = 0.16f) => new Vector3(x, y, LaneZ(lane, x));

        // El carril y sus vecinos inmediatos (ordenados por z a la altura de x): para gases e hidrante.
        public List<int> NeighborLanes(int lane, float x)
        {
            var order = new List<int>();
            for (int l = 0; l < lanes.Count; l++) order.Add(l);
            order.Sort((a, b) => LaneZ(a, x).CompareTo(LaneZ(b, x)));
            int i = order.IndexOf(lane);
            var r = new List<int> { lane };
            if (i > 0) r.Add(order[i - 1]);
            if (i >= 0 && i < order.Count - 1) r.Add(order[i + 1]);
            return r;
        }

        // Carril más cercano a un punto (dentro de la plaza).
        public int NearestLane(Vector3 p, float maxDist)
        {
            int best = -1;
            float bd = maxDist;
            for (int l = 0; l < lanes.Count; l++)
            {
                if (p.x < WestX(l) || p.x > EastX(l)) continue;
                float d = Mathf.Abs(LaneZ(l, p.x) - p.z);
                if (d < bd) { bd = d; best = l; }
            }
            return best;
        }

        // ---------------------------------------------------------------- slots

        public int FreeCount
        {
            get { int n = 0; foreach (var s in slots) if (s.Free) n++; return n; }
        }

        public bool HasFree(int size = 1) => size >= 2 ? HasFreePair() : FreeCount >= 1;

        public bool HasFreePair()
        {
            foreach (var s in slots) if (s.Free && Neighbor(s, true) != null) return true;
            return false;
        }

        // Vecino libre en el mismo carril (índice +1 o -1).
        Slot Neighbor(Slot s, bool mustBeFree)
        {
            foreach (var o in slots)
                if (o.lane == s.lane && Mathf.Abs(o.index - s.index) == 1 && (!mustBeFree || o.Free)) return o;
            return null;
        }

        // Slot libre más cercano (en XZ) a un punto del mundo, dentro de maxDist.
        public Slot Nearest(Vector3 p, float maxDist)
        {
            Slot best = null;
            float bd = maxDist * maxDist;
            foreach (var s in slots)
            {
                if (!s.Free) continue;
                float dx = s.position.x - p.x, dz = s.position.z - p.z, d = dx * dx + dz * dz;
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        // Par de slots libres contiguos cuyo centro está más cerca del punto (ítems de 2 slots: hidrante).
        public Slot NearestPair(Vector3 p, float maxDist, out Slot second)
        {
            Slot best = null;
            second = null;
            float bd = maxDist * maxDist;
            foreach (var s in slots)
            {
                if (!s.Free) continue;
                foreach (var o in slots)
                {
                    if (o.lane != s.lane || o.index != s.index + 1 || !o.Free) continue;
                    var mid = (s.position + o.position) * 0.5f;
                    float dx = mid.x - p.x, dz = mid.z - p.z, d = dx * dx + dz * dz;
                    // también vale tocar cualquiera de los dos slots
                    d = Mathf.Min(d, Sq(s.position, p), Sq(o.position, p));
                    if (d < bd) { bd = d; best = s; second = o; }
                }
            }
            return best;
        }

        static float Sq(Vector3 a, Vector3 b) { float dx = a.x - b.x, dz = a.z - b.z; return dx * dx + dz * dz; }

        public void Occupy(Slot s, GameObject go)
        {
            s.occupant = go;
            if (s.highlight) s.highlight.enabled = false;
        }

        public void SetHighlight(bool on)
        {
            showing = on;
        }

        void Update()
        {
            // Los libres titilan siempre (ola suave a lo largo del carril); en modo colocar brillan fuerte.
            float t = Time.unscaledTime;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                // slot ocupado: se oculta el diseño del piso (losa hundida) para que el ítem quede limpio sobre el pavimento
                if (s.marker && s.marker.gameObject.activeSelf != s.Free) s.marker.gameObject.SetActive(s.Free);
                if (!s.highlight) continue;
                s.highlight.enabled = s.Free;
                if (!s.Free) continue;
                float w = 0.5f + 0.5f * Mathf.Sin(t * (showing ? pulseSpeed * 1.6f : pulseSpeed) - s.index * 0.45f - s.lane * 1.3f);
                var c = highlightColor;
                c.a *= showing ? Mathf.Lerp(0.55f, 1f, w) : idleGlow * Mathf.Lerp(0.2f, 1f, w * w);
                mats[i].SetColor(BaseColor, c);
            }
        }
    }
}
