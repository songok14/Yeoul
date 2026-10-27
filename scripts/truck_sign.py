"""트럭 화면 배경(truckstop.png) 간판에 "여울호"를 칠한다.

원본 그림 간판에는 AI가 뭉갠 글씨가 흐리게 남아 있다. 그 자리를 나무판 결이 살아 있게 다시 채우고
(밝기는 둘레 깨끗한 판에서, 결은 같은 줄 오른쪽 판에서 가져온다) 그 위에 원근과 등불 빛을 따라 글씨를 칠한다.

    python3 scripts/truck_sign.py            # godot/art/backgrounds/truckstop.png 를 고친다
    python3 scripts/truck_sign.py --preview out.png

간판 밖(칠판 등)은 지금 파일 그대로 두고 간판 판만 원본(커밋 4c27610)에서 다시 만든다.
"""
import subprocess
import sys
from io import BytesIO

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy.ndimage import gaussian_filter, map_coordinates

ROOT = __file__.rsplit("/scripts/", 1)[0]
TARGET = f"{ROOT}/godot/art/backgrounds/truckstop.png"
ORIGINAL = "4c27610:godot/art/backgrounds/truckstop.png"
FONT = f"{ROOT}/godot/fonts/GowunBatang-Bold.ttf"

# 간판 판 안쪽 네 귀퉁이 (900×1600 그림 좌표): 왼위, 오위, 오아래, 왼아래
QUAD = [(405.0, 453.0), (747.0, 380.0), (747.0, 505.0), (405.0, 560.0)]
# 펴 놓은 판 크기 (가로 342px 판을 3배로)
RW, RH = 1026, 330
# 펴 놓은 판에서 다시 채울 곳 (뭉갠 글씨): 위 물고기 오른쪽부터 오른쪽 결 띠 앞까지
FILL = (395, 14, 962, 264)
FISH_X = 420  # 이보다 왼쪽의 어두운 칠은 물고기 테두리
# 결을 가져올 띠 (같은 줄에서 오른쪽 깨끗한 판)
SRC_X = (968, 1012)
TEXT = "여울호"
STROKE = 3  # 글씨 굵게 (펴 놓은 판 px)
INK = np.array([52, 92, 160], float)  # 아래 물결과 같은 푸른 페인트


def homography(src, dst):
    a = []
    for (x, y), (u, v) in zip(src, dst):
        a.append([x, y, 1, 0, 0, 0, -u * x, -u * y, -u])
        a.append([0, 0, 0, x, y, 1, -v * x, -v * y, -v])
    _, _, vt = np.linalg.svd(np.array(a))
    return vt[-1].reshape(3, 3) / vt[-1][-1]


def warp(img, h, w, hgt):
    """img(H×W×C)를 h(출력 좌표 → 입력 좌표)로 w×hgt 크기로 뽑는다."""
    ys, xs = np.mgrid[0:hgt, 0:w].astype(float)
    p = h @ np.stack([xs.ravel(), ys.ravel(), np.ones(xs.size)])
    sx, sy = p[0] / p[2], p[1] / p[2]
    out = np.stack([map_coordinates(img[..., c], [sy, sx], order=1, mode="nearest")
                    for c in range(img.shape[2])], -1)
    return out.reshape(hgt, w, img.shape[2])


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def membrane(img, known, step=2):
    """known 자리 값(잔결은 흐려서)을 두고 나머지를 라플라스 방정식으로 채운다. 계산은 1/step 크기로"""
    from scipy.sparse import lil_matrix
    from scipy.sparse.linalg import spsolve
    k = known.astype(float)
    soft = np.stack([gaussian_filter(img[..., c] * k, 4) for c in range(3)], -1)
    soft /= np.maximum(gaussian_filter(k, 4), 1e-6)[..., None]
    h, w = known.shape
    small = soft[::step, ::step]
    kn = known[::step, ::step]
    sh, sw = kn.shape
    idx = -np.ones((sh, sw), int)
    unknown = np.argwhere(~kn)
    idx[~kn] = np.arange(len(unknown))
    a = lil_matrix((len(unknown), len(unknown)))
    b = np.zeros((len(unknown), 3))
    for n, (y, x) in enumerate(unknown):
        cnt = 0
        for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if not (0 <= yy < sh and 0 <= xx < sw):
                continue  # 판 가장자리는 기울기 0
            cnt += 1
            if kn[yy, xx]:
                b[n] += small[yy, xx]
            else:
                a[n, idx[yy, xx]] = -1
        a[n, n] = cnt
    sol = spsolve(a.tocsr(), b)
    out = small.copy()
    out[~kn] = sol
    big = np.stack([np.asarray(Image.fromarray(out[..., c].astype(np.float32), "F").resize((w, h), Image.BILINEAR))
                    for c in range(3)], -1)
    # 알려진 자리는 원래 흐린 값 그대로
    return np.where(known[..., None], soft, gaussian_filter(big, (2, 2, 0)))


