"""SD 시트를 칸마다 잘라 배경을 지우고 PNG로 저장한다. 바탕이 단색(#EDEDED 등)이든 체크무늬가 그림에 박혔든 같은 방법으로 지운다.

사용법: python3 scripts/sd_cells.py <시트.png> <행> <열> <저장 폴더> <이름1,이름2,...> [--hair=0.45]
- 이름은 왼쪽 위부터 줄 순서. 빈칸으로 둘 칸은 이름을 - 로
- 바탕: 칸 가장자리와 이어진 밝은 무채색(체크무늬의 흰색·회색 칸 포함)
- 그림 둘레 흰 빛: 바탕에서 몇 픽셀 안쪽까지 하얗고 색이 없는 점을 벗긴다 (살색·옷 색은 남는다)
- --hair=비율: 칸 위쪽 이 비율까지(머리 쪽)는 가닥 사이에 갇힌 흰 빛도 바탕 가까우면 지운다. 0 이면 끈다
- 덩어리는 가장 큰 것과, 그 15% 넘는 것(손잡은 아이 등)만 남긴다. 강조 표시(! 한숨 같은 작은 조각)는 빠진다
- 크기는 바꾸지 않는다. 같은 시트의 칸은 같은 비율이다
필요: pillow, numpy, scipy
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

PAD = 14


def cutout(rgb: np.ndarray, hair: float) -> np.ndarray:
    a = rgb.astype(int)
    mx, mn = a.max(2), a.min(2)
    bgish = ((mx - mn) < 30) & (mn > 150)
    lab, _ = ndimage.label(bgish)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(edge))

    # 바탕에 둘러싸인 작은 바탕 조각 (팔과 몸 사이 등)
    holes = bgish & ~bg
    lab2, n2 = ndimage.label(holes)
    if n2:
        sizes = ndimage.sum(holes, lab2, range(1, n2 + 1))
        for i, s in enumerate(sizes, 1):
            if s < 1500:
                region = lab2 == i
                ring = ndimage.binary_dilation(region, iterations=2) & ~region
                if bg[ring].mean() > 0.3:
                    bg |= region

    # 둘레 흰 빛
    glow = ((mx - mn) < 46) & (mn > 172)
    for _ in range(12):
        bg |= ndimage.binary_dilation(bg) & glow
    if hair > 0:
        near = ndimage.distance_transform_edt(~bg) < 14
        white = ((mx - mn) < 40) & (mn > 170)
        zone = np.zeros_like(bg)
        zone[: int(len(bg) * hair)] = True
        bg |= near & white & zone

    fg = ndimage.binary_opening(~bg, iterations=1)
    lab3, n3 = ndimage.label(fg)
    if n3 == 0:
        return np.zeros(a.shape[:2])
    sizes = ndimage.sum(fg, lab3, range(1, n3 + 1))
    big = sizes.max()
    fg = np.isin(lab3, [i + 1 for i, s in enumerate(sizes) if s >= big * 0.15])
    alpha = ndimage.gaussian_filter(fg.astype(float), 0.7)
    return np.clip((alpha - 0.15) / 0.7, 0, 1)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    opts = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--'))
    src, rows, cols, out_dir, names = args[0], int(args[1]), int(args[2]), Path(args[3]), args[4].split(',')
    hair = float(opts.get('hair', 0.45))
    sheet = np.array(Image.open(src).convert('RGB'))
    h, w = sheet.shape[:2]
    out_dir.mkdir(parents=True, exist_ok=True)
    for i, name in enumerate(names):
        if name == '-':
            continue
        r, c = divmod(i, cols)
        x0, x1 = int(w * c / cols), int(w * (c + 1) / cols)
        y0, y1 = int(h * r / rows), int(h * (r + 1) / rows)
        bx0, by0, bx1, by1 = max(0, x0 - PAD), max(0, y0 - PAD), min(w, x1 + PAD), min(h, y1 + PAD)
        rgb = sheet[by0:by1, bx0:bx1]
        alpha = cutout(rgb, hair)
        ys, xs = np.nonzero(alpha > 0.03)
        if len(xs) == 0:
            print(f'{name}: 비었다')
            continue
        im = Image.fromarray(np.dstack([rgb, (alpha * 255).astype(np.uint8)]))
        im = im.crop((max(0, xs.min() - 3), max(0, ys.min() - 3), xs.max() + 4, ys.max() + 4))
        path = out_dir / f'{name}.png'
        im.save(path, optimize=True)
        print(f'{name}: {im.width}x{im.height}')


if __name__ == '__main__':
    main()
