using System.Collections.Generic;
using UnityEngine;

namespace Argensimia
{
    // Idioma: español (original) o inglés. El texto se escribe en español en todo el código y L.T() lo cambia
    // al inglés si hay traducción (UiKit.Txt lo aplica solo a los textos fijos). Cambiar de idioma recarga la escena.
    public static class L
    {
        const string Key = "lang";
        static int en = -1;
        public static bool En
        {
            get { if (en < 0) en = PlayerPrefs.GetString(Key, "es") == "en" ? 1 : 0; return en == 1; }
            set { en = value ? 1 : 0; PlayerPrefs.SetString(Key, value ? "en" : "es"); PlayerPrefs.Save(); }
        }

        public static string T(string es)
        {
            if (!En || string.IsNullOrEmpty(es)) return es;
            return Ui.TryGetValue(es, out var t) ? t : es;
        }

        public static string F(string es, params object[] args) => string.Format(T(es), args);

        // ------------------------------------------------------------------ interfaz
        static readonly Dictionary<string, string> Ui = new Dictionary<string, string>
        {
            // menú y carga
            ["NUEVA PARTIDA"] = "NEW GAME",
            ["¡Defendé la Casa Rosada!"] = "Defend the Casa Rosada!",
            ["MÚSICA"] = "MUSIC",
            ["EFECTOS"] = "SOUND FX",
            ["CARGANDO"] = "LOADING",
            ["RANKING"] = "RANKING",
            ["MEJORES GOBIERNOS"] = "BEST GOVERNMENTS",
            ["Todavía no hay partidas.\n¡Sé el primero en aguantar!"] = "No games yet.\nBe the first to hold on!",
            [" mandato"] = " term",
            [" mandatos"] = " terms",
            ["¡ENTRASTE AL TOP 5!"] = "YOU MADE THE TOP 5!",
            ["Puntaje"] = "Score",
            ["Tu nombre"] = "Your name",
            ["GUARDAR"] = "SAVE",
            ["ESPAÑOL"] = "ESPAÑOL",
            ["SALIR"] = "QUIT",
            ["VENTANA"] = "WINDOWED",
            ["PANTALLA COMPLETA"] = "FULLSCREEN",
            ["ENGLISH"] = "ENGLISH",
            ["<color=#f2c14e>¡COMPLETASTE EL PERÍODO!</color>\nPopularidad final: {0}%"] = "<color=#f2c14e>TERM COMPLETED!</color>\nFinal popularity: {0}%",
            ["<color=#ff7a70>PERDISTE LA GOBERNABILIDAD</color>\nLa Plaza quedó en manos de la calle en el mandato {0}"] = "<color=#ff7a70>YOU LOST GOVERNABILITY</color>\nThe Plaza fell to the streets in term {0}",
            ["<color=#ff7a70>EL PRESIDENTE RENUNCIÓ</color>\nSe fue en helicóptero en el mandato {0}"] = "<color=#ff7a70>THE PRESIDENT RESIGNED</color>\nFled by helicopter in term {0}",
            ["  ·  Puntaje <color=#f2c14e>{0}</color>"] = "  ·  Score <color=#f2c14e>{0}</color>",
            // HUD
            ["ÚLTIMO MOMENTO"] = "BREAKING NEWS",
            ["PRESUPUESTO"] = "BUDGET",
            ["MANDATO"] = "TERM",
            ["MANDATO {0}"] = "TERM {0}",
            ["POPULARIDAD"] = "POPULARITY",
            ["GOBERNABILIDAD"] = "GOVERNABILITY",
            ["PRÓXIMA OLEADA"] = "NEXT WAVE",
            ["MANIFESTANTES RESTANTES"] = "PROTESTERS LEFT",
            ["¡ARRANCAR!"] = "START!",
            ["CAMBIAR\nMANO"] = "NEW\nHAND",
            ["ACTIVAR"] = "PLAY CARD",
            ["ACEPTAR"] = "ACCEPT",
            ["SOLO EN PREVIA"] = "PREP PHASE ONLY",
            ["SIN LUGAR"] = "NO ROOM",
            ["FALTA $"] = "NEED $",
            ["EVITAR $"] = "AVOID $",
            ["PAUSA"] = "PAUSE",
            ["CONTINUAR"] = "CONTINUE",
            ["RENUNCIAR"] = "RESIGN",
            ["MAZO VACÍO"] = "EMPTY DECK",
            ["1 CARTA"] = "1 CARD",
            ["10 CARTAS"] = "10 CARDS",
            ["Elegí un lugar en el camino para "] = "Pick a spot on the road for ",
            ["Elegí un lugar o tocá una igual para mejorarla: "] = "Pick a spot or tap a matching one to upgrade it: ",
            ["¡NIVEL {0}!"] = "LEVEL {0}!",
            ["CRISIS: SE APLICA\nAL ARRANCAR"] = "CRISIS: APPLIES\nON START",
            ["¡SUBILE EL NIVEL\nA UNA DEL MAPA!"] = "LEVEL UP ONE\nON THE MAP!",
            ["1 CRISIS SE APLICA AL ARRANCAR"] = "1 CRISIS APPLIES ON START",
            ["{0} CRISIS SE APLICAN AL ARRANCAR"] = "{0} CRISES APPLY ON START",
            ["CRISIS FORZADAS"] = "FORCED CRISES",
            ["Se van a aplicar las cartas de crisis forzadas. ¿Seguro que querés comenzar?\n<size=24>Podés evitarlas pagando desde la carta.</size>"] = "The forced crisis cards will be applied. Start anyway?\n<size=24>You can avoid them by paying from the card.</size>",
            ["VOLVER"] = "BACK",
            ["COMENZAR"] = "START",
            ["FIN DEL MANDATO {0}"] = "END OF TERM {0}",
            ["Sueldo <color=#7be36a>+${0}</color>    ·    Llegaron al vallado: <color=#ff7a70>{1}</color>    ·    Gobernabilidad {2}/{3}"] = "Salary <color=#7be36a>+${0}</color>    ·    Reached the fence: <color=#ff7a70>{1}</color>    ·    Governability {2}/{3}",
            ["    ·    Policía <color=#7be36a>+{0}% vida</color>"] = "    ·    Police <color=#7be36a>+{0}% health</color>",
            ["OBTUVISTE {0} CARTAS PARA TU MAZO"] = "YOU GOT {0} CARDS FOR YOUR DECK",
            ["CAMBIAR $"] = "SWAP $",
            ["CAMBIADA"] = "SWAPPED",
            ["¡COMPLETASTE EL PERÍODO!"] = "TERM COMPLETED!",
            ["PERDISTE LA GOBERNABILIDAD"] = "YOU LOST GOVERNABILITY",
            ["MODO ENDLESS"] = "ENDLESS MODE",
            ["JUGAR DE NUEVO"] = "PLAY AGAIN",
            ["REINTENTAR"] = "RETRY",
            ["También por norte y sur"] = "Also from north and south",
            [" GOB"] = " GOV",
            // carta
            ["ARTE PENDIENTE"] = "ART PENDING",
            ["GRATIS"] = "FREE",
            ["NV "] = "LV ",
            ["COLOCAR"] = "DEPLOY",
            ["EFECTO"] = "EFFECT",
            ["MINISTRO"] = "MINISTER",
            ["CRISIS"] = "CRISIS",
            ["COMÚN"] = "COMMON",
            ["POCO COMÚN"] = "UNCOMMON",
            ["RARA"] = "RARE",
            ["VIDA"] = "HEALTH",
            ["DAÑO"] = "DAMAGE",
            ["RITMO"] = "RATE",
            ["ALCANCE"] = "RANGE",
            [" cas."] = " tiles",
            [" popularidad"] = " popularity",
            ["%/mandato"] = "%/term",
            [" sueldo"] = " salary",
            [" gobernabilidad"] = " governability",
            [" gobernabilidad (y máx.)"] = " governability (and max)",
            [" gob. máx."] = " max gov.",
            ["% vida policía"] = "% police health",
            ["% del dinero"] = "% of your money",
            [" columna"] = " column",
            ["oleada -"] = "wave -",
            [" gob./mandato"] = " gov./term",
            // progresión y logros
            ["PLATA"] = "CASH",
            ["REFUERZO"] = "BOOST",
            ["ATURDE"] = "STUN",
            ["PLAZA"] = "PLAZA",
            ["LOGROS"] = "AWARDS",
            ["¡BIEN!"] = "GREAT!",
            ["¡MEJORA PERMANENTE!"] = "PERMANENT UPGRADE!",
            ["¡NUEVA CARTA DESBLOQUEADA!"] = "NEW CARD UNLOCKED!",
            ["Sobreviviste {0} mandatos en total."] = "You have survived {0} terms in total.",
            ["+$250 AL EMPEZAR"] = "+$250 AT START",
            ["+1 GOBERNABILIDAD MÁX."] = "+1 MAX GOVERNABILITY",
            ["Cada partida nueva arranca con más plata."] = "Every new game starts with more money.",
            ["Cada partida nueva arranca con más gobernabilidad."] = "Every new game starts with more governability.",
            ["Desde tu próxima partida puede salir en las cartas del cierre de cada mandato."] = "From your next game on, it can show up among the end-of-term cards.",
            ["Ya tenés todas las cartas: cada {0} mandatos ganás una mejora permanente."] = "You have every card: every {0} terms you earn a permanent upgrade.",
            ["Próxima carta en {0} mandatos."] = "Next card in {0} terms.",
            ["Mandatos sobrevividos en total: <color=#f2c14e>{0}</color>"] = "Terms survived in total: <color=#f2c14e>{0}</color>",
            ["Mejoras permanentes: <color=#f2c14e>{0}</color> (+${1} al empezar, +{2} gob. máx.)"] = "Permanent upgrades: <color=#f2c14e>{0}</color> (+${1} at start, +{2} max gov.)",
            ["Próxima carta en <color=#f2c14e>{0}</color> mandatos"] = "Next card in <color=#f2c14e>{0}</color> terms",
            ["{0} mand."] = "{0} terms",
            // estado de ánimo
            ["TRANQUI"] = "CALM",
            ["TENSO"] = "TENSE",
            ["CALLE CALIENTE"] = "HEATED STREETS",
            ["ESTALLIDO"] = "UPRISING",
        };

