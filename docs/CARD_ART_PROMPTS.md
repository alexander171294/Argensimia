# Argensimia — prompts del arte de cartas

Generados con `BlenderSrc/card_art.py` (Nano Banana 2 Lite en Atlas Cloud, 3:2). Las imágenes en revisión quedan en `CardArt/review/` (fuera de `Assets`, no versionado); se aprueban y recién ahí se importan como `CardDef.art`.

## Dónde va el arte

Marco de 320 × 218 px en la carta (3:2, se recorta con *envelope*). Arriba a la izquierda va el chip del tipo y a la derecha el de rareza (~15% superior). En las colocables, el 28% inferior lo tapan los badges VIDA / DAÑO / RITMO / ALCANCE.

## Estilo común (va al principio de cada prompt)

> Illustration for a playing card in a mobile tower-defense game set in Plaza de Mayo, Buenos Aires, where the player defends the pink Casa Rosada (government house) with riot police against crowds of protesters, term after term, as satirical political humor. Art style: warm, semi-realistic painterly illustration in the spirit of Ikariam and Clash Royale card art; chunky caricature characters with big, readable silhouettes and slightly exaggerated proportions; sunny late-afternoon light, warm golden key light with cool violet shadows, lively saturated greens and pinks, soft painted textures, clean shapes, strong focal point, playful tone (never gory or graphic). Every person is a fictional generic character, not resembling any real politician or public figure. No text, no letters, no numbers, no logos, no flags with writing, no watermark, no border or frame.

- Colocables: > Composition: landscape 3:2. Keep the main subject in the central band. The top 15% and the bottom 28% of the image will be covered by game UI badges, so keep them as simple background or ground with no important details.
- Resto: > Composition: landscape 3:2. Keep the main subject centered. The top 15% will be covered by small UI chips, so keep it as simple background.

## Cartas

