# CLAUDE.md

이 문서는 Claude Code가 이 Unity 프로젝트에서 작업할 때 참고하는 프로젝트 가이드입니다.

## 1. 프로젝트 개요

**Word Puzzle**은 한글 단어를 자모(초성/중성/종성) 단위로 분해해 "숫자야구(스트라이크/볼/아웃)" 방식으로
정답을 맞히는 Unity 모바일 퍼즐 게임입니다.

주요 게임 흐름:
- **싱글 플레이**: 글자 수를 선택하면 랜덤 단어가 주어지고, 자모 토큰을 조합해 정답을 추리. 포인트로 힌트 구매 가능.
- **일일 도전**: 날짜를 시드로 모든 유저에게 동일한 단어가 주어지는 1일 1회 도전 모드. 스트릭 기록.
- **멀티플레이**: Photon(PUN2) 기반 1:1 대전. 턴제로 번갈아 제출하며, 특정 턴 수 이후 자동 힌트가 공개됨.
- 진행 상황(포인트, 클리어 기록, 승/패)은 로컬(PlayerPrefs)에 저장되고 Firebase로 클라우드 동기화됨.

## 2. 폴더 구조 (Assets 기준)

```
Assets/
├── Scripts/
│   ├── Core/       자모 변환·판정 엔진 (싱글/일일/멀티 공용, 순수 로직)
│   ├── Data/       단어 데이터 모델 및 로더
│   ├── Single/     싱글 플레이 컨트롤러·힌트 컨트롤러
│   ├── Daily/      일일 도전 컨트롤러
│   ├── Multi/      Photon 기반 멀티플레이 전체 로직
│   ├── Save/       저장 데이터 구조 + SaveManager
│   ├── Firebase/   Firebase 연동 매니저
│   ├── UI/         공용 UI 컴포넌트(팝업, 토큰뷰, 히스토리 등)
│   └── Audio/      BGM/SFX 매니저
├── Scenes/         Intro, SingleGame, DailyChallenge, MultiMenu, MultiRoom
├── Prefabs/        TokenCell(자모 칸), InputHistoryItem(입력 기록 1줄)
├── StreamingAssets/
│   ├── words.json                     핵심 단어 데이터베이스 (런타임 로드)
│   └── google-services-desktop.json   Firebase 데스크탑 빌드용 설정
├── Editor/BuildScript.cs   Android 빌드 CLI 스크립트 (Mono 백엔드 고정)
├── Firebase/, Photon/, ExternalDependencyManager/   서드파티 SDK (직접 수정 금지)
└── google-services.json   Firebase 안드로이드 설정 (민감 파일, 임의 수정 금지)
```

- `Resources` 폴더는 별도로 없고, 서드파티 SDK 내부에 `PhotonServerSettings.asset` 등이 각 SDK의
  `Resources/` 경로에 있습니다(Photon 연결 설정).
- 프리팹은 `TokenCell`, `InputHistoryItem` 2개뿐이며 나머지 UI는 씬에 직접 배치되어 있습니다.

## 3. 주요 시스템

### 단어/퍼즐 판정 시스템 (`Scripts/Core`)
- `JamoConverter`: 한글 음절을 초성/중성/종성으로 분해. 판정용 토큰(`GetAnswerTokens`)과
  화면 표시용 토큰(`GetDisplayTokens`, 회전 치환 규칙 적용 — 예: ㅗ/ㅜ/ㅓ→ㅏㅑ 계열)을 분리해서 생성.
  쌍자음/겹받침/복합모음은 개별 자모로 추가 분해됨.
- `BaseballJudge`: 정답 토큰과 입력 토큰을 비교해 Strike/Ball/Out 판정. 토큰별 판정 결과(`TokenHit[]`)도 함께 반환.
- 이 두 클래스는 싱글/일일/멀티 세 모드가 그대로 공유하는 핵심 로직이므로, 여기를 수정하면 전 모드에 영향.

### 힌트 시스템
- 싱글: `HintController`가 자모 힌트(포인트 소모, 랜덤 위치 1칸 공개), `SingleGameController`가 한 줄 힌트(단어 설명 공개)를 관리.
- 일일: 한 줄 힌트만 존재(`DailyChallengeController.OnLineHint`).
- 멀티: 자동 힌트 — 각 플레이어 5턴씩(총 10턴) 이후 글자 수 공개, 각 플레이어 20턴씩(총 40턴) 이후 단어 힌트(또는 첫 음절) 자동 공개(`MultiGameController.RefreshHints`).
- 맞춘 위치(Strike)는 힌트 여부와 무관하게 자동으로 `TokenView.LockPosition`으로 고정 표시됨.

