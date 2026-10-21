// 1장 달력과 낚시 규정 (금지체장, 금어기)
// 규정 수치는 FishingPlan 앱의 기본표(core/catches/Limits.kt)를 따른다

import { SPECIES_BY_ID, type SpeciesId } from '../data/fish';
import { S } from '../state/save';

/** 1장 1일차 = 봄 4월 16일. 16일차부터 5월이라 감성돔 금어기가 걸린다 */
const CH1_START = new Date(2026, 3, 16);

export function dateOf(day = S.day): { month: number; date: number } {
  const d = new Date(CH1_START);
  d.setDate(d.getDate() + day - 1);
  return { month: d.getMonth() + 1, date: d.getDate() };
}

export function dateLabel(day = S.day): string {
  const { month, date } = dateOf(day);
  return `${month}월 ${date}일`;
}

export function isClosedSeason(species: SpeciesId, day = S.day): boolean {
  const c = SPECIES_BY_ID[species].closed;
  if (!c) return false;
  const { month, date } = dateOf(day);
  const v = month * 100 + date;
  return v >= c[0] * 100 + c[1] && v <= c[2] * 100 + c[3];
}

/** 챙길 수 없는 이유. 문제없으면 null */
export function releaseReason(species: SpeciesId, size: number, day = S.day): string | null {
  const sp = SPECIES_BY_ID[species];
  if (sp.limitCm !== undefined && size <= sp.limitCm) return `금지체장 ${sp.limitCm}cm 이하라 놓아줘야 한다`;
  if (isClosedSeason(species, day) && sp.closed) {
    const [m1, d1, m2, d2] = sp.closed;
    return `금어기(${m1}/${d1}~${m2}/${d2})라 놓아줘야 한다`;
  }
  return null;
}

/** 오늘 금어기인 어종 이름 */
export function closedToday(day = S.day): string[] {
  return Object.values(SPECIES_BY_ID).filter((sp) => isClosedSeason(sp.id, day)).map((sp) => sp.name);
}
