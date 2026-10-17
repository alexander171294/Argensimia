# Argensimia — efectos de sonido sintetizados por código (numpy, sin samples externos).
# Escribe WAV mono 16-bit 44,1 kHz en Assets/Resources/SFX. Ejecutar desde el MCP de Blender:
#   exec(open(".../BlenderSrc/sfx_gen.py").read()); build_all()
# Tono: caricaturesco y cálido (como el arte), con guiños argentinos: silbato, bombo murguero, cacerolazo.
import numpy as np, wave, os

SR = 44100
TAU = 2 * np.pi
PROJECT = '/Users/alexander/git/argensimia/Argensimia'
OUT = PROJECT + '/Assets/Resources/SFX'
R = np.random.default_rng(1810)

# ------------------------------------------------------------------ primitivas
def N(d): return max(1, int(round(d * SR)))
def T(d): return np.arange(N(d)) / SR
def noise(d): return R.standard_normal(N(d))

def _mask(f, lo, hi, order):
    m = np.ones_like(f)
    if lo: m = m / np.sqrt(1 + (lo / np.maximum(f, 1e-3)) ** (2 * order))
    if hi: m = m / np.sqrt(1 + (f / hi) ** (2 * order))
    return m

def filt(x, lo=None, hi=None, order=2, circular=False):
    n = len(x)
    y = x if circular else np.concatenate([x, np.zeros(N(0.25))])
    f = np.fft.rfftfreq(len(y), 1 / SR)
    Y = np.fft.irfft(np.fft.rfft(y) * _mask(f, lo, hi, order), len(y))
    return Y if circular else Y[:n]

def norm(x, p=1.0):
    m = np.max(np.abs(x))
    return x * (p / m) if m > 0 else x

def bn(d, lo=None, hi=None, order=2):  # ruido filtrado normalizado (pico ~1)
    return norm(filt(noise(d), lo, hi, order))

def sweep_bp(x, f0, f1, q=1.2):  # pasabanda que barre de f0 a f1 (SVF de Chamberlin)
    n = len(x)
    fc = f0 * (f1 / f0) ** (np.arange(n) / max(1, n - 1))
    F = 2 * np.sin(np.pi * np.minimum(fc, SR / 6.5) / SR)
    qq = 1.0 / q
    lp = bp = 0.0
    out = np.empty(n)
    for i in range(n):
        hp = x[i] - lp - qq * bp
        bp += F[i] * hp
        lp += F[i] * bp
        out[i] = bp
    return norm(out)

def phase(f, d):
    if np.ndim(f) == 0: f = np.full(N(d), float(f))
    return TAU * np.cumsum(f) / SR

def ramp(a, b, d):
    return a * (b / a) ** np.linspace(0, 1, N(d))

def dec(d, tau, att=0.002):
    e = np.exp(-T(d) / tau)
    a = N(att)
    if a > 1: e[:a] *= np.linspace(0, 1, a)
    return e

def additive(f, d, amps):
    ph = phase(f, d)
    fmax = float(np.max(f))
    out = np.zeros(len(ph))
    for k, a in enumerate(amps, 1):
        if a == 0: continue
        if k * fmax > 16000: break
        out += a * np.sin(k * ph)
    return out

def square(f, d, maxk=31): return additive(f, d, [(1.0 / k if k % 2 else 0) for k in range(1, maxk + 1)])

def bell(freqs, amps, decs, d, att=0.0015):
    t = T(d)
    x = np.zeros(len(t))
    for f, a, dd in zip(freqs, amps, decs):
        x += a * np.sin(TAU * f * t + R.uniform(0, TAU)) * np.exp(-t / dd)
    a = N(att)
    x[:a] *= np.linspace(0, 1, a)
    return x

def brass(f, d, att=0.03, rel=0.1, bright=0.7, vib=0.0):
    t = T(d)
    ff = np.full(len(t), float(f))
    if vib: ff = ff * (1 + vib * np.sin(TAU * 5.5 * t) * np.clip((t - 0.1) / 0.2, 0, 1))
    ph = phase(ff, d)
    env = np.minimum(1, t / att) * np.clip((d - t) / rel, 0, 1)
    b = bright * (0.45 + 0.55 * np.minimum(1, t / (att * 2.5)))
    x = np.zeros(len(t))
    for k in range(1, 40):
        if k * ff.max() > 14000: break
        x += (b ** (k - 1)) / k ** 0.6 * np.sin(k * ph)
    return x * env * 0.5

def smoothnoise(n, rate):  # ruido suave y periódico (sirve para loops)
    k = max(4, int(n / SR * rate))
    v = np.append(R.random(k), 0)
    v[-1] = v[0]
    x = np.linspace(0, k, n, endpoint=False)
    i = np.floor(x).astype(int)
    fr = (1 - np.cos(np.pi * (x - i))) / 2
    return v[i] * (1 - fr) + v[i + 1] * fr

def plus(*xs):  # suma señales de distinto largo
    L = max(len(x) for x in xs)
    y = np.zeros(L)
    for x in xs: y[:len(x)] += x
    return y

