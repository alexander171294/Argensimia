using UnityEngine;

namespace Argensimia
{
    // Efectos de sonido (Resources/SFX, sintetizados por BlenderSrc/sfx_gen.py). Todo pasa por GameAudio:
    // pool de voces, enfriamiento por clip, tope de voces iguales y paneo según dónde cae el evento en pantalla.
    //  Ui: sonidos de interfaz (siguen sonando con la pausa).  At: eventos del mapa (se pausan con la partida).
    public static class Snd
    {
        public static void Ui(string id, float vol = 1, float pitch = 1, float cooldown = 0.03f)
            => GameAudio.PlaySfx(id, vol, pitch, 0.02f, cooldown, null, true);

        public static void At(string id, Vector3 pos, float vol = 1, float cooldown = 0.05f, float pitchVar = 0.07f, float pitch = 1)
            => GameAudio.PlaySfx(id, vol, pitch, pitchVar, cooldown, pos, false);

        // Loop atado a un objeto (ej. el chorro del hidrante): se prende y apaga con fundido y muere con el objeto.
        public static void Loop(Component owner, string id, bool on, float vol = 1) => SndLoop.Set(owner, id, on, vol);
    }

    public class SndLoop : MonoBehaviour
    {
        AudioSource src;
        string id;
        float level, target;

        public static void Set(Component owner, string id, bool on, float vol)
        {
            if (!owner) return;
            var l = owner.GetComponent<SndLoop>();
            if (!l)
            {
                if (!on) return;
                var clip = GameAudio.Clip(id);
                if (!clip) return;
                l = owner.gameObject.AddComponent<SndLoop>();
                l.Init(id, clip);
            }
            l.target = on ? vol : 0;
        }

        void Init(string i, AudioClip clip)
        {
            id = i;
            src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0;
            src.volume = 0;
            src.time = Random.Range(0, clip.length * 0.9f);
            src.Play();
        }

        void Update()
        {
            level = Mathf.MoveTowards(level, target, Time.deltaTime * 5f);
            src.panStereo = GameAudio.PanFor(transform.position, out _);
            src.volume = level * GameAudio.SfxGain(id);
        }
    }
}
