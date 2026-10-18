using System.IO;
using Argensimia;
using Argensimia.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;

// Assets de juego (fuentes TMP, cartas, unidades, GameAssets, materiales de FX) + objeto "Game" en la escena.
public static class HudSetup
{
    const string ResUi = "Assets/Resources/UI/";
    const string CardsDir = "Assets/Resources/Cards/";
    const string UnitsDir = "Assets/Resources/Units/";
    const string FxDir = "Assets/Resources/FX/";
    const string PlazaMats = "Assets/Materials/Plaza/";

    [MenuItem("Argensimia/4. Create Game Assets (fuentes, cartas, unidades)")]
    public static string CreateUiAssets()
    {
        Directory.CreateDirectory(ResUi);
        Directory.CreateDirectory(CardsDir);
        Directory.CreateDirectory(UnitsDir);
        Directory.CreateDirectory(FxDir);
        string r = MakeFont("Assets/UI/Fonts/Roboto-Black.ttf", ResUi + "Roboto-Black SDF.asset")
                 + MakeFont("Assets/UI/Fonts/Roboto-Bold.ttf", ResUi + "Roboto-Bold SDF.asset")
                 + MakeFont("Assets/UI/Fonts/LuckiestGuy-Regular.ttf", ResUi + "LuckiestGuy SDF.asset")   // logo (Apache 2.0)
                 + MakeFont("Assets/UI/Fonts/LilitaOne-Regular.ttf", ResUi + "LilitaOne SDF.asset")       // títulos y botones (OFL)
                 + LogoMaterials();
        AssetDatabase.DeleteAsset(CardsDir + "refuerzo.asset"); // reemplazada por Infantería con escudos
        AssetDatabase.DeleteAsset(CardsDir + "caballeria.asset"); // reemplazada por Motorizada
        int n = Cards();
        int u = Units();
        Assets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "fonts: " + r + " | cartas: " + n + " | unidades: " + u;
    }

    // ------------------------------------------------------------------ cartas

