// 임시 도트 그림. 텍스처 생성과 배경 그리기 도우미

import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { SPECIES, type FishLook } from '../data/fish';
import { mixColor } from '../utils';
import { txt } from './ui';

type Palette = Record<string, string>;

function pixelTexture(scene: Phaser.Scene, key: string, rows: string[], pal: Palette): void {
  if (scene.textures.exists(key)) return;
  const h = rows.length;
  const w = Math.max(...rows.map((r) => r.length));
  const tex = scene.textures.createCanvas(key, w, h);
  if (!tex) return;
  const ctx = tex.getContext();
  rows.forEach((row, y) => {
    [...row].forEach((ch, x) => {
      const c = pal[ch];
      if (c) {
        ctx.fillStyle = c;
        ctx.fillRect(x, y, 1, 1);
      }
    });
  });
  tex.refresh();
  tex.setFilter(Phaser.Textures.FilterMode.NEAREST);
}

const PERSON_BACK = [
  '....hhhh....',
  '...hhhhhh...',
  '..hhhhhhhh..',
  '..hhhhhhhh..',
  '...hhhhhh...',
  '....ssss....',
  '..yyyyyyyy..',
  '.yyyyYyyyyy.',
  '.yyyyYyyyyy.',
  'syyyyYyyyyys',
  'syyyyYyyyyys',
  '.yyyyYyyyyy.',
  '..yyyyyyyy..',
  '..bbbbbbbb..',
  '..bbbbbbbb..',
  '..bbb..bbb..',
  '..bbb..bbb..',
  '..bbb..bbb..',
  '..kkk..kkk..',
  '.kkkk..kkkk.',
];

const PERSON_FRONT = [
  '....hhhh....',
  '..hhhhhhhh..',
  '.hhhhhhhhhh.',
  '.hhssssssshh',
  '.hsskssskshh',
  '.hssssssssh.',
  '..ssspssss..',
  '...ssssss...',
  '..yyyyyyyy..',
  '.yyyyYYyyyy.',
  'syyyyYYyyyys',
  'syyyyYYyyyys',
  '.yyyyYYyyyy.',
  '..yyyyyyyy..',
  '..bbbbbbbb..',
  '..bbb..bbb..',
  '..bbb..bbb..',
  '..bbb..bbb..',
  '..kkk..kkk..',
  '.kkkk..kkkk.',
];

const CAT = [
  '.k.........k..',
  'kgk.......kgk.',
  'kggkkkkkkkggk.',
  'kgdgggdgggdgk.',
  'kgyykgggkyygk.',
  'kggggwpwggggk.',
  '.kgggwwwgggk..',
  '..kkrkkkkkk...',
  '..kgrfdgdgk...',
  '.kgdwwwwwdgk..',
  '.kggwwwwwggk..',
  '.kgdgwwwgdgk.k',
  '.kggggggggkkgk',
  '..kkkkkkkkkkk.',
];

const NOSSI = [
  '....nnnnnn....',
  '..nnnnnnnnnn..',
  '..nnnnwnnnnn..',
  '.aaaaaaaaaaaa.',
  '..gsssssssssg.',
  '..gskksssskkg.',
  '..gsskssssksg.',
  '..sssssssssss.',
  '..ssswwwwwsss.',
  '...swwsswwss..',
  '...sssssssss..',
  '....ssssss....',
  '..oooooooooo..',
  '.oooowwwwoooo.',
  '.ooooowwooooo.',
  '.oooooooooooo.',
];

const BASE_PAL: Palette = {
  s: '#f0c8a0',
  k: '#2b2b2b',
  p: '#c9706a',
};

/** 손님 옷/머리 색 조합 */
const CUSTOMER_PALS: Palette[] = [
  { h: '#3b2a20', y: '#d96b5b', Y: '#b4503f', b: '#3d4a63' },
  { h: '#cfcfcf', y: '#7a9a5a', Y: '#5d7a42', b: '#5a4a3a' },
  { h: '#1d1d1d', y: '#5a7fc1', Y: '#3f5f9a', b: '#2f2f2f' },
  { h: '#8a5a3b', y: '#e3c35a', Y: '#b89838', b: '#6a4a6a' },
  { h: '#4a3020', y: '#e8e8e8', Y: '#c0c0c0', b: '#2f5689' },
  { h: '#2a1d1a', y: '#c47ab8', Y: '#9a5590', b: '#3b3b55' },
];
export const CUSTOMER_COUNT = CUSTOMER_PALS.length;

