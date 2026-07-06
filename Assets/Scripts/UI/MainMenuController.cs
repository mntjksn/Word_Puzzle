using UnityEngine;
using UnityEngine.SceneManagement;

namespace WordPuzzle.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private WordLengthSelectPopup _singleModePopup;

        public void OnSingleMode()
        {
            if (_singleModePopup != null) _singleModePopup.Show();
            else SceneManager.LoadScene("SingleGame");
        }

        public void OnDailyMode()  => SceneManager.LoadScene("DailyChallenge");
        public void OnMultiMode()  => SceneManager.LoadScene("MultiMenu");
        public void OnSettings()   => Debug.Log("[Menu] 설정 (미구현)");
    }
}
