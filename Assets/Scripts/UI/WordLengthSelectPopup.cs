using UnityEngine;
using UnityEngine.SceneManagement;

namespace WordPuzzle.UI
{
    public class WordLengthSelectPopup : MonoBehaviour
    {
        public const string PrefKey = "single_word_length";

        private void Awake()
        {
            InitFonts();
            gameObject.SetActive(false);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public void OnSelect2() => Load(2);
        public void OnSelect3() => Load(3);
        public void OnSelect4() => Load(4);
        public void OnSelect5() => Load(5);
        public void OnSelect6() => Load(6);

        private void Load(int length)
        {
            PlayerPrefs.SetInt(PrefKey, length);
            PlayerPrefs.Save();
            SceneManager.LoadScene("SingleGame");
        }

        // 씬에 이미 로드된 Maplestory 폰트를 찾아 팝업 내 TMP에 적용
        private void InitFonts()
        {
            TMPro.TMP_FontAsset boldFont = null, lightFont = null;
            var all = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
            foreach (var f in all)
            {
                if (f.name.Contains("Bold"))  boldFont  = f;
                if (f.name.Contains("Light")) lightFont = f;
            }
            if (boldFont == null || lightFont == null) return;

            foreach (var tmp in GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                string n = tmp.gameObject.name;
                bool useBold = (n == "NumText" || n == "Title");
                tmp.font = useBold ? boldFont : lightFont;
            }
        }
    }
}
