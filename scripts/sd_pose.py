"""배경이 투명한 SD 포즈 한 장(낚싯대 든 여울 등)을 게임용으로 정리하고 기준점을 기록한다.

사용법: python3 scripts/sd_pose.py <원본.png> <이름> --body=<몸 오른쪽 끝 x> [--erase=x,y] [--tip=x,y]
- 원본은 배경이 이미 투명해야 한다 (흰 바탕이면 art.py 로 먼저 지운다)
- --erase=x,y: 그 점에서 오른쪽 아래를 지운다. 낚싯대 끝에 그려진 줄과 봉돌을 없앨 때 (줄은 게임이 그린다)
- --body: 몸만 있는 세로 띠의 오른쪽 끝 x. 그 안에서 머리 꼭대기와 발바닥을 재서 키로 삼는다
- --tip: 낚싯대 끝. 주지 않으면 가장 오른쪽 위 점
- --tall=비율: 앉은 포즈처럼 키가 선 키의 이 비율일 때. 게임이 선 키로 맞추므로 키를 비율로 나눠 기록한다
- 결과: godot/art/yeoul/<이름>.png (키가 KEEP_BODY px 이 되게 줄인다)
        godot/data/poses.json 의 <이름>: 발(아래 가운데), 낚싯대 끝, 키 (모두 결과 그림 픽셀)
게임은 키를 기준으로 크기를 맞추므로 원본마다 크기가 달라도 같은 키로 보인다.
필요: pillow, numpy
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / 'godot' / 'art' / 'yeoul'
POSES = ROOT / 'godot' / 'data' / 'poses.json'
KEEP_BODY = 560


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    opts = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--'))
    src, name = args[0], args[1]
    a = np.array(Image.open(src).convert('RGBA'))
    h, w = a.shape[:2]

    if 'erase' in opts:
        ex, ey = (int(v) for v in opts['erase'].split(','))
        a[ey:, ex:, 3] = 0

    alpha = a[..., 3] > 100
    body_x = int(opts['body'])
    band = alpha[:, :body_x]
    rows = np.nonzero(band.any(axis=1))[0]
    top, bottom = int(rows.min()), int(rows.max())
    # 발: 몸 띠 맨 아래 40px 의 가로 가운데
    feet = np.nonzero(band[bottom - 40:bottom + 1].any(axis=0))[0]
    foot = ((feet.min() + feet.max()) / 2, bottom)

    if 'tip' in opts:
        tip = tuple(float(v) for v in opts['tip'].split(','))
    else:
        ys, xs = np.nonzero(alpha)
        i = int(np.argmax(xs * 4 - ys))
        tip = (float(xs[i]), float(ys[i]))

    # 여백을 자르고 키에 맞춰 줄인다
    ys, xs = np.nonzero(a[..., 3] > 8)
    x0, y0, x1, y1 = max(0, xs.min() - 4), max(0, ys.min() - 4), min(w, xs.max() + 5), min(h, ys.max() + 5)
    im = Image.fromarray(a).crop((x0, y0, x1, y1))
    k = min(1.0, KEEP_BODY / (bottom - top))
    im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    out = OUT_DIR / f'{name}.png'
    im.save(out, optimize=True)

    poses = json.loads(POSES.read_text()) if POSES.exists() else {}
    poses[name] = {
        'foot': [round((foot[0] - x0) * k, 1), round((foot[1] - y0) * k, 1)],
        'tip': [round((tip[0] - x0) * k, 1), round((tip[1] - y0) * k, 1)],
        'body': round((bottom - top) * k / float(opts.get('tall', 1)), 1),
    }
    POSES.write_text(json.dumps(poses, ensure_ascii=False, indent=2) + '\n')
    print(f'{name}: {im.width}x{im.height} {out.stat().st_size // 1024}KB {poses[name]}')


if __name__ == '__main__':
    main()
