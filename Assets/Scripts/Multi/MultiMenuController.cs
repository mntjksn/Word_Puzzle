#if PHOTON_UNITY_NETWORKING
using System.Text;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WordPuzzle.Multi
{
    public class MultiMenuController : MonoBehaviour
    {
        [Header("패널")]
        [SerializeField] private GameObject connectingPanel;
        [SerializeField] private GameObject menuPanel;

        [Header("입력")]
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField roomCodeInput;

        [Header("버튼")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;

        [Header("상태 텍스트")]
        [SerializeField] private TextMeshProUGUI statusText;

        private PhotonMultiplayerManager _mgr;

        private void Start()
        {
            if (backButton)   backButton.onClick.AddListener(OnBack);
            if (createButton) createButton.onClick.AddListener(OnCreateRoom);
            if (joinButton)   joinButton.onClick.AddListener(OnJoinRoom);

            ShowConnecting(true);

            _mgr = PhotonMultiplayerManager.Instance;
            if (_mgr == null)
            {
                Debug.LogError("[MultiMenu] PhotonMultiplayerManager 없음");
                return;
            }

            _mgr.OnConnectedCallback        = OnConnected;
            _mgr.OnConnectionFailedCallback = msg => SetStatus("연결 실패: " + msg);
            _mgr.OnJoinedRoomCallback       = () => SceneManager.LoadScene("MultiRoom");
            _mgr.OnJoinFailedCallback       = msg => { SetStatus("실패: " + msg); SetButtons(true); };

            if (PhotonNetwork.IsConnected)
                OnConnected();
            else
                _mgr.Connect();
        }

        private void OnDestroy()
        {
            if (_mgr == null) return;
            _mgr.OnConnectedCallback        = null;
            _mgr.OnConnectionFailedCallback = null;
            _mgr.OnJoinedRoomCallback       = null;
            _mgr.OnJoinFailedCallback       = null;
        }

        // ── 버튼 핸들러 ────────────────────────────────────────────────────

        public void OnCreateRoom()
        {
            ApplyNickname();
            string code = GenerateCode();
            SetStatus("방 생성 중...");
            SetButtons(false);
            _mgr.CreateRoom(code);
        }

        public void OnJoinRoom()
        {
            string code = roomCodeInput != null ? roomCodeInput.text.Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(code)) { SetStatus("방 코드를 입력해주세요."); return; }
            ApplyNickname();
            SetStatus("방 입장 중...");
            SetButtons(false);
            _mgr.JoinRoom(code);
        }

        public void OnBack() => SceneManager.LoadScene("Intro");

        // ── 내부 헬퍼 ──────────────────────────────────────────────────────

        private void OnConnected()
        {
            ShowConnecting(false);
            SetStatus("");
        }

        private void ShowConnecting(bool show)
        {
            if (connectingPanel) connectingPanel.SetActive(show);
            if (menuPanel)       menuPanel.SetActive(!show);
        }

        private void SetStatus(string msg)
        {
            if (statusText) statusText.text = msg;
        }

        private void SetButtons(bool interactable)
        {
            if (createButton) createButton.interactable = interactable;
            if (joinButton)   joinButton.interactable   = interactable;
        }

        private void ApplyNickname()
        {
            string nick = nicknameInput != null ? nicknameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(nick))
                nick = "플레이어" + Random.Range(100, 999);
            _mgr.SetNickname(nick);
        }

        private static string GenerateCode()
        {
            const string pool = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var sb  = new StringBuilder(6);
            var rng = new System.Random();
            for (int i = 0; i < 6; i++) sb.Append(pool[rng.Next(pool.Length)]);
            return sb.ToString();
        }
    }
}
#endif
