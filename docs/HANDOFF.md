# 이어가기 (2026-10-22, 브랜치 `ccr-c846e49a-1tqms8`)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 작업해 줘. (cloud 세션) 모든 답변, 문서, 커밋 메시지, 코드 주석은 한국어로 써줘.

## 지금까지 상황
- 작업 브랜치: ccr-c846e49a-1tqms8 (여기서 이어서 작업하고 푸시해. PR은 만들지 마). 마지막 코드 커밋 a45fdbe (CI 빌드 #20 성공, 사진 확인함).
- 먼저 읽을 것: CLAUDE.md, docs/HANDOFF.md, docs/GODOT_PLAN.md(단계표 9절, 그림 역할 8절), docs/ART_PROMPTS.md(맨 위 표와 "대화 일러스트" 절), docs/GAME_DESIGN.md 9절(U4 밸런스 점검)
- 웹 버전(src/, Phaser)은 기준으로 남기고 고닷 4.7.2 C#으로 옮기는 중. U0~U6 완료(U5·U6은 폰 확인 대기).
  - U3: 낚시 계산을 src/systems/reel.ts로 빼고 C# core/Hanjeopsi.Core/Systems/ReelFight.cs로. 골든 core/golden/reel.json(릴링 260판 프레임 단위 일치)
  - U4: core/Hanjeopsi.Core.Tests/AutoPlayer.cs, AutoPlayTests.cs. 적당히 잘하면 20/20 클리어(평균 22일), 초보는 20/20 차압. 돈·미끼·쿨러가 다 비면 할 게 없는 막힘이 있음(구제책은 미정)
  - U5: godot/scripts/App(GameState 자동 로드, BootLoader), Ui(Palette, UiKit, GameButton, Screen(모달·토스트·말풍선·대화), Hud, ArtStage, SignButton, DialogueBox), Screens(TitleScreen, TownScreen). 첫 장면 boot.tscn, 점검 화면은 타이틀 아래 '점검'
- 지난 세션에 한 일:
  - 대화창(DialogueBox) 사진 확인. 선택지가 할머니 얼굴을 가리지 않는다.
  - 여울 일러스트 흰 테두리: scripts/clean_halo.py 에 --fringe 추가(머리 둘레 가는 가닥 사이 분홍빛 바탕, 머리 옆 작은 흰 조각). 갈색 머리 여울 그림 10장에 적용. 흰머리(할머니)·회색 머리(노씨)에는 쓰지 않는다.
  - U6 낚시 화면: godot/scenes/fishing.tscn, Screens/FishingScreen, Ui/FishingField(찌·그림자·물보라·줄), Ui/ReelGauge(파워·텐션). 방향 고르기와 고등어 힌트 → 파워 → 속임 입질(톡) → 챔질 → 릴링 → 결과 모달(물고기 그림, 규정, 쿨러) → 철수. 처음 낚은 어종은 여울 peace 일러스트 대화. 마을 방파제(간판·메뉴)에서 들어간다. 문구는 texts.json fishing, 거리 구간 이름 zones.
  - 사용자가 준 SD 낚시 포즈로 여울을 바꿨다: scripts/sd_pose.py 로 godot/art/yeoul/fish-ready(대 세움)·fish-wait(기다림)·fish-reel(감기), 기준점은 godot/data/poses.json, 읽기는 Ui/SdPoses. 낚싯대는 그림에 있고 줄만 코드로 그린다. 파워만큼 젖히기, 던지면 휘두르기, 입질에 쿡, 챔질에 들기, 텐션·저항에 끌려가기, 위험하면 떨림.
  - CI 사진: fishing, fishing-power, fishing-reel, fishing-result, fishing-new, fishing-tall 추가(이름이 -tall 로 끝나면 1080×2400).
  - 사용자에게 APK 빌드 #20 을 보냈다.
- 사용자가 준 SD 원본(채팅 첨부): 뒷모습 3장은 이미 넣었다. 옆모습(당기기·서 있기), 15포즈 시트(체크무늬 배경이 그려져 있음), 6포즈 시트(#EEE, 배경을 지우면 가는 낚싯대가 끊긴다)는 아직 안 썼다. 결과 기쁨 SD, 앉아 기다리기 등에 쓸 수 있다.
- 확인 필요: 폰에서 낚시 한 판(손맛, 텐션 감, 결과·철수 뒤 마을 시간이 한 칸 지나는지).

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
1. 사용자가 폰에서 본 낚시 화면 의견이 있으면 먼저 반영.
2. 다음 단계 U7: 위판장(경매), 미끼가게(장비·미끼), 수첩(도감·미각). 웹 MarketScene·ShopScene·NotebookScene 과 코어 Market·Game 행동을 그대로 쓴다. 마을 메뉴 NotYet 자리를 연결. CI 사진 모드에 장면 추가.
3. 남은 SD 원본(옆모습 당기기 등)을 쓸 곳이 있으면 sd_pose.py 로 넣는다(체크무늬 시트는 배경 지우기를 따로 해야 한다).
마지막: dotnet test core/Hanjeopsi.sln, dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 웹을 고쳤으면 npm run build·npm run shot·node scripts/playtest.mjs. 커밋 후 git push -u origin ccr-c846e49a-1tqms8, CI 사진 확인.
````
