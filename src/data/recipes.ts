import type { SpeciesId } from './fish';

export type Flavor = 'salty' | 'sweet' | 'umami' | 'sour' | 'bitter';

export const FLAVOR_NAME: Record<Flavor, string> = {
  salty: '짠맛',
  sweet: '단맛',
  umami: '감칠맛',
  sour: '신맛',
  bitter: '쓴맛',
};

export const FLAVOR_TOOL: Record<Flavor, string> = {
  salty: '소금',
  sweet: '설탕',
  umami: '멸치 육수',
  sour: '식초',
  bitter: '쑥갓',
};

export interface RecipeStep {
  flavor: Flavor;
  /** 정답 구간 중심 (0~1). 요리마다 거의 고정이라 감으로도 익힐 수 있다 */
  target: number;
}

export interface Recipe {
  id: string;
  name: string;
  species: SpeciesId;
  price: number;
  /** 회 요리는 신선도에 더 민감하다 */
  raw: boolean;
  steps: RecipeStep[];
}

export const RECIPES: Recipe[] = [
  { id: 'bollak-gui', name: '볼락구이', species: 'bollak', price: 13_000, raw: false, steps: [{ flavor: 'salty', target: 0.58 }] },
  { id: 'godeungeo-jorim', name: '고등어조림', species: 'godeungeo', price: 15_000, raw: false, steps: [{ flavor: 'salty', target: 0.62 }, { flavor: 'sweet', target: 0.38 }] },
  { id: 'noraemi-maeun', name: '노래미 매운탕', species: 'noraemi', price: 17_000, raw: false, steps: [{ flavor: 'salty', target: 0.5 }, { flavor: 'umami', target: 0.72 }] },
  { id: 'hakgongchi-muchim', name: '학공치 회무침', species: 'hakgongchi', price: 15_000, raw: true, steps: [{ flavor: 'sweet', target: 0.45 }, { flavor: 'sour', target: 0.66 }] },
  { id: 'gamseongdom-hoe', name: '감성돔 숙성회', species: 'gamseongdom', price: 48_000, raw: true, steps: [{ flavor: 'umami', target: 0.4 }] },
];

export const RECIPE_BY_SPECIES: Record<SpeciesId, Recipe> = Object.fromEntries(
  RECIPES.map((r) => [r.species, r]),
) as Record<SpeciesId, Recipe>;

/** 맛 평가 한마디 [좋음, 모자람, 지나침] */
export const FLAVOR_REVIEW: Record<Flavor, [string, string, string]> = {
  salty: ['간이 딱 맞아요', '좀 싱거워요', '너무 짜요'],
  sweet: ['달큰하니 좋네요', '단맛이 모자라요', '너무 달아요'],
  umami: ['국물이 깊어요', '맛이 밍밍해요', '너무 진해요'],
  sour: ['새콤해서 입맛 돌아요', '새콤함이 부족해요', '너무 시큼해요'],
  bitter: ['쌉싸름해서 좋아요', '향이 약해요', '너무 써요'],
};
