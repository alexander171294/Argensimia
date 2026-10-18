using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Música de fondo en loop (alterna las canciones de Resources/Music), efectos de sonido (Resources/SFX, ver Snd)
    // y ambiente (multitud, bombo, cacerolas, helicóptero) según el estado de la partida. Volúmenes de música y
    // efectos guardados. Sobrevive a la recarga de escena (NUEVA PARTIDA), así el audio no se corta en el loop menú ↔ partida.
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio I { get; private set; }

        const string KeyMusic = "vol_music", KeySfx = "vol_sfx";
        public static float Music { get; private set; } = 0.7f;
        public static float Sfx { get; private set; } = 0.8f;
        public static event System.Action Changed;

        readonly List<AudioClip> tracks = new List<AudioClip>();
        AudioSource[] src;
        int cur, track;
        float fade = 1;
        const float CrossFade = 2.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I) return;
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            go.AddComponent<GameAudio>();
        }

        void Awake()
        {
            I = this;
            Music = PlayerPrefs.GetFloat(KeyMusic, 0.7f);
            Sfx = PlayerPrefs.GetFloat(KeySfx, 0.8f);
            tracks.AddRange(Resources.LoadAll<AudioClip>("Music"));
            // orden del loop (alterna marchas y temas más movidos); los que no estén en la lista van al final
            tracks.Sort((a, b) => Order(a.name).CompareTo(Order(b.name)));
            src = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                src[i] = gameObject.AddComponent<AudioSource>();
                src[i].playOnAwake = false;
                src[i].loop = false;
                src[i].spatialBlend = 0;
                src[i].ignoreListenerPause = true;
            }
            if (tracks.Count > 0) Play(0, false);
            InitSfx();
        }

        static readonly string[] Playlist = { "La_Marcha_del_Balcon", "Fuerzas_de_la_Plaza", "Candombe_en_la_Plaza", "La_Gran_Marcha", "Corte_de_Calle" };
        static int Order(string n) { int i = System.Array.IndexOf(Playlist, n); return i < 0 ? 100 : i; }

        void Play(int t, bool cross)
        {
            track = t % tracks.Count;
            if (cross) cur = 1 - cur;
            src[cur].clip = tracks[track];
            src[cur].Play();
            fade = cross ? 0 : 1;
        }

        void Update()
        {
            UpdateAmbience();
            if (tracks.Count == 0) return;
            var a = src[cur];
            fade = Mathf.MoveTowards(fade, 1, Time.unscaledDeltaTime / CrossFade);
            a.volume = Music * fade;
            var b = src[1 - cur];
            if (b.isPlaying) { b.volume = Music * (1 - fade); if (fade >= 1) b.Stop(); }
            // pasa a la siguiente canción con fundido unos segundos antes del final
            if (a.clip && a.time >= a.clip.length - CrossFade) Play(track + 1, true);
            else if (!a.isPlaying && fade >= 1) Play(track + 1, false);
        }

        public static void SetMusic(float v)
        {
            Music = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeyMusic, Music);
            Changed?.Invoke();
        }

        public static void SetSfx(float v)
        {
            Sfx = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeySfx, Sfx);
            Changed?.Invoke();
            PlaySfx("ui_tap", 1, 1, 0, 0.08f, null, true); // muestra del volumen elegido
        }

        void OnApplicationPause(bool p) { if (p) PlayerPrefs.Save(); }
        void OnApplicationQuit() => PlayerPrefs.Save();

        // ------------------------------------------------------------------ efectos

        // Volumen relativo de cada clip (los WAV vienen normalizados al pico).
        static readonly Dictionary<string, float> Gain = new Dictionary<string, float>
        {
            // interfaz
            { "ui_tap", 0.35f }, { "ui_error", 0.45f }, { "card_draw", 0.3f }, { "card_flip", 0.35f }, { "card_zoom", 0.28f },
            { "card_play", 0.45f }, { "card_shuffle", 0.4f }, { "coin", 0.45f }, { "spend", 0.32f }, { "pop_up", 0.42f },
            { "pop_down", 0.45f }, { "boo", 0.5f }, { "clock_tick", 0.3f }, { "mood_down", 0.5f }, { "stamp", 0.5f }, { "upgrade", 0.55f }, { "heal", 0.38f },
            { "crisis", 0.55f }, { "evade", 0.45f }, { "wave_start", 0.6f }, { "fanfare", 0.55f }, { "gameover", 0.42f },
            // policía
            { "place", 0.75f }, { "unit_down", 0.5f }, { "heli_start", 0.6f }, { "whistle", 0.4f }, { "whistle_short", 0.28f }, { "shotgun", 0.28f }, { "rubber_hit", 0.14f },
            { "baton", 0.24f }, { "fence_clang", 0.2f }, { "defense_hit", 0.2f }, { "gas_launch", 0.38f }, { "gas_hiss", 0.28f },
            { "siren_yelp", 0.28f }, { "cuffs", 0.34f }, { "moto_pass", 0.42f }, { "break_crash", 0.5f },
            // manifestantes
            { "poof", 0.16f }, { "boing", 0.34f }, { "gate_bang", 0.5f }, { "mortar_launch", 0.38f }, { "explosion", 0.5f },
            { "fire_ignite", 0.32f },
            // loops
            { "loop_water", 0.32f }, { "loop_crowd", 0.5f }, { "loop_bombo", 0.36f }, { "loop_cacerola", 0.18f }, { "loop_heli", 0.75f },
        };
        static float GainOf(string id) => Gain.TryGetValue(id, out var g) ? g : 0.5f;
        public static float SfxGain(string id) => GainOf(id) * Sfx;

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        AudioSource[] pool;
        int poolNext;
        const int Voices = 24, MaxSame = 4;

        void InitSfx()
        {
            // Efectos en PCM en memoria (sin decodificar al cargar): con Vorbis + DecompressOnLoad el build de Linux quedaba mudo.
            foreach (var c in Resources.LoadAll<AudioClip>("SFX"))
            {
                clips[c.name] = c;
                if (c.loadState == AudioDataLoadState.Unloaded) c.LoadAudioData();
            }
            Debug.Log("[GameAudio] efectos cargados: " + clips.Count + " · música: " + tracks.Count + " · salida " + AudioSettings.outputSampleRate + " Hz");
            pool = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0;
                pool[i] = s;
            }
            crowdSrc = LoopSource("loop_crowd");
            bomboSrc = LoopSource("loop_bombo");
            caceSrc = LoopSource("loop_cacerola");
            heliSrc = LoopSource("loop_heli");
        }

        public static AudioClip Clip(string id) => I && I.clips.TryGetValue(id, out var c) ? c : null;

        // Paneo según la posición en pantalla; offscreen = fuera de cuadro (se escucha más bajo).
        public static float PanFor(Vector3 world, out bool offscreen)
        {
            offscreen = false;
            var cam = Camera.main;
            if (!cam) return 0;
            var v = cam.WorldToViewportPoint(world);
            offscreen = v.z < 0 || v.x < -0.1f || v.x > 1.1f || v.y < -0.1f || v.y > 1.1f;
            return Mathf.Clamp((v.x - 0.5f) * 1.8f, -0.75f, 0.75f);
        }

        public static void PlaySfx(string id, float vol, float pitch, float pitchVar, float cooldown, Vector3? at, bool ui)
        {
            if (!I || Sfx <= 0.001f || vol <= 0) return;
            var a = I;
            var clip = Clip(id);
            if (!clip) return;
            float now = Time.unscaledTime;
            if (cooldown > 0 && a.lastPlay.TryGetValue(id, out var last) && now - last < cooldown) return;
            int same = 0;
            AudioSource free = null;
            foreach (var s in a.pool)
            {
                if (s.isPlaying) { if (s.clip == clip) same++; }
                else if (!free) free = s;
            }
            if (same >= MaxSame) return;
            if (!free) { free = a.pool[a.poolNext]; a.poolNext = (a.poolNext + 1) % a.pool.Length; }
            a.lastPlay[id] = now;
            float pan = 0;
            if (at.HasValue) { pan = PanFor(at.Value, out bool off); if (off) vol *= 0.45f; }
            free.Stop();
            free.clip = clip;
            free.volume = Mathf.Clamp01(vol * GainOf(id) * Sfx);
            free.pitch = pitch * (1 + Random.Range(-pitchVar, pitchVar));
            free.panStereo = pan;
            free.ignoreListenerPause = ui;
            free.Play();
        }

        // ------------------------------------------------------------------ ambiente

        AudioSource crowdSrc, bomboSrc, caceSrc, heliSrc;
        float crowdV, bomboV, caceV, heliV, heliP = 1;

        AudioSource LoopSource(string id)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0;
            s.volume = 0;
            s.clip = Clip(id);
            if (s.clip) s.Play();
            return s;
        }

        // Menú: la multitud rodea el helipuerto (cantos, bombo, cacerolas) y el helicóptero suspendido.
        // Oleada: la multitud y el bombo crecen con la cantidad de manifestantes; cacerolas si hay caceroleros.
        // Previa y cierre: murmullo de la calle que sube con la bronca.
        void UpdateAmbience()
        {
            if (!crowdSrc) return;
            float crowd = 0, bombo = 0, cace = 0, heli = 0, hp = 1;
            var flow = GameFlow.I;
            var gs = GameState.I;
            bool menu = flow && (flow.state == GameFlow.State.Menu || flow.state == GameFlow.State.ToMenu);
            bool leaving = flow && flow.state == GameFlow.State.ToMenu; // el helicóptero es el protagonista
            if (leaving) { crowd = 0.3f; bombo = 0.15f; }
            else if (menu) { crowd = 0.6f; bombo = 0.4f; }
            else if (gs && gs.phase == Phase.Oleada)
            {
                int n = 0, cac = 0;
                foreach (var e in Enemy.All)
                {
                    if (!e || e.Dead) continue;
                    n++;
                    if (e.def && e.def.kind == UnitKind.Cacerolero) cac++;
                }
                float k = Mathf.Clamp01(n / 30f);
                crowd = 0.15f + 0.6f * k;
                bombo = n > 2 ? 0.3f + 0.35f * k + 0.08f * gs.MoodIndex : 0;
                cace = cac > 0 ? Mathf.Clamp01(0.3f + cac * 0.1f) : 0;
            }
            else if (gs && gs.phase != Phase.Derrota) crowd = 0.06f + 0.05f * gs.MoodIndex;
            // Helicóptero: al perder arranca fuerte (tono y golpes de pala siguen al rotor); en el menú queda bajito.
            // Al apretar NUEVA PARTIDA (ToGame) no se escucha.
            if (menu && flow.RotorSpeed > 1)
            {
                float r = Mathf.Clamp01(flow.RotorSpeed / 1300f);
                hp = 0.5f + 0.7f * r;
                heli = leaving ? 0.6f + 0.4f * r : 0.3f;
            }
            float dt = Time.unscaledDeltaTime;
            crowdV = Mathf.MoveTowards(crowdV, crowd, dt * 0.5f);
            bomboV = Mathf.MoveTowards(bomboV, bombo, dt * 0.6f);
            caceV = Mathf.MoveTowards(caceV, cace, dt * 0.6f);
            heliV = Mathf.MoveTowards(heliV, heli, dt * (heli < heliV && !menu ? 2f : 0.6f));
            heliP = Mathf.MoveTowards(heliP, hp, dt * 1.2f);
            crowdSrc.volume = crowdV * SfxGain("loop_crowd");
            crowdSrc.panStereo = menu ? 0 : -0.25f; // los manifestantes vienen por la izquierda (este)
            bomboSrc.volume = bomboV * SfxGain("loop_bombo");
            bomboSrc.panStereo = menu ? 0.1f : -0.35f;
            caceSrc.volume = caceV * SfxGain("loop_cacerola");
            caceSrc.panStereo = menu ? -0.15f : -0.3f;
            heliSrc.volume = heliV * SfxGain("loop_heli");
            heliSrc.pitch = heliP;
        }
    }
}
