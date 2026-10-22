# 이어가기 (2026-10-22, 브랜치 `ccr-c846e49a-1tqms8`)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 작업해 줘. (cloud 세션) 모든 답변, 문서, 커밋 메시지, 코드 주석은 한국어로 써줘.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul, 작업 브랜치 ccr-c846e49a-1tqms8 (여기서 이어서 작업하고 푸시해. PR은 만들지 마). main 이 기본 브랜치이고 예전 브랜치(claude/name-change-issue-l462yn, claude/epic-maxwell-25ekj4)까지 모두 합쳐 두었다(작업은 ccr 브랜치에서 하고 끝나면 main 에도 올린다). 마지막 코드 커밋 21696b7, CI 빌드 #24 성공(사진 확인함, APK 안 Hanjeopsi.dll 확인). 그 뒤 문서 커밋만 있다.
- 먼저 읽을 것: CLAUDE.md, docs/HANDOFF.md, docs/GODOT_PLAN.md(단계표 9절, 그림 역할 8절, 폰에서 볼 것 10-2), docs/ART_PROMPTS.md, docs/GAME_DESIGN.md 9절
- 웹 버전(src/, Phaser)은 기준으로 남기고 고닷 4.7.2 C#으로 옮기는 중. U0~U7 완료(U5·U6·U7은 폰 확인 대기). 사용자에게 APK 빌드 #24를 보냈다.
- U7 뒤에 한 일: 사용자가 다시 준 SD 원본으로 낚시 포즈 셋을 더했다. fish-cast(던지는 순간), fish-pull(릴링 중 텐션이 위험 가까이거나 꾹꾹 저항일 때 0.5초 유지), fish-joy(낚아 올리면 톡 뛰고 0.75초 뒤 결과). CI 사진 fishing-pull, fishing-joy.
- 지난 세션에 한 일 (U7):
  - 공통 바탕 godot/scripts/Ui/PlaceScreen: HUD(노치까지 칠함) → 제목 줄(HeaderRight) → 탭(Ui/TabStrip, 금색 알약이 미끄러짐) → 내용(Content, 탭 바꾸면 BuildPage 를 다시 그려 살짝 떠오름) → 아래 "마을로" 막대. 스크롤은 쓰지 않고 쪽 넘김(Pager). FitsToHeight 를 켜면 자리가 정해진 뒤 Content 높이로 한 번 더 그린다. 조각: Paper, Board, FishIcon, Pill, SmallButton, Rule, TopAligned. 고닷 내장 TabBar 와 이름이 겹쳐서 TabStrip.
  - 위판장 Screens/MarketScreen (scenes/market.tscn): 다섯 어종 시세판, 쿨러 묶음(어종·신선도)별 팔기와 전부 팔기, 한 쪽 줄 수는 화면 높이로 계산. 경매 연출: 값이 일곱 번 툭툭 뛰고 "낙찰!" 도장이 쾅. 납품 의뢰 카드, 노씨 의뢰는 납품 뒤 노씨 대화창.
  - 미끼가게 Screens/ShopScreen: 들어오면 노씨 대화창(일러스트) 한마디, 제목 줄 둥근 얼굴(AtlasTexture + ClipChildren)을 누르면 다음 말. 미끼·낚싯대·릴·쿨러·채비·대나무대 줄. 장비 그림이 없어서 Ui/GearIcon 이 코드로 그린다. 장비를 사면 노씨 한마디(마지막 대는 "아끼던 거다").
  - 수첩 Screens/NotebookScreen: 줄 쳐진 종이(NotePaper), 쿨러(신선도 막대, 8개씩 쪽), 도감(모르는 어종은 실루엣, 누르면 큰 그림 모달), 미각 단계 줄기(TasteNode), 조황일지 12장. 제목 옆 SD 고등어(loaf)를 누르면 말풍선.
  - 코어 Game: CoolerGroups, SellableFish, RequestFishHave, RecipeForSpecies (+ CoolerGroupTests, 전체 76개 통과). 글은 godot/data/texts.json 의 auction·shop·notebook, 노씨 대사는 dialogue.json 의 nossi·nossiSays. 데이터 클래스는 MissingMemberHandling.Error 라 JSON 키를 늘리면 Texts.cs/Dialogue.cs 도 같이 고친다.
  - 배경 godot/art/backgrounds/market.png, baitshop.png (웹 webp 를 PNG로). 마을 메뉴·간판의 위판장·미끼가게·수첩 연결(여울호 장사만 NotYet).
  - CI 사진 추가: market, market-auction, market-request, market-tall, shop, shop-talk, shop-tall, notebook, notebook-dex, notebook-detail, notebook-taste, notebook-journal.
