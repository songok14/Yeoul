# 이어가기 (2026-10-22, 브랜치 `ccr-c846e49a-1tqms8`)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 작업해 줘. (cloud 세션) 모든 답변, 문서, 커밋 메시지, 코드 주석은 한국어로 써줘.

## 지금까지 상황
- 작업 브랜치: ccr-c846e49a-1tqms8 (여기서 이어서 작업하고 푸시해. PR은 만들지 마). 마지막 푸시 커밋 7bdd81c.
- 먼저 읽을 것: CLAUDE.md, docs/HANDOFF.md, docs/GODOT_PLAN.md(단계표 9절, 그림 역할 8절), docs/ART_PROMPTS.md(맨 위 표와 "대화 일러스트" 절), docs/GAME_DESIGN.md 9절(U4 밸런스 점검)
- 웹 버전(src/, Phaser)은 기준으로 남기고 고닷 4.7.2 C#으로 옮기는 중. U0~U5 완료.
  - U3: 낚시 계산을 src/systems/reel.ts로 빼고 C# core/Hanjeopsi.Core/Systems/ReelFight.cs로. 골든 core/golden/reel.json(릴링 260판 프레임 단위 일치)
  - U4: core/Hanjeopsi.Core.Tests/AutoPlayer.cs, AutoPlayTests.cs. 적당히 잘하면 20/20 클리어(평균 22일), 초보는 20/20 차압. 돈·미끼·쿨러가 다 비면 할 게 없는 막힘이 있음(구제책은 미정)
  - U5: godot/scripts/App(GameState 자동 로드, BootLoader), Ui(Palette, UiKit, GameButton, Screen(모달·토스트·말풍선·대화), Hud, ArtStage, SignButton, DialogueBox), Screens(TitleScreen, TownScreen). 첫 장면 boot.tscn, 점검 화면은 타이틀 아래 '점검'
- 이번 세션 마지막에 한 일: 그림 역할을 "인게임 캐릭터는 SD, 대화와 감정은 일러스트"로 정함. 대화창 DialogueBox(감정 일러스트가 옆에서 들어오고 이름표, 타자기 글, 선택지)를 만들어 여울식당(할머니 한마디 + 갚기·잠자기 선택지), 새 게임 첫마디(welcome), 1장 클리어·차압에 적용. 대화 데이터는 godot/data/dialogue.json(speakers, scenes). 일러스트 14장은 godot/art/portraits/. 머리카락 사이 흰 배경은 scripts/clean_halo.py로 정리(요리복 두 장만 --strong=0.45)
- 확인 필요: 커밋 7bdd81c의 CI 빌드 #17 결과를 아직 못 봤다. 이 커밋에서 대화창을 고쳤다(화면 전체를 살짝 어둡게, 선택지는 말하는 사람 반대쪽, 폭 최대 620). 사진 town-restaurant.png, talk-welcome.png, talk-clear.png를 열어 선택지가 할머니 얼굴을 가리지 않는지, 요리복 여울 머리 둘레 흰 테두리가 사라졌는지 확인한다. 빌드 #16 APK는 사용자에게 보냈지만 #15였고, #16 이후 APK는 아직 안 보냈다.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility 렌더러, 세로 1080×1920. 유니티는 라이선스 문제로 접음. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts. 수치와 글은 godot/data/*.json. 웹 계산을 바꾸면 npm run golden.
- 그림 역할(2026-10-22 사용자 결정): 화면 위에서 움직이는 캐릭터(마을·낚시·트럭, 한 줄 반응 말풍선)는 SD 2~2.5등신. 누군가와 이야기하는 장면은 대화창 + 감정 일러스트. 감정이 터지는 순간은 9:16 한 장 컷신. 배경은 수채화. SD 3(여울 표정 시트)은 뒤로 미루고 대화 일러스트 표정 시트(ART_PROMPTS "대화 1~3")를 먼저 받는다.
- 화면 품질을 중요하게 본다: 웹보다 예쁘고 부드럽게(Tween 연출, 버튼 눌림 튕김, 모달 톡 튀어나옴, 페이드 전환).
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발, 줄표 금지. 아버지 말과 1장 기준(30일 300만 원)은 STORY.md 그대로.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 커밋·푸시.
- 끝나면 APK 파일을 보내 주고 폰에서 무엇을 볼지 알려 준다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED.
- docs/GODOT_PLAN.md 단계표와 docs/ROADMAP.md 체크박스를 갱신한다.

## 환경 제약
- 컨테이너는 매번 새로 만들어진다. 다시 설치: npm ci, apt-get install -y dotnet-sdk-8.0, pip install scipy numpy (clean_halo.py용).
- 고닷은 여기서 실행 못 한다. C# 컴파일 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI가 찍은 사진으로 본다.
- CI: 푸시하면 .github/workflows/android.yml이 돈다(core/, godot/ 변경 때만, 3~4분). 결과는 ci-build 브랜치: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치 폴더>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. 사진 목록은 워크플로 shots=( ... ). 실행 상태는 GitHub MCP(mcp__github__actions_list 등, ToolSearch로 불러옴).
- github.com raw 다운로드는 막혀 있다(글꼴은 fonts.gstatic.com에서 받았다).
- 웹 화면은 소프트웨어 렌더링으로 초당 7프레임 정도. npm run shot, node scripts/playtest.mjs(10분쯤)는 그래도 돈다.
- rm -rf 로 폴더를 지우는 명령은 안전 검사에 막힐 수 있으니 새 폴더를 만들어 쓴다.

## 이번에 해 줬으면 하는 것
1. 빌드 #17(커밋 7bdd81c) 결과와 사진 확인. 대화창 선택지 위치, 일러스트 흰 테두리를 보고 필요하면 고쳐서 다시 푸시. 끝나는 기준: 사진에서 겹침·가림·흰 테두리 없음.
2. APK를 합쳐 사용자에게 보내고 폰에서 볼 것(타이틀, 새로 시작 → 여울 첫마디 대화, 여울식당 할머니 대화와 선택지, 빚 갚기·잠자기, 이어하기, 노치·긴 화면) 안내.
3. 다음 단계 U6: 낚시 화면(godot/scenes/fishing.tscn, Screens/FishingScreen). 코어 ReelRules/BiteWait/ReelFight를 그대로 쓰고 방향 고르기 → 파워 게이지 → 입질 대기(속임 입질) → 챔질 → 릴링(텐션 게이지, 그림자, 물보라) → 결과(물고기 그림, 규정, 쿨러) → 철수. 배경 bg-breakwater를 PNG로 옮기고, 캐릭터는 SD(여울 뒷모습 등), 결과 감정은 일러스트 규칙을 따른다. CI 사진 모드에 fishing 장면 추가.
4. 마을 메뉴에서 방파제를 누르면 U6 낚시 화면으로 가게 연결.
마지막: dotnet test core/Hanjeopsi.sln, dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 웹을 고쳤으면 npm run build·npm run shot·node scripts/playtest.mjs. 커밋 후 git push -u origin ccr-c846e49a-1tqms8, CI 사진 확인.
````
