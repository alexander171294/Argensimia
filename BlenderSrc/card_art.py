# Argensimia — arte de cartas con Nano Banana 2 Lite (Atlas Cloud).
# Uso (MCP de Blender, que tiene red en la máquina de Alex):
#   exec(open(".../BlenderSrc/card_art.py").read()); start(["vallado", ...] o None = todas)
#   progress()  -> estado;  gallery() -> regenera CardArt/review/index.html
# La API key se lee de ~/.argensimia_atlas_key (no va al repo).
# Salida: CardArt/review/<id>_vN.jpg (fuera de Assets: no entra al juego hasta aprobarla).
import json, os, threading, time, urllib.request, html

PROJECT = '/Users/alexander/git/argensimia/Argensimia'
OUT = PROJECT + '/CardArt/review'
MODEL = 'google/nano-banana-2-lite/text-to-image'
API = 'https://api.atlascloud.ai/api/v1/model/generateImage'

STYLE = (
    "Illustration for a playing card in a mobile tower-defense game set in Plaza de Mayo, Buenos Aires, "
    "where the player defends the pink Casa Rosada (government house) with riot police against crowds of protesters, "
    "term after term, as satirical political humor. "
    "Art style: warm, semi-realistic painterly illustration in the spirit of Ikariam and Clash Royale card art; "
    "chunky caricature characters with big, readable silhouettes and slightly exaggerated proportions; "
    "sunny late-afternoon light, warm golden key light with cool violet shadows, lively saturated greens and pinks, "
    "soft painted textures, clean shapes, strong focal point, playful tone (never gory or graphic). "
    "Every person is a fictional generic character, not resembling any real politician or public figure. "
    "No text, no letters, no numbers, no logos, no flags with writing, no watermark, no border or frame. "
)
FULL = ("Full-bleed painting that fills the entire canvas edge to edge: the scene (sky, buildings, ground) continues to every border; "
        "no letterbox bars, no solid color bands, no blank margins, no frame, no icons, no interface elements, no buttons, no stickers. ")
# El arte va en un marco 3:2; arriba van los chips de tipo/rareza y en las colocables abajo van los badges de stats.
# No se lo decimos al modelo (si no, dibuja íconos falsos): solo le pedimos el sujeto centrado y bordes tranquilos.
LAYOUT_PLACE = (FULL + "Landscape 3:2. Main subject in the middle of the frame, slightly above center, with open ground in front of it "
                "and sky or background above it; keep the edges and corners calm and uncluttered. ")
LAYOUT_OTHER = (FULL + "Landscape 3:2. Main subject centered, with a bit of sky or background above it; keep the corners calm and uncluttered. ")

