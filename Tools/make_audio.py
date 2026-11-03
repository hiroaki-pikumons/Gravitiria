"""Procedural music & SFX generator for Gravity (no external samples)."""
import math, os, sys, wave
import numpy as np

SR = 44100
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(7)


def note(n):  # MIDI -> Hz
    return 440.0 * 2 ** ((n - 69) / 12.0)


def env_adsr(n, a, d, s, r, sr=SR):
    a, d, r = int(a * sr), int(d * sr), int(r * sr)
    sus = max(n - a - d - r, 0)
    e = np.concatenate([
        np.linspace(0, 1, a, endpoint=False),
        np.linspace(1, s, d, endpoint=False),
        np.full(sus, s),
        np.linspace(s, 0, r, endpoint=True),
    ])
    return e[:n] if len(e) >= n else np.pad(e, (0, n - len(e)))


def saw_additive(freq, n, harmonics=8, detune=0.0):
    t = np.arange(n) / SR
    out = np.zeros(n)
    for f in (freq * (1 - detune), freq * (1 + detune)) if detune else (freq,):
        for k in range(1, harmonics + 1):
            if f * k > SR / 2.2: break
            out += np.sin(2 * np.pi * f * k * t) / k
    return out / (1.2 * (2 if detune else 1))


def square_additive(freq, n, harmonics=7):
    t = np.arange(n) / SR
    out = np.zeros(n)
    for k in range(1, harmonics * 2, 2):
        if freq * k > SR / 2.2: break
        out += np.sin(2 * np.pi * freq * k * t) / k
    return out / 1.1


def sine(freq, n, phase=0.0):
    t = np.arange(n) / SR
    return np.sin(2 * np.pi * freq * t + phase)