    static int Cards()
    {
        int n = 0;
        // ---- Policía (Colocar). Stats: vida, daño, ritmo (golpes/s), alcance, área, velocidad
        n += Card("vallado", "Vallado", CardKind.Colocar, 620, "#1fa59a", 3, Rarity.Comun,
            "Una línea de vallas al fondo del slot. Frena a todos hasta que la derriban; los encapuchados la rompen el triple de rápido. Las balas de goma pueden disparar por detrás.",
            "Vallan la Plaza: «es preventivo», explican desde Casa Rosada",
            c => { c.defense = DefenseKind.Vallado; c.stats = S(300, 0, 0, 0); });
        n += Card("infanteria", "Infantería", CardKind.Colocar, 1140, "#3557b8", 2, Rarity.Comun,
            "Tanque barato: una línea de agentes con escudos que frena a todos los que llegan y los golpea cuerpo a cuerpo. No llega detrás de un vallado.",
            "Despliegan la Infantería frente a la Casa Rosada",
            c => { c.defense = DefenseKind.Infanteria; c.stats = S(600, 8, 1, 1.5f); });
        n += Card("balas_goma", "Escopeteros", CardKind.Colocar, 1380, "#c0612b", 2, Rarity.PocoComun,
            "Infantería sin escudo con escopetas antitumulto. Disparan balas de goma en su carril; desde atrás de un vallado le tiran primero a los que lo golpean. También frenan a los que llegan.",
            "Balas de goma en la Plaza: hay heridos y la oposición pide un informe",
            c => { c.defense = DefenseKind.BalasGoma; c.stats = S(400, 9, 1.2f, 14); });
        n += Card("lanzagases", "Lanzagases", CardKind.Colocar, 1620, "#6d7d2e", 1, Rarity.PocoComun,
            "Un agente con lanzagranadas: tira gas al grupo más denso, en su carril o los de al lado. Daño por segundo en área y -40% de velocidad.",
            "Gases en la Plaza: vecinos denuncian ardor en los ojos",
            c => { c.defense = DefenseKind.Lanzagases; c.stats = S(120, 4, 0.35f, 21, 3.5f); c.slow = 0.4f; c.slowTime = 3; });
        n += Card("detencion", "Detenciones", CardKind.Colocar, 1760, "#4b4f9e", 1, Rarity.PocoComun,
            "Brigada de detención: cada 4 s saca de juego a un manifestante cercano. Prioriza a los especiales; no puede con columnas.",
            "Detenciones en la Plaza: la oposición pide explicaciones",
            c => { c.defense = DefenseKind.Detencion; c.stats = S(200, 0, 0.25f, 2.5f); });
        n += Card("motorizada", "Motorizada", CardKind.Colocar, 2230, "#24408f", 0, Rarity.Rara,
            "Cada 15 s sale una pasada a los porrazos hasta Av. de Mayo: golpea, empuja 3 m y aturde 1 s. En el slot ya aparece otra recargando, que también frena y recibe golpes.",
            "Irrumpe la policía motorizada en la Plaza",
            c => { c.defense = DefenseKind.Motorizada; c.stats = S(300, 40, 0, 0); c.push = 3; c.stun = 1; c.cycleOff = 12; });
        n += Card("hidrante", "Camión hidrante", CardKind.Colocar, 3470, "#2a7fd1", 0, Rarity.Rara,
            "Cañón de agua que apunta al más cercano, en su carril o los de al lado: lo moja, lo empuja y lo frena 60%. Tanque de 8 s, recarga 6 s. Ocupa 2 slots.",
            "Hidrantes en la Plaza: denuncian represión",
            c => { c.defense = DefenseKind.Hidrante; c.stats = S(900, 3, 0, 28); c.push = 2; c.slow = 0.6f; c.cycleOn = 8; c.cycleOff = 6; c.slotSize = 2; });

        // ---- Desbloqueables: una cada 3 mandatos sobrevividos, sumando partidas (Meta.UnlockOrder). Copias 0: solo por el cierre.
        n += Card("puntero", "Puntero", CardKind.Colocar, 700, "#e0a030", 0, Rarity.Comun,
            "El puntero del barrio arma su parrillita en la Plaza: mientras está en pie genera $12/s durante la oleada. Es frágil: cuidalo.",
            "Choripanes gratis en la Plaza: «es una jornada solidaria»",
            c => { c.defense = DefenseKind.Puntero; c.stats = S(150, 0, 0, 0); c.moneyRate = 12; });
        n += Card("contenedores", "Contenedores", CardKind.Colocar, 1300, "#3e8e4e", 0, Rarity.Comun,
            "Dos contenedores de basura atravesados en el carril: aguantan muchísimo y los encapuchados no los pueden saltar.",
            "Usan contenedores de basura como barricada frente a la Casa Rosada",
            c => { c.defense = DefenseKind.Contenedores; c.stats = S(1500, 0, 0, 0); c.noJump = true; });
        n += Card("infiltrado", "Infiltrado", CardKind.Colocar, 900, "#5d6b7a", 0, Rarity.PocoComun,
            "Un policía de civil entre la gente: tarda 10 s en ubicarse y nadie lo ve. Después, el primero que pasa queda detenido y los de alrededor, aturdidos. Un solo uso; a una columna le pega fuerte.",
            "Denuncian policías de civil infiltrados en la marcha",
            c => { c.defense = DefenseKind.Infiltrado; c.stats = S(100, 150, 0, 0, 4); c.stun = 2; c.cycleOn = 10; });
        n += Card("comisario", "Comisario", CardKind.Colocar, 1600, "#2f4f9f", 0, Rarity.Rara,
            "El comisario arenga por megáfono: las defensas a 1 casillero pegan 30% más fuerte (45% y 60% en NV2 y NV3). También frena.",
            "El comisario toma el mando del operativo en la Plaza",
            c => { c.defense = DefenseKind.Comisario; c.stats = S(300, 0, 0, 6.8f); c.buff = 0.3f; });
        n += Card("concertina", "Concertina", CardKind.Colocar, 500, "#7b8794", 0, Rarity.Comun,
            "Rollos de alambre en el piso: no frenan a nadie, pero lastiman y demoran a todos los que pasan. Nadie le puede pegar.",
            "Alambre de púas en la Plaza: «es una medida preventiva»",
            c => { c.defense = DefenseKind.Concertina; c.stats = S(100, 6, 0, 0); c.slow = 0.3f; });
        n += Card("requisa", "Requisa", CardKind.Colocar, 1700, "#6b4fa0", 0, Rarity.PocoComun,
            "Cada 8 s desarma al especial más peligroso a 2 casilleros: el encapuchado, el piquetero o el mortero pasan a ser manifestantes comunes.",
            "Requisa en los accesos a la Plaza: secuestran palos y morteros caseros",
            c => { c.defense = DefenseKind.Requisa; c.stats = S(250, 0, 0.125f, 13.6f); });
        n += Card("desvio", "Desvío", CardKind.Colocar, 800, "#e8742c", 0, Rarity.PocoComun,
            "Un agente de tránsito con conos y vallas manda a los que llegan al carril vecino mejor defendido. Nadie le pega, pero no sirve con columnas organizadas.",
            "Desvían la marcha por una calle lateral",
            c => { c.defense = DefenseKind.Desvio; c.stats = S(100, 0, 0, 0); });
        n += Card("estruendo", "Estruendo", CardKind.Colocar, 1100, "#e04a2f", 0, Rarity.PocoComun,
            "Granada de estruendo: al segundo explota y aturde 3 s a todos en 6 m, con algo de daño. Un solo uso.",
            "Una granada de estruendo sacude la Plaza de Mayo",
            c => { c.defense = DefenseKind.Estruendo; c.stats = S(100, 60, 0, 0, 6); c.stun = 3; c.cycleOn = 0.8f; });
        n += Card("drone", "Drone", CardKind.Colocar, 2100, "#3a9ad9", 0, Rarity.Rara,
            "Vuela sobre su slot y dispara a cualquier carril hasta 3 casilleros, primero a los especiales. No frena a nadie; el mortero lo puede bajar.",
            "Un drone policial sobrevuela la Plaza",
            c => { c.defense = DefenseKind.Drone; c.stats = S(200, 10, 1.2f, 20.4f); });
        n += Card("tanqueta", "Tanqueta", CardKind.Colocar, 3900, "#4a5560", 0, Rarity.Rara,
            "Blindado antidisturbios: frena, aguanta muchísimo y cada 16 s dispara espuma al grupo más denso de toda la Plaza (daño en área, moja y frena 70%). Ocupa 2 slots.",
            "Llega la tanqueta antidisturbios a la Plaza",
            c => { c.defense = DefenseKind.Tanqueta; c.stats = S(1400, 45, 0, 60, 7); c.slow = 0.7f; c.slowTime = 4; c.cycleOff = 16; c.slotSize = 2; });

        // ---- Efecto
        n += Card("impuestazo", "Impuestazo", CardKind.Efecto, 0, "#d8452f", 1, Rarity.Comun,
            "La AFIP sale a recaudar. Entra plata, pero la calle se calienta y las columnas llegan más enojadas.",
            "AFIP anuncia un nuevo impuesto «transitorio»", c => { c.moneyDelta = 3000; c.popularityDelta = -16; });
        n += Card("plan_platita", "Plan platita", CardKind.Efecto, 1850, "#8a3fc4", 1, Rarity.PocoComun,
            "Reparto de fondos. Sube la popularidad: menos gente en la calle y columnas más tranquilas.",
            "Lluvia de bonos: «es para reactivar el consumo»", c => c.popularityDelta = 10);
        n += Card("prestamo", "Préstamo FMI", CardKind.Efecto, 0, "#d9961c", 0, Rarity.Rara,
            "Entran dólares ya, a cambio de ajuste: baja el ingreso por segundo durante las oleadas.",
            "Llegó el desembolso del Fondo: «es el último», aseguran", c => { c.moneyDelta = 5000; c.incomeDelta = -15; });
        n += Card("blanqueo", "Blanqueo", CardKind.Efecto, 1000, "#2f9a4a", 1, Rarity.PocoComun,
            "Moratoria para que entre plata al sistema. Sube el ingreso por segundo, con algo de costo político.",
            "Lanzan un blanqueo: «esta vez sí», prometen", c => { c.incomeDelta = 20; c.popularityDelta = -6; });
        n += Card("cadena_nacional", "Cadena nacional", CardKind.Efecto, 520, "#c7477a", 2, Rarity.Comun,
            "Mensaje presidencial en todos los canales. Sube un poco la popularidad.",
            "Cadena nacional: el Presidente pide «calma y paciencia»", c => c.popularityDelta = 5);
        n += Card("feriado_puente", "Feriado puente", CardKind.Efecto, 1520, "#e07b2a", 0, Rarity.PocoComun,
            "Medio país se va a la costa: la próxima oleada viene 20% más chica. Solo en la previa.",
            "Decretan feriado puente: rutas colapsadas hacia la costa", c => { c.nextWaveReduction = 0.2f; c.previaOnly = true; });
        n += Card("dolar_soja", "Dólar soja", CardKind.Efecto, 0, "#9aa83a", 0, Rarity.PocoComun,
            "Tipo de cambio especial para el campo: entra mucha plata de golpe. Solo en la previa.",
            "Nuevo dólar soja: liquidan récord en una semana", c => { c.moneyDelta = 4000; c.popularityDelta = -4; c.previaOnly = true; });

        // ---- Ministros (gabinete de 3; solo salen en el 1 de 3)
        n += Card("min_economia", "Min. Economía", CardKind.Ministro, 1850, "#b8860b", 0, Rarity.PocoComun,
            "Ministro pasivo: +$25/s en las oleadas, pero cada mandato resta 2% de popularidad.",
            "Asume el nuevo ministro de Economía: «no hay plata», advierte", c => { c.incomeDelta = 25; c.popPerMandate = -2; });
        n += Card("min_seguridad", "Min. Seguridad", CardKind.Ministro, 2230, "#2b4a8a", 0, Rarity.Rara,
            "Ministro pasivo: la policía tiene +20% de vida y al cerrar cada mandato recupera hasta 30% (en vez de 25%). La mano dura cuesta: -1% de popularidad por mandato.",
            "Asume la nueva ministra de Seguridad: «el que las hace, las paga»", c => { c.defenseHpBonus = 0.2f; c.healBonus = 0.05f; c.popPerMandate = -1; });
        n += Card("min_desarrollo", "Min. Desarrollo", CardKind.Ministro, 2470, "#3f9a7a", 0, Rarity.Rara,
            "Ministro pasivo de Desarrollo Social: cada mandato suma 3% de popularidad.",
            "Desarrollo Social anuncia refuerzo de planes", c => c.popPerMandate = 3);

        // ---- Tanda 2: solo entran por las cartas del cierre (copias 0). Siempre suman por un lado y restan por otro.
        n += Card("maquinita", "La maquinita", CardKind.Efecto, 0, "#4f8a3a", 1, Rarity.Comun,
            "El Banco Central imprime: entra plata ya, pero la inflación se nota en la calle.",
            "Emisión récord: «es para cubrir el déficit», explican", c => { c.moneyDelta = 2000; c.popularityDelta = -8; });
        n += Card("futbol", "Fútbol para todos", CardKind.Efecto, 700, "#2e8bc0", 0, Rarity.Comun,
            "Partidos gratis por TV abierta. La gente se queda en casa, pero sale caro mantenerlo.",
            "Vuelve el fútbol gratis: la oposición habla de «pan y circo»", c => { c.popularityDelta = 6; c.incomeDelta = -5; });
        n += Card("aguinaldo", "Aguinaldo", CardKind.Efecto, 1200, "#c4883f", 0, Rarity.PocoComun,
            "Medio aguinaldo extra para todos: sube mucho la popularidad y baja el ingreso por segundo.",
            "Anuncian un aguinaldo extra: festejos en los barrios", c => { c.popularityDelta = 12; c.incomeDelta = -12; });
        n += Card("operativo", "Relevo", CardKind.Efecto, 900, "#3a5f9e", 0, Rarity.PocoComun,
            "Operativo sorpresa, relevo de tropas en plena Plaza: toda la policía recupera 40% de su vida al instante. A la gente no le gusta.",
            "Relevo sorpresa de las fuerzas en la Plaza de Mayo", c => { c.healNow = 0.4f; c.popularityDelta = -3; });
        n += Card("decretazo", "Decretazo", CardKind.Efecto, 0, "#8a5a2b", 0, Rarity.PocoComun,
            "DNU por la caja: entra plata, pero se pierde gobernabilidad (nunca te deja en 0).",
            "Decretazo: el Congreso denuncia «avasallamiento institucional»", c => { c.moneyDelta = 3500; c.govDelta = -2; });
        n += Card("refuerzo_federal", "Refuerzo militar", CardKind.Efecto, 2800, "#2c6a5a", 0, Rarity.Rara,
            "Llegan tropas militares a la Capital: +3 de gobernabilidad y +3 al máximo. A la gente no le gusta nada.",
            "Tropas militares desembarcan en la Capital", c => { c.govDelta = 3; c.maxGovDelta = 3; c.popularityDelta = -5; });
        n += Card("privatizacion", "Privatización", CardKind.Efecto, 0, "#6a4a8a", 0, Rarity.Rara,
            "Se vende una empresa del Estado: mucha plata de golpe, pero se pierde una fuente de ingresos y la calle se enoja.",
            "Venden la empresa estatal: «es para sanear las cuentas»", c => { c.moneyDelta = 7000; c.popularityDelta = -10; c.incomeDelta = -10; });
        n += Card("acuerdo_cgt", "Pacto social", CardKind.Efecto, 2600, "#8a2f2f", 0, Rarity.Rara,
            "Acuerdo con la CGT: la próxima oleada viene 30% más chica, a cambio de concesiones que bajan el ingreso. Solo en la previa.",
            "Gobierno y CGT firman un acuerdo: «paz social», celebran", c => { c.nextWaveReduction = 0.3f; c.incomeDelta = -10; c.previaOnly = true; });
        n += Card("super_ministro", "Super Ministro", CardKind.Ministro, 2900, "#c9a227", 0, Rarity.Rara,
            "Ministro pasivo: concentra Economía, Producción y Hacienda. +$50/s en las oleadas y +$500 de sueldo por mandato, pero el ajuste resta 3% de popularidad por mandato.",
            "Asume el Super Ministro: «vamos a ordenar la macro»", c => { c.incomeDelta = 50; c.salaryDelta = 500; c.popPerMandate = -3; });
        n += Card("min_interior", "Min. del Interior", CardKind.Ministro, 1900, "#5a6e3a", 0, Rarity.PocoComun,
            "Ministro pasivo: negocia con gobernadores y recupera 1 de gobernabilidad al cerrar cada mandato. Las transferencias bajan el ingreso.",
            "El ministro del Interior cierra acuerdos con los gobernadores", c => { c.govPerMandate = 1; c.incomeDelta = -10; });
        n += Card("vocero", "Vocero", CardKind.Ministro, 1300, "#b8477a", 0, Rarity.PocoComun,
            "Ministro pasivo: el vocero presidencial da conferencias todos los días. +2% de popularidad por mandato; la pauta oficial cuesta $8/s.",
            "El vocero presidencial: «está todo bajo control»", c => { c.popPerMandate = 2; c.incomeDelta = -8; });

        // ---- Crisis (entran solas a la mano; el costo es lo que sale evitarlas)
        n += Card("devaluacion", "Devaluación", CardKind.Crisis, 1850, "#7a2d2d", 0, Rarity.Comun,
            "Salto del dólar: perdés el 30% de la plata. Aceptala o pagá para evitarla.",
            "Disparada del dólar: el Gobierno habla de «reacomodamiento»", c => { c.moneyMultiplier = 0.7f; c.inPool = false; });
        n += Card("paro_general", "Paro general", CardKind.Crisis, 2470, "#8a1f1f", 0, Rarity.Comun,
            "La CGT para el país: +1 columna organizada en la próxima oleada.",
            "La CGT confirma paro general y movilización a Plaza de Mayo", c => { c.extraColumns = 1; c.inPool = false; });
        n += Card("tarifazo", "Tarifazo", CardKind.Crisis, 1240, "#7a3d1f", 0, Rarity.Comun,
            "Aumentan luz, gas y transporte: baja el ingreso por segundo.",
            "Tarifazo: las boletas llegan con aumentos de tres cifras", c => { c.incomeDelta = -10; c.inPool = false; });
        n += Card("escandalo", "Escándalo", CardKind.Crisis, 1520, "#6a2a5a", 0, Rarity.Comun,
            "Se filtra un audio comprometedor: -15% de popularidad.",
            "Se filtró un audio y el Gobierno habla de «operación»", c => { c.popularityDelta = -15; c.inPool = false; });
        return n;
    }

