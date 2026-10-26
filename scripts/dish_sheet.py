"""요리 시트(배경 #EDEDED)를 그림마다 잘라 godot/art/dishes 에 PNG로 저장한다.

사용법: python3 scripts/dish_sheet.py <시트.png> <이름1,이름2,...> [긴 변 최대 px]
- 줄마다 그림 수가 달라도 된다 (위 3, 아래 2). 덩어리를 위 줄부터, 줄 안에서는 왼쪽부터 이름을 붙인다
- 배경 지우기는 art.py 와 같다 (가장자리와 이어진 밝은 무채색). 김은 대부분 같이 지워진다
- 그림끼리 닿아 덩어리가 모자라면 가장 넓은 덩어리를 알파가 가장 적은 세로줄에서 가른다
- 그림마다 가장 큰 덩어리만 남기고(김 같은 작은 조각은 지운다), 긴 변을 최대 px(기본 512)로 줄인다
필요: pillow, numpy, scipy
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from art import cutout, trim  # noqa: E402
from sd_sheet import clear_pockets  # noqa: E402

OUT = Path(__file__).resolve().parent.parent / 'godot' / 'art' / 'dishes'


def blobs(sheet: Image.Image, min_area=8000):
    a = np.array(sheet.getchannel('A')) > 40
    lab, _ = ndimage.label(ndimage.binary_dilation(a, iterations=3))
    found = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        area = int((lab[sl] == i).sum())
        if area >= min_area:
            found.append((i, sl))
    return lab, order(found)


def split_widest(sheet: Image.Image, lab, found):
    """가장 넓은 덩어리를 알파가 가장 적은 세로줄(가운데 60% 안)에서 둘로 가른다"""
    i, sl = max(found, key=lambda b: b[1][1].stop - b[1][1].start)
    a = (np.array(sheet.getchannel('A'))[sl] > 40) & (lab[sl] == i)
    cols = a.sum(axis=0)
    w = len(cols)
    lo, hi = int(w * 0.2), int(w * 0.8)
    cut = lo + int(np.argmin(cols[lo:hi]))
    nxt = lab.max() + 1
    region = lab[sl]
    right = np.zeros_like(region, dtype=bool)
    right[:, cut:] = True
    region[(region == i) & right] = nxt
    lab[sl] = region
    left_sl = (sl[0], slice(sl[1].start, sl[1].start + cut))
    right_sl = (sl[0], slice(sl[1].start + cut, sl[1].stop))
    rest = [b for b in found if b[0] != i]
    return rest + [(i, left_sl), (nxt, right_sl)]


def order(found):
    found = sorted(found, key=lambda b: (b[1][0].start + b[1][0].stop) / 2)
    rows = []
    for b in found:
        cy = (b[1][0].start + b[1][0].stop) / 2
        if rows and abs(cy - rows[-1][0]) < (b[1][0].stop - b[1][0].start) * 0.5:
            rows[-1][1].append(b)
        else:
            rows.append([cy, [b]])
    out = []
    for _, row in rows:
        out += sorted(row, key=lambda b: b[1][1].start)
    return out


def main():
    src, names = sys.argv[1], sys.argv[2].split(',')
    max_side = int(sys.argv[3]) if len(sys.argv) > 3 else 512
    raw = Image.open(src)
    sheet = clear_pockets(cutout(raw), raw)
    lab, found = blobs(sheet)
    while 0 < len(found) < len(names):
        found = order(split_widest(sheet, lab, found))
    if len(found) != len(names):
        sys.exit(f'그림 {len(found)}개, 이름 {len(names)}개')
    OUT.mkdir(parents=True, exist_ok=True)
    arr = np.array(sheet)
    for name, (i, sl) in zip(names, found):
        piece = arr[sl].copy()
        piece[..., 3] = np.where(lab[sl] == i, piece[..., 3], 0)
        # 김 같은 작은 떨어진 조각은 지운다 (가장 큰 조각의 3% 미만)
        parts, n = ndimage.label(piece[..., 3] > 40)
        if n > 1:
            sizes = ndimage.sum(np.ones_like(parts), parts, range(1, n + 1))
            small = [k + 1 for k in range(n) if sizes[k] < sizes.max() * 0.03]
            piece[..., 3] = np.where(np.isin(parts, small), 0, piece[..., 3])
        im = trim(Image.fromarray(piece), pad=4)
        im.thumbnail((max_side, max_side), Image.LANCZOS)
        p = OUT / f'{name}.png'
        im.save(p, optimize=True)
        print(f'{name:20s} {im.size[0]}x{im.size[1]} {p.stat().st_size // 1024}KB')


if __name__ == '__main__':
    main()
