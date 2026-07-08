using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Firebase;
using WordPuzzle.Save;
using DataSnapshot = global::Firebase.Database.DataSnapshot;

namespace WordPuzzle.UI
{
    public class ProfilePopup : MonoBehaviour
    {
        [Header("닉네임")]
        [SerializeField] private TextMeshProUGUI currentNicknameText;
        [SerializeField] private Button          changeNicknameButton;
        [SerializeField] private GameObject      editNicknameGroup;
        [SerializeField] private TMP_InputField  nicknameInput;
        [SerializeField] private Button          setNicknameButton;
        [SerializeField] private Button          cancelEditButton;
        [SerializeField] private TextMeshProUGUI nickStatusText;

        [Header("멀티 전적")]
        [SerializeField] private TextMeshProUGUI winText;
        [SerializeField] private TextMeshProUGUI loseText;
        [SerializeField] private TextMeshProUGUI winRateText;

        [Header("단어 클리어")]
        [SerializeField] private TextMeshProUGUI clear2Text;
        [SerializeField] private TextMeshProUGUI clear3Text;
        [SerializeField] private TextMeshProUGUI clear4Text;
        [SerializeField] private TextMeshProUGUI clear5Text;
        [SerializeField] private TextMeshProUGUI clear6Text;

        [Header("일일 도전")]
        [SerializeField] private TextMeshProUGUI dailyClearCountText;
        [SerializeField] private TextMeshProUGUI dailyStreakText;

        [Header("공통")]
        [SerializeField] private Button closeButton;

        private static readonly Color ColOk  = new Color(0.2f, 0.9f, 0.6f, 1f);
        private static readonly Color ColErr = new Color(1f,   0.3f, 0.3f, 1f);
        private static readonly Color ColInfo= new Color(0.7f, 0.85f, 1f,  0.85f);

        private void Start()
        {
            if (closeButton)         closeButton.onClick.AddListener(Hide);
            if (setNicknameButton)   setNicknameButton.onClick.AddListener(OnSetNicknameClicked);
            if (changeNicknameButton)changeNicknameButton.onClick.AddListener(ShowEditPanel);
            if (cancelEditButton)    cancelEditButton.onClick.AddListener(HideEditPanel);
            HideEditPanel();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            HideEditPanel();
            SetStatus("", ColInfo);
            RefreshLocal();

            if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady)
                FirebaseManager.Instance.LoadUserData(ApplyFirebaseData);
        }

        public void Hide() => gameObject.SetActive(false);

        // ── 로컬 데이터 즉시 표시 ──────────────────────────────────────────

        private void ShowEditPanel()
        {
            if (editNicknameGroup)    editNicknameGroup.SetActive(true);
            if (cancelEditButton)     cancelEditButton.gameObject.SetActive(true);
            if (changeNicknameButton) changeNicknameButton.gameObject.SetActive(false);
            if (nicknameInput)        nicknameInput.SetTextWithoutNotify(PlayerPrefs.GetString(SettingsPopup.NicknameKey, ""));
            SetStatus("", ColInfo);
        }

        private void HideEditPanel()
        {
            if (editNicknameGroup)    editNicknameGroup.SetActive(false);
            if (cancelEditButton)     cancelEditButton.gameObject.SetActive(false);
            if (changeNicknameButton) changeNicknameButton.gameObject.SetActive(true);
            SetStatus("", ColInfo);
        }

        private void RefreshLocal()
        {
            string nick = PlayerPrefs.GetString(SettingsPopup.NicknameKey, "");
            if (currentNicknameText)
                currentNicknameText.text = string.IsNullOrEmpty(nick) ? "닉네임 없음" : nick;

            var multi  = SaveManager.LoadMulti();
            var single = SaveManager.LoadSingle();
            var daily  = SaveManager.LoadDaily();
            ApplyStats(multi.WinCount, multi.LoseCount, single.ClearCountByLength,
                       daily.TotalDailyClearCount, daily.StreakDays);
        }

        // ── Firebase 데이터 반영 ───────────────────────────────────────────

