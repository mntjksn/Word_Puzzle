using UnityEngine;
using UnityEngine.SceneManagement;
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

        private const string ContinueKey = "IsContinue";

        private void Start()
        {
            if (_continueButton != null)
                _continueButton.SetActive(SaveManager.HasMidGame());
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
