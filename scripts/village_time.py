# 마을 낮 그림(village-2.png)으로 아침(village-1)과 저녁(village-3)을 만든다. 새벽(village-0)은 따로 받은 밤 그림.
# 사용법: python3 scripts/village_time.py
# 하늘의 흰 구름은 색을 입히지 않고 하얗게 둔다 (사용자 요청 2026-10-22). 고닷 ArtStage.KeepClouds 셰이더와 같은 기준.
import os

import numpy as np
from PIL import Image

DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'godot', 'art', 'backgrounds')


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def cloud_mask(img):
    """그림 위쪽의 밝고 채도 낮은 곳 (흰 구름)"""
    mx = img.max(axis=2)
    mn = img.min(axis=2)
    sat = np.where(mx > 1e-3, (mx - mn) / np.maximum(mx, 1e-3), 0)
    h = img.shape[0]
    y = np.linspace(0, 1, h)[:, None]
    return (smooth(0.66, 0.84, mx) * (1 - smooth(0.14, 0.3, sat)) * (1 - smooth(0.30, 0.40, y)))[..., None]


def grade(img, top, bottom, mul, gamma, sky_mix, sky_col):
    h = img.shape[0]
    y = np.linspace(0, 1, h)[:, None, None]
    g = np.array(top) * (1 - y) + np.array(bottom) * y
    out = img * (1 - mul + mul * g)
    w = np.clip(1 - y / 0.42, 0, 1) ** 1.5 * sky_mix
    out = out * (1 - w) + np.array(sky_col) * w * (0.6 + 0.4 * img.mean(axis=2, keepdims=True))
    out = np.clip(out, 0, 1) ** gamma
    # 흰 구름은 원래대로
    m = cloud_mask(img)
    return out * (1 - m) + img * m


def main():
    day = np.asarray(Image.open(os.path.join(DIR, 'village-2.png')).convert('RGB')).astype(np.float64) / 255
    morning = grade(day, [1.05, 0.95, 0.85], [1.08, 0.98, 0.86], 0.55, 1.0, 0.18, [1.0, 0.82, 0.68])
    evening = grade(day, [1.08, 0.8, 0.72], [1.02, 0.86, 0.74], 0.6, 1.06, 0.42, [1.0, 0.6, 0.48])
    for name, img in (('village-1', morning), ('village-3', evening)):
        Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(os.path.join(DIR, name + '.png'), optimize=True)
        print('저장', name)


if __name__ == '__main__':
    main()