# id: (título, tipo, color de la carta, escena)
CARDS = {
 # --- colocables (policía)
 "vallado": ("Vallado", "Colocar", "#1FA59A",
   "A line of black steel crowd-control barriers (bike-rack style) holding back an angry, pushing crowd of cartoon protesters with drums, "
   "pots and blank placards, seen from the protected side: the barriers bend and lean under the pressure but hold. "
   "No police anywhere. The pink Casa Rosada softly out of focus behind the crowd."),
 "infanteria": ("Infantería", "Colocar", "#3557B8",
   "A row of riot police in dark navy uniforms, helmets with raised visors and big round transparent shields, standing shoulder to shoulder, "
   "low heroic angle, determined faces, plaza and palm trees behind."),
 "balas_goma": ("Escopeteros", "Colocar", "#C0612B",
   "Two riot police officers without shields aiming pump-action riot shotguns with bright orange foregrips, bright muzzle flash, "
   "small rubber pellets streaking forward, light smoke, dynamic diagonal composition."),
 "lanzagases": ("Lanzagases", "Colocar", "#6D7D2E",
   "A lone riot police officer with a gas mask firing a chunky 37mm grenade launcher, a green tear-gas canister arcing through the air "
   "with a white smoke trail, a soft gas cloud blooming in the distance."),
 "detencion": ("Detenciones", "Colocar", "#4B4F9E",
   "An arrest squad of two burly riot officers comically carrying away a flailing cartoon protester by the arms, his feet off the ground, "
   "shiny handcuffs, slapstick tone, plaza behind."),
 "motorizada": ("Motorizada", "Colocar", "#24408F",
   "Three police motorcycles in navy and white roaring forward in formation, riders in helmets with batons raised, dust and speed lines, "
   "front three-quarter view, Avenida de Mayo buildings blurred behind."),
 "hidrante": ("Camión hidrante", "Colocar", "#2A7FD1",
   "A big armored riot-police water cannon truck, chunky and cartoonish, its roof cannon blasting a powerful arc of water, "
   "red and blue siren lights glowing, spray and droplets sparkling in the sun."),
 # --- desbloqueables (cada 3 mandatos sobrevividos, entre partidas)
 "puntero": ("Puntero", "Colocar", "#E0A030",
   "A chubby cheerful neighborhood political broker in an apron and a cap, grilling choripán sausages on a small street parrilla grill "
   "with thick appetizing smoke, handing a sandwich forward with a wink, a little cash box with banknotes peeking out on a folding table, plaza behind."),
 "contenedores": ("Contenedores", "Colocar", "#3E8E4E",
   "Two big green municipal trash dumpsters on wheels tipped over and pushed together as a barricade across plaza paving, "
   "dented and sturdy, a hooded protester comically bouncing off the top of them, no police, pink Casa Rosada blurred behind."),
 "infiltrado": ("Infiltrado", "Colocar", "#5D6B7A",
   "An undercover cop disguised as a protester blending into a cheerful crowd: grey hoodie, cap, backpack and a blank placard, "
   "a secret police earpiece and a pair of handcuffs peeking from his pocket, sly sideways glance, the crowd unaware."),
 "comisario": ("Comisario", "Colocar", "#2F4F9F",
   "A big-bellied police commissioner with a thick mustache and a peaked cap with golden visor, shouting orders through a megaphone, "
   "riot officers behind him standing taller with a glowing golden aura, heroic low angle."),
 "concertina": ("Concertina", "Colocar", "#7B8794",
   "Coils of shiny razor wire (concertina) laid on wooden sawhorse frames across the plaza paving, glinting in the sun, "
   "a couple of cartoon protesters hopping and tiptoeing comically over it with torn trousers, no gore."),
 "requisa": ("Requisa", "Colocar", "#6B4FA0",
   "Two police officers at a folding table searching protesters' backpacks, a growing pile of confiscated items: wooden sticks, "
   "homemade cardboard mortar tubes, balaclavas and slingshots, a surprised protester with empty hands, playful tone."),
 "desvio": ("Desvío", "Colocar", "#E8742C",
   "A traffic officer in a bright reflective vest with a whistle and white gloves pointing the way, orange traffic cones and a striped "
   "detour barrier redirecting a line of confused cartoon protesters down a side street, a big curved arrow painted on the street."),
 "estruendo": ("Estruendo", "Colocar", "#E04A2F",
   "A flash-bang stun grenade bursting in the middle of the plaza with a bright white flash, comic shockwave rings and cartoon stars, "
   "protesters knocked dizzy with spirals over their heads, nobody hurt, slapstick tone."),
 "drone": ("Drone", "Colocar", "#3A9AD9",
   "A chunky police quadcopter drone with blue and red beacon lights hovering above the plaza crowd, a camera lens looking down, "
   "shooting a small paintball pellet at a hooded protester, sunny sky."),
 "tanqueta": ("Tanqueta", "Colocar", "#4A5560",
   "A chunky cartoonish armored riot-control vehicle with big wheels, navy and grey armor plates and a turret cannon "
   "launching a huge arc of white foam over the crowd, foam bubbles everywhere, siren lights glowing."),
 # --- efectos
 "impuestazo": ("Impuestazo", "Efecto", "#D8452F",
   "A giant cartoon tax-collector magnet hovering over the city, pulling coins and banknotes out of the pockets of small surprised citizens, "
   "money swirling upward."),
 "maquinita": ("La maquinita", "Efecto", "#4F8A3A",
   "An old cast-iron banknote printing press running wild in a vault, spitting endless stacks of generic pastel banknotes "
   "(no real currency design), bills flying everywhere, steam puffing."),
 "cadena_nacional": ("Cadena nacional", "Efecto", "#C7477A",
   "A wall of many vintage CRT televisions in a cozy living room, all showing the same fictional president giving a speech at a podium "
   "with light-blue and white flags behind, a bored family on the sofa."),
 "futbol": ("Fútbol para todos", "Efecto", "#2E8BC0",
   "A happy family packed on a sofa cheering wildly at a football match on TV, mate gourd and snacks, confetti, "
   "players in plain jerseys with no crests on the screen."),
 "plan_platita": ("Plan platita", "Efecto", "#8A3FC4",
   "A cheerful fictional official on a balcony tossing paper envelopes stuffed with banknotes to a delighted crowd below, "
   "envelopes fluttering like confetti."),
 "blanqueo": ("Blanqueo", "Efecto", "#2F9A4A",
   "Banknotes being literally laundered: a washing machine full of foamy cash and bills hanging on a clothesline to dry "
   "on a Buenos Aires rooftop with domes in the background."),
 "aguinaldo": ("Aguinaldo", "Efecto", "#C4883F",
   "Happy workers opening bonus envelopes and celebrating around a smoky asado grill, string lights, festive mood."),
 "operativo": ("Relevo", "Efecto", "#3A5F9E",
   "A fresh riot police squad stepping off a police bus to relieve tired officers who rest, drink water and get bandaged, "
   "a soft green healing glow around them."),
 "decretazo": ("Decretazo", "Efecto", "#8A5A2B",
   "A gigantic rubber stamp slamming down on a decree document on a polished desk, papers flying, golden wax seal, ink splash, "
   "dramatic top light."),
 "feriado_puente": ("Feriado puente", "Efecto", "#E07B2A",
   "A packed highway of small cars loaded with surfboards, beach umbrellas and coolers heading to the Atlantic coast, "
   "bright sun, the beach and ocean ahead."),
 "dolar_soja": ("Dólar soja", "Efecto", "#9AA83A",
   "A golden soybean field at harvest with a combine harvester, generic green banknotes raining down from the sky "
   "like a harvest of money, happy farmer."),
 "prestamo": ("Préstamo FMI", "Efecto", "#D9961C",
   "A huge open suitcase overflowing with generic green banknotes, chained to a heavy iron ball and shackle, "
   "a faceless banker in a suit seen from behind handing it over across a long table."),
 "refuerzo_federal": ("Refuerzo militar", "Efecto", "#2C6A5A",
   "A column of federal gendarmerie troops in olive-green uniforms and green trucks arriving at the plaza, "
   "marching in formation, dust and sunlight."),
 "privatizacion": ("Privatización", "Efecto", "#6A4A8A",
   "A giant auction gavel slamming next to a toy-like state train and oil refinery displayed on an auction pedestal, "
   "coins bursting into the air."),
 "acuerdo_cgt": ("Pacto social", "Efecto", "#8A2F2F",
   "A firm handshake across a negotiation table between a burly union leader in a work jacket and a government official in a suit, "
   "folders, coffee cups and medialunas, window with the pink Casa Rosada facade."),
 # --- ministros (caricaturas ficticias, de medio cuerpo)
 "min_economia": ("Min. Economía", "Ministro", "#B8860B",
   "Half-body caricature portrait of a fictional economy minister in a grey suit and round glasses, holding a giant calculator, "
   "rising charts and coins floating behind."),
 "min_interior": ("Min. del Interior", "Ministro", "#5A6E3A",
   "Half-body caricature portrait of a fictional interior minister with a mustache, on the phone, a big map of the provinces "
   "of Argentina with pins on the wall behind him."),
 "vocero": ("Vocero", "Ministro", "#B8477A",
   "Half-body caricature portrait of a fictional presidential spokesperson at a press-room podium full of microphones, "
   "camera flashes popping, confident smile."),
 "super_ministro": ("Super Ministro", "Ministro", "#C9A227",
   "Half-body caricature of a fictional muscular minister in a suit with a flowing cape, juggling three briefcases, "
   "golden heroic glow, no emblem on the chest."),
 "min_seguridad": ("Min. Seguridad", "Ministro", "#2B4A8A",
   "Half-body caricature portrait of a fictional stern security minister in a dark suit holding a police radio, "
   "arms crossed, riot police silhouettes behind."),
 "min_desarrollo": ("Min. Desarrollo", "Ministro", "#3F9A7A",
   "Half-body caricature of a warm fictional social development minister serving soup at a community kitchen, "
   "smiling neighbors and kids around."),
 # --- crisis
 "devaluacion": ("Devaluación", "Crisis", "#7A2D2D",
   "A peso coin deflating like a sad balloon while a green arrow rockets upward, price tags flying, a shocked shopper, "
   "dramatic red-tinted light."),
 "escandalo": ("Escándalo", "Crisis", "#6A2A5A",
   "A leaked audio scandal: a smartphone playing a recording with glowing sound waves, a sweating fictional politician "
   "caught in a swarm of paparazzi camera flashes."),
 "paro_general": ("Paro general", "Crisis", "#8A1F1F",
   "General strike: closed metal shop shutters, an empty bus stop, workers with arms crossed under plain red union banners "
   "without writing, quiet street."),
 "tarifazo": ("Tarifazo", "Crisis", "#7A3D1F",
   "A shocked family staring at exploding utility bills: a lightbulb, a gas stove flame and a bus ticket shooting upward "
   "like rockets with sparks."),
}