class Mix:
    def __init__(s): s.parts = []
    def add(s, x, t0=0.0, g=1.0):
        s.parts.append((N(t0) if t0 > 0 else 0, np.asarray(x) * g)); return s
    def out(s):
        L = max(o + len(x) for o, x in s.parts)
        y = np.zeros(L)
        for o, x in s.parts: y[o:o + len(x)] += x
        return y

def reverb(x, wet=0.18, tail=1.1, tau=0.3):
    ir = filt(noise(tail), 250, 6000) * np.exp(-T(tail) / tau)
    ir[:N(0.012)] = 0
    ir /= np.sqrt(np.sum(ir ** 2))
    n = len(x) + len(ir)
    y = np.fft.irfft(np.fft.rfft(x, n) * np.fft.rfft(ir, n), n)
    out = np.zeros(n)
    out[:len(x)] += x
    return out + wet * y

def finish(x, peak=0.9):
    x = np.asarray(x, float)
    m = np.max(np.abs(x))
    if m <= 0: return x
    idx = np.nonzero(np.abs(x) > m * 2e-4)[0]
    x = x[:idx[-1] + 1].copy()
    a = min(N(0.0006), len(x)); x[:a] *= np.linspace(0, 1, a)
    r = min(N(0.01), len(x)); x[-r:] *= np.linspace(1, 0, r)
    return norm(x, peak)

def write(name, x):
    os.makedirs(OUT, exist_ok=True)
    y = (np.clip(x, -1, 1) * 32767).astype('<i2')
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(y.tobytes())
    return len(y) / SR

# ------------------------------------------------------------------ piezas reutilizables
def whistle(d):  # silbato policial de bolita: tono agudo con trino ~31 Hz
    t = T(d)
    f = 2950 * (1 + 0.012 * np.sin(TAU * 31 * t))
    ph = phase(f, d)
    am = 1 - 0.55 * (0.5 + 0.5 * np.sin(TAU * 31 * t + 0.5))
    tone = (np.sin(ph) + 0.12 * np.sin(2 * ph)) * am
    br = bn(d, 1800, 6000) * 0.18 * am
    env = np.minimum(1, t / 0.012) * np.clip((d - t) / 0.03, 0, 1)
    return (tone + br) * env

def bombo_hit(acc=1.0):  # bombo murguero
    d = 0.6; t = T(d)
    f = 48 + 30 * np.exp(-t / 0.03)
    body = np.sin(phase(f, d)) * np.exp(-t / 0.28)
    over = (0.3 * np.sin(TAU * 112 * t) + 0.15 * np.sin(TAU * 171 * t)) * np.exp(-t / 0.09)
    beat = bn(d, None, 1800) * np.exp(-t / 0.012) * 0.45
    x = body + over + beat
    x[:N(0.001)] *= np.linspace(0, 1, N(0.001))
    return x * acc

def platillo():
    return bell([3150, 4420, 5930, 7310], [0.5, 0.4, 0.35, 0.25], [0.25, 0.2, 0.15, 0.1], 0.4) + bn(0.4, 4000) * dec(0.4, 0.12, 0.001) * 0.6

def pot_hit(base):  # cacerola + cuchara
    return plus(bell([base, base * 2.32, base * 3.91, base * 5.13], [1, 0.6, 0.4, 0.25], [0.16, 0.1, 0.07, 0.04], 0.3), bn(0.01, 3000) * dec(0.01, 0.002, 0.0003) * 0.6)

def crowd(d, voices=36, shout=False, loop=False):
    # multitud: muchas "voces" (diente de sierra por formantes de vocales) que se prenden y apagan como sílabas
    n = N(d); t = np.arange(n) / SR
    out = np.zeros(n)
    V = [(730, 1090, 2440), (530, 1840, 2480), (570, 840, 2410), (440, 1020, 2240), (660, 1720, 2410), (390, 2300, 3000)]
    for v in range(voices):
        f0 = R.uniform(170, 330) if shout else R.uniform(100, 240)
        if loop: f0 = round(f0 * d) / d
        dr = smoothnoise(n, R.uniform(0.8, 2.5)); dr -= dr.mean()
        vib = round(R.uniform(3, 6) * d) / d
        f = f0 * (1 + 0.12 * dr + 0.03 * np.sin(TAU * vib * t + R.uniform(0, 6)))
        ph = np.cumsum(f) / SR
        src = 2 * (ph % 1) - 1
        F = V[R.integers(len(V))]
        s = (filt(src, F[0] * 0.8, F[0] * 1.25, 2, True) + 0.7 * filt(src, F[1] * 0.85, F[1] * 1.15, 2, True)
             + 0.25 * filt(src, F[2] * 0.9, F[2] * 1.1, 2, True))
        g = smoothnoise(n, R.uniform(1, 2.5) if shout else R.uniform(2, 4.5))
        thr = 0.25 if shout else 0.45
        g = np.clip((g - thr) / (1 - thr), 0, 1) ** (1.2 if shout else 1.6)
        out += s * g * R.uniform(0.4, 1.0)
    out = norm(out)
    air = norm(filt(R.standard_normal(n), 300, 3200, 2, True))
    out = out + air * 0.35 * (0.5 + 0.5 * smoothnoise(n, 0.7))
    return norm(out)

