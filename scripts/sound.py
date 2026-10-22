# 게임 소리를 코드로 합성한다 (효과음, 배경음). 외부 소리 파일을 쓰지 않으니 저작권 걱정이 없다.
# 사용법: python3 scripts/sound.py            모두 만들기
#         python3 scripts/sound.py plop town   이름을 골라 만들기
# 결과: godot/audio/sfx/<이름>.ogg, godot/audio/bgm/<이름>.ogg (ffmpeg 로 Vorbis 인코딩)
# 배경음은 끝이 처음으로 이어지게 꼬리를 앞에 겹쳐 둔다. 고닷에서 Loop 를 켜서 튼다.

import os
import subprocess
import sys
import tempfile

import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, fftconvolve, lfilter, sosfilt

SR = 44100
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'godot', 'audio')
rng = np.random.default_rng(2026)


# ───────── 기본 재료 ─────────

def t_of(dur):
    return np.arange(int(dur * SR)) / SR


def env(n, a=0.005, d=0.0, s=1.0, r=0.05, hold=None):
    """간단한 ADSR. hold 는 서스테인 길이(초), 없으면 n 에 맞춘다"""
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    h_n = n - a_n - d_n - r_n if hold is None else int(hold * SR)
    h_n = max(0, h_n)
    e = np.concatenate([
        np.linspace(0, 1, a_n, endpoint=False),
        np.linspace(1, s, d_n, endpoint=False),
        np.full(h_n, s),
        np.linspace(s, 0, r_n),
    ])
    if len(e) < n:
        e = np.concatenate([e, np.zeros(n - len(e))])
    return e[:n]


def expdecay(n, tau):
    return np.exp(-np.arange(n) / SR / tau)


def lp(x, hz, order=2):
    sos = butter(order, min(hz, SR / 2 - 100) / (SR / 2), 'low', output='sos')
    return sosfilt(sos, x)


def hp(x, hz, order=2):
    sos = butter(order, hz / (SR / 2), 'high', output='sos')
    return sosfilt(sos, x)


def bp(x, lo, hi, order=2):
    sos = butter(order, [lo / (SR / 2), min(hi, SR / 2 - 100) / (SR / 2)], 'band', output='sos')
    return sosfilt(sos, x)


def noise(n):
    return rng.uniform(-1, 1, n)


def brown(n):
    b = np.cumsum(rng.normal(0, 1, n))
    b = hp(b, 20)
    return b / (np.max(np.abs(b)) + 1e-9)


