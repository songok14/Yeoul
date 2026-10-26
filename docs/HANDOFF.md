# 이어가기 (2026-10-26, 작업 브랜치 `ccr-c846e49a-1tqms8`, 힘싸움·어종 71종 뒤)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어.
- 설명은 간략하고 쉽게 한다.
- 애매하거나 모호한 부분이 있다면 질문을 통해 해결하고 넘어간다.

## 지금까지 상황
- 원격 https://github.com/songok14/Yeoul (공개 레포). 작업 브랜치 ccr-c846e49a-1tqms8 에서 이어서 하고, 확인이 끝나면 main 에도 올린다(git push origin ccr-c846e49a-1tqms8:main). PR은 만들지 않는다. 시작하면 git fetch origin 후 그 브랜치로 체크아웃.
- 브랜치 끝: 5a8756e 다음 이 문서 커밋. main 끝: 5698aa7.
  - main 에는 사용자가 다른 브랜치에서 PR #1(마을 밤·비 배경, Ui/RainOverlay, GameState 날씨 인자), PR #2(WebP 마을 배경 우선 로드)를 합쳐 두었다. 작업 브랜치에도 같은 내용(8151162, 343e53a)이 따로 들어와 있다.
  - main 으로 올리기 전에 git merge origin/main 으로 합치고 겹친 곳(TownScreen, GameState, 워크플로)을 확인한다. 강제 푸시 금지.
- 사용자에게 마지막으로 보낸 APK는 빌드 #42(aec7e2d). 그 뒤 커밋 ff88126(힘싸움·그린 낚싯대·진동, CI #44 성공, 사진은 아직 안 봄)과 5a8756e(어종 71종, CI 확인 필요)가 있다.
- 먼저 읽을 것: CLAUDE.md, docs/GODOT_PLAN.md(그림 역할 8절, 소리 8-1, 단계표 9절, 폰에서 볼 것 10-2), docs/GAME_DESIGN.md 3절(고닷 낚시)·9절, docs/STORY.md, docs/ART_PROMPTS.md
- 단계: U0~U9, U11 완료(U5~U11 폰 확인 대기). 남은 큰 단계 U10(SD 다듬기), U12(폰에서 1장 끝까지).