/** 물고기를 몸통 윤곽 함수로 한 픽셀씩 그린다 */
function fishTexture(scene: Phaser.Scene, key: string, L: FishLook): void {
  if (scene.textures.exists(key)) return;
  const jaw = L.jaw ?? 0;
  const W = jaw + L.len + 5;
  const H = L.h + 5;
  const tex = scene.textures.createCanvas(key, W, H);
  if (!tex) return;
  const ctx = tex.getContext();
  const px = (x: number, y: number, c: string) => {
    ctx.fillStyle = c;
    ctx.fillRect(x, y, 1, 1);
  };
  const cy = (H - 1) / 2 + 0.5;
  const x0 = jaw;
  const half = (x: number) => {
    const t = x / (L.len - 1);
    const k = t < 0.3 ? Math.sqrt(t / 0.3) * 0.8 + 0.2 : 1 - Math.pow((t - 0.3) / 0.7, 1.4) * 0.72;
    return (L.h / 2) * k;
  };

  // 꼬리
  const tailX = x0 + L.len - 1;
  const spread = L.h / 2 / 4 + 0.35;
  for (let i = 0; i < 5; i++) {
    const top = Math.round(cy - 0.6 - i * spread);
    const bot = Math.round(cy + 0.6 + i * spread);
    if (i < 2) for (let y = top; y <= bot; y++) px(tailX + i, y, L.fin);
    else {
      px(tailX + i, top, L.fin);
      px(tailX + i, top + 1, L.fin);
      px(tailX + i, bot, L.fin);
      px(tailX + i, bot - 1, L.fin);
    }
  }

  // 몸통
  for (let x = 0; x < L.len; x++) {
    const hh = half(x);
    const yTop = Math.round(cy - hh);
    const yBot = Math.round(cy + hh);
    const t = x / (L.len - 1);
    for (let y = yTop; y <= yBot; y++) {
      const rel = (y - yTop) / Math.max(1, yBot - yTop);
      let c = rel < 0.38 ? L.back : rel < 0.68 ? L.body : L.belly;
      if (L.pattern === 'wave' && rel < 0.45 && (x + (y % 2) * 2) % 4 === 0) c = L.patColor;
      if (L.pattern === 'band' && t > 0.18 && t < 0.85 && x % 4 === 1 && rel < 0.85) c = L.patColor;
      if (L.pattern === 'spot' && rel < 0.7 && (x * 7 + y * 13) % 5 === 0 && t > 0.15) c = L.patColor;
      if (y === yTop || y === yBot) c = L.fin;
      px(x0 + x, y, c);
    }
    // 등지느러미
    if (t > 0.28 && t < 0.78) {
      if (!L.spiny || x % 2 === 0) px(x0 + x, yTop - 1, L.fin);
      if (L.spiny && x % 2 === 0 && t < 0.6) px(x0 + x, yTop - 2, L.fin);
    }
  }

  // 가슴지느러미
  const pfx = x0 + Math.round(L.len * 0.32);
  px(pfx, Math.round(cy + 1), L.fin);
  px(pfx + 1, Math.round(cy + 1), L.fin);

  // 눈
  const ex = x0 + Math.max(1, Math.round(L.len * 0.12));
  const ey = Math.round(cy - half(ex - x0) * 0.35);
  if (L.bigEye) {
    px(ex, ey, '#f4f4f4');
    px(ex + 1, ey, '#f4f4f4');
    px(ex, ey + 1, '#f4f4f4');
    px(ex + 1, ey + 1, '#111111');
  } else {
    px(ex, ey, '#111111');
    px(ex + 1, ey, '#f4f4f4');
  }

  // 입과 학공치 아래턱
  px(x0, Math.round(cy), L.fin);
  for (let j = 1; j <= jaw; j++) px(x0 - j, Math.round(cy + 0.6), j === jaw ? '#d9534f' : L.fin);

  tex.refresh();
  tex.setFilter(Phaser.Textures.FilterMode.NEAREST);
}

export function createTextures(scene: Phaser.Scene): void {
  pixelTexture(scene, 'yeoul-back', PERSON_BACK, { ...BASE_PAL, h: '#2a1d1a', y: '#f2c14e', Y: '#c99a2e', b: '#34507a' });
  pixelTexture(scene, 'yeoul', PERSON_FRONT, { ...BASE_PAL, h: '#2a1d1a', y: '#f2c14e', Y: '#c99a2e', b: '#34507a' });
  pixelTexture(scene, 'grandma', PERSON_FRONT, { ...BASE_PAL, h: '#d7d7d7', y: '#8a6aa8', Y: '#f4ecd8', b: '#5a4a6a' });
  pixelTexture(scene, 'cat', CAT, {
    k: '#2b2b2b', g: '#8d939b', d: '#4f555c', w: '#f4f4f4', y: '#d8c040', p: '#e89a9a', r: '#d9534f', f: '#f4f4f4',
  });
  pixelTexture(scene, 'nossi', NOSSI, {
    ...BASE_PAL, n: '#2f3d5a', w: '#f4f4f4', a: '#1e2840', g: '#bdbdbd', s: '#d8a77a', o: '#6b6b3a',
  });
  CUSTOMER_PALS.forEach((pal, i) => pixelTexture(scene, `cust${i}`, PERSON_FRONT, { ...BASE_PAL, ...pal }));
  for (const sp of SPECIES) fishTexture(scene, `fish-${sp.id}`, sp.look);

  // 1픽셀 흰 점 (파티클, 물비늘용)
  if (!scene.textures.exists('dot')) {
    const tex = scene.textures.createCanvas('dot', 2, 2);
    if (tex) {
      const ctx = tex.getContext();
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, 2, 2);
      tex.refresh();
    }
  }
}

