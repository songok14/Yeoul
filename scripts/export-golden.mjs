// 웹 버전 코드로 유니티 코어 테스트 기준값(unity/golden/*.json)을 만든다
// 사용법: node scripts/export-golden.mjs

import { build } from 'esbuild';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const outDir = join(root, 'unity', 'golden');
const targets = ['market'];

// localStorage가 없는 node 에서도 save.ts 가 조용히 넘어가도록
globalThis.localStorage = { getItem: () => null, setItem: () => {}, removeItem: () => {} };

mkdirSync(outDir, { recursive: true });
for (const name of targets) {
  const res = await build({
    entryPoints: [join(root, 'scripts', 'golden', `${name}.ts`)],
    bundle: true,
    platform: 'node',
    format: 'esm',
    write: false,
  });
  const code = res.outputFiles[0].text;
  const mod = await import(`data:text/javascript;base64,${Buffer.from(code).toString('base64')}`);
  const file = join(outDir, `${name}.json`);
  writeFileSync(file, JSON.stringify(mod.build(), null, 1) + '\n');
  console.log(`기준값 저장: ${file}`);
}