def main():
    preview = sys.argv[sys.argv.index("--preview") + 1] if "--preview" in sys.argv else None
    raw = subprocess.run(["git", "-C", ROOT, "show", ORIGINAL], capture_output=True, check=True).stdout
    orig = np.asarray(Image.open(BytesIO(raw)).convert("RGBA")).astype(float)
    cur = np.asarray(Image.open(TARGET).convert("RGBA")).astype(float)

    rect = [(0, 0), (RW, 0), (RW, RH), (0, RH)]
    to_img = homography(rect, QUAD)  # 펴 놓은 판 → 그림
    board = warp(orig[..., :3], to_img, RW, RH)

    # 다시 채울 마스크 (가장자리는 부드럽게)
    ys, xs = np.mgrid[0:RH, 0:RW].astype(float)
    x0, y0, x1, y1 = FILL
    m = (smoothstep(x0 - 20, x0 + 20, xs) * (1 - smoothstep(x1 - 10, x1 + 6, xs))
         * (1 - smoothstep(y1 - 10, y1 + 4, ys)))
    if y0 > 0:
        m *= smoothstep(y0 - 20, y0 + 20, ys)
    # 물고기 그림(검은 테두리·푸른 몸)과 물결은 지우지 않는다
    from scipy.ndimage import binary_dilation
    lum0 = board.mean(-1)
    blue = board[..., 2] > board[..., 0] + 8
    protect = blue | ((lum0 < 120) & (xs < FISH_X))
    protect = binary_dilation(protect, iterations=4)
    m *= 1 - gaussian_filter(protect.astype(float), 1.5)

    # 밝기: 깨끗한 판 색을 알려진 값으로 두고 나머지를 막처럼 매끈하게 잇는다 (라플라스 방정식).
    # 경계에서 둘레 판과 같은 값이 되어 네모난 자국이 남지 않는다. 물고기·물결 자리는 모르는 값으로 둔다
    lumi = board.mean(-1)
    keep = ((m < 0.02) & (lumi > 150) & (board[..., 2] < board[..., 0])
            & (xs < 1014) & (ys > 4))
    low = membrane(board, keep)

    # 결: 오른쪽 띠의 잔무늬(고주파)를 같은 줄에 조각조각 이어 붙인다
    hp = board - np.stack([gaussian_filter(board[..., c], 6) for c in range(3)], -1)
    sx0, sx1 = SRC_X
    # 못 자국 같은 큰 무늬는 반복되면 눈에 띄니 잔결만 남긴다
    strip = np.tanh(hp[:, sx0:sx1] / 10) * 10
    sw = strip.shape[1]
    rng = np.random.default_rng(7)
    detail = np.zeros_like(board)
    acc = np.zeros((RH, RW, 1))
    step, fade = 40, 14
    for px in range(0, RW, step):
        piece = strip if rng.random() < 0.5 else strip[:, ::-1]
        dy = int(rng.integers(-2, 3))
        piece = np.roll(piece, dy, axis=0)
        width = min(step + fade, RW - px, sw)
        w = np.ones(width)
        w[:fade] = np.linspace(0.05, 1, fade)[: min(fade, width)]
        w[-fade:] = np.minimum(w[-fade:], np.linspace(1, 0.05, fade)[-min(fade, width):])
        detail[:, px:px + width] += piece[:, :width] * w[None, :, None]
        acc[:, px:px + width, 0] += w[None, :]
    detail /= np.maximum(acc, 1e-3)
    # 위쪽 띠(테두리 가까운 칠 벗겨진 곳)는 무늬가 반복돼 보이니 약하게
    detail *= (0.25 + 0.75 * smoothstep(20, 90, ys))[..., None]

    filled = low + detail * 1.1
    mm = m[..., None]
    board2 = board * (1 - mm) + filled * mm

    # 글씨: 고운바탕 굵게, 판 위에 칠한 페인트처럼 (결·빛을 따라간다)
    scale = 3
    layer = Image.new("L", (RW * scale, RH * scale), 0)
    font = ImageFont.truetype(FONT, 165 * scale)
    d = ImageDraw.Draw(layer)
    bx = d.textbbox((0, 0), TEXT, font=font)
    tw, th = bx[2] - bx[0], bx[3] - bx[1]
    cx, cy = 690 * scale, 126 * scale
    d.text((cx - tw / 2 - bx[0], cy - th / 2 - bx[1]), TEXT, font=font, fill=255)
    layer = layer.filter(ImageFilter.MaxFilter(2 * STROKE * scale + 1))
    layer = layer.filter(ImageFilter.GaussianBlur(scale * 0.8)).resize((RW, RH), Image.LANCZOS)
    a = np.asarray(layer).astype(float) / 255
    # 붓 자국: 가로로 긴 잡음으로 칠이 조금 얇은 곳을 만든다
    noise = gaussian_filter(rng.standard_normal((RH, RW)), (1.2, 6))
    noise /= noise.std()
    a = np.clip(a * (0.9 + 0.07 * noise), 0, 1)
    # 판 밝기(등불 빛)를 페인트에도 입히고 나무결을 비친다
    light = low.mean(-1, keepdims=True) / 225.0
    ink = INK[None, None, :] * np.clip(light, 0.75, 1.15) + detail * 0.6
    board3 = board2 * (1 - a[..., None] * 0.93) + ink * (a[..., None] * 0.93)

    # 그림에 다시 붙인다: 판 안쪽 픽셀만
    to_rect = np.linalg.inv(to_img)
    out = cur.copy()
    hh, ww = out.shape[:2]
    yy, xx = np.mgrid[0:hh, 0:ww].astype(float)
    p = to_rect @ np.stack([xx.ravel(), yy.ravel(), np.ones(xx.size)])
    rx, ry = (p[0] / p[2]).reshape(hh, ww), (p[1] / p[2]).reshape(hh, ww)
    inside = (rx >= 0) & (rx <= RW - 1) & (ry >= 0) & (ry <= RH - 1)
    for c in range(3):
        v = map_coordinates(board3[..., c], [ry[inside], rx[inside]], order=1)
        # 원본과 같은 바탕 위로 바뀐 부분만 얹는다 (판 테두리 근처는 원본 그대로)
        base = map_coordinates(board[..., c], [ry[inside], rx[inside]], order=1)
        ch = out[..., c]
        o = orig[..., c][inside]
        ch[inside] = np.where(np.abs(v - base) > 0.5, v, o)
    res = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")
    if preview:
        res.convert("RGB").crop((380, 330, 800, 580)).resize((1260, 750), Image.LANCZOS).save(preview)
    else:
        res.save(TARGET, optimize=True)
    print("ok", preview or TARGET)


if __name__ == "__main__":
    main()
