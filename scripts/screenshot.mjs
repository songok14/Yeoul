// 빌드된 게임을 띄워 주요 화면을 휴대폰 크기로 찍는다
// 사용법: npm run build && npm run shot [-- 이름 ...]
import { preview } from 'vite';
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';

const OUT = 'screenshots';
const PORT = 4173;
const only = process.argv.slice(2);

/** 게임 좌표(450x800)를 화면 좌표로 바꿔 누른다 */
async function tap(page, gx, gy, holdMs = 0) {
  const box = await page.locator('canvas').boundingBox();
  const x = box.x + (gx / 450) * box.width;
  const y = box.y + (gy / 800) * box.height;
  await page.mouse.move(x, y);
  await page.mouse.down();
  if (holdMs) await page.waitForTimeout(holdMs);
  await page.mouse.up();
}

const shots = [
  { name: '01-title', url: '/' },
  { name: '02-prologue', url: '/?scene=Prologue&part=1', wait: 2500 },
  {
    name: '03-prologue-salt',
    url: '/?scene=Prologue&part=2',
    wait: 2500,
    steps: async (page) => {
      await tap(page, 225, 400);
      await page.waitForTimeout(400);
      await tap(page, 225, 400);
      await page.waitForTimeout(700);
    },
  },
  { name: '04-town', url: '/?scene=Town&demo=1', wait: 1200 },
  { name: '05-fishing-aim', url: '/?scene=Fishing&demo=1', wait: 1500 },
  {
    name: '06-fishing-power',
    url: '/?scene=Fishing&demo=1',
    wait: 1200,
    steps: async (page) => {
      await tap(page, 225, 400);
      await page.waitForTimeout(500);
    },
  },
  {
    name: '07-fishing-reel',
    url: '/?scene=Fishing&tutorial=1',
    wait: 1200,
    steps: async (page) => {
      await tap(page, 225, 400);
      await page.waitForTimeout(500);
      await tap(page, 225, 400);
      // 튜토리얼은 2.2초 뒤 입질
      await page.waitForTimeout(3200);
      await tap(page, 225, 400);
      await page.waitForTimeout(200);
      await tap(page, 225, 400, 900);
      await page.waitForTimeout(150);
    },
  },
  { name: '08-market', url: '/?scene=Market&demo=1', wait: 1200 },
  { name: '09-market-request', url: '/?scene=Market&demo=1&tab=request', wait: 1200 },
  { name: '10-truck-menu', url: '/?scene=Truck&demo=1', wait: 1200 },
  {
    name: '11-truck-cook',
    url: '/?scene=Truck&demo=1',
    wait: 1200,
    steps: async (page) => {
      await tap(page, 330, 740);
      await page.waitForTimeout(1500);
    },
  },
  {
    name: '12-truck-serve',
    url: '/?scene=Truck&demo=1',
    wait: 1200,
    steps: async (page) => {
      await tap(page, 330, 740);
      await page.waitForTimeout(1200);
      for (let i = 0; i < 2; i++) {
        await tap(page, 225, 630);
        await page.waitForTimeout(900);
      }
      await page.waitForTimeout(500);
    },
  },
  { name: '13-shop', url: '/?scene=Shop&demo=1', wait: 1200 },
  { name: '14-notebook-cooler', url: '/?scene=Notebook&demo=1&tab=cooler', wait: 1000 },
  { name: '15-notebook-dex', url: '/?scene=Notebook&demo=1&tab=dex', wait: 1000 },
  { name: '16-notebook-taste', url: '/?scene=Notebook&demo=1&tab=taste', wait: 1000 },
  {
    name: '17-town-restaurant',
    url: '/?scene=Town&demo=1',
    wait: 1200,
    steps: async (page) => {
      await tap(page, 225, 758);
      await page.waitForTimeout(600);
    },
  },
];

await mkdir(OUT, { recursive: true });
const server = await preview({ preview: { port: PORT, strictPort: true, open: false }, logLevel: 'warn' });
const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] });
const errors = [];
try {
  for (const shot of shots) {
    if (only.length && !only.some((o) => shot.name.includes(o))) continue;
    const context = await browser.newContext({
      viewport: { width: 390, height: 844 },
      deviceScaleFactor: 2,
      isMobile: true,
      hasTouch: true,
    });
    const page = await context.newPage();
    page.on('console', (m) => {
      // 헤드리스 소프트웨어 WebGL 경고는 게임과 무관하다
      if (/swiftshader|GroupMarkerNotSet|GPU stall/i.test(m.text())) return;
      if (m.type() === 'error' || m.type() === 'warning') errors.push(`[${shot.name}] ${m.type()}: ${m.text()}`);
    });
    page.on('pageerror', (e) => errors.push(`[${shot.name}] pageerror: ${e.message}`));
    await page.goto(`http://localhost:${PORT}${shot.url}`);
    await page.waitForSelector('canvas');
    await page.waitForTimeout(shot.wait ?? 1500);
    if (shot.steps) await shot.steps(page);
    await page.screenshot({ path: `${OUT}/${shot.name}.png` });
    console.log('찍음', shot.name);
    await context.close();
  }
} finally {
  await browser.close();
  await new Promise((r) => server.httpServer.close(r));
}
if (errors.length) {
  console.log('\n콘솔 오류/경고:');
  for (const e of errors) console.log(e);
  process.exitCode = 1;
} else console.log('\n콘솔 오류 없음');
