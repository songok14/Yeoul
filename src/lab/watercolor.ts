// 수채화 그리기 도구: 종이, 물감 번짐, 떨리는 잉크 선

import { rng, type Ctx } from './paint';

export const INK = '#4B3A2F';
export const SERIF = "'Gowun Batang'";
export const SANS = "'Gowun Dodum'";

let r = rng(3);

export function seed(n: number): void {
  r = rng(n);
}

export const rand = (): number => r();

/** 울퉁불퉁한 덩어리 경로 */
export function blob(cx: number, cy: number, rx: number, ry: number, wobble = 0.16, n = 16): Path2D {
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
export function wrect(x: number, y: number, w: number, h: number, j = 1.6): Path2D {
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
export function wash(ctx: Ctx, path: Path2D, color: string, alpha = 0.4, blur = 1.4, edge = true, knock = false): void {
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
export function ink(ctx: Ctx, pts: [number, number][], w = 1.1, color = INK, alpha = 0.85, close = false): void {
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

export const inkRect = (ctx: Ctx, x: number, y: number, w: number, h: number, wd = 1.1) =>
  ink(ctx, [[x, y], [x + w, y], [x + w, y + h], [x, y + h]], wd, INK, 0.85, true);

export function paper(ctx: Ctx, x: number, y: number, w: number, h: number, base = '#F6EEDD'): void {
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

