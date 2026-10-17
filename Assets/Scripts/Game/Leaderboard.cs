using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Ranking local de partidas (top 10), guardado en PlayerPrefs como JSON.
    public static class Leaderboard
    {
        public const int Size = 5;
        const string Key = "leaderboard_v1", KeyName = "leaderboard_last_name";

        [System.Serializable]
        public class Entry
        {
            public string name;
            public int score;
            public int mandates;
            public string date;
        }

        [System.Serializable]
        class Data { public List<Entry> list = new List<Entry>(); }

        static Data data;
        static Data D
        {
            get
            {
                if (data != null) return data;
                try { data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "")); } catch { data = null; }
                if (data == null || data.list == null) data = new Data();
                data.list.Sort((a, b) => b.score.CompareTo(a.score));
                return data;
            }
        }

        public static IReadOnlyList<Entry> Top => D.list;
        public static string LastName { get => PlayerPrefs.GetString(KeyName, ""); set => PlayerPrefs.SetString(KeyName, value); }

        public static bool Qualifies(int score) => score > 0 && (D.list.Count < Size || score > D.list[D.list.Count - 1].score);

        // Agrega y devuelve la posición (0 = primero), o -1 si no entró.
        public static int Add(string name, int score, int mandates)
        {
            if (!Qualifies(score)) return -1;
            var e = new Entry { name = string.IsNullOrWhiteSpace(name) ? "Anónimo" : name.Trim(), score = score, mandates = mandates, date = System.DateTime.Now.ToString("dd/MM/yy") };
            int i = 0;
            while (i < D.list.Count && D.list[i].score >= score) i++;
            D.list.Insert(i, e);
            if (D.list.Count > Size) D.list.RemoveRange(Size, D.list.Count - Size);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
            return i;
        }
    }
}
