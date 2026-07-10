using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using WordPuzzle.Audio;
using WordPuzzle.Core;
using WordPuzzle.Data;
using WordPuzzle.Save;
using WordPuzzle.UI;

namespace WordPuzzle.Daily
{
    public class DailyChallengeController : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TokenView        tokenView;
        [SerializeField] private InputHistoryView historyView;
        [SerializeField] private TMP_InputField   inputField;
        [SerializeField] private TextMeshProUGUI  difficultyText;
        [SerializeField] private TextMeshProUGUI  attemptText;
        [SerializeField] private TextMeshProUGUI  pointsText;
        [SerializeField] private TextMeshProUGUI  lineHintLabel;
        [SerializeField] private UnityEngine.UI.Button btnLineHint;

        [Header("클리어 패널")]
        [SerializeField] private GameObject      clearPanel;
        [SerializeField] private TextMeshProUGUI clearWordText;
        [SerializeField] private TextMeshProUGUI clearAttemptText;
        [SerializeField] private TextMeshProUGUI clearPointsText;

        [Header("다음 도전 팝업")]
        [SerializeField] private GameObject      nextChallengePopup;
        [SerializeField] private TextMeshProUGUI nextTimerText;

        private const int DailyReward = 10;

        private WordData       _currentWord;
        private List<string>   _answerTokens;
        private SingleSaveData _singleSave;
        private DailySaveData  _dailySave;
        private int            _attempts;
        private bool           _isCleared;
        private bool           _lineHintUsed;
        private Coroutine      _timerCoroutine;

        private void Awake()
        {
            if (inputField != null)
                inputField.onSubmit.AddListener(_ => OnSubmit());
        }

        private void Start()
        {
            _singleSave = SaveManager.LoadSingle();
            _dailySave  = SaveManager.LoadDaily();
            StartCoroutine(WaitAndInit());
        }

        private IEnumerator WaitAndInit()
        {
            while (WordDatabase.Instance == null || !WordDatabase.Instance.IsLoaded)
                yield return null;
            InitGame();
        }

        private void InitGame()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");

            if (_dailySave.LastPlayDate != today)
            {
                _dailySave.LastPlayDate      = today;
                _dailySave.IsClearedToday    = false;
                _dailySave.TodayAttempts     = 0;
                _dailySave.TodayClearTime    = 0f;
                _dailySave.DailyLineHintUsed = false;
                SaveManager.SaveDaily(_dailySave);
            }

            _currentWord = WordDatabase.Instance.GetDailyChallenge(DateTime.Now);
            if (_currentWord == null) { Debug.LogError("[Daily] 단어 없음"); return; }

            _answerTokens = JamoConverter.GetAnswerTokens(_currentWord.word);
            var display   = JamoConverter.GetDisplayTokens(_currentWord.word);
            ShuffleList(display);

            _attempts     = _dailySave.TodayAttempts;
            _lineHintUsed = _dailySave.DailyLineHintUsed;
            _isCleared    = _dailySave.IsClearedToday;

            if (!_isCleared) SoundManager.Instance?.PlaySfx("match_start");

            if (difficultyText != null) difficultyText.text = $"{_currentWord.length}글자";
            UpdateAttemptText();
            UpdatePointsText();
            tokenView.Build(display);
            historyView.Clear();

            if (lineHintLabel != null)
            {
                if (_lineHintUsed && !string.IsNullOrEmpty(_currentWord.hint))
                { lineHintLabel.text = _currentWord.hint; lineHintLabel.gameObject.SetActive(true); }
                else lineHintLabel.gameObject.SetActive(false);
            }
            if (btnLineHint != null) btnLineHint.interactable = !_lineHintUsed;
            if (nextChallengePopup != null) nextChallengePopup.SetActive(false);