def midi(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def sweep_sine(f0, f1, dur, curve=1.0):
    n = int(dur * SR)
    k = np.linspace(0, 1, n) ** curve
    f = f0 + (f1 - f0) * k
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def mix_at(buf, x, at, gain=1.0):
    """buf 에 x 를 at 초 자리부터 더한다 (모노 또는 스테레오)"""
    i = int(at * SR)
    if i >= len(buf):
        return
    j = min(len(buf), i + len(x))
    buf[i:j] += gain * x[:j - i]


def pan(x, p):
    """p: -1 왼쪽 ~ 1 오른쪽. 스테레오 (n, 2)"""
    l = np.cos((p + 1) * np.pi / 4)
    r = np.sin((p + 1) * np.pi / 4)
    return np.stack([x * l, x * r], axis=1)


def reverb_ir(seconds=1.8, decay=0.5, bright=6000, stereo=True):
    n = int(seconds * SR)
    chans = []
    for _ in range(2 if stereo else 1):
        ir = noise(n) * expdecay(n, decay)
        ir = lp(ir, bright)
        ir[0] = 0
        chans.append(ir / np.sqrt(np.sum(ir ** 2)))
    return chans


def reverb(x, wet=0.25, seconds=1.8, decay=0.5, bright=6000):
    """스테레오 입력 (n, 2) → 같은 길이 + 꼬리"""
    irs = reverb_ir(seconds, decay, bright)
    if x.ndim == 1:
        x = np.stack([x, x], axis=1)
    out = np.zeros((len(x) + len(irs[0]) - 1, 2))
    for c in range(2):
        out[:len(x), c] += x[:, c] * (1 - wet)
        out[:, c] += wet * fftconvolve(x[:, c], irs[c]) * 0.6
    return out


# ───────── 악기 ─────────

def pluck(f, dur=1.6, bright=0.5, damp=0.996):
    """Karplus-Strong 현 뜯기 (기타, 우쿨렐레 같은 소리)"""
    n = int(dur * SR)
    N = int(SR / f)
    x = np.zeros(n)
    burst = lp(noise(N), 1500 + 9000 * bright)
    x[:N] = burst
    a = np.zeros(N + 2)
    a[0] = 1
    a[N] = -damp / 2
    a[N + 1] = -damp / 2
    y = lfilter([1.0], a, x)
    y = lp(y, 2500 + 6000 * bright)
    y *= env(n, a=0.002, r=0.05)
    return y / (np.max(np.abs(y)) + 1e-9)


def epiano(f, dur=2.0, vel=1.0):
    """FM 전자 피아노 (부드러운 종소리 섞인 건반)"""
    t = t_of(dur)
    n = len(t)
    mod_i = 1.6 * vel * expdecay(n, 0.35)
    mod = np.sin(2 * np.pi * f * t) * mod_i
    car = np.sin(2 * np.pi * f * t + mod)
    tine = np.sin(2 * np.pi * f * 7.0 * t) * 0.08 * vel * expdecay(n, 0.08)
    y = (car + tine) * expdecay(n, 0.9) * env(n, a=0.003, r=0.08)
    return y


def marimba(f, dur=1.2, vel=1.0):
    t = t_of(dur)
    n = len(t)
    y = np.sin(2 * np.pi * f * t) * expdecay(n, 0.45)
    y += 0.35 * np.sin(2 * np.pi * f * 3.93 * t) * expdecay(n, 0.07)
    y += 0.12 * np.sin(2 * np.pi * f * 9.2 * t) * expdecay(n, 0.02)
    return y * vel * env(n, a=0.002, r=0.05)


def bell(f, dur=2.5, vel=1.0):
    """맑은 종 (비정수 배음)"""
    t = t_of(dur)
    n = len(t)
    y = np.zeros(n)
    for ratio, amp, tau in [(1, 1, 1.2), (2.0, 0.5, 0.8), (2.76, 0.35, 0.5), (5.4, 0.2, 0.25), (8.9, 0.1, 0.12)]:
        y += amp * np.sin(2 * np.pi * f * ratio * t) * expdecay(n, tau)
    return y * vel * env(n, a=0.001, r=0.05) / 2.2


def musicbox(f, dur=1.4, vel=1.0):
    t = t_of(dur)
    n = len(t)
    y = np.sin(2 * np.pi * f * t) * expdecay(n, 0.6)
    y += 0.3 * np.sin(2 * np.pi * f * 4.0 * t) * expdecay(n, 0.12)
    y += 0.1 * np.sin(2 * np.pi * f * 6.5 * t) * expdecay(n, 0.05)
    return y * vel * env(n, a=0.001, r=0.05)


def pad(freqs, dur, attack=1.2, release=1.2, cutoff=1400, detune=0.004):
    """천천히 부풀었다 사라지는 바탕 화음"""
    t = t_of(dur)
    n = len(t)
    y = np.zeros(n)
    for f in freqs:
        for d in (-detune, 0, detune):
            ph = rng.uniform(0, 1)
            saw = 2 * ((f * (1 + d) * t + ph) % 1) - 1
            y += saw
    y = lp(y, cutoff, 2) / (3 * len(freqs))
    return y * env(n, a=attack, r=release)


def bass(f, dur, vel=1.0):
    t = t_of(dur)
    n = len(t)
    tri = 2 * np.abs(2 * ((f * t) % 1) - 1) - 1
    y = 0.75 * np.sin(2 * np.pi * f * t) + 0.25 * tri
    return y * vel * env(n, a=0.008, d=0.15, s=0.6, r=0.08)


def kick(vel=1.0):
    n = int(0.35 * SR)
    y = sweep_sine(120, 45, 0.35, 0.3) * expdecay(n, 0.12)
    return y * vel


def shaker(vel=1.0, dur=0.09):
    n = int(dur * SR)
    y = hp(noise(n), 6000) * env(n, a=0.01, r=dur * 0.7)
    return y * vel * 0.5


def rim(vel=1.0):
    n = int(0.06 * SR)
    y = bp(noise(n), 1500, 4000) * expdecay(n, 0.01) + 0.6 * np.sin(2 * np.pi * 1700 * t_of(0.06)) * expdecay(n, 0.015)
    return y * vel


# ───────── 저장 ─────────

def norm(x, peak=0.9):
    m = np.max(np.abs(x)) + 1e-9
    return x / m * peak


def save(kind, name, x, quality=4):
    folder = os.path.join(ROOT, kind)
    os.makedirs(folder, exist_ok=True)
    if kind == 'sfx':
        # 끝을 짧게 줄여 '틱' 하는 소리를 없앤다
        f = min(len(x), int(0.015 * SR))
        fade = np.linspace(1, 0, f)
        x = x.copy()
        x[-f:] = (x[-f:].T * fade).T
    x = np.clip(x, -1, 1)
    pcm = (x * 32767).astype(np.int16)
    with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as f:
        tmp = f.name
    wavfile.write(tmp, SR, pcm)
    out = os.path.join(folder, name + '.ogg')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', tmp, '-c:a', 'libvorbis', '-q:a', str(quality), out], check=True)
    os.remove(tmp)
    print('저장', os.path.relpath(out), f'{len(x) / SR:.2f}초', f'{os.path.getsize(out) // 1024}KB')


def loopify(x, length):
    """length 샘플을 넘는 꼬리(잔향)를 앞에 겹쳐서 끝과 처음이 자연스럽게 이어지게 한다"""
    out = x[:length].copy()
    tail = x[length:]
    while len(tail) > 0:
        k = min(len(tail), length)
        out[:k] += tail[:k]
        tail = tail[k:]
    return out


# ───────── 효과음 ─────────

def sfx_tap():
    n = int(0.09 * SR)
    y = sweep_sine(900, 520, 0.09, 0.5) * expdecay(n, 0.018)
    y += 0.3 * bp(noise(n), 2000, 5000) * expdecay(n, 0.004)
    return norm(y, 0.55)


def sfx_pop():
    """모달이 톡 튀어나올 때"""
    n = int(0.16 * SR)
    y = sweep_sine(380, 900, 0.16, 0.6) * env(n, a=0.004, r=0.1)
    return norm(lp(y, 3000), 0.5)


def sfx_page():
    n = int(0.22 * SR)
    y = bp(noise(n), 1500, 7000) * env(n, a=0.03, r=0.15) * (0.5 + 0.5 * np.sin(np.linspace(0, 6, n)))
    return norm(y, 0.4)


def sfx_whoosh():
    """낚싯대 휘두르기와 줄 풀리는 소리"""
    dur = 0.55
    n = int(dur * SR)
    x = noise(n)
    # 바람 소리: 띠 필터 가운데가 올라갔다 내려간다
    out = np.zeros(n)
    seg = 256
    for i in range(0, n, seg):
        k = i / n
        fc = 700 + 2600 * np.sin(np.pi * k) ** 1.5
        out[i:i + seg] = bp(x[max(0, i - 2048):i + seg], fc * 0.6, fc * 1.4)[-len(out[i:i + seg]):]
    out *= env(n, a=0.12, r=0.3)
    # 릴에서 줄이 풀리는 가는 지르르
    zip_ = bp(noise(n), 5000, 9000) * (np.sin(np.linspace(0, 140 * np.pi, n)) > 0.2) * env(n, a=0.2, r=0.2) * 0.25
    return norm(out + zip_, 0.6)


def water_drop(f0=900, f1=1800, dur=0.12):
    """물방울 '똑': 위로 휘는 짧은 사인 (거품 공명)"""
    n = int(dur * SR)
    return sweep_sine(f0, f1, dur, 0.5) * expdecay(n, dur / 4)


def splash(dur=0.6, size=1.0):
    n = int(dur * SR)
    y = lp(noise(n), 2500 + 2500 * size) * expdecay(n, dur / 4) * env(n, a=0.004, r=0.1)
    y = hp(y, 200)
    # 튀는 물방울
    for _ in range(int(6 * size)):
        at = rng.uniform(0.02, dur * 0.7)
        d = water_drop(rng.uniform(600, 1300), rng.uniform(1500, 2600), 0.08) * rng.uniform(0.1, 0.3)
        mix_at(y, d, at)
    return y


def sfx_plop():
    """찌가 물에 퐁당"""
    n = int(0.5 * SR)
    y = np.zeros(n)
    mix_at(y, sweep_sine(260, 620, 0.16, 0.4) * expdecay(int(0.16 * SR), 0.05), 0, 0.9)
    mix_at(y, splash(0.45, 0.4), 0.0, 0.35)
    return norm(lp(y, 5000), 0.75)


def sfx_nibble():
    """속임 입질: 찌를 톡"""
    n = int(0.18 * SR)
    y = np.zeros(n)
    mix_at(y, water_drop(700, 1300, 0.09), 0, 0.8)
    mix_at(y, water_drop(900, 1500, 0.07), 0.07, 0.4)
    return norm(y, 0.45)


def sfx_bite():
    """진짜 입질: 찌가 쑥 들어간다"""
    n = int(0.6 * SR)
    y = np.zeros(n)
    mix_at(y, sweep_sine(520, 160, 0.25, 0.5) * expdecay(int(0.25 * SR), 0.1), 0, 0.8)
    mix_at(y, splash(0.5, 0.7), 0.02, 0.6)
    mix_at(y, water_drop(500, 1100, 0.12), 0.05, 0.5)
    return norm(y, 0.85)


def sfx_strike():
    """챔질: 대를 휙 채고 줄이 팽팽해진다"""
    dur = 0.35
    n = int(dur * SR)
    y = bp(noise(n), 1500, 6000) * env(n, a=0.01, r=0.12, hold=0.06) * 0.7
    tw = sweep_sine(180, 120, dur, 0.5) * expdecay(n, 0.12)
    tw += 0.4 * sweep_sine(540, 360, dur, 0.5) * expdecay(n, 0.08)
    mix_at(y, tw, 0.05, 0.7)
    return norm(y, 0.8)


def sfx_reel():
    """릴 감기 (짧은 반복): 톱니 딸깍이 빠르게"""
    dur = 0.48
    n = int(dur * SR)
    y = np.zeros(n)
    step = dur / 12
    for i in range(12):
        click = bp(noise(int(0.012 * SR)), 2500, 7000) * expdecay(int(0.012 * SR), 0.003)
        mix_at(y, click, i * step, 0.8 if i % 2 == 0 else 0.55)
    hum = sweep_sine(210, 210, dur) * 0.08 + 0.05 * bp(noise(n), 300, 900)
    return norm(y + hum, 0.45)


def sfx_surge():
    """물고기 저항: 물을 철썩 차고 줄이 끌려간다"""
    n = int(0.55 * SR)
    y = splash(0.55, 1.0) * 0.8
    drag = bp(noise(n), 3000, 8000) * env(n, a=0.02, r=0.3) * 0.25
    return norm(y + drag, 0.75)


def sfx_snap():
    """줄이 툭 끊어진다"""
    dur = 0.7
    n = int(dur * SR)
    y = np.zeros(n)
    crack = bp(noise(int(0.03 * SR)), 2000, 10000) * expdecay(int(0.03 * SR), 0.006)
    mix_at(y, crack, 0, 1.0)
    twang = sweep_sine(330, 140, 0.6, 0.4) * expdecay(int(0.6 * SR), 0.18)
    twang += 0.5 * sweep_sine(660, 280, 0.6, 0.4) * expdecay(int(0.6 * SR), 0.1)
    mix_at(y, twang, 0.01, 0.5)
    return norm(y, 0.8)


def sfx_slip():
    """바늘이 빠졌다: 힘 빠지는 짧은 소리"""
    n = int(0.45 * SR)
    y = sweep_sine(420, 210, 0.45, 0.7) * env(n, a=0.01, r=0.3) * 0.6
    mix_at(y, splash(0.35, 0.4), 0, 0.4)
    return norm(lp(y, 3000), 0.55)


def sfx_land():
    """끌어올렸다: 물을 박차고 나오는 소리"""
    n = int(0.9 * SR)
    y = np.zeros(n)
    mix_at(y, splash(0.8, 1.4), 0, 0.9)
    up = sweep_sine(200, 700, 0.3, 0.6) * env(int(0.3 * SR), a=0.02, r=0.15) * 0.35
    mix_at(y, up, 0, 1)
    return norm(y, 0.8)


def jingle(notes, inst=bell, step=0.09, tail=1.2, vel=1.0, width=0.4):
    total = step * len(notes) + tail
    out = np.zeros((int(total * SR), 2))
    for i, m in enumerate(notes):
        if m is None:
            continue
        x = inst(midi(m), tail + 0.3, vel)
        mix_at(out, pan(x, width * (i / max(1, len(notes) - 1) * 2 - 1)), i * step)
    return out


def sfx_catch():
    """낚았다! 밝은 짧은 가락"""
    j = jingle([72, 76, 79, 84], musicbox, 0.085, 1.0)
    return norm(reverb(j, 0.25, 1.2, 0.35), 0.7)


def sfx_newfish():
    """새 어종: 조금 더 긴 가락"""
    j = jingle([67, 72, 76, 79, 76, 79, 84], musicbox, 0.1, 1.4)
    k = jingle([None, None, None, 60, None, None, 67], marimba, 0.1, 1.4, width=0)
    j[:len(k)] += 0.5 * k
    return norm(reverb(j, 0.3, 1.5, 0.45), 0.75)


def sfx_big():
    """큰 놈이다! 북 두 번과 낮게 솟는 소리"""
    dur = 1.0
    n = int(dur * SR)
    y = np.zeros(n)
    mix_at(y, kick(1.0), 0, 1)
    mix_at(y, kick(0.8), 0.16, 1)
    rise = sweep_sine(110, 330, 0.8, 1.5) * env(int(0.8 * SR), a=0.3, r=0.2) * 0.4
    rise += 0.3 * sweep_sine(165, 495, 0.8, 1.5) * env(int(0.8 * SR), a=0.3, r=0.2)
    mix_at(y, lp(rise, 1500), 0.1, 1)
    return norm(reverb(pan(y, 0), 0.2, 1.0, 0.3), 0.85)


def sfx_coin():
    """돈 받기: 동전 두 번 짤랑"""
    out = np.zeros(int(0.7 * SR))
    for i, f in enumerate([1975, 2637]):
        n = int(0.5 * SR)
        t = t_of(0.5)
        x = (np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 1.5 * t) + 0.25 * np.sin(2 * np.pi * f * 2.4 * t)) * expdecay(n, 0.12)
        mix_at(out, x, i * 0.07, 0.6)
    return norm(out, 0.55)


