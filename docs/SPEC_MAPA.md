# Argensimia — Spec del juego y del mapa Plaza de Mayo

Actualizado: 2026-10-17 · Alex. **Versión 0.1.2** (`PlayerSettings.bundleVersion`, se muestra en el menú; semver: subir el menor con cada tanda de features y el parche con arreglos). Versión editable: https://claude.ai/code/artifact/966a4aee-2203-47c0-8cd8-33cb5aacf6e0

## Últimos cambios

Solo los últimos 5 cambios como máximo, del más nuevo al más viejo; al sumar uno se quita el más viejo.

| Fecha | Cambio |
| --- | --- |
| 2026-10-17 | **0.1.2**: botones SALIR y PANTALLA COMPLETA/VENTANA en el menú (escritorio). |
| 2026-10-17 | **0.1.1**: el puntaje y el ranking cuentan el mandato en el que estabas (antes mostraba uno menos). |
| 2026-10-17 | **0.1.0**: inglés completo con botón de idioma; GitHub y versión en el menú; ranking top 5 abajo a la izquierda; MANIFESTANTES RESTANTES durante la oleada; vallas NV2/NV3 más robustas; al terminar se recarga la escena; sin warnings de compilación. |
| 2026-10-17 | Reloj de 30 s en la previa; abucheo al perder popularidad; MAZO VACÍO sobre el mazo; columna roja de 5 (3 + 2); carriles 1-2-3-4; el mortero avanza con línea libre; balizas en patrulleros; logo con ARG en bandera e ícono de la app. |
| 2026-10-17 | Niveles bufeados (vida ×2/×3,5, daño ×1,75/×3, ritmo, alcance y golpe múltiple de infantería); slots circulares; pantalla de carga al arrancar. |

## Resumen

Argensimia es un tower defense mobile en la Plaza de Mayo: el jugador defiende la Casa Rosada con policía frente a columnas de manifestantes, mandato tras mandato. Es el único mapa del juego, así que se trabaja con nivel de detalle alto.

- **Proyecto:** `/Users/alexander/git/argensimia/Argensimia`, Unity 6000.4.1f1, URP (renderer de PC en Forward+). Repo: `github.com/alexander171294/Argensimia`.
- **Escena:** `Assets/Scenes/Plaza.unity`, generada por código. No se edita a mano: cualquier cambio manual se pierde en el próximo rebuild.
- **Objetivo visual:** ilustración semirrealista y cálida estilo Ikariam, con vista isométrica en perspectiva. Edificios, Casa Rosada y escenografía con detalle; árboles, gente, autos y policía quedan low poly a propósito, con silueta caricaturesca bien legible.
- **Momento del día:** tarde soleada. Ventanas oscuras, faroles y vidrieras apagados.
- **Objetivo del jugador:** sobrevivir mandatos (= oleadas). Se pierde solo si la gobernabilidad llega a 0, o renunciando desde la pausa.

## Reglas de trabajo

Todo se hace con los MCP de Blender y de Unity; nada de CLI de Unity ni control de pantalla.