def lowpass(x, cutoff):
    """One-pole lowpass (vectorized via lfilter-free recursion using cumulative trick is hard; use frame loop)."""
    alpha = 1 - math.exp(-2 * math.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    # chunked python loop: acceptable for SFX-length signals
    for i in range(len(x)):
        acc += alpha * (x[i] - acc)
        y[i] = acc
    return y


def place(track, start_sec, sig, gain=1.0, pan=0.0):
    s = int(start_sec * SR)
    e = min(s + len(sig), track.shape[0])
    if e <= s: return
    seg = sig[: e - s] * gain
    l, r = math.cos((pan + 1) * math.pi / 4), math.sin((pan + 1) * math.pi / 4)
    track[s:e, 0] += seg * l
    track[s:e, 1] += seg * r


def kick(dur=0.35):
    n = int(dur * SR); t = np.arange(n) / SR
    f = 160 * np.exp(-t * 28) + 48
    ph = np.cumsum(2 * np.pi * f / SR)
    body = np.sin(ph) * np.exp(-t * 9)
    click = rng.standard_normal(n) * np.exp(-t * 400) * 0.4
    return (body + click) * 0.9


def snare(dur=0.25):
    n = int(dur * SR); t = np.arange(n) / SR
    noise = rng.standard_normal(n) * np.exp(-t * 22)
    body = np.sin(2 * np.pi * 190 * t) * np.exp(-t * 30)
    return (noise * 0.7 + body * 0.6) * 0.8


def hat(dur=0.06, open_=False):
    n = int((0.25 if open_ else dur) * SR); t = np.arange(n) / SR
    noise = rng.standard_normal(n)
    noise = noise - np.concatenate([[0], noise[:-1]])  # crude highpass
    return noise * np.exp(-t * (14 if open_ else 70)) * 0.35


def pluck(freq, dur, kind="saw"):
    n = int(dur * SR)
    w = saw_additive(freq, n, 7, detune=0.004) if kind == "saw" else square_additive(freq, n, 5)
    return w * env_adsr(n, 0.004, dur * 0.5, 0.25, 0.08)


def bass(freq, dur):
    n = int(dur * SR)
    w = saw_additive(freq, n, 6) * 0.7 + sine(freq / 2, n) * 0.5
    return w * env_adsr(n, 0.005, 0.08, 0.7, 0.05)


def pad(freqs, dur):
    n = int(dur * SR)
    w = sum(saw_additive(f, n, 4, detune=0.006) for f in freqs) / len(freqs)
    return w * env_adsr(n, min(0.8, dur * 0.3), 0.2, 0.8, min(0.8, dur * 0.3))


def lead(freq, dur):
    n = int(dur * SR); t = np.arange(n) / SR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.minimum(t / 0.3, 1)
    ph = np.cumsum(2 * np.pi * freq * vib / SR)
    w = np.sin(ph) * 0.6 + np.sin(2 * ph) * 0.25 + np.sin(3 * ph) * 0.12
    return w * env_adsr(n, 0.02, 0.1, 0.75, 0.12)


def riser(dur):
    n = int(dur * SR); t = np.arange(n) / SR
    noise = rng.standard_normal(n)
    sweep = np.sin(np.cumsum(2 * np.pi * (200 + 1400 * (t / dur) ** 2) / SR))
    return (noise * 0.5 + sweep * 0.3) * (t / dur) ** 2 * 0.8


def boom(dur=1.2):
    n = int(dur * SR); t = np.arange(n) / SR
    return (np.sin(2 * np.pi * 42 * t) + 0.5 * np.sin(2 * np.pi * 63 * t)) * np.exp(-t * 3.5)


def echo(track, delay_sec, feedback, taps=3, gain=0.4):
    d = int(delay_sec * SR)
    out = track.copy()
    for i in range(1, taps + 1):
        g = gain * feedback ** (i - 1)
        shifted = np.zeros_like(track)
        shifted[i * d:] = track[: len(track) - i * d]
        # alternate pan for width
        shifted[:, 0] *= 1.0 if i % 2 else 0.6
        shifted[:, 1] *= 0.6 if i % 2 else 1.0
        out += shifted * g
    return out


def finalize(track, peak=0.89):
    track = track - track.mean(axis=0)
    m = np.max(np.abs(track))
    if m > 0: track *= peak / m
    return track


def write_wav(path, track):
    data = (np.clip(track, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(data.tobytes())
    print("wrote", path, f"{len(track) / SR:.1f}s")


# ---------------------------------------------------------------- Stage BGM
def make_stage_bgm():
    bpm = 142; beat = 60 / bpm; bar = beat * 4; bars = 32
    total = int(bars * bar * SR)
    drums = np.zeros((total, 2)); bassT = np.zeros((total, 2)); arpT = np.zeros((total, 2))
    padT = np.zeros((total, 2)); leadT = np.zeros((total, 2)); fxT = np.zeros((total, 2))

    E, G, A, B, C, D = 40, 43, 45, 47, 48, 50  # MIDI (E2 = 40)
    chords = [  # (root, chord tones for arp) per 2 bars
        (E, [E, G, B, E + 12]), (C, [C, E + 12, G + 12, C + 12]),
        (D, [D, A + 12 - 12, D + 12, A]), (B, [B, D + 12 + 1 - 1, B + 12, D + 12 + 1]),  # B major-ish tension (D#)
    ]
    chords[3] = (B, [B, 51, 54, 59])  # B D# F# B
    chords[2] = (D, [D, 54, 57, 62])  # D F# A D

    kick_pat = [1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0]
    bass_pat = [0, 0, 12, 0, 0, 12, 0, 7, 0, 0, 12, 0, 0, 12, 7, 12]
    for b in range(bars):
        chord = chords[(b // 2) % 4]
        root, tones = chord
        section_b = 8 <= b < 16 or 24 <= b < 32
        for s in range(16):
            t = b * bar + s * beat / 4
            if kick_pat[s]: place(drums, t, kick())
            if s in (4, 12): place(drums, t, snare())
            if s % 2 == 0 or section_b:
                place(drums, t, hat(open_=(s == 14 and b % 2 == 1)), gain=0.9 if s % 4 == 0 else 0.55)
            # bass
            place(bassT, t, bass(note(root + bass_pat[s]), beat / 4 * 0.95), gain=0.75)
            # arp (16ths over 2 octaves, direction flips each bar)
            seq = tones + [x + 12 for x in tones]
            idx = s % len(seq) if b % 2 == 0 else (len(seq) - 1 - s % len(seq))
            place(arpT, t, pluck(note(seq[idx] + 12), beat / 4 * 1.1, "square"), gain=0.28, pan=0.35 if s % 2 else -0.35)
        # pad (per bar)
        place(padT, b * bar, pad([note(x + 12) for x in tones[:3]], bar), gain=0.22)
        # risers before drops
        if b in (14, 30):
            place(fxT, b * bar, riser(bar * 2), gain=0.5)
        if b in (0, 16):
            place(fxT, b * bar, boom(), gain=0.8)

    # lead melody in B sections (E minor, 8th notes), two 8-bar phrases
    mel = [64, 67, 71, 74, 71, 67, 66, 64, 62, 64, 67, 71, 69, 67, 66, 62,
           64, 67, 71, 76, 74, 71, 69, 66, 67, 71, 74, 78, 76, 71, 69, 71]
    for start in (8, 24):
        for i, n in enumerate(mel):
            t = start * bar + i * beat / 2
            dur = beat / 2 * (1.7 if i % 8 == 7 else 0.95)
            place(leadT, t, lead(note(n), dur), gain=0.33, pan=0.1)
        # held note at the end
        place(leadT, (start + 8) * bar - beat, lead(note(76 if start == 8 else 79), beat * 2), gain=0.3)

    arpT = echo(arpT, beat * 0.75, 0.55, taps=3, gain=0.45)
    leadT = echo(leadT, beat * 1.5, 0.5, taps=2, gain=0.35)
    mix = drums * 0.95 + bassT + arpT + padT + leadT + fxT
    # loop-friendly: crossfade tail into head
    xf = int(0.05 * SR)
    mix[:xf] = mix[:xf] * np.linspace(0, 1, xf)[:, None] + mix[-xf:] * np.linspace(1, 0, xf)[:, None]
    write_wav(os.path.join(OUT, "bgm_stage.wav"), finalize(mix))


# ---------------------------------------------------------------- Title BGM
def make_title_bgm():
    bpm = 84; beat = 60 / bpm; bar = beat * 4; bars = 16
    total = int(bars * bar * SR)
    padT = np.zeros((total, 2)); arpT = np.zeros((total, 2)); drums = np.zeros((total, 2)); bassT = np.zeros((total, 2))
    prog = [[52, 55, 59, 62], [48, 52, 55, 59], [45, 48, 52, 55], [47, 51, 54, 57]]  # Em7 Cmaj7 Am7 B
    for b in range(bars):
        tones = prog[(b // 2) % 4]
        place(padT, b * bar, pad([note(x) for x in tones], bar * 1.05), gain=0.35)
        place(bassT, b * bar, bass(note(tones[0] - 12), bar * 0.9), gain=0.35)
        for s in range(8):
            t = b * bar + s * beat / 2
            n = tones[s % 4] + (12 if s >= 4 else 0) + 12
            place(arpT, t, pluck(note(n), beat * 0.9, "saw"), gain=0.16, pan=-0.5 + s / 7)
        place(drums, b * bar, kick(0.5), gain=0.45)
        place(drums, b * bar + beat * 2.5, kick(0.4), gain=0.3)
    arpT = echo(arpT, beat * 0.75, 0.6, taps=4, gain=0.5)
    mix = padT + arpT + drums + bassT
    xf = int(0.08 * SR)
    mix[:xf] = mix[:xf] * np.linspace(0, 1, xf)[:, None] + mix[-xf:] * np.linspace(1, 0, xf)[:, None]
    write_wav(os.path.join(OUT, "bgm_title.wav"), finalize(mix, 0.8))


# ---------------------------------------------------------------- SFX
def sfx_goal():
    n = int(1.6 * SR); tr = np.zeros((n, 2))
    for i, m in enumerate([64, 67, 71, 76, 83]):
        f = note(m); d = 0.9 if i < 4 else 1.3; nn = int(d * SR); t = np.arange(nn) / SR
        w = (np.sin(2 * np.pi * f * t) + 0.4 * np.sin(2 * np.pi * f * 2 * t) + 0.15 * np.sin(2 * np.pi * f * 3 * t)) * np.exp(-t * 3.2)
        place(tr, i * 0.09, w, gain=0.5, pan=-0.4 + i * 0.2)
    return finalize(tr, 0.8)


def sfx_death():
    n = int(0.7 * SR); t = np.arange(n) / SR; tr = np.zeros((n, 2))
    noise = rng.standard_normal(n) * np.exp(-t * 7)
    zap = np.sin(np.cumsum(2 * np.pi * (900 * np.exp(-t * 6) + 60) / SR)) * np.exp(-t * 5)
    place(tr, 0, noise * 0.6 + zap * 0.7)
    return finalize(tr, 0.85)


def sfx_flip():
    n = int(0.55 * SR); t = np.arange(n) / SR; tr = np.zeros((n, 2))
    noise = lowpass(rng.standard_normal(n), 1800) * np.sin(np.pi * t / 0.55) ** 1.5
    sweep = np.sin(np.cumsum(2 * np.pi * (1200 - 900 * t / 0.55) / SR)) * np.exp(-t * 4)
    place(tr, 0, noise * 1.6 + sweep * 0.25)
    return finalize(tr, 0.7)


def sfx_jump():
    n = int(0.35 * SR); t = np.arange(n) / SR; tr = np.zeros((n, 2))
    w = np.sin(np.cumsum(2 * np.pi * (220 + 700 * t / 0.35) / SR)) * np.exp(-t * 6)
    place(tr, 0, w + 0.3 * np.sin(np.cumsum(2 * np.pi * (440 + 1400 * t / 0.35) / SR)) * np.exp(-t * 8))
    return finalize(tr, 0.7)


def sfx_static():
    n = int(2.0 * SR); tr = np.zeros((n, 2))
    noise = rng.standard_normal(n)
    brown = np.cumsum(noise); brown -= np.convolve(brown, np.ones(400) / 400, mode="same")
    crackle = (rng.random(n) < 0.0015) * rng.standard_normal(n) * 3
    sig = brown / (np.max(np.abs(brown)) + 1e-9) * 0.6 + crackle * 0.2
    place(tr, 0, sig)
    xf = int(0.03 * SR)
    tr[:xf] *= np.linspace(0, 1, xf)[:, None]; tr[-xf:] *= np.linspace(1, 0, xf)[:, None]
    return finalize(tr, 0.5)


def sfx_select():
    n = int(0.18 * SR); t = np.arange(n) / SR; tr = np.zeros((n, 2))
    place(tr, 0, (np.sin(2 * np.pi * 880 * t) + 0.5 * np.sin(2 * np.pi * 1320 * t)) * np.exp(-t * 22))
    return finalize(tr, 0.6)


def sfx_fanfare():
    n = int(3.2 * SR); tr = np.zeros((n, 2))
    seq = [(0.0, 64), (0.18, 67), (0.36, 71), (0.54, 76), (0.9, 74), (1.1, 76), (1.3, 79), (1.7, 83)]
    for st, m in seq:
        f = note(m); nn = int(1.8 * SR); t = np.arange(nn) / SR
        w = (np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * 2 * f * t) + 0.2 * np.sin(2 * np.pi * 3 * f * t)) * np.exp(-t * 2.2)
        place(tr, st, w, gain=0.45, pan=math.sin(st * 3) * 0.5)
    place(tr, 1.7, boom(1.5), gain=0.5)
    return finalize(tr, 0.85)


make_stage_bgm()
make_title_bgm()
for name, fn in [("sfx_goal", sfx_goal), ("sfx_death", sfx_death), ("sfx_flip", sfx_flip), ("sfx_jump", sfx_jump),
                 ("sfx_static", sfx_static), ("sfx_select", sfx_select), ("sfx_fanfare", sfx_fanfare)]:
    write_wav(os.path.join(OUT, name + ".wav"), fn())