| Carta | id | Tipo | Color | Escena |
| --- | --- | --- | --- | --- |
| Vallado | `vallado` | Colocar | `#1FA59A` | A line of black steel crowd-control barriers (bike-rack style) across pink-beige plaza paving, a couple of them dented and leaning from impacts, a concrete block with yellow-black hazard stripes, the pink Casa Rosada softly out of focus behind. |
| Infantería | `infanteria` | Colocar | `#3557B8` | A row of riot police in dark navy uniforms, helmets with raised visors and big round transparent shields, standing shoulder to shoulder, low heroic angle, determined faces, plaza and palm trees behind. |
| Escopeteros | `balas_goma` | Colocar | `#C0612B` | Two riot police officers without shields aiming pump-action riot shotguns with bright orange foregrips, bright muzzle flash, small rubber pellets streaking forward, light smoke, dynamic diagonal composition. |
| Lanzagases | `lanzagases` | Colocar | `#6D7D2E` | A lone riot police officer with a gas mask firing a chunky 37mm grenade launcher, a green tear-gas canister arcing through the air with a white smoke trail, a soft gas cloud blooming in the distance. |
| Detenciones | `detencion` | Colocar | `#4B4F9E` | An arrest squad of two burly riot officers comically carrying away a flailing cartoon protester by the arms, his feet off the ground, shiny handcuffs, slapstick tone, plaza behind. |
| Motorizada | `motorizada` | Colocar | `#24408F` | Three police motorcycles in navy and white roaring forward in formation, riders in helmets with batons raised, dust and speed lines, front three-quarter view, Avenida de Mayo buildings blurred behind. |
| Camión hidrante | `hidrante` | Colocar | `#2A7FD1` | A big armored riot-police water cannon truck, chunky and cartoonish, its roof cannon blasting a powerful arc of water, red and blue siren lights glowing, spray and droplets sparkling in the sun. |
| Impuestazo | `impuestazo` | Efecto | `#D8452F` | A giant cartoon tax-collector magnet hovering over the city, pulling coins and banknotes out of the pockets of small surprised citizens, money swirling upward. |
| La maquinita | `maquinita` | Efecto | `#4F8A3A` | An old cast-iron banknote printing press running wild in a vault, spitting endless stacks of generic pastel banknotes (no real currency design), bills flying everywhere, steam puffing. |
| Cadena nacional | `cadena_nacional` | Efecto | `#C7477A` | A wall of many vintage CRT televisions in a cozy living room, all showing the same fictional president giving a speech at a podium with light-blue and white flags behind, a bored family on the sofa. |
| Fútbol para todos | `futbol` | Efecto | `#2E8BC0` | A happy family packed on a sofa cheering wildly at a football match on TV, mate gourd and snacks, confetti, players in plain jerseys with no crests on the screen. |
| Plan platita | `plan_platita` | Efecto | `#8A3FC4` | A cheerful fictional official on a balcony tossing paper envelopes stuffed with banknotes to a delighted crowd below, envelopes fluttering like confetti. |
| Blanqueo | `blanqueo` | Efecto | `#2F9A4A` | Banknotes being literally laundered: a washing machine full of foamy cash and bills hanging on a clothesline to dry on a Buenos Aires rooftop with domes in the background. |
| Aguinaldo | `aguinaldo` | Efecto | `#C4883F` | Happy workers opening bonus envelopes and celebrating around a smoky asado grill, string lights, festive mood. |
| Relevo | `operativo` | Efecto | `#3A5F9E` | A fresh riot police squad stepping off a police bus to relieve tired officers who rest, drink water and get bandaged, a soft green healing glow around them. |
| Decretazo | `decretazo` | Efecto | `#8A5A2B` | A gigantic rubber stamp slamming down on a decree document on a polished desk, papers flying, golden wax seal, ink splash, dramatic top light. |
| Feriado puente | `feriado_puente` | Efecto | `#E07B2A` | A packed highway of small cars loaded with surfboards, beach umbrellas and coolers heading to the Atlantic coast, bright sun, the beach and ocean ahead. |
| Dólar soja | `dolar_soja` | Efecto | `#9AA83A` | A golden soybean field at harvest with a combine harvester, generic green banknotes raining down from the sky like a harvest of money, happy farmer. |
| Préstamo FMI | `prestamo` | Efecto | `#D9961C` | A huge open suitcase overflowing with generic green banknotes, chained to a heavy iron ball and shackle, a faceless banker in a suit seen from behind handing it over across a long table. |
| Refuerzo federal | `refuerzo_federal` | Efecto | `#2C6A5A` | A column of federal gendarmerie troops in olive-green uniforms and green trucks arriving at the plaza, marching in formation, dust and sunlight. |
| Privatización | `privatizacion` | Efecto | `#6A4A8A` | A giant auction gavel slamming next to a toy-like state train and oil refinery displayed on an auction pedestal, coins bursting into the air. |
| Pacto social | `acuerdo_cgt` | Efecto | `#8A2F2F` | A firm handshake across a negotiation table between a burly union leader in a work jacket and a government official in a suit, folders, coffee cups and medialunas, window with the pink Casa Rosada facade. |
| Min. Economía | `min_economia` | Ministro | `#B8860B` | Half-body caricature portrait of a fictional economy minister in a grey suit and round glasses, holding a giant calculator, rising charts and coins floating behind. |
| Min. del Interior | `min_interior` | Ministro | `#5A6E3A` | Half-body caricature portrait of a fictional interior minister with a mustache, on the phone, a big map of the provinces of Argentina with pins on the wall behind him. |
| Vocero | `vocero` | Ministro | `#B8477A` | Half-body caricature portrait of a fictional presidential spokesperson at a press-room podium full of microphones, camera flashes popping, confident smile. |
| Super Ministro | `super_ministro` | Ministro | `#C9A227` | Half-body caricature of a fictional muscular minister in a suit with a flowing cape, juggling three briefcases, golden heroic glow, no emblem on the chest. |
| Min. Seguridad | `min_seguridad` | Ministro | `#2B4A8A` | Half-body caricature portrait of a fictional stern security minister in a dark suit holding a police radio, arms crossed, riot police silhouettes behind. |
| Min. Desarrollo | `min_desarrollo` | Ministro | `#3F9A7A` | Half-body caricature of a warm fictional social development minister serving soup at a community kitchen, smiling neighbors and kids around. |
| Devaluación | `devaluacion` | Crisis | `#7A2D2D` | A peso coin deflating like a sad balloon while a green arrow rockets upward, price tags flying, a shocked shopper, dramatic red-tinted light. |
| Escándalo | `escandalo` | Crisis | `#6A2A5A` | A leaked audio scandal: a smartphone playing a recording with glowing sound waves, a sweating fictional politician caught in a swarm of paparazzi camera flashes. |
| Paro general | `paro_general` | Crisis | `#8A1F1F` | General strike: closed metal shop shutters, an empty bus stop, workers with arms crossed under plain red union banners without writing, quiet street. |
| Tarifazo | `tarifazo` | Crisis | `#7A3D1F` | A shocked family staring at exploding utility bills: a lightbulb, a gas stove flame and a bus ticket shooting upward like rockets with sparks. |

Cada prompt termina con «Use <color> as the dominant accent color of the image.» y, en ministros y crisis, una línea de tono.