        // ------------------------------------------------------------------ cartas (título, descripción, titular)
        static readonly Dictionary<string, (string t, string d, string h)> Cards = new Dictionary<string, (string, string, string)>
        {
            ["puntero"] = ("Broker", "The neighborhood political broker sets up his grill in the Plaza: while he stands he makes $12/s during the wave. He's fragile: protect him.", "Free choripán in the Plaza: «it's a solidarity day»"),
            ["contenedores"] = ("Dumpsters", "Two trash dumpsters across the lane: they take a huge beating and hooded rioters can't jump them.", "Dumpsters become a barricade in front of the Casa Rosada"),
            ["infiltrado"] = ("Undercover", "A plainclothes cop in the crowd: takes 10 s to settle in and nobody sees him. Then the first one to pass gets arrested and those around are stunned. Single use; hits columns hard.", "Undercover police reported among the protesters"),
            ["comisario"] = ("Commissioner", "The commissioner rallies through a megaphone: defenses within 1 tile hit 30% harder (45% and 60% at LV2 and LV3). He also blocks.", "The commissioner takes command in the Plaza"),
            ["concertina"] = ("Razor Wire", "Coils of wire on the ground: they block nobody, but they hurt and slow everyone who crosses. Nobody can hit them.", "Razor wire goes up around the Plaza"),
            ["requisa"] = ("Search", "Every 8 s it disarms the most dangerous special within 2 tiles: hooded rioters, picketers and mortar guys become regular protesters.", "Police search bags at the Plaza entrances"),
            ["desvio"] = ("Detour", "A traffic officer with cones and barriers sends arrivals to the best-defended neighboring lane. Nobody hits him, but it doesn't work on organized columns.", "The march is diverted down a side street"),
            ["estruendo"] = ("Flash-Bang", "Stun grenade: it goes off after a second and stuns everyone within 6 m for 3 s, with some damage. Single use.", "A flash-bang shakes Plaza de Mayo"),
            ["drone"] = ("Drone", "Hovers over its slot and fires at any lane up to 3 tiles away, specials first. Blocks nobody; mortar guys can shoot it down.", "A police drone flies over the Plaza"),
            ["tanqueta"] = ("Riot Tank", "Armored riot vehicle: blocks, takes a huge beating and every 16 s fires foam at the densest group in the whole Plaza (area damage, soaks and slows 70%). Takes 2 slots.", "The riot tank rolls into the Plaza"),
            ["vallado"] = ("Barricade", "A line of fences at the back of the slot. Stops everyone until it's torn down; hooded rioters break it three times faster. Rubber bullets can fire from behind it.", "Police fence off the Plaza"),
            ["infanteria"] = ("Infantry", "Cheap tank: a line of shield officers that stops everyone and hits them in melee. Can't reach behind a barricade.", "Riot infantry forms a line in the Plaza"),
            ["balas_goma"] = ("Shotguns", "Shieldless officers with riot shotguns. They fire rubber bullets down their lane; behind a barricade they shoot whoever is hitting it first. They also stop anyone who arrives.", "Rubber bullets fly over the Plaza"),
            ["lanzagases"] = ("Tear Gas", "An officer with a grenade launcher: fires gas at the densest group, in his lane or the neighboring ones. Damage over time in an area and -40% speed.", "Tear gas clouds cover the Plaza"),
            ["detencion"] = ("Arrests", "Arrest squad: every 4 s it takes a nearby protester out of the game. Prefers specials; can't handle columns.", "Arrest squad takes protesters away"),
            ["motorizada"] = ("Motorcycles", "Every 15 s a pass charges down to Av. de Mayo: hits, pushes 3 m and stuns 1 s. Another one is already reloading in the slot, which also blocks and takes hits.", "Police motorcycles charge the protesters"),
            ["hidrante"] = ("Water Cannon", "A water cannon aiming at the nearest target, in its lane or the neighboring ones: soaks, pushes and slows 60%. 8 s tank, 6 s reload. Takes 2 slots.", "The water cannon truck enters the Plaza"),
            ["impuestazo"] = ("Tax Hike", "The tax agency goes collecting. Money comes in, but the streets heat up and columns arrive angrier.", "Massive tax hike announced"),
            ["maquinita"] = ("Money Printer", "The Central Bank prints: money now, but inflation is felt in the streets.", "The money printer runs at full speed"),
            ["cadena_nacional"] = ("National Address", "A presidential message on every channel. Popularity goes up a little.", "The President speaks on national TV"),
            ["futbol"] = ("Free Football", "Free matches on public TV. People stay home, but it's expensive to keep up.", "Football is free on TV again"),
            ["plan_platita"] = ("Cash Handouts", "Funds are handed out. Popularity rises: fewer people on the streets and calmer columns.", "The Government hands out cash"),
            ["blanqueo"] = ("Tax Amnesty", "An amnesty so money enters the system. Income per second goes up, at some political cost.", "Tax amnesty: undeclared money comes back"),
            ["aguinaldo"] = ("Bonus Pay", "An extra half-salary bonus for everyone: popularity rises a lot and income per second drops.", "Surprise bonus for all workers"),
            ["operativo"] = ("Relief", "Surprise operation, fresh troops in the Plaza: all police recover 40% health instantly. People don't like it.", "Fresh troops relieve the police line"),
            ["decretazo"] = ("Decree", "A decree by the back door: money comes in, but you lose governability (never down to 0).", "The President signs a sweeping decree"),
            ["feriado_puente"] = ("Long Weekend", "Half the country heads to the coast: the next wave is 20% smaller. Prep phase only.", "Long weekend: the city empties out"),
            ["dolar_soja"] = ("Soy Dollar", "Special exchange rate for farmers: a lot of money comes in at once. Prep phase only.", "Soy dollar: farm exports pour in"),
            ["prestamo"] = ("IMF Loan", "Dollars come in now, in exchange for austerity: income per second drops during waves.", "New IMF loan approved"),
            ["refuerzo_federal"] = ("Military Support", "Military troops arrive in the capital: +3 governability and +3 max. People don't like it at all.", "Military trucks roll into the capital"),
            ["privatizacion"] = ("Privatization", "A state company is sold: lots of money at once, but you lose a source of income and the streets get angry.", "A state company is up for sale"),
            ["acuerdo_cgt"] = ("Social Pact", "Deal with the unions: the next wave is 30% smaller, in exchange for concessions that lower income. Prep phase only.", "Government and unions sign a social pact"),
            ["min_economia"] = ("Economy Min.", "Passive minister: +$25/s during waves, but each term costs 2% popularity.", "New Economy Minister takes office"),
            ["min_interior"] = ("Interior Min.", "Passive minister: negotiates with governors and recovers 1 governability at the end of each term. Transfers lower income.", "New Interior Minister takes office"),
            ["vocero"] = ("Spokesperson", "Passive minister: the presidential spokesperson holds daily press conferences. +2% popularity per term; official ads cost $8/s.", "New presidential spokesperson"),
            ["super_ministro"] = ("Super Minister", "Passive minister: runs Economy, Production and Treasury. +$50/s during waves and +$500 salary per term, but austerity costs 3% popularity per term.", "A Super Minister takes over the economy"),
            ["min_seguridad"] = ("Security Min.", "Passive minister: police get +20% health and recover up to 30% (instead of 25%) at the end of each term. Iron fist costs -1% popularity per term.", "New Security Minister promises order"),
            ["min_desarrollo"] = ("Welfare Min.", "Passive Social Development minister: +3% popularity every term.", "New Social Development Minister"),
            ["devaluacion"] = ("Devaluation", "The dollar jumps: you lose 30% of your money. Accept it or pay to avoid it.", "Devaluation: the peso collapses"),
            ["escandalo"] = ("Scandal", "A compromising audio leaks: -15% popularity.", "Scandal: a compromising audio leaks"),
            ["paro_general"] = ("General Strike", "The unions stop the country: +1 organized column in the next wave.", "General strike: the country stops"),
            ["tarifazo"] = ("Utility Hike", "Electricity, gas and transport go up: income per second drops.", "Utility bills skyrocket"),
        };

