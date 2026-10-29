"""앞모습·뒷모습 SD 한 장에서 걷기 두 장을 만든다 (다리를 한쪽씩 들어 올린다).

언덕 마을은 거의 위아래로 걸어서, 앞·뒤 그림 한 장으로는 떠서 미끄러지는 것처럼 보였다.
다리 사이 틈(가랑이)을 찾아 그 아래를 왼쪽·오른쪽 다리로 나누고, 한쪽 다리를 위로 줄여 발을 든다.
진짜 걷기 그림(ART_PROMPTS SD 3-1)이 오면 같은 이름으로 바꿔 넣는다.

    python3 scripts/sd_step.py godot/art/yeoul/front.png godot/art/yeoul/walk-front   # walk-front-1/2.png
    python3 scripts/sd_step.py --preview <png> <그림들>
"""
import sys

import numpy as np
from PIL import Image

LIFT = 0.07       # 든 발이 올라가는 높이 (그림 키 비율)
SHOE = 0.11       # 신발 높이 (그림 키 비율). 신발은 줄이지 않고 통째로 올린다


def legs(a):
    """가랑이 높이와 다리를 나누는 x. 아래에서 위로 훑어 가운데 틈이 닫히는 줄을 찾는다"""
    h, w = a.shape
    solid = a > 40
    ys, xs = np.where(solid)
    cx = int((xs.min() + xs.max()) / 2)
    bottom = ys.max()
    split, top = None, None
    for y in range(bottom - int(h * 0.04), int(h * 0.5), -1):
        row = solid[y, cx - w // 5: cx + w // 5]
        gap = np.where(~row)[0]
        if len(gap) == 0:
            top = y + 1
            break
        split = cx - w // 5 + int(np.median(gap))
    return top, split


def step(im, side):
    a = np.asarray(im)
    h, w = a.shape[:2]
    top, split = legs(a[:, :, 3])
    out = a.copy()
    x0, x1 = (0, split) if side == 0 else (split, w)
    bottom = np.where(a[:, x0:x1, 3].max(1) > 40)[0].max() + 1
    lift = int(h * LIFT)
    shoe_y = bottom - int(h * SHOE)
    pants = a[top:shoe_y, x0:x1]
    shoe = a[shoe_y:bottom, x0:x1]
    new = np.asarray(Image.fromarray(pants).resize((x1 - x0, pants.shape[0] - lift), Image.LANCZOS))
    out[top:, x0:x1] = 0
    out[top:shoe_y - lift, x0:x1] = new
    out[shoe_y - lift:bottom - lift, x0:x1] = shoe
    return Image.fromarray(out), top, split


def main():
    args = sys.argv[1:]
    if args[0] == '--preview':
        shots = []
        for p in args[2:]:
            im = Image.open(p).convert('RGBA')
            for s in (0, 1):
                shots.append(step(im, s)[0])
            shots.append(im)
        c = Image.new('RGBA', (sum(s.width for s in shots) + 10 * len(shots), max(s.height for s in shots)), (70, 80, 90, 255))
        x = 0
        for s in shots:
            c.alpha_composite(s, (x, 0))
            x += s.width + 10
        c.save(args[1])
        return
    src, dst = args
    im = Image.open(src).convert('RGBA')
    for s in (0, 1):
        out, top, split = step(im, s)
        out.save(f'{dst}-{s + 1}.png', optimize=True)
        print(f'{dst}-{s + 1}.png 가랑이 y={top} 나눔 x={split}')


if __name__ == '__main__':
    main()