### 히스토리/저장 시스템 (`Scripts/Save`)
- `InputHistoryView` / `InputHistoryItem`: 입력 기록 리스트 UI (싱글/일일/멀티 공용).
- `SaveManager`: PlayerPrefs 기반 로컬 저장 담당 창구. 저장 시 `FirebaseManager`로도 자동 전달(있으면).
  - `SingleSaveData`, `DailySaveData`, `MultiSaveData`: 각 모드의 누적 진행 데이터.
  - `MidGameSave`: 싱글 플레이 중도 이탈 시 진행 상태(맞춘 위치, 힌트 로그, 히스토리, 토큰 풀 상태) 저장 → 재접속 시 복원.

### UI 시스템 (`Scripts/UI`)
- `TokenView`: 자모 토큰 칸을 동적으로 배치/재생성. 화면 너비에 맞춰 셀 크기·간격 자동 조정.
- 팝업류(`SettingsPopup`, `ProfilePopup`, `NicknameSetupPopup`, `AccountRestorePopup`, `WordLengthSelectPopup`)는
  모두 `SetActive(true/false)`로 단순 표시/숨김 처리하는 패턴.
- `MainMenuController`가 각 팝업/씬 전환의 진입점 역할.
- `ConfettiSystem`, `StarParticleSystem`: 클리어 연출용 파티클 이펙트.

### Firebase 연동 (`Scripts/Firebase/FirebaseManager.cs`)
- 싱글톤, 익명 로그인 기반. `users/{uid}` 하위에 닉네임/싱글/일일/멀티 데이터를 저장.
- 닉네임은 `nicknames/{key}` 인덱스로 중복 체크 후 저장.
- 기기 변경 등으로 새 uid가 발급되면 `OnNewUserDetected` 이벤트 발행 → `AccountRestorePopup`이 닉네임 기반으로
  이전 계정 데이터를 복구하거나 신규 시작을 선택하게 함.
- 로컬(PlayerPrefs)이 항상 1차 저장소이고, Firebase는 백업/기기 간 동기화 역할.

### 멀티플레이 (`Scripts/Multi`, Photon PUN2)
- 전체 스크립트가 `#if PHOTON_UNITY_NETWORKING` 전처리 가드로 감싸져 있음.
- `PhotonMultiplayerManager`: 서버 접속, 방 생성/입장 (지역 `kr` 고정).
- `MultiRoomController`: 대기방 준비 상태 동기화, 카운트다운 → 게임 시작.
- `MultiGameController`: 선공/후공 카드 선택 연출, Room Custom Properties(`wordId`, `turnIndex`, `turnStartTime`)로
  턴 상태 동기화, 60초 턴 타이머, 자동 힌트 공개.
- `MultiSubmitController`: 입력 제출, "히든 아이템"(상대에게만 결과를 숨기는 기능) 처리.
- `MultiNetworkEvents`: RaiseEvent용 이벤트 코드 상수 모음(1~10). 새 이벤트 추가 시 여기에 코드 등록.

### 기타 매니저
- `SoundManager`: BGM/SFX 볼륨 관리, PlayerPrefs 저장.
- `WordDatabase`: `words.json`을 코루틴으로 로드, ID/단어/글자수/날짜 시드 기반 조회 제공. 로드 완료 전에는
  `WordDatabase.Instance.IsLoaded`를 반드시 확인 후 사용(대부분의 컨트롤러가 `WaitAndInit` 코루틴 패턴 사용).

## 4. 데이터 구조

### 단어 데이터 (`StreamingAssets/words.json` → `WordData`)
```csharp
public class WordData
{
    public int    id;          // 고유 ID
    public string word;        // 실제 단어(한글)
    public int    length;      // 글자 수 (음절 수 기준)
    public int    difficulty;  // 난이도
    public string hint;        // 한 줄 힌트 문구
}
```
- **주의**: `id`는 `MidGameSave`, Photon Room Custom Properties(`wordId`)에서 단어를 다시 찾는 키로 쓰입니다.
  기존 단어의 `id`를 변경하거나 삭제하면 저장된 중도 게임/진행 중인 멀티 세션이 깨질 수 있습니다.
- `length`는 싱글 플레이 글자 수 선택, 일일 도전 풀(2~4글자) 필터링에 직접 사용되므로 실제 `word` 글자 수와
  반드시 일치해야 합니다.