        public static string CardTitle(CardDef d) => En && d && Cards.TryGetValue(d.id, out var c) ? c.t : d ? d.title : "";
        public static string CardDesc(CardDef d) => En && d && Cards.TryGetValue(d.id, out var c) ? c.d : d ? d.description : "";
        public static string CardHeadline(CardDef d) => En && d && Cards.TryGetValue(d.id, out var c) ? c.h : d ? d.headline : null;

        // ------------------------------------------------------------------ manifestantes (singular, plural)
        static readonly Dictionary<UnitKind, (string one, string many)> Units = new Dictionary<UnitKind, (string, string)>
        {
            [UnitKind.Comun] = ("Protester", "Protesters"),
            [UnitKind.Cacerolero] = ("Pot-banger", "Pot-bangers"),
            [UnitKind.Jubilado] = ("Retiree", "Retirees"),
            [UnitKind.Encapuchado] = ("Hooded rioter", "Hooded rioters"),
            [UnitKind.Piquetero] = ("Picketer", "Picketers"),
            [UnitKind.Mortero] = ("Mortar guy", "Mortar guys"),
            [UnitKind.Columna] = ("Organized column", "Organized columns"),
        };
        public static string UnitName(UnitDef u, bool plural, System.Func<string, string> pluralEs)
        {
            if (En && Units.TryGetValue(u.kind, out var n)) return plural ? n.many : n.one;
            return plural ? pluralEs(u.title) : u.title;
        }

