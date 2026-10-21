// 공용 UI 부품: 글자, 버튼, 패널, 모달, 토스트

import Phaser from 'phaser';
import { C, FONT, FONT_BOLD, FONT_SMALL, GAME_H, GAME_W, RENDER_SCALE, T } from '../config';

export type TextSize = 'title' | 'h1' | 'body' | 'small';

const SIZE: Record<TextSize, { font: string; px: number; bold?: boolean }> = {
  title: { font: FONT_BOLD, px: 44, bold: true },
  h1: { font: FONT_BOLD, px: 22, bold: true },
  body: { font: FONT, px: 21 },
  small: { font: FONT_SMALL, px: 17 },
};

export interface TextOpts {
  size?: TextSize;
  color?: string;
  align?: 'left' | 'center' | 'right';
  wrap?: number;
  origin?: [number, number];
  stroke?: string;
  lineSpacing?: number;
}

export function txt(scene: Phaser.Scene, x: number, y: number, str: string, o: TextOpts = {}): Phaser.GameObjects.Text {
  const s = SIZE[o.size ?? 'body'];
  const t = scene.add.text(x, y, str, {
    fontFamily: `'${s.font}'`,
    fontStyle: s.bold ? 'bold' : 'normal',
    fontSize: `${s.px}px`,
    color: o.color ?? T.paper,
    align: o.align ?? 'left',
    lineSpacing: o.lineSpacing ?? 6,
    padding: { top: 2, bottom: 2 },
    wordWrap: o.wrap ? { width: o.wrap, useAdvancedWrap: true } : undefined,
    stroke: o.stroke,
    strokeThickness: o.stroke ? (o.size === 'title' ? 6 : o.size === 'small' ? 3 : 4) : 0,
  });
  const [ox, oy] = o.origin ?? [0, 0];
  t.setOrigin(ox, oy);
  // 카메라를 두 배로 확대하므로 글자도 두 배 해상도로 그린다
  t.setResolution(RENDER_SCALE);
  return t;
}

/** 도트 느낌 패널: 그림자, 테두리, 윗면 밝은 줄 */
export function drawPanel(
  g: Phaser.GameObjects.Graphics,
  x: number,
  y: number,
  w: number,
  h: number,
  fill: number = C.paper,
  border: number = C.ink,
  shadow = true,
): void {
  if (shadow) {
    g.fillStyle(C.black, 0.35);
    g.fillRect(x + 4, y + 4, w, h);
  }
  g.fillStyle(border, 1);
  g.fillRect(x, y, w, h);
  g.fillStyle(fill, 1);
  g.fillRect(x + 3, y + 3, w - 6, h - 6);
  g.fillStyle(C.white, 0.18);
  g.fillRect(x + 3, y + 3, w - 6, 3);
}

export interface ButtonOpts {
  w?: number;
  h?: number;
  color?: number;
  textColor?: string;
  size?: TextSize;
  sub?: string;
  disabled?: boolean;
}

export class Button extends Phaser.GameObjects.Container {
  private bg: Phaser.GameObjects.Graphics;
  private label: Phaser.GameObjects.Text;
  private subLabel?: Phaser.GameObjects.Text;
  private zone: Phaser.GameObjects.Zone;
  private bw: number;
  private bh: number;
  private color: number;
  enabled = true;

  constructor(scene: Phaser.Scene, x: number, y: number, label: string, onClick: () => void, o: ButtonOpts = {}) {
    super(scene, x, y);
    this.bw = o.w ?? 180;
    this.bh = o.h ?? 52;
    this.color = o.color ?? C.blue;
    this.bg = scene.add.graphics();
    this.add(this.bg);

    const ly = o.sub ? -10 : 0;
    this.label = txt(scene, 0, ly, label, { size: o.size ?? 'h1', color: o.textColor ?? T.white, origin: [0.5, 0.5], align: 'center' });
    this.add(this.label);
    if (o.sub) {
      this.subLabel = txt(scene, 0, 14, o.sub, { size: 'small', color: '#e8eef5', origin: [0.5, 0.5], align: 'center' });
      this.add(this.subLabel);
    }

    this.zone = scene.add.zone(0, 0, this.bw, this.bh).setInteractive({ useHandCursor: true });
    this.add(this.zone);
    let pressed = false;
    this.zone.on('pointerdown', () => {
      if (!this.enabled) return;
      pressed = true;
      this.draw(true);
    });
    this.zone.on('pointerout', () => {
      if (pressed) this.draw(false);
      pressed = false;
    });
    this.zone.on('pointerup', () => {
      if (!this.enabled || !pressed) return;
      pressed = false;
      this.draw(false);
      onClick();
    });

    this.draw(false);
    if (o.disabled) this.setEnabled(false);
    scene.add.existing(this);
  }

