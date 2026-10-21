"""원본 일러스트를 게임용 이미지로 손질한다.

사용법: python3 scripts/art.py <원본 폴더>
- 흰 배경과 가짜 투명 체크무늬를 지운다 (가장자리에서 이어진 밝은 무채색만)
- 모음 시트에서 필요한 그림만 잘라 낸다
- public/assets/ 에 WebP로 저장한다
필요: pillow, numpy, scipy
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

RAW = Path(sys.argv[1]) if len(sys.argv) > 1 else Path('art-raw')
OUT = Path(__file__).resolve().parent.parent / 'public' / 'assets'


def bg_mask(rgb: np.ndarray) -> np.ndarray:
    """가장자리와 이어진 밝은 무채색 영역 = 배경"""
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    light = (mn > 196) & (mx - mn < 12)
    lab, _ = ndimage.label(light)
    edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    edge = edge[edge > 0]
    bg = np.isin(lab, edge)
    # 머리카락 사이처럼 갇힌 체크무늬 조각도 지운다 (밝고 작은 무채색 덩어리)
    holes = light & ~bg
    hl, n = ndimage.label(holes)
    if n:
        sizes = ndimage.sum(holes, hl, range(1, n + 1))
        # 체크무늬는 밝은 칸과 어두운 칸이 섞여 있어 밝기 편차가 크다. 흰 털·흰 옷은 고르다
        spread = ndimage.standard_deviation(mn, hl, range(1, n + 1))
        kill = [i + 1 for i in range(n) if sizes[i] < 4000 and spread[i] > 9]
        bg |= np.isin(hl, kill)
    return bg


def cutout(img: Image.Image) -> Image.Image:
    rgb = np.array(img.convert('RGB'))
    bg = bg_mask(rgb)
    alpha = np.where(bg, 0, 255).astype(np.uint8)
    a = Image.fromarray(alpha).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
    out = Image.fromarray(rgb).convert('RGBA')
    out.putalpha(a)
    return out


def parts(img: Image.Image, min_area=6000):
    """투명하게 만든 시트에서 덩어리별 상자를 큰 순서로"""
    a = np.array(img.getchannel('A')) > 40
    a = ndimage.binary_dilation(a, iterations=3)
    lab, n = ndimage.label(a)
    boxes = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        area = int((lab[sl] == i).sum())
        if area >= min_area:
            boxes.append((area, (sl[1].start, sl[0].start, sl[1].stop, sl[0].stop)))
    return sorted(boxes, reverse=True)


def largest_only(img: Image.Image) -> Image.Image:
    """가장 큰 덩어리만 남긴다 (옆 그림 조각 제거)"""
    arr = np.array(img)
    a = arr[..., 3] > 20
    lab, n = ndimage.label(ndimage.binary_dilation(a, iterations=2))
    if n > 1:
        sizes = ndimage.sum(a, lab, range(1, n + 1))
        keep = lab == (int(np.argmax(sizes)) + 1)
        arr[..., 3] = np.where(keep, arr[..., 3], 0)
    return Image.fromarray(arr)


def trim(img: Image.Image, pad=6) -> Image.Image:
    box = img.getchannel('A').point(lambda v: 255 if v > 20 else 0).getbbox()
    if not box:
        return img
    x0, y0, x1, y1 = box
    return img.crop((max(0, x0 - pad), max(0, y0 - pad), min(img.width, x1 + pad), min(img.height, y1 + pad)))


def save(img: Image.Image, name: str, max_h: int | None = None, max_w: int | None = None, q=86):
    if max_h and img.height > max_h:
        img = img.resize((round(img.width * max_h / img.height), max_h), Image.LANCZOS)
    if max_w and img.width > max_w:
        img = img.resize((max_w, round(img.height * max_w / img.width)), Image.LANCZOS)
    OUT.mkdir(parents=True, exist_ok=True)
    p = OUT / f'{name}.webp'
    img.save(p, 'WEBP', quality=q, method=6)
    print(f'{name:22s} {img.size[0]}x{img.size[1]} {p.stat().st_size // 1024}KB')


def raw(name: str) -> Image.Image:
    return Image.open(RAW / f'{name}.png')


if __name__ == '__main__' and len(sys.argv) > 2 and sys.argv[2] == 'inspect':
    for n in sys.argv[3:]:
        im = cutout(raw(n))
        print(n, im.size, [b for _, b in parts(im)][:14])
    sys.exit()


def build():
    # 배경: 화면(450x800)의 두 배 크기로
    bgs = {
        'bg-village-0': '02-village-dawn',
        'bg-village-1': '03-village-morning',
        'bg-village-2': '01-village-day',
        'bg-village-3': '04-village-evening',
        'bg-breakwater': '05-breakwater-a',
        'bg-market': '07-market',
        'bg-plaza': '08-plaza',
        'bg-baitshop': '10-baitshop-b',
        'bg-restaurant': '13-restaurant-out',
        'bg-alley': '12-alley-sunset',
    }
    for key, src in bgs.items():
        im = raw(src).convert('RGB').resize((900, 1600), Image.LANCZOS)
        save(im, key, q=82)

    # 인물: 배경을 지우고 잘라 낸다
    singles = {
        'ch-yeoul': ('15-yeoul-anime-front', None),
        'ch-yeoul-back': ('17-yeoul-anime-back', None),
        'ch-grandma': ('24-grandma-apron', None),
        'ch-grandma-stew': ('23-grandma-stew', None),
        'ch-nossi': ('25-oldman-apron', None),
        'ch-cat': ('31-cat-sheet-b', (8, 0, 375, 636)),
        'ch-cat-walk': ('31-cat-sheet-b', (19, 634, 238, 1007)),
        'ch-cat-sleep': ('31-cat-sheet-b', (934, 760, 1305, 994)),
        'truck': ('33-truck-sheet', (42, 561, 791, 961)),
    }
    for key, (src, box) in singles.items():
        im = raw(src)
        if box:
            im = im.crop(box)
        im = trim(largest_only(cutout(im)))
        save(im, key, max_h=900, max_w=900)


if __name__ == '__main__' and (len(sys.argv) < 3 or sys.argv[2] == 'build'):
    build()
