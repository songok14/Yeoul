// 여울호 푸드트럭 계산: 재료 고르기, 요리값, 손님 반응

import { SPECIES_BY_ID } from '../data/fish';
import { FLAVOR_REVIEW, RECIPES, type Recipe } from '../data/recipes';
import { S, type FishItem } from '../state/save';
import { round100 } from '../utils';
import { stageOf, type FreshStage } from './economy';
import { canTaste, tasteLevel } from './taste';

export interface SeasonResult {
  quality: number;
  side: -1 | 0 | 1;
}

/** 요리 신선도 배율. 회는 신선도에 민감하고 숙성 어종은 숙성일 때 가장 좋다 */
export function dishFresh(recipe: Recipe, stage: FreshStage): number {
  const aging = SPECIES_BY_ID[recipe.species].aging;
  if (recipe.raw) return { live: 1, fresh: 0.8, aged: aging ? 1.25 : 0.5, spoiled: 0 }[stage];
  return { live: 1, fresh: 0.95, aged: aging ? 1.1 : 0.75, spoiled: 0 }[stage];
}

/** 이 요리에 쓸 가장 좋은 물고기. 요리 배율이 높은 것, 같으면 큰 것 */
export function bestFishFor(recipe: Recipe): FishItem | null {
  const list = S.cooler.filter((f) => f.species === recipe.species && stageOf(f.fresh) !== 'spoiled');
  if (list.length === 0) return null;
  return list.sort((a, b) => dishFresh(recipe, stageOf(b.fresh)) - dishFresh(recipe, stageOf(a.fresh)) || b.size - a.size)[0];
}

export function cookable(): Recipe[] {
  return RECIPES.filter((r) => bestFishFor(r) !== null);
}

export function averageQuality(results: SeasonResult[]): number {
  return results.reduce((a, r) => a + r.quality, 0) / results.length;
}

/** 요리값 = 메뉴 가격 × 신선도 배율 × (0.4 + 0.8 × 간 맞추기 정확도) */
export function dishPay(recipe: Recipe, stage: FreshStage, avg: number): number {
  return round100(recipe.price * dishFresh(recipe, stage) * (0.4 + 0.8 * avg));
}

/** 손님 기분 0 실망, 1 보통, 2 만족 */
export function moodOf(avg: number): 0 | 1 | 2 {
  return avg >= 0.75 ? 2 : avg >= 0.45 ? 1 : 0;
}

/** 손님 한마디. 모르는 맛은 ??? 로 보이고 감칠맛이 돌아오면 리뷰 전문이 보인다 */
export function reviewLines(recipe: Recipe, results: SeasonResult[], stage: FreshStage, mood: 0 | 1 | 2): string[] {
  const lines = recipe.steps.map((s, i) => {
    if (!canTaste(s.flavor)) return '???';
    const r = results[i];
    const idx = r.side === 0 || r.quality >= 0.8 ? 0 : r.side < 0 ? 1 : 2;
    return FLAVOR_REVIEW[s.flavor][idx];
  });
  if (tasteLevel() >= 3) {
    if (recipe.raw && stage === 'aged' && SPECIES_BY_ID[recipe.species].aging) lines.push('숙성이 잘 됐네요. 쫀득해요');
    else if (stage === 'live') lines.push('생선이 아주 싱싱해요');
    lines.push(mood === 2 ? '또 올게요!' : mood === 1 ? '먹을 만했어요' : '다음엔 더 잘해 줘요');
  }
  return lines;
}