def wrap(buf, n):  # loop: la cola que se pasa del final vuelve al principio
    y = buf[:n].copy()
    tail = buf[n:]
    y[:len(tail)] += tail
    return y

# ------------------------------------------------------------------ UI
def s_ui_tap():
    d = 0.08
    return np.sin(phase(ramp(1500, 650, d), d)) * dec(d, 0.016, 0.001) + bn(d, 2500, 9000) * dec(d, 0.003, 0.0005) * 0.3

def s_ui_error():
    m = Mix()
    for f0, t0 in [(185, 0), (150, 0.12)]:
        d = 0.1; t = T(d)
        m.add(filt(square(f0, d), None, 1600) * np.minimum(1, t / 0.004) * np.clip((d - t) / 0.03, 0, 1), t0)
    return m.out()

def s_card_draw():
    d = 0.22; t = T(d)
    x = sweep_bp(noise(d), 700, 5200, 1.4) * np.sin(np.pi * t / d) ** 1.5
    return Mix().add(x).add(bn(0.02, 1500, 7000) * dec(0.02, 0.004) * 0.5, 0.17).out()

def s_card_flip():
    d = 0.1
    return bn(d, 1800, 7000) * dec(d, 0.022, 0.002) + np.sin(phase(ramp(400, 180, d), d)) * dec(d, 0.02) * 0.4

def s_card_zoom():
    d = 0.32; t = T(d)
    return sweep_bp(noise(d), 350, 2800, 1.0) * np.sin(np.pi * t / d) ** 2

def s_card_play():
    m = Mix()
    d = 0.26; t = T(d)
    m.add(sweep_bp(noise(d), 500, 4000, 1.1) * np.sin(np.pi * t / d) ** 2 * 0.6)
    m.add(bell([1046.5, 1568, 2093, 3136], [1, 0.7, 0.5, 0.25], [0.55, 0.45, 0.3, 0.18], 1.0) * 0.6, 0.14)
    for i in range(7): m.add(bell([R.uniform(3000, 6500)], [0.25], [0.06], 0.15), 0.16 + i * 0.045)
    return reverb(m.out(), 0.22)

def s_card_shuffle():
    m = Mix(); tt = 0.0
    for i in range(16):
        d = 0.05
        m.add(bn(d, R.uniform(1500, 2500), 8000) * dec(d, R.uniform(0.008, 0.014), 0.001) * R.uniform(0.5, 1), tt)
        tt += 0.045 * (1 - i / 22)
    m.add(s_card_flip() * 0.8, tt + 0.02)
    return m.out()

def s_coin():  # caja registradora: "cha-ching"
    m = Mix()
    m.add(bn(0.08, 2500, 9000) * dec(0.08, 0.02, 0.001) * 0.6)
    m.add(bell([2637, 3520 * 1.004, 5274, 7040], [1, 0.8, 0.45, 0.25], [0.4, 0.32, 0.2, 0.12], 0.9), 0.07)
    m.add(bell([3136, 4186 * 1.003, 6272], [0.7, 0.5, 0.3], [0.35, 0.25, 0.15], 0.8), 0.15)
    return reverb(m.out(), 0.12)

def s_spend():  # moneda que cae y rebota
    m = Mix()
    m.add(bell([2200, 3310, 5150], [1, 0.6, 0.35], [0.1, 0.07, 0.05], 0.25))
    m.add(bell([2350, 3500], [0.45, 0.3], [0.06, 0.04], 0.15), 0.075)
    m.add(bell([2420], [0.2], [0.04], 0.1), 0.12)
    return m.out()

def marimba(f, d=0.5):
    t = T(d)
    return (np.sin(TAU * f * t) + 0.3 * np.sin(TAU * 4 * f * t) * np.exp(-t / 0.03)) * dec(d, 0.18, 0.002)

def s_pop_up():
    m = Mix()
    for i, f in enumerate([523.25, 659.25, 783.99, 1046.5]): m.add(marimba(f, 0.6 if i == 3 else 0.35), i * 0.075)
    return reverb(m.out(), 0.15)

def s_pop_down():  # "wah wah" de trompeta
    m = Mix(); tt = 0.0
    for i, (f, d) in enumerate([(392, 0.17), (370, 0.17), (349.2, 0.17), (330, 0.55)]):
        m.add(brass(f, d, att=0.02, rel=0.06 if i < 3 else 0.25, bright=0.55, vib=0.025 if i == 3 else 0), tt)
        tt += d + 0.02
    return reverb(m.out(), 0.12)

def s_mood_down():  # sube la bronca: tambor grave + cluster de bronces
    m = Mix()
    m.add(np.sin(phase(ramp(110, 55, 0.5), 0.5)) * dec(0.5, 0.22, 0.001))
    m.add(bn(0.5, None, 600) * dec(0.5, 0.05, 0.001) * 0.4)
    cl = (brass(146.8, 1.1, att=0.25, rel=0.4, bright=0.5) + 0.8 * brass(155.6, 1.1, att=0.25, rel=0.4, bright=0.5)
          + 0.7 * brass(98, 1.1, att=0.3, rel=0.4, bright=0.4))
    m.add(norm(cl) * 0.6, 0.05)
    return reverb(m.out(), 0.25)

