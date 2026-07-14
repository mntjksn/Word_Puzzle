using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WordPuzzle.Audio
{
    // BGM/SFX 재생 관리 싱글톤. Intro 씬에만 배치되어 있고 DontDestroyOnLoad로 이후 씬까지 유지됨
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private const string BgmVolumeKey = "BGM_VOLUME";
        private const string SfxVolumeKey = "SFX_VOLUME";
        private const string ResourceRoot = "Sound";

        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        private readonly Dictionary<string, AudioClip> _sfxClips = new Dictionary<string, AudioClip>();

        public float BgmVolume => bgmSource != null ? bgmSource.volume : PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        public float SfxVolume => sfxSource != null ? sfxSource.volume : PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // MultiMenu/MultiRoom 씬처럼 카메라 AudioListener가 없는 씬에서도 오디오 출력을 보장
            if (GetComponent<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();

            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop         = true;
            bgmSource.playOnAwake  = false;
            sfxSource.playOnAwake  = false;

            LoadClips();

            bgmSource.volume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            sfxSource.volume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

            var bgmClip = Resources.Load<AudioClip>(ResourceRoot + "/BGM");
            if (bgmClip != null)
            {
                bgmSource.clip = bgmClip;
                bgmSource.Play();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // 씬 카메라의 AudioListener와 중복되지 않도록 타 오브젝트의 것을 비활성화
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var al in FindObjectsOfType<AudioListener>())
            {
                if (al.gameObject != gameObject)
                    al.enabled = false;
            }
        }

        // Resources/Sound/SFX 폴더의 클립을 파일명 기준으로 캐싱 → 인스펙터 연결 없이 이름으로 재생 가능
        private void LoadClips()
        {
            var clips = Resources.LoadAll<AudioClip>(ResourceRoot + "/SFX");
            foreach (var clip in clips)
                _sfxClips[clip.name] = clip;
        }

        public void PlaySfx(string clipName)
        {
            if (string.IsNullOrEmpty(clipName) || sfxSource == null) return;
            if (_sfxClips.TryGetValue(clipName, out var clip) && clip != null)
                sfxSource.PlayOneShot(clip);
        }

        public void SetBgmVolume(float value)
        {
            float v = Mathf.Clamp01(value);
            if (bgmSource != null) bgmSource.volume = v;
            PlayerPrefs.SetFloat(BgmVolumeKey, v);
            PlayerPrefs.Save();
        }

        public void SetSfxVolume(float value)
        {
            float v = Mathf.Clamp01(value);
            if (sfxSource != null) sfxSource.volume = v;
            PlayerPrefs.SetFloat(SfxVolumeKey, v);
            PlayerPrefs.Save();
        }
    }
}
