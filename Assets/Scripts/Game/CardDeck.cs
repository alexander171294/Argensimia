using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Una carta concreta del mazo: definición + nivel (fusión: 2 iguales del mismo nivel = 1 de nivel +1).
    public class CardInst
    {
        public CardDef def;
        public int level = 1;
        public CardInst(CardDef d, int l = 1) { def = d; level = l; }
    }

    // Mazo: pila de robo + descarte. Cuando se vacía el robo, se rebaraja el descarte.
    public class CardDeck : MonoBehaviour
    {
        public int handSize = 5;
        [Tooltip("Costo de descartar toda la mano y robar una nueva")] public int renewCost = 300;
        [Tooltip("Vacío = carga todo Resources/Cards")] public List<CardDef> catalog = new List<CardDef>();
        public int maxLevel = 3;

        readonly List<CardInst> drawPile = new List<CardInst>();
        readonly List<CardInst> discardPile = new List<CardInst>();

        public int DrawCount => drawPile.Count;
        public int DiscardCount => discardPile.Count;

        void Awake()
        {
            if (catalog.Count == 0) catalog.AddRange(Resources.LoadAll<CardDef>("Cards"));
            foreach (var c in catalog)
            {
                if (c.kind == CardKind.Crisis) continue;
                for (int i = 0; i < c.copies; i++) drawPile.Add(new CardInst(c));
            }
            Shuffle(drawPile);
        }

        public CardInst Draw()
        {
            if (drawPile.Count == 0)
            {
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
            }
            if (drawPile.Count == 0) return null;
            var c = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);
            return c;
        }

        // Crisis y ministros no vuelven al mazo.
        public void Discard(CardInst c)
        {
            if (c == null || c.def == null) return;
            if (c.def.kind == CardKind.Crisis || c.def.kind == CardKind.Ministro) return;
            discardPile.Add(c);
        }

        public void AddToDeck(CardDef d) { if (d) discardPile.Add(new CardInst(d)); }

        // Cartas que pueden salir en el 1 de 3, ponderadas por rareza (más raras desde el mandato 4).
        public List<CardDef> RollPicks(int count, int mandate)
        {
            var pool = catalog.FindAll(c => c.inPool && c.kind != CardKind.Crisis);
            var res = new List<CardDef>();
            float rareBoost = mandate >= 4 ? 1.8f : 1f;
            for (int k = 0; k < count && pool.Count > 0; k++)
            {
                float total = 0;
                foreach (var c in pool) total += Weight(c, rareBoost);
                float r = Random.value * total;
                CardDef pick = pool[pool.Count - 1];
                foreach (var c in pool) { r -= Weight(c, rareBoost); if (r <= 0) { pick = c; break; } }
                res.Add(pick);
                pool.Remove(pick);
            }
            return res;
        }

        static float Weight(CardDef c, float rareBoost) =>
            c.rarity == Rarity.Comun ? 60 : c.rarity == Rarity.PocoComun ? 30 : 10 * rareBoost;

        public List<CardDef> Crises() => catalog.FindAll(c => c.kind == CardKind.Crisis);

        static void Shuffle(List<CardInst> l)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (l[i], l[j]) = (l[j], l[i]);
            }
        }
    }
}
