# 이어가기 (2026-10-27, cloud, main 에서 바로 작업, 다듬기 1~3차와 마을 퀘스트 뒤)

다음 세션 첫 메시지로 아래 코드 블록을 그대로 붙여 넣는다.

````
songok14/Yeoul 레포의 모바일 낚시 경영 게임 "한 접시의 바다"를 이어서 해 줘. (cloud 세션)

## 답변 방식
- 생각하는 과정을 포함한 모든 답변은 한국어로 답변한다. 문서, 커밋 메시지, 코드 주석, 게임 안 글도 한국어.
- 설명은 간략하고 쉽게 한다.
- 애매하거나 모호한 부분이 있다면 질문을 통해 해결하고 넘어간다.

## 지금까지 상황
- 환경: cloud. 원격 https://github.com/songok14/Yeoul (공개). 이제 브랜치는 main 과 ci-build(CI 결과 창고)만 쓴다. 세션이 다른 이름의 작업 브랜치를 주더라도 main 에서 작업해 main 에 푸시한다(사용자 결정 2026-10-27). PR은 만들지 않는다. 시작하면 git fetch origin, git checkout main, git pull 후 docs/HANDOFF.md 부터 읽는다.
- 원격에 지울 브랜치가 남아 있으면(ccr-c846e49a-1tqms8, claude/name-change-issue-l462yn) 사용자가 GitHub Branches 화면에서 지운다. 이 환경에서는 git push --delete 가 끊긴다.
- main 끝: 9c4bb9b 다음 이 문서 커밋. 사용자에게 마지막으로 보낸 APK는 빌드 #89(9c4bb9b).
- 먼저 읽을 것: CLAUDE.md, docs/POLISH.md(다듬기 목록, 지금 단계의 중심), docs/GODOT_PLAN.md(단계표 9절, 폰에서 볼 것 10-2), docs/GAME_DESIGN.md 3·6·7·8·9절과 7절 "마을 의뢰", docs/STORY.md, docs/ART_PROMPTS.md
- 단계: U0~U9, U11, U11-1~5 완료. 지금은 U12 전 다듬기(POLISH.md). 남은 큰 단계 U10(SD 다듬기 나머지), U12(폰에서 1장 끝까지).

### 지난 세션(2026-10-26~27)에 한 일 (모두 main 에 있음)
- 다듬기 1차: 낚시 손맛 연출(입질·챔질·질주·머리 흔들기·지침·끌어올림 진동·흔들림·번쩍임), 대·줄(손잡이에서 이어지고 물고기 쪽으로 휨), 가진 찌 그림, 지친 물고기(tug.tiredRiseMul·tiredReelMul), 릴별 게이지 구간(reels[].slack/danger), 낚싯대별 최대 거리(reach 0.74~1, "최대 17.8m"), species.json spots(1장 방파제 고기만 41종), 시세판 쿨러 어종 먼저, 여울식당→할머니댁, 트럭 간판·칠판 그림 손질, 상점 그림 체크무늬 지우기(item_sheet.py --holes).
- 다듬기 2차: 던지기 단추, 여울 앞에서 퍼지는 부채꼴, 힘은 아래 게이지로만, 대화창 닫힐 때 아래 버튼 눌림 막기, 새 장비는 노씨 첫 부탁 뒤에 판매·보상은 한 단계씩, 위판장 전부 팔기 위로·스크롤, 마을에서 여울이 걸어가기, 새 여울 낚시 포즈 여덟 장, 결과 카드, 진동 세기(약·보통·강), 트럭 메뉴 접기·이름 여울호, 마을 간판에 상태·아래 판 줄임·갈매기, 수첩 쿨러 스크롤, 타이틀 그늘.
- 마을 사람 SD 넷(어부·위판장 아주머니·초등학생·택배 기사)과 의뢰인 SD 다섯(노씨·바다횟집 사장·읍내 일식집·포장마차 아저씨·민박집 할머니) godot/art/villagers, 시간대마다 마당을 걷고 누르면 한마디(dialogue.json villagers).
- 은행 직원 대화 일러스트 여섯 표정(화자 banker), 상환 장면에서 직접 말한다.
- 마을 퀘스트: 납품 의뢰를 위판장 탭에서 빼서 마을 의뢰인 머리 위 !(맡기) → "볼락 1/2" → ✔(직접 건네기). 추적 카드(마을·낚시), "의뢰 고기!", 수첩 의뢰 탭. DeliveryRequest.accepted(고닷만), DialogueLine name·art.
- 위판장 쿨러 줄에 한 마리씩 크기(크기가 클수록 비싸다: 값 = 기준가 × (크기/평균)^1.5 × 시세 × 신선도).
- 테스트 128개, 자동 플레이 적당히 잘함 20/20 평균 22일.