def s_stamp():
    m = Mix()
    m.add(np.sin(phase(ramp(95, 40, 0.35), 0.35)) * dec(0.35, 0.1, 0.001))
    m.add(bn(0.1, 400, 3500) * dec(0.1, 0.025, 0.001) * 0.8)
    return reverb(m.out(), 0.15)

def s_clock_tick():  # reloj de la previa: "toc" seco de madera
    d = 0.12; t = T(d)
    return (np.sin(TAU * 1250 * t) * np.exp(-t / 0.012) + 0.5 * np.sin(TAU * 2600 * t) * np.exp(-t / 0.006)
            + bn(d, 2000, 7000) * dec(d, 0.002, 0.0002) * 0.3)

# ------------------------------------------------------------------ partida / cartas
def s_place():  # "toc" grave de pieza de ajedrez apoyada sobre el tablero
    d = 0.3; t = T(d)
    wood = (np.sin(TAU * 185 * t) * np.exp(-t / 0.055) + 0.55 * np.sin(TAU * 430 * t) * np.exp(-t / 0.03)
            + 0.25 * np.sin(TAU * 780 * t) * np.exp(-t / 0.018))
    thump = np.sin(phase(ramp(110, 70, d), d)) * dec(d, 0.035, 0.0008) * 0.8
    click = bn(d, 900, 3200) * dec(d, 0.003, 0.0003) * 0.35
    x = wood + thump + click
    x[:N(0.0008)] *= np.linspace(0, 1, N(0.0008))
    return reverb(x, 0.08, 0.5, 0.12)

def s_upgrade():
    m = Mix()
    for i, f in enumerate([523.25, 659.25, 783.99, 1046.5, 1318.5, 1568]):
        m.add(bell([f, 2 * f, 3.01 * f], [1, 0.4, 0.15], [0.35, 0.2, 0.1], 0.6), i * 0.055)
    m.add(bell([1046.5, 1318.5, 1568, 2093], [0.6, 0.5, 0.5, 0.3], [0.9, 0.8, 0.7, 0.5], 1.4), 0.33)
    d = 0.9; t = T(d)
    m.add(bn(d, 5000, 12000) * np.sin(np.pi * t / d) * 0.25, 0.1)
    return reverb(m.out(), 0.25)

def s_heal():
    d = 1.1; t = T(d)
    x = sum(np.sin(TAU * f * t + R.uniform(0, 6)) for f in [784, 987.8, 1174.7, 1568])
    env = np.minimum(1, t / 0.18) * np.exp(-np.maximum(0, t - 0.18) / 0.35)
    m = Mix().add(x * env * (0.8 + 0.2 * np.sin(TAU * 9 * t)) * 0.4)
    for i in range(8): m.add(bell([R.uniform(2500, 5000)], [0.2], [0.05], 0.12), 0.05 + i * 0.09)
    return reverb(m.out(), 0.3)

def s_crisis():  # cortina de "último momento": da-da-DAAA
    m = Mix()
    chord = [146.8, 293.7, 349.2, 440, 587.3]
    for t0, d in [(0, 0.1), (0.15, 0.1), (0.3, 0.75)]:
        m.add(sum(brass(f, d, att=0.008, rel=0.05 if d < 0.2 else 0.35, bright=0.75) for f in chord) / 3, t0)
    m.add(np.sin(phase(ramp(90, 60, 0.8), 0.8)) * dec(0.8, 0.3) * 0.9, 0.3)
    m.add(bn(0.8, 4000) * dec(0.8, 0.25) * 0.12, 0.3)
    return reverb(m.out(), 0.22)

def s_evade():
    d = 0.38; t = T(d)
    w = sweep_bp(noise(d), 3500, 450, 1.0) * np.sin(np.pi * t / d) ** 2
    return reverb(Mix().add(w).add(bell([1568, 3136], [0.8, 0.3], [0.4, 0.2], 0.8), 0.26).out(), 0.18)

def s_whistle(): return reverb(Mix().add(whistle(0.2)).add(whistle(0.6), 0.27).out(), 0.15)
def s_whistle_short(): return reverb(whistle(0.17), 0.12)

def s_wave_start():  # pitazo + la multitud se enciende + bombo
    m = Mix()
    m.add(whistle(0.65) * 0.8)
    d = 2.2; t = T(d)
    m.add(crowd(d, voices=26, shout=True) * np.clip((t - 0.15) / 0.6, 0, 1) * np.clip((d - t) / 0.9, 0, 1) * 0.8, 0.2)
    m.add(bombo_hit(1.0), 0.75); m.add(bombo_hit(0.8), 1.0); m.add(bombo_hit(1.0), 1.25)
    return reverb(m.out(), 0.15)

def s_fanfare():  # fin de mandato: tranquilo pero claro (marimba ascendente + acorde suave de cornos + campanita)
    m = Mix()
    for i, f in enumerate([392.0, 523.25, 659.25]): m.add(marimba(f, 0.5), i * 0.11)
    pad = sum(brass(f, 1.5, att=0.12, rel=0.7, bright=0.42) for f in [261.63, 329.63, 392.0, 523.25])
    m.add(norm(pad) * 0.42, 0.3)
    m.add(bell([1046.5, 1568, 2093], [0.5, 0.3, 0.15], [0.7, 0.5, 0.3], 1.2) * 0.45, 0.33)
    return reverb(m.out(), 0.16, 1.0, 0.3)

