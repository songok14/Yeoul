"""배경을 지운 일러스트(RGBA)에 남은 흰 테두리를 정리한다.

사용법: python3 scripts/clean_halo.py [--strong=0.45] <그림.png|webp> [...]   (제자리에서 고친다)
  --strong=비율: 그림 위쪽 이 비율 안(머리 부분)에서는 크림빛 테두리까지 지운다. 상반신 그림에만 쓴다
  --fringe: 머리 둘레에서 삐져나온 가는 가닥과 그 사이 분홍빛 바탕을 밝을수록 투명하게 푼다.
            갈색 머리 그림에만 쓴다 (흰머리·회색 머리에는 머리카락까지 지워진다)
- 투명한 바깥과 이어진 밝은 무채색 픽셀을 지운다 (머리카락 사이로 남은 흰 배경)
- 머리카락 사이에 갇힌 흰 조각도 지운다 (주변이 대부분 머리카락·투명이고 고르게 흰 것)
- 남은 가장자리의 밝은 픽셀은 반투명으로 풀어 흰 테가 보이지 않게 한다
- 손대는 곳은 머리카락(갈색) 근처뿐이다. 흰 옷 가장자리는 건드리지 않는다
테두리 선 안쪽의 흰 옷, 눈 반짝임은 바깥과 이어져 있지 않아 남는다.
필요: pillow, numpy, scipy
"""
import sys

import numpy as np
from PIL import Image
from scipy import ndimage


def clean(path: str, strong: float = 0.0) -> None:
    im = Image.open(path).convert('RGBA')
    a = np.array(im)
    rgb = a[..., :3].astype(int)
    alpha = a[..., 3].astype(int)
    mx = rgb.max(axis=2)
    mn = rgb.min(axis=2)
    light = (mn > 200) & (mx - mn < 22) & (alpha > 0)
    clear = alpha < 16

    # 머리카락(갈색) 위치
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hair = (alpha > 128) & (r >= g) & (g >= b) & (r - b > 35) & (mx < 215)
    hair = ndimage.binary_opening(hair, iterations=1)
    near_hair = ndimage.binary_dilation(hair, iterations=70)

    # 1) 투명한 바깥과 이어진 밝은 무채색 덩어리마다 판단한다.
    #    대부분이 머리카락 근처에 있으면 남은 배경, 아니면 흰 옷 가장자리라 남긴다
    lab, n = ndimage.label(light)
    bg = np.zeros_like(light)
    if n:
        # 배경을 지울 때 생긴 반투명 가장자리까지 '바깥'으로 본다 (흰 테는 그 안쪽에 붙어 있다)
        touch_out = ndimage.binary_dilation(alpha < 230, iterations=3)
        idx = range(1, n + 1)
        sizes = ndimage.sum(light, lab, idx)
        outside = ndimage.sum(touch_out & light, lab, idx)
        by_hair = ndimage.sum(near_hair & light, lab, idx)
        # 머리카락 가닥 사이에 갇힌 작은 흰 조각: 바로 옆이 거의 다 머리카락이고 아주 흰 것
        tight = ndimage.binary_dilation(hair, iterations=5)
        in_hair = ndimage.sum(tight & light, lab, idx)
        whiteness = ndimage.mean(mn, lab, idx)
        # 남은 배경은 거의 순백, 옷은 크림색이라 색이 조금 있다
        tint = ndimage.mean((mx - mn).astype(float), lab, idx)
        top = ndimage.mean(np.indices(mn.shape)[0].astype(float), lab, idx)
        h = mn.shape[0]

        def pale(i):
            # 머리 부분이면 머리색이 비친 크림빛 테두리도 배경으로 본다
            if strong and top[i] < h * strong:
                return whiteness[i] > 215 and tint[i] < 32
            return whiteness[i] > 232 and tint[i] < 9

        kill = [i + 1 for i in range(n)
                if sizes[i] >= 20 and by_hair[i] / sizes[i] > 0.6 and pale(i)
                and (outside[i] > 0 or (sizes[i] < 1500 and in_hair[i] / sizes[i] > 0.95 and whiteness[i] > 238))]
        bg = np.isin(lab, kill)

    alpha[bg] = 0

    # 3) 머리카락 둘레 4px 의 밝은 픽셀은 밝을수록 투명하게
    edge = ndimage.binary_dilation(alpha == 0, iterations=4) & (alpha > 0) & ~hair & ndimage.binary_dilation(hair, iterations=10)
    lum = mn.astype(float)
    fade = np.clip((lum - 170) / 70, 0, 1)
    alpha = np.where(edge, (alpha * (1 - 0.85 * fade)).astype(int), alpha)

    a[..., 3] = np.clip(alpha, 0, 255).astype(np.uint8)
    out = Image.fromarray(a, 'RGBA')
    if path.endswith('.webp'):
        out.save(path, quality=90, method=6)
    else:
        out.save(path, optimize=True)
    print(f'정리 {path}: 배경 픽셀 {int(bg.sum())}개 지움')


