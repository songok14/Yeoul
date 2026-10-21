import Phaser from 'phaser';
import { GAME_H, GAME_W, RENDER_SCALE } from './config';
import { loadFonts } from './fonts';
import { BootScene } from './scenes/BootScene';
import { FishingScene } from './scenes/FishingScene';
import { MarketScene } from './scenes/MarketScene';
import { NotebookScene } from './scenes/NotebookScene';
import { PrologueScene } from './scenes/PrologueScene';
import { ShopScene } from './scenes/ShopScene';
import { TitleScene } from './scenes/TitleScene';
import { TownScene } from './scenes/TownScene';
import { TruckScene } from './scenes/TruckScene';

async function start(): Promise<void> {
  await loadFonts();
  const game = new Phaser.Game({
    type: Phaser.AUTO,
    parent: 'game',
    width: GAME_W * RENDER_SCALE,
    height: GAME_H * RENDER_SCALE,
    backgroundColor: '#1b2a41',
    antialias: true,
    scale: {
      mode: Phaser.Scale.FIT,
      autoCenter: Phaser.Scale.CENTER_BOTH,
    },
    input: { activePointers: 2 },
    scene: [BootScene, TitleScene, PrologueScene, TownScene, FishingScene, MarketScene, TruckScene, ShopScene, NotebookScene],
  });
  (window as unknown as { __game: Phaser.Game }).__game = game;
}

void start();
