import galmuri11 from 'galmuri/dist/Galmuri11.woff2?url';
import galmuri11Bold from 'galmuri/dist/Galmuri11-Bold.woff2?url';
import galmuri9 from 'galmuri/dist/Galmuri9.woff2?url';
import { FONT, FONT_BOLD, FONT_SMALL } from './config';

/** Phaser 텍스트가 대체 글꼴로 그려지지 않도록 게임 시작 전에 글꼴을 모두 불러온다 */
export async function loadFonts(): Promise<void> {
  const faces = [
    new FontFace(FONT, `url(${galmuri11})`),
    new FontFace(FONT_BOLD, `url(${galmuri11Bold})`),
    new FontFace(FONT_SMALL, `url(${galmuri9})`),
  ];
  await Promise.all(
    faces.map(async (face) => {
      try {
        await face.load();
        document.fonts.add(face);
      } catch (err) {
        console.warn('글꼴을 불러오지 못했습니다', face.family, err);
      }
    }),
  );
}