def prompt_for(cid):
    title, kind, color, scene = CARDS[cid]
    layout = LAYOUT_PLACE if kind == "Colocar" else LAYOUT_OTHER
    mood = {"Crisis": "Mood: tense and dramatic but still cartoonish. ",
            "Ministro": "Mood: satirical portrait, expressive face. "}.get(kind, "")
    return f"{STYLE}{layout}{mood}Scene: {scene} Use {color} as the dominant accent color of the image."

def _key():
    return open(os.path.expanduser('~/.argensimia_atlas_key')).read().strip()

H = lambda: {"Authorization": "Bearer " + _key(), "Content-Type": "application/json",
             "User-Agent": "Mozilla/5.0 (Macintosh) argensimia-card-art/1.0", "Accept": "application/json"}

STATE = {"running": False, "done": [], "failed": {}, "queue": []}

def next_version(cid):
    v = 1
    while os.path.exists(f"{OUT}/{cid}_v{v}.jpg"): v += 1
    return v

def gen_one(cid, extra=""):
    body = {"model": MODEL, "prompt": prompt_for(cid) + (" " + extra if extra else ""), "aspect_ratio": "3:2",
            "enable_sync_mode": True}
    req = urllib.request.Request(API, data=json.dumps(body).encode(), headers=H())
    with urllib.request.urlopen(req, timeout=180) as r: res = json.loads(r.read())
    d = res.get("data") or {}
    if d.get("status") != "completed" or not d.get("outputs"): raise RuntimeError(str(res)[:300])
    url = d["outputs"][0]
    v = next_version(cid)
    path = f"{OUT}/{cid}_v{v}.jpg"
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=120) as r: open(path, "wb").write(r.read())
    json.dump({"id": cid, "v": v, "prompt": body["prompt"], "url": url}, open(path[:-4] + ".json", "w"), ensure_ascii=False, indent=1)
    return path

