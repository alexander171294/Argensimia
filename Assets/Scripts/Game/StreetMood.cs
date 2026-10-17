using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Decorado que cambia con el humor popular, siempre fuera del área de juego (plaza) y de la Casa Rosada:
    // - calles alrededor de la plaza: cuanto menos popular, más manifestantes (muy popular = casi vacías);
    // - CALLE CALIENTE (<50%): gomas quemadas y humo en las bocacalles;
    // - desde 30% para abajo: edificios de enfrente (sur) y de la izquierda (Av. de Mayo) incendiados, más cuanto
    //   más baja la popularidad; los de abajo de la cámara no humean: en su lugar hay una cortina de humo alta,
    //   grande y dispersa, a la altura de la cámara. Todo el humo se inclina apenas hacia la derecha (viento).
    public class StreetMood : MonoBehaviour
    {
        [Tooltip("Edificios incendiados con 5% de popularidad o menos")] public int burningBuildings = 14;
        [Tooltip("Popularidad desde la que empiezan los incendios")] public float fireThreshold = 30;
        [Tooltip("Popularidad con el máximo de incendios")] public float fireMax = 5;

        readonly List<GameObject> boca = new List<GameObject>(), calles = new List<GameObject>();
        readonly List<GameObject> banners = new List<GameObject>();
        readonly List<ParticleSystem> streetSmoke = new List<ParticleSystem>();
        readonly List<GameObject> streetFires = new List<GameObject>();
        readonly List<GameObject> buildingFires = new List<GameObject>();
        readonly List<Renderer> fireCandidates = new List<Renderer>();
        readonly List<GameObject> haze = new List<GameObject>();
        readonly List<ParticleSystem> curtain = new List<ParticleSystem>();
        int shownA, shownB;
        float tick, pop = -1;
        Color fog0, bg0, sun0;
        float fogStart0, fogEnd0, sunI0;
        Light sun;
        Camera cam;
        Transform fxRoot;

        GameState gs => GameState.I;

        void Start()
        {
            var root = GameObject.Find("PlazaDeMayo");
            if (!root) { enabled = false; return; }
            var crowd = root.transform.Find("Protesters");
            if (crowd)
            {
                // el orden de los hijos ya es el orden de aparición (PlazaBuilder.BuildCrowds)
                var a = crowd.Find("Crowd_Bocacalles"); if (a) foreach (Transform c in a) boca.Add(c.gameObject);
                var b = crowd.Find("Crowd_Calles"); if (b) foreach (Transform c in b) calles.Add(c.gameObject);
                foreach (Transform c in crowd) if (c.name == "banner") banners.Add(c.gameObject);
            }
            foreach (var p in boca) p.SetActive(false);
            foreach (var p in calles) p.SetActive(false);
            foreach (var b in banners) b.SetActive(false);

            fxRoot = new GameObject("MoodFX").transform;
            var fx = root.transform.Find("FX");
            if (fx) foreach (Transform s in fx) { var ps = s.GetComponent<ParticleSystem>(); if (ps) { streetSmoke.Add(ps); ps.Stop(); } }

            // gomas quemadas en las bocacalles
            foreach (var p in new[] { new Vector3(47, 0.02f, 6.5f), new Vector3(47, 0.02f, -6f), new Vector3(28, 0.02f, -30.5f), new Vector3(-3, 0.02f, -31f), new Vector3(9, 0.02f, 30.5f) })
            {
                var g = new GameObject("TireFire");
                g.transform.SetParent(fxRoot);
                g.transform.position = p;
                FxKit.Prim(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.12f, 0), new Vector3(1f, 0.12f, 1f), new Color(0.06f, 0.06f, 0.06f));
                FxKit.Fire(g.transform, new Vector3(0, 0.25f, 0), 0.9f);
                FxKit.Smoke(g.transform, new Vector3(0, 0.8f, 0), 1.1f, new Color(0.16f, 0.15f, 0.15f, 0.75f), 7);
                g.SetActive(false);
                streetFires.Add(g);
            }

            // humo bajo en las calles de enfrente (sur) y de la izquierda (Av. de Mayo); nunca en la de abajo de la cámara
            var hazePts = new List<Vector3>();
            for (float x = -38; x <= 44; x += 9) hazePts.Add(new Vector3(x, 0.5f, -31));
            for (float z = -24; z <= 24; z += 8) hazePts.Add(new Vector3(47, 0.5f, z));
            foreach (var p in hazePts)
            {
                var ps = FxKit.Smoke(fxRoot, p, 2.4f, new Color(0.42f, 0.4f, 0.38f, 0.45f), 3);
                var mm = ps.main; mm.startSpeed = 0.7f; mm.startLifetime = 10f;
                ps.gameObject.SetActive(false);
                haze.Add(ps.gameObject);
            }

            // cortina alta frente a la cámara: humo de los edificios de abajo, visto desde adentro, grande y disperso
            for (float x = -60; x <= 50; x += 18)
            {
                var ps = FxKit.Particles(fxRoot, new Vector3(x, 36, 33), Quaternion.Euler(-90, 0, 0), GameAssets.I.smoke,
                    new Color(0.5f, 0.47f, 0.45f, 0.16f), 0.35f, 16f, 14f, 24f, 1.2f, 70, -0.01f, 9f, 1.5f, 200, FxKit.Wind * 1.6f);
                curtain.Add(ps);
                var em = ps.emission; em.rateOverTime = 0;
            }

            // candidatos a incendio: fila de enfrente (sur) y Av. de Mayo (izquierda). Nunca la Casa Rosada ni la fila bajo la cámara.
            var city = root.transform.Find("City");
            if (city)
                foreach (Transform blk in city)
                    foreach (Transform b in blk)
                    {
                        if (!b.name.StartsWith("building")) continue;
                        var r = b.GetComponentInChildren<Renderer>();
                        if (!r) continue;
                        var c = r.bounds.center;
                        bool front = c.z < -30 && c.z > -75 && c.x > -48 && c.x < 50;
                        bool left = c.x > 46 && c.x < 115 && c.z < 30 && c.z > -40;
                        if (front || left) fireCandidates.Add(r);
                    }
            fireCandidates.Sort((a, b) => Score(a.bounds.center).CompareTo(Score(b.bounds.center)));

            sun = root.GetComponentInChildren<Light>();
            cam = Camera.main;
            fog0 = RenderSettings.fogColor; fogStart0 = RenderSettings.fogStartDistance; fogEnd0 = RenderSettings.fogEndDistance;
            if (cam) bg0 = cam.backgroundColor;
            if (sun) { sun0 = sun.color; sunI0 = sun.intensity; }
        }

        static int Step(List<GameObject> l, int shown, int target)
        {
            int step = Mathf.Max(1, Mathf.Abs(target - shown) / 10);
            for (int i = 0; i < step && shown != target; i++)
            {
                if (target > shown) l[shown++].SetActive(true);
                else l[--shown].SetActive(false);
            }
            return shown;
        }

        // primero los que más se ven: centro de la fila de enfrente y la esquina de Av. de Mayo
        static float Score(Vector3 c) => Mathf.Abs(c.x - 8) * 0.7f + Mathf.Abs(c.z + 42) + (c.x > 46 ? 12 : 0);

        void Update()
        {
            if (!gs) return;
            pop = pop < 0 ? gs.popularity : Mathf.MoveTowards(pop, gs.popularity, Time.deltaTime * 25);

            // gente en las calles (decorado):
            //  >70%: algunos sueltos en las bocacalles; 70%: bocacalles llenas sin amontonarse;
            //  70%→30%: se van llenando las calles que rodean la plaza; ≤30% (donde empieza el humo): todo lleno.
            float fracA = pop >= 75 ? Mathf.Lerp(0.08f, 0.3f, Mathf.InverseLerp(100, 75, pop)) : Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(75, 70, pop));
            float fracB = Mathf.Pow(Mathf.InverseLerp(70, 30, pop), 1.1f);
            tick -= Time.deltaTime;
            if (tick <= 0)
            {
                tick = 0.03f;
                shownA = Step(boca, shownA, Mathf.RoundToInt(fracA * boca.Count));
                shownB = Step(calles, shownB, Mathf.RoundToInt(fracB * calles.Count));
            }
            bool showBanners = pop < 70;
            foreach (var b in banners) if (b.activeSelf != showBanners) b.SetActive(showBanners);

            // desde 30%: humo y gomas en las bocacalles
            bool hot = pop < 30; // el humo de calle empieza al 30%, cuando ya están llenas todas las calles
            foreach (var ps in streetSmoke)
            {
                if (hot && !ps.isPlaying) ps.Play();
                else if (!hot && ps.isPlaying) ps.Stop();
            }
            for (int i = 0; i < streetFires.Count; i++)
            {
                bool on = pop < 30 - i * 3;
                if (streetFires[i].activeSelf != on) streetFires[i].SetActive(on);
            }

            // incendios: desde el umbral para abajo, más edificios cuanto más baja la popularidad
            float fireK = Mathf.Clamp01((fireThreshold - pop) / Mathf.Max(1, fireThreshold - fireMax));
            int burning = fireK <= 0 ? 0 : Mathf.CeilToInt(fireK * Mathf.Min(burningBuildings, fireCandidates.Count));
            while (buildingFires.Count < burning) buildingFires.Add(Ignite(fireCandidates[buildingFires.Count % fireCandidates.Count]));
            for (int i = 0; i < buildingFires.Count; i++)
            {
                bool on = i < burning;
                if (buildingFires[i] && buildingFires[i].activeSelf != on) buildingFires[i].SetActive(on);
            }
            int hazeOn = Mathf.CeilToInt(fireK * haze.Count);
            for (int i = 0; i < haze.Count; i++)
            {
                bool on = i < hazeOn;
                if (haze[i].activeSelf != on) haze[i].SetActive(on);
            }
            foreach (var c in curtain) { var em = c.emission; em.rateOverTime = 1.4f * fireK; }

            // niebla más cerrada y sucia, sol más rojo
            var smog = new Color(0.62f, 0.55f, 0.5f);
            RenderSettings.fogColor = Color.Lerp(fog0, smog, fireK);
            RenderSettings.fogStartDistance = Mathf.Lerp(fogStart0, 55, fireK);
            RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd0, 250, fireK);
            if (cam) cam.backgroundColor = Color.Lerp(bg0, smog, fireK);
            if (sun) { sun.color = Color.Lerp(sun0, new Color(1f, 0.66f, 0.45f), fireK); sun.intensity = Mathf.Lerp(sunI0, sunI0 * 0.82f, fireK); }
        }

        GameObject Ignite(Renderer r)
        {
            var b = r.bounds;
            var g = new GameObject("BuildingFire");
            g.transform.SetParent(fxRoot);
            g.transform.position = b.center;
            var toPlaza = new Vector3(-b.center.x, 0, -b.center.z).normalized;
            var face = b.center + Vector3.Scale(toPlaza, b.extents) * 0.95f;
            // fuego en planta baja y pisos bajos (lo que ve la cámara) y en el techo
            for (int i = 0; i < 4; i++)
            {
                float y = b.min.y + 1.5f + i * Random.Range(2.5f, 4.5f);
                var side = Vector3.Cross(Vector3.up, toPlaza) * Random.Range(-b.extents.magnitude * 0.3f, b.extents.magnitude * 0.3f);
                FxKit.Fire(g.transform, face + side + Vector3.up * (y - b.center.y) - b.center, Random.Range(2.6f, 3.6f), 30);
            }
            FxKit.Fire(g.transform, new Vector3(0, b.extents.y, 0), 4.5f, 34);
            // humo denso: columna negra desde el techo y humo pegado a la fachada
            var s = FxKit.Smoke(g.transform, new Vector3(0, b.extents.y + 3, 0), 7f, new Color(0.12f, 0.11f, 0.11f, 0.9f), 12);
            var main = s.main; main.startLifetime = 14f; main.startSpeed = 3f;
            FxKit.Smoke(g.transform, face - b.center + Vector3.up * (4 - b.extents.y), 3.5f, new Color(0.2f, 0.19f, 0.18f, 0.75f), 8);
            return g;
        }
    }
}
