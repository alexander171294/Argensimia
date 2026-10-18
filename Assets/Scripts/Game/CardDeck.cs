using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Una carta concreta del mazo. El nivel de las colocables ya no se sube en la mano: se sube en el mapa
    // soltando la carta sobre una defensa igual (ver Defense.Upgrade).
    public class CardInst
    {
        public CardDef def;
        public int level = 1;
        public CardInst(CardDef d, int l = 1) { def = d; level = l; }
    }

    // Mazo finito: arranca con las copias iniciales (10) y no se rebaraja. Lo que se juega se consume;
    // cuando se vacía se pueden comprar cartas al azar (GameHudShop). Cada cierre de mandato suma 3 (ver GameHud.ShowClose).
    public class CardDeck : MonoBehaviour
    {
        public int handSize = 5;
        [Tooltip("Costo de devolver la mano al fondo del mazo y robar una nueva")] public int renewCost = 300;
        [Tooltip("Cartas que entran al mazo al cerrar cada mandato")] public int rewardCount = 3;
        [Tooltip("Costo de cambiar una carta del cierre por otra al azar (1 vez por carta)")] public int rerollCost = 100;
        [Tooltip("Mazo vacío: comprar 1 carta al azar")] public int buyCost = 500;
        [Tooltip("Mazo vacío: comprar 10 cartas al azar (descuento por cantidad)")] public int buy10Cost = 4200;
        [Tooltip("Vacío = carga todo Resources/Cards")] public List<CardDef> catalog = new List<CardDef>();
        public int maxLevel = 3;

        readonly List<CardInst> drawPile = new List<CardInst>(); // el tope es el final de la lista

        public int DrawCount => drawPile.Count;
        public int StartCount { get; private set; }

        void Awake()
        {
            if (catalog.Count == 0) catalog.AddRange(Resources.LoadAll<CardDef>("Cards"));
            // las desbloqueables entran recién en la partida siguiente a ganarlas (Meta se consulta al arrancar)
            catalog.RemoveAll(c => !c || !Meta.IsUnlocked(c.id));
            foreach (var c in catalog)
            {
                if (c.kind == CardKind.Crisis) continue;
                for (int i = 0; i < c.copies; i++) drawPile.Add(new CardInst(c));
            }
            StartCount = drawPile.Count;
            Shuffle(drawPile);
        }

        public CardInst Draw()
        {
            if (drawPile.Count == 0) return null;
            var c = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);
            return c;
        }

        // Una carta que vuelve sin jugarse (CAMBIAR MANO, la que pisa una crisis) va al fondo del mazo.
        // Crisis no vuelven nunca.
        public void ReturnToDeck(CardInst c)
        {
            if (c == null || c.def == null || c.def.kind == CardKind.Crisis) return;
            drawPile.Insert(0, c);
        }

        // Carta nueva: entra en un lugar al azar del mazo.
        public void AddToDeck(CardDef d)
        {
            if (!d) return;
            drawPile.Insert(Random.Range(0, drawPile.Count + 1), new CardInst(d));
        }

        // Cartas del cierre, ponderadas por rareza (más raras desde el mandato 4). Sin repetir dentro de la tanda.
        public List<CardDef> RollPicks(int count, int mandate)
        {
            var res = new List<CardDef>();
            for (int k = 0; k < count; k++)
            {
                var c = RollOne(mandate, res);
                if (!c) break;
                res.Add(c);
            }
            return res;
        }

        // Una carta al azar que no esté en exclude (para el CAMBIAR de cada carta del cierre).
        public CardDef RollOne(int mandate, ICollection<CardDef> exclude)
        {
            var pool = catalog.FindAll(c => c.inPool && c.kind != CardKind.Crisis && (exclude == null || !exclude.Contains(c)));
            if (pool.Count == 0) return null;
            float rareBoost = mandate >= 4 ? 1.8f : 1f;
            float total = 0;
            foreach (var c in pool) total += Weight(c, rareBoost);
            float r = Random.value * total;
            foreach (var c in pool) { r -= Weight(c, rareBoost); if (r <= 0) return c; }
            return pool[pool.Count - 1];
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
