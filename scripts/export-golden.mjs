// 웹 버전 코드로 C# 코어 테스트 기준값(core/golden/*.json)을 만든다
// 사용법: node scripts/export-golden.mjs

import { build } from 'esbuild';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const outDir = join(root, 'core', 'golden');
const targets = ['market', 'rules', 'scenario'];

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
  const out = mod.build();
  // 큰 파일(시나리오)은 들여쓰기 없이
  const text = JSON.stringify(out, null, name === 'scenario' ? 0 : 1);
  writeFileSync(file, text + '\n');
  console.log(`기준값 저장: ${file}`);
}
