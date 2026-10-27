"""SD 시트(배경 #EDEDED)를 칸마다 잘라 게임용 PNG로 저장한다.

사용법: python3 scripts/sd_sheet.py <시트.png> <행> <열> <저장 폴더> <이름1,이름2,...> [--face=right]
      python3 scripts/sd_sheet.py --face=right <이미 자른 그림들...>   (방향만 맞추기, 제자리에서 고친다)
- --face=right: 걷기 그림이 오른쪽을 보게 맞춘다. 얼굴(살빛)이 머리 가운데보다 왼쪽에 있으면 좌우를 뒤집는다.
  게임의 SD 걷기 그림은 모두 오른쪽을 본다. 왼쪽으로 갈 때만 FlipH 를 켠다
- 배경 지우기는 art.py 와 같다 (가장자리와 이어진 밝은 무채색)
- 머리카락 사이에 갇힌 배경 조각도 시트 배경색과 비교해 지운다
- 칸마다 가장 큰 덩어리만 남긴다. 강조 표시(! 같은 작은 선)는 빠진다
- 크기는 바꾸지 않는다. 같은 시트의 프레임은 같은 비율이라 애니메이션에 그대로 쓴다
- 게임에서는 아래 가운데를 기준점으로 둔다 (발이 같은 바닥선)
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


def facing(im: Image.Image) -> float:
    """보는 방향 점수. 양수면 오른쪽, 음수면 왼쪽을 본다.
    옆으로 걷는 SD 는 머리에서 얼굴(살빛)이 앞쪽, 머리카락이 뒤쪽에 있다. 0 에 가까우면 거의 정면이라 믿기 어렵다"""
    a = np.array(im.convert('RGBA')).astype(int)
    solid = a[..., 3] > 128
    ys = np.nonzero(solid)[0]
    if len(ys) == 0:
        return 0.0
    top, bot = ys.min(), ys.max()
    head = slice(top, top + int((bot - top) * 0.42))
    r, g, b = a[head, :, 0], a[head, :, 1], a[head, :, 2]
    skin = solid[head] & (r > 170) & (r > g) & (g > b) & (r - b > 35) & (r - b < 140) & (g > 110)
    hx, sx = np.nonzero(solid[head])[1], np.nonzero(skin)[1]
    if len(sx) < 50:
        return 0.0
    return float((sx.mean() - hx.mean()) / max(1, hx.max() - hx.min()))


def face(im: Image.Image, want: str, name: str) -> Image.Image:
    """want 쪽을 보게 좌우를 맞춘다"""
    score = facing(im)
    if abs(score) < 0.02:
        print(f'{name}: 방향이 애매하다 ({score:+.3f}). 눈으로 확인할 것')
    if (score < 0) == (want == 'right') and score != 0:
        print(f'{name}: 좌우 뒤집음 ({score:+.3f})')
        return im.transpose(Image.FLIP_LEFT_RIGHT)
    return im


def main():
    want = next((a.split('=', 1)[1] for a in sys.argv[1:] if a.startswith('--face=')), None)
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    if want not in (None, 'right', 'left'):
        sys.exit('--face 는 right 또는 left')
    if want and all(a.lower().endswith('.png') for a in args):
        # 이미 자른 그림의 방향만 맞춘다
        for f in args:
            im = Image.open(f)
            fixed = face(im, want, f)
            if fixed is not im:
                fixed.save(f, optimize=True)
        return
    src, rows, cols, out, names = args[0], int(args[1]), int(args[2]), Path(args[3]), args[4].split(',')
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
            if want:
                im = face(im, want, name)
            p = out / f'{name}.png'
            im.save(p, optimize=True)
            print(f'{name:16s} {im.size[0]}x{im.size[1]} {p.stat().st_size // 1024}KB')


if __name__ == '__main__':
    main()