  private draw(pressed: boolean): void {
    const g = this.bg;
    const w = this.bw;
    const h = this.bh;
    const x = -w / 2;
    const y = -h / 2;
    const off = pressed ? 3 : 0;
    const base = this.enabled ? this.color : C.grayDark;
    g.clear();
    g.fillStyle(C.black, 0.4);
    g.fillRect(x + 3, y + 5, w, h);
    g.fillStyle(C.ink, 1);
    g.fillRect(x, y + off, w, h);
    g.fillStyle(base, 1);
    g.fillRect(x + 3, y + 3 + off, w - 6, h - 6);
    g.fillStyle(C.white, this.enabled ? 0.25 : 0.1);
    g.fillRect(x + 3, y + 3 + off, w - 6, 4);
    g.fillStyle(C.black, 0.18);
    g.fillRect(x + 3, y + h - 7 + off, w - 6, 4);
    this.label.setY((this.subLabel ? -10 : 0) + off);
    this.subLabel?.setY(14 + off);
  }

  setEnabled(v: boolean): this {
    this.enabled = v;
    this.label.setAlpha(v ? 1 : 0.6);
    this.subLabel?.setAlpha(v ? 1 : 0.7);
    this.draw(false);
    return this;
  }

  setLabel(s: string): this {
    this.label.setText(s);
    return this;
  }

  setSub(s: string): this {
    this.subLabel?.setText(s);
    return this;
  }
}

export interface ModalButton {
  label: string;
  color?: number;
  onClick?: () => void;
  disabled?: boolean;
  keepOpen?: boolean;
}

export interface ModalOpts {
  title?: string;
  body?: string;
  buttons: ModalButton[];
  width?: number;
  /** 본문 아래에 내용을 더 그리고 그만큼의 높이를 돌려준다 */
  extra?: (c: Phaser.GameObjects.Container, top: number, width: number) => number;
  vertical?: boolean;
}

/** 화면 가운데 대화상자. 뒤쪽 입력을 막는다 */
export function modal(scene: Phaser.Scene, o: ModalOpts): Phaser.GameObjects.Container {
  const w = o.width ?? 400;
  const c = scene.add.container(0, 0).setDepth(1000);
  const dim = scene.add.rectangle(0, 0, GAME_W, GAME_H, C.black, 0.55).setOrigin(0, 0).setInteractive();
  c.add(dim);

  const inner = scene.add.container(GAME_W / 2, 0);
  c.add(inner);
  const g = scene.add.graphics();
  inner.add(g);

  let y = 24;
  const parts: Phaser.GameObjects.GameObject[] = [];
  if (o.title) {
    const t = txt(scene, 0, y, o.title, { size: 'h1', color: T.ink, origin: [0.5, 0], align: 'center', wrap: w - 40 });
    parts.push(t);
    y += t.height + 12;
  }
  if (o.body) {
    const b = txt(scene, 0, y, o.body, { size: 'small', color: T.inkSoft, origin: [0.5, 0], align: 'center', wrap: w - 48 });
    parts.push(b);
    y += b.height + 16;
  }
  inner.add(parts);
  if (o.extra) y += o.extra(inner, y, w);

  const vertical = o.vertical ?? o.buttons.length > 2;
  const bh = 50;
  const close = () => c.destroy();
  if (vertical) {
    for (const b of o.buttons) {
      const btn = new Button(scene, 0, y + bh / 2, b.label, () => {
        if (!b.keepOpen) close();
        b.onClick?.();
      }, { w: w - 60, h: bh, color: b.color ?? C.blue, disabled: b.disabled });
      inner.add(btn);
      y += bh + 10;
    }
    y += 6;
  } else {
    const n = o.buttons.length;
    const bw = n === 1 ? 200 : (w - 60 - (n - 1) * 12) / n;
    o.buttons.forEach((b, i) => {
      const bx = n === 1 ? 0 : -((w - 60) / 2) + bw / 2 + i * (bw + 12);
      const btn = new Button(scene, bx, y + bh / 2, b.label, () => {
        if (!b.keepOpen) close();
        b.onClick?.();
      }, { w: bw, h: bh, color: b.color ?? C.blue, disabled: b.disabled });
      inner.add(btn);
    });
    y += bh + 16;
  }

  const h = y + 8;
  drawPanel(g, -w / 2, 0, w, h, C.paper, C.ink);
  inner.setY(Math.round((GAME_H - h) / 2));

  inner.setScale(0.9);
  inner.setAlpha(0);
  scene.tweens.add({ targets: inner, scale: 1, alpha: 1, duration: 140, ease: 'Back.Out' });
  return c;
}

