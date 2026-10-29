"""걷는 마을 지도 꼭대기 위로 이어 붙일 하늘 그림을 만든다.

지도 맨 위 오른쪽의 진짜 하늘 조각(구름·별)을 잘라 좌우로 뒤집어 가며 이어 붙이고,
위로 갈수록 크게 키워 쌓는다(머리 위 구름이 더 크게 보이는 원근). 이음매는 겹쳐서 녹인다.
아래 끝은 지도 위 끝과 겹치는 띠(점점 투명)라 지도와 닿는 곳에 선이 남지 않는다.

    python3 scripts/town_sky.py            # godot/art/backgrounds/town-map-<시간>-sky.webp 넷
    python3 scripts/town_sky.py --preview <png>   # 지도 위에 붙여 본 모습

높이는 ArtStage.SkyCap / SkyFeather (논리 좌표 170 / 26) 에 맞춘다.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
BG = ROOT / 'godot' / 'art' / 'backgrounds'
CAP, FEATHER = 170, 26          # 논리 좌표 (지도 너비 450)

# 지도마다 나무·산·달이 없는 하늘 조각 (원본 픽셀: x0, x1, y0, y1)
PATCH = {
    'morning': (150, 941, 0, 44),
    'day': (150, 941, 0, 44),
    'dusk': (160, 941, 0, 46),
    'night': (130, 800, 0, 42),
}
# 조각 안에서 하늘색으로 지울 곳 (원본 픽셀 x0, x1, y0, y1). 가는 새털구름은 크게 키우면 찢어진 모양이 된다
ERASE = {
    'day': [(690, 800, 0, 34), (850, 941, 0, 44)],
}
# 노을빛을 뺄 하늘 (저녁): 구름 사이 주황·분홍 빛을 이 연보라로 바꾼다. 구름은 그대로. 지도와 닿는 아래는 원래 색
CALM = {
    'dusk': (196, 182, 232),
}


def smooth(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def row_strip(patch, width, scale, flip0, shift):
    """하늘 조각을 scale 배로 키워 좌우로 뒤집어 가며 가로로 이어 붙인 띠"""
    ph, pw = patch.shape[:2]
    p = np.asarray(Image.fromarray(patch).resize((int(pw * scale), int(ph * scale)), Image.LANCZOS)).astype(float)
    pw2 = p.shape[1]
    over = max(8, pw2 // 6)
    out = np.zeros((p.shape[0], width + pw2 * 2, 3))
    wsum = np.zeros((1, out.shape[1], 1))
    x, k = 0, 0
    while x < out.shape[1]:
        tile = p[:, ::-1] if (k + flip0) % 2 else p
        w = np.ones(pw2)
        w[:over] = smooth(np.linspace(0, 1, over))
        w[-over:] = smooth(np.linspace(1, 0, over))
        x1 = min(out.shape[1], x + pw2)
        out[:, x:x1] += tile[:, :x1 - x] * w[None, :x1 - x, None]
        wsum[:, x:x1] += w[None, :x1 - x, None]
        x += pw2 - over
        k += 1
    out /= np.maximum(wsum, 1e-6)
    s = shift % pw2
    return out[:, s:s + width]


def build(name):
    im = np.asarray(Image.open(BG / f'town-map-{name}.webp').convert('RGB')).astype(float)
    H, W = im.shape[:2]
    k = W / 450
    cap, feat = int(CAP * k), int(FEATHER * k)
    x0, x1, y0, y1 = PATCH[name]
    patch = im[y0:y1, x0:x1].copy()
    for ex0, ex1, ey0, ey1 in ERASE.get(name, []):
        # 그 줄의 맑은 하늘(흰 구름이 아닌 곳) 색으로 채우고 가장자리는 녹인다
        for y in range(ey0, ey1):
            row = patch[y - y0]
            clear = row[row.min(1) < 200]
            sky_c = np.median(clear, 0) if len(clear) else row.mean(0)
            xs = np.arange(ex0 - x0, ex1 - x0)
            edge = np.minimum(xs - xs[0], xs[-1] - xs) / 12.0
            edge = np.minimum(edge, (ey1 - y) / 6.0)
            m = smooth(edge)[:, None]
            row[xs] = row[xs] * (1 - m) + sky_c * m
    # 조각 안 위아래 밝기 차이(위는 짙고 아래는 옅다)를 펴 둔다. 그대로 쌓으면 띠마다 가로 줄이 생긴다
    rowmean = patch.mean(1, keepdims=True)
    patch = np.clip(patch - rowmean + patch.mean((0, 1), keepdims=True), 0, 255).astype(np.uint8)
    # 하늘 색 흐름은 따로: 아래는 지도 맨 위 줄 색, 위로 갈수록 조각 맨 위 줄 색보다 조금 더 짙게
    c_bottom = im[0:3, x0:x1].mean((0, 1))
    c_top = rowmean[0, 0] * 0.82
    total = cap + feat
    sky = np.zeros((total, W, 3))
    wsum = np.zeros((total, 1, 1))
    # 아래(지도 쪽)부터 위로 띠를 쌓는다. 위로 갈수록 크게
    bottom = total
    layer = 0
    rng = np.random.default_rng(3)
    while bottom > 0:
        scale = 1.0 + 0.45 * layer
        strip = row_strip(patch, W, scale, layer % 2, int(rng.integers(0, 2000)))
        sh = strip.shape[0]
        over = max(6, sh // 2)
        top = bottom - sh
        w = np.ones(sh)
        w[:over] = smooth(np.linspace(0, 1, over))
        if layer > 0:
            w[-over:] = smooth(np.linspace(1, 0, over))
        a, b = max(0, top), bottom
        sky[a:b] += strip[a - top:] * w[a - top:, None, None]
        wsum[a:b] += w[a - top:, None, None]
        bottom = top + over
        layer += 1
    sky /= np.maximum(wsum, 1e-6)
    t = smooth(np.linspace(1, 0, total) ** 0.8)[:, None, None]
    grad = c_bottom * (1 - t) + c_top * t
    sky += grad - patch.mean((0, 1))
    if name in CALM:
        lav = np.array(CALM[name], float)
        warm = smooth((sky[..., 0] - sky[..., 2] + 5) / 40.0)[..., None]   # 빨강이 파랑보다 센 곳 = 노을빛
        lum = sky.mean(2, keepdims=True) / lav.mean()
        ramp = smooth((1 - np.linspace(0, 1, total) - FEATHER / (CAP + FEATHER)) / 0.35)[:, None, None]
        sky = sky * (1 - warm * ramp) + lav * np.clip(lum, 0.85, 1.15) * warm * ramp
    # 맨 위 끝(보통 HUD 밑에 숨는다)은 HUD 남색으로 녹인다. 의뢰 띠가 길어 더 내려 보여도 끝 선이 안 보이게
    deep = np.array([0x0f, 0x1a, 0x2b], float)
    m = smooth(np.linspace(0, 1, int(total * 0.14)))[:, None, None]
    sky[:len(m)] = deep * (1 - m) + sky[:len(m)] * m
    # 아래 겹치는 띠: 지도 맨 위와 같은 그림을 먼저 깔고 위에서부터 하늘로 녹인다
    alpha = np.ones(total)
    alpha[cap:] = 1 - smooth(np.linspace(0, 1, feat))
    rgba = np.concatenate([np.clip(sky, 0, 255), alpha[:, None, None].repeat(W, 1) * 255], 2).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA'), im


def main():
    preview = sys.argv[2] if len(sys.argv) > 2 and sys.argv[1] == '--preview' else None
    shots = []
    for name in PATCH:
        sky, im = build(name)
        if preview:
            W = im.shape[1]
            cap = sky.height - int(FEATHER * W / 450)
            base = Image.new('RGBA', (W, cap + 700))
            base.paste(Image.fromarray(im[:700].astype(np.uint8)).convert('RGBA'), (0, cap))
            base.alpha_composite(sky, (0, 0))
            shots.append(base.convert('RGB'))
        else:
            out = BG / f'town-map-{name}-sky.webp'
            sky.save(out, 'WEBP', quality=88, method=6)
            print(out.name, sky.size, out.stat().st_size // 1024, 'KB')
    if preview:
        w = sum(s.width for s in shots)
        c = Image.new('RGB', (w, shots[0].height))
        x = 0
        for s in shots:
            c.paste(s, (x, 0))
            x += s.width
        c.resize((w // 2, c.height // 2)).save(preview)


if __name__ == '__main__':
    main()