def _worker():
    while True:
        with LOCK:
            if not STATE["queue"]: return
            cid, extra = STATE["queue"].pop(0)
        for attempt in range(2):
            try:
                gen_one(cid, extra); STATE["done"].append(cid); STATE["failed"].pop(cid, None); break
            except Exception as e:
                STATE["failed"][cid] = repr(e)[:300]; time.sleep(3)

LOCK = threading.Lock()

def start(ids=None, extra="", threads=6):
    os.makedirs(OUT, exist_ok=True)
    ids = ids or list(CARDS)
    STATE.update(running=True, done=[], failed={}, queue=[(i, extra) for i in ids], t0=time.time())
    ws = [threading.Thread(target=_worker, daemon=True) for _ in range(threads)]
    for w in ws: w.start()
    def finish():
        for w in ws: w.join()
        STATE["running"] = False
        gallery()
    threading.Thread(target=finish, daemon=True).start()
    return len(ids)

def progress():
    return {"running": STATE["running"], "done": len(STATE["done"]), "queue": len(STATE["queue"]),
            "failed": STATE["failed"], "secs": round(time.time() - STATE.get("t0", time.time()))}

def gallery():
    # Galería local: cada versión en una maqueta de la carta (marco del arte 320x218, chips arriba y badges abajo en las colocables).
    items = []
    for cid, (title, kind, color, scene) in CARDS.items():
        vs = sorted([f for f in os.listdir(OUT) if f.startswith(cid + "_v") and f.endswith(".jpg")],
                    key=lambda f: int(f.split("_v")[-1][:-4]))
        items.append((cid, title, kind, color, scene, vs))
    cards = []
    for cid, title, kind, color, scene, vs in items:
        vers = "".join(
            f'<figure><div class="art" style="--c:{color}"><img src="{v}" loading="lazy">'
            f'<span class="chip l">{kind.upper()}</span>'
            + ('<div class="badges"><b>VIDA</b><b>DAÑO</b><b>RITMO</b><b>ALC.</b></div>' if kind == "Colocar" else "")
            + f'</div><figcaption>{v[:-4]}</figcaption></figure>' for v in reversed(vs)) or '<p class="none">sin generar</p>'
        cards.append(f'<section><h2 style="--c:{color}">{html.escape(title)} <small>{cid} · {kind}</small></h2>'
                     f'<p class="scene">{html.escape(scene)}</p><div class="row">{vers}</div></section>')
    page = """<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Argensimia · arte de cartas</title><style>
body{font:15px/1.4 -apple-system,system-ui,sans-serif;background:#1b1f2a;color:#eee;margin:0;padding:24px}
h1{margin:0 0 4px}.sub{color:#aab;margin:0 0 24px}
section{background:#252a37;border-radius:14px;padding:16px;margin:0 0 18px}
h2{margin:0 0 4px;border-left:8px solid var(--c);padding-left:10px}h2 small{font-weight:400;color:#99a;font-size:13px}
.scene{color:#bbc;margin:0 0 12px;font-size:13px}.row{display:flex;gap:14px;flex-wrap:wrap}
figure{margin:0}.art{position:relative;width:320px;height:218px;border-radius:13px;overflow:hidden;outline:4px solid var(--c);background:#000}
.art img{width:100%;height:100%;object-fit:cover}
.chip{position:absolute;top:8px;background:rgba(0,0,0,.55);font:700 12px system-ui;padding:6px 10px;border-radius:8px}.chip.l{left:8px}
.badges{position:absolute;left:0;right:0;bottom:8px;display:flex;justify-content:center;gap:6px}
.badges b{width:68px;height:46px;background:rgba(10,13,26,.72);border-radius:9px;font-size:10px;color:#f2c14e;display:flex;align-items:flex-end;justify-content:center;padding-bottom:4px;box-sizing:border-box}
figcaption{color:#99a;font-size:12px;margin-top:6px}.none{color:#c77}
</style><h1>Argensimia · arte de cartas</h1><p class="sub">Cada imagen se ve recortada como en la carta (marco 320×218, chips arriba y, en las colocables, los badges abajo). La más nueva primero.</p>""" + "".join(cards)
    open(OUT + "/index.html", "w", encoding="utf-8").write(page)
    return OUT + "/index.html"


