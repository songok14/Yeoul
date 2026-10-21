// 시안 3: 따뜻한 수채화 감성 (종이 질감, 번지는 물감, 잉크 선, 고운바탕·고운돋움)

import '@fontsource/gowun-dodum/400.css';
import '@fontsource/gowun-batang/700.css';
import Phaser from 'phaser';
import { canvasTex, rng, text, type Ctx } from './paint';

const SERIF = "'Gowun Batang'";
const SANS = "'Gowun Dodum'";
const INK = '#4B3A2F';
const SCALE = 2;
const SAMPLE = '4일차새벽아침낮저녁D-26 86,000원갚을돈2,640,000원오늘들은소문노래미값이23%떨어졌대위판장미끼가게여울식당방파제여울호수첩낚시1칸경매중장사장비미끼상환휴식도감미각·';

export async function loadWatercolorFonts(): Promise<void> {
  await Promise.all([document.fonts.load(`400 20px ${SANS}`, SAMPLE), document.fonts.load(`700 20px ${SERIF}`, SAMPLE)]);
}

let r = rng(3);

/** 울퉁불퉁한 덩어리 경로 */
function blob(cx: number, cy: number, rx: number, ry: number, wobble = 0.16, n = 16): Path2D {
  const p = new Path2D();
  const pts: [number, number][] = [];
  for (let i = 0; i < n; i++) {
    const a = (i / n) * Math.PI * 2;
    const k = 1 + (r() - 0.5) * wobble * 2;
    pts.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]);
  }
  const mid = (i: number) => {
    const a = pts[i % n];
    const b = pts[(i + 1) % n];
    return [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2];
  };
  const m0 = mid(0);
  p.moveTo(m0[0], m0[1]);
  for (let i = 1; i <= n; i++) {
    const m = mid(i);
    p.quadraticCurveTo(pts[i % n][0], pts[i % n][1], m[0], m[1]);
  }
  p.closePath();
  return p;
}

/** 가장자리가 흔들리는 사각형 */
function wrect(x: number, y: number, w: number, h: number, j = 1.6): Path2D {
  const p = new Path2D();
  const steps = (len: number) => Math.max(2, Math.round(len / 14));
  const jit = () => (r() - 0.5) * j * 2;
  p.moveTo(x + jit(), y + jit());
  const edge = (x0: number, y0: number, x1: number, y1: number) => {
    const n = steps(Math.hypot(x1 - x0, y1 - y0));
    for (let i = 1; i <= n; i++) p.lineTo(x0 + ((x1 - x0) * i) / n + jit(), y0 + ((y1 - y0) * i) / n + jit());
  };
  edge(x, y, x + w, y);
  edge(x + w, y, x + w, y + h);
  edge(x + w, y + h, x, y + h);
  edge(x, y + h, x, y);
  p.closePath();
  return p;
}

/** 물감 한 겹: 여러 번 살짝 어긋나게 칠하고 가장자리에 물감이 고이게 한다 */
function wash(ctx: Ctx, path: Path2D, color: string, alpha = 0.4, blur = 1.4, edge = true, knock = false): void {
  ctx.save();
  if (knock) {
    ctx.fillStyle = '#F7F0E2';
    ctx.fill(path);
  }
  ctx.globalCompositeOperation = 'multiply';
  for (let i = 0; i < 3; i++) {
    ctx.save();
    ctx.translate((r() - 0.5) * 2.4, (r() - 0.5) * 2.4);
    ctx.globalAlpha = alpha * 0.45;
    ctx.filter = `blur(${blur}px)`;
    ctx.fillStyle = color;
    ctx.fill(path);
    ctx.restore();
  }
  if (edge) {
    ctx.globalAlpha = alpha * 0.55;
    ctx.filter = 'blur(0.8px)';
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.3;
    ctx.stroke(path);
  }
  ctx.restore();
}

