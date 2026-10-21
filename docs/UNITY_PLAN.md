# 유니티 이전 계획

웹 버전(Phaser 3.90 + TypeScript, `src/`)은 1장 물비늘포 MVP로 재미 확인을 마쳤다. 이제 모바일 게임으로 제대로 만들기 위해 유니티로 옮긴다.
웹 버전은 지우지 않고 **기획과 동작의 기준**으로 남긴다. 유니티에서 같은 상황이면 같은 결과가 나와야 한다.

## 1. 결정 사항

| 항목 | 결정 |
|---|---|
| 엔진 | Unity 6 LTS, Universal 2D(URP) 템플릿, Personal(무료) |
| 목표 기기 | 안드로이드 먼저, 이후 iOS. 세로 1080×1920 기준 |
| 언어 | C# 9 (Unity 6 지원 범위). `record`, 파일 범위 namespace, `global using`은 쓰지 않는다 |
| 규칙 코드 | UnityEngine을 모르는 순수 C# 코어. 여기(클라우드)서 dotnet으로 빌드하고 xUnit으로 테스트 |
| 데이터 | JSON (`Resources/HanjeopsiData/*.json`). 파싱은 Newtonsoft Json(유니티 공식 무료 패키지) |
| 웹과 같은지 확인 | 웹 코드로 기준값(골든)을 뽑아 C# 테스트가 비교한다 |
| 그림 | 배경은 지금 수채화 일러스트. 인게임 캐릭터·물체는 SD(2~2.5등신). 일러스트는 컷신 전용 |
| 돈 | 유료 에셋, 유료 서비스, 유료 플러그인 금지. 유니티 공식 무료 패키지만 쓴다 |

## 2. 폴더 구조

```
Yeoul/
├─ src/, public/, docs/ ...           웹 버전 (기준. 지우지 않는다)
├─ scripts/
│  ├─ export-golden.mjs               웹 코드로 기준값을 뽑는다 (npm run golden)
│  └─ golden/market.ts                시세·신선도 기준값 만드는 코드
└─ unity/
   ├─ README.md                       빌드와 테스트 방법
   ├─ golden/                         웹에서 뽑은 기준값 JSON (커밋한다)
   ├─ com.songok.hanjeopsi.core/      ① 순수 C# 코어 (유니티 로컬 패키지)
   │  ├─ package.json
   │  ├─ Runtime/                     Hanjeopsi.Core.asmdef (noEngineReferences)
   │  │  ├─ Util/                     JsMath, Mulberry32, IRandom
   │  │  ├─ Data/                     Species, Gear, Balance, Texts, GameData(로더)
   │  │  ├─ State/                    FishItem, (SaveData)
   │  │  └─ Systems/                  Freshness, Market, (Time, Taste, Rules, Fishing, Reel, Truck, Requests)
   │  └─ Resources/HanjeopsiData/     species, gear, balance, texts .json
   ├─ com.songok.hanjeopsi.game/      ② 유니티 쪽 코드 (로컬 패키지)
   │  └─ Runtime/                     Hanjeopsi.Game.asmdef → Hanjeopsi.Core 참조
   │     ├─ Boot/                     GameDataLoader, (GameManager, SaveStore)
   │     ├─ Scenes/                   (씬별 컨트롤러)
   │     ├─ UI/                       (버튼, 모달, 게이지, HUD)
   │     └─ Debug/                    CoreCheck (코어 연결 확인)
   ├─ dotnet/                         ③ 코어 빌드·테스트 (유니티는 이 폴더를 모른다)
   │  ├─ Hanjeopsi.sln
   │  ├─ Hanjeopsi.Core/              패키지 Runtime 소스를 그대로 컴파일 (netstandard2.1, C# 9)
   │  └─ Hanjeopsi.Core.Tests/        xUnit 테스트 (net8.0)
   └─ HanjeopsiBada/                  ④ 유니티 프로젝트 (PC에서 Unity Hub로 만든다)
      ├─ Assets/
      │  ├─ Art/Backgrounds, Art/Characters, Art/Fish, Art/Dishes, Art/Cutscenes, Art/UI
      │  ├─ Fonts/                    고운돋움, 고운바탕 (OFL) + TMP 폰트 에셋
      │  ├─ Prefabs/, Scenes/, Settings/
      │  └─ link.xml                  IL2CPP 코드 제거 방지
      └─ Packages/manifest.json       ①②를 file: 로 연결
```

