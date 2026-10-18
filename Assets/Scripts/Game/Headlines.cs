using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Banco de titulares de la marquesina ÚLTIMO MOMENTO. Cada evento tiene varias variantes: se elige una al azar
    // sin repetir la última, y cada evento tiene un enfriamiento para no llenar la marquesina con lo mismo.
    // {0}, {1}… se reemplazan con los argumentos (nombre de la carta, nivel, etc.).
    public static class Headlines
    {
        static readonly Dictionary<string, string[]> pool = new Dictionary<string, string[]>
        {
            ["wave_start"] = new[]
            {
                "Arranca el mandato {0}: columnas avanzan por Av. de Mayo",
                "Mandato {0}: los manifestantes se concentran en Av. de Mayo",
                "Comienza el mandato {0} y la Plaza vuelve a llenarse",
                "Mandato {0}: «estamos preparados», aseguran en Casa Rosada",
                "Primeras columnas del mandato {0} llegan desde el Congreso",
                "Mandato {0}: cortes y bombos en el microcentro",
            },
            ["wear"] = new[] { "Desgaste de gestión: la imagen del Gobierno cae {0}% al arrancar el mandato", "Encuesta: la gente se cansa del Gobierno (-{0}%)", "«El que gobierna se desgasta», admiten en Casa Rosada (-{0}%)" },
            ["infiltrado"] = new[] { "Un policía de civil detiene a un manifestante en plena marcha", "Denuncian infiltrados entre los manifestantes", "«Parecía uno más», cuenta un testigo de la detención" },
            ["requisa"] = new[] { "Requisa en la Plaza: secuestran palos y morteros caseros", "Desarman a un grupo de encapuchados en los accesos" },
            ["desvio"] = new[] { "Desvían a los manifestantes por una calle lateral", "Operativo de tránsito: la marcha cambia de recorrido" },
            ["estruendo"] = new[] { "Estruendo en la Plaza: corridas y confusión", "Una granada de estruendo dispersa a los manifestantes" },
            ["tanqueta"] = new[] { "La tanqueta dispara espuma sobre la Plaza", "Espuma antidisturbios: la Plaza parece un lavadero" },
            ["wave_final"] = new[]
            {
                "ESTALLIDO: todas las columnas marchan sobre la Casa Rosada",
                "Movilización histórica: la Plaza desborda de gente",
                "Último mandato: la oposición convoca a una marcha masiva",
            },
            ["victory"] = new[]
            {
                "Terminó el período: el Gobierno completa su mandato",
                "Histórico: el Presidente termina el mandato en la Casa Rosada",
            },
            ["mood_up"] = new[]
            {
                "La calle se calienta: las columnas llegan más enojadas",
                "Crece el malestar social y la Plaza se pone tensa",
                "Encuesta: se desploma la imagen del Gobierno",
                "Cacerolazos en los barrios: el humor social empeora",
                "La oposición habla de «clima de fin de ciclo»",
            },
            ["mood_down"] = new[]
            {
                "Baja la tensión en la Plaza de Mayo",
                "Encuesta: repunta la imagen del Gobierno",
                "Clima más tranquilo en las calles porteñas",
                "Se descomprime la Plaza: «la gente entendió», dicen en el Gobierno",
            },
            ["gov_lost"] = new[]
            {
                "Crisis institucional: el Gobierno pierde el control de la Plaza",
                "Caos en la Plaza: el Gobierno ya no controla la situación",
            },
            ["arrive"] = new[]
            {
                "Manifestantes llegan al vallado de la Casa Rosada",
                "Rompen el cerco: un grupo alcanza las rejas de Casa Rosada",
                "Tensión en las rejas: la gobernabilidad está en juego",
                "Golpean las vallas de la Casa Rosada: preocupación en el Gabinete",
                "«Así no se puede gobernar», se quejan en Balcarce 50",
            },
            ["column_break"] = new[]
            {
                "El hidrante dispersa una columna organizada: cada uno por su lado",
                "Agua fría para la columna: se desarma y sus militantes siguen sueltos",
                "Se rompe la columna frente al camión hidrante",
            },
            ["arrive_column"] = new[]
            {
                "Una columna organizada llega a las puertas de la Casa Rosada",
                "La columna sindical se planta frente al Gobierno",
            },
            ["jump"] = new[]
            {
                "Un encapuchado salta el cerco policial",
                "Encapuchados burlan el operativo y avanzan",
                "Un manifestante salta las vallas y sigue corriendo",
                "Video viral: un encapuchado esquiva a la policía",
            },
            ["tire"] = new[]
            {
                "Piqueteros prenden gomas sobre la Plaza",
                "Humo negro en la Plaza: arden neumáticos",
                "Corte con gomas: «no nos vamos a mover», advierten",
            },
            ["mortar"] = new[]
            {
                "Lanzan morteros contra las fuerzas de seguridad",
                "Pirotecnia y morteros: momentos de tensión en la Plaza",
                "Estruendo en la Plaza: atacan a la policía con morteros",
            },
            ["capture"] = new[]
            {
                "La brigada se lleva detenidos de la Plaza",
                "Detenciones en la Plaza: la oposición pide explicaciones",
                "Organismos de DDHH denuncian detenciones arbitrarias",
            },
            ["capture_jubilado"] = new[]
            {
                "Indignación: detienen a un jubilado en la Plaza",
                "Golpean a un jubilado y las redes estallan",
                "Repudio general por la represión a jubilados",
            },
            ["defense_break"] = new[]
            {
                "Retroceden las fuerzas de seguridad: cae la línea de {0}",
                "Desbordan a la policía: se rompe la línea de {0}",
                "Los manifestantes superan a {0}",
                "Golpe al operativo: se retira {0}",
            },
            ["fence_break"] = new[]
            {
                "Derriban un vallado en la Plaza",
                "Vuelan las vallas: los manifestantes abren paso",
            },
            ["upgrade"] = new[]
            {
                "Refuerzan la línea de {0}: ahora es nivel {1}",
                "Más efectivos para {0}: el operativo pasa a nivel {1}",
                "Seguridad blinda la línea de {0} (nivel {1})",
            },
            ["minister_swap"] = new[]
            {
                "Renuncia {0}: asume {1}",
                "Cambios en el Gabinete: sale {0}, entra {1}",
                "Se va {0} y llega {1}: «es una renovación», explican",
            },
            ["crisis_evaded"] = new[]
            {
                "El Gobierno logra desactivar la crisis: «la situación está bajo control»",
                "Desactivan la crisis a último momento: alivio en Casa Rosada",
                "Negociación exitosa: se evita la crisis",
                "«Hicimos lo que había que hacer», celebran en el Gobierno",
            },
            ["new_cards"] = new[]
            {
                "Nuevas medidas en carpeta: {0}",
                "El Gabinete prepara: {0}",
                "Trascendió que el Gobierno analiza: {0}",
            },
            ["close"] = new[]
            {
                "Termina el mandato: la policía se reagrupa y atiende a sus heridos",
                "Fin del mandato: relevan efectivos y reparan las vallas",
                "Cierra el mandato: el Gobierno respira y hace balance",
            },
            ["renew"] = new[]
            {
                "El Gobierno cambia el plan de acción",
                "Golpe de timón: Casa Rosada redefine su agenda",
                "«Vamos a reformular la estrategia», reconocen fuentes oficiales",
            },
            ["deck_empty"] = new[]
            {
                "Se quedan sin medidas: el Gobierno improvisa",
                "Cajones vacíos: ya no quedan proyectos en carpeta",
                "«Ya no se nos ocurre nada», admiten en off en el Gabinete",
            },
            ["buy"] = new[]
            {
                "El Gobierno contrata asesores: llega una medida nueva",
                "Consultora privada le vende una idea al Gabinete",
                "Compran un proyecto «llave en mano» para salir del paso",
            },
            ["buy10"] = new[]
            {
                "Paquete completo: el Gobierno compra diez medidas de una",
                "Megaplan: llegan diez proyectos nuevos a Casa Rosada",
                "«Compramos al por mayor y ahorramos», celebran en Hacienda",
            },
            ["reroll"] = new[]
            {
                "Marcha atrás: el Gobierno cambia una medida a último momento",
                "Cambian un proyecto antes de anunciarlo",
            },
        };

        // Relleno: titulares de clima + frases célebres de la política argentina.
        public static readonly string[] Filler =
        {
            "El Gobierno asegura que está todo controlado",
            "Fuentes oficiales: «la Plaza está tranquila»",
            "Cortes en Av. de Mayo: recomiendan evitar la zona",
            "El dólar abre en alza y el mercado mira a la Plaza",
            "Columnas avanzan desde el Congreso hacia Casa Rosada",
            "Encuesta: la imagen del Gobierno, en caída libre",
            "El vocero desmiente renuncias en el Gabinete",
            "El riesgo país vuelve a subir y el Gobierno habla de «ruido político»",
            "Reunión de Gabinete a puertas cerradas en Casa Rosada",
            "La CGT evalúa un nuevo plan de lucha",
            "Gobernadores piden más fondos: «sin plata no hay apoyo»",
            "Inflación del mes: «es el último dato malo», promete el Gobierno",
            "Taxistas y colectivos desvían recorridos por la Plaza",
            "El Congreso posterga la sesión por la movilización",
            "Recuerdan en Casa Rosada: «la casa está en orden»",
            "Un ministro repite el clásico «estamos mal pero vamos bien»",
            "En el Gabinete citan a Perón: «la única verdad es la realidad»",
            "Desde el Gobierno: «mejor que decir es hacer, mejor que prometer es realizar»",
            "Un funcionario pide «pasar el invierno»",
            "Optimismo oficial: «estamos condenados al éxito»",
            "El vocero insiste: «el que depositó dólares recibirá dólares»",
            "Polémica: un ministro dice que «el que apuesta al dólar pierde»",
            "En Casa Rosada repiten: «de casa al trabajo y del trabajo a casa»",
            "Un diputado oficialista: «para un argentino no hay nada mejor que otro argentino»",
            "El Gobierno promete que «lo peor ya pasó»",
            "Explicación oficial de la crisis: «pasaron cosas»",
            "El ministro de Economía, sincero: «no hay plata»",
            "Un funcionario anticipa que «se viene el segundo semestre»",
            "En la Plaza vuelve el grito «¡que se vayan todos!»",
            "Desde el Gobierno: «con la democracia se come, se cura y se educa»",
            "Un ministro: «les hablé con el corazón y me contestaron con el bolsillo»",
            "Frase de un asesor: «si decía lo que iba a hacer, no me votaba nadie»",
            "Un vocero pide «paciencia, que ya llega la lluvia de inversiones»",
            "Desde el Gabinete: «la patria es el otro»",
            "Oficialismo y oposición coinciden: «nadie se salva solo»",
            "Llamado presidencial: «síganme, no los voy a defraudar»",
            "La oposición advierte: «ramal que para, ramal que cierra»",
        };

        static readonly Dictionary<string, int> last = new Dictionary<string, int>();
        static readonly Dictionary<string, float> nextAllowed = new Dictionary<string, float>();
        static readonly List<int> fillerBag = new List<int>();

        public static string Pick(string key, params object[] args)
        {
            string[] v = null;
            if (L.En) L.Headlines.TryGetValue(key, out v); // en inglés, si hay banco propio
            if (v == null && !pool.TryGetValue(key, out v)) return null;
            if (v.Length == 0) return null;
            return string.Format(PickOne(key, v), args);
        }

        // Variantes separadas por "|" (titular de una carta).
        public static string PickVariant(string variants)
        {
            if (string.IsNullOrEmpty(variants)) return null;
            if (variants.IndexOf('|') < 0) return variants;
            var v = variants.Split('|');
            return PickOne("card:" + v[0], v).Trim();
        }

        static string PickOne(string key, string[] v)
        {
            int prev = last.TryGetValue(key, out var p) ? p : -1;
            int i = Random.Range(0, v.Length);
            if (v.Length > 1 && i == prev) i = (i + 1 + Random.Range(0, v.Length - 1)) % v.Length;
            last[key] = i;
            return v[i];
        }

        // Titular con enfriamiento (s de tiempo real): para eventos que pasan muy seguido.
        public static void Say(string key, float cooldown, params object[] args)
        {
            if (!GameState.I) return;
            float now = Time.unscaledTime;
            if (cooldown > 0 && nextAllowed.TryGetValue(key, out var t) && now < t) return;
            nextAllowed[key] = now + cooldown;
            var h = Pick(key, args);
            if (h != null) GameState.I.PushHeadline(h);
        }

        public static void Say(string key, params object[] args) => Say(key, 0, args);

        // Relleno sin repetir hasta agotar la lista (bolsa barajada).
        public static string NextFiller()
        {
            var filler = L.En ? L.Filler : Filler;
            if (fillerBag.Count == 0 || fillerBag[fillerBag.Count - 1] >= filler.Length) fillerBag.Clear();
            if (fillerBag.Count == 0)
            {
                for (int i = 0; i < filler.Length; i++) fillerBag.Add(i);
                for (int i = fillerBag.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (fillerBag[i], fillerBag[j]) = (fillerBag[j], fillerBag[i]); }
            }
            int k = fillerBag[fillerBag.Count - 1];
            fillerBag.RemoveAt(fillerBag.Count - 1);
            return filler[k];
        }
    }
}