def sfx_sold():
    """낙찰! 종 한 번"""
    x = bell(midi(84), 2.0) + 0.6 * bell(midi(91), 2.0)
    return norm(reverb(pan(x, 0), 0.3, 1.6, 0.5), 0.7)


def sfx_tick():
    """시세가 뛰는 동안 딸깍"""
    n = int(0.04 * SR)
    y = np.sin(2 * np.pi * 2200 * t_of(0.04)) * expdecay(n, 0.006)
    return norm(y, 0.3)


def sfx_stamp():
    """도장 쾅"""
    n = int(0.35 * SR)
    y = kick(0.9)[:n] * 0.8
    y += lp(noise(n), 1200) * expdecay(n, 0.03) * 0.6
    return norm(y, 0.8)


def meow(f0=620, dur=0.55, sad=False):
    """고양이 야옹: 음높이가 올랐다 내리고 입 모양(포먼트)이 '이-아-우'로 바뀐다"""
    n = int(dur * SR)
    k = np.linspace(0, 1, n)
    contour = f0 * (1 + 0.35 * np.sin(np.pi * k ** 0.7)) * (1 - 0.25 * k if not sad else 1 - 0.45 * k)
    contour *= 1 + 0.01 * np.sin(2 * np.pi * 6 * k * dur)
    phase = 2 * np.pi * np.cumsum(contour) / SR
    src = np.zeros(n)
    for h in range(1, 14):
        src += np.sin(h * phase) / h ** 1.1
    out = np.zeros(n)
    seg = 512
    for i in range(0, n, seg):
        kk = i / n
        f1 = 500 + 600 * np.sin(np.pi * min(1, kk * 1.4))
        f2 = 1800 + 900 * np.sin(np.pi * min(1, kk * 1.2)) - 600 * kk
        a = src[max(0, i - 2048):i + seg]
        y = bp(a, f1 * 0.7, f1 * 1.3) + 0.7 * bp(a, f2 * 0.8, f2 * 1.2)
        out[i:i + seg] = y[-len(out[i:i + seg]):]
    out *= env(n, a=0.04, r=0.18)
    out += 0.04 * bp(noise(n), 3000, 8000) * env(n, a=0.02, r=0.1)
    return out


