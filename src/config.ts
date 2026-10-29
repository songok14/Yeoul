// 화면과 색, 글꼴 같은 전역 상수

export const GAME_W = 450;
export const GAME_H = 800;

export const FONT = 'NanumSquareRound';
export const FONT_BOLD = 'Jua';
export const FONT_SMALL = 'NanumSquareRound';
/** 실제 캔버스는 논리 크기의 몇 배로 그리는지. 일러스트와 글자를 선명하게 */
export const RENDER_SCALE = 2;

/** 숫자 색 (Graphics용) */
export const C = {
  night: 0x1b2a41,
  deep: 0x12304a,
  sea: 0x2e6f95,
  seaLight: 0x4ea3c7,
  foam: 0xd8f1f7,
  sand: 0xe8d5a3,
  paper: 0xf4ecd8,
  paperDark: 0xd9cba8,
  ink: 0x2b2b2b,
  inkSoft: 0x5b5148,
  wood: 0x8a5a3b,
  woodDark: 0x5e3b26,
  red: 0xd9534f,
  redDark: 0x9e3330,
  gold: 0xf2c14e,
  green: 0x6abf69,
  greenDark: 0x3f8a4a,
  blue: 0x4a7fc1,
  blueDark: 0x2f5689,
  gray: 0x8d939b,
  grayDark: 0x5c6269,
  concrete: 0xb7b2a6,
  concreteDark: 0x857f74,
  white: 0xffffff,
  black: 0x000000,
} as const;

/** 글자 색 (Text용) */
export const T = {
  paper: '#f4ecd8',
  ink: '#2b2b2b',
  inkSoft: '#5b5148',
  gold: '#f2c14e',
  red: '#e0605b',
  green: '#7fd17e',
  blue: '#8cc8ff',
  gray: '#a7adb5',
  white: '#ffffff',
} as const;

export const SAVE_KEY = 'hanjeopsi-save-v1';

/** 1장 목표: 30일 안에 1차 상환금 300만 원 */
export const CH1_DEADLINE = 30;
export const CH1_DEBT = 3_000_000;
export const START_MONEY = 20_000;
export const START_BAIT = 12;
export const CASTS_PER_TRIP = 6;
export const CUSTOMERS_PER_SHIFT = 5;
