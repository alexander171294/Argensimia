using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Argensimia.UI;

namespace Argensimia
{
    // Loop menú ↔ partida.
    //  Menú: cámara detrás del helipuerto mirando la parte trasera de la Casa Rosada; el helicóptero recién despegó
    //        (inclinado, saliendo) y alrededor hay manifestantes con gomas prendidas y humo.
    //  Perder: la cámara panea desde la vista de juego hasta el helipuerto mientras el helicóptero despega y se
    //        detiene justo cuando queda en la pose del menú.
    //  NUEVA PARTIDA: la cámara panea de vuelta a la vista de juego mientras el helicóptero aterriza otra vez.
    //        Si ya se jugó, la escena se recarga (estado inicial limpio) en la misma pose del menú y arranca el paneo.
    public class GameFlow : MonoBehaviour
    {
        public enum State { Menu, ToGame, Playing, ToMenu }

        public static GameFlow I { get; private set; }
        static bool autoStart; // NUEVA PARTIDA tras recargar la escena

        public State state;
        public bool HudVisible => state == State.Playing;

        [Header("Helicóptero en el menú")]
        [Tooltip("Desplazamiento desde el helipuerto (m)")] public Vector3 airOffset = new Vector3(-3f, 8.4f, -5.5f);
        [Tooltip("Rotación en el aire: nariz abajo, rumbo, alabeo")] public Vector3 airEuler = new Vector3(13f, -120f, 10f);
        [Header("Cámara del menú")]
        public Vector3 menuCamPos = new Vector3(-111.98f, 29.39f, -1.44f);
        public Vector3 menuCamEuler = new Vector3(42.028f, 87.981f, 0f);
        [Header("Blur circular del menú (centro nítido en el helicóptero y el helipuerto)")]
        [Tooltip("Radio nítido (alto de pantalla)")] public float blurRadius = 0.3f;
        [Tooltip("Transición")] public float blurSoft = 0.2f;
        [Tooltip("Desenfoque máx (px a 1080p)")] public float blurPx = 22f;
        [Tooltip("Rotor en el menú: rápido pero se sigue con la mirada (°/s)")] public float menuRotor = 230f;
        [Header("Tiempos (s)")]
        public float panTime = 6.5f, spinUp = 1.2f, liftTime = 4.4f;
        float blur; // 0 = juego, 1 = menú
        // flotación sutil del helicóptero suspendido (respeta la pose si se lo mueve a mano)
        public Vector3 HoverBasePos { get; private set; }
        public Quaternion HoverBaseRot { get; private set; }
        Vector3 lastPos = Vector3.one * 9999;
        Quaternion lastRot;
        float floatT;
        // hidrante del menú tirando agua a la multitud
        Transform hidrante, cannon;
        ParticleSystem jet;
        Material sirenRed, sirenBlue;
        Color sirenRedEm, sirenBlueEm;
        Vector3 waterTarget;

        Vector3 MenuLook => menuCamPos + Quaternion.Euler(menuCamEuler) * Vector3.forward * 40f;
        public float idleRotor = 90f, flyRotor = 1300f;

        Camera cam;
        Vector3 gamePos, gameLook;
        Transform heli;
        Spin rotor;
        public float RotorSpeed => rotor ? rotor.speed : 0; // el audio del helicóptero sigue al rotor
        Vector3 padPos;
        Quaternion padRot;
        Transform crowd;
        MenuScreen menu;
        bool hovering, played;

        Vector3 AirPos => padPos + airOffset;
        Quaternion AirRot => Quaternion.Euler(airEuler);

        void Awake()
        {
            I = this;
            cam = Camera.main;
            if (cam)
            {
                gamePos = cam.transform.position;
                var f = cam.transform.forward;
                gameLook = gamePos + f * (Mathf.Abs(f.y) > 0.01f ? -gamePos.y / f.y : 70f);
            }
            var h = GameObject.Find("Helipuerto/helicopter");
            if (!h || !cam) { state = State.Playing; return; }
            heli = h.transform;
            padPos = heli.position;
            padRot = heli.rotation;
            rotor = heli.GetComponentInChildren<Spin>();
            state = State.Menu;
            SetCam(menuCamPos, MenuLook);
            SetHeli(1);
            SetRotor(menuRotor);
            hovering = true;
            blur = 1;
            BuildCrowd(false);
            SetupHidrante();
            // balizas de los patrulleros de la barricada (desfasadas para que no titilen juntas)
            SirenLights.Add(GameObject.Find("Helipuerto/MenuPatrullero1"), 0.13f);
            SirenLights.Add(GameObject.Find("Helipuerto/MenuPatrullero2"), 0.61f);
        }