# ------------------------------------------------------------------ edición (completar recortes, retoques)
EDIT_MODEL = 'google/nano-banana-2-lite/edit'
COMPLETE_PROMPT = ("Outpaint this painting into a complete 3:2 landscape image: the picture was cut off, so extend the scene "
                   "(sky, buildings, ground) to fill the whole canvas seamlessly, matching the same painterly style, lighting and colors. "
                   "Keep all existing characters and content exactly as they are. No bars, no borders, no text, no icons.")

def edit_image(cid, image_path, prompt):
    import base64
    data = "data:image/jpeg;base64," + base64.b64encode(open(image_path, 'rb').read()).decode()
    body = {"model": EDIT_MODEL, "prompt": prompt, "images": [data], "aspect_ratio": "3:2", "enable_sync_mode": True}
    req = urllib.request.Request(API, data=json.dumps(body).encode(), headers=H())
    with urllib.request.urlopen(req, timeout=180) as r: res = json.loads(r.read())
    d = res.get("data") or {}
    if d.get("status") != "completed" or not d.get("outputs"): raise RuntimeError(str(res)[:300])
    v = next_version(cid)
    path = f"{OUT}/{cid}_v{v}.jpg"
    req = urllib.request.Request(d["outputs"][0], headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=120) as r: open(path, "wb").write(r.read())
    json.dump({"id": cid, "v": v, "edit_of": os.path.basename(image_path), "prompt": prompt}, open(path[:-4] + ".json", "w"), ensure_ascii=False, indent=1)
    return path

def crop_rows(src, top, bottom, dst):
    # recorta filas (contadas desde arriba) con bpy y guarda JPEG
    import bpy, numpy as np
    img = bpy.data.images.load(src); w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4); bpy.data.images.remove(img)
    b = bottom or h
    sub = px[h - b:h - top].copy()
    im = bpy.data.images.new("crop", w, sub.shape[0], alpha=False); im.pixels = sub.ravel()
    im.filepath_raw = dst; im.file_format = 'JPEG'; im.save(); bpy.data.images.remove(im)
    return dst
