#if PHOTON_UNITY_NETWORKING
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Core;
using WordPuzzle.Data;
using WordPuzzle.Save;
using WordPuzzle.UI;

namespace WordPuzzle.Multi
{
    public class MultiGameController : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        [Header("UI 참조")]
        [SerializeField] private TokenView              tokenView;
        [SerializeField] private TextMeshProUGUI        timerText;
        [SerializeField] private TextMeshProUGUI        turnIndicatorText;
        [SerializeField] private TextMeshProUGUI        myNicknameText;
        [SerializeField] private TextMeshProUGUI        opponentNicknameText;
        [SerializeField] private GameObject             gamePanel;
        [SerializeField] private GameObject             lobbyCard;
        [SerializeField] private MultiResultScreen      resultScreen;
        [SerializeField] private MultiSubmitController  submitController;
        [SerializeField] private MultiResultHistoryView historyView;

        [Header("힌트 UI")]
        [SerializeField] private TextMeshProUGUI wordLengthHintText;
        [SerializeField] private TextMeshProUGUI autoHintText;

        [Header("선공/후공 선택 패널")]
        [SerializeField] private GameObject      selectionPanel;
        [SerializeField] private TextMeshProUGUI selectionTitle;
        [SerializeField] private Image           card1Img;
        [SerializeField] private Image           card2Img;
        [SerializeField] private TextMeshProUGUI card1Text;
        [SerializeField] private TextMeshProUGUI card2Text;
        [SerializeField] private TextMeshProUGUI selectionResultText;

        private static readonly Color ColCardBack   = new Color(0.22f, 0.24f, 0.42f, 1f);
        private static readonly Color ColCardFirst  = new Color(0.85f, 0.65f, 0.10f, 1f);
        private static readonly Color ColCardSecond = new Color(0.28f, 0.46f, 0.90f, 1f);

        private const string PropWordId    = "wordId";
        private const string PropTurn      = "turnIndex";
        private const string PropStartTime = "turnStartTime";
        private const double TurnDuration  = 60.0;

        private WordData     _currentWord;
        private List<string> _answerTokens;
        private int          _turnIndex;
        private double       _turnStartTime;
        private bool         _isRunning;
        private bool         _activePlayerSubmitted;
        private int          _prevTimerSeconds = -1;
        private int[]        _turnActors;
        private int          _lastBuiltWordId = -1; // 동일 단어 재빌드 방지(잠금 누적용)

        private bool _cardSelected;  // CardTapped 이벤트 수신 후 RevealCoroutine 시작됨
        private bool _cardTapSent;   // 이미 CardTapped 이벤트를 전송했음 (중복 전송 방지)
        private bool _isMyTurnFirst;

        private readonly Dictionary<int, MultiPlayerState> _players = new Dictionary<int, MultiPlayerState>();

        private int  ActiveActorNumber => (_turnActors != null && _turnActors.Length == 2)
                                          ? _turnActors[_turnIndex % 2] : -1;
        private bool IsMyTurn          => ActiveActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

        private void OnEnable()  => PhotonNetwork.AddCallbackTarget(this);
        private void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