def strings(f, d, att=0.03, rel=0.8, detune=0.006, voices=5):
    t = T(d); x = np.zeros(len(t))
    for v in range(voices):
        ff = f * (1 + detune * (v - (voices - 1) / 2) / ((voices - 1) / 2)) * (1 + 0.004 * np.sin(TAU * R.uniform(4.5, 6) * t))
        ph = phase(ff, d) + R.uniform(0, TAU)
        for k in range(1, 30):
            if k * f > 9000: break
            x += np.sin(k * ph) / k
    env = np.minimum(1, t / att) * np.exp(-np.maximum(0, t - att) / rel)
    return x * env / voices

def s_gameover():  # cierre dramático: redoble de timbal que crece y golpe orquestal en menor
    m = Mix()
    roll = 0.9; tt = 0.0; k = 0
    while tt < roll:
        a = 0.15 + 0.85 * (tt / roll) ** 1.6
        d = 0.35; t = T(d)
        hit = np.sin(phase(ramp(95, 78, d), d)) * dec(d, 0.12, 0.002) + bn(d, None, 900) * dec(d, 0.01, 0.001) * 0.4
        m.add(hit * a, tt); tt += 0.055; k += 1
    chord = [73.42, 146.83, 174.61, 220.0, 293.66]
    st = sum(strings(f, 2.4, att=0.015, rel=0.7) for f in chord)
    m.add(norm(filt(st, 60, 2200)) * 0.9, roll)
    d = 1.6; t = T(d)
    m.add(np.sin(phase(ramp(70, 45, d), d)) * dec(d, 0.45, 0.002) * 1.0, roll)
    m.add(bn(1.6, 3500) * dec(1.6, 0.5, 0.002) * 0.25, roll)
    return reverb(m.out(), 0.3, 1.6, 0.45)

def s_shotgun():  # escopeta con posta de goma
    d = 0.6; t = T(d)
    env = 0.9 * np.exp(-t / 0.01) + 0.35 * np.exp(-t / 0.09)
    env[:N(0.0008)] *= np.linspace(0, 1, N(0.0008))
    m = Mix().add(bn(d, None, 5000) * env)
    m.add(np.sin(phase(ramp(150, 50, 0.15), 0.15)) * dec(0.15, 0.04, 0.0008) * 0.9)
    m.add(bn(0.4, 200, 1800) * dec(0.4, 0.08, 0.01) * 0.15, 0.13)  # eco en los edificios
    return m.out()

def s_rubber_hit():
    d = 0.12; t = T(d)
    return bn(d, 700, 2600) * dec(d, 0.018, 0.0008) + np.sin(TAU * 240 * t) * dec(d, 0.03, 0.001) * 0.5

def s_baton():  # bastonazo contra escudo / palo
    d = 0.18; t = T(d)
    x = bn(d, 1100, 4500) * dec(d, 0.012, 0.0005)
    x += (np.sin(TAU * 520 * t) + 0.6 * np.sin(TAU * 1340 * t)) * dec(d, 0.045, 0.001) * 0.45
    x += np.sin(phase(ramp(160, 90, d), d)) * dec(d, 0.03, 0.001) * 0.5
    return x

def s_fence_clang():  # palazo contra la valla: golpe seco de metal, sin notas sostenidas
    d = 0.25; t = T(d)
    hit = bn(d, 300, 2600) * dec(d, 0.012, 0.0004)
    cl = np.zeros(len(t))
    for i in range(9):
        f = R.uniform(320, 2900)
        cl += R.uniform(0.3, 1) * np.sin(TAU * f * t + R.uniform(0, 6)) * np.exp(-t / R.uniform(0.012, 0.04))
    thud = np.sin(phase(ramp(130, 70, d), d)) * dec(d, 0.04, 0.0006)
    x = hit + norm(cl) * 0.55 + thud * 0.7
    m = Mix().add(x).add(bn(0.06, 800, 4000) * dec(0.06, 0.006, 0.0004) * 0.25, 0.035)  # rebote de la chapa
    return m.out()

def s_defense_hit():
    d = 0.2; t = T(d)
    return (np.sin(phase(ramp(150, 80, d), d)) * dec(d, 0.05, 0.0008) + bn(d, 300, 1600) * dec(d, 0.02, 0.0005) * 0.6
            + np.sin(TAU * 900 * t) * dec(d, 0.02) * 0.2)

def s_gas_launch():  # lanzagranadas: "fwomp" + tubo
    m = Mix()
    m.add(np.sin(phase(ramp(190, 55, 0.18), 0.18)) * dec(0.18, 0.08, 0.001))
    m.add(bn(0.35, None, 900) * dec(0.35, 0.06, 0.001) * 0.7)
    m.add(bell([2200, 3700], [0.2, 0.1], [0.05, 0.03], 0.1), 0.005)
    d = 0.5; t = T(d)
    m.add(sweep_bp(noise(d), 1500, 600, 1.0) * np.sin(np.pi * t / d) * 0.2, 0.05)
    return m.out()