def sfx_meow():
    return norm(meow(), 0.55)


def sfx_meow_short():
    return norm(meow(760, 0.3), 0.5)


def sfx_hiss():
    """혼난 고등어: 짧게 앵"""
    return norm(meow(900, 0.25, True), 0.45)


def sfx_sneak():
    """고등어가 살금살금: 피치카토 네 걸음"""
    out = np.zeros(int(1.2 * SR))
    for i, m in enumerate([57, 60, 57, 62]):
        x = pluck(midi(m), 0.5, 0.3, 0.99)
        mix_at(out, x, i * 0.2, 0.5)
    return norm(out, 0.5)


def sfx_steal():
    """뺏겼다: 장난스러운 내림 가락"""
    j = jingle([79, 75, 72, 67], marimba, 0.08, 0.8)
    return norm(j, 0.6)


def sfx_sizzle():
    """생선이 지글지글 (간 맞추기 시작)"""
    dur = 1.6
    n = int(dur * SR)
    y = hp(noise(n), 3000) * 0.4
    pops = np.zeros(n)
    for _ in range(40):
        at = rng.uniform(0, dur - 0.02)
        p = bp(noise(int(0.01 * SR)), 2000, 8000) * expdecay(int(0.01 * SR), 0.002)
        mix_at(pops, p, at, rng.uniform(0.3, 1))
    y = (y * (0.7 + 0.3 * np.sin(np.linspace(0, 40, n))) + pops) * env(n, a=0.1, r=0.4)
    return norm(y, 0.5)