/** 떨리는 잉크 선 */
function ink(ctx: Ctx, pts: [number, number][], w = 1.1, color = INK, alpha = 0.85, close = false): void {
  ctx.save();
  ctx.strokeStyle = color;
  ctx.globalAlpha = alpha;
  ctx.lineWidth = w;
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.beginPath();
  const all = close ? [...pts, pts[0]] : pts;
  ctx.moveTo(all[0][0], all[0][1]);
  for (let i = 1; i < all.length; i++) {
    const [x0, y0] = all[i - 1];
    const [x1, y1] = all[i];
    const n = Math.max(1, Math.round(Math.hypot(x1 - x0, y1 - y0) / 10));
    for (let k = 1; k <= n; k++) {
      const t = k / n;
      ctx.lineTo(x0 + (x1 - x0) * t + (k < n ? (r() - 0.5) * 0.9 : 0), y0 + (y1 - y0) * t + (k < n ? (r() - 0.5) * 0.9 : 0));
    }
  }
  ctx.stroke();
  ctx.restore();
}

const inkRect = (ctx: Ctx, x: number, y: number, w: number, h: number, wd = 1.1) =>
  ink(ctx, [[x, y], [x + w, y], [x + w, y + h], [x, y + h]], wd, INK, 0.85, true);

function paper(ctx: Ctx, x: number, y: number, w: number, h: number, base = '#F6EEDD'): void {
  ctx.fillStyle = base;
  ctx.fillRect(x, y, w, h);
  ctx.save();
  for (let i = 0; i < (w * h) / 22; i++) {
    ctx.globalAlpha = 0.05 + r() * 0.07;
    ctx.fillStyle = r() > 0.5 ? '#B8A27E' : '#FFFFFF';
    ctx.fillRect(x + r() * w, y + r() * h, 0.6 + r(), 0.6 + r());
  }
  ctx.globalAlpha = 0.06;
  ctx.strokeStyle = '#A08A66';
  ctx.lineWidth = 0.4;
  for (let i = 0; i < (w * h) / 900; i++) {
    const fx = x + r() * w;
    const fy = y + r() * h;
    ctx.beginPath();
    ctx.moveTo(fx, fy);
    ctx.quadraticCurveTo(fx + 4, fy + (r() - 0.5) * 4, fx + 8 + r() * 6, fy + (r() - 0.5) * 3);
    ctx.stroke();
  }
  ctx.restore();
}

