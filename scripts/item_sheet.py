"""장비 시트(2줄 × 5칸, 10단계)를 칸마다 잘라 godot/art/gear/<종류>-<단계>.png 로 저장한다.

사용법: python3 scripts/item_sheet.py <시트.png> <종류> [긴 변 최대 px]
- 단계는 왼쪽 위부터 1~10 (위 줄 1~5, 아래 줄 6~10)
- 바탕은 sd_cells.py 와 같이 지운다 (단색이든 그림에 박힌 체크무늬든)
- 바탕이 이미 투명한 시트(낚싯대 등)는 덩어리로 나눈다. 칸을 넘는 긴 그림도 된다
필요: pillow, numpy, scipy
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from sd_cells import cutout  # noqa: E402

OUT = Path(__file__).resolve().parent.parent / 'godot' / 'art' / 'gear'
PAD = 14


def save(im: Image.Image, kind: str, n: int, max_side: int):
    a = np.array(im)[..., 3]
    ys, xs = np.nonzero(a > 8)
    im = im.crop((max(0, xs.min() - 3), max(0, ys.min() - 3), xs.max() + 4, ys.max() + 4))
    im.thumbnail((max_side, max_side), Image.LANCZOS)
    p = OUT / f'{kind}-{n}.png'
    im.save(p, optimize=True)
    print(f'{p.name:16s} {im.width}x{im.height} {p.stat().st_size // 1024}KB')


def from_grid(src: Image.Image, kind: str, max_side: int):
    sheet = np.array(src.convert('RGB'))
    h, w = sheet.shape[:2]
    for i in range(10):
        r, c = divmod(i, 5)
        x0, x1 = int(w * c / 5), int(w * (c + 1) / 5)
        y0, y1 = int(h * r / 2), int(h * (r + 1) / 2)
        bx0, by0, bx1, by1 = max(0, x0 - PAD), max(0, y0 - PAD), min(w, x1 + PAD), min(h, y1 + PAD)
        rgb = sheet[by0:by1, bx0:bx1]
        alpha = cutout(rgb, 0)
        save(Image.fromarray(np.dstack([rgb, (alpha * 255).astype(np.uint8)])), kind, i + 1, max_side)


def from_alpha(src: Image.Image, kind: str, max_side: int):
    a = np.array(src.convert('RGBA'))
    solid = a[..., 3] > 200
    lab, n = ndimage.label(ndimage.binary_dilation(solid, iterations=2))
    objs = [(i + 1, sl) for i, sl in enumerate(ndimage.find_objects(lab)) if (lab[sl] == i + 1).sum() > 3000]
    if len(objs) != 10:
        sys.exit(f'덩어리 {len(objs)}개')
    h = a.shape[0]
    # 위 줄부터, 줄 안에서는 왼쪽부터 (덩어리 중심이 위 절반이면 위 줄)
    objs.sort(key=lambda o: (((o[1][0].start + o[1][0].stop) / 2) > h / 2, o[1][1].start))
    for k, (i, sl) in enumerate(objs):
        piece = a[sl].copy()
        keep = ndimage.binary_dilation(lab[sl] == i, iterations=1)
        # 둘레에 번진 빛(반투명)은 지운다
        piece[..., 3] = np.where(keep & (piece[..., 3] > 200), 255, 0).astype(np.uint8)
        save(Image.fromarray(piece), kind, k + 1, max_side)


def main():
    src, kind = sys.argv[1], sys.argv[2]
    max_side = int(sys.argv[3]) if len(sys.argv) > 3 else 256
    OUT.mkdir(parents=True, exist_ok=True)
    im = Image.open(src)
    if im.mode == 'RGBA' and (np.array(im)[..., 3] < 10).mean() > 0.3:
        from_alpha(im, kind, max_side)
    else:
        from_grid(im, kind, max_side)


if __name__ == '__main__':
    main()