def s_gas_hiss():  # cae la granada y larga el gas
    m = Mix()
    m.add(bell([3100, 4650], [0.4, 0.2], [0.05, 0.03], 0.1))
    m.add(bell([2900], [0.25], [0.04], 0.08), 0.08)
    d = 1.6; t = T(d)
    env = np.minimum(1, t / 0.03) * (0.55 + 0.45 * np.exp(-t / 0.25)) * np.clip((d - t) / 0.6, 0, 1)
    m.add(bn(d, 1800, 9000) * env * 0.8 * (1 + 0.15 * np.sin(TAU * 7 * t)), 0.05)
    return m.out()

def s_siren_yelp():
    d = 0.8; t = T(d)
    u = (t % 0.2) / 0.2
    f = 620 + 780 * u ** 0.7
    x = filt(square(f, d, maxk=11), None, 3500)
    return reverb(x * np.minimum(1, t / 0.02) * np.clip((d - t) / 0.08, 0, 1), 0.12)

def s_cuffs():  # esposas: cremallera + clack
    m = Mix()
    for i in range(6):
        m.add(plus(bell([3200 * R.uniform(0.95, 1.05), 4700, 6100], [0.6, 0.4, 0.25], [0.012, 0.009, 0.006], 0.04),
                   bn(0.01, 3000) * dec(0.01, 0.002, 0.0002) * 0.4), i * 0.032)
    m.add(plus(bell([1800, 2950, 4400], [0.9, 0.6, 0.35], [0.06, 0.04, 0.03], 0.2), bn(0.03, 1500) * dec(0.03, 0.004, 0.0003) * 0.8), 0.26)
    return m.out()

def s_moto_pass():  # moto que acelera y pasa
    d = 1.4; t = T(d)
    f0 = np.interp(t, [0, 0.2, 0.65, 1.0, 1.4], [40, 52, 92, 80, 70])
    f = f0 * (1 + 0.06 * np.tanh((0.7 - t) * 5))
    ph = phase(f, d)
    x = np.zeros(len(t))
    for k in range(1, 40): x += np.sin(k * ph + k * 0.7) / k ** 0.85
    x = norm(filt(x, None, 2600))
    ex = bn(d, 120, 900) * (0.5 + 0.5 * np.sin(ph * 0.5)) * 0.6
    env = np.interp(t, [0, 0.15, 0.6, 0.85, 1.4], [0.15, 0.6, 1, 0.8, 0])
    return (x + ex) * env

def s_break_crash():  # se desarma una defensa
    m = Mix()
    m.add(np.sin(phase(ramp(90, 40, 0.5), 0.5)) * dec(0.5, 0.15, 0.001))
    m.add(bn(0.6, None, 3500) * dec(0.6, 0.12, 0.001) * 0.7)
    for i in range(14):
        t0 = 0.05 + R.uniform(0, 0.6) ** 1.4
        f = R.uniform(700, 3200)
        m.add(bell([f, f * R.uniform(1.4, 2.7)], [0.5, 0.25], [R.uniform(0.02, 0.07), 0.02], 0.15) * R.uniform(0.3, 0.9) * max(0.1, 1 - t0 / 0.8), t0)
    return reverb(m.out(), 0.18)

def s_unit_down():  # cae un agente: quejido que se apaga + golpe del cuerpo + el equipo que rebota
    d = 0.75; t = T(d)
    f0 = 175 * (88 / 175) ** (t / d) * (1 + 0.02 * np.sin(TAU * 7 * t))
    src = additive(f0, d, [1.0 / k for k in range(1, 40)])
    voice = sweep_bp(src, 780, 470, 3.0) + 0.55 * sweep_bp(src, 1150, 820, 4.0) + 0.2 * sweep_bp(src, 2500, 2300, 5.0)
    env = np.minimum(1, t / 0.04) * np.clip(1 - t / d, 0, 1) ** 1.4
    m = Mix().add(norm(voice) * env * 0.8)
    dd = 0.4; tt = T(dd)
    m.add((np.sin(phase(ramp(95, 50, dd), dd)) * dec(dd, 0.09, 0.001) + bn(dd, None, 700) * dec(dd, 0.03, 0.001) * 0.6) * 0.9, 0.42)
    for i, t0 in enumerate([0.47, 0.55, 0.61, 0.66]):
        f = R.uniform(600, 1600)
        m.add(bell([f, f * 2.3], [0.5, 0.2], [0.03, 0.015], 0.08) * (0.5 - i * 0.1), t0)
    return reverb(m.out(), 0.12)

def s_heli_start():  # turbina que arranca y levanta vueltas
    d = 4.2; t = T(d)
    f = np.interp(t, [0, 0.4, 2.6, 4.2], [180, 260, 1100, 1180])
    ph = phase(f, d)
    whine = np.sin(ph) + 0.35 * np.sin(2 * ph) + 0.12 * np.sin(3.01 * ph)
    env = np.interp(t, [0, 0.3, 2.5, 3.4, 4.2], [0, 0.5, 1, 0.8, 0])
    air = bn(d, 200, 3000) * np.interp(t, [0, 1.5, 3.0, 4.2], [0.05, 0.25, 0.5, 0])
    m = Mix().add(whine * env * 0.6 + air)
    m.add(plus(bn(0.05, 1000, 5000) * dec(0.05, 0.006, 0.0003), np.sin(TAU * 140 * T(0.15)) * dec(0.15, 0.04)) * 0.6)  # clack del arranque
    return m.out()