왜 패키지로 나누나:
- 코어 소스가 **한 벌**만 있다. 유니티는 패키지로 읽고 dotnet은 같은 파일을 컴파일한다.
- 유니티 프로젝트(④)를 Unity Hub에서 빈 폴더에 새로 만들 수 있다. Hub는 비어 있지 않은 폴더에 프로젝트를 만들지 못한다.
- 씬, 프리팹, 그림처럼 에디터가 만드는 것만 ④에 둔다.

## 3. 어셈블리

| 어셈블리 | 위치 | 참조 | 하는 일 |
|---|---|---|---|
| `Hanjeopsi.Core` | core 패키지 Runtime | Newtonsoft.Json만 | 규칙 계산, 데이터 모델, 세이브 구조. `noEngineReferences: true`라 UnityEngine을 쓰면 컴파일이 안 된다 |
| `Hanjeopsi.Game` | game 패키지 Runtime | Core, Unity | 씬 컨트롤러, UI, 연출, 입력, 사운드, 세이브 파일 입출력 |
| `Hanjeopsi.Core.Tests` | unity/dotnet | Core, xUnit | 웹과 같은 결과인지, 설계표 수치가 맞는지 |
| (나중) `Hanjeopsi.Game.Tests` | game 패키지 Tests | Game, Unity Test Framework | 씬이 열리는지 같은 플레이 모드 확인 |

웹의 구조 규칙을 그대로 가져간다.
- 수치와 대사는 JSON(`src/data` 자리), 계산은 Core `Systems`(`src/systems` 자리), 상태 변경은 Core `GameActions`(`state/actions.ts` 자리).
- 씬 컨트롤러는 그리기와 입력만 맡는다. 계산식이 씬 코드에 들어가면 테스트할 수 없으니 Core로 뺀다.

## 4. 데이터 (JSON)

| 파일 | 웹 원본 | 상태 |
|---|---|---|
| `species.json` | `src/data/fish.ts` (도트용 `look`은 뺐다) | ✅ |
| `gear.json` | `src/data/gear.ts` | ✅ |
| `balance.json` | `config.ts` 상수, 시세·신선도 상수 | ✅ (시세·신선도·1장 목표) |
| `texts.json` | 신선도 단계 이름, 위판장 소식 | ✅ |
| `recipes.json` | `src/data/recipes.ts` (맛 이름, 도구, 리뷰 포함) | 다음 |
| `requests.json` | `src/data/requests.ts` | 다음 |
| `story.json` | `src/data/story.ts` (페이지, 배경 키, 컷신 키, 이벤트) | 다음 |
| `dialogue.json` | 할머니 대사, 고등어 말풍선, 토스트 문구 등 씬에 흩어진 글 | 다음 |
| `taste.json` | `systems/taste.ts` 미각 단계표 | 다음 |

- 필드 이름은 웹과 같은 camelCase. C# 쪽은 PascalCase 필드에 Newtonsoft camelCase 규칙으로 읽는다.
- 모르는 필드가 있으면 바로 오류를 낸다(`MissingMemberHandling.Error`). 오타를 조용히 넘기지 않기 위해서다.
- `species.json`의 순서는 시세 시드에 들어간다. 순서를 바꾸면 시세 흐름이 바뀌니 새 어종은 뒤에 붙인다.
- 어종 id는 FishingPlan 도감 그림 이름(`fish_<id>`)과 같다. 금지체장·금어기는 FishingPlan `core/catches/Limits.kt` 기본표를 따른다(테스트 `규정이_FishingPlan_기본표와_같다`).

