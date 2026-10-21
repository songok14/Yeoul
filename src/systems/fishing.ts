// 입질 어종 결정과 크기

import { SPECIES, type Species } from '../data/fish';
import { weightedPick } from '../utils';

export type Zone = 0 | 1 | 2;
export const ZONE_NAME = ['근거리', '중거리', '원거리'] as const;

export function zoneOf(dist01: number): Zone {
  if (dist01 < 0.35) return 0;
  if (dist01 < 0.7) return 1;
  return 2;
}

export function rollSpecies(zone: Zone, slot: number, hot: boolean): Species {
  const weights = SPECIES.map((sp) => {
    let w = sp.zone[zone] * sp.slot[slot];
    if (sp.rare) w *= hot ? 2.2 : 0.7;
    return w;
  });
  return weightedPick(SPECIES, weights);
}

export function rollSize(sp: Species, hot: boolean): number {
  let r = Math.pow(Math.random(), 1.6);
  if (hot) r = Math.pow(r, 0.75);
  return Math.round((sp.minSize + (sp.maxSize - sp.minSize) * r) * 10) / 10;
}
