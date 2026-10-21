# unity/ · 한 접시의 바다 유니티 버전

전체 계획과 에디터 작업 순서는 [`docs/UNITY_PLAN.md`](../docs/UNITY_PLAN.md).

| 폴더 | 내용 |
|---|---|
| `com.songok.hanjeopsi.core/` | UnityEngine 없이 도는 게임 규칙과 데이터 (로컬 UPM 패키지) |
| `com.songok.hanjeopsi.game/` | 씬, UI, 연출 같은 유니티 쪽 코드 (로컬 UPM 패키지) |
| `dotnet/` | 코어를 dotnet으로 빌드하고 xUnit으로 테스트 |
| `golden/` | 웹 버전 코드로 뽑은 기준값. 테스트가 이것과 비교한다 |
| `HanjeopsiBada/` | 유니티 프로젝트 (Unity Hub로 만든다) |

## 코어 빌드와 테스트

.NET 8 SDK가 필요하다. 클라우드 세션(Ubuntu 24.04)에서는 `apt-get install -y dotnet-sdk-8.0` 으로 설치된다.

```bash
cd unity/dotnet
dotnet test          # 빌드 + 테스트. 모두 통과해야 한다
```

## 웹 수치를 바꿨을 때

```bash
npm run golden       # 웹 코드로 unity/golden/*.json 다시 만들기
cd unity/dotnet && dotnet test
```

JSON 데이터(`com.songok.hanjeopsi.core/Resources/HanjeopsiData/`)도 같이 고쳐야 테스트가 통과한다.

## 코드 규칙

- 코어는 C# 9까지만 쓴다(Unity 6). `record`, `init`, 파일 범위 namespace, `global using` 금지. dotnet 빌드가 `LangVersion 9.0`으로 막아 준다.
- 코어에서 `UnityEngine`을 쓰지 않는다. asmdef의 `noEngineReferences`가 막아 준다.
- 경고도 오류로 친다(`TreatWarningsAsErrors`).
- 주석과 문구는 한국어. 식별자는 영어.
