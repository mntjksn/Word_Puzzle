using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Firebase;

namespace WordPuzzle.UI
{
    public class AccountRestorePopup : MonoBehaviour
    {
        [SerializeField] private TMP_InputField  nicknameInput;
        [SerializeField] private Button          restoreButton;
        [SerializeField] private Button          newUserButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button          closeButton;

        private static readonly Color ColOk   = new Color(0.2f, 0.9f, 0.6f, 1f);
        private static readonly Color ColErr  = new Color(1f,   0.3f, 0.3f, 1f);
        private static readonly Color ColInfo = new Color(0.7f, 0.85f, 1f,  0.85f);

        private void OnEnable()
        {
            FirebaseManager.OnNewUserDetected += Show;
        }

        private void OnDisable()
        {
            FirebaseManager.OnNewUserDetected -= Show;
        }

        private void Start()
        {
            if (restoreButton) restoreButton.onClick.AddListener(OnRestoreClicked);
            if (newUserButton) newUserButton.onClick.AddListener(OnNewUserClicked);
            if (closeButton)   closeButton.onClick.AddListener(OnNewUserClicked);

            // Firebase가 이미 신규 유저를 감지했으면 즉시 표시
            if (FirebaseManager.NewUserDetected) Show();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SetStatus("", ColInfo);
            if (nicknameInput) nicknameInput.SetTextWithoutNotify("");
        }

        private void Hide() => gameObject.SetActive(false);

        private void OnRestoreClicked()
        {
            string nick = nicknameInput != null ? nicknameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(nick) || nick.Length < 2)
            {
                SetStatus("2글자 이상 입력해주세요.", ColErr);
                return;
            }

            var fb = FirebaseManager.Instance;
            if (fb == null || !fb.IsReady)
            {
                SetStatus("서버 연결 중입니다. 잠시 후 다시 시도해주세요.", ColErr);
                return;
            }

            SetStatus("복구 중...", ColInfo);
            SetButtons(false);

            fb.CheckAndRestoreAccount(nick, (success, msg) =>
            {
                SetButtons(true);
                if (success)
                {
                    SetStatus("복구 완료!", ColOk);
                    Invoke(nameof(Hide), 1.2f);
                }
                else
                {
                    SetStatus(msg, ColErr);
                }
            });
        }

        private void OnNewUserClicked()
        {
            FirebaseManager.Instance?.ConfirmNewUser();
            Hide();
        }

        private void SetStatus(string msg, Color col)
        {
            if (statusText == null) return;
            statusText.text  = msg;
            statusText.color = col;
        }

        private void SetButtons(bool on)
        {
            if (restoreButton) restoreButton.interactable = on;
            if (newUserButton) newUserButton.interactable = on;
        }
    }
}