## 5. 웹과 같은 결과 확인 (골든 테스트)

```
웹 코드 (src/systems/*.ts)
   │  npm run golden  (esbuild로 묶어서 node에서 실행)
   ▼
unity/golden/market.json   ← 시드 4개 × 45일 × 5종 시세, 물고기 값 300건, 낙찰가, endDay 신선도 감소 ...
   │  dotnet test
   ▼
C# Core 가 같은 입력으로 계산해서 비트 단위로 비교
```

- 난수는 웹과 같은 mulberry32와 hash를 C#으로 옮겼다(uint 연산). 시드가 같으면 시세가 소수점 끝자리까지 같다.
- JS `Math.round`(.5는 위로)와 C# `Math.Round`(은행가 반올림)가 달라서 `JsMath.Round`를 쓴다.
- 웹이 `Math.random()`을 쓰는 곳(입질 어종, 크기, 경매 낙찰 배율, 손님 메뉴)은 C#에서 `IRandom`을 받는다. 골든 스크립트에서 `Math.random`을 mulberry32로 바꿔 끼우면 같은 난수 순서로 비교할 수 있다.
- 릴링 물리처럼 웹에서 씬 안에 들어 있는 계산은 먼저 웹 쪽을 `src/systems/reel.ts`로 빼서(동작은 그대로) 골든을 뽑고, 같은 식을 C#으로 옮긴다.
- 웹 수치를 바꾸면 `npm run golden` 후 `dotnet test`. 유니티 쪽 수치만 바꾸면 테스트가 깨지니 일부러 갈라지는 경우에만 골든 대신 C# 테스트 기대값을 고치고 이 문서에 적는다.

## 6. 씬 목록

| 유니티 씬 | 웹 씬 | 내용 | 비고 |
|---|---|---|---|
| `Boot` | `BootScene` | 데이터 로드, 세이브 로드, 폰트·아틀라스 준비 | `GameManager`(DontDestroyOnLoad) 생성 |
| `Title` | `TitleScene` | 새로 시작 / 이어하기 | |
| `Story` | `PrologueScene` | 컷신 일러스트 + 타자기 글. 프롤로그 1·2부, 1장 클리어, 차압 결말 | 미니 간 맞추기(소금) 연출 포함 |
| `Town` | `TownScene` | 마을 허브. 시간대별 배경, SD 여울·고등어·트럭, 6개 메뉴, 식당 모달 | |
| `Fishing` | `FishingScene` | 방향 → 파워 → 입질 → 챔질 → 릴링, 결과 모달 | 가장 손이 많이 가는 씬 |
| `Market` | `MarketScene` | 시세판, 경매 연출, 납품 의뢰 탭 | |
| `Truck` | `TruckScene` | 손님 SD 입장, 주문, 간 맞추기 게이지, 반응 | |
| `Shop` | `ShopScene` | 미끼, 장비 업그레이드 | |
| `Notebook` | `NotebookScene` | 쿨러, 도감, 미각 회복, 조황일지 | |

- 공통 UI(HUD, 버튼, 모달, 토스트, 말풍선, 떠오르는 숫자)는 프리팹으로 만들어 모든 씬에서 쓴다.
- 씬 이동은 `SceneRouter.Go("Town", 인자)` 하나로. 웹의 `goTo`처럼 인자는 매번 새로 넘긴다.
- 화면 좌표: 웹 논리 450×800 → 유니티 1080×1920 (2.4배). 카메라 직교 크기 9.6(PPU 100). UI는 Canvas Scaler 기준 1080×1920, Match 0(가로 기준), 노치는 Safe Area로 피한다.

## 7. 웹 기능과 유니티 구현 대응표

