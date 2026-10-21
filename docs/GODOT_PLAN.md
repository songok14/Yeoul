# 고닷 이전 계획

웹 버전(Phaser 3.90 + TypeScript, `src/`)은 1장 물비늘포 MVP로 재미 확인을 마쳤다. 이제 모바일 게임으로 제대로 만들기 위해 **고닷 4(C#)**로 옮긴다.
웹 버전은 지우지 않고 **기획과 동작의 기준**으로 남긴다. 고닷에서 같은 상황이면 같은 결과가 나와야 한다.

> 처음에는 유니티로 시작했다(2026-10). 유니티는 무료 플랜도 빌드마다 계정 라이선스가 필요해서 Claude가 혼자 APK를 만들 수 없었다.
> 이 게임은 취미 프로젝트라 수익 기능이 필요 없고, "Claude가 빌드하고 사용자는 폰에서 테스트"하는 흐름이 중요해서 라이선스가 없는 고닷으로 바꿨다.
> 코어 규칙 C#과 골든 테스트는 그대로 가져왔다. 유니티 쪽 코드는 git 기록에만 남아 있다.

## 1. 결정 사항

| 항목 | 결정 |
|---|---|
| 엔진 | **Godot 4.7.2 .NET(C#)**. MIT 라이선스, 계정·비용 없음 |
| 렌더러 | Compatibility(OpenGL ES 3). 2D 게임이라 충분하고 오래된 폰까지 돈다 |
| 목표 기기 | 안드로이드 먼저(arm64). 세로 1080×1920 기준, 화면이 길면 세로로 늘어난다(stretch `canvas_items` + `expand`) |
| 언어 | C# (.NET 8). 코어와 고닷 쪽 모두 |
| 규칙 코드 | 고닷을 모르는 순수 C# 코어(`core/`). dotnet으로 빌드하고 xUnit으로 테스트 |
| 데이터 | JSON (`godot/data/*.json`). 파싱은 Newtonsoft Json |
| 웹과 같은지 확인 | 웹 코드로 기준값(골든)을 뽑아 C# 테스트가 비교한다 |
| 빌드 | **GitHub Actions**가 공식 고닷 배포판으로 APK를 만든다. 비밀값이 필요 없다. 비공개 레포는 한 달 2,000분 무료 |
| 화면 확인 | CI가 가상 화면(Xvfb)에서 사진을 찍어 `ci-build` 브랜치에 올린다. Claude가 받아서 눈으로 본다 |
| 그림 | 배경은 지금 수채화 일러스트. 인게임 캐릭터·물체는 SD(2~2.5등신). 일러스트는 컷신 전용 |
| 돈 | 유료 에셋, 유료 서비스, 유료 플러그인 금지 |

질을 높이는 데 쓰는 고닷 기본 기능: `Tween`(움직임), `AnimationPlayer`(연출), `Skeleton2D`(SD 부품 뼈대 애니메이션), `GPUParticles2D`/`CPUParticles2D`(물보라, 김), 셰이더(물결, 빛), `AudioStreamPlayer`(효과음). 모두 엔진에 들어 있다.

## 2. 폴더 구조

```
Yeoul/
├─ src/, public/, docs/ ...           웹 버전 (기준. 지우지 않는다)
├─ scripts/
│  ├─ export-golden.mjs               웹 코드로 기준값을 뽑는다 (npm run golden)
│  ├─ golden/*.ts                     기준값 만드는 코드 (market, rules, scenario)
│  └─ sd_sheet.py                     SD 시트(#EDEDED)를 칸마다 잘라 PNG로
├─ core/                              ① 순수 C# 코어
│  ├─ Hanjeopsi.sln
│  ├─ Hanjeopsi.Core/                 규칙 (net8.0)
│  │  ├─ Util/                        JsMath, Mulberry32, IRandom
│  │  ├─ Data/                        Species, Gear, Balance, Texts, Recipe, RequestTemplate, TasteStage, GameData(로더)
│  │  ├─ State/                       FishItem, SaveData, SaveJson
│  │  ├─ Systems/                     Freshness, Market, Rules, Taste, FishingRoll, SeasonJudge, Truck, (ReelFight)
│  │  └─ Game.cs                      한 판: 세이브 + 규칙 + 행동 (웹 actions.ts, time.ts)
│  ├─ Hanjeopsi.Core.Tests/           xUnit 테스트
│  └─ golden/                         웹에서 뽑은 기준값 JSON
├─ godot/                             ② 고닷 프로젝트
│  ├─ project.godot, Hanjeopsi.csproj, export_presets.cfg, icon.png
│  ├─ data/                           species, gear, balance, texts, recipes, requests, taste .json
│  ├─ art/                            backgrounds/, yeoul/, cat/ (PNG)
│  ├─ fonts/                          고운돋움 (OFL)
│  ├─ scenes/                         .tscn (지금은 core_lab.tscn)
│  ├─ scripts/
│  │  ├─ Boot/                        GameDataLoader, SaveStore, SystemRandom
│  │  └─ Lab/                         CoreLab (점검 화면)
│  └─ keystore/ci-test.keystore       테스트 APK 서명 키 (덮어 설치용, 공개 값)
└─ .github/workflows/android.yml      코어 테스트 → 고닷 받기 → 사진 → APK → 릴리스 test-latest, ci-build
```

- 코어는 고닷을 모른다. 고닷 쪽(`godot/scripts`)은 `ProjectReference`로 코어를 쓴다. 계산식이 화면 코드에 들어가면 테스트할 수 없으니 코어로 뺀다.
- 고닷 쪽 C#은 여기서도 `dotnet build godot/Hanjeopsi.csproj`로 진짜 GodotSharp API에 대고 컴파일된다.
- `.godot/`, `build/`, `bin/`, `obj/`는 커밋하지 않는다. 에디터가 만드는 `*.import`, `*.uid` 파일은 PC에서 에디터를 열었을 때 커밋해도 된다.

## 3. 웹 구조 규칙을 그대로

- 수치와 대사는 JSON(`src/data` 자리), 계산은 코어 `Systems`(`src/systems` 자리), 상태 변경은 코어 `Game`(`state/actions.ts` 자리).
- 화면 코드는 그리기와 입력만 맡는다.
- 상태는 `Game.Save` 하나. 행동이 끝나면 `Game.Saved` 이벤트로 `SaveStore`가 `user://save.json`에 쓴다.

## 4. 데이터 (JSON)

| 파일 | 웹 원본 | 상태 |
|---|---|---|
| `species.json` | `src/data/fish.ts` (도트용 `look`은 뺐다) | ✅ |
| `gear.json` | `src/data/gear.ts` | ✅ |
| `balance.json` | `config.ts` 상수와 시스템 상수 (시세, 신선도, 시간, 의뢰, 고양이, 입질, 판정, 트럭) | ✅ |
| `texts.json` | 신선도 단계, 위판장 소식, 시간대, 날짜, 규정 문구, 손님 리뷰 | ✅ |
| `recipes.json` | `src/data/recipes.ts` (맛 이름, 도구, 리뷰 포함) | ✅ |
| `requests.json` | `src/data/requests.ts` (노씨 의뢰는 `flag: nossiFavor`) | ✅ |
| `taste.json` | `systems/taste.ts` 미각 단계표 | ✅ |
| `story.json` | `src/data/story.ts` (페이지, 배경 키, 컷신 키, 이벤트) | 다음 |
| `dialogue.json` | 할머니 대사, 노씨 대사, 고등어 말풍선, 토스트 문구 등 씬에 흩어진 글 | 다음 |

- 필드 이름은 웹과 같은 camelCase. 모르는 필드가 있으면 바로 오류를 낸다(오타 방지).
- `species.json`의 순서는 시세 시드에 들어간다. 새 어종은 뒤에 붙인다.
- 어종 id는 FishingPlan 도감 그림 이름(`fish_<id>`)과 같다. 금지체장·금어기는 FishingPlan `core/catches/Limits.kt` 기본표를 따른다.

## 5. 웹과 같은 결과 확인 (골든 테스트)

```
웹 코드 (src/systems/*.ts, state/actions.ts)
   │  npm run golden  (esbuild로 묶어서 node에서 실행)
   ▼
core/golden/market.json   ← 시드 4개 × 45일 × 5종 시세, 물고기 값 300건, 낙찰가, endDay 신선도 감소
core/golden/rules.json    ← 달력·금어기 60일, 놓아주기 이유, 어종·크기 뽑기 600번, 판정 200번, 요리값, 손님 한마디
core/golden/scenario.json ← 새 게임부터 행동 2,800여 단계. 매 단계 세이브 전체와 화면 값
   │  dotnet test core/Hanjeopsi.sln
   ▼
C# 코어가 같은 입력으로 계산해서 비교 (테스트 62개)
```

- 난수는 웹과 같은 mulberry32와 hash를 C#으로 옮겼다(uint 연산). 시드가 같으면 시세가 소수점 끝자리까지 같다.
- JS `Math.round`(.5는 위로)와 C# `Math.Round`(은행가 반올림)가 달라서 `JsMath.Round`를 쓴다.
- 웹이 `Math.random()`을 쓰는 곳은 C#에서 `IRandom`을 받는다. 골든 스크립트에서 `Math.random`을 mulberry32로 바꿔 끼워 같은 난수 순서로 비교한다.
- 웹에서 씬 안에 있던 계산은 먼저 웹 쪽을 systems로 빼서(동작은 그대로) 골든을 뽑는다. U2에서 `truck.ts`, `season.ts`, `chapter.ts`, `actions.ts`의 `serveDish`·`completePrologue`·`useBait`를 뺐다. 릴링 물리는 U3에서 `reel.ts`로.
- 시나리오 골든은 웹이 행동을 고르고 인자까지 남긴다. C#은 같은 인자로 같은 행동을 하고 세이브 JSON을 키 하나하나 비교한다.

## 6. 화면 목록

| 고닷 씬 | 웹 씬 | 내용 |
|---|---|---|
| `boot.tscn` | `BootScene` | 데이터·세이브 로드, 첫 화면으로 (autoload `GameState`) |
| `title.tscn` | `TitleScene` | 새로 시작 / 이어하기 |
| `story.tscn` | `PrologueScene` | 컷신 일러스트 + 타자기 글. 프롤로그 1·2부, 1장 클리어, 차압 결말, 소금 간 맞추기 |
| `town.tscn` | `TownScene` | 마을 허브. 시간대별 배경, SD 여울·고등어·트럭, 6개 메뉴, 식당 |
| `fishing.tscn` | `FishingScene` | 방향 → 파워 → 입질 → 챔질 → 릴링, 결과 |
| `market.tscn` | `MarketScene` | 시세판, 경매 연출, 납품 의뢰 |
| `truck.tscn` | `TruckScene` | SD 손님 입장, 주문, 간 맞추기 게이지, 반응 |
| `shop.tscn` | `ShopScene` | 미끼, 장비 |
| `notebook.tscn` | `NotebookScene` | 쿨러, 도감, 미각, 조황일지 |
| `core_lab.tscn` | (없음) | 지금 APK의 점검 화면. 씬을 다 옮기면 메뉴 깊숙이 넣거나 뺀다 |

- 공통 UI(HUD, 버튼, 모달, 토스트, 말풍선, 떠오르는 숫자)는 씬으로 만들어 재사용하고, 색과 글꼴은 `Theme` 하나로 모은다.
- 화면 좌표: 웹 논리 450×800 → 고닷 1080×1920 (2.4배).
- 노치·제스처 바는 `DisplayServer.GetDisplaySafeArea()`로 피한다.

## 7. 웹 기능과 고닷 구현 대응표

| 웹 | 고닷 | 어디에 | 상태 |
|---|---|---|---|
| `utils.ts` mulberry32, hash, clamp, round100, won | `Util/Mulberry32`, `Util/JsMath` | core | ✅ |
| `systems/economy.ts` 신선도, 시세, 물고기 값, 등락, 소식 | `Systems/Freshness`, `Systems/Market` | core | ✅ |
| `time.ts` 시간 칸, 날 넘김, 의뢰 갱신 | `Game.UseSlot`, `EndDay`, `RefreshRequests` | core | ✅ |
| `calendar.ts` 날짜, 금지체장, 금어기 | `Systems/Rules` (해를 넘는 금어기도 FishingPlan과 같게) | core | ✅ |
| `chapter.ts` 1장 클리어·차압 | `Game.CheckChapter` | core | ✅ |
| `taste.ts` 미각 단계, 해금 | `Systems/Taste` | core | ✅ |
| `fishing.ts` 거리 구간, 어종·크기, 고등어 힌트 정확도 | `Systems/FishingRoll` | core | ✅ |
| `systems/truck.ts` 재료 고르기, 요리값, 손님 기분과 한마디 | `Systems/Truck` | core | ✅ |
| `systems/season.ts` 간 맞추기 정확도 | `Systems/SeasonJudge` | core | ✅ |
| `state/save.ts`, `state/actions.ts` | `State/SaveData`, `SaveJson`, `Game` (웹 세이브 JSON을 그대로 읽는다) | core | ✅ |
| `FishingScene` 릴링 텐션·거리·저항·지침, 챔질, 입질 대기 | `Systems/ReelFight` (dt 단위 Step) | core | U3 |
| localStorage | `SaveStore` → `user://save.json` (임시 파일에 쓰고 바꿔 끼움) | godot | ✅ |
| `ui/ui.ts` txt, Button, modal, toast, bubble | `Theme` + 재사용 씬 | godot | U5 |
| 고운돋움·고운바탕 | 프로젝트 기본 글꼴 (TTF 그대로, 동적 글리프) | godot | ✅ (고운돋움) |
| tween, 카메라 흔들기 | `Tween`, `Camera2D` 오프셋 | godot | U5~ |
| `scripts/art.py` | SD 시트는 `scripts/sd_sheet.py`(PNG). 배경은 WebP를 PNG로 | scripts | ✅ |
| `scripts/screenshot.mjs` | CI 사진 모드 (`-- --shot=<경로>`), `ci-build` 브랜치 | godot / CI | ✅ |
| `scripts/playtest.mjs` | 코어 자동 플레이 테스트(dotnet) | core | U4 |

## 8. 그림 적용 방향

- **배경**: `public/assets/bg-*.webp`를 PNG로 바꿔 `godot/art/backgrounds`에 (마을 4장 완료). 900×1600 원본이라 1.2배 확대된다. 원본 고해상도가 있으면 다시 뽑는다.
- **인게임 SD**: 시트(#EDEDED 배경)를 `scripts/sd_sheet.py`로 칸마다 잘라 PNG로. 크기는 바꾸지 않아 같은 시트 프레임은 같은 비율이다. 여울 8장, 고등어 12장 완료.
  ```bash
  python3 scripts/sd_sheet.py <시트.png> 2 4 godot/art/yeoul front,three-quarter,side,back,idle,wave,walk-1,walk-2
  ```
- **애니메이션**: 숨쉬기·통통 튀기는 `Tween`, 걷기·낚시 동작은 `AnimatedSprite2D` 프레임, 부품 분리 그림이 오면 `Skeleton2D` + `Polygon2D` 뼈대. 표정은 눈·입 스프라이트 교체.
- **컷신**: 다듬은 감정 일러스트를 `story.tscn`에서만 쓴다. 세로 9:16, 아래 30%는 글 상자 자리.
- 프롬프트는 `docs/ART_PROMPTS.md`.

## 9. 단계별 일정

한 단계가 끝날 때마다 `dotnet test` 통과, CI APK 빌드와 화면 사진 확인, 폰 테스트, 이 표 체크.

| 단계 | 내용 | 확인 방법 | 상태 |
|---|---|---|---|
| U0 | 계획, 코어 프로젝트, 시세·신선도 이전과 골든 테스트, SD 프롬프트 | `dotnet test` | ✅ |
| U1 | 고닷 프로젝트, CI APK 빌드와 릴리스 `test-latest`, 화면 사진 | APK가 폰에 깔리고 켜진다 | ✅ (폰 확인 대기) |
| U2 | 코어 나머지 규칙, 골든 확장(62개), 점검 화면 `CoreLab` | `dotnet test`, 폰에서 "웹 일치 점검" | ✅ (폰 확인 대기) |
| U3 | 릴링 물리를 웹에서 `reel.ts`로 빼고 C# `ReelFight`로 | `dotnet test` | |
| U4 | 코어 자동 플레이: 새 게임부터 30일을 규칙대로 돌려 예외가 없는지, 빚을 갚을 수 있는지 | `dotnet test` | |
| U5 | 공통 UI(Theme, 버튼, 모달, HUD, 토스트, 화면 전환), Boot·Title·Town | 사진, 폰 | |
| U6 | Fishing (터치, 게이지, 찌, 그림자, 물보라, 결과) | 사진, 폰 | |
| U7 | Market·Shop·Notebook | 사진, 폰 | |
| U8 | Truck (SD 손님, 간 맞추기, 반응) | 사진, 폰 | |
| U9 | Story와 컷신, 프롤로그 튜토리얼, 클리어·차압 결말 | 사진, 폰 | |
| U10 | SD 그림 교체와 애니메이션 다듬기 | 폰 | |
| U11 | 효과음·진동, 설정 화면 | 폰 | |
| U12 | 폰에서 1장 끝까지 플레이, 웹과 수치 비교 | 폰 | |
| U13 | iOS (맥 필요. 나중에 정한다) | 아이폰 | |

## 10. 사용자가 할 일

**없다.** Claude가 `core/`나 `godot/`를 푸시하면 GitHub Actions가 APK를 만든다. 사용자는 폰에서 받아 테스트만 한다.

### 10-1. 폰에 설치하기 (빌드마다)
1. 폰 브라우저나 GitHub 앱에서 github.com/songok14/Yeoul → **Releases** → **테스트 빌드 #번호** (태그 `test-latest`).
2. `hanjeopsi-번호.apk`를 받아 연다. 처음이면 "출처를 알 수 없는 앱 설치"를 허용한다.
3. 같은 테스트 키로 서명해서 다음 빌드는 지우지 않고 덮어 설치된다. 세이브도 남는다.
4. 빌드 하나는 10분 안팎 걸린다.

### 10-2. 지금 APK(점검 화면)에서 볼 것
- 마을 배경 위에 SD 여울과 고등어가 나오는지. 누르면 포즈가 바뀌고 걷기는 두 프레임이 번갈아 나온다.
- 한글이 깨지지 않는지 (고운돋움).
- 버튼 9개로 하루를 돌려 본다: 출조(6번 던지기, 미니게임 없이 다 낚음) → 경매 → 트럭 장사 → 잠자기.
- **웹 일치 점검**을 누르면 11개 항목이 모두 ○ 인지. 하나라도 × 면 화면을 찍어 보내 준다.
- 앱을 껐다 켜면 "이어하기"로 같은 날짜, 같은 돈이 나오는지.
- 노치나 아래 제스처 바에 글자나 버튼이 가리지 않는지.

### 10-3. (선택) PC에서 고닷으로 열어 보기
1. godotengine.org에서 **Godot 4.7.2 .NET** 판을 받는다(설치 없이 압축만 풀면 된다). .NET 8 SDK도 필요하다.
2. 고닷 실행 → Import → `Yeoul/godot/project.godot`.
3. F5로 실행. 화면 크기는 창에서 540×960(절반)으로 보인다.

## 11. 위험과 메모

- **Claude는 여기서 고닷을 실행하지 못한다.** 고닷 배포처가 이 환경에서 막혀 있다. C# 코드는 `dotnet build`로 진짜 GodotSharp에 대고 컴파일하고, 실행과 화면 확인은 CI 사진으로 한다.
- **CI 사진은 PC용 OpenGL(소프트웨어 렌더링)**이라 폰과 아주 조금 다를 수 있다. 최종 확인은 폰에서.
- **Actions 사용량**: 비공개 레포는 한 달 2,000분 무료. 빌드 한 번 10분 안팎. `core/`, `godot/`, 워크플로가 바뀐 푸시에만 돈다.
- **`ci-build` 브랜치**는 매번 강제로 덮어쓰는 결과물 전용 브랜치다. 작업 브랜치로 쓰지 않는다.
- **테스트 서명 키**는 레포에 공개돼 있다. 스토어에 올릴 일이 생기면 새 키를 비밀값으로 둔다.
- **한글 폰트**: TTF(약 7MB)를 그대로 쓴다. 출시 단계에서 쓰는 글자만 남겨 줄일 수 있다.
- **시세 시드**: 세이브의 `seed`와 `species.json` 순서가 같으면 웹과 같은 흐름이다.
- **규정 확인**: 금지체장·금어기는 챕터를 만들 때마다 FishingPlan 기본표와 실제 고시를 다시 확인한다.
