"""Gera os efeitos sonoros provisórios do jogo (síntese própria, sem assets de terceiros).

Uso: python3 Ferramentas/gerar_sons.py Assets/Audio
"""
import math
import random
import struct
import sys
import wave
from pathlib import Path

RATE = 22050
random.seed(7)


def save(path, samples):
    peak = max(1e-6, max(abs(s) for s in samples))
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(32000 * 0.9 * s / peak)) for s in samples))


def env(i, n, attack=0.005, power=2.0):
    t = i / RATE
    a = min(1.0, t / attack) if attack > 0 else 1.0
    return a * (1 - i / n) ** power


def sweep(dur, f0, f1, power=2.0, harm=0.0):
    n = int(dur * RATE)
    out, phase = [], 0.0
    for i in range(n):
        f = f0 + (f1 - f0) * i / n
        phase += 2 * math.pi * f / RATE
        s = math.sin(phase) + harm * math.sin(2 * phase)
        out.append(s * env(i, n, power=power))
    return out


def noise(dur, smooth=0.0, power=2.0):
    n = int(dur * RATE)
    out, prev = [], 0.0
    for i in range(n):
        prev = prev * smooth + random.uniform(-1, 1) * (1 - smooth)
        out.append(prev * env(i, n, power=power))
    return out


def mix(*tracks):
    n = max(len(t) for t, _ in tracks)
    out = [0.0] * n
    for t, g in tracks:
        for i, s in enumerate(t):
            out[i] += s * g
    return out


def concat(*parts):
    return [s for p in parts for s in p]


sounds = {
    "salto": mix((sweep(0.14, 260, 620, power=1.5), 1.0), (noise(0.14, 0.6), 0.25)),
    "pouso": mix((sweep(0.12, 110, 60, power=3), 1.0), (noise(0.1, 0.85, power=3), 0.6)),
    "passo": mix((noise(0.06, 0.8, power=3), 1.0), (sweep(0.05, 140, 90, power=3), 0.4)),
    "deslize": noise(0.4, 0.55, power=1.2),
    "falha": mix((sweep(0.55, 520, 120, power=1.2, harm=0.3), 1.0), (noise(0.25, 0.7, power=3), 0.5)),
    "estacao_ok": concat(sweep(0.12, 660, 660, power=1.5), sweep(0.25, 990, 990, power=2)),
    "caixa": mix((noise(0.18, 0.75, power=2.5), 1.0), (sweep(0.18, 220, 150, power=3), 0.7)),
    "tronco_aviso": mix(
        ([math.sin(2 * math.pi * 95 * i / RATE) * (0.6 + 0.4 * math.sin(2 * math.pi * 14 * i / RATE)) * env(i, int(0.45 * RATE), 0.03, 1.2)
          for i in range(int(0.45 * RATE))], 1.0),
        (noise(0.45, 0.9, power=1.2), 0.5),
    ),
}

out_dir = Path(sys.argv[1])
out_dir.mkdir(parents=True, exist_ok=True)
for name, data in sounds.items():
    save(out_dir / f"{name}.wav", data)
    print(name, f"{len(data) / RATE:.2f}s")