def sfx_good():
    """손님이 기뻐한다"""
    j = jingle([76, 79, 84], bell, 0.1, 1.0)
    return norm(reverb(j, 0.25, 1.2, 0.4), 0.6)


def sfx_okay():
    j = jingle([72, 74], marimba, 0.12, 0.6)
    return norm(j, 0.5)


def sfx_bad():
    """손님이 실망했다"""
    j = jingle([67, 63], marimba, 0.18, 0.7, 0.8)
    return norm(lp(j, 2000), 0.5)


def sfx_door():
    """가게 문 종 딸랑"""
    out = np.zeros(int(1.4 * SR))
    for i in range(3):
        mix_at(out, bell(midi(88 + (i % 2) * 3), 1.0, 0.6), i * 0.07)
    return norm(reverb(pan(out, 0), 0.2), 0.55)


def sfx_step():
    n = int(0.07 * SR)
    y = lp(noise(n), 900) * expdecay(n, 0.015)
    return norm(y, 0.35)


SFX = {
    'tap': sfx_tap, 'pop': sfx_pop, 'page': sfx_page,
    'whoosh': sfx_whoosh, 'plop': sfx_plop, 'nibble': sfx_nibble, 'bite': sfx_bite, 'strike': sfx_strike,
    'reel': sfx_reel, 'surge': sfx_surge, 'snap': sfx_snap, 'slip': sfx_slip, 'land': sfx_land,
    'catch': sfx_catch, 'newfish': sfx_newfish, 'big': sfx_big,
    'coin': sfx_coin, 'sold': sfx_sold, 'tick': sfx_tick, 'stamp': sfx_stamp,
    'meow': sfx_meow, 'meow-short': sfx_meow_short, 'hiss': sfx_hiss, 'sneak': sfx_sneak, 'steal': sfx_steal,
    'sizzle': sfx_sizzle, 'good': sfx_good, 'okay': sfx_okay, 'bad': sfx_bad, 'door': sfx_door, 'step': sfx_step,
}