            if (_isCleared)
            {
                clearPanel.SetActive(true);
                if (inputField       != null) inputField.interactable  = false;
                if (clearWordText    != null) clearWordText.text    = _currentWord.word;
                if (clearAttemptText != null) clearAttemptText.text = $"{_attempts}번 만에 성공!";
                if (clearPointsText  != null) clearPointsText.text  = "오늘의 도전 완료!";
            }
            else
            {
                clearPanel.SetActive(false);
                if (inputField != null) inputField.interactable = true;
            }
        }

        public void OnSubmit()
        {
            if (_isCleared) return;
            string input = inputField.text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            _attempts++;
            UpdateAttemptText();
            inputField.text = "";

            if (input.Length != _currentWord.length)
            {
                SoundManager.Instance?.PlaySfx("wrong");
                historyView.AddErrorEntry(input);
                _dailySave.TodayAttempts = _attempts;
                SaveManager.SaveDaily(_dailySave);
                return;
            }

            var inputTokens = JamoConverter.GetAnswerTokens(input);
            var result      = BaseballJudge.Judge(_answerTokens, inputTokens);
            SoundManager.Instance?.PlaySfx(result.IsCorrect ? "correct" : "wrong");
            historyView.AddEntry(input, result, _answerTokens.Count);

            if (result.Hits != null)
                for (int i = 0; i < result.Hits.Length; i++)
                    if (result.Hits[i] == TokenHit.Strike)
                        tokenView.LockPosition(i, _answerTokens[i], JamoConverter.ToDisplayJamo(_answerTokens[i]));

            _dailySave.TodayAttempts = _attempts;
            SaveManager.SaveDaily(_dailySave);

            if (result.IsCorrect) OnClear();
        }

        public void OnLineHint()
        {
            if (_lineHintUsed || string.IsNullOrEmpty(_currentWord?.hint)) return;
            SoundManager.Instance?.PlaySfx("hint_reveal");
            _lineHintUsed = true;
            _dailySave.DailyLineHintUsed = true;
            SaveManager.SaveDaily(_dailySave);
            if (lineHintLabel != null) { lineHintLabel.text = _currentWord.hint; lineHintLabel.gameObject.SetActive(true); }
            if (btnLineHint   != null) btnLineHint.interactable = false;
        }

        private void OnClear()
        {
            _isCleared = true;
            if (!_dailySave.IsClearedToday)
                _dailySave.TotalDailyClearCount++;
            _dailySave.IsClearedToday = true;
            _singleSave.Points       += DailyReward;
            SaveManager.SaveSingle(_singleSave);
            SaveManager.SaveDaily(_dailySave);

            if (inputField != null) inputField.interactable = false;
            UpdatePointsText();
            clearPanel.SetActive(true);
            if (clearWordText    != null) clearWordText.text    = _currentWord.word;
            if (clearAttemptText != null) clearAttemptText.text = $"{_attempts}번 만에 성공!";
            if (clearPointsText  != null) clearPointsText.text  = $"+{DailyReward}P 획득!";
        }

        // 다음 도전 팝업 열기
        public void OnShowNextTimer()
        {
            if (nextChallengePopup == null) return;
            nextChallengePopup.SetActive(true);
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerLoop());
        }

        public void OnCloseNextTimer()
        {
            if (nextChallengePopup != null) nextChallengePopup.SetActive(false);
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
        }

        private static readonly WaitForSeconds OneSecondWait = new WaitForSeconds(1f);

        private IEnumerator TimerLoop()
        {
            while (nextChallengePopup != null && nextChallengePopup.activeSelf)
            {
                if (nextTimerText != null)
                {
                    var remain = DateTime.Now.Date.AddDays(1) - DateTime.Now;
                    nextTimerText.text = $"{remain.Hours:D2}:{remain.Minutes:D2}:{remain.Seconds:D2}";
                }
                yield return OneSecondWait;
            }
        }

        public void OnBackButton()
        {
            SoundManager.Instance?.PlaySfx("button_back");
            SceneManager.LoadScene("Intro");
        }

        private void UpdateAttemptText()
        {
            if (attemptText != null)
                attemptText.text = _attempts == 0 ? "도전 중..." : $"{_attempts}번째 시도";
        }

        private void UpdatePointsText()
        {
            if (pointsText != null)
                pointsText.text = $"{_singleSave.Points} P";
        }

        private static void ShuffleList(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
