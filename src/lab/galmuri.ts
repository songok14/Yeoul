import galmuri11Bold from 'galmuri/dist/Galmuri11-Bold.woff2?url';
import galmuri9 from 'galmuri/dist/Galmuri9.woff2?url';
import { loadFace } from './paint';

export const GALMURI_BOLD = 'Galmuri11Bold';
export const GALMURI_SMALL = 'Galmuri9';

export async function loadGalmuri(): Promise<void> {
  await Promise.all([loadFace(GALMURI_BOLD, galmuri11Bold), loadFace(GALMURI_SMALL, galmuri9)]);
}