# ───────── 배경음 ─────────

# 음 이름 → MIDI
NOTE = {'C': 0, 'C#': 1, 'Db': 1, 'D': 2, 'D#': 3, 'Eb': 3, 'E': 4, 'F': 5, 'F#': 6, 'Gb': 6, 'G': 7, 'G#': 8, 'Ab': 8, 'A': 9, 'A#': 10, 'Bb': 10, 'B': 11}
QUALITY = {'': [0, 4, 7], 'm': [0, 3, 7], 'maj7': [0, 4, 7, 11], 'm7': [0, 3, 7, 10], '7': [0, 4, 7, 10], 'add9': [0, 4, 7, 14], 'sus2': [0, 2, 7], 'sus4': [0, 5, 7], '6': [0, 4, 7, 9]}


def chord(name, octave=4):
    root = name[:2] if len(name) > 1 and name[1] in '#b' else name[:1]
    q = name[len(root):]
    base = 12 * (octave + 1) + NOTE[root]
    return [base + i for i in QUALITY[q]], base


def waves(dur, swell=7.0, level=1.0):
    """파도: 천천히 밀려왔다 빠지는 갈색 소음 + 부서지는 거품 (스테레오). 끝이 처음으로 이어진다"""
    n0 = int(dur * SR)
    xf = 2 * SR
    n = n0 + xf
    out = np.zeros((n, 2))
    for c in range(2):
        b = lp(brown(n), 500) * 0.8
        foam = lp(hp(noise(n), 1200), 5000) * 0.25
        k = np.arange(n) / SR
        ph = rng.uniform(0, 2 * np.pi)
        # 여러 주기를 겹쳐서 파도 크기가 매번 다르게
        sw = 0.55 + 0.45 * np.sin(2 * np.pi * k / swell + ph) * (0.7 + 0.3 * np.sin(2 * np.pi * k / (swell * 2.7) + ph * 2))
        sw = np.clip(sw, 0.05, 1)
        out[:, c] = (b * sw + foam * sw ** 3) * level
    # 넘친 2초를 앞 2초에 같은 힘으로 겹친다
    k = np.linspace(0, np.pi / 2, xf)[:, None]
    out[:xf] = out[:xf] * np.sin(k) + out[n0:] * np.cos(k)
    return out[:n0]


def gull(dur=0.5):
    """멀리 갈매기"""
    n = int(dur * SR)
    k = np.linspace(0, 1, n)
    f = 1500 + 700 * np.sin(np.pi * k) - 400 * k
    s = np.sin(2 * np.pi * np.cumsum(f) / SR)
    s = bp(s + 0.3 * np.sin(4 * np.pi * np.cumsum(f) / SR), 1000, 4000)
    return s * env(n, a=0.05, r=0.2) * 0.15


def bgm_town():
    """마을: 따뜻한 기타 아르페지오와 실로폰 가락. 88bpm, F장조, 16마디"""
    bpm = 88
    beat = 60 / bpm
    bar = beat * 4
    prog = ['Fmaj7', 'Dm7', 'Bbmaj7', 'C7', 'Fmaj7', 'Am7', 'Bbmaj7', 'Csus4',
            'Dm7', 'Am7', 'Bbmaj7', 'Fmaj7', 'Gm7', 'Am7', 'Bbmaj7', 'C7']
    length = int(bar * len(prog) * SR)
    out = np.zeros((length + 4 * SR, 2))
    # 가락 (마디마다 8분음표 8칸, None 은 쉼)
    melody = [
        [72, None, 74, 76, None, 72, None, None], [77, None, 76, 74, None, None, 72, None],
        [74, None, 72, 70, None, 69, None, None], [67, None, None, None, 72, None, None, None],
        [72, None, 74, 76, None, 79, None, 77], [76, None, None, 72, None, None, 69, None],
        [70, None, 72, 74, None, 77, None, 76], [74, None, None, None, None, None, None, None],
        [77, None, 76, 74, None, 72, None, None], [76, None, 74, 72, None, 69, None, None],
        [70, None, 72, 74, None, 70, None, 69], [72, None, None, None, 69, None, 72, None],
        [70, None, 69, 67, None, 70, None, None], [72, None, 74, 76, None, 72, None, None],
        [77, None, 76, 74, None, 72, None, 70], [72, None, None, None, None, None, None, None],
    ]
    for i, name in enumerate(prog):
        t0 = i * bar
        notes, root = chord(name, 3)
        # 베이스: 1, 3박
        mix_at(out, pan(bass(midi(root - 12), beat * 1.8, 0.55), 0), t0)
        mix_at(out, pan(bass(midi(root - 12 + 7), beat * 1.8, 0.4), 0), t0 + 2 * beat)
        # 기타 아르페지오 (8분음표)
        pattern = [0, 1, 2, 3 if len(notes) > 3 else 1, 2, 1, 2, 0]
        for j, p in enumerate(pattern):
            m = notes[min(p, len(notes) - 1)] + (12 if p >= 2 else 0)
            mix_at(out, pan(pluck(midi(m), 1.4, 0.35), -0.35), t0 + j * beat / 2, 0.22)
        for j, m in enumerate(melody[i]):
            if m is not None:
                mix_at(out, pan(marimba(midi(m), 1.0, 0.9), 0.3), t0 + j * beat / 2, 0.28)
        # 부드러운 바탕
        mix_at(out, pan(pad([midi(n + 12) for n in notes[:3]], bar + 0.6, 0.6, 0.6, 1100), 0), t0, 0.05)
        # 살랑 셰이커
        for j in range(8):
            mix_at(out, pan(shaker(0.12 if j % 2 else 0.06), 0.5), t0 + j * beat / 2)
    out = reverb(out, 0.22, 2.0, 0.6)
    return loopify(out, length)


