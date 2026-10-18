using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Progresión entre partidas (PlayerPrefs, JSON): mandatos sobrevividos en total, cartas desbloqueadas,
    // mejoras permanentes, contadores (cartas jugadas, detenidos…) y logros.
    //  · Cada 3 mandatos sobrevividos (sumando todas las partidas) se desbloquea la siguiente carta de UnlockOrder.
    //    Entra al sorteo del cierre desde la PRÓXIMA partida (CardDeck filtra el catálogo al arrancar) y se anuncia
    //    al volver al menú (Pending).
    //  · Con las 10 cartas desbloqueadas, cada 3 mandatos suma una mejora permanente (alterna +$250 iniciales y
    //    +1 de gobernabilidad máxima).
    public static class Meta
    {
        const string Key = "meta_v1";
        public const int MandatesPerUnlock = 3;

        public static readonly string[] UnlockOrder =
            { "puntero", "contenedores", "infiltrado", "comisario", "concertina", "requisa", "desvio", "estruendo", "drone", "tanqueta" };

        [System.Serializable]
        class Data
        {
            public int mandates;
            public int perks;
            public List<string> unlocked = new List<string>();
            public List<string> pending = new List<string>();
            public List<string> achievements = new List<string>();
            public List<string> keys = new List<string>();
            public List<int> vals = new List<int>();
        }

        static Data data;
        // sin recarga de dominio (editor) los estáticos sobreviven entre Play: se limpian al arrancar
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { data = null; dirty = false; Unlocked = null; }
        static Data D
        {
            get
            {
                if (data != null) return data;
                try { data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "")); } catch { data = null; }
                if (data == null) data = new Data();
                data.unlocked ??= new List<string>(); data.pending ??= new List<string>(); data.achievements ??= new List<string>();
                data.keys ??= new List<string>(); data.vals ??= new List<int>();
                return data;
            }
        }

        static bool dirty;
        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
            dirty = false;
        }
        static void MarkDirty() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(D)); dirty = true; }
        public static void SaveIfDirty() { if (dirty) Save(); }

        // ------------------------------------------------------------------ mandatos y desbloqueos

        public static int TotalMandates => D.mandates;
        public static int UnlockedCount => D.unlocked.Count;
        public static int Perks => D.perks;
        public static bool AllUnlocked => D.unlocked.Count >= UnlockOrder.Length;
        public static bool IsLockable(string id) => System.Array.IndexOf(UnlockOrder, id) >= 0;
        public static bool IsUnlocked(string id) => !IsLockable(id) || D.unlocked.Contains(id);
        public static string NextUnlock => AllUnlocked ? null : UnlockOrder[D.unlocked.Count];
        // mandatos que faltan para la próxima recompensa (carta o mejora)
        public static int ToNextReward => MandatesPerUnlock - D.mandates % MandatesPerUnlock;

        // Mejoras permanentes: la 1ª, 3ª, 5ª… suman $250 iniciales; la 2ª, 4ª… +1 de gobernabilidad máxima.
        public static int StartMoneyBonus => 250 * ((D.perks + 1) / 2);
        public static int MaxGovBonus => D.perks / 2;

        public static event System.Action<string> Unlocked;

        // Se llama al cerrar cada mandato. Devuelve la recompensa nueva (id de carta o "perk:N"), o null.
        public static string MandateSurvived()
        {
            D.mandates++;
            string got = null;
            int earned = D.mandates / MandatesPerUnlock;
            while (D.unlocked.Count + D.perks < earned)
            {
                if (!AllUnlocked)
                {
                    got = UnlockOrder[D.unlocked.Count];
                    D.unlocked.Add(got);
                }
                else { D.perks++; got = "perk:" + D.perks; }
                D.pending.Add(got);
                Unlocked?.Invoke(got);
            }
            Save();
            if (AllUnlocked) Achievements.Unlock("coleccionista");
            return got;
        }

        // Recompensas ganadas que todavía no se mostraron (se muestran al volver al menú).
        public static List<string> TakePending()
        {
            var l = new List<string>(D.pending);
            D.pending.Clear();
            Save();
            return l;
        }

        // ------------------------------------------------------------------ contadores

        public static int Count(string key)
        {
            int i = D.keys.IndexOf(key);
            return i < 0 ? 0 : D.vals[i];
        }

        public static int Add(string key, int n = 1)
        {
            int i = D.keys.IndexOf(key);
            if (i < 0) { D.keys.Add(key); D.vals.Add(n); MarkDirty(); return n; }
            D.vals[i] += n;
            MarkDirty();
            return D.vals[i];
        }

        public static void Max(string key, int v)
        {
            if (v > Count(key)) Add(key, v - Count(key));
        }

        // ------------------------------------------------------------------ logros

        public static bool HasAchievement(string id) => D.achievements.Contains(id);
        internal static bool AddAchievement(string id)
        {
            if (D.achievements.Contains(id)) return false;
            D.achievements.Add(id);
            Save();
            return true;
        }
        public static int AchievementCount => D.achievements.Count;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Argensimia/Debug/Borrar progresión y logros")]
        static void ResetAll() { data = new Data(); Save(); Debug.Log("Meta: progresión borrada"); }
#endif
        // Para pruebas desde el editor: suma mandatos sobrevividos.
        public static void DebugAddMandates(int n) { for (int i = 0; i < n; i++) MandateSurvived(); }
    }
}
