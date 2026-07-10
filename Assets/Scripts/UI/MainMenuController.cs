using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordPuzzle.Audio;
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
            SoundManager.Instance?.PlaySfx("button_click");
            SaveManager.ClearMidGame();
            if (_singleModePopup != null) _singleModePopup.Show();
            else SceneManager.LoadScene("SingleGame");
        }

        public void OnContinue()
        {
            SoundManager.Instance?.PlaySfx("button_click");
            PlayerPrefs.SetInt(ContinueKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("SingleGame");
        }

        public void OnHowToPlay()
        {
            SoundManager.Instance?.PlaySfx("button_click");
            if (_howToPlayPopup == null) return;
            _howToPlayPopup.SetActive(true);
            var scrollRect = _howToPlayPopup.GetComponentInChildren<ScrollRect>();
            if (scrollRect != null) StartCoroutine(ResetScrollToTop(scrollRect));
        }

        public void OnHowToPlayClose()
        {
            SoundManager.Instance?.PlaySfx("button_back");
            if (_howToPlayPopup) _howToPlayPopup.SetActive(false);
        }

        // 팝업을 열 때마다 스크롤을 맨 위로 되돌림 (레이아웃 계산 이후에 적용해야 함)
        private IEnumerator ResetScrollToTop(ScrollRect scrollRect)
        {
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        public void OnDailyMode()
        {
            SoundManager.Instance?.PlaySfx("button_click");
            SceneManager.LoadScene("DailyChallenge");
        }

        public void OnMultiMode()
        {
            SoundManager.Instance?.PlaySfx("button_click");
            SceneManager.LoadScene("MultiMenu");
        }

        public void OnSettings()
        {
            SoundManager.Instance?.PlaySfx("button_click");
            if (_settingsPopup) _settingsPopup.Show();
        }

        public void OnProfile()
        {
            SoundManager.Instance?.PlaySfx("button_click");
            if (_profilePopup) _profilePopup.Show();
        }
    }
}