def bgm_sea():
    """방파제 낚시: 파도와 갈매기 위에 느린 마림바. 72bpm, D장조, 8마디"""
    bpm = 72
    beat = 60 / bpm
    bar = beat * 4
    prog = ['Dadd9', 'Bm7', 'Gmaj7', 'Asus4', 'Dadd9', 'F#m', 'Gmaj7', 'A']
    length = int(bar * len(prog) * SR)
    out = np.zeros((length + 4 * SR, 2))
    out[:length] += waves(length / SR, 6.5, 0.35)
    for at in [3.1, 12.5, 21.0]:
        mix_at(out, pan(gull(0.45), 0.6), at)
        mix_at(out, pan(gull(0.35), 0.6), at + 0.5, 0.7)
    for i, name in enumerate(prog):
        t0 = i * bar
        notes, root = chord(name, 4)
        mix_at(out, pan(pad([midi(n) for n in notes[:3]], bar + 1.0, 1.5, 1.5, 900), 0), t0 - 0.3 if i else 0, 0.07)
        mix_at(out, pan(bass(midi(root - 24), bar * 0.9, 0.35), 0), t0)
        # 마림바: 화음 음을 느리게 굴린다
        arp = [0, 2, 1, 2] if i % 2 == 0 else [1, 2, 0, None]
        for j, p in enumerate(arp):
            if p is None:
                continue
            m = notes[p] + 12
            mix_at(out, pan(marimba(midi(m), 1.4, 0.8), -0.3 + 0.2 * j), t0 + j * beat, 0.16)
    out = reverb(out, 0.3, 2.6, 0.9, 5000)
    return loopify(out, length)


def bgm_market():
    """위판장: 통통 튀는 베이스와 경쾌한 뜯기. 112bpm, G장조, 8마디"""
    bpm = 112
    beat = 60 / bpm
    bar = beat * 4
    prog = ['G', 'Em', 'C', 'D', 'G', 'Em', 'Am7', 'D7']
    length = int(bar * len(prog) * 2 * SR)
    out = np.zeros((length + 4 * SR, 2))
    melody = [[79, None, 81, 83, None, 79, 76, None], [79, None, None, 76, None, 74, None, None],
              [72, None, 74, 76, None, 79, None, 76], [74, None, None, None, 78, None, 81, None],
              [83, None, 81, 79, None, 76, None, 79], [81, None, 79, 76, None, None, 74, None],
              [72, None, 76, 79, None, 76, None, 72], [74, None, None, None, None, None, None, None]]
    for rep in range(2):
        for i, name in enumerate(prog):
            t0 = (rep * len(prog) + i) * bar
            notes, root = chord(name, 3)
            for j, off in enumerate([0, 7, 12, 7]):
                mix_at(out, pan(bass(midi(root - 12 + off), beat * 0.45, 0.5), 0), t0 + j * beat)
            # 뒷박 화음 뜯기
            for j in range(4):
                for k, m in enumerate(notes[:3]):
                    mix_at(out, pan(pluck(midi(m + 12), 0.5, 0.6, 0.99), 0.4), t0 + j * beat + beat / 2 + k * 0.008, 0.12)
            mix_at(out, pan(kick(0.5), 0), t0)
            mix_at(out, pan(kick(0.4), 0), t0 + 2 * beat)
            for j in range(8):
                mix_at(out, pan(shaker(0.2 if j % 2 else 0.1, 0.07), -0.4), t0 + j * beat / 2)
            mix_at(out, pan(rim(0.25), 0.2), t0 + beat)
            mix_at(out, pan(rim(0.25), 0.2), t0 + 3 * beat)
            if rep == 1:
                for j, m in enumerate(melody[i]):
                    if m is not None:
                        mix_at(out, pan(musicbox(midi(m), 0.8, 0.9), -0.2), t0 + j * beat / 2, 0.2)
    out = reverb(out, 0.15, 1.4, 0.4)
    return loopify(out, length)