| 웹 | 유니티 | 어디에 | 상태 |
|---|---|---|---|
| `utils.ts` mulberry32, hash, clamp, round100, won | `Util/Mulberry32`, `Util/JsMath` | Core | ✅ |
| `systems/economy.ts` 신선도 단계·배율 | `Systems/Freshness` | Core | ✅ |
| `systems/economy.ts` 시세, 물고기 값, 등락, 소식 | `Systems/Market` | Core | ✅ |
| `time.ts` endDay 신선도 감소 | `Freshness.ApplyDailyDecay` | Core | ✅ |
| `actions.ts` sellFish 낙찰가 | `Market.AuctionTotal` | Core | ✅ |
| `time.ts` 시간 칸, 날 넘김, 의뢰 갱신 | `Systems/GameClock`, `Systems/Requests` | Core | 다음 |
| `calendar.ts` 날짜, 금지체장, 금어기 | `Systems/Rules` (해를 넘는 금어기도 처리. FishingPlan과 같게) | Core | 다음 |
| `taste.ts` 미각 단계, 해금 | `Systems/Taste` | Core | 다음 |
| `fishing.ts` 거리 구간, 어종·크기 뽑기 | `Systems/FishingRoll` | Core | 다음 |
| `FishingScene` 릴링 텐션·거리·저항·지침, 챔질 판정, 고등어 힌트 정확도 | `Systems/ReelFight` (dt 단위 Step) | Core | 다음 |
| `TruckScene` dishFresh, 요리값, 손님 반응 고르기 | `Systems/Truck` | Core | 다음 |
| `gauge.ts` 간 맞추기 정확도 | `Systems/SeasonJudge` | Core | 다음 |
| `state/save.ts` SaveData, createSave | `State/SaveData` (필드 이름 웹과 같게. 웹 세이브 JSON을 그대로 읽을 수 있다) | Core | 다음 |
| `state/actions.ts` 상태 변경 | `GameActions` | Core | 다음 |
| localStorage 저장 | `SaveStore` → `Application.persistentDataPath/save.json` | Game | 다음 |
| `ui/ui.ts` txt, Button, modal, toast, bubble | TextMeshPro + uGUI 프리팹 | Game | 다음 |
| 고운돋움·고운바탕 | TMP 폰트 에셋 (Dynamic, 한글 2350자 + 자주 쓰는 기호) | Game | 다음 |
| `ui/hud.ts` | `Hud` 프리팹 | Game | 다음 |
| `ui/art.ts` 임시 도트 손님·얼굴 | SD 손님 스프라이트 | Game | 그림 대기 |
| tween, 카메라 흔들기 | 코루틴 + 간단한 Tween 헬퍼 (무료. DOTween 무료판도 가능) | Game | 다음 |
| `scripts/art.py` (WebP) | PNG 출력 옵션 추가. 유니티는 WebP를 못 읽는다 | 웹 scripts | 다음 |
| `scripts/screenshot.mjs`, `playtest.mjs` | 에디터 메뉴 "한접시/전체 씬 열어보기", 코어 자동 플레이 테스트(dotnet) | Game / Core | 나중 |
| `?scene=Town&demo=1` | Boot 씬 인스펙터의 "시작 씬"과 "체험 세이브" 옵션 | Game | 다음 |

## 8. 그림 적용 방향

