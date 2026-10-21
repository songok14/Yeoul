// 실제 클릭으로 새 게임부터 하루를 끝까지 돌려 보는 자동 플레이 점검
// 사용법: npm run build && node scripts/playtest.mjs
import { preview } from 'vite';
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';

const PORT = 4174;
const server = await preview({ preview: { port: PORT, strictPort: true, open: false }, logLevel: 'warn' });
const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] });
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
const errors = [];
page.on('pageerror', (e) => errors.push(e.message));
page.on('console', (m) => {
  if (m.type() === 'error' && !/swiftshader|GroupMarker/i.test(m.text())) errors.push(m.text());
});
await mkdir('screenshots', { recursive: true });

const wait = (ms) => page.waitForTimeout(ms);
const scene = () => page.evaluate(() => window.__game.scene.getScenes(true).map((s) => s.scene.key).join(','));
const save = () => page.evaluate(() => JSON.parse(localStorage.getItem('hanjeopsi-save-v1')));

async function toScreen(gx, gy) {
  const box = await page.locator('canvas').boundingBox();
  return [box.x + (gx / 450) * box.width, box.y + (gy / 800) * box.height];
}
async function tap(gx, gy) {
  const [x, y] = await toScreen(gx, gy);
  await page.mouse.click(x, y);
}
/** 화면에 보이는 버튼을 글자로 찾아 누른다 */
async function press(label) {
  const pos = await page.evaluate((label) => {
    const found = [];
    const walk = (objs) => {
      for (const o of objs) {
        if (o.list) walk(o.list);
        if (o.type === 'Text' && o.text.startsWith(label) && o.parentContainer?.zone) {
          const c = o.parentContainer;
          if (c.enabled === false) continue;
          const m = c.getWorldTransformMatrix();
          found.push([m.tx, m.ty]);
        }
      }
    };
    for (const s of window.__game.scene.getScenes(true)) walk(s.children.list);
    return found.at(-1) ?? null;
  }, label);
  if (!pos) throw new Error(`버튼 없음: ${label} (씬 ${await scene()})`);
  await tap(pos[0], pos[1]);
  await wait(350);
}

async function untilScene(key, tapWhile = false, timeout = 20000) {
  const t0 = Date.now();
  while (!(await scene()).includes(key)) {
    if (Date.now() - t0 > timeout) throw new Error(`${key} 씬이 열리지 않음 (지금 ${await scene()})`);
    if (tapWhile) await tap(225, 300);
    await wait(250);
  }
  await wait(400);
}

const phase = () => page.evaluate(() => window.__game.scene.getScene('Fishing').phase);

/** 낚시 한 번: 던지고, 입질에 챔질하고, 텐션을 보며 감는다 */
async function oneCast(lane = 225) {
  await tap(lane, 400);
  await wait(200);
  // 파워 게이지가 0.75쯤일 때 멈춘다
  for (let i = 0; i < 80; i++) {
    const p = await page.evaluate(() => window.__game.scene.getScene('Fishing').gaugePos);
    if (p > 0.7 && p < 0.85) break;
    await wait(15);
  }
  await tap(225, 400);
  for (let i = 0; i < 200; i++) {
    const ph = await phase();
    if (ph === 'bite') {
      await tap(225, 400);
      break;
    }
    if (ph !== 'wait' && ph !== 'flying') return `대기 중 단계 ${ph}`;
    await wait(40);
  }
  const [x, y] = await toScreen(225, 400);
  const t0 = Date.now();
  let down = false;
  for (let i = 0; i < 3000; i++) {
    const st = await page.evaluate(() => {
      const s = window.__game.scene.getScene('Fishing');
      return { ph: s.phase, t: s.tension };
    });
    if (st.ph !== 'reel') {
      if (down) await page.mouse.up();
      return `${st.ph}(${((Date.now() - t0) / 1000).toFixed(1)}초)`;
    }
    const want = st.t < 72;
    if (want && !down) {
      await page.mouse.move(x, y);
      await page.mouse.down();
      down = true;
    } else if (!want && down) {
      await page.mouse.up();
      down = false;
    }
    await wait(30);
  }
  return 'timeout';
}

