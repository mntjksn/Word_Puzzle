using UnityEngine;

namespace WordPuzzle.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private const string BgmKey = "vol_bgm";
        private const string SfxKey = "vol_sfx";

        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        public float BgmVolume
        {
            get => bgmSource != null ? bgmSource.volume : PlayerPrefs.GetFloat(BgmKey, 1f);
            set
            {
                float v = Mathf.Clamp01(value);
                if (bgmSource != null) bgmSource.volume = v;
                PlayerPrefs.SetFloat(BgmKey, v);
                PlayerPrefs.Save();
            }
        }

        public float SfxVolume
        {
            get => sfxSource != null ? sfxSource.volume : PlayerPrefs.GetFloat(SfxKey, 1f);
            set
            {
                float v = Mathf.Clamp01(value);
                if (sfxSource != null) sfxSource.volume = v;
                PlayerPrefs.SetFloat(SfxKey, v);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

            bgmSource.volume = PlayerPrefs.GetFloat(BgmKey, 1f);
            sfxSource.volume = PlayerPrefs.GetFloat(SfxKey, 1f);
            bgmSource.loop = true;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null) sfxSource.PlayOneShot(clip);
        }
    }
}