def s_boo():  # pierde popularidad: abucheo de la gente ("¡uuuh!") con algunos gritos enojados
    d = 1.6; n = N(d); t = T(d)
    out = np.zeros(n)
    V = [(320, 800, 2240), (400, 820, 2400), (480, 900, 2300)]  # u / o cerradas
    for v in range(30):
        f0 = R.uniform(110, 230)
        glide = 1 - 0.18 * np.clip((t - 0.25) / 1.2, 0, 1)
        f = f0 * glide * (1 + 0.015 * np.sin(TAU * R.uniform(4, 7) * t + R.uniform(0, 6)))
        ph = np.cumsum(f) / SR
        src = 2 * (ph % 1) - 1
        F = V[R.integers(len(V))]
        sv = filt(src, F[0] * 0.8, F[0] * 1.25, 2) + 0.5 * filt(src, F[1] * 0.85, F[1] * 1.15, 2) + 0.15 * filt(src, F[2] * 0.9, F[2] * 1.1, 2)
        st = R.uniform(0, 0.18)
        env = np.clip((t - st) / 0.12, 0, 1) * np.clip((d - t) / 0.55, 0, 1) * (0.85 + 0.15 * smoothnoise(n, 3))
        out += sv * env * R.uniform(0.5, 1)
    out = norm(out)
    for k in range(5):  # gritos sueltos: "¡eh!", "¡ah!"
        dd = R.uniform(0.22, 0.35); tt = T(dd)
        f0 = R.uniform(230, 330) * (1 - 0.15 * tt / dd)
        ph = np.cumsum(f0) / SR
        src = 2 * (ph % 1) - 1
        sh = filt(src, 600, 900, 2) + 0.7 * filt(src, 1500, 1900, 2)
        sh = norm(sh) * np.minimum(1, tt / 0.02) * np.clip((dd - tt) / 0.1, 0, 1)
        o = N(R.uniform(0.05, 0.9)); m = min(len(sh), n - o); out[o:o + m] += sh[:m] * 0.35
    air = norm(filt(R.standard_normal(n), 250, 2500)) * 0.2 * np.clip((d - t) / 0.6, 0, 1) * np.minimum(1, t / 0.15)
    return reverb(out + air, 0.15)

# ------------------------------------------------------------------ manifestantes
def s_poof():  # sale de escena: "puf" caricaturesco
    d = 0.28
    m = Mix().add(sweep_bp(noise(d), 2200, 350, 0.9) * dec(d, 0.08, 0.004))
    m.add(np.sin(phase(ramp(320, 140, 0.06), 0.06)) * dec(0.06, 0.02, 0.001) * 0.5)
    return m.out()

def s_boing():  # encapuchado salta el retén
    d = 0.55; t = T(d)
    f = (260 + 140 * (t / d)) + 95 * np.sin(TAU * 15 * t) * np.exp(-t / 0.28)
    ph = phase(f, d)
    x = (np.sin(ph) + 0.25 * np.sin(2 * ph)) * dec(d, 0.2, 0.003)
    return x + sweep_bp(noise(d), 600, 2500, 1) * np.sin(np.pi * t / d) ** 2 * 0.25

def s_gate_bang():  # llegan al vallado de la Casa Rosada
    m = Mix()
    m.add(np.sin(phase(ramp(80, 40, 0.4), 0.4)) * dec(0.4, 0.12, 0.001))
    m.add(bell([180, 437, 812, 1290, 1910, 2730], [0.8, 0.7, 0.6, 0.45, 0.3, 0.2], [0.6, 0.45, 0.35, 0.25, 0.18, 0.12], 1.0))
    for i, t0 in enumerate([0.09, 0.16, 0.21]):
        m.add(bell([437 * 1.05, 1290 * 1.03, 2730], [0.4, 0.3, 0.15], [0.08, 0.06, 0.04], 0.2) * (0.7 - i * 0.2), t0)
    m.add(bn(0.2, None, 2500) * dec(0.2, 0.03, 0.001) * 0.6)
    d = 1.4; t = T(d)
    m.add(crowd(d, voices=20, shout=True) * np.minimum(1, t / 0.08) * np.exp(-t / 0.5) * 0.5, 0.05)
    return reverb(m.out(), 0.15)

def s_mortar_launch():
    m = Mix()
    m.add(np.sin(phase(ramp(130, 40, 0.25), 0.25)) * dec(0.25, 0.08, 0.001))
    m.add(bn(0.3, None, 700) * dec(0.3, 0.06, 0.001) * 0.8)
    m.add(bell([880, 1990], [0.25, 0.1], [0.2, 0.1], 0.4), 0.002)
    d = 0.6; t = T(d)
    m.add(sweep_bp(noise(d), 900, 2400, 1.2) * np.sin(np.pi * t / d) * 0.15, 0.08)
    return m.out()

