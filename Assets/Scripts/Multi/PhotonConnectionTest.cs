#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace WordPuzzle.Multi
{
    // Photon 연결 상태를 화면에 표시하는 테스트용 컴포넌트
    public class PhotonConnectionTest : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TextMeshProUGUI statusText;

        private void Start()
        {
            UpdateStatus("서버 연결 중...");
            Debug.Log("[ConnectionTest] Photon 연결 시작");
        }

        public override void OnConnectedToMaster()
        {
            UpdateStatus($"마스터 서버 연결 성공\n리전: {PhotonNetwork.CloudRegion}\n핑: {PhotonNetwork.GetPing()}ms");
            Debug.Log($"[ConnectionTest] 마스터 서버 연결 성공 | 리전: {PhotonNetwork.CloudRegion} | 핑: {PhotonNetwork.GetPing()}ms");
        }

        public override void OnJoinedLobby()
        {
            UpdateStatus($"로비 입장 완료\nNickName: {PhotonNetwork.NickName}\nSDK: {PhotonNetwork.PunVersion}");
            Debug.Log("[ConnectionTest] 로비 입장 완료 → 연결 테스트 통과");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            UpdateStatus($"연결 실패: {cause}");
            Debug.LogWarning($"[ConnectionTest] 연결 실패: {cause}");
        }

        private void UpdateStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }
    }
}
#endif