### 이번 세션(2026-10-22~26)에 한 일
- 소리: 효과음 31개는 scripts/sound.py 합성(사용자가 좋다고 함). 배경음은 사용자가 준 Pixabay 곡 다섯 개(title, town, sea, market, truck←"restaurant")를 scripts/bgm.py 로 넣었다(앞뒤 무음 자르기, 끝 3초를 처음에 겹쳐 반복, −18dB). 원본 mp3는 레포에 없다.
- 진동: Haptics(전체 세기 Master 0.7), 손맛 세기 SpotRules.Feel(어종 힘·크기·귀함·큰 놈 → 0.15~1)로 입질·질주·버티기·끊김 진동 세기와 길이를 나눈다. 수첩 설정 탭(효과음·배경음·진동 켜고 끄기, user://settings.cfg).
- 낚시(고닷만, 웹과 다름):
  - 부채꼴 위 금색 바늘로 방향, 파워 게이지로 거리. 고등어가 가리키는 금빛 지점에 가까울수록 귀한 고기와 큰 놈. 입질 시간은 어디든 같다(SpotRules).
  - 큰 놈 입질, 고등어 도둑(쿨러를 노리면 고등어를 눌러 말리기).
  - 낚는 동안 물고기는 그리지 않는다(사용자가 그림자·튀어 오르는 물고기를 "짜친다"며 뺌). 줄 끝은 들끓는 물과 물기둥으로 보여 준다.
  - 힘싸움 TugFight(core/Hanjeopsi.Core/Systems/Tug.cs, balance.json tug): 물고기가 0.6초 예고 뒤 왼쪽·오른쪽으로 질주한다. 반대쪽 화면 절반을 누르면 버티며 물고기 힘이 크게 빠지고, 같은 쪽을 누르면 텐션이 치솟는다. 팔팔한 물고기는 잘 안 끌려오고 다 지치면 쉽게 온다. 화면에 물고기 힘 막대, 질주 화살표, 반대쪽 금빛 띠.
  - 감기·버티기 SD 포즈(fish-reel, fish-pull)는 그림 속 낚싯대를 손잡이에서 지우고 FishingField.DrawRod 가 그린다. 텐션만큼 물고기 쪽으로 휜다(poses.json grip·rodDir·rodLen).
  - 낚싯줄: 느슨하면 처지고 팽팽하면 곧게, 위험하면 떨리며 금빛→빨강.
- 어종 71종: FishingPlan 도감(songok14/fishingplan 의 core/.../catches/Fishdex.kt)의 바다 어종 전부를 같은 id·이름·별(1~3)로. 그림은 그 레포 app/src/main/res/drawable-nodpi/fish_<id>.webp 를 PNG로(godot/art/fish). 노래미는 정확한 이름 쥐노래미로 바꿨다.
  - species.json 새 필드: stars, months(달별 제철 가중치 12칸), seas(south/east/west/jeju). 1장 바다는 남해(balance chapter1.sea)라 61종이 낚인다.
  - 금지체장·금어기는 FishingPlan 앱 기본표(Limits.kt BASE)만 따른다.
  - 수첩 도감은 3열×4줄 카드 쪽 넘김. 위판장 시세판은 제철 어종 다섯 칸(Game.BoardSpecies), 마을 소식도 제철 어종만(Game.HeadlineToday).
  - 새 어종은 아직 트럭 요리(recipes.json 5개)와 납품 의뢰(requests.json 8개)가 없다. 위판장에서만 판다.
- 테스트: 웹과 견주는 골든 테스트는 웹 모양 데이터(TestData.Game: 웹 다섯 종, 웹 이름, 웹 REEL_TUNING)로, 자동 플레이와 고닷 규칙은 TestData.Full(71종). 지금 89개 통과. 자동 플레이: 적당히 잘하면 1장 20/20 클리어(평균 24일), 기다림 2.2초, 힘싸움 평균 12초, 질주 3.2번(버팀 76%).
- 이야기 화면(U9): Screens/StoryScreen, godot/data/story.json(p1·p2·clear·fail). 새 게임은 타이틀 → p1 → 튜토리얼 낚시 → p2(소금) → 마을. 장면을 넘길 때 그림이 글 상자를 덮던 버그는 고쳤다(사용자가 "모든 대사 잘 나온다" 확인).
- 그림: 사용자가 준 여울 일러스트 새 판 여섯 장을 godot/art/portraits 에 넣고, 잔머리가 심한 옛 판 다섯 장은 지웠다. 지친 여울 그림 다리 사이 흰 배경 지움. 마을 배경은 사용자가 준 눈높이 그림(밤·낮). 아침·저녁은 scripts/village_time.py 로 만든다(흰 구름은 색을 입히지 않는다. ArtStage.KeepClouds 셰이더도 같은 기준). 손님 SD 여섯 명 모두 집배원과 같은 방향으로 걸어 들어온다.
- songok14/fishingplan 은 이 세션에서 읽기로만 붙였다(/home/user/fishingplan). 그 레포에는 쓰지 않는다. 새 세션에서 필요하면 add_repo 로 다시 붙인다.

### 확인 필요
- CI #44(ff88126) 사진: fishing-run(질주와 금빛 띠), fishing-reel·pull(그린 낚싯대가 손잡이에 자연스럽게 붙는지), story 장면들.
- CI(5a8756e) 사진: notebook-dex(3열×4줄 카드, 글자 넘침), market(시세판 다섯 칸), town(소식, 사용자 비 효과와 같이).
- 폰: 힘싸움 손맛(반대쪽 누르기가 이해되는지, 너무 어렵거나 쉬운지), 진동 세기, 그린 낚싯대, 새 어종이 봄 남해답게 나오는지, 배경음 음량과 반복 이음새.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility 렌더러, 세로 1080×1920. 유니티는 라이선스 문제로 접음. "유니티면 고퀄?" 물음에는 엔진보다 연출·그림·손맛 문제라 고닷 유지로 정했다. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts. 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다). 웹 계산을 바꾸면 npm run golden.
- 고닷 낚시는 웹과 다르게 간다(위 내용). 웹 src/ 는 다섯 어종·옛 낚시 그대로 기준으로 남긴다.
- 그림 역할: 화면 위에서 움직이는 캐릭터는 SD 2~2.5등신, 대화와 감정은 일러스트, 감정이 터지는 순간은 9:16 컷신(아래 30% 글 상자). 배경은 수채화.
- 낚는 동안 물고기 그림은 보이지 않는다(결과창 그림은 보인다).
- 어종 이름·그림·별은 FishingPlan 도감과 같게, 규정은 그 앱 기본표만.
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발, 줄표 금지. 아버지 말과 1장 기준(30일 300만 원)은 STORY.md 그대로.
- 배경음은 Pixabay 무료 곡(상업용 무료, 출처 표시 필요 없음). 효과음은 합성.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 커밋·푸시. 끝나면 APK 파일을 보내 주고 폰에서 무엇을 볼지 알려 준다.
- 사용자는 폰에서 직접 해 보고 사진과 함께 의견을 준다. 받으면 바로 반영한다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED(docs/ART_PROMPTS.md 형식). 사용자가 그림을 주면 바로 활용해서 더 자연스럽고 예쁘게 만든다.
- docs/GODOT_PLAN.md 단계표, docs/ROADMAP.md 체크박스, docs/GAME_DESIGN.md 표, docs/HANDOFF.md 를 갱신한다.