- SD 원본은 레포에 넣지 않는다. 15포즈 시트에는 아직 안 쓴 포즈(의자에 앉아 기다리기, 엎드려 졸기, 웅크려 기다리기 등)가 있다. 쓰려면 사용자에게 다시 첨부를 부탁한다.
- 확인 필요: 폰에서 낚시 한 판(손맛, 텐션, 철수 뒤 시간 한 칸), U7 화면(경매 연출, 쪽 넘김 단추가 막대에 안 가리는지, 노씨 대화, 탭 미끄러짐), 노치·긴 화면. 조황일지에서 얻은 장 쪽지를 살짝 기울이려 했는데 GridContainer 안이라 기울지 않는다(사소함).

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility 렌더러, 세로 1080×1920. 유니티는 라이선스 문제로 접음. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts. 수치와 글은 godot/data/*.json. 웹 계산을 바꾸면 npm run golden.
- 그림 역할: 화면 위에서 움직이는 캐릭터(마을·낚시·트럭, 한 줄 말풍선)는 SD 2~2.5등신. 누군가와 이야기하는 장면은 대화창 + 감정 일러스트(할머니·노씨 포함). 감정이 터지는 순간은 9:16 한 장 컷신. 배경은 수채화.
- 낚시 화면의 여울은 뒷모습 SD 포즈를 단계별로 바꿔 끼우고 기울기로 움직임을 준다(대는 그림, 줄은 코드).
- 가게 화면은 PlaceScreen 바탕, 스크롤 대신 화면 높이에 맞춘 쪽 넘김.
- 화면 품질을 중요하게 본다: 웹보다 예쁘고 부드럽게(Tween 연출, 버튼 눌림 튕김, 모달 톡 튀어나옴, 페이드 전환).
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발, 줄표 금지. 아버지 말과 1장 기준(30일 300만 원)은 STORY.md 그대로.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 커밋·푸시.
- 끝나면 APK 파일을 보내 주고 폰에서 무엇을 볼지 알려 준다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED.
- 사용자가 그림을 주면 바로 활용해서 더 자연스럽고 예쁘게 만든다.
- docs/GODOT_PLAN.md 단계표와 docs/ROADMAP.md 체크박스를 갱신한다.

## 환경 제약
- 컨테이너는 매번 새로 만들어진다. 다시 설치: npm ci, apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy (numpy·pillow는 있음). 시작하면 git fetch origin ccr-c846e49a-1tqms8 후 그 브랜치로 체크아웃(처음엔 다른 브랜치에 있을 수 있다).
- 고닷은 여기서 실행 못 한다. C# 컴파일 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI가 찍은 사진으로 본다.
- CI: 푸시하면 .github/workflows/android.yml이 돈다(core/, godot/, 워크플로 변경 때만, 사진 28장이라 6~7분). 결과는 ci-build 브랜치: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치 폴더>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. 실행 상태는 GitHub MCP(mcp__github__actions_list, ToolSearch로 불러옴). 기다릴 때는 ci-build 커밋 제목에 내 커밋 해시가 들어올 때까지 백그라운드로 git fetch 를 반복하고 Monitor 로 결과 파일을 지켜본다(포그라운드 sleep 은 막힘).
- github.com raw 다운로드는 막혀 있다.
- 웹 화면은 소프트웨어 렌더링으로 초당 7프레임 정도. npm run shot, node scripts/playtest.mjs(10분쯤)는 그래도 돈다.
- rm -rf 로 폴더를 지우는 명령은 안전 검사에 막힐 수 있으니 새 폴더를 만들어 쓴다.
- 커밋 안 된 변경이 있으면 멈출 때 훅이 커밋하라고 한다. 사진 확인 전 문서는 git stash 로 잠깐 치워 둔다.

## 이번에 해 줬으면 하는 것
1. 사용자가 폰에서 본 낚시·위판장·미끼가게·수첩 의견이 있으면 먼저 반영하고 CI 사진으로 확인.
2. 다음 단계 U8: 여울호 트럭 장사. 웹 src/scenes/TruckScene.ts 와 코어 Truck·SeasonJudge·ServeDish 를 그대로 쓴다. SD 손님(그림이 없으면 docs/ART_PROMPTS.md 형식으로 손님 SD 시트 프롬프트를 준다), 간 맞추기 게이지, 손님 반응 말풍선, 미각 단계에 따라 ??? 표시. 글은 texts.json 에. 마을 메뉴·트럭의 NotYet 자리를 연결. CI 사진 모드(shots 목록)에 장면 추가. 끝나는 기준: 사진에서 겹침·가림·화면 밖 글자 없음.
마지막: dotnet test core/Hanjeopsi.sln, dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 웹을 고쳤으면 npm run build·npm run shot·node scripts/playtest.mjs. 커밋 후 git push -u origin ccr-c846e49a-1tqms8 (main 도 맞추려면 git push origin ccr-c846e49a-1tqms8:main), CI 사진 확인, APK 보내기, GODOT_PLAN·ROADMAP·HANDOFF 갱신.
````
