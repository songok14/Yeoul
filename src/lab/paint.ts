// 스타일 시안용 캔버스 그리기 도우미

import Phaser from 'phaser';

export type Ctx = CanvasRenderingContext2D;

/** 논리 크기 w×h를 scale배 캔버스 텍스처로 그린다 */
export function canvasTex(scene: Phaser.Scene, key: string, w: number, h: number, scale: number, draw: (ctx: Ctx) => void): string {
  const tex = scene.textures.createCanvas(key, Math.round(w * scale), Math.round(h * scale));
  if (!tex) return key;
  const ctx = tex.getContext();
  ctx.save();
  ctx.scale(scale, scale);
  draw(ctx);
  ctx.restore();
  tex.refresh();
  return key;
}

export function rr(ctx: Ctx, x: number, y: number, w: number, h: number, r: number | number[]): void {
  ctx.beginPath();
  ctx.roundRect(x, y, w, h, r);
}

export function rng(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function vgrad(ctx: Ctx, y0: number, y1: number, stops: [number, string][]): CanvasGradient {
  const g = ctx.createLinearGradient(0, y0, 0, y1);
  for (const [o, c] of stops) g.addColorStop(o, c);
  return g;
}

export function glow(ctx: Ctx, x: number, y: number, r: number, color: string, alpha = 1): void {
  const g = ctx.createRadialGradient(x, y, 0, x, y, r);
  g.addColorStop(0, color);
  g.addColorStop(1, 'rgba(255,255,255,0)');
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.fillStyle = g;
  ctx.fillRect(x - r, y - r, r * 2, r * 2);
  ctx.restore();
}

/** 그림자를 깐 채로 한 번 그린다 */
export function withShadow(ctx: Ctx, color: string, blur: number, oy: number, fn: () => void): void {
  ctx.save();
  ctx.shadowColor = color;
  ctx.shadowBlur = blur;
  ctx.shadowOffsetY = oy;
  fn();
  ctx.restore();
}

export function text(
  ctx: Ctx,
  s: string,
  x: number,
  y: number,
  font: string,
  color: string,
  align: CanvasTextAlign = 'left',
  baseline: CanvasTextBaseline = 'middle',
): number {
  ctx.font = font;
  ctx.fillStyle = color;
  ctx.textAlign = align;
  ctx.textBaseline = baseline;
  ctx.fillText(s, x, y);
  return ctx.measureText(s).width;
}

export async function loadFace(family: string, url: string, weight = '400'): Promise<void> {
  const face = new FontFace(family, `url(${url})`, { weight });
  await face.load();
  document.fonts.add(face);
}
