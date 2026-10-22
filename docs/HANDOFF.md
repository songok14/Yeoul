# 이어가기 (2026-10-22, 브랜치 `ccr-c846e49a-1tqms8`)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 작업해 줘. (cloud 세션) 모든 답변, 문서, 커밋 메시지, 코드 주석은 한국어로 써줘.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul, 작업 브랜치 ccr-c846e49a-1tqms8 (여기서 이어서 작업하고 푸시해. PR은 만들지 마). 마지막 코드 커밋 a45fdbe, CI 빌드 #20 성공(사진 확인함). 그 뒤 문서 커밋만 있다.
- 먼저 읽을 것: CLAUDE.md, docs/HANDOFF.md, docs/GODOT_PLAN.md(단계표 9절, 그림 역할 8절), docs/ART_PROMPTS.md, docs/GAME_DESIGN.md 9절
- 웹 버전(src/, Phaser)은 기준으로 남기고 고닷 4.7.2 C#으로 옮기는 중. U0~U6 완료(U5·U6은 폰 확인 대기). 사용자에게 APK 빌드 #20을 보냈다.
- 지난 세션에 한 일:
  - 대화창 사진 확인(선택지가 할머니 얼굴을 가리지 않음).
  - scripts/clean_halo.py 에 --fringe 추가: 갈색 머리 둘레 가는 가닥 사이 분홍빛 바탕과 머리 옆 작은 흰 조각을 정리. 여울 일러스트 10장에 적용. 흰머리(할머니)·회색 머리(노씨)에는 쓰지 않는다. 요리복·peace 그림은 크게 보면 머리 둘레에 흰 점이 조금 남아 있다.
  - U6 낚시 화면: godot/scenes/fishing.tscn, Screens/FishingScreen, Ui/FishingField(찌·그림자·물보라·줄), Ui/ReelGauge(파워·텐션). 방향 고르기와 고등어 힌트 → 파워 → 속임 입질(톡) → 챔질 → 릴링 → 결과 모달(흔들리는 물고기 그림, 크기, 시세, 규정·쿨러) → 철수(던졌으면 시간 한 칸). 처음 낚은 어종은 여울 peace 일러스트 대화가 먼저. 마을 방파제 간판·메뉴에서 들어간다. 문구는 godot/data/texts.json 의 fishing, 거리 구간 이름은 zones. 받침 조사는 UiKit.Topic(은/는), UiKit.Object(을/를). 배경 godot/art/backgrounds/breakwater.png, 물고기 그림 godot/art/fish/*.png.
  - 사용자가 준 SD 낚시 포즈(뒷모습 3장)로 여울을 바꿨다: scripts/sd_pose.py → godot/art/yeoul/fish-ready(대 세움)·fish-wait(기다림)·fish-reel(감기), 기준점(발·대 끝·키)은 godot/data/poses.json, 읽기는 Ui/SdPoses. 낚싯대는 그림에 있고 줄만 코드로 그린다. 발밑을 축으로 기울여 연출: 파워만큼 젖히기, 던지면 휘두르기, 입질에 쿡, 챔질에 들기, 텐션·저항에 끌려가기, 위험 구간이면 떨림. 고등어는 왼쪽 끝.
  - CI 사진에 fishing, fishing-power, fishing-reel, fishing-result, fishing-new, fishing-tall 추가(이름이 -tall 로 끝나면 1080×2400).
- 아직 안 쓴 SD 원본(사용자가 채팅에 첨부, 레포에 없음. 필요하면 다시 첨부해 달라고 한다): 옆모습 당기기·서 있기(투명 배경), 옆모습 당기기와 뒷모습(어두운 빛 배경, 빛은 투명), 15포즈 시트(체크무늬가 그림에 박혀 있음), 6포즈 시트(#EEE 배경, sd_sheet.py 로 지우면 가는 낚싯대가 끊긴다).
- 확인 필요: 폰에서 낚시 한 판(손맛, 텐션 감, 결과·철수 뒤 마을 시간이 한 칸 지나는지, 노치·긴 화면).

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility 렌더러, 세로 1080×1920. 유니티는 라이선스 문제로 접음. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts. 수치와 글은 godot/data/*.json. 웹 계산을 바꾸면 npm run golden.
- 그림 역할: 화면 위에서 움직이는 캐릭터(마을·낚시·트럭, 한 줄 말풍선)는 SD 2~2.5등신. 누군가와 이야기하는 장면은 대화창 + 감정 일러스트. 감정이 터지는 순간은 9:16 한 장 컷신. 배경은 수채화.
- 낚시 화면의 여울은 뒷모습 SD 포즈를 단계별로 바꿔 끼우고 기울기로 움직임을 준다(대는 그림, 줄은 코드).
- 화면 품질을 중요하게 본다: 웹보다 예쁘고 부드럽게(Tween 연출, 버튼 눌림 튕김, 모달 톡 튀어나옴, 페이드 전환).
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발, 줄표 금지. 아버지 말과 1장 기준(30일 300만 원)은 STORY.md 그대로.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 커밋·푸시.
- 끝나면 APK 파일을 보내 주고 폰에서 무엇을 볼지 알려 준다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED.
- 사용자가 그림을 주면 바로 활용해서 더 자연스럽고 예쁘게 만든다.
- docs/GODOT_PLAN.md 단계표와 docs/ROADMAP.md 체크박스를 갱신한다.

## 환경 제약
- 컨테이너는 매번 새로 만들어진다. 다시 설치: npm ci, apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy (numpy·pillow는 있음).
- 고닷은 여기서 실행 못 한다. C# 컴파일 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI가 찍은 사진으로 본다.
- CI: 푸시하면 .github/workflows/android.yml이 돈다(core/, godot/, 워크플로 변경 때만, 사진이 늘어 5~6분). 결과는 ci-build 브랜치: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치 폴더>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. 실행 상태는 GitHub MCP(mcp__github__actions_list, ToolSearch로 불러옴). 기다릴 때는 ci-build 커밋 제목에 내 커밋 해시가 들어올 때까지 백그라운드로 git fetch 를 반복한다.
- github.com raw 다운로드는 막혀 있다.
- 웹 화면은 소프트웨어 렌더링으로 초당 7프레임 정도. npm run shot, node scripts/playtest.mjs(10분쯤)는 그래도 돈다.
- rm -rf 로 폴더를 지우는 명령은 안전 검사에 막힐 수 있으니 새 폴더를 만들어 쓴다.

## 이번에 해 줬으면 하는 것
1. 사용자가 폰에서 본 낚시 화면 의견이 있으면 먼저 반영하고 CI 사진으로 확인.
2. 다음 단계 U7: 위판장(경매), 미끼가게(장비·미끼), 수첩(도감·미각). 웹 src/scenes/MarketScene·ShopScene·NotebookScene 과 코어 Market·Game 행동(SellFish, BuyBait, UpgradeGear, FeedCat, 도감)을 그대로 쓴다. 글은 texts.json 에. 마을 메뉴와 간판의 NotYet 자리를 연결. 할머니·노씨와 이야기하는 부분은 대화창 + 일러스트, 움직이는 캐릭터는 SD. CI 사진 모드(shots 목록)에 장면 추가. 끝나는 기준: 사진에서 겹침·가림·화면 밖 글자 없음.
3. 남은 SD 원본을 쓸 곳이 있으면(예: 결과 기쁨 SD, 옆모습 당기기) 사용자에게 다시 첨부를 부탁하고 sd_pose.py 로 넣는다.
마지막: dotnet test core/Hanjeopsi.sln, dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 웹을 고쳤으면 npm run build·npm run shot·node scripts/playtest.mjs. 커밋 후 git push -u origin ccr-c846e49a-1tqms8, CI 사진 확인, APK 보내기, GODOT_PLAN·ROADMAP·HANDOFF 갱신.
````
