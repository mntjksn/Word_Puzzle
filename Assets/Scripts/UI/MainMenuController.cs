using UnityEngine;
using UnityEngine.SceneManagement;
using WordPuzzle.Firebase;
using WordPuzzle.Save;

namespace WordPuzzle.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private WordLengthSelectPopup _singleModePopup;
        [SerializeField] private GameObject            _continueButton;
        [SerializeField] private GameObject            _howToPlayPopup;
        [SerializeField] private SettingsPopup         _settingsPopup;
        [SerializeField] private ProfilePopup          _profilePopup;
        [SerializeField] private NicknameSetupPopup    _nicknameSetupPopup;

        private const string ContinueKey = "IsContinue";

        private void Start()
        {
            if (_continueButton != null)
                _continueButton.SetActive(SaveManager.HasMidGame());

            // 닉네임 없으면 설정 팝업 (AccountRestorePopup이 없는 경우 fallback)
            if (_nicknameSetupPopup != null && NicknameSetupPopup.NeedsNickname())
                Invoke(nameof(ShowNicknameSetupIfNeeded), 0.8f);
        }

        private void ShowNicknameSetupIfNeeded()
        {
            if (_nicknameSetupPopup == null) return;
            if (!NicknameSetupPopup.NeedsNickname()) return;
            // AccountRestorePopup이 신규 유저 처리 중이면 거기서 띄움
            if (FirebaseManager.NewUserDetected) return;
            _nicknameSetupPopup.Show();
        }

        public void OnSingleMode()
        {
            SaveManager.ClearMidGame();
            if (_singleModePopup != null) _singleModePopup.Show();
            else SceneManager.LoadScene("SingleGame");
        }

        public void OnContinue()
        {
            PlayerPrefs.SetInt(ContinueKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("SingleGame");
        }

        public void OnHowToPlay()       { if (_howToPlayPopup) _howToPlayPopup.SetActive(true);  }
        public void OnHowToPlayClose()  { if (_howToPlayPopup) _howToPlayPopup.SetActive(false); }

        public void OnDailyMode()  => SceneManager.LoadScene("DailyChallenge");
        public void OnMultiMode()  => SceneManager.LoadScene("MultiMenu");
        public void OnSettings()   { if (_settingsPopup) _settingsPopup.Show(); }
        public void OnProfile()    { if (_profilePopup)  _profilePopup.Show();  }
    }
}