        // ── 게임 시작 ─────────────────────────────────────────────────
        public void StartGame()
        {
            if (lobbyCard) lobbyCard.SetActive(false);
            gamePanel.SetActive(true);
            historyView.Clear();
            submitController.ResetForGame();
            if (wordLengthHintText) wordLengthHintText.gameObject.SetActive(false);
            if (autoHintText)       autoHintText.gameObject.SetActive(false);

            _players.Clear();
            foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
                _players[p.ActorNumber] = new MultiPlayerState(p.ActorNumber, p.NickName);

            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
            foreach (var kv in _players)
            {
                if (kv.Key == myActor) { if (myNicknameText)      myNicknameText.text      = kv.Value.Nickname; }
                else                   { if (opponentNicknameText) opponentNicknameText.text = kv.Value.Nickname; }
            }

            _lastBuiltWordId = -1;

            // 선공/후공 선택 패널 표시
            _cardSelected = false;
            _cardTapSent  = false;
            ShowSelectionPanel();

            // MasterClient가 랜덤으로 선공 결정 후 모두에게 전달
            if (PhotonNetwork.IsMasterClient)
            {
                var actors = new List<int>(PhotonNetwork.CurrentRoom.Players.Keys);
                actors.Sort();
                if (Random.Range(0, 2) == 1) { int tmp = actors[0]; actors[0] = actors[1]; actors[1] = tmp; }
                var data = new object[] { (object)actors[0], (object)actors[1] };
                PhotonNetwork.RaiseEvent(
                    MultiNetworkEvents.TurnOrderSet, data,
                    MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
            }
        }

        // ── 선공/후공 선택 UI ─────────────────────────────────────────
        private void ShowSelectionPanel()
        {
            if (selectionPanel) selectionPanel.SetActive(true);
            if (selectionTitle) selectionTitle.text = "선공 카드를 선택하세요!";
            if (selectionResultText) selectionResultText.gameObject.SetActive(false);
            if (card1Img)  card1Img.color  = ColCardBack;
            if (card2Img)  card2Img.color  = ColCardBack;
            if (card1Text) card1Text.text   = "?";
            if (card2Text) card2Text.text   = "?";
        }

        // 버튼에서 호출 (카드 인덱스 0=왼쪽, 1=오른쪽)
        public void OnCard1Tapped() => TapCard(0);
        public void OnCard2Tapped() => TapCard(1);

        private void TapCard(int cardIdx)
        {
            if (_cardTapSent || _cardSelected) return;
            if (_turnActors == null) return;
            _cardTapSent = true;
            // 직접 RevealCoroutine 호출 대신 Photon 이벤트로 양쪽에 동시 전달
            PhotonNetwork.RaiseEvent(
                MultiNetworkEvents.CardTapped,
                new object[] { (object)cardIdx },
                MultiNetworkEvents.All,
                MultiNetworkEvents.Reliable);
        }

        // CardTapped 이벤트 수신 — 양쪽 클라이언트가 동시에 RevealCoroutine 실행
        private void OnCardTappedReceived(int cardIdx)
        {
            if (_cardSelected) return;
            _cardSelected = true;
            _cardTapSent  = true;
            StartCoroutine(RevealCoroutine(cardIdx == 0));
        }

        private IEnumerator WaitForTapOrAutoReveal()
        {
            float elapsed = 0f;
            while (!_cardSelected && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            // MasterClient만 자동 탭 이벤트 전송 (중복 방지)
            if (!_cardSelected && !_cardTapSent && PhotonNetwork.IsMasterClient)
            {
                _cardTapSent = true;
                PhotonNetwork.RaiseEvent(
                    MultiNetworkEvents.CardTapped,
                    new object[] { (object)Random.Range(0, 2) },
                    MultiNetworkEvents.All,
                    MultiNetworkEvents.Reliable);
            }
        }

        private IEnumerator RevealCoroutine(bool tappedCard1)
        {
            // 내가 선공이면 → 탭한 카드 = 선공, 나머지 = 후공
            bool tap1IsFirst = tappedCard1 ? _isMyTurnFirst : !_isMyTurnFirst;

            RevealCard(isCard1: true,  isFirst: tap1IsFirst);
            yield return new WaitForSeconds(0.35f);
            RevealCard(isCard1: false, isFirst: !tap1IsFirst);

            if (selectionResultText)
            {
                selectionResultText.gameObject.SetActive(true);
                selectionResultText.text  = _isMyTurnFirst ? "선공!" : "후공";
                selectionResultText.color = _isMyTurnFirst ? ColCardFirst : ColCardSecond;
            }
            if (selectionTitle)
                selectionTitle.text = _isMyTurnFirst ? "선공입니다!" : "후공입니다";

            yield return new WaitForSeconds(2.5f);

            if (selectionPanel) selectionPanel.SetActive(false);

            // MasterClient: DB 로드 확인 후 단어 설정
            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(StartRoundAfterDB());
        }

        private IEnumerator StartRoundAfterDB()
        {
            while (WordDatabase.Instance == null || !WordDatabase.Instance.IsLoaded)
                yield return null;

            int length = Random.Range(2, 7);
            var word   = WordDatabase.Instance.GetRandom(length);
            if (word == null)
            {
                Debug.LogError("[Multi] 단어 없음 length=" + length);
                yield break;
            }
            SetRoomProperties(word.id, 0, PhotonNetwork.Time);
        }

        private void RevealCard(bool isCard1, bool isFirst)
        {
            var img  = isCard1 ? card1Img  : card2Img;
            var text = isCard1 ? card1Text : card2Text;
            if (img)  img.color = isFirst ? ColCardFirst : ColCardSecond;
            if (text) text.text  = isFirst ? "선공" : "후공";
        }

        // ── Photon 콜백 ──────────────────────────────────────────────
        public override void OnRoomPropertiesUpdate(Hashtable changedProps)
        {
            // PropWordId와 PropTurn이 동시에 오는 경우:
            // DB 미로드 시 return 하면 PropTurn 처리가 통째로 건너뛰어지는 버그 수정
            if (changedProps.ContainsKey(PropWordId))
            {
                int wordId = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropWordId];
                if (WordDatabase.Instance == null || !WordDatabase.Instance.IsLoaded)
                    StartCoroutine(DelayedBuildAndSetup(wordId)); // return 없이 코루틴만 시작
                else
                    BuildWord(wordId);
            }

            if (changedProps.ContainsKey(PropTurn))
            {
                _turnIndex             = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropTurn];
                _turnStartTime         = (double)PhotonNetwork.CurrentRoom.CustomProperties[PropStartTime];
                _activePlayerSubmitted = false;
                _isRunning             = true;
                _prevTimerSeconds      = -1;

                // _answerTokens가 이미 있으면 즉시 턴 세팅, 없으면 DelayedBuildAndSetup에서 처리
                if (_answerTokens != null)
                    SetupTurn();
            }
        }

