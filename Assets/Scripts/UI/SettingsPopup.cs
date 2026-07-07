using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Audio;

namespace WordPuzzle.UI
{
    public class SettingsPopup : MonoBehaviour
    {
        [SerializeField] private Slider          bgmSlider;
        [SerializeField] private Slider          sfxSlider;
        [SerializeField] private Button          closeButton;
        [SerializeField] private TMP_InputField  nicknameInput;

        public const string NicknameKey = "PlayerNickname";

        private void Start()
        {
            if (bgmSlider)     bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (sfxSlider)     sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (closeButton)   closeButton.onClick.AddListener(Hide);
            if (nicknameInput) nicknameInput.onEndEdit.AddListener(OnNicknameChanged);
        }

        public void Show()
        {
            if (SoundManager.Instance != null)
            {
                if (bgmSlider) bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);
                if (sfxSlider) sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
            }
            if (nicknameInput)
                nicknameInput.SetTextWithoutNotify(PlayerPrefs.GetString(NicknameKey, ""));
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void OnBgmChanged(float value)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.BgmVolume = value;
        }

        public void OnSfxChanged(float value)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.SfxVolume = value;
        }

        private void OnNicknameChanged(string value)
        {
            PlayerPrefs.SetString(NicknameKey, value.Trim());
            PlayerPrefs.Save();
        }
    }
}
