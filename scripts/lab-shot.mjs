// 스타일 시안 페이지를 찍는다. 사용법: npm run build && node scripts/lab-shot.mjs [1 2 3]
import { preview } from 'vite';
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';

const styles = process.argv.slice(2).length ? process.argv.slice(2) : ['1', '2', '3'];
await mkdir('screenshots', { recursive: true });
const server = await preview({ preview: { port: 4176, strictPort: true, open: false }, logLevel: 'warn' });
const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] });
const errors = [];
for (const s of styles) {
  const page = await browser.newPage({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2.3 });
  page.on('pageerror', (e) => errors.push(`[${s}] ${e.message}`));
  page.on('console', (m) => {
    if (m.type() === 'error' && !/swiftshader|GroupMarker/i.test(m.text())) errors.push(`[${s}] ${m.text()}`);
  });
  await page.goto(`http://localhost:4176/lab.html?style=${s.replace('sheet', '3&sheet=1')}`);
  await page.waitForSelector('canvas');
  await page.waitForTimeout(2500);
  await page.screenshot({ path: `screenshots/style-${s}.png` });
  console.log('찍음 style', s);
  await page.close();
}
await browser.close();
await new Promise((r) => server.httpServer.close(r));
console.log(errors.length ? errors.join('\n') : '오류 없음');