    // Variantes extra de titular por carta (la marquesina elige una al azar sin repetir la última).
    static readonly System.Collections.Generic.Dictionary<string, string> MoreHeadlines = new System.Collections.Generic.Dictionary<string, string>
    {
        ["puntero"] = "El puntero del barrio reparte choris en la Plaza|«Acá nadie se queda sin su chori», promete un militante",
        ["contenedores"] = "Contenedores en el medio de la calle: la Ciudad pide devolverlos|Barricada de contenedores frente a Balcarce 50",
        ["infiltrado"] = "Polémica por agentes de civil entre los manifestantes|«No hay infiltrados», asegura el Ministerio de Seguridad",
        ["comisario"] = "El comisario a cargo: «acá mando yo»|Arenga por megáfono: la policía se agranda",
        ["concertina"] = "Ponen alambre de púas alrededor de la Plaza|Organismos de DDHH critican la concertina",
        ["requisa"] = "Revisan mochilas en los accesos a la Plaza|Secuestran gomeras, palos y pasamontañas",
        ["desvio"] = "Cambian el recorrido de la marcha a último momento|Operativo de tránsito: la columna dobla por una lateral",
        ["estruendo"] = "Estruendo en la Plaza: corridas y confusión|Granadas de estruendo: «solo hacen ruido», dicen",
        ["drone"] = "Drones policiales vigilan la marcha desde el aire|«Big Brother» en la Plaza: denuncian vigilancia con drones",
        ["tanqueta"] = "La tanqueta llena de espuma la Plaza de Mayo|Blindados en el microcentro: la oposición pide explicaciones",
        ["vallado"] = "Más vallas en la Plaza: el Gobierno blinda la Casa Rosada|Cercan la Plaza de Mayo: vecinos no pueden cruzar",
        ["infanteria"] = "Escudos y cascos: la Infantería forma frente a Balcarce 50|Refuerzan el operativo con la Guardia de Infantería",
        ["balas_goma"] = "Disparos de goma en la Plaza: denuncian represión|Organismos de DDHH condenan el uso de balas de goma",
        ["lanzagases"] = "Nube de gas sobre la Plaza: corridas en Av. de Mayo|Lacrimógenos en el microcentro: cierran comercios",
        ["detencion"] = "Despliegan la brigada de detenciones en la Plaza|Policía de civil identifica a los «revoltosos»",
        ["motorizada"] = "Las motos de la Federal avanzan sobre los manifestantes|Ruido de motores en la Plaza: llega la motorizada",
        ["hidrante"] = "Llegan los «Neptunos»: chorros de agua en la Plaza|El camión hidrante dispersa a los manifestantes",
        ["impuestazo"] = "Nuevo tributo: «es solo por esta vez», dicen en Hacienda|Rechazo empresario al nuevo impuesto",
        ["plan_platita"] = "El Gobierno reparte bonos antes de las elecciones|«Es para que la gente llegue a fin de mes», justifican",
        ["prestamo"] = "Acuerdo con el FMI: llegan los dólares y el ajuste|El Fondo aprueba el desembolso con «metas exigentes»",
        ["blanqueo"] = "Blanqueo de capitales: «traigan los dólares del colchón»|La oposición critica el «perdón fiscal»",
        ["cadena_nacional"] = "El Presidente habla por cadena nacional|Interrumpen la novela para un mensaje presidencial",
        ["feriado_puente"] = "Fin de semana largo: la Costa Atlántica, colapsada|Récord de turistas por el feriado puente",
        ["dolar_soja"] = "El campo liquida dólares con el nuevo tipo de cambio|Entra una cosecha de dólares al Banco Central",
        ["maquinita"] = "El Central imprime billetes a toda máquina|Récord de emisión: los economistas advierten por la inflación",
        ["futbol"] = "Vuelven los partidos gratis por TV abierta|Fútbol para todos: los bares se llenan",
        ["aguinaldo"] = "Medio aguinaldo extra: largas filas en los cajeros|Festejo en los barrios por el aguinaldo extra",
        ["operativo"] = "Llegan efectivos de refresco a la Plaza|Cambio de guardia sorpresa: la policía se reorganiza",
        ["decretazo"] = "El Presidente firma un DNU de madrugada|Por decreto: el Gobierno saltea al Congreso",
        ["refuerzo_federal"] = "Camiones militares entran a la Capital|Las tropas toman posiciones alrededor de la Plaza",
        ["privatizacion"] = "Venden la joya de la abuela: privatizan otra empresa|Protestas de trabajadores por la privatización",
        ["acuerdo_cgt"] = "Pacto social: la CGT levanta la medida de fuerza|Foto en Casa Rosada con la cúpula de la CGT",
        ["min_economia"] = "Nuevo ministro de Economía: el dólar reacciona|Juró el ministro de Economía: «hay que ordenar las cuentas»",
        ["min_seguridad"] = "Nueva ministra de Seguridad promete «orden en las calles»|Mano dura: asume la nueva conducción de Seguridad",
        ["min_desarrollo"] = "Asume Desarrollo Social: «nadie va a quedar afuera»|Más planes sociales con la nueva gestión",
        ["super_ministro"] = "Super Ministro: concentra Economía, Producción y Agricultura|Llega el Super Ministro y los mercados festejan",
        ["min_interior"] = "El ministro del Interior recorre las provincias|Acuerdo con los gobernadores: «hay gobernabilidad»",
        ["vocero"] = "Conferencia del vocero: «no tenemos nada para informar»|El vocero presidencial desmiente todo",
        ["devaluacion"] = "Salta el dólar: corridas en las casas de cambio|El peso se derrumba frente al dólar",
        ["paro_general"] = "Paro total: no hay colectivos ni trenes|La CGT marcha con todas las columnas a la Plaza",
        ["tarifazo"] = "Llegan las boletas: aumentos de tres cifras en luz y gas|Ruidazo en los barrios contra el tarifazo",
        ["escandalo"] = "Escándalo: aparecen bolsos con dinero en un convento|Filtran un video comprometedor de un funcionario",
    };