- 단어를 추가하는 것은 안전하지만, 기존 `id`/`length`를 바꾸는 것은 저장 데이터 호환성 문제를 일으킬 수 있습니다.

### 저장 데이터 (모두 `JsonUtility` + PlayerPrefs 직렬화, `Scripts/Save/SaveData.cs`, `MidGameSave.cs`)
- `SingleSaveData`: 글자수별 클리어 수, 클리어한 단어 ID 목록, 힌트 사용 수, 포인트.
- `DailySaveData`: 마지막 플레이 날짜, 오늘 클리어 여부, 스트릭, 오늘 시도 횟수 등.
- `MultiSaveData`: 승/패/총 판수.
- `MidGameSave` / `HistoryEntryData`: 싱글 중도 저장용 스냅샷(토큰 잠금 상태, 힌트 로그, 입력 히스토리).
- **주의**: 이 클래스들의 필드를 이름 변경/삭제하면 기존 유저의 PlayerPrefs에 저장된 JSON을
  `JsonUtility.FromJson`으로 역직렬화할 때 값이 유실되거나 예외가 날 수 있습니다. 필드 추가는 비교적 안전하지만,
  기존 필드는 되도록 유지하고 마이그레이션이 필요하면 별도로 설계해야 합니다.

### ScriptableObject
- 현재 프로젝트에는 게임 데이터용 ScriptableObject가 없습니다. 단어 DB는 JSON + 런타임 로드 방식만 사용 중입니다.

## 5. 코딩 규칙

- 기존 코드 스타일을 우선 따릅니다: `private` 필드는 `_camelCase`, `[SerializeField] private` 우선 사용,
  네임스페이스는 `WordPuzzle.{도메인}` 규칙(`WordPuzzle.Core`, `WordPuzzle.UI` 등).
- 변수명/함수명은 의미가 드러나게 작성(축약어 남용 금지).
- 주석은 필요한 부분에만, 한글로 짧게(왜 그렇게 했는지 — 특히 동기화 타이밍, race condition 회피 이유 등
  코드만 봐서는 알기 어려운 부분 위주). 코드가 이미 설명하는 내용은 주석으로 반복하지 않습니다.
- 불필요한 리팩터링, 과한 추상화, 미래를 대비한 인터페이스/설정 옵션 추가는 지양합니다. 요청받은 범위만 수정합니다.
- 모바일 빌드 대상이므로 매 프레임 할당, 불필요한 `Instantiate`/`Destroy` 반복, 과도한 `Find` 계열 호출은
  피하고 기존 패턴(캐싱된 참조, 이벤트 기반 갱신)을 따릅니다.

## 6. 작업 시 주의사항

- 기존 기능을 수정하기 전에 관련 스크립트와 데이터 흐름(예: `WordDatabase` → 컨트롤러 → `TokenView`/`SaveManager`)을
  먼저 읽고 파악한 뒤 변경합니다.
- Unity 씬/프리팹/인스펙터에 연결된 `[SerializeField]` 참조가 필요한 변경(새 UI 요소 추가, 컴포넌트 교체 등)은
  코드만 고치지 말고, 씬/프리팹에서 어떤 설정을 추가로 해야 하는지 반드시 함께 안내합니다(Claude Code가 직접
  씬 파일을 편집하지 않는 한).
- Firebase 설정 파일(`google-services.json`, `StreamingAssets/google-services-desktop.json`)이나
  Photon `PhotonServerSettings.asset`의 App ID 등 민감 정보가 담긴 파일은 요청 없이 임의로 수정/삭제하지 않습니다.
- `StreamingAssets/words.json`의 스키마(필드 구성)나 기존 데이터의 `id`/`length`는 임의로 바꾸지 않습니다.
  단어 추가/오타 수정 등은 가능하나, 구조 변경은 사용자 확인 후 진행합니다.
- 변경 후에는 어떤 기능/모드(싱글·일일·멀티)에 영향이 갈 수 있는지 간단히 설명합니다.

## 7. Claude Code 작업 참고 규칙

- 작업 시작 전: 관련 파일을 먼저 읽고 구조(호출 관계, 데이터 흐름)를 파악한 뒤 수정을 시작합니다.
- 수정 전: 어떤 파일을 어떻게 바꿀지 간단히 설명합니다.
- 수정 후: 어떤 기능이 어떻게 바뀌었는지 요약합니다.
- 에러 가능성이 있는 부분(예: 저장 데이터 호환성, Photon 이벤트 순서, 씬 인스펙터 연결 누락 등)은 별도로 짚어줍니다.