def fringe(path: str, top: float = 0.5, band: int = 30) -> None:
    """그림 위쪽 top 비율 안, 바깥에서 band px 안쪽의 머리 둘레만 손댄다"""
    a = np.array(Image.open(path).convert('RGBA'))
    rgb = a[..., :3].astype(int); alpha = a[..., 3].astype(float)
    mx = rgb.max(2); mn = rgb.min(2)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hair = (alpha > 128) & (r >= g) & (g >= b) & (r - b > 35) & (mx < 215)
    hair = ndimage.binary_opening(hair, iterations=1)
    outside = alpha < 16
    # 그림 바깥(큰 투명 덩어리)에서 가까운 곳만 본다
    lab, n = ndimage.label(outside)
    sizes = ndimage.sum(outside, lab, range(1, n + 1))
    big = np.isin(lab, [i + 1 for i in range(n) if sizes[i] > 400])
    near_out = ndimage.binary_dilation(big, iterations=band)
    near_hair = ndimage.binary_dilation(hair, iterations=6)
    h = a.shape[0]
    rows = np.indices(mn.shape)[0] < h * top
    zone = near_out & near_hair & rows & (alpha > 0)
    f = np.clip((205 - mn) / 35.0, 0, 1)
    alpha = np.where(zone, alpha * f, alpha)
    # 머리 덩어리에서 삐져나온 가는 가닥(두께 6px 미만)은 밝을수록 더 투명하게
    solid = ndimage.binary_opening(alpha > 128, iterations=3)
    thin = (alpha > 0) & ~ndimage.binary_dilation(solid, iterations=1) & near_out & rows
    f2 = np.clip((185 - mn) / 60.0, 0, 1)
    alpha = np.where(thin, alpha * f2, alpha)
    # 머리 바로 옆 순백 조각(600px 미만, 바깥에서 10px 안). 후드처럼 큰 흰 덩어리는 크기로 거른다
    white = (alpha > 24) & (mn > 212) & (mx - mn < 32) & rows
    lab3, n3 = ndimage.label(white)
    if n3:
        idx = range(1, n3 + 1)
        sz3 = ndimage.sum(white, lab3, idx)
        near3 = ndimage.sum(white & ndimage.binary_dilation(hair, iterations=14), lab3, idx)
        out3 = ndimage.sum(white & ndimage.binary_dilation(big, iterations=10), lab3, idx)
        chips = [i + 1 for i in range(n3) if sz3[i] < 600 and out3[i] > 0 and near3[i] / sz3[i] > 0.5]
        alpha = np.where(np.isin(lab3, chips), 0, alpha)
    # 떨어져 남은 작은 조각(150px 미만)은 지운다
    vis = alpha > 24
    lab2, n2 = ndimage.label(vis)
    if n2:
        sz = ndimage.sum(vis, lab2, range(1, n2 + 1))
        small = np.isin(lab2, [i + 1 for i in range(n2) if sz[i] < 150])
        alpha = np.where(small & rows, 0, alpha)
    a[..., 3] = np.clip(alpha, 0, 255).astype(np.uint8)
    Image.fromarray(a, 'RGBA').save(path, optimize=True)
    print(f'가닥 정리 {path}')


if __name__ == '__main__':
    strong = 0.0
    do_fringe = False
    for p in sys.argv[1:]:
        if p == '--fringe':
            do_fringe = True
            continue
        if p.startswith('--strong='):
            strong = float(p.split('=', 1)[1])
            continue
        if do_fringe:
            fringe(p)
        else:
            clean(p, strong)
