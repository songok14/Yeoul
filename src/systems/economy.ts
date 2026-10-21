// 신선도, 시세, 물고기 값 계산

import { SPECIES, SPECIES_BY_ID, type SpeciesId } from '../data/fish';
import { TACKLES } from '../data/gear';
import { S, type FishItem } from '../state/save';
import { clamp, hash, mulberry32, round100 } from '../utils';

export type FreshStage = 'live' | 'fresh' | 'aged' | 'spoiled';

export const STAGE_NAME: Record<FreshStage, string> = {
  live: '활어',
  fresh: '선어',
  aged: '숙성',
  spoiled: '상함',
};

export const STAGE_COLOR: Record<FreshStage, string> = {
  live: '#7fd17e',
  fresh: '#8cc8ff',
  aged: '#f2c14e',
  spoiled: '#a08a7a',
};

export function stageOf(fresh: number): FreshStage {
  if (fresh > 75) return 'live';
  if (fresh > 40) return 'fresh';
  if (fresh > 10) return 'aged';
  return 'spoiled';
}

/** 신선도에 따른 값 배율. 숙성 어종은 숙성 단계에서 오히려 값이 오른다 */
export function freshFactor(species: SpeciesId, stage: FreshStage): number {
  switch (stage) {
    case 'live':
      return 1;
    case 'fresh':
      return 0.75;
    case 'aged':
      return SPECIES_BY_ID[species].aging ? 1.3 : 0.45;
    default:
      return 0;
  }
}

export function sizeFactor(species: SpeciesId, size: number): number {
  return Math.pow(size / SPECIES_BY_ID[species].avgSize, 1.5);
}

/** 날짜별 시세 배율. 전날 값에서 조금씩 움직이는 랜덤 워크 */
export function priceMul(species: SpeciesId, day: number): number {
  const idx = SPECIES.findIndex((s) => s.id === species);
  let m = 1;
  for (let d = 1; d <= day; d++) {
    const r = mulberry32(hash(S.seed, d, idx))();
    m = clamp(m + (r - 0.5) * 0.5, 0.55, 1.7);
    // 가끔 풍어나 품귀가 터진다
    const shock = mulberry32(hash(S.seed, d, idx, 7))();
    if (shock < 0.06) m = clamp(m * 0.6, 0.55, 1.7);
    else if (shock > 0.95) m = clamp(m * 1.45, 0.55, 1.7);
  }
  return m;
}

/** 평균 크기 활어 기준 오늘 시세 */
export function todayBasePrice(species: SpeciesId, day = S.day): number {
  return round100(SPECIES_BY_ID[species].basePrice * priceMul(species, day));
}

export function fishValue(item: FishItem, day = S.day): number {
  const stage = stageOf(item.fresh);
  return round100(
    SPECIES_BY_ID[item.species].basePrice *
      sizeFactor(item.species, item.size) *
      priceMul(item.species, day) *
      freshFactor(item.species, stage),
  );
}

export function changePercent(species: SpeciesId, day = S.day): number {
  if (day <= 1) return 0;
  const today = priceMul(species, day);
  const prev = priceMul(species, day - 1);
  return Math.round(((today - prev) / prev) * 100);
}

export function coolerCapacity(): number {
  return TACKLES[S.tackle].capacity;
}

export function dailyDecay(): number {
  return TACKLES[S.tackle].decay;
}

/** 오늘의 위판장 소식 한 줄 */
export function marketHeadline(day = S.day): string {
  if (day <= 1) return '물비늘포 위판장 소식: 봄 볼락이 올라오기 시작했다';
  let best = SPECIES[0];
  let bestAbs = -1;
  for (const sp of SPECIES) {
    const c = changePercent(sp.id, day);
    if (Math.abs(c) > bestAbs) {
      bestAbs = Math.abs(c);
      best = sp;
    }
  }
  const c = changePercent(best.id, day);
  if (Math.abs(c) < 5) return '오늘 시세는 잔잔하다';
  return c > 0 ? `${best.name} 값이 ${c}% 뛰었다` : `${best.name} 값이 ${-c}% 떨어졌다`;
}