export interface SkyColors {
  top: number;
  bot: number;
  seaTop: number;
  seaBot: number;
  sun: number;
  sunY: number;
  dark: boolean;
}

export const SKY: SkyColors[] = [
  { top: 0x1c2a4a, bot: 0x7a6390, seaTop: 0x3b4f7a, seaBot: 0x1a2c4a, sun: 0xf6e7b0, sunY: 0.85, dark: true },
  { top: 0x76bde3, bot: 0xf6d6a0, seaTop: 0x5aa7cf, seaBot: 0x2a6286, sun: 0xffe9a8, sunY: 0.6, dark: false },
  { top: 0x4fa6e3, bot: 0xbfe6f7, seaTop: 0x4596c6, seaBot: 0x24618c, sun: 0xfff6d0, sunY: 0.2, dark: false },
  { top: 0x3b3a6b, bot: 0xf08a5d, seaTop: 0x8a6a8a, seaBot: 0x2e3a64, sun: 0xff9a4a, sunY: 0.8, dark: false },
];

/** 위에서 아래로 색이 계단처럼 변하는 띠 */
export function bands(g: Phaser.GameObjects.Graphics, x: number, y: number, w: number, h: number, top: number, bot: number, step = 6): void {
  const n = Math.max(1, Math.ceil(h / step));
  for (let i = 0; i < n; i++) {
    g.fillStyle(mixColor(top, bot, n === 1 ? 0 : i / (n - 1)), 1);
    g.fillRect(x, y + i * step, w, Math.min(step, h - i * step));
  }
}

/** 하늘, 해, 먼 섬, 바다를 한 번에 그린다 */
export function drawSeascape(scene: Phaser.Scene, slot: number, skyTop: number, horizon: number, seaBottom: number): Phaser.GameObjects.Graphics {
  const s = SKY[slot] ?? SKY[0];
  const g = scene.add.graphics();
  bands(g, 0, skyTop, GAME_W, horizon - skyTop, s.top, s.bot, 8);

  // 해 또는 달
  const sunX = slot === 3 ? 330 : slot === 0 ? 110 : 300;
  const sunY = skyTop + (horizon - skyTop) * s.sunY;
  g.fillStyle(s.sun, 0.35);
  g.fillCircle(sunX, sunY, 30);
  g.fillStyle(s.sun, 1);
  g.fillCircle(sunX, sunY, 20);

  // 별 (새벽)
  if (slot === 0) {
    g.fillStyle(C.white, 0.8);
    for (let i = 0; i < 18; i++) g.fillRect((i * 97) % GAME_W, skyTop + ((i * 53) % Math.max(10, horizon - skyTop - 30)), 2, 2);
  }

  // 먼 섬 실루엣
  const isl = mixColor(s.bot, 0x1b2a41, 0.55);
  g.fillStyle(isl, 1);
  g.fillTriangle(10, horizon, 70, horizon - 26, 150, horizon);
  g.fillTriangle(90, horizon, 130, horizon - 14, 190, horizon);
  g.fillTriangle(300, horizon, 360, horizon - 18, 430, horizon);

  bands(g, 0, horizon, GAME_W, seaBottom - horizon, s.seaTop, s.seaBot, 8);
  // 해 그림자
  g.fillStyle(s.sun, 0.25);
  for (let i = 0; i < 6; i++) g.fillRect(sunX - 24 + (i % 2) * 8, horizon + 6 + i * 10, 48 - (i % 2) * 16, 3);
  return g;
}

