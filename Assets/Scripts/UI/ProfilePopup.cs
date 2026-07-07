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
        [SerializeField] private TMP_InputField nicknameInput;

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

        [Header("공통")]
        [SerializeField] private Button closeButton;

        private void Start()
        {
            if (closeButton)   closeButton.onClick.AddListener(Hide);
            if (nicknameInput) nicknameInput.onEndEdit.AddListener(OnNicknameChanged);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshLocal();

            // Firebase 로드 후 덮어쓰기
            if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady)
                FirebaseManager.Instance.LoadUserData(ApplyFirebaseData);
        }

        public void Hide() => gameObject.SetActive(false);

        // 로컬 데이터로 즉시 표시
        private void RefreshLocal()
        {
            if (nicknameInput)
                nicknameInput.SetTextWithoutNotify(
                    PlayerPrefs.GetString(SettingsPopup.NicknameKey, ""));

            var multi  = SaveManager.LoadMulti();
            var single = SaveManager.LoadSingle();
            ApplyStats(multi.WinCount, multi.LoseCount, single.ClearCountByLength);
        }

        // Firebase DataSnapshot → UI 반영
        private void ApplyFirebaseData(DataSnapshot snap)
        {
            if (snap == null) return;

            // 닉네임
            if (snap.HasChild("nickname") && nicknameInput)
            {
                string nick = snap.Child("nickname").Value?.ToString() ?? "";
                nicknameInput.SetTextWithoutNotify(nick);
                PlayerPrefs.SetString(SettingsPopup.NicknameKey, nick);
            }

            // 멀티 전적
            int win  = ParseInt(snap, "multi/win");
            int lose = ParseInt(snap, "multi/lose");

            // 단어 클리어
            int[] clears = new int[7];
            for (int len = 2; len <= 6; len++)
                clears[len] = ParseInt(snap, $"single/clearByLength/{len}");

            ApplyStats(win, lose, clears);
        }

        private void ApplyStats(int win, int lose, int[] clears)
        {
            if (winText)  winText.text  = win.ToString();
            if (loseText) loseText.text = lose.ToString();

            int total = win + lose;
            float rate = total > 0 ? (float)win / total * 100f : 0f;
            if (winRateText) winRateText.text = $"{rate:F0}%";

            if (clear2Text) clear2Text.text = (clears?.Length > 2 ? clears[2] : 0).ToString();
            if (clear3Text) clear3Text.text = (clears?.Length > 3 ? clears[3] : 0).ToString();
            if (clear4Text) clear4Text.text = (clears?.Length > 4 ? clears[4] : 0).ToString();
            if (clear5Text) clear5Text.text = (clears?.Length > 5 ? clears[5] : 0).ToString();
            if (clear6Text) clear6Text.text = (clears?.Length > 6 ? clears[6] : 0).ToString();
        }

        private void OnNicknameChanged(string value)
        {
            string trimmed = value.Trim();
            PlayerPrefs.SetString(SettingsPopup.NicknameKey, trimmed);
            PlayerPrefs.Save();
            FirebaseManager.Instance?.SaveNickname(trimmed);
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