## 환경 제약
- 컨테이너는 새로 만들어질 수 있다. 다시 설치: apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy (numpy·pillow·ffmpeg 는 있음). 웹을 고칠 때만 npm ci.
- 고닷은 여기서 실행 못 한다. C# 컴파일 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI가 찍은 사진으로 본다.
- CI: core/, godot/, 워크플로를 바꾼 푸시에 .github/workflows/android.yml 이 돈다(사진 약 60장, 8~10분). 결과는 ci-build 브랜치: git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치 폴더>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk. 실행 상태는 GitHub MCP actions_list(ToolSearch로 불러옴).
- 기다릴 때는 백그라운드 Bash 로 ci-build 커밋 제목에 커밋 해시가 들어올 때까지 git fetch 를 반복한다(출력에 해시를 먼저 찍지 않는다). 포그라운드 sleep 은 막힌다.
- 음악·소리 사이트(Pixabay, OpenGameArt, freesound 등)는 네트워크가 막혀 있다. 곡이 필요하면 사용자에게 받아 scripts/bgm.py 로 넣는다. github.com raw 다운로드도 막혀 있다.
- 사진 여러 장을 한꺼번에 열면 요청 한도에 걸린다. 여러 사진은 PIL 로 한 장에 이어 붙여 본다.
- 다른 사람이 같은 브랜치에 푸시할 수 있다(이번에 사용자가 비 효과를 올렸다). 푸시가 거절되면 fetch 후 merge(리베이스·강제 푸시 금지).

## 이번에 해 줬으면 하는 것
1. 사용자가 폰에서 본 의견이 있으면 먼저 반영하고 CI 사진으로 확인한다.
2. git merge origin/main 으로 main 의 PR #1·#2 를 합치고, 빌드·테스트 뒤 CI(최신 커밋) 사진을 본다: fishing-run, fishing-reel, fishing-pull, notebook-dex, market, town 계열, story 계열. 겹침·가림·화면 밖 글자, 그린 낚싯대 이음새를 고친다. 끝나면 main 에 올리고 APK를 보낸다.
3. 문서 갱신: GAME_DESIGN 3절에 힘싸움(tug 수치표)과 어종 71종(제철·바다 필드, 규정 표), GODOT_PLAN 단계표와 10-2(폰에서 볼 것: 힘싸움, 진동, 도감 71종), ROADMAP 체크.
4. 새 어종에 트럭 요리와 납품 의뢰를 더할지 사용자에게 물어본다(예: 참돔회, 도다리쑥국, 갑오징어숙회, 우럭매운탕, 농어구이). 하면 recipes.json·requests.json 에 넣고 자동 플레이로 1장 균형(적당히 잘함 18/20 이상)을 확인한다.
5. 그다음은 사용자와 정한다: U10(여울 SD 17포즈 시트의 앉아 기다리기·놀람 포즈를 낚시 대기·입질에. 시트는 다시 첨부 부탁), 컷신 전용 그림(ART_PROMPTS 컷 1~8), U12 폰 1장 통 플레이.
마지막: dotnet test core/Hanjeopsi.sln(지금 89개), dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true, 웹을 고쳤으면 npm run build·npm run shot·node scripts/playtest.mjs. 커밋 후 git push origin ccr-c846e49a-1tqms8, CI 사진 확인, git push origin ccr-c846e49a-1tqms8:main, APK 보내기, 문서와 HANDOFF 갱신.
````
