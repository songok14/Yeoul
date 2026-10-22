"""배경을 지운 일러스트(RGBA)에 남은 흰 테두리를 정리한다.

사용법: python3 scripts/clean_halo.py [--strong=0.45] <그림.png|webp> [...]   (제자리에서 고친다)
  --strong=비율: 그림 위쪽 이 비율 안(머리 부분)에서는 크림빛 테두리까지 지운다. 상반신 그림에만 쓴다
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


if __name__ == '__main__':
    strong = 0.0
    for p in sys.argv[1:]:
        if p.startswith('--strong='):
            strong = float(p.split('=', 1)[1])
            continue
        clean(p, strong)
