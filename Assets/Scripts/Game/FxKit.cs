using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Argensimia
{
    // Partículas, barras de vida y props primitivos creados en runtime.
    public static class FxKit
    {
        public static ParticleSystem Particles(Transform parent, Vector3 localPos, Quaternion localRot, Material mat, Color col,
            float speed, float life, float s0, float s1, float rate, float angle, float gravity, float radius = 0.1f, float grow = 1.8f, int max = 600, float windX = 0)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startSpeed = speed; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1);
            main.startColor = col; main.gravityModifier = gravity; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = angle; sh.radius = radius;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, grow));
            var ca = ps.colorOverLifetime; ca.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.12f), new GradientAlphaKey(0.75f, 0.6f), new GradientAlphaKey(0, 1) });
            ca.color = g;
            if (windX != 0)
            {
                // viento suave hacia la derecha de la pantalla (= -X en el mundo)
                var v = ps.velocityOverLifetime;
                v.enabled = true;
                v.space = ParticleSystemSimulationSpace.World;
                v.x = new ParticleSystem.MinMaxCurve(windX);
                v.y = new ParticleSystem.MinMaxCurve(0);
                v.z = new ParticleSystem.MinMaxCurve(0);
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        // Viento global: todos los humos se inclinan apenas hacia la derecha de la pantalla.
        public const float Wind = -0.9f;

        public static ParticleSystem Smoke(Transform parent, Vector3 pos, float scale, Color col, float rate = 6)
        {
            var a = GameAssets.I;
            return Particles(parent, pos, Quaternion.Euler(-90, 0, 0), a.smoke, col, 1.4f * scale, 7f, 2.2f * scale, 4.5f * scale, rate, 18, -0.03f, 0.6f * scale, 2.6f, 600, Wind * Mathf.Sqrt(scale));
        }

        public static ParticleSystem Fire(Transform parent, Vector3 pos, float scale, float rate = 22)
        {
            var a = GameAssets.I;
            return Particles(parent, pos, Quaternion.Euler(-90, 0, 0), a.fire, new Color(1f, 0.55f, 0.18f, 0.9f), 2.2f * scale, 0.9f, 0.7f * scale, 1.6f * scale, rate, 14, -0.15f, 0.5f * scale, 0.4f, 600, Wind * 0.4f);
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 lp, Vector3 ls, Color c, Quaternion? lr = null)
        {
            var go = GameObject.CreatePrimitive(t);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localRotation = lr ?? Quaternion.identity;
            go.transform.localScale = ls;
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            return go;
        }

        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();
        public static Material Mat(Color c)
        {
            if (!cache.TryGetValue(c, out var m) || m == null) { m = GameAssets.I.Lit(c); cache[c] = m; }
            return m;
        }
    }

    // Barra de vida mirando a cámara (dos quads unlit). Solo se ve si la vida no está llena.
    public class HpBar : MonoBehaviour
    {
        Transform fill;
        Material fillMat;
        GameObject root;
        float width, thick;
        public Color? fixedColor; // barra de otro dato (ej. espera de la motorizada)
        public bool alwaysShow;

        public static HpBar Create(Transform owner, float height, float width = 1.6f, float thick = 0.4f)
        {
            var go = new GameObject("HpBar");
            go.transform.SetParent(owner, false);
            go.transform.localPosition = Vector3.up * height;
            var b = go.AddComponent<HpBar>();
            b.width = width;
            b.thick = thick;
            b.root = go;
            var bg = Quad(go.transform, new Color(0.05f, 0.05f, 0.08f, 1), new Vector3(width + 0.16f, thick + 0.14f, 1), 0.002f);
            var f = Quad(go.transform, new Color(0.35f, 0.9f, 0.3f, 1), new Vector3(width, thick, 1), 0f);
            b.fill = f.transform;
            b.fillMat = f.GetComponent<Renderer>().material;
            b.Set(1);
            return b;
        }

        static GameObject Quad(Transform p, Color c, Vector3 s, float z)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(p, false);
            q.transform.localPosition = new Vector3(0, 0, z);
            q.transform.localScale = s;
            var r = q.GetComponent<Renderer>();
            r.material = new Material(GameAssets.I.unlit);
            r.material.SetColor("_BaseColor", c);
            r.shadowCastingMode = ShadowCastingMode.Off;
            return q;
        }

        public void Set(float k)
        {
            k = Mathf.Clamp01(k);
            bool show = alwaysShow || (k < 0.999f && k > 0);
            foreach (Transform c in transform) c.gameObject.SetActive(show);
            if (!show) return;
            fill.localScale = new Vector3(width * k, thick, 1);
            fill.localPosition = new Vector3(-width * (1 - k) * 0.5f, 0, 0);
            fillMat.SetColor("_BaseColor", fixedColor ?? Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.35f, 0.9f, 0.3f), k));
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
            var ls = transform.parent.lossyScale;
            transform.localScale = new Vector3(1 / Mathf.Max(0.01f, ls.x), 1 / Mathf.Max(0.01f, ls.y), 1 / Mathf.Max(0.01f, ls.z));
        }
    }
}
