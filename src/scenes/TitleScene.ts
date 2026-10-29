import Phaser from 'phaser';
import { C, GAME_W, T } from '../config';
import { TITLE } from '../data/story';
import { newGame } from '../state/actions';
import { hasSave, loadSave } from '../state/save';
import { backdrop, sprite, Button, fadeIn, goTo, modal, txt } from '../ui/ui';

export class TitleScene extends Phaser.Scene {
  constructor() {
    super('Title');
  }

  create(): void {
    fadeIn(this);
    backdrop(this, 'bg-village-3');
    const shade = this.add.graphics();
    shade.fillGradientStyle(0x0f1a2b, 0x0f1a2b, 0x0f1a2b, 0x0f1a2b, 0.55, 0.55, 0, 0);
    shade.fillRect(0, 0, GAME_W, 320);
    shade.fillGradientStyle(0x0f1a2b, 0x0f1a2b, 0x0f1a2b, 0x0f1a2b, 0, 0, 0.7, 0.7);
    shade.fillRect(0, 560, GAME_W, 240);

    const cat = sprite(this, 'ch-cat-sleep', 330, 600, 52);
    this.tweens.add({ targets: cat, scaleY: cat.scaleY * 1.04, duration: 1600, yoyo: true, repeat: -1, ease: 'Sine.InOut' });

    txt(this, GAME_W / 2, 140, TITLE.name, { size: 'title', color: T.paper, origin: [0.5, 0.5], stroke: '#1b2a41' });
    txt(this, GAME_W / 2, 186, TITLE.subtitle, { size: 'h1', color: '#f3d9a4', origin: [0.5, 0.5], stroke: '#1b2a41' });
    txt(this, GAME_W / 2, 226, '나는 맛을 잃었다.\n다시 맛을 느낄 수 있는 건\n내 손으로 낚은 물고기뿐이다.', {
      size: 'small', color: '#fbf3e0', origin: [0.5, 0], align: 'center', lineSpacing: 8, stroke: '#1b2a41',
    });

    const saved = hasSave();
    new Button(this, GAME_W / 2, 650, '새로 시작', () => {
      if (saved) {
        modal(this, {
          title: '새로 시작할까요?',
          body: '지금까지의 기록이 지워져요.',
          buttons: [
            { label: '취소', color: C.grayDark },
            { label: '새로 시작', color: C.red, onClick: () => this.startNew() },
          ],
        });
      } else this.startNew();
    }, { w: 240, h: 56, color: C.red });

    new Button(this, GAME_W / 2, 722, '이어하기', () => {
      if (loadSave()) goTo(this, 'Town');
    }, { w: 240, h: 56, color: C.blue, disabled: !saved });

    txt(this, GAME_W / 2, 782, 'v0.1 MVP · 1장 물비늘포', { size: 'small', color: '#d8d0c0', origin: [0.5, 0.5] });
  }

  private startNew(): void {
    newGame();
    goTo(this, 'Prologue', { part: 1 });
  }
}
