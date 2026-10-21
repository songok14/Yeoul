"""SD 시트(배경 #EDEDED)를 칸마다 잘라 유니티용 PNG로 저장한다.

사용법: python3 scripts/sd_sheet.py <시트.png> <행> <열> <저장 폴더> <이름1,이름2,...>
- 배경 지우기는 art.py 와 같다 (가장자리와 이어진 밝은 무채색)
- 머리카락 사이에 갇힌 배경 조각도 시트 배경색과 비교해 지운다
- 칸마다 가장 큰 덩어리만 남긴다. 강조 표시(! 같은 작은 선)는 빠진다
- 크기는 바꾸지 않는다. 같은 시트의 프레임은 같은 비율이라 애니메이션에 그대로 쓴다
- 유니티에서는 피벗을 아래 가운데로 둔다 (발이 같은 바닥선)
필요: pillow, numpy, scipy
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from art import cutout, trim  # noqa: E402


def clear_pockets(sheet: Image.Image, src: Image.Image, tol=6, min_px=20) -> Image.Image:
    """머리카락 사이처럼 갇힌 배경 조각을 지운다. 시트 배경색(모서리)과 거의 같은 무채색만"""
    rgb = np.array(src.convert('RGB')).astype(int)
    bg = np.median(np.concatenate([rgb[:8, :8].reshape(-1, 3), rgb[-8:, -8:].reshape(-1, 3)]), axis=0)
    mx, mn = rgb.max(axis=2), rgb.min(axis=2)
    near = (np.abs(rgb - bg).max(axis=2) <= tol) & (mx - mn <= 5)
    lab, n = ndimage.label(near)
    if n:
        sizes = ndimage.sum(near, lab, range(1, n + 1))
        keep = np.isin(lab, [i + 1 for i in range(n) if sizes[i] >= min_px])
        arr = np.array(sheet)
        a = np.where(keep, 0, arr[..., 3]).astype(np.uint8)
        arr[..., 3] = np.array(Image.fromarray(a).filter(ImageFilter.MinFilter(3)))
        return Image.fromarray(arr)
    return sheet


def cells(sheet: Image.Image, rows: int, cols: int, min_area=4000):
    """칸마다 가장 큰 덩어리 하나. 덩어리 중심이 들어간 칸으로 나눈다"""
    a = np.array(sheet.getchannel('A')) > 40
    lab, n = ndimage.label(ndimage.binary_dilation(a, iterations=4))
    h, w = a.shape
    best = {}
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        area = int((lab[sl] == i).sum())
        if area < min_area:
            continue
        cy = (sl[0].start + sl[0].stop) / 2
        cx = (sl[1].start + sl[1].stop) / 2
        key = (min(rows - 1, int(cy / h * rows)), min(cols - 1, int(cx / w * cols)))
        if key not in best or best[key][0] < area:
            best[key] = (area, i, sl)
    return lab, best


def main():
    src, rows, cols, out, names = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), Path(sys.argv[4]), sys.argv[5].split(',')
    if len(names) != rows * cols:
        sys.exit(f'이름 {len(names)}개, 칸 {rows * cols}개')
    raw = Image.open(src)
    sheet = clear_pockets(cutout(raw), raw)
    lab, best = cells(sheet, rows, cols)
    out.mkdir(parents=True, exist_ok=True)
    arr = np.array(sheet)
    for r in range(rows):
        for c in range(cols):
            name = names[r * cols + c]
            if (r, c) not in best:
                print(f'{name}: 칸이 비었다')
                continue
            _, i, sl = best[(r, c)]
            piece = arr[sl].copy()
            piece[..., 3] = np.where(lab[sl] == i, piece[..., 3], 0)
            im = trim(Image.fromarray(piece), pad=4)
            p = out / f'{name}.png'
            im.save(p, optimize=True)
            print(f'{name:16s} {im.size[0]}x{im.size[1]} {p.stat().st_size // 1024}KB')


if __name__ == '__main__':
    main()