    static UnitStats S(float hp, float dmg, float rate, float range, float area = 0, float speed = 0) =>
        new UnitStats { hp = hp, damage = dmg, rate = rate, range = range, area = area, speed = speed };

    // Crea o actualiza (sin pisar el arte asignado a mano).
    static int Card(string id, string title, CardKind kind, int cost, string hex, int copies, Rarity rarity, string desc, string headline,
        System.Action<CardDef> setup)
    {
        string path = CardsDir + id + ".asset";
        var c = AssetDatabase.LoadAssetAtPath<CardDef>(path);
        bool isNew = c == null;
        if (isNew) c = ScriptableObject.CreateInstance<CardDef>();
        var art = c.art;
        c.id = id; c.title = title; c.kind = kind; c.cost = cost; c.copies = copies; c.description = desc;
        c.headline = MoreHeadlines.TryGetValue(id, out var more) ? headline + "|" + more : headline; // variantes separadas por |
        c.rarity = rarity; c.inPool = true;
        c.popularityDelta = 0; c.moneyDelta = 0; c.incomeDelta = 0; c.popPerMandate = 0; c.defenseHpBonus = 0;
        c.moneyMultiplier = 1; c.extraColumns = 0; c.nextWaveReduction = 0; c.previaOnly = false;
        c.salaryDelta = 0; c.govDelta = 0; c.maxGovDelta = 0; c.govPerMandate = 0; c.healNow = 0; c.healBonus = 0;
        c.moneyRate = 0; c.buff = 0; c.noJump = false;
        c.stats = default; c.maxBlock = 0; c.slow = 0; c.slowTime = 0; c.stun = 0; c.push = 0; c.cycleOn = 0; c.cycleOff = 0; c.slotSize = 1;
        ColorUtility.TryParseHtmlString(hex, out c.color);
        setup(c);
        
        c.art = art;
        if (isNew) AssetDatabase.CreateAsset(c, path);
        EditorUtility.SetDirty(c);
        return 1;
    }