        private void ApplyFirebaseData(DataSnapshot snap)
        {
            if (snap == null) return;

            if (snap.HasChild("nickname"))
            {
                string nick = snap.Child("nickname").Value?.ToString() ?? "";
                PlayerPrefs.SetString(SettingsPopup.NicknameKey, nick);
                if (currentNicknameText) currentNicknameText.text = string.IsNullOrEmpty(nick) ? "닉네임 없음" : nick;
                if (nicknameInput) nicknameInput.SetTextWithoutNotify(nick);
            }

            int win  = ParseInt(snap, "multi/win");
            int lose = ParseInt(snap, "multi/lose");

            int[] clears = new int[7];
            for (int len = 2; len <= 6; len++)
                clears[len] = ParseInt(snap, $"single/clearByLength/{len}");

            int dailyClearCount = ParseInt(snap, "daily/totalDailyClearCount");
            int streakDays      = ParseInt(snap, "daily/streakDays");

            ApplyStats(win, lose, clears, dailyClearCount, streakDays);
        }

        // ── 닉네임 설정 버튼 ─────────────────────────────────────────────

        private void OnSetNicknameClicked()
        {
            string nick = nicknameInput != null ? nicknameInput.text.Trim() : "";

            if (string.IsNullOrEmpty(nick))
            {
                SetStatus("닉네임을 입력해주세요.", ColErr);
                return;
            }
            if (nick.Length < 2)
            {
                SetStatus("2글자 이상 입력해주세요.", ColErr);
                return;
            }

            var fb = FirebaseManager.Instance;
            if (fb == null || !fb.IsReady)
            {
                SaveNickLocal(nick);
                SetStatus("저장 완료!", ColOk);
                if (currentNicknameText) currentNicknameText.text = nick;
                Invoke(nameof(HideEditPanel), 0.8f);
                return;
            }

            SetStatus("확인 중...", ColInfo);
            SetNickButton(false);

            fb.CheckAndSaveNickname(nick, (success, msg) =>
            {
                SetNickButton(true);
                if (success)
                {
                    SaveNickLocal(nick);
                    SetStatus("닉네임이 설정됐습니다!", ColOk);
                    if (currentNicknameText) currentNicknameText.text = nick;
                    Invoke(nameof(HideEditPanel), 1.0f);
                }
                else
                {
                    SetStatus(msg, ColErr);
                }
            });
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────────

        private void ApplyStats(int win, int lose, int[] clears, int dailyClearCount, int streakDays)
        {
            if (winText)  winText.text  = win.ToString();
            if (loseText) loseText.text = lose.ToString();

            int total = win + lose;
            float rate = total > 0 ? (float)win / total * 100f : 0f;
            if (winRateText) winRateText.text = $"{rate:F0}%";

            SetClear(clear2Text, clears, 2);
            SetClear(clear3Text, clears, 3);
            SetClear(clear4Text, clears, 4);
            SetClear(clear5Text, clears, 5);
            SetClear(clear6Text, clears, 6);

            if (dailyClearCountText) dailyClearCountText.text = dailyClearCount.ToString();
            if (dailyStreakText)     dailyStreakText.text      = streakDays.ToString();
        }

        private static void SetClear(TextMeshProUGUI t, int[] arr, int idx)
        {
            if (t != null) t.text = (arr != null && arr.Length > idx ? arr[idx] : 0).ToString();
        }

        private static void SaveNickLocal(string nick)
        {
            PlayerPrefs.SetString(SettingsPopup.NicknameKey, nick);
            PlayerPrefs.Save();
        }

        private void SetStatus(string msg, Color col)
        {
            if (nickStatusText == null) return;
            nickStatusText.text  = msg;
            nickStatusText.color = col;
        }

        private void SetNickButton(bool on)
        {
            if (setNicknameButton) setNicknameButton.interactable = on;
        }

        private static int ParseInt(DataSnapshot snap, string path)
        {
            var node = snap;
            foreach (string key in path.Split('/'))
            {
                if (!node.HasChild(key)) return 0;
                node = node.Child(key);
            }
            return int.TryParse(node.Value?.ToString(), out int v) ? v : 0;
        }
    }
}
