using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Detenciones: cada tanto un agente de la brigada sale corriendo hasta el manifestante más peligroso a alcance
    // (cualquier carril, 5 casilleros: un poco más que el hidrante), lo agarra (queda aturdido y desaparece detenido)
    // y vuelve a su lugar. Varios agentes pueden salir a la vez según el nivel (1 / 2 / 3). No puede con columnas.
    public partial class Defense
    {
        readonly List<Transform> squad = new List<Transform>();
        int running;
        static readonly HashSet<Enemy> targeted = new HashSet<Enemy>();
        const float RunSpeed = 15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetArrests() => targeted.Clear();

        void Arrest()
        {
            if (cd > 0 || running >= Mathf.Min(level, Mathf.Max(1, squad.Count - 1))) return;
            targeted.RemoveWhere(e => !e || e.Dead);
            Enemy t = null;
            float r = def.stats.range + HalfLen, r2 = r * r, bestD = float.MaxValue;
            var p = transform.position;
            foreach (var e in Enemy.All)
            {
                if (e.Dead || !e.onLane || e.Jumping || e.def.members > 1 || targeted.Contains(e)) continue;
                var dp = e.transform.position - p;
                float d2 = dp.x * dp.x + dp.z * dp.z;
                if (d2 > r2) continue;
                if (!t || e.Cost > t.Cost || (Mathf.Approximately(e.Cost, t.Cost) && d2 < bestD)) { t = e; bestD = d2; }
            }
            if (!t) return;
            // el agente más cercano al objetivo que esté en su lugar
            Transform cop = null;
            float cd2 = float.MaxValue;
            foreach (var o in squad)
            {
                if (!o || o.parent != model) continue;
                float d = (o.position - t.transform.position).sqrMagnitude;
                if (d < cd2) { cd2 = d; cop = o; }
            }
            if (!cop) return;
            cd = 1f / Mathf.Max(0.05f, def.stats.rate * Mul);
            targeted.Add(t);
            StartCoroutine(RunAndArrest(cop, t));
            Snd.At("whistle_short", transform.position, 0.7f, 0.4f);
            punch = 1;
        }

        IEnumerator RunAndArrest(Transform cop, Enemy t)
        {
            running++;
            var home = model;
            var fid = cop.GetComponent<Fidget>();
            if (fid) fid.enabled = false;
            Vector3 homeLocal = cop.localPosition;
            Quaternion homeRot = cop.localRotation;
            cop.SetParent(transform, true); // sigue colgado de la defensa: si se rompe, el agente se va con ella
            float phase = 0;
            bool caught = false;
            // ida
            while (t && !t.Dead)
            {
                var target = t.transform.position;
                var to = target - cop.position; to.y = 0;
                float dist = to.magnitude;
                if (dist < 1.3f) { caught = true; break; }
                if (dist < 4f) t.Stun(0.6f); // lo frena justo antes de agarrarlo
                Step(cop, to / Mathf.Max(0.01f, dist), Mathf.Min(dist, RunSpeed * Time.deltaTime), ref phase);
                yield return null;
            }
            if (caught && t && !t.Dead)
            {
                t.Capture();
                Snd.At("cuffs", t.transform.position, 1, 0.1f);
                yield return new WaitForSeconds(0.35f); // forcejeo
            }
            targeted.Remove(t);
            // vuelta
            while (cop && home && home == model)
            {
                var back = home.TransformPoint(homeLocal);
                var to = back - cop.position; to.y = 0;
                float dist = to.magnitude;
                if (dist < 0.2f) break;
                Step(cop, to / Mathf.Max(0.01f, dist), Mathf.Min(dist, RunSpeed * Time.deltaTime), ref phase);
                yield return null;
            }
            running--;
            if (!cop) yield break;
            if (!home || home != model) { Destroy(cop.gameObject); yield break; } // la defensa subió de nivel mientras corría
            cop.SetParent(home, true);
            cop.localPosition = homeLocal;
            cop.localRotation = homeRot;
            if (fid) fid.enabled = true;
        }

        // Paso de carrera: avanza, mira hacia donde va, rebota y se inclina.
        static void Step(Transform cop, Vector3 dir, float d, ref float phase)
        {
            phase += Time.deltaTime * 16f;
            var pos = cop.position + dir * d;
            pos.y = 0.16f + Mathf.Abs(Mathf.Sin(phase)) * 0.25f;
            cop.position = pos;
            if (dir.sqrMagnitude > 0.001f)
                cop.rotation = Quaternion.Slerp(cop.rotation, Quaternion.LookRotation(dir) * Quaternion.Euler(12, 0, Mathf.Sin(phase) * 6f), Time.deltaTime * 12f);
        }
    }
}
