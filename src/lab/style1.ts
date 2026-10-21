// 시안 1: 깔끔한 캐주얼 일러스트 (둥근 UI, 파스텔 바다, Pretendard)

import Phaser from 'phaser';
import pBold from 'pretendard/dist/web/static/woff2/Pretendard-Bold.woff2?url';
import pExtra from 'pretendard/dist/web/static/woff2/Pretendard-ExtraBold.woff2?url';
import pMedium from 'pretendard/dist/web/static/woff2/Pretendard-Medium.woff2?url';
import pSemi from 'pretendard/dist/web/static/woff2/Pretendard-SemiBold.woff2?url';
import { canvasTex, glow, loadFace, rng, rr, text, vgrad, withShadow, type Ctx } from './paint';

const F = 'Pretendard';
const INK = '#24324A';
const SUB = '#7C8794';
const SCALE = 2;

export async function loadCasualFonts(): Promise<void> {
  await Promise.all([loadFace(F, pMedium, '500'), loadFace(F, pSemi, '600'), loadFace(F, pBold, '700'), loadFace(F, pExtra, '800')]);
}

const font = (w: number, px: number) => `${w} ${px}px ${F}`;

function cloud(ctx: Ctx, x: number, y: number, s: number): void {
  ctx.save();
  ctx.translate(x, y);
  ctx.scale(s, s);
  ctx.fillStyle = 'rgba(205,229,244,0.9)';
  rr(ctx, -58, 4, 128, 22, 11);
  ctx.fill();
  ctx.fillStyle = '#FFFFFF';
  for (const [cx, cy, r] of [[-30, 6, 22], [0, -8, 30], [32, 2, 24], [-52, 14, 14], [56, 14, 14]]) {
    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.fill();
  }
  rr(ctx, -60, 6, 130, 20, 10);
  ctx.fill();
  ctx.restore();
}

function hill(ctx: Ctx, x0: number, x1: number, base: number, peak: number, color: string): void {
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.moveTo(x0, base);
  ctx.bezierCurveTo(x0 + (x1 - x0) * 0.25, peak, x0 + (x1 - x0) * 0.6, peak - 6, x1, base);
  ctx.closePath();
  ctx.fill();
}

function scallop(ctx: Ctx, x: number, y: number, w: number, r: number, colors: string[]): void {
  const n = Math.round(w / (r * 2));
  const step = w / n;
  for (let i = 0; i < n; i++) {
    ctx.fillStyle = colors[i % colors.length];
    ctx.beginPath();
    ctx.arc(x + step * i + step / 2, y, step / 2, 0, Math.PI);
    ctx.fill();
  }
}

function pill(ctx: Ctx, cx: number, cy: number, label: string, dot: string, arrow = false): void {
  ctx.font = font(700, 14);
  const w = ctx.measureText(label).width + 40 + (arrow ? 14 : 0);
  withShadow(ctx, 'rgba(36,50,74,0.18)', 10, 3, () => {
    ctx.fillStyle = '#FFFFFF';
    rr(ctx, cx - w / 2, cy - 15, w, 30, 15);
    ctx.fill();
  });
  ctx.fillStyle = dot;
  ctx.beginPath();
  ctx.arc(cx - w / 2 + 16, cy, 5, 0, Math.PI * 2);
  ctx.fill();
  text(ctx, label, cx - w / 2 + 28, cy + 1, font(700, 14), INK);
  if (arrow) text(ctx, '›', cx + w / 2 - 16, cy, font(800, 18), dot, 'center');
}