function drawScene(ctx: Ctx): void {
  r = rng(3);
  paper(ctx, 0, 0, 450, 800);

  // 하늘
  const sky = ctx.createLinearGradient(0, 0, 0, 324);
  sky.addColorStop(0, 'rgba(150,192,214,0.85)');
  sky.addColorStop(0.55, 'rgba(196,220,228,0.7)');
  sky.addColorStop(0.85, 'rgba(244,214,186,0.75)');
  sky.addColorStop(1, 'rgba(246,206,170,0.8)');
  ctx.fillStyle = sky;
  ctx.fillRect(0, 0, 450, 324);
  for (let i = 0; i < 7; i++) wash(ctx, blob(r() * 450, 20 + r() * 140, 60 + r() * 60, 18 + r() * 16, 0.3), '#7FAFC8', 0.16, 8, false);
  for (let i = 0; i < 4; i++) wash(ctx, blob(r() * 450, 270 + r() * 40, 70, 12, 0.3), '#EFA97E', 0.14, 8, false);
  ctx.save();
  ctx.filter = 'blur(10px)';
  ctx.globalAlpha = 0.7;
  ctx.fillStyle = '#FBE2A6';
  ctx.beginPath();
  ctx.arc(318, 236, 54, 0, Math.PI * 2);
  ctx.fill();
  ctx.filter = 'blur(2px)';
  ctx.globalAlpha = 0.95;
  ctx.fillStyle = '#FFF2CC';
  ctx.fill(blob(318, 236, 30, 29, 0.05));
  ctx.restore();
  // 구름은 흰 물감으로 살짝 얹는다
  ctx.save();
  ctx.globalAlpha = 0.75;
  ctx.filter = 'blur(3px)';
  ctx.fillStyle = '#FFFCF4';
  for (const [x, y, rx, ry] of [[90, 176, 60, 18], [120, 166, 34, 16], [250, 142, 44, 13], [276, 136, 26, 12]]) ctx.fill(blob(x, y, rx, ry, 0.2));
  ctx.restore();
  wash(ctx, blob(96, 188, 58, 6, 0.3), '#9FB2C2', 0.3, 3, false);
  wash(ctx, blob(256, 152, 40, 5, 0.3), '#9FB2C2', 0.28, 3, false);

  // 먼 섬
  const hill = (x0: number, x1: number, top: number, col: string, a: number) => {
    const p = new Path2D();
    p.moveTo(x0, 324);
    p.bezierCurveTo(x0 + (x1 - x0) * 0.3, top, x0 + (x1 - x0) * 0.6, top - 4, x1, 324);
    p.closePath();
    wash(ctx, p, col, a, 3);
  };
  hill(-30, 200, 262, '#8C97B4', 0.42);
  hill(250, 480, 272, '#8C97B4', 0.4);
  hill(20, 170, 290, '#6F9488', 0.45);

  // 바다
  wash(ctx, wrect(-10, 320, 470, 136, 3), '#4F9BAE', 0.55, 4);
  wash(ctx, wrect(-10, 392, 470, 62, 5), '#3A86A2', 0.22, 5, false);
  ctx.save();
  ctx.strokeStyle = '#FBF6EA';
  ctx.lineCap = 'round';
  for (let i = 0; i < 46; i++) {
    const x = r() * 450;
    const y = 330 + r() * 120;
    ctx.globalAlpha = 0.55 + r() * 0.35;
    ctx.lineWidth = 0.8 + r() * 1.2;
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + 6 + r() * 18, y + (r() - 0.5));
    ctx.stroke();
  }
  ctx.strokeStyle = '#F6D78A';
  for (let i = 0; i < 9; i++) {
    const w = 44 - i * 4;
    ctx.globalAlpha = 0.8 - i * 0.07;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(318 - w / 2 + (r() - 0.5) * 6, 330 + i * 10);
    ctx.lineTo(318 + w / 2 + (r() - 0.5) * 6, 330 + i * 10);
    ctx.stroke();
  }
  ctx.restore();

  // 작은 배
  wash(ctx, wrect(84, 368, 44, 9, 0.8), '#FBF6EA', 0.9, 1, false, true);
  wash(ctx, wrect(86, 372, 40, 3, 0.5), '#C8664A', 0.7, 1, false);
  ink(ctx, [[82, 368], [130, 368], [122, 378], [90, 378]], 1, INK, 0.8, true);
  ink(ctx, [[98, 368], [98, 358], [112, 358], [112, 368]], 1);

  // 방파제와 등대
  wash(ctx, wrect(292, 402, 170, 22, 1.2), '#A8A39A', 0.5, 2);
  ink(ctx, [[292, 402], [460, 402]], 1);
  ink(ctx, [[292, 424], [460, 424]], 0.9, INK, 0.5);
  const lh = new Path2D();
  lh.moveTo(398, 402);
  lh.lineTo(426, 402);
  lh.lineTo(421, 336);
  lh.lineTo(403, 336);
  lh.closePath();
  wash(ctx, lh, '#E9E3D8', 0.5, 1.2, true, true);
  for (const y of [350, 376]) wash(ctx, wrect(401, y, 22, 12, 0.6), '#C8664A', 0.7, 1, false);
  wash(ctx, blob(412, 326, 22, 22, 0.1), '#F4D48A', 0.5, 6, false);
  wash(ctx, wrect(404, 320, 16, 14, 0.5), '#F4D48A', 0.7, 1, false);
  ink(ctx, [[398, 402], [403, 336], [421, 336], [426, 402]], 1.1);
  inkRect(ctx, 404, 320, 16, 14, 1);
  ink(ctx, [[400, 320], [412, 308], [424, 320]], 1.1);
  wash(ctx, (() => {
    const p = new Path2D();
    p.moveTo(400, 320);
    p.lineTo(412, 308);
    p.lineTo(424, 320);
    p.closePath();
    return p;
  })(), '#B5483A', 0.7, 1, false);

  // 바닷가와 마당
  wash(ctx, wrect(-10, 446, 470, 220, 3), '#E6C79A', 0.38, 4, false);
  ctx.save();
  ctx.strokeStyle = '#FBF6EA';
  ctx.globalAlpha = 0.9;
  ctx.lineWidth = 2.5;
  ctx.beginPath();
  for (let x = -10; x <= 460; x += 12) ctx.lineTo(x, 450 + Math.sin(x / 18) * 2 + (r() - 0.5) * 2);
  ctx.stroke();
  ctx.restore();

  // 위판장
  wash(ctx, wrect(18, 438, 146, 102), '#CFE0E2', 0.42, 1.4, true, true);
  wash(ctx, wrect(10, 412, 162, 30, 1.2), '#5E8FB5', 0.65, 1.4, true, true);
  for (let x = 16; x < 170; x += 9) ink(ctx, [[x, 414], [x, 440]], 0.6, '#2F4D66', 0.5);
  ink(ctx, [[10, 412], [172, 412], [172, 442], [10, 442]], 1.2, INK, 0.85, true);
  inkRect(ctx, 18, 442, 146, 98, 1);
  wash(ctx, wrect(34, 468, 114, 72, 1), '#5B6470', 0.5, 2);
  wash(ctx, blob(91, 476, 50, 10, 0.2), '#E8B870', 0.35, 4, false);
  inkRect(ctx, 34, 468, 114, 72, 0.9);
  const crate = (x: number, y: number, col: string) => {
    wash(ctx, wrect(x, y, 30, 17, 0.6), col, 0.7, 1);
    ink(ctx, [[x, y], [x + 30, y], [x + 30, y + 17], [x, y + 17]], 0.8, INK, 0.7, true);
  };
  crate(42, 521, '#5E8FB5');
  crate(76, 521, '#D9A441');
  crate(110, 521, '#5E8FB5');
  crate(59, 503, '#D9A441');
  crate(93, 503, '#EDE6DA');

  // 미끼가게
  wash(ctx, wrect(172, 456, 100, 20, 0.6), '#F6EEDD', 0.1, 1, false, true);
  wash(ctx, wrect(178, 452, 90, 88), '#E9CF8E', 0.5, 1.4, true, true);
  for (let i = 0; i < 6; i++) wash(ctx, wrect(178 + i * 15, 458, 15, 16, 0.4), i % 2 ? '#F6EEDD' : '#8FAF8A', 0.75, 1, false);
  ink(ctx, [[174, 458], [272, 458]], 1.2);
  for (let i = 0; i < 6; i++) {
    ctx.save();
    ctx.globalAlpha = 0.85;
    ctx.strokeStyle = INK;
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.arc(185.5 + i * 15, 474, 7.5, 0, Math.PI);
    ctx.stroke();
    ctx.restore();
  }
  inkRect(ctx, 178, 452, 90, 88, 1);
  wash(ctx, wrect(208, 494, 30, 46, 0.6), '#A0704C', 0.65, 1);
  inkRect(ctx, 208, 494, 30, 46, 1);
  wash(ctx, wrect(184, 514, 16, 20, 0.6), '#5E8FB5', 0.6, 1);
  wash(ctx, wrect(246, 518, 14, 16, 0.6), '#C8664A', 0.6, 1);

  // 여울식당
  wash(ctx, wrect(282, 446, 152, 94), '#F3E3C8', 0.4, 1.4, true, true);
  wash(ctx, wrect(410, 446, 24, 94, 1), '#C9B49A', 0.3, 3, false);
  for (const px of [286, 422]) {
    wash(ctx, wrect(px, 448, 7, 92, 0.6), '#9A6B4B', 0.6, 1, false);
    ink(ctx, [[px, 448], [px, 540]], 0.9, INK, 0.7);
    ink(ctx, [[px + 7, 448], [px + 7, 540]], 0.9, INK, 0.7);
  }
  const roof = new Path2D();
  roof.moveTo(262, 438);
  roof.quadraticCurveTo(300, 402, 357, 400);
  roof.quadraticCurveTo(414, 402, 454, 438);
  roof.quadraticCurveTo(440, 452, 430, 448);
  roof.lineTo(284, 448);
  roof.quadraticCurveTo(274, 452, 262, 438);
  roof.closePath();
  wash(ctx, roof, '#5F6678', 0.7, 1.4, true, true);
  ctx.save();
  ctx.globalAlpha = 0.55;
  ctx.strokeStyle = INK;
  ctx.lineWidth = 0.7;
  for (let i = 0; i < 4; i++) {
    ctx.beginPath();
    ctx.moveTo(276 + i * 2, 430 - i * 7);
    ctx.quadraticCurveTo(357, 402 - i * 4 + 12, 438 - i * 2, 430 - i * 7);
    ctx.stroke();
  }
  ctx.restore();
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.9;
  ctx.lineWidth = 1.3;
  ctx.stroke(roof);
  ctx.restore();
  const win = (x: number) => {
    wash(ctx, blob(x + 17, 484, 30, 24, 0.15), '#F2C46D', 0.32, 6, false);
    wash(ctx, wrect(x, 468, 34, 30, 0.6), '#F2C46D', 0.6, 1);
    inkRect(ctx, x, 468, 34, 30, 1);
    ink(ctx, [[x + 17, 468], [x + 17, 498]], 0.8);
    ink(ctx, [[x, 483], [x + 34, 483]], 0.8);
  };
  win(302);
  win(378);
  wash(ctx, wrect(341, 478, 34, 62, 0.6), '#8A5E42', 0.6, 1);
  inkRect(ctx, 341, 478, 34, 62, 1);
  wash(ctx, wrect(337, 464, 42, 26, 0.6), '#3F5E8C', 0.75, 1);
  ink(ctx, [[337, 464], [379, 464], [379, 490], [337, 490]], 1, INK, 0.85, true);
  ink(ctx, [[358, 464], [358, 490]], 0.9, '#FBF6EA', 0.9);

  // 동백 덤불
  const camellia = (x: number, y: number, s: number) => {
    wash(ctx, blob(x, y, 22 * s, 14 * s, 0.25), '#5E8A5E', 0.55, 2);
    wash(ctx, blob(x + 6 * s, y - 8 * s, 12 * s, 8 * s, 0.25), '#7FA674', 0.45, 2, false);
    for (let i = 0; i < 5; i++) wash(ctx, blob(x + (r() - 0.5) * 30 * s, y + (r() - 0.5) * 16 * s, 3.2 * s, 3.2 * s, 0.2, 8), '#C8434A', 0.85, 0.6, false);
  };
  camellia(288, 538, 1);
  camellia(436, 536, 0.9);
  camellia(176, 540, 0.8);

  // 길
  wash(ctx, wrect(-10, 556, 470, 56, 2), '#B9B0A3', 0.38, 3);
  ctx.save();
  ctx.globalAlpha = 0.5;
  ctx.strokeStyle = '#FBF6EA';
  ctx.lineWidth = 2;
  for (let x = 10; x < 450; x += 50) {
    ctx.beginPath();
    ctx.moveTo(x, 584);
    ctx.lineTo(x + 22, 584);
    ctx.stroke();
  }
  ctx.restore();
}