- **Blender MCP** (`execute_blender_code`): lee y escribe archivos del proyecto y genera y exporta los modelos. Antes de generar hay que recargar los módulos en este orden: `gen_assets` → `hq` → resto. `hq` parchea `A.box`, `A.finish` y `GA.M`; sin él se exporta sin UV ni texturas.
- **Unity MCP:** `menu` (pasos de Argensimia), `eval` (C# en el editor), `recompile`, `capture_game_view`, `editor_play`/`editor_stop`.
- Para revisar de cerca se crea una cámara temporal `TmpCloseCam` y se captura con `capture_game_view(camera=…)`. Hay que borrarla antes de guardar.
- **Play en el editor sin foco:** el proyecto tiene `Run In Background` activo; si el tiempo no avanza (`Time.frameCount` fijo), poner `Application.runInBackground = true` vía `eval`.
- **No editar scripts en Play:** cualquier escritura de `.cs` dispara una recarga de dominio y los estáticos (`GameState.I`, etc.) quedan nulos (NullReference en `WaveDirector.Update`). Salir de Play antes de tocar código.
- **Una sola instancia de Blender y de Unity:** los cambios van en secuencia. No conviene usar varios agentes tocando la escena en paralelo.
- Si el servidor de Unity no responde tras un recompile, refresh o entrar a Play, alcanza con reintentar la llamada.
- **Audio:** importar música como MP3 (o WAV). Los OGG exportados con ffmpeg fallan en el importador de Unity (error FSBTool/FMOD).

## Pipeline y archivos

Los modelos se generan por código en Blender y se exportan a FBX. Los colores salen de `palette.txt` y la escena la arma `PlazaBuilder.cs`. Todo el gameplay y la UI se crean en runtime.

| Archivo | Qué hace |
| --- | --- |
| `BlenderSrc/gen_assets.py` | Primitivas, clase `A`, `face_box`/`face_cy`/`face_put`, `tri_prism`, edificios haussmannianos (`building`) y props base (camión policial, agente, gente, banderas, autos). |
| `BlenderSrc/hq.py` | Biseles, sombreado suave, UV de caja, texturas procedurales (`T_*` + normales). |
| `BlenderSrc/props_hq.py` | Faroles, palmeras, árboles, arbustos, canteros, reja, monumento, edificio de esquina y `ground()`. |
| `BlenderSrc/buildings_kit.py` | Estilos italianizante, art nouveau y art déco, y la torre circular (`rotunda`). |
| `BlenderSrc/porteno_kit.py` | Edificios porteños eclécticos (Otto Wulff, terraza con camarógrafo) y la valla policial en L (`valla_l`). |
| `BlenderSrc/casa_v2.py` | Casa Rosada (alas de 2 vanos, pabellones, bloque central). |
| `BlenderSrc/heli_kit.py` | Helipuerto (`helipad`), helicóptero presidencial caricaturesco y detallado (`helicopter`) y rotor aparte (`heli_rotor`). |
| `BlenderSrc/sfx_gen.py` | Efectos de sonido sintetizados con numpy (sin samples): `build_all()` escribe los WAV en `Assets/Resources/SFX`. |
| `BlenderSrc/card_art.py` | Arte de cartas: genera (`start`), completa recortes y retoca (`edit_image`, `crop_rows`) y arma la galería de revisión. Key en `~/.argensimia_atlas_key`. |
| `BlenderSrc/palette.txt` | Una línea por material: `nombre hex emisión [textura metros_por_repetición]`. |
| `BlenderSrc/plaza_map.svg` | Mapa limpio de la plaza (canteros, carriles, refuerzos). El boceto original está en `plaza_map_boceto.svg`. |
| `BlenderSrc/plaza_design_v3.py` | Geometría armónica: carriles, refuerzos, conectores y canteros. Escribe `plaza_map.svg`. |
| `BlenderSrc/plaza_map.py` | Lee el SVG y lo convierte a coordenadas de mundo. |
| `BlenderSrc/plaza_design_v2.py` | Ubica los objetos de la plaza. Escribe `plaza_props.txt` y `plaza_layout.json`. |
| `Assets/Editor/PlazaBuilder.cs` | Materiales, remapeo, post y renderer (`SetupRendering`), y armado de toda la escena (incluido el helipuerto y la escena del menú). |
| `Assets/Editor/HudSetup.cs` | Fuentes TMP, cartas y unidades (menú 4: crea o actualiza los assets sin pisar el arte) y objeto Game en la escena (menú 5). |
| `Assets/Editor/IconBuilder.cs` | Ícono de la app (menú 7): renderiza en Play la "A" con bandera sobre el Sol de Mayo a `Assets/Art/Icon/AppIcon.png` y lo asigna en Player Settings. |
| `Assets/Editor/SlotBuilder.cs` | Genera los slots policiales desde lanes, sus texturas y materiales (menú 6). |
| `Assets/Shaders/TiltShift.shader` | Desenfoque de bordes (Full Screen Pass) y blur circular del menú (globales `_MenuBlur`, `_MenuCenter`). |
| `Assets/Scripts/Game/` | `Loc` (idioma `L`: español/inglés, interfaz, cartas, unidades y titulares), `Headlines` (banco de titulares de la marquesina), `GameState` (dinero, popularidad, gobernabilidad, mandatos, gabinete, curación del cierre), `CardDeck` (mazo finito), `CardDef`, `UnitDef`, `WaveDirector` (oleadas), `Enemy`, `Defense` (+ niveles, mejora, curación, proyectiles, gas, motorizada), `PoliceSlots`, `LaneArrows`, `StreetMood` (decorado por popularidad), `Fidget` (micro-movimientos), `GameFlow` (menú ↔ partida), `GameAudio` (música, volúmenes, pool de efectos y ambiente), `Snd` (API de efectos + `SndLoop` por objeto), `Leaderboard` (ranking top 10), `SirenLights` (balizas de patrulleros), `Spin`, `FxKit`. |
| `Assets/Scripts/UI/` | `GameHud` (parcial: HUD, flujo y cierre, alertas, arrastre de cartas `GameHudDrag`, compra con mazo vacío `GameHudShop`, menú), `CardView`, `MenuScreen` (menú, ranking y pedido de nombre), `LoadingScreen`, `VolumeSlider`, `UiKit` (fuentes, logo con `LogoWave`). |
| `Assets/Resources/` | `Cards/`, `Units/`, `Music/` (5 temas MP3, streaming), `SFX/` (45 WAV), `UI/` (fuentes), `FX/`, `GameAssets.asset`. |

**Orden para regenerar todo**

1. Blender: `exec` de `plaza_design_v3.py`, después de `plaza_design_v2.py` (con `plaza_map` recargado).
2. Blender: recargar `gen_assets`, `hq`, `props_hq`, `buildings_kit`, `porteno_kit`, `casa_v2` y `heli_kit`, y regenerar los modelos tocados. `props_hq.ground()` es obligatorio si cambió el layout.
3. Unity: `Assets/Refresh`.
4. Unity: `Argensimia/2. Remap Model Materials`, solo si hay materiales o modelos nuevos. Deja `police_officer.fbx` legible (lo necesita la Escopeteros para sacar el escudo).
5. Unity: `PlazaBuilder.SetupRendering()` vía `eval`, solo si cambió el post o el renderer.
6. Unity: `Argensimia/4. Create Game Assets`, si cambiaron cartas o unidades en `HudSetup.cs`.
7. Unity: `Argensimia/3. Build Plaza Scene`. Llama a `SetupMaterials`, arma el helipuerto, agrega Game (estado, mazo, oleadas, HUD, GameFlow) y PoliceSlots, y guarda la escena.

## Coordenadas y cámara

El mundo usa metros en Unity: X hacia el este, Z hacia el norte e Y hacia arriba. La Casa Rosada está al oeste (X negativo) y el monumento en el origen.

- **Plaza (pavimento):** X de −40 a 40 y Z de −26 a 26. El borde de los canteros está en ±37 × ±23, con un paseo perimetral de 3 m.
- **Alturas:** pavimento 0,16; tapa de los canteros 0,44; calles 0.
- **Cámara de juego:** mira desde el norte hacia el sur, con la Casa Rosada a la derecha y los manifestantes entrando por la izquierda (este). FOV 50°, inclinación 50°, rotación Y 180°, objetivo (−6, 0, 3,9), distancia 72 m → posición (−6, 55,16, 50,18). Plano cercano a 1 m.
- **Cámara del menú:** posición (−111,98, 29,39, −1,44), rotación (42,03, 87,98, 0), detrás del helipuerto mirando la fachada trasera de la Casa Rosada (`GameFlow.menuCamPos/menuCamEuler`).
- **Sol:** `Euler(52, −72, 0)`. Viene del este, ilumina la fachada de la Casa Rosada y proyecta sombras hacia el oeste para no cruzar la plaza.
- **SVG (`plaza_map.svg`):** misma vista que la cámara, con arriba = sur y derecha = oeste. Escala 11,25 px/m; el rectángulo de la plaza va de (45, 25) a (945, 610) px.
- **Códigos de color del SVG:** canteros `#738F2D`; carriles con trazo `#B25757` de grosor ≥ 8; refuerzos con el mismo color y grosor 5.
- **Modelos:** frente en Blender −Y = Unity +Z con yaw 0. `ground` se exporta rotado 180° (`flip=True`); los datos de layout ya están en coordenadas de mundo.

## Diseño de la plaza

La plaza tiene 4 carriles de manifestantes de este a oeste, simétricos respecto del eje E–O, más 2 entradas de refuerzo norte–sur. El pasto ocupa el 39% de la superficie.

| Elemento | Definición | Ancho |
| --- | --- | --- |
| Carriles exteriores | z = ±(15,5 + 1,6·sen(π(x+5)/20)) | 4,6 m |
| Carriles interiores | z = ±(5,8 + 3,4·b(x) + 0,7·sen(π(x−5)/20)·(1−b(x))), con b(x) = e^−(x/12)² | 4,6 m |
| Refuerzos | x = 0, desde el borde de la plaza hasta la explanada | 3,6 m |
| Conectores | x = ±23, entre carril interior y exterior | 3,2 m |
| Explanada central | círculo centrado en el origen | radio 9,5 m |

**Reglas de ubicación de objetos** (`plaza_design_v2.py`)

- **Despeje:** ningún objeto a menos de 2,4 m del eje de un carril ni a menos de 1,8 m de un refuerzo. Los bancos pueden acercarse a 1,7 / 1,5 m.
- **Faroles:** sobre el cordón de cada cantero (0,55 m hacia adentro), cada ~10 m. Hay 4 más en la explanada y otros en el perímetro.
- **Bancos:** en el borde pavimentado (0,55 m afuera del cantero), de espaldas al pasto. Hay un cesto cada dos bancos.
- **Árboles:** solo low poly (sin palmeras), en el centro del cantero, a más de 2,5 m del borde y separados ≥ 7 m. Hay 18 en total, 2 de ellos jacarandás.
- **Bordes de cantero:** arbustos oscuros cada 1,75 m a 0,95 m del borde, con un macizo de flores cada 4 posiciones.
- **Monumento:** escala 0,85, cantero circular de flores con radio 4,2–5,4 m y una rejita baja con radio 5,75 m.
- **Perímetro:** sin reja. El único vallado es el de la Casa Rosada.

El piso es pavimento color arena rosada, con un cordón de piedra de 0,45 m alrededor de cada cantero, sin discos ni anillos decorativos.

## Casa Rosada, helipuerto y franja policial

La Casa Rosada ocupa todo el lado oeste, detrás de un único vallado negro con 4 entradas alineadas con los carriles.

- **Modelo:** las alas tienen 2 vanos (fachada de 50,4 m). Se escala 0,98 en juego: ~49 m de frente y ~25 m de profundidad, con el frente en x ≈ −47,5.
- **Vallado:** está en x = −46, a lo ancho de z ±25,6, y vuelve hacia atrás hasta x = −74. Las entradas están en z = ±5,17 y ±16,22, con 4,4 m de ancho y un par de faroles cada una (`GateZ` y `GateW` en `PlazaBuilder.cs`, iguales a `casa_gates_z` del layout).
- **Helipuerto:** en la explanada trasera, centro (−87, 0,16, 0). Plataforma de 19 m con cordón, anillo amarillo, círculo blanco, la H, 16 balizas y manga de viento. Encima, el helicóptero presidencial (escala 1,15) con el rotor girando (`Spin`).
- **Escena del menú (fija en la escena):** `MenuHidrante` (camión en (−70,66, 0, −30,06)) y dos `MenuPatrullero` cruzados en V en (−73,3, 0, 30,9) y (−68,1, 0, 32,0). En runtime `GameFlow` agrega la multitud y las gomas prendidas.
- **Colores:** muros `#e3979a`, molduras `#f7d2c2` y techos verde cobre `#5f9e86`. Todas las ventanas son oscuras.

## Ciudad y edificios

Las manzanas se llenan con un kit de modelos de 14 × 14 m, sin repetir modelo entre vecinos. La fila de enfrente sigue una secuencia curada.

| Modelo | Estilo | Pisos | Rasgos | Fachada en juego |
| --- | --- | --- | --- | --- |
| `building_a/b/c` | Haussmanniano | 4/5/6 | Mansarda de pizarra con lucarnas, persianas, frontones en el piso noble | Cremas (`M_FacadeV_*`) |
| `building_it_a/b/c` | Italianizante | 4/5/3 | Ventanas en arco, balaustradas, terraza con macetas y tanque | Ocres y rosados (`M_FacadeW_*`) |
| `building_nv_a/b` | Art nouveau | 5/6 | Bow-window central, cúpulas de zinc en las esquinas | Cremas |
| `building_dc_a/b` | Art déco | 7/6 | Pilastras continuas, últimos pisos escalonados, mástil | Piedra (`M_FacadeS_*`) |
| `building_pt_a/b/c`, `building_otto` | Porteño ecléctico | 5–7 | Basamento oscuro con atlantes, bow-windows, mansarda, agujas con linterna | Piedra |
| `building_corner` | Haussmanniano de esquina | 5 | Cúpula en la ochava | Cremas |
| `building_round` | Torre circular | 7 + cúpula (~41 m) | Balcones corridos, tambor, cúpula de zinc con linterna | Piedra |

- **Manzanas:** `PlazaBuilder.Blocks()` debe coincidir con `gen_assets.layout_blocks()`. Las calles son de 10 m, la Av. de Mayo al este de 12 m.
- **Fila de enfrente:** es la fila sur, z −70 a −36, la que mira a la plaza.
- **Fila cercana a la cámara:** es la fila norte, con altura × 1,35–1,6, para que asome desenfocada en el borde inferior.
- **Planta baja:** todos los modelos tienen locales con toldo (rojo, verde o azul), cartel y puerta.

## Dirección de arte

La clave es luz principal cálida con sombras frías, verdes y rosas vivos, y un piso que no se queme. Todo se define en `BuildLightingAndCamera` y `SetupRendering`.

| Ajuste | Valor |
| --- | --- |
| Sol | color (1, 0,87, 0,68), intensidad 2,5, sombras suaves con fuerza 0,82 |
| Ambiente trilight | cielo (0,64, 0,68, 0,84), ecuador (0,70, 0,64, 0,64), suelo (0,36, 0,28, 0,26) |
| Tonemapping | ACES |
| Ajustes de color | exposición +0,3, contraste 10, saturación 20 |
| Balance de blancos | temperatura 6, tinte 3 |
| Split toning | sombras violeta (0,5, 0,47, 0,64), luces naranja (1, 0,8, 0,58) |
| Bloom | umbral 1,0, intensidad 0,55 |
| Viñeta | 0,24, cálida |
| SSAO | renderer feature: intensidad 1,6, radio 0,6 |
| Tilt-shift (juego) | `TiltShift.mat`: foco 0,46, franja nítida 0,31, intensidad arriba/abajo 0,55, bordes 1,0 desde 0,78 |
| Blur circular (menú) | nítido en un círculo de radio 0,3 (alto de pantalla) centrado entre helipuerto y helicóptero, transición 0,2, desenfoque 22 px |
| Fondo y niebla | (0,93, 0,82, 0,70), lineal de 170 a 420 m |

- **Paleta clave:** pavimento `#ddb59c`, pasto `#6c9f2e`, arbustos `#3f6b1f` y `#4a7a26`, copas `#4a7a26` → `#9cc23f`, ventanas `#2b3240` (sin emisión).
- **Emisión:** los materiales sin emisión deben quedar con `_EmissionColor` negro. `MakeMat` ya lo fuerza; si no, aparecen ventanas color arena.
- **Vida (`Fidget`):** nada queda estático. Policía de la fila de adelante firme (respira), filas de atrás se reacomodan con pasitos; manifestantes inquietos (miran a los costados, rebotan); motos con el motor en marcha (cuerpo vibra, ruedas fijas); retroceso al disparar. La columna organizada se mueve en bloque, firme.

## Datos para gameplay

`BlenderSrc/plaza_layout.json` es la fuente de verdad del mapa para cualquier sistema de juego. Todo está en coordenadas de mundo (x, z).

| Clave | Contenido |
| --- | --- |
| `lanes` | 4 polilíneas de este a oeste (de x ≈ +42 a −42), una por carril de manifestantes. |
| `reinforce` | 2 segmentos norte y sur en x = 0, del borde de la plaza a la explanada: entradas laterales. |
| `casa_gates_z` | z de las 4 entradas del vallado: −16,22, −5,17, 5,17, 16,22. |
| `lawns` | polígonos de los 14 canteros, zonas no transitables. |
| `center_r`, `ring` | radio de la explanada (9,5) y radios del cantero del monumento. |
| `plaza` | semiextensión de la plaza (40, 26). |

- **Cambios de mapa:** se hacen editando `plaza_design_v3.py`, o el SVG con sus códigos de color, y regenerando. Nunca editando la escena.

## Loop de partida

- **Arranque:** pantalla de carga (`LoadingScreen`) hasta que haya ~15 cuadros fluidos seguidos (mín. 0,6 s, máx. 8 s); recién ahí entra el menú con la cinta.
- **Menú → partida:** el juego abre en el menú. NUEVA PARTIDA panea la cámara (6,5 s, curva por arriba del mapa) hasta la vista de juego mientras el helicóptero aterriza; al llegar aparece el HUD. Si ya se jugó, la escena se recarga en la pose del menú (estado inicial limpio) y arranca el paneo: el corte no se nota.
- **Mandato:** previa de planificación con reloj de 30 s a la izquierda de ¡ARRANCAR! (se pausa con la pausa y en los paneos; tictac los últimos 5 s; al agotarse cierra zoom/colocación/confirmación y arranca, aplicando las crisis); el cierre con las 3 cartas no tiene tiempo (se juegan cartas y se colocan o mejoran defensas; la oleada se recalcula en vivo con la popularidad) → ¡ARRANCAR! → oleada → cierre → siguiente previa. Sin límite de mandatos (`totalMandates = 0`).
- **Cierre:** sueldo + efecto de los ministros + la policía en el mapa recupera hasta 25% de su vida máxima (30% con Min. Seguridad, solo a las que les falta) → pantalla OBTUVISTE 3 CARTAS PARA TU MAZO (ver Sistema de cartas) → CONTINUAR → se completa la mano con lo que haya en el mazo → crisis forzadas.
- **Fin:** al perder la gobernabilidad, o al RENUNCIAR desde la pausa (cuando el helicóptero queda suspendido se recarga la escena para cortar la partida de fondo y el menú arranca con el resultado), se oculta el HUD, aparece la multitud en el helipuerto, la cámara panea hasta la vista del menú y el helicóptero despega y se detiene justo en la pose de flotación. El menú queda con la cinta del resultado ("PERDISTE LA GOBERNABILIDAD" / "EL PRESIDENTE RENUNCIÓ") y el detalle debajo ("Se fue en helicóptero en el mandato N").
- **Puntaje:** (25 × costo de cada unidad eliminada o detenida + dinero restante / 10) × mandatos jugados (el que estabas jugando cuenta) × popularidad final (mín. 10%). Se muestra en la cinta del menú al terminar; si entra al top 5 se pide el nombre (`Leaderboard`, PlayerPrefs) y aparece en el panel RANKING del menú.
- **Vender carta:** en el zoom, el tacho arriba a la izquierda descarta la carta (no vuelve al mazo) a cambio de $100; no aplica a crisis.
- **Valores iniciales:** $2.500, +$40/s (solo durante la oleada), 78% de popularidad (TRANQUI), gobernabilidad 20/20, mazo de 15 (quedan 10 tras repartir la mano de 5).
- **Sueldo de cierre:** $2.000 + $40 × popularidad (+$500 con Super Ministro).

## Popularidad

La popularidad (0–100) es la dificultad: define cuánta gente viene y qué tan enojada.

| Estado | Rango | Efecto |
| --- | --- | --- |
| TRANQUI | ≥ 70 | Bronca ×1 (vida, daño, velocidad) |
| TENSO | ≥ 50 | Bronca ×1,15 / ×1,1 / ×1,05 |
| CALLE CALIENTE | ≥ 30 | ×1,35 / ×1,25 / ×1,1; 1 especial garantizado por oleada; gomas y humo en las bocacalles |
| ESTALLIDO | < 30 | ×1,6 / ×1,5 / ×1,2; la oleada llega en 3 pulsos; edificios incendiados; 2 crisis forzadas por cierre |

- **Cantidad:** `GameState.Difficulty` = 0,6 con 100% hasta 2 con 0%; multiplica el presupuesto de la oleada.
- **Decorado (`StreetMood`):** menos popularidad = calles más llenas, gomas, humo y fuego, siempre fuera de la plaza y de la Casa Rosada.
- **Dónde baja:** cartas (Impuestazo −16%, Privatización −10%, La maquinita −8%, Blanqueo −6%, Refuerzo militar −5%, Dólar soja −4%, Relevo −3%), crisis Escándalo −15%, ministros por mandato (Super Ministro −3%, Min. Economía −2%, Min. Seguridad −1%), jubilado golpeado o detenido −0,1% (máx. 1 vez/s por jubilado). El cacerolero que llega solo resta gobernabilidad.
- **Dónde sube:** Aguinaldo +12%, Plan platita +10%, Fútbol para todos +6%, Cadena nacional +5%, Min. Desarrollo +3% y Vocero +2% por mandato.
- **Números rojos:** toda pérdida se muestra flotando en rojo: sobre el manifestante (−% popularidad, −N GOB) y en el panel (popularidad agrupada cada 0,6 s; plata exacta de la Devaluación).

## Oleadas y manifestantes

`WaveDirector` arma cada oleada gastando un presupuesto por mandato: 20, 32, 48, 68, 100 y después ×1,18 por mandato (vida ×1,08). Se multiplica por la dificultad y se reduce con Feriado puente o Pacto social.

- **Carriles activos:** uno más por mandato: 1, 2, 3 y desde el mandato 4 los 4 (primero los interiores). Una flecha **roja** en el extremo este marca cada carril activo; los nuevos titilan fuerte en la previa.
- **Entradas norte y sur:** recién desde el mandato 5 (50% de la oleada entra por el refuerzo, x = 0, y toma un carril de su lado).
- **Especiales:** salen más hacia el final de la oleada y se ven en la esquina por donde van a entrar durante la previa. Columnas extra con Paro general.
- **Retención:** todo retén policial (vallado, infanterías, motorizada en espera, hidrante, brigada) frena al **100%** a los que llegan, sin tope. La única excepción es el encapuchado.

| Unidad | Costo | Vida | Golpe | Ritmo | Vel. | Gob. al llegar | Desde | Peso | Especial | Rasgos | Diseño |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Manifestante | 1 | 40 | 5 | 1/s | 1,4 | 1 | M1 | 6 | — | — | Cuerpo común, a veces bandera, bombo o cartel |
| Cacerolero | 1 | 30 | 0 | — | 1,2 | 1 | M1 | 2,2 | — | No ataca | Cacerola y cuchara |
| Jubilado | 1 | 25 | 2 | 0,5/s | 0,8 | 1 | M2 | 1,5 | — | −0,1% popularidad al ser golpeado o detenido | Bajo y encorvado, boina, anteojos, bastón, bolsa |
| Encapuchado | 2 | 50 | 15 | 1,2/s | 2,0 | 2 | M2 | 1,6 | Sí | Daño ×3 a vallas; 30% de saltar cada retén (salto en arco con giro, titular en la marquesina) | Todo negro, pasamontañas con ranura, palo |
| Piquetero | 3 | 80 | 6 | 1/s | 1,2 | 2 | M3 | 1,1 | Sí | Cada 10 s prende una goma que bloquea 8 s el slot libre más cercano | Flaco y alto, gorra, pañuelo rojo, palo largo, goma |
| Mortero | 4 | 35 | 25 (área 2 m) | 0,25/s | 1,0 | 3 | M3 | 0,8 | Sí | Con la línea libre avanza; solo cuando un retén lo frena se planta y dispara, a veces al obstáculo de adelante y a veces a la defensa más cercana a tiro (14 m) en los carriles de al lado | Grandote y agazapado: rastas largas, anteojos, barba, mochila, remera roja y jogging celeste; caño casero encintado cruzado al cuerpo (el disparo sale de la boca con humo) |
| Columna organizada | 6 | 600 | 10 | 1/s | 0,7 | 5 | M4 | 0,5 | Sí | 5 personas; +20% de velocidad a los que tiene a 4 m; resiste 50% del empuje | Bloque rígido de dos filas (3 + 2), remera y gorra rojas, jean oscuro, bandera adelante |

Velocidades base; en juego se multiplican por 1,3 (`speedScale`). Datos en `Assets/Resources/Units/` (`UnitDef`, con `jumpChance`).

## Defensas policiales

Todas ocupan slots, frenan al 100% y se rompen al quedar sin gobernabilidad (vida), liberando el slot. El alcance en las cartas se muestra en casilleros (1 casillero = 6,8 m).

| Defensa | Vida | Qué hace | Diseño NV1 → NV2 → NV3 |
| --- | --- | --- | --- |
| Vallado | 300 | Vallas en L al fondo (oeste) del slot. Al recibir golpes se inclinan hacia atrás y vibran. | 1 línea de `valla_l` × 2 → doble línea + bolsas de arena + reja alta antidisturbios → + muro New Jersey con franjas, chapas policiales y balizas |
| Infantería | 600 | Cuerpo a cuerpo (8 de daño, 1/s, 1,5 m). No llega detrás de un vallado. | 6 agentes (2 filas de 3) → 8 → 10 |
| Escopeteros | 400 | Dispara en su carril hasta 2 casilleros (9 de daño, 1,2/s); prioriza a los que golpean un vallado adelante, después a los que frenan otra defensa, después al más cercano. Trazador rápido con impacto. | Misma formación sin escudo, escopetas con guardamano naranja: 6 → 8 → 10 |
| Lanzagases | 120 | Granada de gas hasta 3 casilleros, al azar entre su carril y los vecinos (los que tengan gente), al grupo más denso: 4 dps en 3,5 m y −40% de velocidad. Tira un agente al azar. | 1 agente → 2 → 3 + cajones de granadas |
| Detenciones | 200 | Cada 4 s saca de juego a un manifestante cercano (prioriza el de mayor costo; no puede con columnas). | 3 agentes → 4 → 5 |
| Motorizada | 300 | Cada 15 s sale una pasada a los porrazos hasta Av. de Mayo: 40 de daño, empuja 3 m y aturde 1 s; en el slot aparece otra recargando. | 2 motos → 3 → 4 (primitivas) |
| Camión hidrante | 900 | Cañón giratorio en el techo, hasta 4 casilleros, en su carril o los vecinos, apuntando solo al más cercano: moja, empuja 2 m/s, −60% de velocidad, 3 dps. Tanque de 8 s que se gasta solo mientras moja; vacío, recarga 6 s. Sirena roja y azul titilando. Ocupa 2 slots. | `police_truck` × 1,25 → + chapas laterales con franja y paragolpes → + jaula en el techo y cañón ×1,3 |

- **Niveles:** vida ×2 / ×3,5; daño ×1,75 / ×3; ritmo ×1,25 / ×1,6 (Detenciones usa el de daño: cada 4 s → 2,3 s → 1,3 s). NV3 suma 1 casillero de alcance a Escopeteros, Lanzagases e Hidrante. La infantería pega a 1 / 2 / 3 manifestantes por golpe; el gas agranda su área 20% por nivel; el hidrante empuja +25% y su tanque dura más. Los agentes crecen 5% por nivel.
- **Mejora:** se suelta una carta igual sobre la defensa del mapa (arrastrando desde la mano, o tocándola en modo colocar). Cobra el costo de la carta y la consume. La vida máxima sube y la vida actual suma la diferencia. Estallido de partículas doradas y "¡NIVEL N!" flotando.
- **Marcas:** rombos de nivel sobre la defensa, mirando a cámara (plateados en NV2, dorados en NV3). Mientras se arrastra una carta, las defensas mejorables de ese tipo muestran un óvalo dorado pulsante y una flecha; la que está bajo el dedo se agranda.
- **Curación:** `Defense.HealAll(frac)`: al cierre de cada mandato (25%, o 30% con Min. Seguridad) y con Relevo (40%); partículas verdes en cada una curada.

## Interfaz (HUD)

El HUD es uGUI generado por código en runtime (`GameHud.cs` y parciales), sin UI serializada en la escena: sobrevive a cada rebuild. Es landscape, táctil y mobile-first. Se oculta en el menú y aparece al terminar el paneo.

| Zona | Contenido |
| --- | --- |
| Arriba izquierda | Presupuesto con moneda e ingreso por segundo. Debajo, en la previa, PRÓXIMA OLEADA con la lista de unidades (especiales en rojo); durante la oleada pasa a MANIFESTANTES RESTANTES con el total y el detalle por tipo (por entrar + en la Plaza). |
| Arriba centro | MANDATO N con barra de progreso de la oleada y GOBERNABILIDAD x/20. |
| Arriba derecha | POPULARIDAD % con barra y cinta de estado (TRANQUI / TENSO / CALLE CALIENTE / ESTALLIDO). Gabinete de ministros y botón de menú (hamburguesa) que abre la pausa. |
| Abajo | Mano de 5 cartas en abanico leve (quedan huecos si el mazo se vació). Mazo a la izquierda con contador (rojo en 0; con 0 el mazo se atenúa y arriba aparece MAZO VACÍO con 1 CARTA $500 y 10 CARTAS $4.200); CAMBIAR MANO ($300) y ¡ARRANCAR! a la derecha. |
| Footer | Marquesina de noticiero con ÚLTIMO MOMENTO; los titulares de cartas y eventos entran al instante (ver Marquesina). |

- **Pausa:** fondo oscuro, título PAUSA afuera y arriba del contenedor. Adentro: volumen de MÚSICA y EFECTOS (barras tipo progressbar sin %, pasos de 5%), CONTINUAR y RENUNCIAR.
- **Crisis forzadas:** cada crisis de la mano tiene un halo rojo pulsante y el cartel "CRISIS: SE APLICA AL ARRANCAR"; sobre ¡ARRANCAR! aparece "N CRISIS SE APLICA(N) AL ARRANCAR". Al arrancar con crisis en la mano se pide confirmación ("Se van a aplicar las cartas de crisis forzadas. ¿Seguro que querés comenzar?") con VOLVER y COMENZAR.
- **Escala:** referencia 1600×900. Teléfonos (≥16:10) escalan por alto; tablets 4:3 por ancho.
- **Input:** módulo UI del Input System. Todos los botones tienen feedback de presión.
- **Fuentes:** TMP SDF dinámicas en `Resources/UI/`: Roboto Black y Bold (texto general), Lilita One (OFL; títulos, botones y todo texto con contorno ≥ 36 pt, vía `UiKit.Txt`) y Luckiest Guy (Apache 2.0; el logo ARGENSIMIA con `UiKit.Logo`: degradé dorado→naranja, contorno marrón, sombra dura y ondita por letra con `LogoWave`). TTF y licencias en `Assets/UI/Fonts/`.
- **Sprites:** procedurales (redondeados, sombra, íconos), sin assets de imagen.

## Menú principal

- **Vista:** cámara del menú (ver Coordenadas) con blur circular: nítidos el helicóptero y el helipuerto, todo lo demás desenfocado.
- **Helicóptero:** recién despegado, inclinado y saliendo hacia la derecha de la pantalla (offset (−3, 8,4, −5,5) desde el helipuerto, rotación (13, −120, 10)). Suspendido con una flotación muy sutil (~20 cm, 1–2° de balanceo) y el rotor en "cámara lenta" (230°/s). Si se mueve a mano en Play, flota alrededor de la nueva pose.
- **Ambiente:** ~170 manifestantes en anillo alrededor del helipuerto y ~80 de relleno a los costados (quedan desenfocados), 5 gomas prendidas con humo, el hidrante tirando agua en arco a la multitud sur con la sirena encendida, y los patrulleros en barricada.
- **Ranking:** panel RANKING abajo a la izquierda (28 px de margen) con el top 5 (medallas, nombre, puntaje, mandatos).
- **Derecha (abajo, de abajo hacia arriba):** SALIR, idioma ENGLISH/ESPAÑOL (recarga la escena), GITHUB (abre el repo) y PANTALLA COMPLETA/VENTANA (pantalla completa en el monitor donde está la ventana; ventana al 75% de ese monitor), con la versión en blanco debajo. SALIR y el modo de pantalla solo aparecen en escritorio.
- **UI:** logo ARGENSIMIA (ARG con la bandera, ENSIMIA banana, ondita por letra); cinta roja inclinada con filetes dorados "¡DEFENDÉ LA CASA ROSADA!" (entra sellada con rebote y pulsa cada 2,4 s; tras una partida muestra el resultado y el detalle abajo, con el contorno TMP por defecto); MÚSICA y EFECTOS en contenedores individuales flotantes; NUEVA PARTIDA.

## Audio

- **Música (`GameAudio`):** 5 temas en `Resources/Music/` (MP3, importados como Vorbis q 0,6 en streaming), en este orden de loop (`GameAudio.Playlist`): La Marcha del Balcón → Fuerzas de la Plaza → Candombe en la Plaza → La Gran Marcha → Corte de Calle. Temas nuevos que no estén en la lista van al final. Pasa de uno a otro con fundido cruzado de 2,5 s. Objeto persistente (`DontDestroyOnLoad`): la música no se corta entre menú y partida ni al recargar.
- **Volúmenes:** música (70% por defecto) y efectos (80%) guardados en `PlayerPrefs`. Al mover EFECTOS suena un tap de muestra.
- **Efectos (`Snd`):** `Snd.Ui(id)` para interfaz (sigue sonando en pausa) y `Snd.At(id, pos)` para eventos del mapa (se pausan con `AudioListener.pause`). Pool de 24 voces 2D, máx. 4 iguales a la vez, enfriamiento por clip, leve variación de tono y paneo según la posición en pantalla (fuera de cuadro, 45%). Volumen relativo por clip en `GameAudio.Gain`.
- **Clips (`Resources/SFX`, generados por `BlenderSrc/sfx_gen.py`, mono, Vorbis; cortos descomprimidos al cargar, loops comprimidos en memoria):**
  - Interfaz: `ui_tap` (todos los `Tap`), `ui_error` (sacudón), `card_draw/flip/zoom/play/shuffle`, `coin` (cha-ching), `spend` (moneda), `pop_up` (marimba), `pop_down` (wah-wah), `mood_down` (sube la bronca), `stamp` (ministro, cinta del menú), `upgrade`, `heal`, `crisis` (cortina de noticiero), `evade`, `wave_start` (pitazo + multitud + bombo), `fanfare` (fin de mandato), `gameover` (trombón triste).
  - Policía: `place` (pisotones) + extra por tipo (valla, sirena del hidrante, moto, silbato), `shotgun`, `rubber_hit`, `baton`, `fence_clang` y `defense_hit` (golpes recibidos), `gas_launch`, `gas_hiss`, `cuffs` + `whistle_short` (detención), `moto_pass`, `break_crash`, `siren_yelp`, `loop_water` (chorro del hidrante, con `SndLoop`).
  - Otros: `boo` (abucheo al perder popularidad), `clock_tick` (reloj de la previa), `unit_down` (cae un agente), `heli_start` (turbina al perder).
  - Manifestantes: `poof` (sale de escena), `boing` (salto del encapuchado), `gate_bang` (llega al vallado), `mortar_launch`, `explosion`, `fire_ignite` (goma).
- **Ambiente (loops):** `loop_crowd` (multitud sintetizada), `loop_bombo` (murga a 120 bpm), `loop_cacerola`, `loop_heli`. Menú: multitud + bombo + cacerolas + helicóptero (tono según `GameFlow.RotorSpeed`). Oleada: multitud y bombo crecen con la cantidad de manifestantes y la bronca, cacerolas si hay caceroleros, paneados a la izquierda (este). Previa y cierre: murmullo lejano que sube con la bronca.

## Idioma

- `L.En` (PlayerPrefs `lang`): el código está en español y `L.T("texto")` devuelve el inglés si hay traducción; `UiKit.Txt` lo aplica solo a los textos fijos y los dinámicos usan `L.T`/`L.F`. Cartas (`L.CardTitle/CardDesc/CardHeadline`), unidades (`L.UnitName`) y titulares (`L.Headlines`, `L.Filler`) tienen su banco en inglés.
- Al cambiar de idioma se recarga la escena. Texto nuevo: escribirlo en español y sumar la traducción en `Loc.cs`.

## Marquesina (titulares)

`Headlines.cs` concentra todos los titulares. Cada evento tiene 2–6 variantes; se elige una al azar sin repetir la anterior. Los eventos frecuentes tienen enfriamiento para no saturar.

| Evento | Enfriamiento |
| --- | --- |
| Arranca el mandato / mandato final / victoria / pérdida de gobernabilidad | — |
| Sube o baja el estado de ánimo | — |
| Manifestante llega al vallado / columna llega | 10 s / 8 s |
| Encapuchado salta / piquetero prende goma / mortero | 6 s / 15 s / 15 s |
| Detención / detención de jubilado | 20 s / 12 s |
| Cae una defensa / cae un vallado | 4 s / 10 s |
| Mejora de nivel, cambio de ministro, crisis evitada, cartas nuevas, cierre (si hubo curación) | — |
| CAMBIAR MANO / CAMBIAR carta del cierre / mazo vacío / compra de cartas | 20 s / 10 s / 90 s / 8 s |

- **Títulos:** cortos, para que el título de la carta no baje de ~33 pt (autoajuste 22–40 en 324 de ancho); el detalle va en la descripción.
- **Cartas:** `CardDef.headline` admite variantes separadas por `|` (las extra salen de `HudSetup.MoreHeadlines`); todas las cartas tienen 3.
- **Relleno:** cuando no hay noticias, sale de una bolsa barajada (no repite hasta agotarla) con titulares de clima político y frases célebres de la política argentina ("la casa está en orden", "estamos mal pero vamos bien", "pasaron cosas", "no hay plata", "¡que se vayan todos!", etc.), puestas como citas de funcionarios del juego.

## Sistema de cartas

Las cartas cuestan dinero; el dinero entra por segundo durante la oleada, por el sueldo del cierre y por cartas que bajan la popularidad.

- **Tipos:** Colocar (policía en slots), Efecto, Ministro (pasivo, gabinete de 3; si está lleno reemplaza al más viejo) y Crisis (entran solas a la mano desde el mandato 2; se evitan pagando su costo o se aplican al arrancar).
- **Mazo finito:** arranca con 15 cartas (se reparten 5 y quedan 10) (las copias iniciales) y no se rebaraja: lo que se juega se consume y, cuando se vacía, aparece encima del mazo (superpuesto, por delante) el panel MAZO VACÍO para comprar cartas al azar: 1 CARTA $500 o 10 CARTAS $4.200 (−16%), en previa u oleada (`CardDeck.buyCost`/`buy10Cost`, `GameHudShop.cs`). Usan las mismas probabilidades por rareza que el cierre; llenan primero los huecos de la mano y el resto queda en el mazo. CAMBIAR MANO ($300) devuelve la mano al fondo del mazo y roba otra. La carta que pisa una crisis (Escándalo, etc.) también vuelve al fondo del mazo; si hay un hueco en la mano, la crisis lo ocupa.
- **Cartas del cierre:** al cerrar cada mandato entran 3 cartas al mazo (pantalla OBTUVISTE 3 CARTAS PARA TU MAZO) (Común 60 / Poco común 30 / Rara 10, raras ×1,8 desde el mandato 4, sin repetir en la tanda). Cada una tiene CAMBIAR $100: gira y aparece otra al azar distinta de las que están en pantalla; se puede usar una sola vez por carta (queda CAMBIADA). CONTINUAR las mete en lugares al azar del mazo.
- **Colocables:** se juegan con tap → ACTIVAR → tocar el mapa, o arrastrando la carta desde la mano al mapa (chica y por arriba del dedo; soltar sobre la mano la devuelve). Sobre una defensa igual la sube de nivel; si no, ocupa el slot libre más cercano. No hay fusión en la mano. Una colocable se ve deshabilitada solo si no hay slot libre ni defensa igual para mejorar.
- **Datos:** `CardDef` (ScriptableObject) en `Assets/Resources/Cards/`, creados o actualizados por `HudSetup` (menú 4) sin pisar el arte. Campos nuevos: `salaryDelta`, `govDelta`, `maxGovDelta`, `govPerMandate`, `healNow`, `healBonus`.
- **Arte:** generado con `BlenderSrc/card_art.py` (Nano Banana 2 Lite en Atlas Cloud; prompts en `docs/CARD_ART_PROMPTS.md`). Revisión en `CardArt/review/index.html` (no versionado); las elegidas (`_selected.json`) se copian a `Assets/Art/Cards/<id>.jpg` (sprite, máx. 1024) y se asignan en `CardDef.art`. Marco 3:2 de 320×218. Sin arte se dibuja la inicial con "ARTE PENDIENTE".
- **Zoom:** tap en la carta → centro con inclinación 3D, ACTIVAR y X (las crisis suman EVITAR $X). Sin plata "FALTA $X"; sin slots ni mejora posible "SIN LUGAR".

**Cartas** (precios: base +30% y después −5%, redondeados a $10). Copias iniciales = mazo de 15; las de 0 solo entran por el cierre.

| Carta | Tipo | Rareza | Costo | Efecto | Copias iniciales |
| --- | --- | --- | --- | --- | --- |
| Vallado | Colocar | Común | $620 | Ver Defensas | 3 |
| Infantería | Colocar | Común | $1.140 | Ver Defensas | 2 |
| Escopeteros | Colocar | Poco común | $1.380 | Ver Defensas | 2 |
| Lanzagases | Colocar | Poco común | $1.620 | Ver Defensas | 1 |
| Detenciones | Colocar | Poco común | $1.760 | Ver Defensas | 1 |
| Motorizada | Colocar | Rara | $2.230 | Ver Defensas | 0 |
| Camión hidrante | Colocar | Rara | $3.470 | Ver Defensas (2 slots) | 0 |
| Impuestazo | Efecto | Común | Gratis | +$3.000, −16% popularidad | 1 |
| La maquinita | Efecto | Común | Gratis | +$2.000, −8% popularidad | 1 |
| Cadena nacional | Efecto | Común | $520 | +5% popularidad | 2 |
| Fútbol para todos | Efecto | Común | $700 | +6% popularidad, −$5/s | 0 |
| Plan platita | Efecto | Poco común | $1.850 | +10% popularidad | 1 |
| Blanqueo | Efecto | Poco común | $1.000 | +$20/s, −6% popularidad | 1 |
| Aguinaldo | Efecto | Poco común | $1.200 | +12% popularidad, −$12/s | 0 |
| Relevo | Efecto | Poco común | $900 | La policía recupera 40% de vida ya, −3% popularidad | 0 |
| Decretazo | Efecto | Poco común | Gratis | +$3.500, −2 gobernabilidad (nunca deja en 0) | 0 |
| Feriado puente | Efecto | Poco común | $1.520 | Próxima oleada −20% (solo en la previa) | 0 |
| Dólar soja | Efecto | Poco común | Gratis | +$4.000, −4% popularidad (solo en la previa) | 0 |
| Préstamo FMI | Efecto | Rara | Gratis | +$5.000, −$15/s | 0 |
| Refuerzo militar | Efecto | Rara | $2.800 | +3 gobernabilidad y +3 al máximo, −5% popularidad | 0 |
| Privatización | Efecto | Rara | Gratis | +$7.000, −10% popularidad, −$10/s | 0 |
| Pacto social | Efecto | Rara | $2.600 | Próxima oleada −30%, −$10/s (solo en la previa) | 0 |
| Min. Economía | Ministro | Poco común | $1.850 | +$25/s, −2% por mandato | 0 |
| Min. del Interior | Ministro | Poco común | $1.900 | +1 gobernabilidad por mandato, −$10/s | 0 |
| Vocero | Ministro | Poco común | $1.300 | +2% por mandato, −$8/s | 0 |
| Super Ministro | Ministro | Rara | $2.900 | +$50/s y +$500 de sueldo, −3% por mandato | 0 |
| Min. Seguridad | Ministro | Rara | $2.230 | +20% vida de la policía, recuperación al cierre 30%, −1% por mandato | 0 |
| Min. Desarrollo | Ministro | Rara | $2.470 | +3% por mandato | 0 |
| Devaluación | Crisis | — | $1.850 para evitarla | −30% del dinero | — |
| Paro general | Crisis | — | $2.470 | +1 columna organizada en la próxima oleada | — |
| Tarifazo | Crisis | — | $1.240 | −$10/s | — |
| Escándalo | Crisis | — | $1.520 | −15% popularidad | — |

Las crisis forzadas entran 1 por cierre desde el mandato 2 (2 si la popularidad es < 30%). Todo es provisorio hasta balancear.

## Slots policiales

Todo elemento policial de una carta ocupa un slot y solo se coloca sobre el camino. Hay 40 slots: 10 por cada uno de los 4 carriles horizontales.

| Parámetro | Valor |
| --- | --- |
| Tamaño | 5,4 m a lo largo del carril × 4,0 m de ancho (carril de 4,6 m); la marca es un círculo de 4 m |
| Separación | ~1,4 m entre slots (1 casillero = 6,8 m entre centros) |
| Tramo | x −36 a 31. Al este queda libre la entrada de las columnas |
| Orientación | Alineado a la tangente del carril (yaw guardado por slot) |
| Ocupación | 1 ítem = 1 slot; el hidrante ocupa 2 contiguos |

- **Fuente:** se generan desde `lanes` de `plaza_layout.json`.
- **Visual:** losa hundida semitransparente (`M_SlotRecess`); al ocupar el slot se oculta para que el ítem quede limpio sobre el pavimento.
- **Brillo:** los libres titilan siempre en dorado con una ola suave a lo largo de cada carril; en modo colocar o al arrastrar una carta brillan fuerte.
- **Colocar:** el tap o el soltar engancha primero una defensa igual mejorable (1,2 m más allá de su mitad de largo); si no, el slot libre más cercano dentro de 4 m (o el par más cercano para el hidrante). Se cobra recién al colocar o mejorar.
- **Goma del piquetero:** ocupa 8 s el slot libre más cercano.
- **Código:** `PoliceSlots.cs` (runtime, incluye `NeighborLanes` para gases e hidrante) y `SlotBuilder.cs` (menú `Argensimia/6. Rebuild Police Slots`).

## Releases

- **Builds:** desde Unity (MCP `build`) a `Builds/Windows/Argensimia.exe`, `Builds/macOS/Argensimia.app` (universal Intel + Apple Silicon) y `Builds/Linux/Argensimia.x86_64`. Solo entra `Plaza.unity`. Backend Mono (permite compilar Windows y Linux desde la Mac).
- **Paquetes** (`BlenderSrc/release.py`, salida en `Release/`, no versionado): instalador de Windows con NSIS (`makensis`, necesita locale UTF-8), `.dmg` con hdiutil (app firmada ad hoc) y AppImage armado a mano (squashfs zstd + runtime type2 de AppImage). Ícono desde `Assets/Art/Icon/AppIcon.png` (.ico de 16 a 256 px).
- **Publicación:** `gh release create vX.Y.Z` con los tres archivos; README con capturas en `docs/screenshots/`.


- [ ] Balance fino de costos, sobre todo de la tanda nueva
- [ ] Modelos de Blender para las motos (hoy primitivas) y prefabs dedicados de defensas (los niveles hoy suman primitivas)
- [ ] Cabildo y Catedral Metropolitana como edificios emblema alrededor de la plaza
- [ ] Vida en las calles: árboles de vereda, mesas de bar, quioscos, peatones, más tránsito
- [ ] Texturas pintadas a mano y AO horneada (salto a nivel Ikariam)
- [ ] Slots en las conexiones de los carriles exteriores
- [ ] Revisar la versión editable (artifact) del spec, que puede quedar desfasada de este archivo
- [x] Sistema de oleadas real, unidades con habilidades y columnas organizadas
- [x] Menú principal, loop menú ↔ partida, pausa y música
- [x] Mazo finito, cartas del cierre con CAMBIAR, niveles en el mapa y curación del cierre
- [x] Arte de las 32 cartas
- [x] Efectos de sonido y ambiente sonoro (sintetizados; reemplazables por samples con el mismo nombre en `Resources/SFX`)

**Problemas conocidos**

- Los italianizantes y art nouveau superan los 18.000 polígonos cada uno (balaustradas). Si hay problemas de rendimiento en móvil, reducir la densidad de balaustres o usar LOD.
- Unity está en **Recompile After Finished Playing** (Preferences > General > Script Changes While Playing): editar scripts con el juego en Play ya no rompe la partida; los cambios entran al salir.
- El servidor MCP de Unity a veces corta la conexión después de un recompile, refresh o al entrar a Play; se resuelve reintentando.
- `CanvasGroup` con alpha < 1 sobre `UiKit.Panel` mezcla el borde dorado con el interior (se ve marrón): no usarlo en paneles.
- TMP: no subir `outlineWidth` por encima de 0,16 (el de `UiKit.OutlineMat`) en textos chicos; desborda el SDF y el texto se ve como bloques negros (pasó en el detalle del menú).
- `M_Sign` es el material de los carteles de protesta. Los carteles de locales usan `M_ShopSign`; no hay que mezclarlos.
