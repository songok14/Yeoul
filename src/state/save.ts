// 세이브 데이터 구조와 localStorage 저장/불러오기

import { CH1_DEBT, SAVE_KEY, START_BAIT, START_MONEY } from '../config';
import type { SpeciesId } from '../data/fish';

export interface FishItem {
  uid: number;
  species: SpeciesId;
  size: number;
  caughtDay: number;
  /** 신선도 0~100 */
  fresh: number;
}

export interface DeliveryRequest {
  id: number;
  template: number;
  reward: number;
  deadline: number;
}

export interface DexEntry {
  count: number;
  best: number;
  released: number;
}

export interface DaySummary {
  day: number;
  earned: number;
  spoiled: number;
  expiredRequests: number;
}

export interface SaveData {
  version: 1;
  seed: number;
  day: number;
  /** 0 새벽, 1 아침, 2 낮, 3 저녁 */
  slot: number;
  money: number;
  debt: number;
  bait: number;
  rod: number;
  reel: number;
  tackle: number;
  cooler: FishItem[];
  nextUid: number;
  dex: Partial<Record<SpeciesId, DexEntry>>;
  recipesCooked: string[];
  dishesCooked: number;
  catAffinity: number;
  requests: DeliveryRequest[];
  nextRequestId: number;
  earnedToday: number;
  totalEarned: number;
  journalPages: number;
  pendingSummary: DaySummary | null;
  flags: Record<string, boolean>;
}

export function createSave(seed = Math.floor(Math.random() * 1e9)): SaveData {
  return {
    version: 1,
    seed,
    day: 1,
    slot: 0,
    money: START_MONEY,
    debt: CH1_DEBT,
    bait: START_BAIT,
    rod: 0,
    reel: 0,
    tackle: 0,
    cooler: [],
    nextUid: 1,
    dex: {},
    recipesCooked: [],
    dishesCooked: 0,
    catAffinity: 0,
    requests: [],
    nextRequestId: 1,
    earnedToday: 0,
    totalEarned: 0,
    journalPages: 0,
    pendingSummary: null,
    flags: {},
  };
}

/** 현재 게임 상태. 모든 씬이 이 객체를 직접 읽고 쓴다 */
export let S: SaveData = createSave();

export function setState(data: SaveData): void {
  S = data;
}

export function persist(): void {
  try {
    localStorage.setItem(SAVE_KEY, JSON.stringify(S));
  } catch (err) {
    console.warn('저장 실패', err);
  }
}

export function hasSave(): boolean {
  try {
    return localStorage.getItem(SAVE_KEY) !== null;
  } catch {
    return false;
  }
}

export function loadSave(): boolean {
  try {
    const raw = localStorage.getItem(SAVE_KEY);
    if (!raw) return false;
    const data = JSON.parse(raw) as Partial<SaveData>;
    if (data.version !== 1) return false;
    // 나중에 필드가 추가돼도 옛 세이브가 깨지지 않도록 기본값 위에 덮는다
    S = { ...createSave(data.seed), ...data } as SaveData;
    return true;
  } catch (err) {
    console.warn('불러오기 실패', err);
    return false;
  }
}

export function deleteSave(): void {
  try {
    localStorage.removeItem(SAVE_KEY);
  } catch {
    /* 무시 */
  }
}
