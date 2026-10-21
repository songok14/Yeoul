// 1장 물비늘포 어종 데이터

export type SpeciesId = 'bollak' | 'godeungeo' | 'noraemi' | 'hakgongchi' | 'gamseongdom';

/** 도트 물고기를 그리기 위한 생김새 정보 */
export interface FishLook {
  len: number;
  h: number;
  back: string;
  body: string;
  belly: string;
  fin: string;
  pattern: 'none' | 'wave' | 'band' | 'spot';
  patColor: string;
  bigEye?: boolean;
  jaw?: number;
  spiny?: boolean;
}

export interface Species {
  id: SpeciesId;
  name: string;
  minSize: number;
  maxSize: number;
  avgSize: number;
  /** 평균 크기 활어 한 마리 기준 경매가 */
  basePrice: number;
  /** 힘 1~5. 텐션이 오르는 속도와 저항 빈도에 영향 */
  strength: number;
  rare: boolean;
  /** 숙성하면 값이 오르는 어종 */
  aging: boolean;
  /** 금지체장(cm). 이 길이 이하는 놓아줘야 한다 */
  limitCm?: number;
  /** 금어기 [시작 월, 일, 끝 월, 일]. 이 기간에는 놓아줘야 한다 */
  closed?: [number, number, number, number];
  /** 거리별 입질 가중치 [근거리, 중거리, 원거리] */
  zone: [number, number, number];
  /** 시간대별 가중치 [새벽, 아침, 낮, 저녁] */
  slot: [number, number, number, number];
  desc: string;
  /** 도감 한 줄 힌트 */
  hint: string;
  look: FishLook;
}

export const SPECIES: Species[] = [
  {
    id: 'bollak',
    name: '볼락',
    minSize: 13,
    maxSize: 28,
    avgSize: 18,
    basePrice: 6000,
    strength: 2,
    rare: false,
    aging: false,
    limitCm: 15,
    zone: [5, 2, 0.4],
    slot: [1.7, 0.8, 0.5, 1.7],
    hint: '새벽·저녁 · 근거리',
    desc: '눈이 왕방울만 한 방파제 터줏대감. 해 뜨기 전과 해 질 녘에 테트라포드 틈에서 잘 문다. 아버지와 처음 낚은 물고기. 15cm 이하는 놓아준다.',
    look: { len: 16, h: 8, back: '#4a3b36', body: '#6e5a4f', belly: '#b9a48c', fin: '#33282a', pattern: 'spot', patColor: '#3a2d29', bigEye: true, spiny: true },
  },
  {
    id: 'godeungeo',
    name: '고등어',
    minSize: 20,
    maxSize: 40,
    avgSize: 28,
    basePrice: 4500,
    strength: 3,
    rare: false,
    aging: false,
    zone: [1, 5, 3],
    slot: [0.6, 1.6, 1.4, 0.8],
    hint: '아침·낮 · 중거리 떼고기',
    desc: '푸른 등에 물결무늬. 떼로 몰려다니고 힘이 좋다. 고양이 고등어는 이 물고기만 보면 눈빛이 달라진다.',
    look: { len: 20, h: 6, back: '#2f6f73', body: '#4f9a92', belly: '#dfe8e6', fin: '#234f52', pattern: 'wave', patColor: '#163a3c' },
  },
  {
    id: 'noraemi',
    name: '노래미',
    minSize: 18,
    maxSize: 36,
    avgSize: 24,
    basePrice: 5500,
    strength: 2,
    rare: false,
    aging: true,
    zone: [4, 3, 1],
    slot: [1, 1, 1, 1],
    hint: '언제나 · 근거리 · 숙성 어종',
    desc: '바위 밑바닥에 붙어 사는 갈색 물고기. 사계절 아무 때나 문다. 하루 이틀 숙성하면 감칠맛이 오른다.',
    look: { len: 18, h: 6, back: '#6b4f2a', body: '#8f6c3c', belly: '#d8c08e', fin: '#4d3818', pattern: 'spot', patColor: '#4d3818' },
  },
  {
    id: 'hakgongchi',
    name: '학공치',
    minSize: 25,
    maxSize: 40,
    avgSize: 30,
    basePrice: 4000,
    strength: 1,
    rare: false,
    aging: false,
    zone: [0.5, 2, 4],
    slot: [0.4, 1.2, 1.7, 0.8],
    hint: '낮 · 원거리 · 힘이 약함',
    desc: '바늘처럼 뾰족한 아래턱을 가진 은빛 물고기. 먼 물 수면에서 떼 지어 다닌다. 힘이 약해서 줄이 느슨하면 금방 빠진다.',
    look: { len: 24, h: 3, back: '#5f8fa8', body: '#a9c9d6', belly: '#eef5f7', fin: '#466d80', pattern: 'none', patColor: '#000000', jaw: 4 },
  },
  {
    id: 'gamseongdom',
    name: '감성돔',
    minSize: 18,
    maxSize: 52,
    avgSize: 34,
    basePrice: 28000,
    strength: 5,
    rare: true,
    aging: true,
    limitCm: 25,
    closed: [5, 1, 5, 31],
    zone: [0, 0.25, 1.1],
    slot: [1.8, 1, 0.5, 1.2],
    hint: '새벽 · 원거리 · 숙성 어종',
    desc: '방파제 꾼들의 꿈. 새벽 먼 물에서 드물게 모습을 보인다. 힘이 엄청나서 장비가 약하면 버티기 어렵다. 숙성회로 최고다. 25cm 이하와 5월 금어기에는 놓아줘야 한다.',
    look: { len: 18, h: 10, back: '#3c4148', body: '#7b838c', belly: '#c9ced3', fin: '#2a2e33', pattern: 'band', patColor: '#4b5259', spiny: true },
  },
];

export const SPECIES_BY_ID: Record<SpeciesId, Species> = Object.fromEntries(
  SPECIES.map((s) => [s.id, s]),
) as Record<SpeciesId, Species>;
