"""낚싯대 몸통 시트 → blank-N.png (곧게 펴고, 손잡이 자르고, 몸통만 세로로 늘인다. 고리는 둥근 채로)

사용법: python3 scripts/blank_sheet.py <시트.png> godot/art/gear   (같은 폴더에 blanks.json: gearlook.json blanks 에 옮긴다)
CUT 은 시트에서 코르크 손잡이가 시작하는 줄이다. 시트마다 눈으로 보고 고친다
"""
import json, sys
import numpy as np
from PIL import Image
from scipy import ndimage
SRC, OUT = sys.argv[1], sys.argv[2]
CUT = 650          # 시트에서 이 줄 아래(코르크 손잡이)는 버린다
RATIO = 40.0       # 게임 대의 길이 / 밑동 굵기
a = np.array(Image.open(SRC).convert('RGBA'))
al = a[..., 3] > 60
lab, n = ndimage.label(al)
objs = [s for s in ndimage.find_objects(lab) if (s[0].stop - s[0].start) > 500]
objs.sort(key=lambda s: s[1].start)
info = []
for k, s in enumerate(objs, 1):
    y0, y1 = s[0].start, min(s[0].stop, CUT)
    x0, x1 = max(0, s[1].start - 30), min(a.shape[1], s[1].stop + 30)
    sub = a[y0:y1, x0:x1].copy()
    m = sub[..., 3] > 60
    h = sub.shape[0]
    # 줄마다 가장 왼쪽 불투명 픽셀 = 몸통 왼쪽 끝 (고리는 오른쪽에만 붙는다)
    left = np.array([np.argmax(r) if r.any() else -1 for r in m], float)
    ok = left >= 0
    ys = np.arange(h)
    # 몸통 굵기: 고리 없는 줄의 너비로 직선 맞춤
    width = np.array([(np.nonzero(r)[0].max() - np.nonzero(r)[0].min() + 1) if r.any() else 0 for r in m], float)
    base = ndimage.median_filter(width, 31)
    plain = ok & (width <= base * 1.15)
    pw = np.polyfit(ys[plain], width[plain], 1)
    bw = np.clip(np.polyval(pw, ys), 3, None)
    pl = np.polyfit(ys[plain], left[plain], 2)
    lf = np.polyval(pl, ys)
    cx = lf + bw / 2                       # 몸통 가운데
    W = int(bw.max() * 2.6)                # 몸통 + 오른쪽 고리 자리
    C = int(bw.max() * 0.6)                # 그림 안 몸통 가운데 x
    body = np.zeros((h, W, 4), np.uint8); ring = np.zeros((h, W, 4), np.uint8)
    for y in range(h):
        sh = int(round(cx[y])) - C
        row = np.zeros((W, 4), np.uint8)
        lo, hi = max(0, sh), min(sub.shape[1], sh + W)
        row[lo - sh:hi - sh] = sub[y, lo:hi]
        xs = np.arange(W)
        inb = np.abs(xs - C) <= bw[y] / 2 + 1.5
        body[y] = np.where(inb[:, None], row, 0)
        ring[y] = np.where(inb[:, None], 0, row)
    # 고리 덩어리
    rm = ring[..., 3] > 60
    rl, rn = ndimage.label(ndimage.binary_dilation(rm, iterations=2))
    rings = []
    for i, sl in enumerate(ndimage.find_objects(rl), 1):
        if (rl[sl] == i).sum() < 40: continue
        rings.append((sl, i))
    f = RATIO * bw[-1] / h
    H = int(round(h * f))
    big = np.array(Image.fromarray(body).resize((W, H), Image.LANCZOS))
    guides = []
    for sl, i in sorted(rings, key=lambda r: r[0][0].start):
        piece = ring[sl].copy(); piece[..., 3] = np.where(rl[sl] == i, piece[..., 3], 0)
        cy = (sl[0].start + sl[0].stop) / 2
        ny = int(round(cy * f - (sl[0].stop - sl[0].start) / 2))
        im = Image.fromarray(big); im.alpha_composite(Image.fromarray(piece), (sl[1].start, max(0, ny)))
        big = np.array(im)
        # 고리 눈 가운데: 늘인 그림에서 위치 (밑동 0 끝 1, 그림 너비 비율)
        gy = max(0, ny) + (sl[0].stop - sl[0].start) / 2
        gx = (sl[1].start + sl[1].stop) / 2
        if gx / W > C / W + 0.08:                      # 몸통 왼쪽에 떨어진 부스러기는 고리가 아니다
            guides.append([round(1 - gy / H, 4), round(gx / W, 3)])
    out = f'{OUT}/blank-{k}.png'
    Image.fromarray(big).save(out, optimize=True)
    info.append({'center': round(C / W, 3), 'base': round(float(bw[-1]), 1), 'guides': guides})
    print(k, Image.fromarray(big).size, '늘임 %.2f' % f, '고리', len(guides), info[-1])
json.dump(info, open(f'{OUT}/blanks.json', 'w'), ensure_ascii=False)
