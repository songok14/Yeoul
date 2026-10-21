// 시안 2: 고퀄 픽셀아트 (225×400 도트 격자, 노을, 디더링, 빛 번짐, Galmuri)

import Phaser from 'phaser';
import { GALMURI_BOLD as FONT_BOLD, GALMURI_SMALL as FONT_SMALL } from './galmuri';
import { canvasTex, rng, type Ctx } from './paint';

const W = 225;
const H = 400;
const BAYER = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];
const SKY = ['#221f48', '#2e2a5c', '#423170', '#5e3a7a', '#83437c', '#ab5277', '#d26a6d', '#ec8c6a', '#f6b077', '#fbd08a'];
const INK = '#3b2416';

let c: Ctx;
const R = (x: number, y: number, w: number, h: number, col: string) => {
  c.fillStyle = col;
  c.fillRect(x, y, w, h);
};
const P = (x: number, y: number, col: string) => R(x, y, 1, 1, col);
const bayer = (x: number, y: number) => BAYER[(y & 3) * 4 + (x & 3)] / 16;

/** 디더링으로 섞은 세로 그라데이션 */
function ditherV(x0: number, y0: number, w: number, h: number, pal: string[]): void {
  for (let y = 0; y < h; y++) {
    const t = (y / Math.max(1, h - 1)) * (pal.length - 1);
    const i = Math.min(pal.length - 2, Math.floor(t));
    const f = t - i;
    for (let x = 0; x < w; x++) P(x0 + x, y0 + y, bayer(x0 + x, y0 + y) < f ? pal[i + 1] : pal[i]);
  }
}

/** 원 모양 빛 번짐 (가장자리로 갈수록 성기게) */
function halo(cx: number, cy: number, r0: number, r1: number, col: string, strength = 0.9): void {
  for (let y = Math.floor(cy - r1); y <= cy + r1; y++)
    for (let x = Math.floor(cx - r1); x <= cx + r1; x++) {
      const d = Math.hypot(x - cx, y - cy);
      if (d < r0 || d > r1) continue;
      const a = (1 - (d - r0) / (r1 - r0)) * strength;
      if (bayer(x, y) < a) P(x, y, col);
    }
}

function disc(cx: number, cy: number, r: number, col: string, clipY = Infinity): void {
  for (let y = Math.floor(cy - r); y <= cy + r; y++)
    for (let x = Math.floor(cx - r); x <= cx + r; x++) if (Math.hypot(x - cx, y - cy) <= r && y < clipY) P(x, y, col);
}

function cloud(x: number, y: number, w: number): void {
  const rows = [
    [6, w - 14],
    [2, w - 6],
    [0, w],
    [3, w - 4],
  ];
  rows.forEach(([dx, rw], i) => {
    R(x + dx, y + i * 2, rw, 2, i === 0 ? '#6a3f78' : i === 3 ? '#f19a6c' : '#a8567a');
  });
  R(x + 4, y + 7, w - 10, 1, '#fbd08a');
}

/** 문자열 도트 스프라이트 */
function sprite(x: number, y: number, rows: string[], pal: Record<string, string>): void {
  rows.forEach((row, j) => [...row].forEach((ch, i) => pal[ch] && P(x + i, y + j, pal[ch])));
}

function txt(s: string, x: number, y: number, font: string, col: string, align: CanvasTextAlign = 'left'): number {
  c.font = font;
  c.fillStyle = col;
  c.textAlign = align;
  c.textBaseline = 'top';
  c.fillText(s, Math.round(x), Math.round(y));
  return Math.round(c.measureText(s).width);
}

function woodPanel(x: number, y: number, w: number, h: number): void {
  R(x, y, w, h, '#2b1b14');
  R(x + 1, y + 1, w - 2, h - 2, '#7a4b2e');
  R(x + 1, y + 1, w - 2, 1, '#a5683f');
  for (let i = 0; i < 6; i++) R(x + 2 + ((i * 37) % (w - 20)), y + 2 + (i % 3), 12, 1, '#6a3f26');
}