        void Start()
        {
            if (state == State.Playing) return;
            menu = MenuScreen.Create(NewGame);
            menu.Show(false, 0);
            StartCoroutine(Boot());
        }

        // Pantalla de carga hasta que el juego deja de trabarse (shaders, texturas, audio); recién ahí entra el menú
        // con la cinta sellada, o arranca el paneo si venimos de NUEVA PARTIDA tras recargar la escena.
        IEnumerator Boot()
        {
            var load = LoadingScreen.Create();
            yield return LoadingScreen.WaitStable();
            yield return load.FadeOut(0.45f);
            if (autoStart) { autoStart = false; StartCoroutine(ToGame()); }
            else menu.Show(true, 0.6f, "¡Defendé la Casa Rosada!");
        }

        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            // En el menú el helicóptero queda suspendido (pose congelada, se puede mover a mano en el editor) y solo gira el rotor.
            float want = state == State.Menu || (state == State.ToMenu && hovering) ? 1 : 0;
            blur = Mathf.MoveTowards(blur, want, Time.unscaledDeltaTime * 0.9f);
            ApplyBlur();
            Hover();
            MenuHidrante();
        }

        // ------------------------------------------------------------------ transiciones

        public void NewGame()
        {
            if (state != State.Menu) return;
            Time.timeScale = 1;
            if (played)
            {
                autoStart = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            menu.Show(false, 0.3f);
            StartCoroutine(ToGame());
        }

        // Fin de partida: sale el helicóptero y queda el menú.
        public void ToMenu(bool win)
        {
            if (state != State.Playing || !heli) return;
            played = true;
            Time.timeScale = 1;
            var gs = GameState.I;
            TakeScore();
            string sub = win
                ? "<color=#f2c14e>¡COMPLETASTE EL PERÍODO!</color>\nPopularidad final: " + Mathf.RoundToInt(gs ? gs.popularity : 0) + "%"
                : "<color=#ff7a70>PERDISTE LA GOBERNABILIDAD</color>\nLa Plaza quedó en manos de la calle en el mandato " + (gs ? gs.mandate : 0);
            StartCoroutine(ToMenuRoutine(sub));
        }

        public void Resign()
        {
            if (state != State.Playing || !heli) return;
            played = true;
            Time.timeScale = 1;
            var gs = GameState.I;
            TakeScore();
            StartCoroutine(ToMenuRoutine("<color=#ff7a70>EL PRESIDENTE RENUNCIÓ</color>\nSe fue en helicóptero en el mandato " + (gs ? gs.mandate : 0)));
            if (WaveDirector.I) WaveDirector.I.Lose(); // corta la oleada; el HUD no muestra nada porque ya no está en partida
        }

        int lastScore, lastMandates;
        void TakeScore()
        {
            var gs = GameState.I;
            lastScore = gs ? gs.Score : 0;
            lastMandates = gs ? gs.MandatesSurvived : 0;
        }

        IEnumerator ToMenuRoutine(string sub)
        {
            sub += "  ·  Puntaje <color=#f2c14e>" + GameState.Money(lastScore) + "</color>";
            state = State.ToMenu;
            var hud = FindAnyObjectByType<GameHud>();
            if (hud) hud.SetHudVisible(false, 0.5f);
            BuildCrowd(true);
            hovering = false;
            heli.SetPositionAndRotation(padPos, padRot);
            float takeoff = spinUp + liftTime;
            StartCoroutine(Delayed(Mathf.Max(0, panTime - takeoff), TakeOff()));
            yield return Pan(cam.transform.position, GroundLook(cam.transform), menuCamPos, MenuLook, panTime);
            while (!hovering) yield return null;
            state = State.Menu;
            menu.Show(true, 0.6f, sub);
            menu.OfferScore(lastScore, lastMandates);
        }

        IEnumerator ToGame()
        {
            state = State.ToGame;
            played = true;
            ClearCrowd();
            StartCoroutine(Land());
            yield return Pan(menuCamPos, MenuLook, gamePos, gameLook, panTime);
            state = State.Playing;
            var hud = FindAnyObjectByType<GameHud>();
            if (hud) hud.SetHudVisible(true, 0.6f);
        }

        IEnumerator Delayed(float d, IEnumerator r)
        {
            if (d > 0) yield return new WaitForSecondsRealtime(d);
            yield return r;
        }

        // ------------------------------------------------------------------ cámara

        static Vector3 GroundLook(Transform t)
        {
            var f = t.forward;
            return t.position + f * (f.y < -0.01f ? -t.position.y / f.y : 70f);
        }

        void SetCam(Vector3 pos, Vector3 look)
        {
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
        }

        // Paneo por arriba del mapa: posición en curva (sube por el medio) y punto de mira interpolado.
        IEnumerator Pan(Vector3 p0, Vector3 l0, Vector3 p1, Vector3 l1, float dur)
        {
            var mid = (p0 + p1) * 0.5f + Vector3.up * Mathf.Max(20f, Vector3.Distance(p0, p1) * 0.3f);
            float t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Ease.InOutCubic(Mathf.Clamp01(t / dur));
                var pos = Vector3.Lerp(Vector3.Lerp(p0, mid, k), Vector3.Lerp(mid, p1, k), k);
                SetCam(pos, Vector3.Lerp(l0, l1, k));
                yield return null;
            }
            SetCam(p1, l1);
        }

