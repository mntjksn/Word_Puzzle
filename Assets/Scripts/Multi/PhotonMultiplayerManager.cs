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
            {
                // BestRegion은 기기마다 다른 서버(jp/kr 등)를 선택해 방 입장 실패를 유발
                // 모든 기기를 kr 서버로 고정
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";
                PhotonNetwork.ConnectUsingSettings();
            }
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
            // 초기 연결 및 방 나간 후 복귀 시 모두 호출됨
            // JoinRoom/CreateRoom은 ConnectedToMaster 상태에서 바로 가능 → JoinLobby 불필요
            Debug.Log($"[Photon] 마스터 서버 연결 | 지역:{PhotonNetwork.CloudRegion}");
            OnConnectedCallback?.Invoke();
        }

        // 방 나가기 완료 후 자동으로 OnConnectedToMaster 호출됨
        public override void OnLeftRoom()
            => Debug.Log("[Photon] 방 나가기 완료");

        public override void OnJoinedRoom()
        {
            Debug.Log($"[Photon] 방 입장: {PhotonNetwork.CurrentRoom.Name} | 지역:{PhotonNetwork.CloudRegion}");
            OnJoinedRoomCallback?.Invoke();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
            => OnPlayerJoinedCallback?.Invoke(newPlayer);

        public override void OnPlayerLeftRoom(Player otherPlayer)
            => OnPlayerLeftCallback?.Invoke(otherPlayer);

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[Photon] 방 생성 실패 ({returnCode}): {message} | 지역:{PhotonNetwork.CloudRegion}");
            OnJoinFailedCallback?.Invoke(message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[Photon] 방 입장 실패 ({returnCode}): {message} | 지역:{PhotonNetwork.CloudRegion}");
            OnJoinFailedCallback?.Invoke($"{message}\n[지역:{PhotonNetwork.CloudRegion}]");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[Photon] 연결 끊김: {cause}");
            OnConnectionFailedCallback?.Invoke(cause.ToString());
        }
    }
}
#endif