function drawTruck(ctx: Ctx): void {
  r = rng(21);
  ctx.save();
  ctx.globalAlpha = 0.25;
  ctx.filter = 'blur(3px)';
  ctx.fillStyle = '#6A5A4A';
  ctx.beginPath();
  ctx.ellipse(82, 80, 76, 6, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
  wash(ctx, wrect(4, 26, 50, 44, 0.8), '#86BCCD', 0.65, 1.2, true, true);
  wash(ctx, wrect(12, 32, 30, 16, 0.5), '#E3F0F2', 0.8, 1, false);
  ink(ctx, [[4, 70], [4, 36], [16, 26], [54, 26], [54, 70]], 1.1);
  inkRect(ctx, 12, 32, 30, 16, 0.9);
  wash(ctx, wrect(52, 4, 104, 68, 1), '#F4E9D2', 0.55, 1.2, true, true);
  for (let i = 0; i < 7; i++) wash(ctx, wrect(58 + i * 13, 10, 13, 13, 0.4), i % 2 ? '#F6EEDD' : '#C8664A', 0.8, 0.8, false);
  inkRect(ctx, 52, 4, 104, 68, 1.1);
  ink(ctx, [[58, 23], [149, 23]], 0.9);
  wash(ctx, wrect(64, 30, 78, 20, 0.5), '#8A5E42', 0.7, 1);
  inkRect(ctx, 64, 30, 78, 20, 0.9);
  text(ctx, '여울호', 96, 61, `700 15px ${SERIF}`, '#2F4A7A', 'center');
  for (const x of [28, 132]) {
    wash(ctx, blob(x, 72, 11, 11, 0.06, 12), '#3E3A3A', 0.85, 0.8, false);
    wash(ctx, blob(x, 72, 4, 4, 0.06, 10), '#D9D2C4', 0.9, 0.5, false);
  }
}

function drawYeoul(ctx: Ctx): void {
  r = rng(31);
  wash(ctx, wrect(16, 58, 8, 18, 0.4), '#3F5E8C', 0.7, 0.8, false, true);
  wash(ctx, wrect(26, 58, 8, 18, 0.4), '#3F5E8C', 0.7, 0.8, false, true);
  const coat = new Path2D();
  coat.moveTo(14, 36);
  coat.quadraticCurveTo(25, 30, 36, 36);
  coat.lineTo(41, 64);
  coat.quadraticCurveTo(25, 68, 9, 64);
  coat.closePath();
  wash(ctx, coat, '#E8B83A', 0.75, 1, true, true);
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.85;
  ctx.lineWidth = 1;
  ctx.stroke(coat);
  ctx.restore();
  wash(ctx, blob(25, 22, 14, 14, 0.04, 14), '#F2C9A8', 0.7, 0.8, true, true);
  const hair = new Path2D();
  hair.arc(25, 19, 15, Math.PI * 1.02, Math.PI * 1.98);
  hair.lineTo(40, 30);
  hair.lineTo(35, 30);
  hair.lineTo(34, 18);
  hair.lineTo(16, 18);
  hair.lineTo(15, 30);
  hair.lineTo(10, 30);
  hair.closePath();
  wash(ctx, hair, '#4B3A2F', 0.85, 0.8);
  wash(ctx, blob(25, 4, 6, 6, 0.05, 10), '#4B3A2F', 0.85, 0.6, false);
  ctx.fillStyle = INK;
  ctx.beginPath();
  ctx.arc(20, 25, 1.4, 0, Math.PI * 2);
  ctx.arc(30, 25, 1.4, 0, Math.PI * 2);
  ctx.fill();
  wash(ctx, blob(17, 29, 3, 2, 0.1, 8), '#E07A7A', 0.4, 1, false);
  wash(ctx, blob(33, 29, 3, 2, 0.1, 8), '#E07A7A', 0.4, 1, false);
}

function drawCat(ctx: Ctx): void {
  r = rng(41);
  wash(ctx, blob(22, 31, 14, 12, 0.08), '#8E949C', 0.6, 1, true, true);
  wash(ctx, blob(22, 14, 12, 11, 0.05), '#8E949C', 0.6, 1, true, true);
  ink(ctx, [[10, 10], [13, 0], [18, 6]], 1);
  ink(ctx, [[26, 6], [31, 0], [34, 10]], 1);
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.8;
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.ellipse(22, 14, 12, 11, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.beginPath();
  ctx.ellipse(22, 31, 14, 12, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.lineWidth = 3;
  ctx.globalAlpha = 0.55;
  ctx.beginPath();
  ctx.moveTo(34, 38);
  ctx.quadraticCurveTo(46, 36, 42, 24);
  ctx.stroke();
  ctx.restore();
  for (const x of [17, 22, 27]) ink(ctx, [[x, 4], [x, 8]], 1, '#5E646C', 0.8);
  ctx.fillStyle = INK;
  ctx.beginPath();
  ctx.arc(18, 15, 1.4, 0, Math.PI * 2);
  ctx.arc(26, 15, 1.4, 0, Math.PI * 2);
  ctx.fill();
  wash(ctx, wrect(19, 24, 6, 9, 0.3), '#C8434A', 0.85, 0.5, false);
}

/** 손그림 아이콘 */
function doodle(ctx: Ctx, kind: string, x: number, y: number): void {
  const L = (pts: [number, number][], close = false) => ink(ctx, pts.map(([a, b]) => [x + a, y + b] as [number, number]), 1.4, INK, 0.9, close);
  switch (kind) {
    case 'rod':
      L([[-8, 9], [7, -9], [7, 2]]);
      ctx.save();
      ctx.strokeStyle = INK;
      ctx.lineWidth = 1.4;
      ctx.beginPath();
      ctx.arc(x + 4, y + 2, 3, 0, Math.PI);
      ctx.stroke();
      ctx.restore();
      break;
    case 'fish':
      L([[-9, 0], [-3, -5], [4, -4], [8, 0], [4, 4], [-3, 5]], true);
      L([[8, 0], [11, -4], [11, 4], [8, 0]]);
      break;
    case 'truck':
      L([[-9, 5], [-9, -5], [3, -5], [3, 5]], true);
      L([[3, -1], [8, -1], [10, 3], [10, 5], [3, 5]]);
      break;
    case 'bait':
      L([[-8, 3], [-5, -3], [-1, 1], [2, -4], [6, 0], [9, -3]]);
      break;
    case 'bowl':
      L([[-9, -1], [9, -1], [6, 6], [-6, 6]], true);
      L([[-3, -4], [-1, -8]]);
      L([[3, -4], [5, -8]]);
      break;
    case 'book':
      L([[-7, -8], [7, -8], [7, 8], [-7, 8]], true);
      L([[-3, -3], [3, -3]]);
      L([[-3, 1], [3, 1]]);
      break;
  }
}

function tornPaper(ctx: Ctx, x: number, y: number, w: number, h: number, color: string, tornTop: boolean, tornBottom: boolean): void {
  const p = new Path2D();
  p.moveTo(x, y);
  if (tornTop) for (let i = 0; i <= w; i += 6) p.lineTo(x + i, y + (r() - 0.5) * 4);
  else p.lineTo(x + w, y);
  p.lineTo(x + w, y + h);
  if (tornBottom) for (let i = w; i >= 0; i -= 6) p.lineTo(x + i, y + h + (r() - 0.5) * 4);
  else p.lineTo(x, y + h);
  p.closePath();
  ctx.save();
  ctx.shadowColor = 'rgba(80,60,40,0.25)';
  ctx.shadowBlur = 8;
  ctx.shadowOffsetY = 2;
  ctx.fillStyle = color;
  ctx.fill(p);
  ctx.restore();
  ctx.save();
  ctx.clip(p);
  paper(ctx, x, y - 4, w, h + 8, color);
  ctx.restore();
}

function tape(ctx: Ctx, x: number, y: number, w: number, angle: number): void {
  ctx.save();
  ctx.translate(x, y);
  ctx.rotate(angle);
  ctx.globalAlpha = 0.7;
  ctx.fillStyle = '#E6D7AE';
  ctx.fillRect(-w / 2, -8, w, 16);
  ctx.restore();
}

function tag(ctx: Ctx, cx: number, cy: number, label: string): void {
  ctx.font = `700 14px ${SERIF}`;
  const w = ctx.measureText(label).width + 26;
  const p = new Path2D();
  p.moveTo(cx - w / 2 + 8, cy - 13);
  p.lineTo(cx + w / 2, cy - 13);
  p.lineTo(cx + w / 2, cy + 13);
  p.lineTo(cx - w / 2 + 8, cy + 13);
  p.lineTo(cx - w / 2, cy);
  p.closePath();
  ctx.save();
  ctx.shadowColor = 'rgba(80,60,40,0.3)';
  ctx.shadowBlur = 6;
  ctx.shadowOffsetY = 2;
  ctx.fillStyle = '#FBF6EA';
  ctx.fill(p);
  ctx.restore();
  ctx.save();
  ctx.strokeStyle = INK;
  ctx.globalAlpha = 0.7;
  ctx.lineWidth = 1;
  ctx.stroke(p);
  ctx.beginPath();
  ctx.arc(cx - w / 2 + 9, cy, 2.5, 0, Math.PI * 2);
  ctx.stroke();
  ctx.restore();
  text(ctx, label, cx + 5, cy + 1, `700 14px ${SERIF}`, INK, 'center');
}

function drawUi(ctx: Ctx): void {
  r = rng(51);
  // 상단 쪽지
  tornPaper(ctx, 12, 12, 426, 94, '#FBF7EE', false, true);
  tape(ctx, 30, 14, 46, -0.25);
  tape(ctx, 420, 14, 46, 0.25);
  text(ctx, '4일차', 28, 46, `700 24px ${SERIF}`, INK);
  const slots = ['새벽', '아침', '낮', '저녁'];
  let sx = 108;
  slots.forEach((s, i) => {
    const w = text(ctx, s, sx, 47, `400 15px ${SANS}`, i === 2 ? INK : i < 2 ? '#B3A28C' : '#7A6652');
    if (i === 2) {
      ctx.save();
      ctx.strokeStyle = '#C8664A';
      ctx.globalAlpha = 0.85;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.ellipse(sx + w / 2, 47, w / 2 + 10, 13, -0.08, 0.2, Math.PI * 2.05);
      ctx.stroke();
      ctx.restore();
    }
    sx += w + 18;
  });
  // 도장처럼 찍힌 D-26
  ctx.save();
  ctx.translate(392, 50);
  ctx.rotate(-0.12);
  ctx.globalAlpha = 0.85;
  ctx.strokeStyle = '#B83A2E';
  ctx.lineWidth = 2.2;
  ctx.beginPath();
  ctx.arc(0, 0, 26, 0, Math.PI * 2);
  ctx.stroke();
  ctx.lineWidth = 0.8;
  ctx.beginPath();
  ctx.arc(0, 0, 22, 0, Math.PI * 2);
  ctx.stroke();
  text(ctx, 'D-26', 0, 1, `700 16px ${SERIF}`, '#B83A2E', 'center');
  ctx.restore();

  wash(ctx, blob(38, 84, 9, 9, 0.08, 10), '#D9A441', 0.8, 0.8);
  text(ctx, '86,000원', 54, 85, `700 20px ${SERIF}`, INK);
  text(ctx, '갚을 돈 2,640,000원', 342, 86, `400 13px ${SANS}`, '#7A6652', 'right');

  // 소문 쪽지
  tornPaper(ctx, 18, 120, 292, 32, '#FFFDF6', true, true);
  text(ctx, '오늘 들은 소문', 32, 136, `700 13px ${SERIF}`, '#4F7C8E');
  text(ctx, '노래미 값이 23% 떨어졌대', 132, 136, `400 14px ${SANS}`, INK);

  // 장소 꼬리표
  tag(ctx, 91, 396, '위판장');
  tag(ctx, 223, 432, '미끼가게');
  tag(ctx, 357, 428, '여울식당');
  tag(ctx, 336, 372, '방파제 →');

  // 아래 크라프트지 판
  tornPaper(ctx, -4, 640, 458, 170, '#E3D1AE', true, false);
  const cards: [string, string, string, string][] = [
    ['방파제', '낚시 · 1칸', '#7FB2CC', 'rod'],
    ['위판장', '경매 중', '#8FBFA8', 'fish'],
    ['여울호', '장사 · 1칸', '#E0907A', 'truck'],
    ['미끼가게', '장비 · 미끼', '#A8C48E', 'bait'],
    ['여울식당', '상환 · 휴식', '#E8BE72', 'bowl'],
    ['수첩', '도감 · 미각', '#B4A6CF', 'book'],
  ];
  cards.forEach(([label, sub, col, ic], i) => {
    const x = 18 + (i % 3) * 142;
    const y = 662 + Math.floor(i / 3) * 70;
    ctx.save();
    ctx.translate(x + 64, y + 30);
    ctx.rotate([-0.02, 0.015, -0.01, 0.02, -0.015, 0.01][i]);
    ctx.translate(-(x + 64), -(y + 30));
    ctx.shadowColor = 'rgba(80,60,40,0.28)';
    ctx.shadowBlur = 5;
    ctx.shadowOffsetY = 2;
    ctx.fillStyle = '#FBF6EA';
    ctx.fillRect(x, y, 128, 60);
    ctx.shadowColor = 'transparent';
    wash(ctx, blob(x + 26, y + 30, 16, 15, 0.18), col, 0.75, 1.5);
    doodle(ctx, ic, x + 26, y + 30);
    text(ctx, label, x + 50, y + 23, `700 17px ${SERIF}`, INK);
    text(ctx, sub, x + 50, y + 43, `400 12px ${SANS}`, '#7A6652');
    ctx.restore();
  });
}

export class WatercolorScene extends Phaser.Scene {
  constructor() {
    super('Watercolor');
  }

  create(): void {
    this.cameras.main.setZoom(SCALE).centerOn(225, 400);
    const img = (key: string, x: number, y: number, w: number, h: number) => this.add.image(x, y, key).setDisplaySize(w, h);
    img(canvasTex(this, 'wc-bg', 450, 800, SCALE, drawScene), 225, 400, 450, 800);
    img(canvasTex(this, 'wc-truck', 160, 86, SCALE, drawTruck), 230, 566, 160, 86);
    const yeoul = img(canvasTex(this, 'wc-yeoul', 50, 80, SCALE, drawYeoul), 104, 590, 50, 80);
    this.tweens.add({ targets: yeoul, y: 587, duration: 900, yoyo: true, repeat: -1, ease: 'Sine.InOut' });
    img(canvasTex(this, 'wc-cat', 48, 44, SCALE, drawCat), 352, 604, 48, 44);
    img(canvasTex(this, 'wc-ui', 450, 800, SCALE, drawUi), 225, 400, 450, 800);
  }
}