function parchment(x: number, y: number, w: number, h: number): void {
  R(x, y, w, h, '#2b1b14');
  R(x + 1, y + 1, w - 2, h - 2, '#f3e2bb');
  R(x + 1, y + 1, w - 2, 1, '#fff4d6');
  R(x + 1, y + h - 3, w - 2, 2, '#d9c193');
}

const ICONS: Record<string, string[]> = {
  rod: ['......#..', '.....#.#.', '....#..#.', '...#...#.', '..#....#.', '.#....##.', '#........', '.........', '.........'],
  fish: ['.........', '..####...', '.#..###.#', '#.######.', '########.', '.######.#', '..####...', '.........', '.........'],
  truck: ['.........', '#####....', '#####.##.', '#####.###', '#########', '#########', '.#....#..', '.........', '.........'],
  bait: ['.........', '.##......', '#..#.....', '...#..##.', '....##..#', '........#', '.......#.', '.........', '.........'],
  bowl: ['..#.#....', '.#.#.....', '..#.#....', '.........', '#########', '.#######.', '..#####..', '...###...', '.........'],
  book: ['.#######.', '.#.....#.', '.#.###.#.', '.#.....#.', '.#.###.#.', '.#.....#.', '.#######.', '.........', '.........'],
};

function drawAll(ctx: Ctx): void {
  c = ctx;
  const r = rng(11);

  // 하늘과 해
  ditherV(0, 0, W, 152, SKY);
  halo(150, 150, 20, 34, '#fbd08a', 0.55);
  disc(150, 150, 20, '#ffd98a', 151);
  disc(150, 150, 15, '#ffe9b0', 151);
  for (let i = 0; i < 20; i++) P(Math.floor(r() * W), Math.floor(r() * 50), r() > 0.5 ? '#c8c0e8' : '#8a84c0');
  cloud(18, 70, 54);
  cloud(96, 52, 38);
  cloud(176, 92, 44);
  // 새
  for (const [bx, by] of [[60, 98], [68, 94], [190, 70]]) {
    P(bx, by, '#3a2752');
    P(bx + 1, by + 1, '#3a2752');
    P(bx + 2, by, '#3a2752');
  }

  // 먼 섬
  const island = (x0: number, x1: number, top: number, col: string) => {
    for (let x = x0; x <= x1; x++) {
      const t = (x - x0) / (x1 - x0);
      const h = Math.round(Math.sin(t * Math.PI) * (152 - top) * (0.85 + 0.15 * Math.sin(t * 9)));
      R(x, 152 - h, 1, h, col);
    }
  };
  island(-10, 80, 128, '#5a3a70');
  island(40, 120, 138, '#4a2f63');
  island(170, 240, 132, '#5a3a70');

  // 바다
  ditherV(0, 152, W, 58, ['#6a4a86', '#4f3f78', '#3a3468', '#2a2a55']);
  for (let i = 0; i < 9; i++) {
    const w = 30 - i * 3 + (i % 2) * 6;
    for (let x = 0; x < w; x += 3 + (i % 2)) R(150 - w / 2 + x, 154 + i * 5, 2, 1, i < 3 ? '#ffe3a3' : '#f6b077');
  }
  for (let i = 0; i < 30; i++) R(Math.floor(r() * W), 156 + Math.floor(r() * 50), 2 + Math.floor(r() * 3), 1, '#7a5a96');
  // 작은 배 그림자
  sprite(40, 162, ['....#.....', '....##....', '..#####...', '##########', '.########.'], { '#': '#261c40' });

  // 방파제와 등대
  R(150, 176, 75, 3, '#9a8590');
  R(150, 179, 75, 8, '#5c5068');
  for (let i = 0; i < 6; i++) sprite(152 + i * 12, 183, ['..#..', '.###.', '#####'], { '#': i % 2 ? '#4a4058' : '#6a5a78' });
  const lx = 206;
  halo(lx, 136, 5, 13, '#f8d090', 0.55);
  for (let y = 142; y < 177; y++) {
    const half = 4 + Math.floor((y - 142) / 12);
    for (let x = -half; x < half; x++) {
      const band = Math.floor((y - 142) / 8) % 2 === 1;
      let col = band ? '#c8404a' : '#ece2ea';
      if (x <= -half + 1) col = band ? '#ff7a6a' : '#ffd8c0';
      if (x >= half - 2) col = band ? '#8a2a3a' : '#a898b0';
      P(lx + x, y, col);
    }
  }
  R(lx - 6, 140, 12, 2, '#2b2234');
  R(lx - 3, 132, 6, 8, '#fff3b0');
  R(lx - 3, 132, 1, 8, '#ffffff');
  R(lx - 4, 130, 8, 2, '#8a2a3a');
  R(lx - 2, 128, 4, 2, '#c8404a');

  // 전봇대와 전깃줄
  for (let x = 0; x < W; x++) {
    const sag = Math.sin(((x < 128 ? x / 128 : (x - 128) / 97) * Math.PI)) * 5;
    P(x, Math.round(157 + sag), '#231a30');
    P(x, Math.round(161 + sag), '#231a30');
  }
  R(127, 150, 3, 112, '#3a2a30');
  R(128, 150, 1, 112, '#5a4048');
  R(120, 156, 17, 2, '#3a2a30');

  // 바닷가 축대
  R(0, 186, W, 2, '#a08a92');
  R(0, 188, W, 10, '#6b5a6e');
  for (let x = 0; x < W; x += 9) R(x, 188 + ((x / 9) % 2) * 5, 1, 5, '#55465a');

  // 마당 돌바닥
  R(0, 198, W, 92, '#6e5560');
  for (let y = 200; y < 290; y += 5)
    for (let x = (y / 5) % 2 ? 0 : 5; x < W; x += 10) {
      R(x, y, 9, 1, '#5e4652');
      P(x + 9, y + 1, '#5e4652');
    }

  // 위판장
  R(2, 200, 80, 14, '#2a4b7d');
  for (let x = 3; x < 81; x++) R(x, 201, 1, 12, x % 3 === 0 ? '#5b8fd0' : '#3e6aa8');
  R(2, 200, 80, 1, '#f19a6c');
  R(2, 214, 80, 1, '#1e3558');
  R(6, 215, 72, 47, '#b8c4cc');
  R(6, 215, 2, 47, '#dbc2b8');
  R(74, 215, 4, 47, '#8a98a4');
  ditherV(12, 224, 60, 38, ['#6a4848', '#4a3444', '#2a2638', '#2a2638']);
  for (const bx of [26, 56]) {
    halo(bx + 1, 230, 2, 6, '#8a5a50', 0.6);
    R(bx, 229, 2, 2, '#fff3b0');
  }
  const crate = (x: number, y: number, col: string, hi: string) => {
    R(x, y, 14, 8, '#1e2038');
    R(x + 1, y + 1, 12, 6, col);
    R(x + 1, y + 1, 12, 1, hi);
    R(x + 3, y + 2, 8, 1, '#e8eef5');
  };
  crate(15, 253, '#3f78b8', '#6aa0d8');
  crate(30, 253, '#d8a03a', '#f2c66a');
  crate(45, 253, '#3f78b8', '#6aa0d8');
  crate(22, 245, '#d8a03a', '#f2c66a');
  crate(37, 245, '#3f78b8', '#6aa0d8');
  parchment(22, 214, 40, 13);
  txt('위판장', 42, 215, `10px ${FONT_SMALL}`, INK, 'center');

  // 미끼가게
  R(84, 212, 46, 4, '#3a2418');
  R(84, 212, 46, 1, '#f19a6c');
  R(86, 216, 42, 46, '#9a6a44');
  for (let y = 219; y < 262; y += 4) R(86, y, 42, 1, '#7a5030');
  R(86, 216, 2, 46, '#c8906a');
  for (let i = 0; i < 7; i++) R(85 + i * 6, 222, 6, 6, i % 2 ? '#e8dcc0' : '#4f9a5a');
  for (let i = 0; i < 7; i++) R(86 + i * 6, 228, 4, 1, i % 2 ? '#e8dcc0' : '#4f9a5a');
  R(84, 221, 46, 1, '#2e6a3a');
  R(90, 234, 16, 12, '#2b1b14');
  R(91, 235, 14, 10, '#ffd36b');
  R(98, 235, 1, 10, '#c89a4a');
  R(91, 240, 14, 1, '#c89a4a');
  R(110, 238, 14, 24, '#4a2e1e');
  R(111, 239, 12, 23, '#6a4028');
  P(120, 250, '#f2c14e');
  sprite(88, 252, ['.###.', '#ooo#', '#ooo#', '.###.'], { '#': '#2b1b14', o: '#4a7fc1' });
  parchment(92, 209, 28, 13);
  txt('미끼', 106, 210, `10px ${FONT_SMALL}`, INK, 'center');

  // 여울식당 (한옥)
  R(134, 256, 88, 6, '#8a8090');
  R(134, 256, 88, 1, '#a89aa8');
  R(138, 218, 80, 38, '#efd9b8');
  R(138, 218, 80, 2, '#c8a888');
  for (const px of [138, 165, 192, 214]) {
    R(px, 218, 4, 38, '#6a4028');
    R(px, 218, 1, 38, '#9a6040');
  }
  // 지붕: 처마 끝이 살짝 들린 곡선
  for (let x = 128; x < 226; x++) {
    const t = (x - 128) / 97;
    const top = Math.round(198 + Math.pow(Math.abs(t - 0.5) * 2, 3) * -5 + Math.sin(t * Math.PI) * -4);
    const bottom = Math.round(216 - Math.pow(Math.abs(t - 0.5) * 2, 4) * 6);
    for (let y = top; y <= bottom; y++) {
      let col = (y - top) % 4 < 2 ? '#3c4252' : '#4c5468';
      if (x % 4 === 0) col = '#2c3040';
      if (y === top) col = '#f19a6c';
      if (y === bottom || y === bottom - 1) col = '#2a2430';
      P(x, y, col);
    }
  }
  const hanji = (x: number) => {
    R(x, 228, 14, 16, '#5a3a24');
    R(x + 1, 229, 12, 14, '#ffe1a0');
    for (let i = 1; i < 4; i++) R(x + 1, 229 + i * 3.5, 12, 1, '#c9a46a');
    for (let i = 1; i < 3; i++) R(x + i * 4.3, 229, 1, 14, '#c9a46a');
  };
  hanji(145);
  hanji(197);
  R(170, 230, 18, 26, '#3a2418');
  R(171, 231, 16, 25, '#6a4028');
  R(169, 226, 20, 12, '#33468a');
  R(178, 226, 2, 12, '#1e2a5a');
  disc(174, 231, 1.5, '#e8e0f0');
  disc(184, 231, 1.5, '#e8e0f0');
  // 붉은 초롱
  halo(162, 226, 4, 9, '#c8605a', 0.5);
  R(161, 219, 1, 3, '#2b1b14');
  R(159, 222, 6, 8, '#d8483f');
  R(159, 222, 1, 8, '#ff8a6a');
  R(159, 224, 6, 1, '#a82a2a');
  R(159, 227, 6, 1, '#a82a2a');
  parchment(155, 206, 46, 13);
  txt('여울식당', 178, 207, `10px ${FONT_SMALL}`, INK, 'center');
  // 동백 화분
  const camellia = (x: number) => {
    sprite(x, 248, ['.ggrg.', 'grggrg', 'gggrgg', '.gggg.', '.bbbb.', '.bbbb.', '..bb..', '......'], {
      g: '#3a6a44', r: '#e04a5a', b: '#8a5a3a',
    });
  };
  camellia(132);
  camellia(212);

  // 가로등
  R(4, 252, 2, 38, '#2b2234');
  R(2, 248, 6, 4, '#2b2234');
  R(3, 249, 4, 2, '#fff3b0');
  halo(5, 251, 3, 9, '#c89a6a', 0.55);

  // 길
  R(0, 288, W, 2, '#8a7a80');
  R(0, 290, W, 24, '#3e3a4e');
  for (let x = 4; x < W; x += 22) R(x, 301, 10, 1, '#c8b090');
  for (let i = 0; i < 40; i++) P(Math.floor(r() * W), 291 + Math.floor(r() * 22), '#4a4658');
  R(0, 314, W, 8, '#6e5560');
  R(0, 314, W, 1, '#8a7a80');

  // 여울호
  ditherV(66, 304, 90, 4, ['#2a2438', '#3e3a4e']);
  R(70, 280, 26, 22, '#2b1b14');
  R(71, 281, 24, 20, '#6aa8c8');
  R(71, 281, 24, 1, '#9ad0e8');
  R(71, 296, 24, 5, '#4a7898');
  R(74, 284, 14, 8, '#c8e8f8');
  R(75, 285, 3, 6, '#f8c07a');
  R(94, 266, 60, 36, '#2b1b14');
  R(95, 267, 58, 34, '#efe2c6');
  R(95, 267, 2, 34, '#fff4d6');
  R(95, 296, 58, 5, '#c8b898');
  for (let i = 0; i < 10; i++) R(96 + i * 6, 269, 6, 6, i % 2 ? '#efe2c6' : '#d8483f');
  for (let i = 0; i < 10; i++) R(97 + i * 6, 275, 4, 1, i % 2 ? '#efe2c6' : '#d8483f');
  R(100, 278, 48, 11, '#3a2418');
  R(104, 282, 8, 5, '#a8b0c0');
  R(116, 281, 10, 6, '#a8b0c0');
  P(108, 280, '#e8e8e8');
  P(120, 279, '#e8e8e8');
  txt('여울호', 124, 290, `10px ${FONT_SMALL}`, '#2f4a80', 'center');
  for (const wx of [80, 142]) {
    disc(wx, 302, 5, '#1e1a28');
    disc(wx, 302, 2, '#8a8a98');
  }

  // 여울과 고등어
  sprite(44, 286, [
    '....hhhh....',
    '..hhhhhhhh..',
    '.hhhhhhhhhh.',
    '.hHssssssHh.',
    '.hskssssksh.',
    '.hssssssssh.',
    '..sspsssss..',
    '...ssssss...',
    '..yyyyyyyy..',
    '.YyyyyOyyyy.',
    'sYyyyyOyyyys',
    'sYyyyyOyyyys',
    '.YyyyyOyyyd.',
    '..yyyyyyyd..',
    '..bbbbbbbb..',
    '..Bbb..bbb..',
    '..Bbb..bbb..',
    '..kkk..kkk..',
    '.kkkk..kkkk.',
  ], { h: '#2a1d1a', H: '#5a3a30', s: '#f0c8a0', k: '#2b2b2b', p: '#d07070', y: '#f2c14e', Y: '#ffe08a', O: '#c99a2e', d: '#b8862a', b: '#34507a', B: '#4a6a98' });
  sprite(164, 294, [
    '.k.....k....',
    'kgk...kgk...',
    'kggkkkggk...',
    'kgyggygggk.k',
    'kggpgggggkgk',
    '.kwwrwggggk.',
    '.kwwfgdggk..',
    '.kgggdgggk..',
    '..kkkkkkk...',
  ], { k: '#2b2b2b', g: '#9aa3ae', d: '#5f6670', y: '#d8c040', p: '#e89a9a', w: '#f4f4f4', r: '#d8483f', f: '#f4f4f4' });

  // 상단 나무 패널
  woodPanel(0, 0, W, 46);
  parchment(3, 3, W - 6, 40);
  txt('4일차', 8, 6, `12px ${FONT_BOLD}`, INK);
  const slots = ['새벽', '아침', '낮', '저녁'];
  let sx = 44;
  slots.forEach((s, i) => {
    c.font = `10px ${FONT_SMALL}`;
    const w = Math.round(c.measureText(s).width) + 4;
    if (i === 3) {
      R(sx - 1, 5, w + 2, 14, '#2b1b14');
      R(sx, 6, w, 12, '#d8483f');
      R(sx, 6, w, 1, '#ff8a6a');
    }
    txt(s, sx + 2, 7, `10px ${FONT_SMALL}`, i === 3 ? '#fff4d6' : i < 3 ? '#a8906a' : INK);
    sx += w + 4;
  });
  txt('D-26', W - 8, 6, `12px ${FONT_BOLD}`, '#c8343a', 'right');
  sprite(8, 26, ['..###..', '.#ooo#.', '#oo#oo#', '#o###o#', '#oo#oo#', '.#ooo#.', '..###..'], { '#': '#8a5a10', o: '#f2c14e' });
  txt('86,000원', 18, 23, `12px ${FONT_BOLD}`, INK);
  txt('빚 2,640,000원', W - 8, 23, `10px ${FONT_SMALL}`, '#7a5a40', 'right');
  R(150, 37, 67, 3, '#c9ad7a');
  R(150, 37, 8, 3, '#4f9a5a');

  // 소식 띠
  R(0, 46, W, 14, 'rgba(34,24,40,0.72)');
  R(0, 59, W, 1, '#2b1b14');
  txt('소식', 6, 48, `10px ${FONT_SMALL}`, '#f2c14e');
  txt('노래미 값이 23% 떨어졌다', 30, 48, `10px ${FONT_SMALL}`, '#f3e2bb');

  // 아래 메뉴
  woodPanel(0, 322, W, 78);
  const menu: [string, string, string, string][] = [
    ['방파제', '낚시·1칸', '#3f78b8', 'rod'],
    ['위판장', '경매 마감', '#2e8a7a', 'fish'],
    ['여울호', '장사·1칸', '#c8404a', 'truck'],
    ['미끼가게', '장비·미끼', '#4f9a5a', 'bait'],
    ['여울식당', '상환·휴식', '#c8862e', 'bowl'],
    ['수첩', '도감·미각', '#6a5a9a', 'book'],
  ];
  menu.forEach(([label, sub, col, ic], i) => {
    const x = 3 + (i % 3) * 74;
    const y = 326 + Math.floor(i / 3) * 36;
    parchment(x, y, 71, 33);
    R(x + 2, y + 4, 13, 13, '#2b1b14');
    R(x + 3, y + 5, 11, 11, col);
    R(x + 3, y + 5, 11, 1, 'rgba(255,255,255,0.35)');
    sprite(x + 4, y + 6, ICONS[ic], { '#': '#fff4d6' });
    txt(label, x + 18, y + 3, `12px ${FONT_BOLD}`, INK);
    txt(sub, x + 18, y + 18, `10px ${FONT_SMALL}`, '#8a6a4a');
  });
}

export class PixelScene extends Phaser.Scene {
  constructor() {
    super('Pixel');
  }

  create(): void {
    this.cameras.main.setZoom(2).centerOn(W / 2, H / 2);
    this.add.image(0, 0, canvasTex(this, 'px-all', W, H, 1, drawAll)).setOrigin(0, 0);
    // 물비늘 반짝임과 초롱 깜빡임
    for (let i = 0; i < 12; i++) {
      const s = this.add.rectangle(10 + i * 18, 160 + ((i * 13) % 44), 2, 1, i % 3 ? 0xffe3a3 : 0xffffff).setOrigin(0, 0).setAlpha(0);
      this.tweens.add({ targets: s, alpha: 1, duration: 500, yoyo: true, repeat: -1, delay: i * 190 });
    }
    const lamp = this.add.rectangle(203, 132, 6, 8, 0xffffff, 0.5).setOrigin(0, 0);
    this.tweens.add({ targets: lamp, alpha: 0, duration: 900, yoyo: true, repeat: -1 });
  }
}
