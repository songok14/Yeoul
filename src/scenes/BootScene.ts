import Phaser from 'phaser';
import { createTextures } from '../ui/art';
import { newGame } from '../state/actions';
import { S, loadSave, persist } from '../state/save';
import { SPECIES } from '../data/fish';

/** 텍스처를 만들고 첫 화면으로 보낸다. ?scene=Town&demo=1 처럼 바로 이동할 수도 있다 */
export class BootScene extends Phaser.Scene {
  constructor() {
    super('Boot');
  }

  preload(): void {
    const images = [
      'bg-village-0', 'bg-village-1', 'bg-village-2', 'bg-village-3', 'bg-breakwater', 'bg-market', 'bg-plaza',
      'bg-baitshop', 'bg-restaurant', 'bg-alley', 'bg-truckstop', 'ch-yeoul', 'ch-yeoul-back', 'ch-grandma', 'ch-grandma-stew',
      'ch-nossi', 'ch-cat', 'ch-cat-walk', 'ch-cat-sleep', 'truck',
      'fish-bollak', 'fish-godeungeo', 'fish-noraemi', 'fish-hakgongchi', 'fish-gamseongdom',
      'y-notaste', 'y-knees', 'y-phone', 'y-back-bag', 'y-rod-broken', 'y-journal', 'y-salty-tear', 'y-peace',
      'y-chef-cheer', 'y-chef-nervous',
    ];
    for (const key of images) this.load.image(key, `assets/${key}.webp`);
  }

  create(): void {
    createTextures(this);
    document.getElementById('loading')?.remove();

    const params = new URLSearchParams(location.search);
    const target = params.get('scene');
    if (params.get('demo')) {
      setupDemo(params.get('demo') ?? '1');
    } else if (target && target !== 'Title' && target !== 'Prologue') {
      if (!loadSave()) setupDemo('1');
    }
    if (target && this.scene.manager.keys[target]) {
      const data: Record<string, unknown> = {};
      params.forEach((v, k) => {
        if (k !== 'scene' && k !== 'demo') data[k] = v;
      });
      this.scene.start(target, data);
      return;
    }
    this.scene.start('Title');
  }
}

/** 스크린샷 확인용 체험 세이브 */
function setupDemo(kind: string): void {
  newGame();
  S.flags.prologueDone = true;
  S.flags.firstTaste = true;
  S.flags.taste1 = true;
  S.money = 86_000;
  S.day = 4;
  S.slot = kind === 'evening' ? 3 : 1;
  S.debt = 2_640_000;
  const catches: [typeof SPECIES[number]['id'], number, number][] = [
    ['bollak', 19.5, 100],
    ['bollak', 22.1, 60],
    ['godeungeo', 31.2, 100],
    ['noraemi', 26.4, 35],
    ['noraemi', 21, 100],
    ['hakgongchi', 33.3, 60],
  ];
  for (const [sp, size, fresh] of catches) {
    S.cooler.push({ uid: S.nextUid++, species: sp, size, caughtDay: S.day, fresh });
    const e = S.dex[sp] ?? { count: 0, best: 0, released: 0 };
    e.count += 1;
    e.best = Math.max(e.best, size);
    S.dex[sp] = e;
  }
  S.flags.taste2 = true;
  persist();
}