function drawScene(ctx: Ctx): void {
  const r = rng(7);
  // 하늘
  ctx.fillStyle = vgrad(ctx, 0, 330, [[0, '#7CC8F0'], [0.6, '#BDE5F6'], [1, '#FFF1DA']]);
  ctx.fillRect(0, 0, 450, 330);
  glow(ctx, 330, 215, 110, 'rgba(255,240,196,0.9)');
  ctx.fillStyle = '#FFF7DC';
  ctx.beginPath();
  ctx.arc(330, 215, 30, 0, Math.PI * 2);
  ctx.fill();

  // 먼 섬
  hill(ctx, -20, 190, 322, 262, '#A9D2E3');
  hill(ctx, 120, 300, 322, 280, '#9CCBDB');
  hill(ctx, 260, 470, 322, 270, '#A9D2E3');
  hill(ctx, 30, 160, 322, 290, '#8CC3B4');

  // 바다
  ctx.fillStyle = vgrad(ctx, 320, 460, [[0, '#6CD0E8'], [1, '#2F9BCC']]);
  ctx.fillRect(0, 320, 450, 140);
  ctx.fillStyle = 'rgba(255,255,255,0.55)';
  ctx.fillRect(0, 320, 450, 2);
  // 해 반사
  for (let i = 0; i < 7; i++) {
    const w = 46 - i * 4 + (i % 2) * 10;
    ctx.fillStyle = `rgba(255,248,220,${0.75 - i * 0.08})`;
    rr(ctx, 330 - w / 2, 330 + i * 11, w, 4, 2);
    ctx.fill();
  }
  // 물결
  ctx.strokeStyle = 'rgba(255,255,255,0.5)';
  ctx.lineWidth = 2.2;
  ctx.lineCap = 'round';
  for (let i = 0; i < 26; i++) {
    const x = r() * 450;
    const y = 340 + r() * 110;
    const w = 8 + r() * 14;
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.quadraticCurveTo(x + w / 2, y - 3, x + w, y);
    ctx.stroke();
  }
  // 작은 배
  ctx.save();
  ctx.translate(108, 372);
  ctx.fillStyle = '#FFFFFF';
  ctx.beginPath();
  ctx.moveTo(-26, 0);
  ctx.lineTo(26, 0);
  ctx.lineTo(18, 10);
  ctx.lineTo(-18, 10);
  ctx.closePath();
  ctx.fill();
  ctx.fillStyle = '#F0645A';
  ctx.fillRect(-23, 2, 46, 3);
  ctx.fillStyle = '#FFFFFF';
  rr(ctx, -10, -12, 18, 12, 3);
  ctx.fill();
  ctx.fillStyle = '#8FD3F4';
  ctx.fillRect(-6, -9, 10, 5);
  ctx.restore();

  // 방파제와 등대
  ctx.fillStyle = '#C9C2B5';
  rr(ctx, 290, 404, 180, 22, 6);
  ctx.fill();
  ctx.fillStyle = '#ECE7DD';
  rr(ctx, 290, 398, 180, 12, 6);
  ctx.fill();
  for (let i = 0; i < 4; i++) {
    ctx.fillStyle = i % 2 ? '#D9D3C8' : '#E7E2D8';
    ctx.beginPath();
    ctx.arc(298 + i * 18, 428, 11, Math.PI, 0);
    ctx.fill();
  }
  ctx.save();
  ctx.translate(412, 400);
  ctx.fillStyle = '#FFFFFF';
  ctx.beginPath();
  ctx.moveTo(-14, 0);
  ctx.lineTo(14, 0);
  ctx.lineTo(10, -66);
  ctx.lineTo(-10, -66);
  ctx.closePath();
  ctx.fill();
  ctx.fillStyle = '#F0645A';
  for (const y of [-16, -42]) {
    ctx.beginPath();
    ctx.moveTo(-13.4, y + 6);
    ctx.lineTo(13.4, y + 6);
    ctx.lineTo(12.4, y - 8);
    ctx.lineTo(-12.4, y - 8);
    ctx.closePath();
    ctx.fill();
  }
  ctx.fillStyle = 'rgba(0,0,0,0.06)';
  ctx.fillRect(2, -66, 12, 66);
  glow(ctx, 0, -76, 34, 'rgba(255,230,140,0.9)');
  ctx.fillStyle = '#3B4A5C';
  rr(ctx, -13, -70, 26, 6, 3);
  ctx.fill();
  ctx.fillStyle = '#FFE08A';
  rr(ctx, -8, -84, 16, 14, 4);
  ctx.fill();
  ctx.fillStyle = '#F0645A';
  ctx.beginPath();
  ctx.moveTo(-12, -84);
  ctx.quadraticCurveTo(0, -100, 12, -84);
  ctx.closePath();
  ctx.fill();
  ctx.restore();

  // 모래 해변과 마을 바닥
  ctx.fillStyle = '#F7E3BC';
  ctx.beginPath();
  ctx.moveTo(0, 452);
  for (let x = 0; x <= 450; x += 30) ctx.quadraticCurveTo(x + 15, 446 + (x % 60 ? 4 : -2), x + 30, 452);
  ctx.lineTo(450, 660);
  ctx.lineTo(0, 660);
  ctx.closePath();
  ctx.fill();
  ctx.strokeStyle = 'rgba(255,255,255,0.9)';
  ctx.lineWidth = 3;
  ctx.beginPath();
  ctx.moveTo(0, 452);
  for (let x = 0; x <= 450; x += 30) ctx.quadraticCurveTo(x + 15, 446 + (x % 60 ? 4 : -2), x + 30, 452);
  ctx.stroke();
  ctx.fillStyle = '#F4E6CC';
  ctx.fillRect(0, 520, 450, 140);

  // 덤불
  const bush = (x: number, y: number, s: number) => {
    ctx.fillStyle = '#7CC36B';
    for (const [dx, dy, rad] of [[-10, 0, 12], [6, -4, 14], [20, 2, 10]]) {
      ctx.beginPath();
      ctx.arc(x + dx * s, y + dy * s, rad * s, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.fillStyle = '#9AD884';
    ctx.beginPath();
    ctx.arc(x + 2 * s, y - 9 * s, 6 * s, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#FF7A8A';
    for (const [dx, dy] of [[-8, -4], [10, -10], [18, 0]]) {
      ctx.beginPath();
      ctx.arc(x + dx * s, y + dy * s, 2.4 * s, 0, Math.PI * 2);
      ctx.fill();
    }
  };

  // 위판장
  withShadow(ctx, 'rgba(60,70,90,0.22)', 14, 6, () => {
    ctx.fillStyle = '#EAF6F4';
    rr(ctx, 16, 434, 150, 106, 12);
    ctx.fill();
  });
  ctx.fillStyle = '#4D8FD8';
  rr(ctx, 8, 412, 166, 30, [16, 16, 4, 4]);
  ctx.fill();
  ctx.fillStyle = '#6BA8E8';
  rr(ctx, 8, 412, 166, 10, [16, 16, 0, 0]);
  ctx.fill();
  scallop(ctx, 8, 442, 166, 9, ['#4D8FD8', '#3D7DC4']);
  ctx.fillStyle = '#41596E';
  rr(ctx, 32, 466, 118, 74, [10, 10, 0, 0]);
  ctx.fill();
  ctx.fillStyle = '#FFE7A8';
  ctx.globalAlpha = 0.25;
  rr(ctx, 32, 466, 118, 30, [10, 10, 0, 0]);
  ctx.fill();
  ctx.globalAlpha = 1;
  const crate = (x: number, y: number, c: string) => {
    ctx.fillStyle = c;
    rr(ctx, x, y, 30, 18, 4);
    ctx.fill();
    ctx.fillStyle = 'rgba(255,255,255,0.35)';
    rr(ctx, x + 3, y + 3, 24, 4, 2);
    ctx.fill();
  };
  crate(40, 520, '#5FB0DC');
  crate(74, 520, '#F2B84B');
  crate(108, 520, '#5FB0DC');
  crate(57, 502, '#E9F4FB');
  crate(91, 502, '#F2B84B');

  // 미끼가게
  withShadow(ctx, 'rgba(60,70,90,0.22)', 14, 6, () => {
    ctx.fillStyle = '#FFE8A6';
    rr(ctx, 176, 452, 92, 88, 10);
    ctx.fill();
  });
  ctx.fillStyle = '#9A6B4B';
  rr(ctx, 172, 444, 100, 14, 7);
  ctx.fill();
  for (let i = 0; i < 6; i++) {
    ctx.fillStyle = i % 2 ? '#FFFFFF' : '#5DBB7A';
    ctx.fillRect(176 + i * 15.3, 458, 15.3, 16);
  }
  scallop(ctx, 176, 474, 92, 7.6, ['#5DBB7A', '#FFFFFF']);
  ctx.fillStyle = '#A8754F';
  rr(ctx, 206, 494, 32, 46, [16, 16, 0, 0]);
  ctx.fill();
  ctx.fillStyle = '#FFD15C';
  ctx.beginPath();
  ctx.arc(231, 518, 2.5, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#4D8FD8';
  rr(ctx, 182, 512, 18, 22, [3, 3, 6, 6]);
  ctx.fill();
  ctx.fillStyle = '#F0645A';
  rr(ctx, 246, 516, 16, 18, [3, 3, 6, 6]);
  ctx.fill();

  // 여울식당
  withShadow(ctx, 'rgba(60,70,90,0.22)', 14, 6, () => {
    ctx.fillStyle = '#FFF5E6';
    rr(ctx, 282, 446, 150, 94, 10);
    ctx.fill();
  });
  ctx.fillStyle = '#C49A72';
  ctx.fillRect(286, 450, 7, 90);
  ctx.fillRect(421, 450, 7, 90);
  // 기와지붕
  ctx.fillStyle = '#56617A';
  ctx.beginPath();
  ctx.moveTo(266, 440);
  ctx.quadraticCurveTo(272, 452, 262, 450);
  ctx.quadraticCurveTo(300, 404, 357, 402);
  ctx.quadraticCurveTo(414, 404, 452, 450);
  ctx.quadraticCurveTo(442, 452, 448, 440);
  ctx.closePath();
  ctx.fill();
  ctx.fillStyle = '#6E7A94';
  ctx.beginPath();
  ctx.moveTo(278, 426);
  ctx.quadraticCurveTo(318, 404, 357, 404);
  ctx.quadraticCurveTo(396, 404, 436, 426);
  ctx.lineTo(430, 430);
  ctx.quadraticCurveTo(396, 412, 357, 412);
  ctx.quadraticCurveTo(318, 412, 284, 430);
  ctx.closePath();
  ctx.fill();
  ctx.fillStyle = '#434C61';
  ctx.fillRect(268, 444, 178, 5);
  // 창문과 문
  const win = (x: number) => {
    glow(ctx, x + 17, 484, 30, 'rgba(255,214,120,0.55)');
    ctx.fillStyle = '#FFE1A0';
    rr(ctx, x, 468, 34, 30, 4);
    ctx.fill();
    ctx.strokeStyle = '#D2A26A';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(x + 17, 468);
    ctx.lineTo(x + 17, 498);
    ctx.moveTo(x, 483);
    ctx.lineTo(x + 34, 483);
    ctx.stroke();
  };
  win(302);
  win(378);
  ctx.fillStyle = '#9A6B4B';
  rr(ctx, 341, 476, 34, 64, 4);
  ctx.fill();
  ctx.fillStyle = '#4F6BA8';
  rr(ctx, 337, 466, 42, 26, [4, 4, 0, 0]);
  ctx.fill();
  ctx.fillStyle = '#FFFFFF';
  ctx.fillRect(357, 466, 2, 26);
  ctx.beginPath();
  ctx.arc(348, 478, 4, 0, Math.PI * 2);
  ctx.arc(368, 478, 4, 0, Math.PI * 2);
  ctx.fill();

  bush(288, 540, 0.9);
  bush(430, 538, 0.8);
  bush(178, 542, 0.7);

  // 길
  ctx.fillStyle = '#EFE2C9';
  ctx.fillRect(0, 548, 450, 10);
  ctx.fillStyle = '#D8D2C8';
  ctx.fillRect(0, 558, 450, 54);
  ctx.fillStyle = '#FFFFFF';
  for (let x = 8; x < 450; x += 48) {
    rr(ctx, x, 583, 24, 4, 2);
    ctx.fill();
  }
  ctx.fillStyle = '#EFE2C9';
  ctx.fillRect(0, 612, 450, 48);
}

function drawTruck(ctx: Ctx): void {
  // 160×86 안에 그린다
  ctx.fillStyle = 'rgba(40,50,70,0.18)';
  ctx.beginPath();
  ctx.ellipse(82, 80, 78, 7, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#7EC8E3';
  rr(ctx, 4, 26, 50, 44, [18, 8, 6, 6]);
  ctx.fill();
  ctx.fillStyle = '#E6F7FD';
  rr(ctx, 12, 32, 30, 18, [10, 4, 3, 3]);
  ctx.fill();
  ctx.fillStyle = '#5FB2D1';
  ctx.fillRect(4, 56, 50, 5);
  withShadow(ctx, 'rgba(40,50,70,0.18)', 6, 2, () => {
    ctx.fillStyle = '#FFF8EC';
    rr(ctx, 50, 4, 106, 68, 10);
    ctx.fill();
  });
  for (let i = 0; i < 7; i++) {
    ctx.fillStyle = i % 2 ? '#FFFFFF' : '#F0645A';
    ctx.fillRect(58 + i * 13, 10, 13, 12);
  }
  scallop(ctx, 58, 22, 91, 6.5, ['#F0645A', '#FFFFFF']);
  ctx.fillStyle = '#8A5A3B';
  rr(ctx, 64, 30, 78, 20, 4);
  ctx.fill();
  ctx.fillStyle = '#C8CDD6';
  rr(ctx, 74, 38, 16, 10, 3);
  ctx.fill();
  rr(ctx, 98, 36, 20, 12, 3);
  ctx.fill();
  text(ctx, '여울호', 94, 61, font(800, 14), '#2F5689', 'center');
  for (const x of [28, 136]) {
    ctx.fillStyle = '#3B4252';
    ctx.beginPath();
    ctx.arc(x, 72, 12, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#C9CED6';
    ctx.beginPath();
    ctx.arc(x, 72, 5, 0, Math.PI * 2);
    ctx.fill();
  }
}

function drawYeoul(ctx: Ctx): void {
  // 50×80, 발끝이 아래
  ctx.fillStyle = 'rgba(40,50,70,0.18)';
  ctx.beginPath();
  ctx.ellipse(25, 77, 16, 4, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#34507A';
  rr(ctx, 16, 58, 8, 18, 4);
  ctx.fill();
  rr(ctx, 26, 58, 8, 18, 4);
  ctx.fill();
  ctx.fillStyle = '#FFCE4F';
  ctx.beginPath();
  ctx.moveTo(14, 36);
  ctx.quadraticCurveTo(25, 30, 36, 36);
  ctx.lineTo(40, 64);
  ctx.quadraticCurveTo(25, 68, 10, 64);
  ctx.closePath();
  ctx.fill();
  ctx.fillStyle = '#F2B731';
  ctx.fillRect(24, 38, 2.5, 26);
  rr(ctx, 6, 40, 8, 18, 4);
  ctx.fill();
  rr(ctx, 36, 40, 8, 18, 4);
  ctx.fill();
  ctx.fillStyle = '#FFD9B8';
  ctx.beginPath();
  ctx.arc(25, 22, 15, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#3B2A26';
  ctx.beginPath();
  ctx.arc(25, 18, 15.5, Math.PI * 1.02, Math.PI * 1.98);
  ctx.fill();
  rr(ctx, 9, 14, 6, 16, 3);
  ctx.fill();
  rr(ctx, 35, 14, 6, 16, 3);
  ctx.fill();
  ctx.beginPath();
  ctx.arc(25, 3, 6, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#2B2B2B';
  for (const x of [19, 31]) {
    ctx.beginPath();
    ctx.ellipse(x, 24, 1.8, 2.4, 0, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.fillStyle = 'rgba(255,130,130,0.45)';
  ctx.beginPath();
  ctx.arc(15, 29, 3, 0, Math.PI * 2);
  ctx.arc(35, 29, 3, 0, Math.PI * 2);
  ctx.fill();
}

function drawCat(ctx: Ctx): void {
  // 48×44
  ctx.fillStyle = 'rgba(40,50,70,0.18)';
  ctx.beginPath();
  ctx.ellipse(22, 42, 16, 3.5, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.strokeStyle = '#8E98A4';
  ctx.lineWidth = 6;
  ctx.lineCap = 'round';
  ctx.beginPath();
  ctx.moveTo(34, 38);
  ctx.quadraticCurveTo(48, 36, 42, 22);
  ctx.stroke();
  ctx.fillStyle = '#9AA3AE';
  ctx.beginPath();
  ctx.ellipse(22, 31, 14, 12, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#FFFFFF';
  ctx.beginPath();
  ctx.ellipse(22, 34, 7, 8, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#9AA3AE';
  ctx.beginPath();
  ctx.moveTo(8, 10);
  ctx.lineTo(13, 0);
  ctx.lineTo(18, 7);
  ctx.moveTo(26, 7);
  ctx.lineTo(31, 0);
  ctx.lineTo(36, 10);
  ctx.fill();
  ctx.beginPath();
  ctx.arc(22, 14, 13, 0, Math.PI * 2);
  ctx.fill();
  ctx.strokeStyle = '#6E7782';
  ctx.lineWidth = 2;
  for (const x of [17, 22, 27]) {
    ctx.beginPath();
    ctx.moveTo(x, 2);
    ctx.lineTo(x, 7);
    ctx.stroke();
  }
  ctx.fillStyle = '#2B2B2B';
  ctx.beginPath();
  ctx.arc(17, 15, 1.8, 0, Math.PI * 2);
  ctx.arc(27, 15, 1.8, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = '#FF9AA2';
  ctx.beginPath();
  ctx.arc(22, 19, 1.5, 0, Math.PI * 2);
  ctx.fill();
  // 목의 찌
  ctx.fillStyle = '#F0645A';
  rr(ctx, 19, 24, 6, 9, 3);
  ctx.fill();
  ctx.fillStyle = '#FFFFFF';
  ctx.fillRect(19, 29, 6, 2);
}

// 메뉴 아이콘 (흰색으로 그린다)
type Icon = 'rod' | 'fish' | 'truck' | 'bait' | 'bowl' | 'book';
function icon(ctx: Ctx, kind: Icon, x: number, y: number): void {
  ctx.save();
  ctx.translate(x, y);
  ctx.strokeStyle = '#FFFFFF';
  ctx.fillStyle = '#FFFFFF';
  ctx.lineWidth = 2.2;
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.beginPath();
  switch (kind) {
    case 'rod':
      ctx.moveTo(-7, 8);
      ctx.lineTo(6, -8);
      ctx.moveTo(6, -8);
      ctx.lineTo(6, 2);
      ctx.arc(3, 2, 3, 0, Math.PI);
      ctx.stroke();
      break;
    case 'fish':
      ctx.ellipse(-2, 0, 7, 4.5, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(4, 0);
      ctx.lineTo(9, -4);
      ctx.lineTo(9, 4);
      ctx.fill();
      break;
    case 'truck':
      rr(ctx, -8, -5, 11, 9, 2);
      ctx.fill();
      rr(ctx, 3, -2, 6, 6, 2);
      ctx.fill();
      ctx.beginPath();
      ctx.arc(-4, 6, 2.2, 0, Math.PI * 2);
      ctx.arc(5, 6, 2.2, 0, Math.PI * 2);
      ctx.fill();
      break;
    case 'bait':
      ctx.moveTo(-7, 2);
      ctx.quadraticCurveTo(-3, -6, 0, 0);
      ctx.quadraticCurveTo(3, 6, 7, -2);
      ctx.stroke();
      break;
    case 'bowl':
      ctx.arc(0, 0, 7, 0, Math.PI);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(-3, -3);
      ctx.quadraticCurveTo(-1, -6, -3, -9);
      ctx.moveTo(3, -3);
      ctx.quadraticCurveTo(5, -6, 3, -9);
      ctx.stroke();
      break;
    case 'book':
      rr(ctx, -7, -8, 14, 16, 2);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(-3, -3);
      ctx.lineTo(3, -3);
      ctx.moveTo(-3, 1);
      ctx.lineTo(3, 1);
      ctx.stroke();
      break;
  }
  ctx.restore();
}

function drawUi(ctx: Ctx): void {
  // 상단 카드
  withShadow(ctx, 'rgba(36,50,74,0.18)', 18, 6, () => {
    ctx.fillStyle = 'rgba(255,255,255,0.95)';
    rr(ctx, 12, 14, 426, 94, 22);
    ctx.fill();
  });
  text(ctx, '4일차', 30, 42, font(800, 22), INK);
  const slots = ['새벽', '아침', '낮', '저녁'];
  let sx = 104;
  slots.forEach((s, i) => {
    const w = s.length === 1 ? 34 : 44;
    const cur = i === 2;
    ctx.fillStyle = cur ? '#FFB238' : i < 2 ? '#EEF1F4' : '#FFFFFF';
    rr(ctx, sx, 29, w, 26, 13);
    ctx.fill();
    if (!cur && i > 2) {
      ctx.strokeStyle = '#E2E7EC';
      ctx.lineWidth = 1.5;
      ctx.stroke();
    }
    text(ctx, s, sx + w / 2, 42.5, font(cur ? 700 : 600, 13), cur ? '#FFFFFF' : i < 2 ? '#A8B0BA' : '#6C7785', 'center');
    sx += w + 6;
  });
  ctx.fillStyle = '#FF6B6B';
  rr(ctx, 362, 28, 60, 28, 14);
  ctx.fill();
  text(ctx, 'D-26', 392, 42.5, font(800, 15), '#FFFFFF', 'center');

  ctx.fillStyle = '#FFC93C';
  ctx.beginPath();
  ctx.arc(39, 84, 10, 0, Math.PI * 2);
  ctx.fill();
  ctx.strokeStyle = '#F2A81D';
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.arc(39, 84, 6, 0, Math.PI * 2);
  ctx.stroke();
  text(ctx, '86,000원', 56, 85, font(800, 18), INK);
  text(ctx, '남은 빚 2,640,000원', 422, 77, font(600, 12), SUB, 'right');
  ctx.fillStyle = '#EEF1F4';
  rr(ctx, 272, 88, 150, 7, 4);
  ctx.fill();
  ctx.fillStyle = '#5BC0EB';
  rr(ctx, 272, 88, 150 * 0.12, 7, 4);
  ctx.fill();

  // 소식
  ctx.font = font(600, 13);
  const news = '노래미 값이 23% 떨어졌다';
  const nw = ctx.measureText(news).width + 96;
  ctx.fillStyle = 'rgba(255,255,255,0.8)';
  rr(ctx, 12, 120, nw, 30, 15);
  ctx.fill();
  ctx.fillStyle = '#2E9ACB';
  rr(ctx, 16, 124, 66, 22, 11);
  ctx.fill();
  text(ctx, '오늘 소식', 49, 135.5, font(700, 12), '#FFFFFF', 'center');
  text(ctx, news, 90, 135.5, font(600, 13), '#3B4A5C');

  // 장소 이름표
  pill(ctx, 91, 396, '위판장', '#2BA58A');
  pill(ctx, 222, 430, '미끼가게', '#5BAE45');
  pill(ctx, 357, 428, '여울식당', '#E8962E');
  pill(ctx, 334, 372, '방파제', '#3D8BE0', true);

  // 아래 메뉴 시트
  withShadow(ctx, 'rgba(36,50,74,0.16)', 24, -6, () => {
    ctx.fillStyle = '#FFFFFF';
    rr(ctx, 0, 640, 450, 170, [28, 28, 0, 0]);
    ctx.fill();
  });
  ctx.fillStyle = '#E1E5EA';
  rr(ctx, 205, 650, 40, 5, 3);
  ctx.fill();
  const tiles: [string, string, string, string, Icon][] = [
    ['방파제', '낚시 · 1칸', '#E6F3FF', '#3D8BE0', 'rod'],
    ['위판장', '경매 중', '#E3F7F1', '#2BA58A', 'fish'],
    ['여울호', '장사 · 1칸', '#FFECE9', '#F0645A', 'truck'],
    ['미끼가게', '장비 · 미끼', '#EEF8E3', '#5BAE45', 'bait'],
    ['여울식당', '상환 · 휴식', '#FFF2DF', '#E8962E', 'bowl'],
    ['수첩', '도감 · 미각', '#EFEBFE', '#7C6BDB', 'book'],
  ];
  tiles.forEach(([label, sub, bg, col, ic], i) => {
    const x = 16 + (i % 3) * 142;
    const y = 666 + Math.floor(i / 3) * 70;
    ctx.fillStyle = bg;
    rr(ctx, x, y, 134, 60, 18);
    ctx.fill();
    ctx.fillStyle = col;
    ctx.beginPath();
    ctx.arc(x + 28, y + 30, 17, 0, Math.PI * 2);
    ctx.fill();
    icon(ctx, ic, x + 28, y + 30);
    text(ctx, label, x + 52, y + 22, font(800, 16), INK);
    text(ctx, sub, x + 52, y + 42, font(600, 12), SUB);
  });
}

export class CasualScene extends Phaser.Scene {
  constructor() {
    super('Casual');
  }

  create(): void {
    const cam = this.cameras.main;
    cam.setZoom(SCALE).centerOn(225, 400);
    const img = (key: string, x: number, y: number, w: number, h: number) =>
      this.add.image(x, y, key).setDisplaySize(w, h);

    img(canvasTex(this, 'bg', 450, 800, SCALE, drawScene), 225, 400, 450, 800);
    const c1 = img(canvasTex(this, 'cloud1', 160, 70, SCALE, (ctx) => cloud(ctx, 80, 36, 1)), 92, 196, 160, 70);
    const c2 = img(canvasTex(this, 'cloud2', 120, 56, SCALE, (ctx) => cloud(ctx, 60, 28, 0.72)), 236, 172, 120, 56);
    this.tweens.add({ targets: c1, x: 112, duration: 6000, yoyo: true, repeat: -1, ease: 'Sine.InOut' });
    this.tweens.add({ targets: c2, x: 216, duration: 7000, yoyo: true, repeat: -1, ease: 'Sine.InOut' });

    // 반짝이는 물결
    for (let i = 0; i < 10; i++) {
      const s = this.add.ellipse(30 + i * 42, 350 + ((i * 37) % 90), 6, 2, 0xffffff, 0.9).setAlpha(0);
      this.tweens.add({ targets: s, alpha: 0.9, duration: 700, yoyo: true, repeat: -1, delay: i * 260 });
    }

    img(canvasTex(this, 'truck', 160, 86, SCALE, drawTruck), 230, 566, 160, 86);
    const yeoul = img(canvasTex(this, 'yeoul', 50, 80, SCALE, drawYeoul), 104, 590, 50, 80);
    this.tweens.add({ targets: yeoul, y: 587, duration: 800, yoyo: true, repeat: -1, ease: 'Sine.InOut' });
    const cat = img(canvasTex(this, 'cat', 48, 44, SCALE, drawCat), 352, 604, 48, 44);
    this.tweens.add({ targets: cat, scaleY: cat.scaleY * 0.96, duration: 900, yoyo: true, repeat: -1 });

    img(canvasTex(this, 'ui', 450, 800, SCALE, drawUi), 225, 400, 450, 800);
  }
}
