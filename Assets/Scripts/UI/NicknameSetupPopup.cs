using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Firebase;

namespace WordPuzzle.UI
{
    // 최초 접속 시 닉네임 설정 팝업
    public class NicknameSetupPopup : MonoBehaviour
    {
        [SerializeField] private TMP_InputField  inputField;
        [SerializeField] private Button          confirmButton;
        [SerializeField] private Button          skipButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private static readonly Color ColOk   = new Color(0.2f,  0.9f,  0.6f,  1f);
        private static readonly Color ColErr  = new Color(1f,    0.3f,  0.3f,  1f);
        private static readonly Color ColInfo = new Color(0.7f,  0.85f, 1f,    0.85f);

        public static bool NeedsNickname()
        {
            string nick = PlayerPrefs.GetString(SettingsPopup.NicknameKey, "");
            return string.IsNullOrEmpty(nick);
        }

        private void Start()
        {
            if (confirmButton) confirmButton.onClick.AddListener(OnConfirm);
            if (skipButton)    skipButton.onClick.AddListener(OnSkip);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (inputField) inputField.text = "";
            SetStatus("", Color.clear);
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnConfirm()
        {
            string nick = inputField != null ? inputField.text.Trim() : "";
            if (string.IsNullOrEmpty(nick))       { SetStatus("닉네임을 입력해주세요.", ColErr); return; }
            if (nick.Length < 2)                  { SetStatus("2글자 이상 입력해주세요.", ColErr); return; }

            var fb = FirebaseManager.Instance;
            if (fb == null || !fb.IsReady)
            {
                SaveNickLocal(nick);
                Hide();
                return;
            }

            SetStatus("확인 중...", ColInfo);
            if (confirmButton) confirmButton.interactable = false;

            fb.CheckAndSaveNickname(nick, (success, msg) =>
            {
                if (confirmButton) confirmButton.interactable = true;
                if (success) { SaveNickLocal(nick); Hide(); }
                else         SetStatus(msg, ColErr);
            });
        }

        private void OnSkip() => Hide();

        private static void SaveNickLocal(string nick)
        {
            PlayerPrefs.SetString(SettingsPopup.NicknameKey, nick);
            PlayerPrefs.Save();
        }

        private void SetStatus(string msg, Color col)
        {
            if (statusText == null) return;
            statusText.text  = msg;
            statusText.color = col;
        }
    }
}