        // ------------------------------------------------------------------ titulares en inglés
        public static readonly Dictionary<string, string[]> Headlines = new Dictionary<string, string[]>
        {
            ["wave_start"] = new[] { "Term {0} begins: columns advance down Av. de Mayo", "Term {0}: protesters gather on Av. de Mayo", "Term {0}: «we are ready», says the Casa Rosada" },
            ["infiltrado"] = new[] { "An undercover cop arrests a protester mid-march", "Infiltrators reported among the protesters" },
            ["requisa"] = new[] { "Police search: sticks and homemade mortars seized", "Hooded rioters disarmed at the entrances" },
            ["desvio"] = new[] { "Protesters diverted down a side street", "Traffic operation: the march changes its route" },
            ["estruendo"] = new[] { "Flash-bang in the Plaza: panic and confusion", "A stun grenade scatters the protesters" },
            ["tanqueta"] = new[] { "The riot tank sprays foam over the Plaza", "Riot foam: the Plaza looks like a car wash" },
            ["wave_final"] = new[] { "UPRISING: every column marches on the Casa Rosada", "Historic turnout: the Plaza overflows" },
            ["victory"] = new[] { "The Government completes its term", "Historic: the President finishes the term in office" },
            ["mood_up"] = new[] { "Streets heat up: columns arrive angrier", "Poll: the Government's approval collapses", "Pot-banging in the neighborhoods: the mood worsens" },
            ["mood_down"] = new[] { "Tension eases in Plaza de Mayo", "Poll: the Government's approval bounces back", "Calmer streets: «people understood», says the Government" },
            ["gov_lost"] = new[] { "Institutional crisis: the Government loses control of the Plaza", "Chaos in the Plaza: the Government is no longer in control" },
            ["arrive"] = new[] { "Protesters reach the Casa Rosada fence", "Tension at the gates: governability at stake", "«You can't govern like this», complains the cabinet" },
            ["arrive_column"] = new[] { "An organized column reaches the Casa Rosada gates", "The union column stands in front of the Government" },
            ["jump"] = new[] { "A hooded rioter jumps the police line", "Viral video: a hooded rioter dodges the police" },
            ["tire"] = new[] { "Picketers set tires on fire in the Plaza", "Black smoke in the Plaza: tires are burning" },
            ["mortar"] = new[] { "Mortars fired at the security forces", "Fireworks and mortars: tense moments in the Plaza" },
            ["capture"] = new[] { "The arrest squad takes protesters away", "Arrests in the Plaza: the opposition demands answers" },
            ["capture_jubilado"] = new[] { "Outrage: a retiree arrested in the Plaza", "A retiree is beaten and social media explodes" },
            ["defense_break"] = new[] { "Security forces fall back: the {0} line breaks", "Protesters overrun the {0}" },
            ["fence_break"] = new[] { "A barricade is torn down in the Plaza", "Fences fly: protesters break through" },
            ["upgrade"] = new[] { "The {0} line is reinforced: now level {1}", "More officers for {0}: the operation reaches level {1}" },
            ["minister_swap"] = new[] { "{0} resigns: {1} takes office", "Cabinet shuffle: out goes {0}, in comes {1}" },
            ["crisis_evaded"] = new[] { "The Government defuses the crisis: «everything is under control»", "Crisis averted at the last minute: relief at the Casa Rosada" },
            ["new_cards"] = new[] { "New measures in the works: {0}", "The Government is said to be considering: {0}" },
            ["close"] = new[] { "Term ends: police regroup and tend to their wounded", "End of term: fresh officers and fences repaired" },
            ["renew"] = new[] { "The Government changes its plan of action", "«We'll rethink the strategy», officials admit" },
            ["deck_empty"] = new[] { "Out of ideas: the Government improvises", "Empty drawers: no projects left in the pipeline" },
            ["buy"] = new[] { "The Government hires consultants: a new measure arrives", "A private firm sells an idea to the cabinet" },
            ["buy10"] = new[] { "Bulk deal: the Government buys ten measures at once", "Megaplan: ten new projects arrive at the Casa Rosada" },
            ["reroll"] = new[] { "U-turn: the Government swaps a measure at the last minute", "A project is changed before being announced" },
        };

        public static readonly string[] Filler =
        {
            "The Government says everything is under control",
            "Officials: «the Plaza is calm»",
            "Roadblocks on Av. de Mayo: avoid the area",
            "The dollar opens higher and markets watch the Plaza",
            "Poll: the Government's image in free fall",
            "The spokesperson denies cabinet resignations",
            "Country risk climbs again; the Government blames «political noise»",
            "The unions consider a new plan of action",
            "Governors ask for more funds: «no money, no support»",
            "Monthly inflation: «the last bad number», the Government promises",
            "Congress postpones the session due to the protest",
            "A minister repeats the classic «we're doing badly but going well»",
            "Official optimism: «we are doomed to succeed»",
            "The Government promises «the worst is over»",
            "Official explanation of the crisis: «things happened»",
            "The Economy Minister, frankly: «there is no money»",
            "An official says «the second half of the year is coming»",
            "In the Plaza the chant returns: «out with them all!»",
            "A spokesman asks for «patience, the rain of investments is coming»",
            "Government and opposition agree: «no one is saved alone»",
        };
    }
}