/** 위로 떠오르며 사라지는 짧은 알림 */
export function toast(scene: Phaser.Scene, msg: string, color: string = T.gold, y = 120): void {
  const t = txt(scene, GAME_W / 2, y, msg, { size: 'h1', color, origin: [0.5, 0.5], stroke: '#1b2a41', align: 'center', wrap: 420 }).setDepth(1100);
  scene.tweens.add({ targets: t, y: y - 40, alpha: 0, delay: 900, duration: 700, onComplete: () => t.destroy() });
}

/** 떠오르는 숫자 (돈 들어올 때) */
export function floatText(scene: Phaser.Scene, x: number, y: number, msg: string, color: string = T.gold): void {
  const t = txt(scene, x, y, msg, { size: 'h1', color, origin: [0.5, 0.5], stroke: '#1b2a41' }).setDepth(1100);
  scene.tweens.add({ targets: t, y: y - 50, alpha: 0, duration: 1200, ease: 'Cubic.Out', onComplete: () => t.destroy() });
}

/** 말풍선 */
export function bubble(
  scene: Phaser.Scene,
  x: number,
  y: number,
  msg: string,
  o: { wrap?: number; tail?: 'down' | 'left' | 'none'; color?: string } = {},
): Phaser.GameObjects.Container {
  const c = scene.add.container(x, y);
  const t = txt(scene, 0, 0, msg, { size: 'small', color: o.color ?? T.ink, wrap: o.wrap ?? 220, align: 'center', origin: [0.5, 0.5] });
  const w = Math.max(60, t.width + 24);
  const h = t.height + 16;
  const g = scene.add.graphics();
  g.fillStyle(C.ink, 1);
  g.fillRect(-w / 2, -h / 2, w, h);
  g.fillStyle(C.white, 1);
  g.fillRect(-w / 2 + 2, -h / 2 + 2, w - 4, h - 4);
  if (o.tail !== 'none') {
    g.fillStyle(C.ink, 1);
    if (o.tail === 'left') g.fillTriangle(-w / 2 + 2, -4, -w / 2 + 2, 8, -w / 2 - 10, 10);
    else g.fillTriangle(-8, h / 2 - 2, 8, h / 2 - 2, 0, h / 2 + 10);
  }
  c.add([g, t]);
  return c;
}

/** 카메라를 어둡게 하고 다른 씬으로 넘어간다 */
export function goTo(scene: Phaser.Scene, key: string, data?: object): void {
  scene.input.enabled = false;
  scene.cameras.main.fadeOut(180, 15, 26, 43);
  scene.cameras.main.once(Phaser.Cameras.Scene2D.Events.FADE_OUT_COMPLETE, () => {
    // 데이터를 넘기지 않으면 Phaser가 이전 실행의 데이터를 재사용하므로 항상 새 객체를 넘긴다
    scene.scene.start(key, data ?? {});
  });
}

export function fadeIn(scene: Phaser.Scene): void {
  scene.input.enabled = true;
  // 모든 씬은 450x800 논리 좌표로 그리고, 카메라가 두 배로 확대해 보여 준다
  scene.cameras.main.setZoom(RENDER_SCALE).centerOn(GAME_W / 2, GAME_H / 2);
  scene.cameras.main.fadeIn(200, 15, 26, 43);
}

/** 화면을 꽉 채우는 배경 그림 */
export function backdrop(scene: Phaser.Scene, key: string, tint?: number): Phaser.GameObjects.Image {
  const img = scene.add.image(0, 0, key).setOrigin(0, 0).setDisplaySize(GAME_W, GAME_H);
  if (tint !== undefined) img.setTint(tint);
  return img;
}

/** 높이를 기준으로 비율을 지켜 그림을 놓는다. 기본 기준점은 발밑 */
export function sprite(scene: Phaser.Scene, key: string, x: number, y: number, h: number, origin: [number, number] = [0.5, 1]): Phaser.GameObjects.Image {
  const img = scene.add.image(x, y, key).setOrigin(origin[0], origin[1]);
  img.setScale(h / img.height);
  return img;
}