- **배경**: `public/assets/bg-*.webp`를 PNG로 바꿔 `Assets/Art/Backgrounds`에. 1080×1920 화면에 900×1600 원본이라 1.2배 확대된다. 원본 고해상도가 있으면 다시 뽑는다.
- **인게임 SD**: 여울, 고등어, 막순 할머니, 노씨, 차세라, 손님, 트럭 여울호, 물고기, 요리. 시트(#EDEDED 배경)를 `art.py`로 자르고 PNG로.
- **애니메이션**:
  - 간단한 것(숨쉬기, 통통 튀기, 꼬리 흔들기)은 한 장 그림 + 코드 트윈.
  - 걷기·낚시 동작은 포즈 그림을 프레임으로 넘기는 스프라이트 애니메이션.
  - 부품을 나눈 그림이 오면 유니티 **2D Animation** 패키지(공식 무료)로 뼈대를 넣는다. 표정은 눈·입 스프라이트 교체(Sprite Resolver).
- **컷신**: 다듬은 감정 일러스트를 `Story` 씬에서만 쓴다. 세로 9:16 한 장 그림, 아래 30%는 글 상자 자리.
- 그림 프롬프트는 `docs/ART_PROMPTS.md`.

## 9. 단계별 일정

한 단계가 끝날 때마다 `dotnet test` 통과, 유니티에서 Play 확인, 이 표 체크.

| 단계 | 내용 | 확인 방법 | 상태 |
|---|---|---|---|
| U0 | 계획, 코어 프로젝트, 시세·신선도 이전과 골든 테스트, SD 프롬프트(여울·고등어) | `dotnet test` 31개 통과 | ✅ |
| U1 | 유니티 프로젝트 만들기, 두 패키지 연결, `CoreCheck`로 시세표 확인 | PC에서 Play | 사용자 확인 대기 |
| U2 | 코어 나머지: 날짜·규정, 시간 칸·날 넘김, 의뢰, 미각, 어종 뽑기, 요리값, 간 맞추기 판정, SaveData, GameActions. 골든 확장 | `dotnet test` | |
| U3 | 릴링 물리를 웹에서 `reel.ts`로 빼고 C# `ReelFight`로. 같은 입력이면 같은 결과 | `dotnet test` | |
| U4 | 코어 자동 플레이: 새 게임부터 30일을 규칙대로 돌려 예외가 없는지, 빚을 갚을 수 있는지 (웹 `playtest.mjs` 자리) | `dotnet test` | |
| U5 | 유니티 공통 UI(폰트, 버튼, 모달, HUD, 토스트), Boot·Title·Town | PC에서 Play | |
| U6 | Fishing 씬 (터치 입력, 게이지, 찌, 그림자, 결과) | PC에서 Play, 폰 | |
| U7 | Market·Shop·Notebook | PC에서 Play | |
| U8 | Truck (손님, 간 맞추기, 반응) | PC에서 Play | |
| U9 | Story 씬과 컷신, 프롤로그 튜토리얼, 클리어·차압 결말 | PC에서 Play | |
| U10 | SD 그림 교체와 애니메이션 | PC에서 Play | |
| U11 | 효과음·진동, 설정 화면 | 폰 | |
| U12 | 안드로이드 빌드, 폰에서 1장 끝까지 플레이, 웹과 수치 비교 | 폰 | |
| U13 | iOS 빌드 (Mac 필요) | 아이폰 | |

## 10. 에디터에서 할 일 (사용자 PC)

### 10-1. 설치 (처음 한 번)
1. Unity Hub를 설치하고 Personal 라이선스로 로그인한다.
2. Hub → Installs → Install Editor → **Unity 6 LTS** 최신 버전(6000.x LTS)을 고른다.
3. 모듈에서 **Android Build Support** 와 그 아래 **OpenJDK**, **Android SDK & NDK Tools** 를 모두 체크한다. (iOS는 Mac에서만)
4. 코드 편집기는 Visual Studio Community(무료) 또는 VS Code + C# Dev Kit.

### 10-2. 레포 받기
```bash
git clone https://github.com/songok14/Yeoul.git
cd Yeoul
git checkout ccr-c846e49a-1tqms8
```

### 10-3. 유니티 프로젝트 만들기
1. Hub → Projects → **New project** → Unity 6 LTS 선택.
2. 템플릿 **Universal 2D** 를 고른다.
3. Project name `HanjeopsiBada`, Location은 `Yeoul/unity` 폴더. 결과 경로가 `Yeoul/unity/HanjeopsiBada`인지 확인하고 Create.
4. 에디터가 열리면 일단 닫는다.

### 10-4. 패키지 두 개 연결
`unity/HanjeopsiBada/Packages/manifest.json`을 메모장으로 열어 `"dependencies": {` 바로 아래에 두 줄을 넣는다.
```json
    "com.songok.hanjeopsi.core": "file:../../com.songok.hanjeopsi.core",
    "com.songok.hanjeopsi.game": "file:../../com.songok.hanjeopsi.game",
```
다시 열면 Newtonsoft Json 패키지도 자동으로 들어온다. Window → Package Manager → In Project 에 "한 접시의 바다 코어"와 "한 접시의 바다 게임"이 보이면 성공.
(메모장 대신 Package Manager의 `+` → Add package from disk 로 각 폴더의 `package.json`을 골라도 된다. 이때는 절대 경로가 들어가니 manifest를 열어 위처럼 상대 경로로 고친다.)

### 10-5. 코어 연결 확인 (U1)
1. Project 창에서 `Assets/Scenes`에 새 씬 `CoreCheck`를 만든다(File → New Scene → Basic 2D).
2. Hierarchy 빈 곳 우클릭 → Create Empty, 이름 `CoreCheck`.
3. Inspector → Add Component → `CoreCheck` 검색해서 붙인다.
4. Play. Console에 아래가 찍히면 성공이다. (시드 12345, 4일차)
```
시드 12345 · 4일차 · 볼락 값이 38% 뛰었다
```
   다섯 어종 시세와 노래미 값도 같이 나온다. 콘솔 화면을 캡처해서 알려 주면 골든 값과 맞춰 본다.
5. 오류가 나면 Console의 빨간 줄 전체를 복사해서 보내 준다.

### 10-6. 프로젝트 설정 (U1에서 같이)
1. Edit → Project Settings → Player
   - Company Name `songok`, Product Name `한 접시의 바다`
   - Android 탭 → Resolution and Presentation → Default Orientation **Portrait**
   - Other Settings → Package Name `com.songok.hanjeopsi`, Minimum API Level **Android 7.0 (API 24)** 이상, Scripting Backend **IL2CPP**, Target Architectures **ARM64**
2. Game 창 위 해상도 목록 → `+` → 1080×1920 (Portrait) 추가해서 고른다.
3. `Assets/link.xml`을 만들고 아래를 넣는다. (IL2CPP가 JSON으로만 쓰는 클래스를 지우지 않게)
```xml
<linker>
  <assembly fullname="Hanjeopsi.Core" preserve="all" />
</linker>
```
4. File → Build Profiles(또는 Build Settings) → Android → Switch Platform.

### 10-7. 커밋할 것과 하지 않을 것
- 커밋: `Assets/`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/`, 패키지 폴더에 새로 생긴 `.meta` 파일.
- 커밋하지 않음: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, 빌드 결과 (`unity/.gitignore`에 있다).

## 11. 위험과 메모

- **유니티 코드는 여기서 컴파일하지 못한다.** Game 패키지 코드는 PC에서 처음 열 때 오류가 날 수 있다. Console 내용을 받아 고친다. 그래서 계산은 최대한 Core로 보낸다.
- **IL2CPP 코드 제거**: Newtonsoft는 리플렉션으로 필드를 채운다. `link.xml`을 빼먹으면 폰 빌드에서만 데이터가 비는 문제가 생긴다.
- **한글 폰트**: TMP 폰트 에셋은 Dynamic으로 만들고, 출시 전에 쓰는 글자만 모아 Static으로 굽는다(용량).
- **WebP**: 유니티는 WebP를 읽지 못한다. 배경과 물고기 그림은 PNG로 바꿔 넣는다.
- **시세 시드**: 세이브의 `seed`와 `species.json` 순서가 같으면 웹과 같은 흐름. 웹 세이브를 옮겨 와도 같은 시세가 나온다.
- **규정 확인**: 금지체장·금어기는 챕터를 만들 때마다 FishingPlan 기본표와 실제 고시를 다시 확인한다.
