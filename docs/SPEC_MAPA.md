# Argensimia — Spec del mapa Plaza de Mayo

Actualizado: 2026-10-16 · Alex. Versión editable: https://claude.ai/code/artifact/966a4aee-2203-47c0-8cd8-33cb5aacf6e0

## Resumen

Argensimia es un tower defense en la Plaza de Mayo: el jugador defiende la Casa Rosada con policía frente a columnas de manifestantes. Es el único mapa del juego, así que se trabaja con nivel de detalle alto.

- **Proyecto:** `/Users/alexander/git/argensimia/Argensimia`, Unity 6000.4.1f1, URP (renderer de PC en Forward+).
- **Escena:** `Assets/Scenes/Plaza.unity`, generada por código. No se edita a mano: cualquier cambio manual se pierde en el próximo rebuild.
- **Objetivo visual:** ilustración semirrealista y cálida estilo Ikariam, con vista isométrica en perspectiva. Edificios, Casa Rosada y escenografía con detalle; árboles, gente, autos y policía quedan low poly a propósito.
- **Momento del día:** tarde soleada. Ventanas oscuras, faroles y vidrieras apagados.

## Reglas de trabajo

Todo se hace con los MCP de Blender y de Unity; nada de CLI de Unity ni control de pantalla.

- **Blender MCP** (`execute_blender_code`): lee y escribe archivos del proyecto y genera y exporta los modelos. Antes de generar hay que recargar los módulos en este orden: `gen_assets` → `hq` → resto. `hq` parchea `A.box`, `A.finish` y `GA.M`; sin él se exporta sin UV ni texturas.
- **Unity MCP:** `menu` (pasos de Argensimia), `eval` (C# en el editor), `recompile`, `capture_game_view`.
- Para revisar de cerca se crea una cámara temporal `TmpCloseCam` y se captura con `capture_game_view(camera=…)`. Hay que borrarla antes de guardar.
- **Una sola instancia de Blender y de Unity:** los cambios van en secuencia. No conviene usar varios agentes tocando la escena en paralelo.
- Si el servidor de Unity no responde tras un recompile o refresh, alcanza con reintentar la llamada.

## Pipeline y archivos

Los modelos se generan por código en Blender y se exportan a FBX. Los colores salen de `palette.txt` y la escena la arma `PlazaBuilder.cs`.

| Archivo | Qué hace |
| --- | --- |
| `BlenderSrc/gen_assets.py` | Primitivas, clase `A`, `face_box`/`face_cy`/`face_put`, `tri_prism`, edificios haussmannianos (`building`) y props base. |
| `BlenderSrc/hq.py` | Biseles, sombreado suave, UV de caja, texturas procedurales (`T_*` + normales). |
| `BlenderSrc/props_hq.py` | Faroles, palmeras, árboles con gradiente de copa, arbustos, canteros de flores, reja, monumento, edificio de esquina con cúpula, y `ground()` (asfalto, veredas, plaza, canteros, cordones, anillo del monumento). |
| `BlenderSrc/buildings_kit.py` | Estilos italianizante, art nouveau y art déco, y la torre circular (`rotunda`). |
| `BlenderSrc/casa_v2.py` | Casa Rosada (alas de 2 vanos, pabellones, bloque central). |
| `BlenderSrc/palette.txt` | Una línea por material: `nombre hex emisión [textura metros_por_repetición]`. |
| `BlenderSrc/plaza_map.svg` | Mapa limpio de la plaza (canteros, carriles, refuerzos). El boceto original está en `plaza_map_boceto.svg`. |
| `BlenderSrc/plaza_design_v3.py` | Geometría armónica: carriles, refuerzos, conectores y canteros. Escribe `plaza_map.svg`. |
| `BlenderSrc/plaza_map.py` | Lee el SVG y lo convierte a coordenadas de mundo. |
| `BlenderSrc/plaza_design_v2.py` | Ubica los objetos de la plaza. Escribe `plaza_props.txt` y `plaza_layout.json`. |
| `Assets/Editor/PlazaBuilder.cs` | Materiales, remapeo, post y renderer (`SetupRendering`), y armado de toda la escena. |
| `Assets/Shaders/TiltShift.shader` | Desenfoque de bordes (Full Screen Pass). |

**Orden para regenerar todo**

1. Blender: `exec` de `plaza_design_v3.py`, después de `plaza_design_v2.py` (con `plaza_map` recargado).
2. Blender: recargar `gen_assets`, `hq`, `props_hq`, `buildings_kit` y `casa_v2`, y regenerar los modelos tocados. `props_hq.ground()` es obligatorio si cambió el layout.
3. Unity: `Assets/Refresh`.
4. Unity: `Argensimia/2. Remap Model Materials`, solo si hay materiales o modelos nuevos.
5. Unity: `PlazaBuilder.SetupRendering()` vía `eval`, solo si cambió el post o el renderer.
6. Unity: `Argensimia/3. Build Plaza Scene`. Llama a `SetupMaterials` y guarda la escena.

## Coordenadas y cámara

El mundo usa metros en Unity: X hacia el este, Z hacia el norte e Y hacia arriba. La Casa Rosada está al oeste (X negativo) y el monumento en el origen.

- **Plaza (pavimento):** X de −40 a 40 y Z de −26 a 26. El borde de los canteros está en ±37 × ±23, con un paseo perimetral de 3 m.
- **Alturas:** pavimento 0,16; tapa de los canteros 0,44; calles 0.
- **Cámara:** mira desde el norte hacia el sur, con la Casa Rosada a la derecha y los manifestantes entrando por la izquierda (este). Tiene FOV 50°, inclinación 50°, rotación Y 180°, objetivo (−6, 0, 1), distancia 72 m y plano cercano a 1 m.
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

## Casa Rosada y franja policial

La Casa Rosada ocupa todo el lado oeste, detrás de un único vallado negro con 4 entradas alineadas con los carriles.

- **Modelo:** las alas tienen 2 vanos (fachada de 50,4 m). Se escala 0,98 en juego: ~49 m de frente y ~25 m de profundidad, con el frente en x ≈ −47,5.
- **Vallado:** está en x = −46, a lo ancho de z ±25,6, y vuelve hacia atrás hasta x = −74. Las entradas están en z = ±5,17 y ±16,22, con 4,4 m de ancho y un par de faroles cada una (`GateZ` y `GateW` en `PlazaBuilder.cs`, iguales a `casa_gates_z` del layout).
- **Franja policial:** va de x −40 a −46 (unos 6 m). Ahí se colocan las fichas del jugador: vallas, agentes e hidrante. Las vallas policiales fijas se sacaron.
- **Colores:** muros `#e3979a`, molduras `#f7d2c2` y techos verde cobre `#5f9e86`. Todas las ventanas son oscuras.

## Ciudad y edificios

Las manzanas se llenan con un kit de 11 modelos de 14 × 14 m, sin repetir modelo entre vecinos. La fila de enfrente sigue una secuencia curada.

| Modelo | Estilo | Pisos | Rasgos | Fachada en juego |
| --- | --- | --- | --- | --- |
| `building_a/b/c` | Haussmanniano | 4/5/6 | Mansarda de pizarra con lucarnas, persianas, frontones en el piso noble | Cremas (`M_FacadeV_*`) |
| `building_it_a/b/c` | Italianizante | 4/5/3 | Ventanas en arco, balaustradas, terraza con macetas y tanque | Ocres y rosados (`M_FacadeW_*`) |
| `building_nv_a/b` | Art nouveau | 5/6 | Bow-window central, cúpulas de zinc en las esquinas | Cremas |
| `building_dc_a/b` | Art déco | 7/6 | Pilastras continuas, últimos pisos escalonados, mástil | Piedra (`M_FacadeS_*`) |
| `building_corner` | Haussmanniano de esquina | 5 | Cúpula en la ochava | Cremas |
| `building_round` | Torre circular | 7 + cúpula (~41 m) | Balcones corridos, tambor, cúpula de zinc con linterna | Piedra |

- **Manzanas:** `PlazaBuilder.Blocks()` debe coincidir con `gen_assets.layout_blocks()`. Las calles son de 10 m, la Av. de Mayo al este de 12 m.
- **Fila de enfrente:** es la fila sur, z −70 a −36, la que mira a la plaza. La torre circular va en la esquina NE de la manzana x 5–40.
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
| Tilt-shift | `TiltShift.mat`: foco 0,46, franja nítida 0,31, intensidad arriba/abajo 0,55, bordes 1,0 desde 0,78 |
| Fondo y niebla | (0,93, 0,82, 0,70), lineal de 170 a 420 m |

- **Paleta clave:** pavimento `#ddb59c`, pasto `#6c9f2e`, arbustos `#3f6b1f` y `#4a7a26`, copas `#4a7a26` → `#9cc23f`, ventanas `#2b3240` (sin emisión).
- **Emisión:** los materiales sin emisión deben quedar con `_EmissionColor` negro. `MakeMat` ya lo fuerza; si no, aparecen ventanas color arena.

## Datos para gameplay

`BlenderSrc/plaza_layout.json` es la fuente de verdad del mapa para cualquier sistema de juego (pathfinding, oleadas, slots de defensa). Todo está en coordenadas de mundo (x, z).

| Clave | Contenido |
| --- | --- |
| `lanes` | 4 polilíneas de este a oeste (de x ≈ +42 a −42), una por carril de manifestantes. |
| `reinforce` | 2 segmentos norte y sur en x = 0, del borde de la plaza a la explanada: entradas especiales de refuerzo. |
| `casa_gates_z` | z de las 4 entradas del vallado: −16,22, −5,17, 5,17, 16,22. |
| `lawns` | polígonos de los 14 canteros, zonas no transitables. |
| `center_r`, `ring` | radio de la explanada (9,5) y radios del cantero del monumento. |
| `plaza` | semiextensión de la plaza (40, 26). |

- **Puntos de entrada:** los grupos de manifestantes entran por la calle este (Av. de Mayo, x ≈ 45–60) y por las calles norte y sur (z ≈ ±31). En la escena actual son grupos estáticos de `BuildCrowds`.
- **Slots de defensa:** todavía no están definidos. La franja policial (x −40 a −46) y los bordes de los carriles son los candidatos naturales.
- **Cambios de mapa:** se hacen editando `plaza_design_v3.py`, o el SVG con sus códigos de color, y regenerando. Nunca editando la escena.

## Pendientes y problemas conocidos

- [ ] Cabildo y Catedral Metropolitana como edificios emblema alrededor de la plaza
- [ ] Vida en las calles: árboles de vereda, mesas de bar bajo toldos, quioscos, peatones, más taxis y colectivos
- [ ] Texturas pintadas a mano para fachadas, techos y piso, y AO horneada en los modelos (salto a nivel Ikariam)
- [ ] Grupos de manifestantes en `BuildCrowds`: posiciones fijas pensadas para la cámara anterior; revisar contra los carriles
- [ ] Definir slots de defensa y el flujo de fichas policiales
- [ ] Ajuste fino del encuadre de la cámara una vez cerrado el layout

**Problemas conocidos**

- Los italianizantes y art nouveau superan los 18.000 polígonos cada uno (balaustradas). Si hay problemas de rendimiento en móvil, reducir la densidad de balaustres o usar LOD.
- El servidor MCP de Unity a veces corta la conexión después de un recompile o refresh; se resuelve reintentando.
- `M_Sign` es el material de los carteles de protesta. Los carteles de locales usan `M_ShopSign`; no hay que mezclarlos.