        static readonly int IdBlur = Shader.PropertyToID("_MenuBlur"), IdCenter = Shader.PropertyToID("_MenuCenter"),
            IdR = Shader.PropertyToID("_MenuR"), IdSoft = Shader.PropertyToID("_MenuSoft"), IdPx = Shader.PropertyToID("_MenuRadius");

        // Blur circular (TiltShift.shader): nítido en el centro (helicóptero + helipuerto), desenfocado alrededor.
        void ApplyBlur()
        {
            float k = Mathf.SmoothStep(0, 1, blur);
            Shader.SetGlobalFloat(IdBlur, k);
            if (k <= 0 || !cam || !heli) return;
            var mid = Vector3.Lerp(padPos, heli.position, 0.55f);
            var vp = cam.WorldToViewportPoint(mid);
            Shader.SetGlobalVector(IdCenter, new Vector4(vp.x, vp.y, 0, 0));
            Shader.SetGlobalFloat(IdR, blurRadius);
            Shader.SetGlobalFloat(IdSoft, blurSoft);
            Shader.SetGlobalFloat(IdPx, blurPx);
        }

        void OnDisable() => Shader.SetGlobalFloat(IdBlur, 0);

        // ------------------------------------------------------------------ helicóptero

        void Hover()
        {
            if (!hovering || !heli) return;
            if ((heli.position - lastPos).sqrMagnitude > 1e-6f || Quaternion.Angle(heli.rotation, lastRot) > 0.01f)
            {
                HoverBasePos = heli.position; // recién llegó o se movió a mano: esa es la nueva pose base
                HoverBaseRot = heli.rotation;
            }
            floatT += Time.deltaTime;
            var p = HoverBasePos + new Vector3(Mathf.Sin(floatT * 0.7f) * 0.12f, Mathf.Sin(floatT * 1.1f) * 0.22f, Mathf.Cos(floatT * 0.5f) * 0.1f);
            var r = HoverBaseRot * Quaternion.Euler(Mathf.Sin(floatT * 0.9f) * 0.8f, 0, Mathf.Sin(floatT * 0.65f) * 1.2f);
            heli.SetPositionAndRotation(p, r);
            lastPos = p;
            lastRot = r;
        }

        // ------------------------------------------------------------------ hidrante del menú

