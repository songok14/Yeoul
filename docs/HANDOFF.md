# 이어가기 (2026-10-22, 브랜치 `main` = `ccr-c846e49a-1tqms8`)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어.
- 설명은 간략하고 쉽게 한다.
- 애매하거나 모호한 부분이 있다면 질문을 통해 해결하고 넘어간다.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul (2026-10-22부터 공개 레포). 기본 브랜치 main. 작업 브랜치 ccr-c846e49a-1tqms8 에서 이어서 하고, 끝나면 main 에도 같은 커밋을 올린다(git push origin ccr-c846e49a-1tqms8:main). 둘 다 a03e6da. PR은 만들지 않는다. 시작하면 git fetch origin ccr-c846e49a-1tqms8 후 그 브랜치로 체크아웃(처음엔 다른 브랜치에 있을 수 있다).
- 마지막 코드 커밋 590ba67, CI 빌드 #29 성공(사진 확인, APK 안 Hanjeopsi.dll 확인). 사용자에게 APK #29를 보냈다.
- 먼저 읽을 것: CLAUDE.md, docs/GODOT_PLAN.md(그림 역할 8절, 단계표 9절, 폰에서 볼 것 10-2), docs/ART_PROMPTS.md, docs/STORY.md, docs/GAME_DESIGN.md 9절
- 웹 버전(src/, Phaser)은 기준으로 남기고 고닷 4.7.2 C#으로 옮기는 중. U0~U8 완료(U5~U8은 폰 확인 대기).
- 이번 세션에 한 일:
  - U7: 공통 바탕 godot/scripts/Ui/PlaceScreen(HUD → 제목 줄 → 탭 Ui/TabStrip → 내용 → "마을로" 막대, 스크롤 대신 쪽 넘김). 위판장 Screens/MarketScreen(시세판, 쿨러 묶음 팔기, 값이 뛰다 "낙찰!" 도장, 납품 의뢰), 미끼가게 Screens/ShopScreen(노씨 대화창 일러스트, 둥근 얼굴 단추, 장비 아이콘은 Ui/GearIcon 이 코드로 그림), 수첩 Screens/NotebookScreen(쿨러, 도감, 미각, 일지). 코어 Game.CoolerGroups 등. 글은 texts.json auction·shop·notebook, 노씨 대사 dialogue.json nossi·nossiSays.
  - 낚시 SD 포즈 셋 추가: fish-cast(던지는 순간), fish-pull(릴링 중 텐션이 위험 가까이거나 꾹꾹 저항이면 0.5초 유지), fish-joy(낚으면 톡 뛰고 0.75초 뒤 결과). poses.json body 는 머리 크기를 맞추려고 손으로 정했다(pull 740, joy 360, cast 520).
  - U8: Screens/TruckScreen, 게이지 Ui/SeasonGauge. 메뉴판 → 손님이 왼쪽에서 걸어와 주문(손님마다 말투, dialogue.json customers) → 간 맞추기(아는 맛만 초록 구간) → 기쁨·보통·실망 포즈, 리뷰 말풍선, 받은 돈, 별 → 걸어 나감. 다섯 명(balance truck.customersPerShift)이나 재료가 떨어지면 끝, 시간 한 칸. 장사 중 판 높이 ShiftPanelHeight 로 고정(손님 몸이 판에 가렸던 것 고침). 간판 "여울호"와 칠판 메뉴는 그림 위에 글자로.
  - SD 손님 24장: godot/art/customers/<fisher|mom|student|mail|grandpa|tourist>-<stand|walk|happy|sad>.png. 새 스크립트 scripts/sd_cells.py 로 잘랐다(단색 바탕과 그림에 박힌 체크무늬를 같은 방법으로 지움, 떨어진 덩어리도 남김, 흰 머리·수염은 --hair=0). 그림은 왼쪽을 봐서 걸어올 때 FlipH.
  - 여울 일러스트 일곱 장을 사용자가 다듬은 고해상도 판으로 교체: 웹 public/assets 높이 900 WebP, 고닷 godot/art/portraits 높이 1400 PNG.
  - 예전 브랜치(claude/name-change-issue-l462yn)를 합쳤다(.claude/skills/session-handoff, CLAUDE.md "세션 마무리").
