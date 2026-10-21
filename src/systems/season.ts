// 간 맞추기 판정. 게이지를 멈춘 위치로 정확도와 방향을 낸다

import { clamp } from '../utils';

/** 정확도(0~1)와 방향(-1 모자람, 0 정답, 1 지나침) */
export function judgeSeason(pos: number, target: number, half: number): { quality: number; side: -1 | 0 | 1 } {
  const d = Math.abs(pos - target);
  const quality = d <= half ? 1 - 0.2 * (d / half) : clamp(0.8 - (d - half) / (half * 2.5), 0, 0.8);
  const side: -1 | 0 | 1 = d <= half ? 0 : pos < target ? -1 : 1;
  return { quality, side };
}