        void SetupHidrante()
        {
            var h = GameObject.Find("Helipuerto/MenuHidrante");
            if (!h || !GameAssets.I) return;
            hidrante = h.transform;
            cannon = new GameObject("Cannon").transform;
            cannon.SetParent(hidrante, false);
            cannon.localPosition = new Vector3(0, 3.95f, 0.6f);
            FxKit.Prim(PrimitiveType.Cylinder, cannon, new Vector3(0, -0.1f, 0), new Vector3(0.6f, 0.12f, 0.6f), new Color(0.12f, 0.13f, 0.15f));
            FxKit.Prim(PrimitiveType.Cylinder, cannon, new Vector3(0, 0, 0.55f), new Vector3(0.2f, 0.55f, 0.2f), new Color(0.22f, 0.24f, 0.27f), Quaternion.Euler(90, 0, 0));
            jet = FxKit.Particles(cannon, new Vector3(0, 0, 1.1f), Quaternion.identity, GameAssets.I.water,
                new Color(0.85f, 0.93f, 1f, 0.8f), 22f, 1.4f, 0.35f, 0.9f, 220, 3f, 0.55f, 0.1f, 2.4f, 3000);
            var em = jet.emission; em.enabled = false;
            waterTarget = padPos + new Vector3(5f, 1.5f, -14f); // la multitud del lado sur del helipuerto
            var r = h.GetComponentInChildren<Renderer>();
            if (r)
            {
                foreach (var m in r.materials)
                {
                    if (m.name.StartsWith("M_LightRed")) { sirenRed = m; sirenRedEm = m.GetColor("_EmissionColor"); }
                    else if (m.name.StartsWith("M_LightBlue")) { sirenBlue = m; sirenBlueEm = m.GetColor("_EmissionColor"); }
                }
            }
        }

        void MenuHidrante()
        {
            if (!cannon) return;
            bool on = state == State.Menu || state == State.ToMenu;
            // apunta a la multitud barriendo un poco de lado a lado
            var dir = waterTarget - cannon.position; dir.y = 0;
            float dist = dir.magnitude;
            float s = Mathf.Clamp(dist * 0.85f * 9.81f * 0.55f / (22f * 22f), 0, 1);
            float ang = 0.5f * Mathf.Asin(s) * Mathf.Rad2Deg;
            var want = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(-ang, Mathf.Sin(Time.time * 0.6f) * 9f, 0);
            cannon.rotation = Quaternion.RotateTowards(cannon.rotation, want, 90 * Time.deltaTime);
            var em = jet.emission;
            if (em.enabled != on) em.enabled = on;
            if (on) { var main = jet.main; main.startLifetime = Mathf.Clamp(dist / 22f + 0.25f, 0.4f, 1.8f); }
            float p = Mathf.Repeat(Time.time * 1.6f, 1f);
            bool red = on && (p < 0.1f || (p > 0.18f && p < 0.28f));
            bool blue = on && ((p > 0.5f && p < 0.6f) || (p > 0.68f && p < 0.78f));
            if (sirenRed) sirenRed.SetColor("_EmissionColor", sirenRedEm * (red ? 2.6f : 0.06f));
            if (sirenBlue) sirenBlue.SetColor("_EmissionColor", sirenBlueEm * (blue ? 2.6f : 0.06f));
        }

