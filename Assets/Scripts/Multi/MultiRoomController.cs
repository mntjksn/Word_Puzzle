#if PHOTON_UNITY_NETWORKING
using ExitGames.Client.Photon;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.Multi
{
    // 대기방 상태 관리: 초대 코드 표시, 준비 버튼, 게임 시작 요청
    public class MultiRoomController : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        [Header("UI 참조")]
        [SerializeField] private TextMeshProUGUI roomCodeText;
        [SerializeField] private TextMeshProUGUI player1Text;
        [SerializeField] private TextMeshProUGUI player1StatsText;
        [SerializeField] private TextMeshProUGUI player1ReadyText;
        [SerializeField] private UnityEngine.UI.Image player1SlotImg;
        [SerializeField] private TextMeshProUGUI player2Text;
        [SerializeField] private TextMeshProUGUI player2StatsText;
        [SerializeField] private TextMeshProUGUI player2ReadyText;
        [SerializeField] private UnityEngine.UI.Image player2SlotImg;
        [SerializeField] private Button          readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button          copyCodeButton;
        [SerializeField] private Button          leaveButton;

        private const string PropWins   = "wins";
        private const string PropLosses = "losses";

        private static readonly Color ColSlotNormal = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color ColSlotReady  = new Color(0.28f, 0.62f, 1f, 0.28f);
        private static readonly Color ColReadyText  = new Color(0.45f, 0.78f, 1f, 1f);

        private readonly System.Collections.Generic.Dictionary<int, bool> _readyStates
            = new System.Collections.Generic.Dictionary<int, bool>();

        private bool _isReady;

        private void Start()
        {
            if (copyCodeButton) copyCodeButton.onClick.AddListener(OnCopyCode);
            if (readyButton)    readyButton.onClick.AddListener(OnReadyButtonClicked);
            if (leaveButton)    leaveButton.onClick.AddListener(OnLeaveRoom);
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
            _readyStates.Clear();

            // 내 전적을 커스텀 프로퍼티로 공유
            var multi = WordPuzzle.Save.SaveManager.LoadMulti();
            var props = new Hashtable { [PropWins] = multi.WinCount, [PropLosses] = multi.LoseCount };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            RefreshUI();
        }

        private void OnDisable()
            => PhotonNetwork.RemoveCallbackTarget(this);

        private void RefreshUI()
        {
            if (!PhotonNetwork.InRoom) return;

            roomCodeText.text = $"초대 코드: {PhotonNetwork.CurrentRoom.Name}";

            int idx = 0;
            foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
            {
                bool ready = _readyStates.TryGetValue(p.ActorNumber, out bool r) && r;
                if (idx == 0) ApplySlot(player1Text, player1StatsText, player1ReadyText, player1SlotImg, p, ready);
                else          ApplySlot(player2Text, player2StatsText, player2ReadyText, player2SlotImg, p, ready);
                idx++;
            }
            if (idx < 2)
            {
                player2Text.text = "대기 중...";
                if (player2StatsText) { player2StatsText.gameObject.SetActive(false); player2StatsText.text = ""; }
                if (player2ReadyText) { player2ReadyText.gameObject.SetActive(false); player2ReadyText.text = ""; }
                if (player2SlotImg)   player2SlotImg.color = ColSlotNormal;
            }
        }

        private void ApplySlot(TextMeshProUGUI nameT, TextMeshProUGUI statsT,
                                TextMeshProUGUI readyT, UnityEngine.UI.Image slotImg,
                                Player p, bool ready)
        {
            if (nameT)  nameT.text  = p.NickName;
            if (statsT) { statsT.gameObject.SetActive(true); statsT.text = GetStatsStr(p); }
            if (readyT)
            {
                readyT.gameObject.SetActive(true);
                readyT.text  = ready ? "준비완료" : "";
                readyT.color = ColReadyText;
            }
            if (slotImg) slotImg.color = ready ? ColSlotReady : ColSlotNormal;
        }

        private static string GetStatsStr(Player p)
        {
            var props = p.CustomProperties;
            int win   = props.ContainsKey("wins")   ? (int)props["wins"]   : 0;
            int lose  = props.ContainsKey("losses") ? (int)props["losses"] : 0;
            int total = win + lose;
            float rate = total > 0 ? (float)win / total * 100f : 0f;
            return $"{win}/{total} 승률 {rate:F0}%";
        }

        // 초대 코드 클립보드 복사
        public void OnCopyCode()
        {
            GUIUtility.systemCopyBuffer = PhotonNetwork.CurrentRoom.Name;
            statusText.text = "코드가 복사되었습니다!";
        }

        // 준비 버튼 토글
        public void OnReadyButtonClicked()
        {
            _isReady             = !_isReady;
            readyButtonText.text = _isReady ? "준비 취소" : "준비";

            _readyStates[PhotonNetwork.LocalPlayer.ActorNumber] = _isReady;
            RefreshUI();

            var data = new object[] { PhotonNetwork.LocalPlayer.ActorNumber, _isReady };
            PhotonNetwork.RaiseEvent(
                MultiNetworkEvents.PlayerReadyChanged, data,
                MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
        }

        public void OnLeaveRoom()
        {
            PhotonMultiplayerManager.Instance?.LeaveRoom();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MultiMenu");
        }

        // Photon 이벤트 수신
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == MultiNetworkEvents.PlayerReadyChanged)
            {
                var data = (object[])photonEvent.CustomData;
                int actorNum = (int)data[0];
                bool ready   = (bool)data[1];
                _readyStates[actorNum] = ready;
                RefreshUI();

                if (PhotonNetwork.IsMasterClient)
                    CheckAllReady();
            }
            else if (photonEvent.Code == MultiNetworkEvents.StartGame)
            {
                FindObjectOfType<MultiGameController>()?.StartGame();
            }
        }

        private void CheckAllReady()
        {
            // 2명이 방에 있으면 게임 시작 (추후 Custom Player Properties로 ready 확인 가능)
            if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return;

            PhotonNetwork.RaiseEvent(
                MultiNetworkEvents.StartGame, null,
                MultiNetworkEvents.All, MultiNetworkEvents.Reliable);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)        => RefreshUI();
        public override void OnPlayerLeftRoom(Player otherPlayer)         => RefreshUI();
        public override void OnPlayerPropertiesUpdate(Player target, Hashtable props) => RefreshUI();
    }
}
#endif
