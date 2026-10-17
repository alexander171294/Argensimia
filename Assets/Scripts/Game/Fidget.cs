using UnityEngine;

namespace Argensimia
{
    // Micro-movimientos para que la gente y las unidades se sientan vivas.
    //  Firm:    respira y se balancea apenas (fila de adelante, firme).
    //  Shuffle: se reacomoda cada tanto: da un pasito, gira un poco y vuelve a quedarse (filas de atrás).
    //  Crowd:   manifestante inquieto: mira para los costados, se balancea y rebota.
    //  Engine:  cuerpo de moto con el motor en marcha (las ruedas quedan fijas).
    public class Fidget : MonoBehaviour
    {
        public enum Mode { Firm, Shuffle, Crowd, Engine }

        public Mode mode;
        public float radius = 0.3f;

        Vector3 basePos, baseScale, off, offT;
        Quaternion baseRot;
        float yaw, yawT, wait, phase, step, kick, speedK;

        public static Fidget Add(GameObject g, Mode m, float radius = 0.3f)
        {
            var f = g.AddComponent<Fidget>();
            f.mode = m;
            f.radius = radius;
            var t = g.transform;
            f.basePos = t.localPosition; f.baseRot = t.localRotation; f.baseScale = t.localScale;
            f.phase = Random.value * 20;
            f.wait = Random.Range(0.2f, 2.5f);
            f.speedK = Random.Range(0.85f, 1.15f);
            return f;
        }

        // Retroceso al disparar / tirar.
        public void Kick() => kick = 1;

        void Update()
        {
            float dt = Time.deltaTime;
            phase += dt * speedK;
            kick = Mathf.MoveTowards(kick, 0, dt * 5);
            Vector3 p = basePos;
            Quaternion r = baseRot;
            float sy = 1;
            switch (mode)
            {
                case Mode.Engine:
                    p += Vector3.up * (Mathf.Sin(phase * 44f) * 0.03f + Mathf.Sin(phase * 9f) * 0.035f + 0.035f);
                    r = baseRot * Quaternion.Euler(0, 0, Mathf.Sin(phase * 37f) * 0.7f);
                    break;
                case Mode.Firm:
                    sy = 1 + Mathf.Sin(phase * 2.1f) * 0.012f;
                    r = baseRot * Quaternion.Euler(0, 0, Mathf.Sin(phase * 0.8f) * 0.9f);
                    break;
                case Mode.Crowd:
                    sy = 1 + Mathf.Sin(phase * 2.4f) * 0.015f;
                    wait -= dt;
                    if (wait <= 0) { yawT = Random.Range(-16f, 16f); wait = Random.Range(1.2f, 3.5f); }
                    yaw = Mathf.MoveTowardsAngle(yaw, yawT, dt * 30);
                    p += Vector3.up * Mathf.Max(0, Mathf.Sin(phase * 1.9f)) * 0.08f;
                    r = Quaternion.Euler(0, yaw, 0) * baseRot * Quaternion.Euler(0, 0, Mathf.Sin(phase * 1.3f) * 3f);
                    break;
                case Mode.Shuffle:
                    sy = 1 + Mathf.Sin(phase * 2.1f) * 0.012f;
                    wait -= dt;
                    if (wait <= 0)
                    {
                        var c = Random.insideUnitCircle * radius;
                        offT = new Vector3(c.x, 0, c.y);
                        yawT = Random.Range(-22f, 22f);
                        wait = Random.Range(1.4f, 4.2f);
                    }
                    var prev = off;
                    off = Vector3.MoveTowards(off, offT, dt * 0.8f);
                    bool mv = (off - prev).sqrMagnitude > 1e-7f;
                    step = mv ? step + dt * 14f : Mathf.MoveTowards(step, Mathf.Round(step / Mathf.PI) * Mathf.PI, dt * 8f);
                    yaw = Mathf.MoveTowardsAngle(yaw, yawT, dt * 45);
                    p += off + Vector3.up * Mathf.Abs(Mathf.Sin(step)) * 0.08f;
                    r = Quaternion.Euler(0, yaw, 0) * baseRot * Quaternion.Euler(0, 0, Mathf.Sin(step) * 2.5f + Mathf.Sin(phase * 0.8f) * 0.8f);
                    break;
            }
            if (kick > 0) p -= baseRot * Vector3.forward * kick * 0.2f;
            var t = transform;
            t.localPosition = p;
            t.localRotation = r;
            t.localScale = new Vector3(baseScale.x, baseScale.y * sy, baseScale.z);
        }
    }
}