def s_explosion():
    d = 1.6; t = T(d)
    env = 0.8 * np.exp(-t / 0.04) + 0.5 * np.exp(-t / 0.45)
    env[:N(0.003)] *= np.linspace(0, 1, N(0.003))
    m = Mix().add(bn(d, None, 1400) * env)
    m.add(np.sin(phase(ramp(70, 28, 0.8), 0.8)) * dec(0.8, 0.3, 0.002) * 0.9)
    m.add(bn(d, 2000) * (R.random(N(d)) < 0.0025) * np.exp(-t / 0.4) * 3)
    return reverb(m.out(), 0.2)

def s_fire_ignite():  # goma que se prende
    m = Mix()
    d = 0.6; t = T(d)
    m.add(sweep_bp(noise(d), 250, 2600, 0.8) * np.sin(np.pi * t / d) ** 1.2)
    m.add(bn(d, None, 400) * np.sin(np.pi * t / d) * 0.5)
    D = 1.3; cr = np.zeros(N(D))
    for i in range(40):
        p = int(R.uniform(0.1, D - 0.02) * SR); L = N(0.006)
        cr[p:p + L] += R.standard_normal(L) * np.exp(-np.arange(L) / 40) * R.uniform(0.3, 1) * np.exp(-p / SR / 0.6)
    m.add(norm(filt(cr, 1500)) * 0.6)
    return m.out()

# ------------------------------------------------------------------ loops (periódicos, sin corte)
def s_loop_crowd(): return norm(crowd(8.0, voices=40, loop=True), 0.9)

def s_loop_bombo():  # murga/hinchada a 120 bpm, 2 compases
    L = 4.0; beat = 0.5; n = N(L)
    buf = np.zeros(n + N(1))
    for b, a in [(0, 1), (1, 0.7), (1.5, 0.75), (2, 1), (3, 0.7), (3.5, 0.75), (4, 1), (5, 0.7), (5.5, 0.75), (6, 1), (6.5, 0.6), (7, 0.85), (7.5, 0.85)]:
        x = bombo_hit(a * R.uniform(0.92, 1)); o = N(b * beat); buf[o:o + len(x)] += x
    for b in [1, 3, 5, 7]:
        x = platillo() * 0.3; o = N(b * beat); buf[o:o + len(x)] += x
    return norm(wrap(buf, n), 0.9)

def s_loop_cacerola():  # cacerolazo: varios vecinos golpeando ollas
    L = 4.0; n = N(L)
    buf = np.zeros(n + N(0.5))
    for p in range(7):
        base = R.uniform(650, 1300); per = 1 / R.uniform(3.2, 5.5)
        t0 = R.uniform(0, per)
        while t0 < L:
            x = pot_hit(base) * R.uniform(0.5, 1); o = N(t0); buf[o:o + len(x)] += x
            t0 += per * R.uniform(0.85, 1.15)
    return norm(wrap(buf, n), 0.9)

def s_loop_heli():  # rotor (16 golpes/s) + turbina
    L = 2.0; n = N(L); t = np.arange(n) / SR
    buf = np.zeros(n + N(0.2))
    for i in range(32):
        d = 0.08; tt = T(d)
        a = 0.85 + 0.15 * np.cos(TAU * i / 16)
        x = (bn(d, 70, 900) * dec(d, 0.018, 0.002) + np.sin(TAU * 68 * tt) * dec(d, 0.025, 0.002) * 0.8) * a
        o = N(i * L / 32); buf[o:o + len(x)] += x
    y = norm(wrap(buf, n))
    wash = norm(filt(R.standard_normal(n), 60, 700, 2, True))
    whine = (np.sin(TAU * 1180 * t) + 0.4 * np.sin(TAU * 2360 * t)) * (0.8 + 0.2 * np.sin(TAU * t / L))
    return norm(y + wash * 0.3 + whine * 0.05, 0.9)

def s_loop_water():  # chorro del hidrante
    L = 2.0; n = N(L)
    hiss = norm(filt(R.standard_normal(n), 500, 7000, 2, True)) * (0.75 + 0.25 * smoothnoise(n, 6))
    low = norm(filt(R.standard_normal(n), 80, 350, 2, True))
    sp = np.zeros(n)
    for i in range(70):
        p = R.integers(0, n - N(0.01)); Lc = N(0.008)
        sp[p:p + Lc] += R.standard_normal(Lc) * np.exp(-np.arange(Lc) / 60) * R.uniform(0.3, 1)
    sp = norm(filt(sp, 1500, 6000, 2, True))
    return norm(hiss + low * 0.35 + sp * 0.3, 0.9)

LOOPS = {'loop_crowd', 'loop_bombo', 'loop_cacerola', 'loop_heli', 'loop_water'}

def build_all(only=None):
    res = {}
    for k, fn in sorted(globals().items()):
        if not k.startswith('s_') or not callable(fn): continue
        name = k[2:]
        if only and name not in only: continue
        x = fn()
        x = norm(np.asarray(x, float), 0.9) if name in LOOPS else finish(x)
        res[name] = round(write(name, x), 3)
    return res