## 확정된 결정
- Godot 4.7.2 .NET(C#), Compatibility, 세로 1080×1920. 유니티 접음. 돈 드는 것 금지.
- 규칙은 core/(순수 C#), 화면은 godot/scripts, 수치와 글은 godot/data/*.json(데이터 클래스는 MissingMemberHandling.Error 라 키를 늘리면 Core/Data/*.cs 도 고친다).
- 고닷은 웹과 다르게 간다. 웹 src/ 는 기준으로 남기고 골든 테스트는 TestData.Game(웹 모양)으로 본다. 고닷 전용 세이브 칸은 null 이면 JSON 에 안 나온다(세이브 클래스에 계산 속성을 두면 JSON 에 나오니 메서드로).
- 그림 역할: 움직이는 캐릭터는 SD 2~2.5등신, 대화와 감정은 일러스트, 감정이 터지는 순간은 9:16 컷신. 배경 수채화. 낚는 동안 물고기 그림은 안 보인다.
- 1장은 남해 방파제(chapter1.spot). 선상·갯바위 고기는 다음 장.
- 게임 안 글은 짧고 자연스럽게. 쉼표 남발·줄표 금지. 아버지 말은 STORY.md 그대로.

## 사용자 선호
- 단계마다 dotnet test 통과, CI APK 빌드 성공, APK 안 Hanjeopsi.dll 확인, CI 사진 직접 보고 고치기까지 한 뒤 main 에 푸시. 끝나면 APK 파일을 보내고 폰에서 볼 것을 알려 준다.
- 사용자는 휴대폰으로 해 보고 사진과 함께 의견을 준다. 받으면 바로 반영하고 POLISH.md 에 줄을 붙인다.
- 그림 프롬프트는 복붙용 코드 블록 하나씩, 시트형, 배경 #EDEDED(docs/ART_PROMPTS.md). 사용자가 그림을 주면 바로 잘라 넣고 화면에 연결한다(sd_sheet.py, sd_pose.py, clean_halo.py).
- 설계가 갈리는 건 AskUserQuestion 으로 묻고(추천안 먼저), 정하면 수치표를 GAME_DESIGN 에 남긴다.

## 환경 제약
- 컨테이너는 새로 만들어질 수 있다. 다시 설치: apt-get update 후 apt-get install -y dotnet-sdk-8.0, pip install scipy.
- 고닷은 여기서 실행 못 한다. C# 점검: dotnet build godot/Hanjeopsi.csproj -p:HanjeopsiNet8=true. 화면은 CI 사진으로 본다.
- CI: core/, godot/, 워크플로를 바꾼 푸시에 android.yml 이 돈다(사진 약 90장, 8~10분). 결과는 ci-build 브랜치(지우지 않는다): git fetch origin ci-build 후 git archive origin/ci-build | tar x -C <스크래치>, APK는 cat hanjeopsi.apk.part-* > hanjeopsi.apk.
- 기다릴 때는 백그라운드 Bash 로 ci-build 커밋 제목에 커밋 해시가 들어올 때까지 git fetch 반복. 포그라운드 sleep 금지.
- 사진 여러 장은 PIL 로 한 장에 이어 붙여 본다.
- github raw·릴리스 다운로드는 막혀 있다. 필요한 파일은 사용자에게 받는다.

## 다음에 할 것
1. 사용자 폰 의견(빌드 #89)을 받아 POLISH.md 에 붙이고 하나씩 고친다.
2. POLISH.md 남은 줄: SD 여울과 고등어가 마당에 떠 보임(U10) 등.
3. 다듬기가 끝나면 U12(폰에서 1장 끝까지).
````

사라지는 것: 컨테이너 스크래치의 CI 사진·APK는 세션이 끝나면 없어진다(APK는 릴리스 `test-latest`에 있다). 사용자가 준 원본 시트는 레포에 넣지 않았다.
