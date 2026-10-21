// 미각 회복 단계

import type { Flavor } from '../data/recipes';
import { S } from '../state/save';

export interface TasteStage {
  name: string;
  flavors: Flavor[];
  cond: string;
  /** 필요한 어종 수 (1장 축소 기준) */
  needSpecies?: number;
  effect: string;
}

export const TASTE_STAGES: TasteStage[] = [
  { name: '무미', flavors: [], cond: '시작', effect: '손님 반응이 전부 ???' },
  { name: '짠맛', flavors: ['salty'], cond: '첫 어종 요리', effect: '짠맛 정답 구간이 보인다' },
  { name: '단맛', flavors: ['salty', 'sweet'], cond: '어종 3종 낚기', needSpecies: 3, effect: '단맛 정답 구간이 보인다' },
  { name: '감칠맛', flavors: ['salty', 'sweet', 'umami'], cond: '어종 5종 낚기', needSpecies: 5, effect: '육수 맛과 손님 리뷰가 보인다' },
  { name: '쓴맛·신맛', flavors: ['salty', 'sweet', 'umami', 'sour', 'bitter'], cond: '다음 챕터에서', effect: '고급 퓨전 요리와 셰프 대회' },
  { name: '완전 회복', flavors: ['salty', 'sweet', 'umami', 'sour', 'bitter'], cond: '???', effect: '???' },
];

export function speciesCaught(): number {
  return Object.values(S.dex).filter((d) => d && d.count > 0).length;
}

export function tasteLevel(): number {
  if (!S.flags.firstTaste) return 0;
  const n = speciesCaught();
  if (n >= 5) return 3;
  if (n >= 3) return 2;
  return 1;
}

export function canTaste(flavor: Flavor): boolean {
  return TASTE_STAGES[tasteLevel()].flavors.includes(flavor);
}

/** 새로 열린 미각 단계가 있으면 돌려주고 기록한다 */
export function checkTasteUnlock(): TasteStage | null {
  const lv = tasteLevel();
  const seen = S.flags[`taste${lv}`];
  if (lv > 0 && !seen) {
    S.flags[`taste${lv}`] = true;
    return TASTE_STAGES[lv];
  }
  return null;
}