try {
  await page.goto(`http://localhost:${PORT}/`);
  await untilScene('Title');
  await press('새로 시작');
  await untilScene('Prologue');
  console.log('프롤로그 시작');
  await untilScene('Fishing', true, 60000);
  console.log('튜토리얼 낚시', await oneCast());
  await wait(500);
  await press('챙기기');
  await untilScene('Prologue');
  // 소금 이벤트까지 넘긴다
  for (let i = 0; i < 20; i++) {
    const hasSalt = await page.evaluate(() => !!window.__game.scene.getScene('Prologue').gauge);
    if (hasSalt) break;
    await tap(225, 300);
    await wait(300);
  }
  await press('소금 치기');
  await wait(1800);
  for (let i = 0; i < 30; i++) {
    const done = await page.evaluate(() => window.__game.scene.getScene('Prologue').blocked && !window.__game.scene.getScene('Prologue').gauge);
    if (done) break;
    await tap(225, 300);
    await wait(300);
  }
  await page.screenshot({ path: 'screenshots/p-taste-unlock.png' });
  await press('물비늘포로');
  await untilScene('Town');
  let s = await save();
  console.log('마을 도착', { day: s.day, slot: s.slot, taste: s.flags.firstTaste, dex: s.dex });

  // 새벽 출조
  await press('방파제');
  await untilScene('Fishing');
  const results = [];
  for (let c = 0; c < 6; c++) {
    const r = await oneCast([120, 225, 330][c % 3]);
    results.push(r);
    await wait(400);
    if (r.startsWith('result')) {
      const btns = await page.evaluate(() => 1);
      void btns;
      try {
        await press('챙기기');
      } catch {
        await press('놓아주기');
      }
      // 미각 해금 모달이 뜨면 닫는다
      await wait(300);
      try {
        await press('좋아');
      } catch {
        /* 없음 */
      }
    }
    await wait(1500);
    if (c === 5) break;
    for (let i = 0; i < 20 && (await phase()) !== 'aim'; i++) await wait(150);
  }
  console.log('출조 결과', results.join(' / '));
  await wait(800);
  await page.screenshot({ path: 'screenshots/p-trip-end.png' });
  await press('마을로');
  await untilScene('Town');
  s = await save();
  console.log('출조 후', { slot: s.slot, bait: s.bait, cooler: s.cooler.map((f) => `${f.species}:${f.size}`), dex: Object.keys(s.dex) });

  // 아침: 위판장에서 한 묶음 팔기
  await press('위판장');
  await untilScene('Market');
  const before = (await save()).money;
  try {
    await press('팔기');
    await wait(2200);
  } catch (e) {
    console.log('팔 물고기 없음', e.message);
  }
  console.log('경매 수익', (await save()).money - before);
  await page.screenshot({ path: 'screenshots/p-market-after.png' });
  await press('마을로');
  await untilScene('Town');

  // 트럭 장사
  if ((await save()).cooler.length > 0) {
    await press('여울호');
    await untilScene('Truck');
    await press('장사 시작');
    for (let n = 0; n < 5; n++) {
      await wait(900);
      for (let k = 0; k < 2; k++) {
        try {
          await press('넣기!');
          await wait(700);
        } catch {
          break;
        }
      }
      await wait(400);
      try {
        await press('다음 손님');
      } catch {
        try {
          await press('장사 마치기');
        } catch {
          break;
        }
      }
      if ((await page.evaluate(() => !!window.__game.scene.getScene('Truck').children.list.find((o) => o.depth === 1000)))) break;
    }
    await wait(800);
    await page.screenshot({ path: 'screenshots/p-truck-end.png' });
    await press('마을로');
    await untilScene('Town');
  }
  s = await save();
  console.log('장사 후', { slot: s.slot, money: s.money, dishes: s.dishesCooked, recipes: s.recipesCooked });

  // 미끼 사고 잠자기
  await press('미끼가게');
  await untilScene('Shop');
  await press('3,000원');
  await press('마을로');
  await untilScene('Town');
  await press('여울식당');
  await press('하루 마치기');
  await press('잠자기');
  await untilScene('Town');
  await wait(600);
  await page.screenshot({ path: 'screenshots/p-next-day.png' });
  s = await save();
  console.log('다음 날', { day: s.day, slot: s.slot, money: s.money, bait: s.bait, fresh: s.cooler.map((f) => f.fresh), requests: s.requests.length });
} catch (e) {
  console.log('실패:', e.message);
  await page.screenshot({ path: 'screenshots/p-fail.png' });
  process.exitCode = 1;
} finally {
  if (errors.length) {
    console.log('페이지 오류:', errors);
    process.exitCode = 1;
  } else console.log('페이지 오류 없음');
  await browser.close();
  await new Promise((r) => server.httpServer.close(r));
}