    // ------------------------------------------------------------------ unidades

    static int Units()
    {
        int n = 0;
        n += Unit("comun", "Manifestante", UnitKind.Comun, 1, S(40, 5, 1, 1, 0, 1.4f), 1, 1, 6, false, u => u.prop = "flag");
        n += Unit("cacerolero", "Cacerolero", UnitKind.Cacerolero, 1, S(30, 0, 0, 0, 0, 1.2f), 1, 1, 2.2f, false,
            u => { u.shirt = 1; u.prop = "pot"; });
        n += Unit("jubilado", "Jubilado", UnitKind.Jubilado, 1, S(25, 2, 0.5f, 1, 0, 0.8f), 1, 2, 1.5f, false,
            u => { u.popOnHit = 0.1f; u.hair = 5; u.shirt = 6; u.scale = 0.95f; u.prop = "cane"; });
        n += Unit("encapuchado", "Encapuchado", UnitKind.Encapuchado, 2, S(50, 15, 1.2f, 1, 0, 2.0f), 2, 2, 1.6f, true,
            u => { u.fenceMultiplier = 3; u.jumpChance = 0.3f; u.shirt = 7; u.hair = 3; u.prop = "hood"; });
        n += Unit("piquetero", "Piquetero", UnitKind.Piquetero, 3, S(80, 6, 1, 1, 0, 1.2f), 2, 3, 1.1f, true,
            u => { u.tireEvery = 10; u.shirt = 5; u.prop = "tire"; });
        n += Unit("mortero", "Mortero", UnitKind.Mortero, 4, S(35, 25, 0.25f, 14, 2, 1.0f), 3, 3, 0.8f, true,
            u => { u.shirt = 0; u.hair = 2; u.prop = "mortar"; u.scale = 1.12f; });
        n += Unit("columna", "Columna organizada", UnitKind.Columna, 6, S(600, 10, 1, 1, 0, 0.7f), 5, 4, 0.5f, true,
            u => { u.auraSpeed = 0.2f; u.auraRadius = 4; u.pushResist = 0.5f; u.members = 5; u.prop = "banner"; }); // dos filas: 3 + 2
        return n;
    }

