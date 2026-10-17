using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Música de fondo en loop (alterna las canciones de Resources/Music) y volúmenes de música / efectos guardados.
    // Sobrevive a la recarga de escena (NUEVA PARTIDA), así la música no se corta en el loop menú ↔ partida.
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
        }

        void OnApplicationPause(bool p) { if (p) PlayerPrefs.Save(); }
        void OnApplicationQuit() => PlayerPrefs.Save();
    }
}
