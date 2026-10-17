using UnityEngine;

namespace Argensimia
{
    // Balizas de un vehículo policial: instancia los materiales M_LightRed / M_LightBlue del modelo y los hace
    // titilar alternados (doble destello rojo, doble destello azul), igual que la sirena del camión hidrante.
    public class SirenLights : MonoBehaviour
    {
        public float speed = 1.6f;
        public float phase;
        Material red, blue;
        Color redEm, blueEm;

        public static SirenLights Add(GameObject vehicle, float phase = 0)
        {
            if (!vehicle) return null;
            var s = vehicle.GetComponent<SirenLights>();
            if (!s) s = vehicle.AddComponent<SirenLights>();
            s.phase = phase;
            return s;
        }

        void Start()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                bool hit = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] && (mats[i].name.StartsWith("M_LightRed") || mats[i].name.StartsWith("M_LightBlue"))) hit = true;
                if (!hit) continue;
                foreach (var m in r.materials) // instancias propias de este vehículo
                {
                    if (m.name.StartsWith("M_LightRed")) { red = m; redEm = Base(m, new Color(1f, 0.1f, 0.08f)); }
                    else if (m.name.StartsWith("M_LightBlue")) { blue = m; blueEm = Base(m, new Color(0.15f, 0.35f, 1f)); }
                }
            }
            foreach (var m in new[] { red, blue }) if (m) m.EnableKeyword("_EMISSION");
        }

        static Color Base(Material m, Color fallback)
        {
            var c = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            return c.maxColorComponent > 0.05f ? c : fallback;
        }

        void Update()
        {
            float p = Mathf.Repeat(Time.time * speed + phase, 1f);
            bool r = p < 0.1f || (p > 0.18f && p < 0.28f);
            bool b = (p > 0.5f && p < 0.6f) || (p > 0.68f && p < 0.78f);
            if (red) red.SetColor("_EmissionColor", redEm * (r ? 2.6f : 0.06f));
            if (blue) blue.SetColor("_EmissionColor", blueEm * (b ? 2.6f : 0.06f));
        }
    }
}