    static int Unit(string id, string title, UnitKind kind, int cost, UnitStats st, int gov, int minMandate, float weight, bool special, System.Action<UnitDef> setup)
    {
        string path = UnitsDir + id + ".asset";
        var u = AssetDatabase.LoadAssetAtPath<UnitDef>(path);
        bool isNew = u == null;
        if (isNew) u = ScriptableObject.CreateInstance<UnitDef>();
        u.id = id; u.title = title; u.kind = kind; u.cost = cost; u.stats = st; u.govDamage = gov; u.minMandate = minMandate;
        u.weight = weight; u.special = special;
        u.popOnHit = 0; u.popOnArrive = 0; u.fenceMultiplier = 1; u.pushResist = 0; u.auraSpeed = 0; u.auraRadius = 4; u.tireEvery = 0; u.jumpChance = 0;
        u.shirt = -1; u.hair = -1; u.scale = 1; u.members = 1; u.prop = "none";
        setup(u);
        if (isNew) AssetDatabase.CreateAsset(u, path);
        EditorUtility.SetDirty(u);
        return 1;
    }

    // ------------------------------------------------------------------ GameAssets + FX

    static GameObject Model(string n) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/" + n + ".fbx");

    static Material[] Mats(string prefix, int count)
    {
        var l = new System.Collections.Generic.List<Material>();
        for (int i = 0; i < count; i++)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(PlazaMats + prefix + i + ".mat");
            if (m) l.Add(m);
        }
        return l.ToArray();
    }

    static void Assets()
    {
        const string path = "Assets/Resources/GameAssets.asset";
        var a = AssetDatabase.LoadAssetAtPath<GameAssets>(path);
        if (!a) { a = ScriptableObject.CreateInstance<GameAssets>(); AssetDatabase.CreateAsset(a, path); }
        a.people = new[] { Model("person_a"), Model("person_b"), Model("person_c"), Model("person_d") };
        a.officer = Model("police_officer"); a.truck = Model("police_truck"); a.valla = Model("valla_l") ? Model("valla_l") : Model("valla");
        a.flagArg = Model("flag_arg"); a.flagRed = Model("flag_red"); a.banner = Model("banner");
        a.puntero = Model("puntero"); a.contenedor = Model("contenedor"); a.comisario = Model("comisario"); a.concertina = Model("concertina");
        a.requisa = Model("requisa"); a.desvio = Model("desvio"); a.drone = Model("drone"); a.tanqueta = Model("tanqueta"); a.tanquetaTurret = Model("tanqueta_torreta");
        a.shirts = Mats("M_Shirt_", 10); a.pants = Mats("M_Pants_", 5); a.skins = Mats("M_SkinV_", 5); a.hairs = Mats("M_HairV_", 6);

        var lit = FxMat("M_PropLit", Shader.Find("Universal Render Pipeline/Lit"));
        lit.SetColor("_BaseColor", Color.white); lit.SetFloat("_Smoothness", 0.15f); lit.enableInstancing = true;
        a.lit = lit;
        var unlit = FxMat("M_BarUnlit", Shader.Find("Universal Render Pipeline/Unlit"));
        unlit.SetColor("_BaseColor", Color.white);
        a.unlit = unlit;
        var soft = SoftTexture();
        a.smoke = Particle("M_FxSmoke", soft, false);
        a.fire = Particle("M_FxFire", soft, true);
        a.water = Particle("M_FxWater", soft, false);
        EditorUtility.SetDirty(a);
    }

    static Material FxMat(string name, Shader sh)
    {
        string p = FxDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Particle(string name, Texture2D tex, bool additive)
    {
        var m = FxMat(name, Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1);
        m.SetFloat("_Blend", additive ? 2 : 0);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D SoftTexture()
    {
        string p = FxDir + "T_FxSoft.png";
        if (!File.Exists(p))
        {
            const int s = 64;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = new Vector2(x + 0.5f - s / 2f, y + 0.5f - s / 2f).magnitude / (s / 2f);
                    float a = Mathf.Clamp01(1 - d);
                    t.SetPixel(x, y, new Color(1, 1, 1, a * a * (3 - 2 * a)));
                }
            t.Apply();
            File.WriteAllBytes(p, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(p);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    // Materiales del logo como assets (así el shader "TextMeshPro/Distance Field", que soporta textura de cara,
    // entra al build): LogoMat (banana) y LogoFlagMat (bandera en la cara de cada letra, textura FlagFace).
    static string LogoMaterials()
    {
        AssetDatabase.Refresh();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResUi + "LuckiestGuy SDF.asset");
        var shader = Shader.Find("TextMeshPro/Distance Field");
        if (!font || !shader) return "FALTA logo ";
        // textura de la bandera: celeste / blanco / celeste
        string texPath = ResUi + "FlagFace.png";
        var tex = new Texture2D(4, 96, TextureFormat.RGBA32, false);
        var celeste = new Color(0x74 / 255f, 0xac / 255f, 0xdf / 255f);
        for (int y = 0; y < 96; y++) { float v = (y + 0.5f) / 96; var c = v > 0.34f && v < 0.66f ? Color.white : celeste; for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c); }
        tex.Apply();
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath);
        var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
        ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None;
        ti.SaveAndReimport();
        var flag = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        Material Make(string name, Color outline, Texture2D face)
        {
            string path = ResUi + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(font.material); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            m.SetFloat(ShaderUtilities.ID_GradientScale, font.atlasPadding + 1);
            m.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasWidth);
            m.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasHeight);
            m.EnableKeyword(ShaderUtilities.Keyword_Outline);
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            m.SetColor(ShaderUtilities.ID_OutlineColor, outline);
            m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.05f, 0.02f, 0.08f, 0.85f));
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.9f);
            m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.25f);
            m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.05f);
            m.SetTexture(ShaderUtilities.ID_FaceTex, face);
            EditorUtility.SetDirty(m);
            return m;
        }
        Make("LogoMat", new Color(0.22f, 0.08f, 0.04f, 1), null);
        Make("LogoFlagMat", new Color(0.05f, 0.11f, 0.28f, 1), flag);
        return "logo ";
    }

    static string MakeFont(string ttf, string path)
    {
        if (File.Exists(path)) return "(existe) ";
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        if (!font) return "FALTA " + ttf + " ";
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 12, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(fa, path);
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        return path + " ";
    }

    // ------------------------------------------------------------------ escena

    // Lo llama PlazaBuilder.Build() antes de guardar la escena.
    public static void AddGameObjects()
    {
        var old = GameObject.Find("Game");
        if (old) Object.DestroyImmediate(old);
        var go = new GameObject("Game");
        go.AddComponent<GameState>();
        go.AddComponent<CardDeck>();
        go.AddComponent<WaveDirector>();
        go.AddComponent<StreetMood>();
        go.AddComponent<GameHud>();
        go.AddComponent<GameFlow>();
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.transform.SetParent(go.transform);
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        SlotBuilder.Build();
    }

    [MenuItem("Argensimia/5. Add Game + HUD to Scene")]
    public static string AddToScene()
    {
        AddGameObjects();
        var s = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        return "Game + HUD agregados a " + s.path;
    }
}
