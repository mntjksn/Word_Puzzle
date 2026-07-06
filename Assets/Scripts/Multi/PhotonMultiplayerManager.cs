#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace WordPuzzle.Multi
{
    // Photon 서버 연결, 방 생성/입장/퇴장 관리
    public class PhotonMultiplayerManager : MonoBehaviourPunCallbacks
    {
        public static PhotonMultiplayerManager Instance { get; private set; }

        private const string GameVersion = "1.0";
        private const int    MaxPlayers  = 2;

        public bool IsConnected => PhotonNetwork.IsConnected;

        public System.Action          OnConnectedCallback;
        public System.Action<string>  OnConnectionFailedCallback;
        public System.Action          OnJoinedRoomCallback;
        public System.Action<Player>  OnPlayerJoinedCallback;
        public System.Action<Player>  OnPlayerLeftCallback;
        public System.Action<string>  OnJoinFailedCallback;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            PhotonNetwork.GameVersion                = GameVersion;
            PhotonNetwork.AutomaticallySyncScene     = true;
            Connect();
        }

        public void Connect()
        {
            if (!PhotonNetwork.IsConnected)
                PhotonNetwork.ConnectUsingSettings();
        }

        public void SetNickname(string nickname)
            => PhotonNetwork.NickName = nickname;

        // 방 생성 (방 이름 = 초대 코드)
        public void CreateRoom(string roomCode)
        {
            var options = new RoomOptions
            {
                MaxPlayers = MaxPlayers,
                IsVisible  = false,
                IsOpen     = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "state", "Waiting" }
                },
                CustomRoomPropertiesForLobby = new[] { "state" }
            };
            PhotonNetwork.CreateRoom(roomCode, options);
        }

        public void JoinRoom(string roomCode)
            => PhotonNetwork.JoinRoom(roomCode);

        public void LeaveRoom()
            => PhotonNetwork.LeaveRoom();

        // --- Photon 콜백 ---

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
            OnConnectedCallback?.Invoke();
        }

        public override void OnJoinedLobby()
            => Debug.Log("[Photon] 로비 입장 완료");

        public override void OnJoinedRoom()
        {
            Debug.Log($"[Photon] 방 입장: {PhotonNetwork.CurrentRoom.Name}");
            OnJoinedRoomCallback?.Invoke();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
            => OnPlayerJoinedCallback?.Invoke(newPlayer);

        public override void OnPlayerLeftRoom(Player otherPlayer)
            => OnPlayerLeftCallback?.Invoke(otherPlayer);

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[Photon] 방 생성 실패: {message}");
            OnJoinFailedCallback?.Invoke(message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[Photon] 방 입장 실패: {message}");
            OnJoinFailedCallback?.Invoke(message);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[Photon] 연결 끊김: {cause}");
            OnConnectionFailedCallback?.Invoke(cause.ToString());
        }
    }
}
#endif
