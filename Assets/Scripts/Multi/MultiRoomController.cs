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
        [SerializeField] private TextMeshProUGUI player2Text;
        [SerializeField] private Button          readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button          copyCodeButton;
        [SerializeField] private Button          leaveButton;

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
                if (idx == 0) player1Text.text = p.NickName;
                else          player2Text.text = p.NickName;
                idx++;
            }
            if (idx < 2) player2Text.text = "대기 중...";
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

        public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshUI();
        public override void OnPlayerLeftRoom(Player otherPlayer)  => RefreshUI();
    }
}
#endif