        // 0 = apoyado en el helipuerto, 1 = pose del menú (despegando inclinado). Sube primero, después se va.
        void SetHeli(float t)
        {
            float up = Ease.OutCubic(Mathf.Clamp01(t * 1.15f));
            float away = t * t;
            var p = padPos + new Vector3(airOffset.x * away, airOffset.y * up, airOffset.z * away);
            heli.SetPositionAndRotation(p, Quaternion.Slerp(padRot, AirRot, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 1.3f - 0.15f))));
        }

        void SetRotor(float s) { if (rotor) rotor.speed = s; }

        IEnumerator TakeOff()
        {
            hovering = false;
            Snd.Ui("heli_start", 1, 1, 1f);
            float t = 0;
            while (t < spinUp)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / spinUp);
                SetRotor(Mathf.Lerp(idleRotor, flyRotor, k * k));
                heli.position = padPos + Vector3.up * Mathf.Sin(t * 40f) * 0.02f * k; // vibra antes de despegar
                yield return null;
            }
            t = 0;
            while (t < liftTime)
            {
                t += Time.unscaledDeltaTime;
                SetHeli(Mathf.Clamp01(t / liftTime));
                yield return null;
            }
            SetHeli(1);
            hovering = true;
            // ya suspendido: el rotor baja a "cámara lenta"
            float r = 0;
            while (r < 1.2f && hovering)
            {
                r += Time.unscaledDeltaTime;
                SetRotor(Mathf.Lerp(flyRotor, menuRotor, Mathf.SmoothStep(0, 1, r / 1.2f)));
                yield return null;
            }
        }

        IEnumerator Land()
        {
            hovering = false;
            var startPos = heli.position;
            var startRot = heli.rotation;
            float t = 0;
            while (t < liftTime)
            {
                t += Time.unscaledDeltaTime;
                float k = 1 - Mathf.Clamp01(t / liftTime);
                SetHeli(k);
                // absorbe la diferencia con la pose de hover del primer cuadro
                float w = Mathf.Clamp01(1 - t / 0.6f);
                heli.SetPositionAndRotation(Vector3.Lerp(heli.position, startPos, w), Quaternion.Slerp(heli.rotation, startRot, w));
                yield return null;
            }
            heli.SetPositionAndRotation(padPos, padRot);
            t = 0;
            while (t < spinUp)
            {
                t += Time.unscaledDeltaTime;
                SetRotor(Mathf.Lerp(flyRotor, idleRotor, Mathf.Sqrt(Mathf.Clamp01(t / spinUp))));
                yield return null;
            }
            SetRotor(idleRotor);
        }

        // ------------------------------------------------------------------ manifestantes del helipuerto

        void BuildCrowd(bool popIn)
        {
            ClearCrowd(true);
            var a = GameAssets.I;
            if (!a) return;
            crowd = new GameObject("MenuCrowd").transform;
            var rnd = new System.Random(7);
            float R(float lo, float hi) => lo + (float)rnd.NextDouble() * (hi - lo);
            float y = padPos.y;

            // gomas quemándose alrededor del helipuerto
            var tires = new[] { new Vector3(-9, 0, -14), new Vector3(3, 0, -16), new Vector3(10, 0, -12), new Vector3(10, 0, 12), new Vector3(-2, 0, 15) };
            foreach (var o in tires)
            {
                var t = new GameObject("Goma").transform;
                t.SetParent(crowd, false);
                t.position = new Vector3(padPos.x + o.x, y, padPos.z + o.z);
                FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.12f, 0), new Vector3(1.1f, 0.12f, 1.1f), new Color(0.06f, 0.06f, 0.06f));
                FxKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0.3f, 0.32f, 0.1f), new Vector3(1.0f, 0.1f, 1.0f), new Color(0.07f, 0.07f, 0.07f), Quaternion.Euler(12, 0, 8));
                FxKit.Fire(t, new Vector3(0, 0.3f, 0), 1.0f, 24);
                FxKit.Smoke(t, new Vector3(0, 1f, 0), 0.8f, new Color(0.32f, 0.3f, 0.3f, 0.45f), 4);
            }

            // anillo denso alrededor del helipuerto (el lado de la Casa Rosada queda libre)
            for (int i = 0; i < 170; i++)
            {
                float ang = R(40f, 320f) * Mathf.Deg2Rad;
                float r = R(11f, 20f);
                var pos = new Vector3(padPos.x + Mathf.Cos(ang) * r, y, padPos.z + Mathf.Sin(ang) * r);
                if (pos.x > -76f || Mathf.Abs(pos.z) > 25f || pos.x < -104f || Clash(pos, 1.3f)) continue;
                Person(a, rnd, pos, i % 4 == 0);
            }
            // relleno a los costados del encuadre (queda desenfocado): no exagerado, solo para que no se vea vacío
            for (int i = 0; i < 140; i++)
            {
                var pos = new Vector3(R(-110f, -76f), y, R(-34f, 34f));
                if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(padPos.x, padPos.z)) < 19f || Clash(pos, 1.6f)) continue;
                Person(a, rnd, pos, i % 5 == 0);
            }
            if (popIn) StartCoroutine(PopCrowd(crowd));
        }

        bool Clash(Vector3 pos, float d2)
        {
            foreach (Transform o in crowd) if ((o.position - pos).sqrMagnitude < d2) return true;
            return false;
        }

        void Person(GameAssets a, System.Random rnd, Vector3 pos, bool flag)
        {
            float R(float lo, float hi) => lo + (float)rnd.NextDouble() * (hi - lo);
            // envoltorio: aparece/desaparece escalando el envoltorio (Fidget mueve a la persona adentro)
            var w = new GameObject("Persona").transform;
            w.SetParent(crowd, false);
            w.position = pos;
            var look = padPos + airOffset * 0.6f - pos; look.y = 0;
            w.rotation = Quaternion.LookRotation(look) * Quaternion.Euler(0, R(-20, 20), 0);
            var p = Instantiate(a.people[rnd.Next(a.people.Length)], w);
            p.transform.localPosition = Vector3.zero;
            p.transform.localRotation = Quaternion.identity;
            p.transform.localScale = Vector3.one * Defense.PeopleScale * R(0.93f, 1.07f);
            foreach (var rr in p.GetComponentsInChildren<Renderer>())
            {
                var sm = rr.sharedMaterials;
                for (int k = 0; k < sm.Length; k++)
                {
                    if (!sm[k]) continue;
                    string nm = sm[k].name;
                    if (nm.StartsWith("M_Shirt")) sm[k] = a.shirts[rnd.Next(a.shirts.Length)];
                    else if (nm.StartsWith("M_Pants")) sm[k] = a.pants[rnd.Next(a.pants.Length)];
                    else if (nm.StartsWith("M_Skin")) sm[k] = a.skins[rnd.Next(a.skins.Length)];
                    else if (nm.StartsWith("M_Hair")) sm[k] = a.hairs[rnd.Next(a.hairs.Length)];
                }
                rr.sharedMaterials = sm;
            }
            if (flag && a.flagArg)
            {
                var fl = Instantiate(rnd.NextDouble() < 0.6 ? a.flagArg : a.flagRed, p.transform);
                fl.transform.localPosition = new Vector3(-0.3f, 0, 0.1f) / 1.45f;
                fl.transform.localRotation = Quaternion.Euler(0, R(-30, 30), 0);
                fl.transform.localScale = Vector3.one / 1.45f;
            }
            Fidget.Add(p, Fidget.Mode.Crowd);
        }

        IEnumerator PopCrowd(Transform c)
        {
            var kids = new System.Collections.Generic.List<(Transform t, Vector3 s)>();
            foreach (Transform k in c) { kids.Add((k, k.localScale)); k.localScale = Vector3.zero; }
            float t = 0;
            while (t < 1.6f && c)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < kids.Count; i++)
                {
                    if (!kids[i].t) continue;
                    float k = Mathf.Clamp01((t - i * 0.025f) / 0.4f);
                    kids[i].t.localScale = kids[i].s * Ease.OutBack(k);
                }
                yield return null;
            }
        }

        void ClearCrowd(bool instant = false)
        {
            if (!crowd) return;
            var c = crowd;
            crowd = null;
            if (instant) { Destroy(c.gameObject); return; }
            foreach (var ps in c.GetComponentsInChildren<ParticleSystem>()) { var em = ps.emission; em.enabled = false; }
            StartCoroutine(ShrinkCrowd(c));
        }

        IEnumerator ShrinkCrowd(Transform c)
        {
            float t = 0;
            while (t < 2.5f && c)
            {
                t += Time.unscaledDeltaTime;
                foreach (Transform k in c)
                    if (!k.GetComponentInChildren<ParticleSystem>()) k.localScale = Vector3.MoveTowards(k.localScale, Vector3.zero, Time.unscaledDeltaTime * 4f);
                yield return null;
            }
            if (c) Destroy(c.gameObject);
        }
    }
}
