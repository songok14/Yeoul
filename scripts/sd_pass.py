"""옆으로 걷는 SD(다리를 벌린 그림)에서 다리를 모은 그림을 만든다 (걸음 중간, 지나가는 자세).

다리를 벌린 그림 두 장만 번갈면 다리가 오가지 않아 미끄러져 보인다.
가랑이 틈으로 뒷다리(왼쪽)와 앞다리(오른쪽)를 가르고, 엉덩이를 축으로 두 다리를 몸 밑으로 모은다
(아래로 갈수록 많이 옮긴다). 앞다리를 위에 그린다. 그림은 오른쪽을 본다(CLAUDE.md).

    python3 scripts/sd_pass.py godot/art/villagers/auntie-walk-1.png godot/art/villagers/auntie-pass.png
    python3 scripts/sd_pass.py --preview <png> <그림들>
"""
import sys

import numpy as np
from PIL import Image

GATHER = 0.6     # 발을 몸 밑으로 얼마나 모을지 (1이면 엉덩이 바로 밑)


def crotch(alpha):
    """가랑이 높이(틈이 닫히는 줄), 다리 맨 아래, 줄마다 틈 가운데 x"""
    solid = alpha > 40
    h, w = solid.shape
    bottom = np.where(solid.any(1))[0].max()
    gaps = {}
    # 아래에서 위로: 발 조금 위 줄(맨 아래는 한쪽 발끝만 닿기도 한다)의 두 발 사이 틈을 따라 올라간다
    row = solid[bottom - max(3, int(h * 0.035))]
    cols = np.where(row)[0]
    jumps = np.where(np.diff(cols) > 2)[0]
    if len(jumps) == 0:
        return None
    j = jumps[np.argmax(np.diff(cols)[jumps])]
    gx = (cols[j] + cols[j + 1]) // 2
    top = None
    for y in range(bottom - max(3, int(h * 0.035)), int(h * 0.4), -1):
        r = solid[y]
        if r[gx]:
            # 틈이 옆으로 움직였으면 가까운 빈칸을 찾는다
            near = [x for x in range(max(0, gx - 6), min(w, gx + 7)) if not r[x]]
            if not near:
                top = y + 1
                break
            gx = min(near, key=lambda x: abs(x - gx))
        # 틈 양옆 빈칸 범위의 가운데
        l = gx
        while l > 0 and not r[l - 1]:
            l -= 1
        rr = gx
        while rr < w - 1 and not r[rr + 1]:
            rr += 1
        gx = (l + rr) // 2
        gaps[y] = gx
    if top is None:
        return None
    for y in range(bottom - max(3, int(h * 0.035)), bottom + 1):
        gaps[y] = gaps[bottom - max(3, int(h * 0.035)) + 1] if (bottom - max(3, int(h * 0.035)) + 1) in gaps else gx
    return top, bottom, gaps


def gather(im):
    a = np.asarray(im.convert('RGBA'))
    h, w = a.shape[:2]
    c = crotch(a[:, :, 3])
    if c is None:
        raise SystemExit('가랑이를 못 찾았다')
    top, bottom, gaps = c
    solid = a[:, :, 3] > 40
    # 발 가운데: 맨 아래 쪽 줄들에서 틈 왼쪽·오른쪽
    yb = range(max(top, bottom - (bottom - top) // 4), bottom + 1)
    lx = [np.where(solid[y, :gaps[y]])[0] for y in yb if y in gaps]
    rx = [gaps[y] + np.where(solid[y, gaps[y]:])[0] for y in yb if y in gaps]
    lx = np.concatenate([v for v in lx if len(v)])
    rx = np.concatenate([v for v in rx if len(v)])
    hip = gaps[top + 1] if top + 1 in gaps else (lx.mean() + rx.mean()) / 2
    dl = (hip - lx.mean()) * GATHER
    dr = (rx.mean() - hip) * GATHER
    out = a.copy()
    out[top:] = 0
    back = np.zeros_like(a)
    front = np.zeros_like(a)
    for y in range(top, h):
        g = gaps.get(y, gaps[max(k for k in gaps if k <= y)] if any(k <= y for k in gaps) else w // 2)
        t = min(1.0, (y - top) / max(1, (bottom - top) * 0.8))
        t = t * t * (3 - 2 * t)
        sl, sr = int(round(dl * t)), int(round(dr * t))
        row = a[y]
        L = row[:g]
        R = row[g:]
        back[y, sl:sl + g] = L[:max(0, min(g, w - sl))]
        x0 = max(0, g - sr)
        front[y, x0:x0 + (w - g)] = R[:w - x0]
    canvas = Image.fromarray(out)
    canvas.alpha_composite(Image.fromarray(back))
    canvas.alpha_composite(Image.fromarray(front))
    return canvas


def main():
    args = sys.argv[1:]
    if args[0] == '--preview':
        shots = []
        for p in args[2:]:
            im = Image.open(p).convert('RGBA')
            shots += [im, gather(im)]
        c = Image.new('RGBA', (sum(s.width for s in shots) + 10 * len(shots), max(s.height for s in shots)), (70, 80, 90, 255))
        x = 0
        for s in shots:
            c.alpha_composite(s, (x, 0))
            x += s.width + 10
        c.save(args[1])
        return
    src, dst = args
    gather(Image.open(src)).save(dst, optimize=True)
    print(dst)


if __name__ == '__main__':
    main()
