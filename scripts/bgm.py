# 받은 배경음(무료 곡)을 게임용 반복 OGG 로 바꾼다.
# 사용법: python3 scripts/bgm.py <원본 mp3/ogg/wav> <이름> [--start=초] [--length=초]
#   이름: title, town, sea, market, truck (godot/audio/bgm/<이름>.ogg 를 덮어쓴다)
#   --start, --length: 곡의 일부만 쓸 때 (기본은 처음부터 끝까지)
# 하는 일: 44.1kHz 스테레오로 읽기 → 음량 맞추기(합성 효과음과 어울리게) → 끝 3초를 처음 3초에 겹쳐 자연스럽게 반복 → Vorbis
# 필요: ffmpeg, numpy, scipy. 원본 곡은 레포에 넣지 않는다. 출처와 라이선스는 docs/GODOT_PLAN.md 소리 표에 적는다.

import os
import subprocess
import sys
import tempfile

import numpy as np
from scipy.io import wavfile

SR = 44100
XFADE = 3.0
TARGET_RMS_DB = -18.0
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'godot', 'audio', 'bgm')


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    opts = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--') and '=' in a)
    if len(args) != 2:
        print(__doc__ or '사용법: python3 scripts/bgm.py <원본> <이름>')
        sys.exit(1)
    src, name = args
    with tempfile.TemporaryDirectory() as tmp:
        wav = os.path.join(tmp, 'in.wav')
        cmd = ['ffmpeg', '-y', '-loglevel', 'error']
        if 'start' in opts:
            cmd += ['-ss', opts['start']]
        cmd += ['-i', src]
        if 'length' in opts:
            cmd += ['-t', opts['length']]
        cmd += ['-ac', '2', '-ar', str(SR), wav]
        subprocess.run(cmd, check=True)
        _, x = wavfile.read(wav)
        x = x.astype(np.float64) / 32768.0

        # 앞뒤 무음을 잘라 반복될 때 빈 틈이 없게 (-50dB 아래)
        loud = np.where(np.max(np.abs(x), axis=1) > 10 ** (-50 / 20))[0]
        if len(loud):
            x = x[loud[0]:loud[-1] + 1]

        # 끝 XFADE 초를 처음에 같은 힘으로 겹친다 → 반복될 때 툭 끊기지 않는다
        n = int(XFADE * SR)
        if len(x) > 4 * n:
            body = x[:-n].copy()
            tail = x[-n:]
            k = np.linspace(0, np.pi / 2, n)[:, None]
            body[:n] = body[:n] * np.sin(k) + tail * np.cos(k)
            x = body

        rms = np.sqrt(np.mean(x ** 2)) + 1e-9
        x *= 10 ** (TARGET_RMS_DB / 20) / rms
        peak = np.max(np.abs(x))
        if peak > 0.95:
            x *= 0.95 / peak

        out_wav = os.path.join(tmp, 'out.wav')
        wavfile.write(out_wav, SR, (x * 32767).astype(np.int16))
        os.makedirs(OUT, exist_ok=True)
        out = os.path.join(OUT, name + '.ogg')
        subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', out_wav, '-c:a', 'libvorbis', '-q:a', '3', out], check=True)
        print('저장', os.path.relpath(out), f'{len(x) / SR:.1f}초', f'{os.path.getsize(out) // 1024}KB')


if __name__ == '__main__':
    main()