        private void SetupTurn()
        {
            submitController.Setup(_answerTokens, _turnIndex, IsMyTurn);
            if (turnIndicatorText)
                turnIndicatorText.text = IsMyTurn ? "내 차례" : "상대방 차례";
            RefreshHints();
        }

        private void RefreshHints()
        {
            if (_currentWord == null) return;

            // 각 플레이어 5턴씩(총 10턴) 이후: 글자 수 공개
            if (wordLengthHintText)
            {
                if (_turnIndex >= 10)
                {
                    wordLengthHintText.text = _currentWord.length + "글자";
                    wordLengthHintText.gameObject.SetActive(true);
                }
                else wordLengthHintText.gameObject.SetActive(false);
            }

            // 각 플레이어 20턴씩(총 40턴) 이후: 단어 힌트(설명) 공개
            if (autoHintText)
            {
                if (_turnIndex >= 40)
                {
                    string hintStr = string.IsNullOrEmpty(_currentWord.hint)
                        ? BuildFirstSyllableHint()
                        : _currentWord.hint;
                    autoHintText.text = "힌트: " + hintStr;
                    autoHintText.gameObject.SetActive(true);
                }
                else autoHintText.gameObject.SetActive(false);
            }
        }

        private string BuildFirstSyllableHint()
        {
            var tokens = JamoConverter.GetDisplayTokens(_currentWord.word);
            if (tokens == null || tokens.Count == 0) return "";
            var sb = new System.Text.StringBuilder(tokens[0]);
            for (int i = 1; i < tokens.Count; i++) sb.Append(" _");
            return sb.ToString();
        }

        private void BuildWord(int wordId)
        {
            _currentWord  = WordDatabase.Instance.GetById(wordId);
            if (_currentWord == null) { Debug.LogError("[Multi] wordId=" + wordId + " 없음"); return; }
            _answerTokens = JamoConverter.GetAnswerTokens(_currentWord.word);

            // 같은 단어면 토큰뷰를 재빌드하지 않음 → 잠금(LockPosition) 누적 유지 + 깜빡임 방지
            if (_lastBuiltWordId != wordId)
            {
                _lastBuiltWordId = wordId;
                var display = JamoConverter.GetDisplayTokens(_currentWord.word);
                // wordId를 시드로 사용 → 두 기기에서 동일한 셔플 결과 보장
                ShuffleList(display, wordId);
                tokenView.Build(display);
            }
        }

        // DB 로드 대기 후 단어 빌드 + 턴 세팅 (PropTurn보다 DB 로드가 늦을 때)
        private IEnumerator DelayedBuildAndSetup(int wordId)
        {
            while (WordDatabase.Instance == null || !WordDatabase.Instance.IsLoaded)
                yield return null;
            BuildWord(wordId);
            if (_isRunning) SetupTurn(); // PropTurn이 이미 처리됐으면 즉시 세팅
        }

