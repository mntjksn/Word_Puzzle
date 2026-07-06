#if PHOTON_UNITY_NETWORKING
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using WordPuzzle.Core;
using WordPuzzle.Data;
using WordPuzzle.Save;
using WordPuzzle.UI;

namespace WordPuzzle.Multi
{
    // 멀티 게임 흐름: 턴 시작/종료, 타이머, 승패 판정
    public class MultiGameController : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        [Header("UI 참조")]
        [SerializeField] private TokenView           tokenView;
        [SerializeField] private TextMeshProUGUI     turnText;
        [SerializeField] private TextMeshProUGUI     timerText;
        [SerializeField] private GameObject          gamePanel;
        [SerializeField] private MultiResultScreen   resultScreen;
        [SerializeField] private MultiSubmitController submitController;
        [SerializeField] private MultiResultHistoryView historyView;

        private const string PropState     = "state";
        private const string PropWordId    = "wordId";
        private const string PropTurn      = "turnIndex";
        private const string PropStartTime = "turnStartTime";
        private const double TurnDuration  = 60.0;

        private WordData     _currentWord;
        private List<string> _answerTokens;
        private int          _turnIndex;
        private double       _turnStartTime;
        private bool         _isRunning;
        private int          _prevTimerSeconds = -1;

        private Dictionary<int, MultiPlayerState> _players = new Dictionary<int, MultiPlayerState>();

        private void OnEnable()  => PhotonNetwork.AddCallbackTarget(this);
        private void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

        // MultiRoomController의 StartGame 이벤트 수신 후 호출
        public void StartGame()
        {
            gamePanel.SetActive(true);
            historyView.Clear();
            submitController.ResetForGame();

            // 플레이어 상태 초기화
            _players.Clear();
            foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
                _players[p.ActorNumber] = new MultiPlayerState(p.ActorNumber, p.NickName);

            // MasterClient가 wordId 선택 후 Room Properties에 저장
            if (PhotonNetwork.IsMasterClient)
            {
                var word = WordDatabase.Instance.GetRandom(3);
                SetRoomProperties(word.id, 0, PhotonNetwork.Time);
            }
        }

        // Room Custom Properties로 문제/턴 동기화
        public override void OnRoomPropertiesUpdate(Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PropWordId))
            {
                int wordId    = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropWordId];
                _currentWord  = WordDatabase.Instance.GetById(wordId);
                _answerTokens = JamoConverter.GetAnswerTokens(_currentWord.word);
                tokenView.Build(JamoConverter.GetDisplayTokens(_currentWord.word));
            }

            if (changedProps.ContainsKey(PropTurn))
            {
                _turnIndex     = (int)PhotonNetwork.CurrentRoom.CustomProperties[PropTurn];
                _turnStartTime = (double)PhotonNetwork.CurrentRoom.CustomProperties[PropStartTime];
                turnText.text  = $"턴 {_turnIndex + 1}";

                foreach (var ps in _players.Values) ps.ResetTurn();

                submitController.Setup(_answerTokens, _turnIndex);
                _isRunning        = true;
                _prevTimerSeconds = -1;
            }
        }

        private void Update()
        {
            if (!_isRunning) return;

            double remaining = TurnDuration - (PhotonNetwork.Time - _turnStartTime);
            if (remaining < 0) remaining = 0;

            // 타이머 UI는 초 단위 변화가 있을 때만 갱신
            int seconds = (int)remaining;
            if (seconds != _prevTimerSeconds)
            {
                timerText.text    = seconds.ToString();
                _prevTimerSeconds = seconds;
            }

            if (remaining <= 0)
                EndTurn();
        }

        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case MultiNetworkEvents.SubmitTurn:
                    OnSubmitReceived((object[])photonEvent.CustomData, photonEvent.Sender);
                    break;
                case MultiNetworkEvents.TurnEnded:
                    ProcessTurnResult();
                    break;
                case MultiNetworkEvents.GameOver:
                    var gameOverData  = (object[])photonEvent.CustomData;
                    ShowResult((int)gameOverData[0]);
                    break;
            }
        }

        private void OnSubmitReceived(object[] rawData, int senderActor)
        {
            var submit = SubmitData.Deserialize(rawData);
            if (!_players.TryGetValue(submit.ActorNumber, out var ps)) return;

            ps.HasSubmitted    = true;
            ps.LastSubmitData  = submit;

            bool isLocal = submit.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
            historyView.OnSubmitReceived(submit, isLocal);

            // 두 명 모두 제출 시 즉시 턴 종료
            bool allSubmitted = true;
            foreach (var p in _players.Values)
                if (!p.HasSubmitted) { allSubmitted = false; break; }

            if (allSubmitted) EndTurn();
        }

        private void EndTurn()
        {
            if (!_isRunning) return;
            _isRunning = false;

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.RaiseEvent(
                    MultiNetworkEvents.TurnEnded, null,
                    MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
            }
        }

        private void ProcessTurnResult()
        {
            MultiPlayerState winner = null;
            foreach (var ps in _players.Values)
            {
                if (ps.LastSubmitData?.IsCorrect != true) continue;

                if (winner == null)
                {
                    winner = ps;
                }
                else
                {
                    // 둘 다 정답이면 더 빠른 제출 시간 승리
                    if (ps.LastSubmitData.SubmitTime < winner.LastSubmitData.SubmitTime)
                        winner = ps;
                }
            }

            if (winner != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.RaiseEvent(
                        MultiNetworkEvents.GameOver, new object[] { winner.ActorNumber },
                        MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
                }
            }
            else
            {
                StartCoroutine(NextTurnDelayed());
            }
        }

        private IEnumerator NextTurnDelayed()
        {
            yield return new WaitForSeconds(2f);
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

            var saveData = SaveManager.LoadMulti();
            saveData.PlayCount++;
            if (isWin) saveData.WinCount++;
            else       saveData.LoseCount++;
            SaveManager.SaveMulti(saveData);

            resultScreen.Show(isWin ? "승리!" : "패배", _players);
        }

        // 상대 퇴장 시 자동 승리
        public override void OnPlayerLeftRoom(Player otherPlayer)
            => ShowResult(PhotonNetwork.LocalPlayer.ActorNumber);

        private void SetRoomProperties(int wordId, int turn, double startTime)
        {
            var props = new Hashtable
            {
                { PropState,     "Playing" },
                { PropWordId,    wordId    },
                { PropTurn,      turn      },
                { PropStartTime, startTime },
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
}
#endif
