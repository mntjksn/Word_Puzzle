using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Audio;

namespace WordPuzzle.UI
{
    public class SettingsPopup : MonoBehaviour
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button closeButton;

        // 닉네임 키는 ProfilePopup에서도 참조하므로 공용으로 유지
        public const string NicknameKey = "PlayerNickname";

        private void Start()
        {
            if (bgmSlider)   bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (sfxSlider)   sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (closeButton) closeButton.onClick.AddListener(() => { SoundManager.Instance?.PlaySfx("button_back"); Hide(); });
        }

        public void Show()
        {
            if (SoundManager.Instance != null)
            {
                if (bgmSlider) bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);
                if (sfxSlider) sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
            }
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void OnBgmChanged(float value)
        {
            SoundManager.Instance?.SetBgmVolume(value);
        }

        public void OnSfxChanged(float value)
        {
            SoundManager.Instance?.SetSfxVolume(value);
        }
    }
}