        private void Update()
        {
            if (!_isRunning) return;

            double remaining = TurnDuration - (PhotonNetwork.Time - _turnStartTime);
            if (remaining < 0) remaining = 0;

            int seconds = (int)remaining;
            if (seconds != _prevTimerSeconds)
            {
                if (timerText) timerText.text = seconds.ToString();
                _prevTimerSeconds = seconds;
            }

            if (remaining <= 0) EndTurn();
        }

        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case MultiNetworkEvents.TurnOrderSet:
                    OnTurnOrderSet((object[])photonEvent.CustomData);
                    break;
                case MultiNetworkEvents.SubmitTurn:
                    OnSubmitReceived((object[])photonEvent.CustomData);
                    break;
                case MultiNetworkEvents.TurnEnded:
                    ProcessTurnResult();
                    break;
                case MultiNetworkEvents.GameOver:
                    ShowResult((int)((object[])photonEvent.CustomData)[0]);
                    break;
                case MultiNetworkEvents.CardTapped:
                    OnCardTappedReceived((int)((object[])photonEvent.CustomData)[0]);
                    break;
            }
        }

        private void OnTurnOrderSet(object[] data)
        {
            _turnActors    = new int[] { (int)data[0], (int)data[1] };
            _isMyTurnFirst = _turnActors[0] == PhotonNetwork.LocalPlayer.ActorNumber;
            StartCoroutine(WaitForTapOrAutoReveal());

            // PropTurn이 TurnOrderSet보다 먼저 도착한 경우 (race condition):
            // _turnActors가 null이어서 SetupTurn이 IsMyTurn=False로 잘못 셋업됐을 수 있음 → 재셋업
            if (_isRunning && _answerTokens != null)
                SetupTurn();
        }

        private void OnSubmitReceived(object[] rawData)
        {
            var submit = SubmitData.Deserialize(rawData);
            // 이미 처리된 제출이거나, 현재 턴과 다른 턴의 제출이거나, 다른 플레이어의 제출이면 무시
            // (_isRunning 체크 제거: 타이머 만료 직후 잠깐 False가 되는 race condition 때문에 제출 무시 버그 발생)
            if (_activePlayerSubmitted) return;
            if (submit.TurnIndex != _turnIndex) return;
            if (submit.ActorNumber != ActiveActorNumber) return;

            _activePlayerSubmitted = true;
            _isRunning             = false;

            bool isLocal = submit.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

            JudgeResult result = null;
            if (submit.InputWord != null && _answerTokens != null)
            {
                var inputTokens = JamoConverter.GetAnswerTokens(submit.InputWord);
                result = BaseballJudge.Judge(_answerTokens, inputTokens);

                for (int i = 0; i < result.Hits.Length; i++)
                    if (result.Hits[i] == TokenHit.Strike)
                        tokenView.LockPosition(i, _answerTokens[i],
                            JamoConverter.ToDisplayJamo(_answerTokens[i]));
            }

            historyView.OnSubmitReceived(submit, isLocal, result, _answerTokens?.Count ?? 0);

            if (submit.IsCorrect)
            {
                if (PhotonNetwork.IsMasterClient)
                    PhotonNetwork.RaiseEvent(
                        MultiNetworkEvents.GameOver, new object[] { submit.ActorNumber },
                        MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                    PhotonNetwork.RaiseEvent(
                        MultiNetworkEvents.TurnEnded, null,
                        MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
            }
        }

        private void EndTurn()
        {
            if (!_isRunning) return;
            _isRunning = false;
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.RaiseEvent(
                    MultiNetworkEvents.TurnEnded, null,
                    MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
        }

        private void ProcessTurnResult()
        {
            if (!_activePlayerSubmitted)
                historyView.AddSkipped(IsMyTurn);
            if (PhotonNetwork.IsMasterClient)
            {
                int wordId = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropWordId];
                SetRoomProperties(wordId, _turnIndex + 1, PhotonNetwork.Time);
            }
        }

        private void ShowResult(int winnerActorNumber)
        {
            _isRunning = false;
            bool isWin = winnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

            var save = SaveManager.LoadMulti();
            save.PlayCount++;
            if (isWin) save.WinCount++;
            else       save.LoseCount++;
            SaveManager.SaveMulti(save);

            string correctWord = _currentWord?.word ?? "";
            resultScreen.Show(isWin, _players, correctWord, _turnIndex + 1, save.WinCount, save.LoseCount);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
            => ShowResult(PhotonNetwork.LocalPlayer.ActorNumber);

        private void SetRoomProperties(int wordId, int turn, double startTime)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { PropWordId,    wordId    },
                { PropTurn,      turn      },
                { PropStartTime, startTime },
            });
        }

        // seed로 wordId를 사용 → 두 기기가 동일한 순서로 셔플됨 (UnityEngine.Random 사용 금지)
        private static void ShuffleList(List<string> list, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
#endif