/** 반짝이는 물비늘 */
export function addSparkles(scene: Phaser.Scene, x: number, y: number, w: number, h: number, count = 22): void {
  for (let i = 0; i < count; i++) {
    const sx = x + Math.random() * w;
    const sy = y + Math.random() * h;
    const len = 4 + Math.round(Math.random() * 8);
    const r = scene.add.rectangle(sx, sy, len, 2, C.foam, 0.8).setAlpha(0);
    scene.tweens.add({
      targets: r,
      alpha: { from: 0, to: 0.85 },
      x: sx + 6,
      duration: 900 + Math.random() * 900,
      yoyo: true,
      repeat: -1,
      delay: Math.random() * 2000,
    });
  }
}

/** 여울호 1톤 트럭 (오른쪽 화물칸이 주방) */
export function drawTruck(scene: Phaser.Scene, x: number, y: number, s = 1): Phaser.GameObjects.Container {
  const c = scene.add.container(x, y);
  const g = scene.add.graphics();
  const r = (rx: number, ry: number, w: number, h: number, col: number) => {
    g.fillStyle(col, 1);
    g.fillRect(rx * s, ry * s, w * s, h * s);
  };
  // 그림자
  g.fillStyle(C.black, 0.25);
  g.fillEllipse(72 * s, 68 * s, 156 * s, 14 * s);
  // 운전석
  r(0, 18, 42, 40, C.ink);
  r(2, 20, 38, 36, 0x8fc1d4);
  r(6, 24, 26, 14, 0xd8f1f7);
  r(2, 46, 38, 4, 0x6a9fb3);
  // 화물칸 주방
  r(44, 0, 100, 62, C.ink);
  r(46, 2, 96, 58, 0xf4ecd8);
  r(56, 14, 76, 26, C.ink);
  r(58, 16, 72, 22, 0x5e3b26);
  // 차양 줄무늬
  for (let i = 0; i < 8; i++) r(52 + i * 10, 6, 10, 8, i % 2 === 0 ? C.red : C.white);
  // 바퀴
  g.fillStyle(C.ink, 1);
  g.fillCircle(20 * s, 60 * s, 9 * s);
  g.fillCircle(126 * s, 60 * s, 9 * s);
  g.fillStyle(C.gray, 1);
  g.fillCircle(20 * s, 60 * s, 4 * s);
  g.fillCircle(126 * s, 60 * s, 4 * s);
  c.add(g);
  const label = txt(scene, 86 * s, 49 * s, '여울호', { size: s >= 1.5 ? 'h1' : 'small', color: '#2f5689', origin: [0.5, 0.5] });
  c.add(label);
  return c;
}

/** 빨간 등대 */
export function drawLighthouse(g: Phaser.GameObjects.Graphics, x: number, y: number, s = 1): void {
  g.fillStyle(C.ink, 1);
  g.fillRect(x - 9 * s, y - 60 * s, 18 * s, 60 * s);
  for (let i = 0; i < 4; i++) {
    g.fillStyle(i % 2 === 0 ? C.red : C.white, 1);
    g.fillRect(x - 7 * s, y - 58 * s + i * 14 * s, 14 * s, 14 * s);
  }
  g.fillStyle(C.ink, 1);
  g.fillRect(x - 11 * s, y - 72 * s, 22 * s, 14 * s);
  g.fillStyle(C.gold, 1);
  g.fillRect(x - 7 * s, y - 70 * s, 14 * s, 10 * s);
  g.fillStyle(C.redDark, 1);
  g.fillTriangle(x - 12 * s, y - 72 * s, x + 12 * s, y - 72 * s, x, y - 84 * s);
}

/** 표정 얼굴 (0 실망, 1 보통, 2 만족) */
export function drawFace(scene: Phaser.Scene, x: number, y: number, mood: 0 | 1 | 2): Phaser.GameObjects.Graphics {
  const g = scene.add.graphics();
  const col = mood === 2 ? C.gold : mood === 1 ? 0xbfc7cf : 0x9fb6d9;
  g.fillStyle(C.ink, 1);
  g.fillCircle(x, y, 20);
  g.fillStyle(col, 1);
  g.fillCircle(x, y, 17);
  g.fillStyle(C.ink, 1);
  g.fillRect(x - 8, y - 6, 4, 5);
  g.fillRect(x + 4, y - 6, 4, 5);
  if (mood === 2) {
    g.fillRect(x - 8, y + 5, 16, 3);
    g.fillRect(x - 10, y + 2, 3, 4);
    g.fillRect(x + 7, y + 2, 3, 4);
  } else if (mood === 1) {
    g.fillRect(x - 7, y + 6, 14, 3);
  } else {
    g.fillRect(x - 8, y + 8, 16, 3);
    g.fillRect(x - 10, y + 10, 3, 3);
    g.fillRect(x + 7, y + 10, 3, 3);
  }
  return g;
}

export function slotTextColor(slot: number): string {
  return SKY[slot]?.dark ? T.paper : T.ink;
}