- 아직 안 쓴 그림(레포에 없음, 쓰려면 다시 첨부 부탁): 여울 SD 17포즈 시트의 의자에 앉아 기다리기, 놀람(!), 엎드려 졸기, 웅크려 기다리기.
- 확인 필요(폰): 낚시 한 판(새 포즈 셋, 손맛, 철수 뒤 시간 한 칸), 위판장·미끼가게·수첩, 트럭 장사(손님 걸음, 간 맞추기, 반응), 대화 일러스트 선명도, 노치·긴 화면.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility 렌더러, 세로 1080×1920. 유니티는 라이선스 문제로 접음. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts. 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다). 웹 계산을 바꾸면 npm run golden.
- 그림 역할: 화면 위에서 움직이는 캐릭터(마을·낚시·트럭, 한 줄 말풍선)는 SD 2~2.5등신. 누군가와 이야기하는 장면은 대화창 + 감정 일러스트(할머니·노씨 포함). 감정이 터지는 순간은 9:16 한 장 컷신(아래 30% 글 상자). 배경은 수채화.
- 가게 화면은 PlaceScreen 바탕, 스크롤 대신 화면 높이에 맞춘 쪽 넘김.
- 화면 품질을 중요하게 본다: Tween 연출, 버튼 눌림 튕김, 모달 톡 튀어나옴, 페이드 전환.
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발, 줄표 금지. 아버지 말과 1장 기준(30일 300만 원)은 STORY.md 그대로.
- 바뀐 점: 레포를 공개로 돌렸다(Actions 무료 사용량이 바닥나 빌드가 1~3초 만에 실패했다). 스토어 출시 때는 새 서명 키를 GitHub Actions Secrets 에 넣는다(테스트 키 godot/keystore/ci-test.keystore 는 공개용).

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 커밋·푸시. 끝나면 APK 파일을 보내 주고 폰에서 무엇을 볼지 알려 준다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED(docs/ART_PROMPTS.md 형식).
- 사용자가 그림을 주면 바로 활용해서 더 자연스럽고 예쁘게 만든다.
- docs/GODOT_PLAN.md 단계표, docs/ROADMAP.md 체크박스, docs/HANDOFF.md 를 갱신한다.

## 환경 제약
- 컨테이너는 매번 새로 만들어진다. 다시 설치: npm ci, apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy (numpy·pillow는 있음).
- 고닷은 여기서 실행 못 한다. C# 컴파일 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI가 찍은 사진으로 본다.
- CI: 푸시하면 .github/workflows/android.yml 이 돈다(core/, godot/, 워크플로 변경 때만, 사진 34장이라 6~7분). 문서만 바꾼 푸시는 안 돈다. 다시 돌릴 때는 GitHub MCP actions_run_trigger(run_workflow, android.yml, ref 브랜치). 결과는 ci-build 브랜치: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치 폴더>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. 실행 상태는 mcp__github__actions_list(ToolSearch로 불러옴).
- 기다릴 때는 백그라운드 Bash 로 ci-build 커밋 제목에 내 커밋 해시가 들어올 때까지 git fetch 를 반복하고 Monitor 로 그 출력 파일을 지켜본다. 출력에 해시를 먼저 찍으면 Monitor 가 바로 끝나니 찍지 않는다. 포그라운드 sleep 은 막힌다. `a && b && (...) &` 처럼 &로 묶으면 앞 명령(푸시)도 같이 백그라운드로 가 버리니 따로 실행한다.
- github.com raw 다운로드는 막혀 있다. rm -rf 는 안전 검사에 막힐 수 있으니 새 폴더를 만들어 쓴다.
- 웹 화면은 소프트웨어 렌더링으로 느리다. npm run shot(몇 분), node scripts/playtest.mjs(10분쯤)는 그래도 돈다.
- 커밋 안 된 변경이나 푸시 안 한 커밋이 있으면 멈출 때 훅이 알린다. 사진 확인 전 문서는 git stash 로 잠깐 치워 둘 수 있다.

## 이번에 해 줬으면 하는 것
1. 사용자가 폰에서 본 의견(낚시, 위판장·미끼가게·수첩, 트럭)이 있으면 먼저 반영하고 CI 사진으로 확인.
2. 사용자가 17포즈 SD 시트를 다시 주면: 앉아 기다리기는 낚시 대기(Wait), 놀람(!)은 입질(Bite) 순간에 넣는다(scripts/sd_cells.py 로 자르고 sd_pose.py 처럼 poses.json 에 발·대 끝·키 기록).
3. 다음 단계 U9: 스토리와 컷신, 프롤로그 튜토리얼(첫 낚시와 첫 볼락구이로 짠맛이 돌아온다), 1장 클리어·차압 결말. 웹 src/scenes/PrologueScene.ts 와 docs/STORY.md 를 따른다. 코어 CompletePrologue, CheckChapter 를 쓴다. 컷신 화면은 9:16 한 장 그림 + 아래 글 상자, 지금 portraits 로 먼저 만들고 더 필요한 컷은 ART_PROMPTS 컷 1~8 프롬프트로 부탁한다. 새 게임 시작 흐름(타이틀 → 프롤로그 → 마을)을 연결한다. CI shots 목록에 장면 추가. 끝나는 기준: 사진에서 겹침·가림·화면 밖 글자 없음.
마지막: dotnet test core/Hanjeopsi.sln, dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 웹을 고쳤으면 npm run build·npm run shot·node scripts/playtest.mjs. 커밋 후 git push -u origin ccr-c846e49a-1tqms8, CI 사진 확인, git push origin ccr-c846e49a-1tqms8:main, APK 보내기, GODOT_PLAN·ROADMAP·HANDOFF 갱신.
````