def bgm_truck():
    """트럭 장사: 우쿨렐레 같은 스트럼과 걷는 베이스. 96bpm, C장조, 8마디 ×2"""
    bpm = 96
    beat = 60 / bpm
    bar = beat * 4
    prog = ['Cmaj7', 'Am7', 'Dm7', 'G7', 'Em7', 'Am7', 'Dm7', 'G7']
    length = int(bar * len(prog) * 2 * SR)
    out = np.zeros((length + 4 * SR, 2))
    melody = [[76, None, 74, 72, None, 74, None, 76], [79, None, None, 76, None, None, None, None],
              [77, None, 76, 74, None, 72, None, 74], [71, None, None, None, 74, None, 77, None],
              [79, None, 77, 76, None, 74, None, 76], [72, None, None, 69, None, 72, None, None],
              [74, None, 77, 81, None, 79, None, 77], [76, None, None, None, 74, None, None, None]]
    strum = [(0, 1.0, 'd'), (1, 0.5, 'u'), (1.5, 0.6, 'u'), (2, 0.9, 'd'), (3, 0.5, 'u'), (3.5, 0.6, 'd')]
    for rep in range(2):
        for i, name in enumerate(prog):
            t0 = (rep * len(prog) + i) * bar
            notes, root = chord(name, 4)
            for at, vel, d in strum:
                order = notes if d == 'd' else notes[::-1]
                for k, m in enumerate(order):
                    mix_at(out, pan(pluck(midi(m), 0.7, 0.45, 0.993), -0.3), t0 + at * beat + k * 0.012, 0.09 * vel)
            walk = [root, root + 4, root + 7, root + 9] if rep == 0 else [root, root + 7, root + 12, root + 7]
            for j, m in enumerate(walk):
                mix_at(out, pan(bass(midi(m - 24), beat * 0.9, 0.5), 0), t0 + j * beat)
            mix_at(out, pan(kick(0.35), 0), t0)
            mix_at(out, pan(rim(0.18), 0.3), t0 + beat)
            mix_at(out, pan(rim(0.18), 0.3), t0 + 3 * beat)
            for j, m in enumerate(melody[i]):
                if m is not None and (rep == 1 or j < 4):
                    mix_at(out, pan(epiano(midi(m), 1.2, 0.7), 0.25), t0 + j * beat / 2, 0.2)
    out = reverb(out, 0.18, 1.6, 0.5)
    return loopify(out, length)


def bgm_title():
    """타이틀: 파도 위로 느린 건반. 66bpm, A단조 → C장조, 8마디"""
    bpm = 66
    beat = 60 / bpm
    bar = beat * 4
    prog = ['Am7', 'Fmaj7', 'Cmaj7', 'G', 'Am7', 'Fmaj7', 'Dm7', 'Esus4']
    length = int(bar * len(prog) * SR)
    out = np.zeros((length + 5 * SR, 2))
    out[:length] += waves(length / SR, 8.0, 0.28)
    melody = [[76, None, None, 74, 72, None, None, None], [72, None, None, 71, 69, None, None, None],
              [67, None, 72, None, 76, None, None, None], [74, None, None, None, None, None, None, None],
              [76, None, None, 79, 77, None, 76, None], [72, None, None, None, 69, None, None, None],
              [74, None, 72, None, 69, None, 72, None], [71, None, None, None, None, None, None, None]]
    for i, name in enumerate(prog):
        t0 = i * bar
        notes, root = chord(name, 3)
        mix_at(out, pan(pad([midi(n + 12) for n in notes[:3]], bar + 1.2, 1.8, 1.8, 800), 0), max(0, t0 - 0.4), 0.06)
        for j, m in enumerate([notes[0], notes[2], notes[1] + 12, notes[2]]):
            mix_at(out, pan(epiano(midi(m), 2.2, 0.6), -0.3), t0 + j * beat, 0.13)
        mix_at(out, pan(bass(midi(root - 12), bar, 0.3), 0), t0)
        for j, m in enumerate(melody[i]):
            if m is not None:
                mix_at(out, pan(epiano(midi(m + 12), 2.4, 0.8), 0.3), t0 + j * beat / 2, 0.16)
    out = reverb(out, 0.35, 3.0, 1.1, 4500)
    return loopify(out, length)


BGM = {'town': bgm_town, 'sea': bgm_sea, 'market': bgm_market, 'truck': bgm_truck, 'title': bgm_title}


def main(names):
    for name, fn in SFX.items():
        if names and name not in names:
            continue
        save('sfx', name, fn())
    for name, fn in BGM.items():
        if names and name not in names:
            continue
        x = fn()
        save('bgm', name, norm(x, 0.8), quality=3)


if __name__ == '__main__':
    main(sys.argv[1:])
