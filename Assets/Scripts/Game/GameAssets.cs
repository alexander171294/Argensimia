using UnityEngine;

namespace Argensimia
{
    // Referencias a modelos y materiales para lo que se instancia en runtime (lo llena HudSetup, menú 4).
    public class GameAssets : ScriptableObject
    {
        public GameObject[] people;
        public GameObject officer, truck, valla, flagArg, flagRed, banner;
        public Material[] shirts, pants, skins, hairs;
        [Tooltip("URP Lit base para props primitivos (se instancia)")] public Material lit;
        [Tooltip("URP Unlit base (barras de vida)")] public Material unlit;
        public Material smoke, fire, water;

        static GameAssets inst;
        public static GameAssets I
        {
            get
            {
                if (inst == null) inst = Resources.Load<GameAssets>("GameAssets");
                return inst;
            }
        }

        public Material Lit(Color c)
        {
            var m = new Material(lit);
            m.SetColor("_BaseColor", c);
            return m;
        }
    }
}
