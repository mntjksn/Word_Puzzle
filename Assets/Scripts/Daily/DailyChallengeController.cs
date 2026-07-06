using System;
using TMPro;
using UnityEngine;
using WordPuzzle.Core;
using WordPuzzle.Data;
using WordPuzzle.Save;
using WordPuzzle.UI;

namespace WordPuzzle.Daily
{
    // 일일 도전 모드 관리
    public class DailyChallengeController : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TokenView        tokenView;
        [SerializeField] private InputHistoryView historyView;
        [SerializeField] private TMP_InputField   inputField;
        [SerializeField] private TextMeshProUGUI  attemptText;
        [SerializeField] private TextMeshProUGUI  elapsedText;
        [SerializeField] private GameObject       clearPanel;
        [SerializeField] private GameObject       alreadyClearedPanel;

        private WordData       _todayWord;
        private System.Collections.Generic.List<string> _answerTokens;
        private DailySaveData  _saveData;
        private float          _elapsedTime;
        private bool           _isPlaying;
        private string         _todayKey;

        private void Start()
        {
            _saveData = SaveManager.LoadDaily();
            _todayKey = DateTime.Now.ToString("yyyyMMdd");
            InitGame();
        }

        private void Update()
        {
            if (!_isPlaying) return;
            _elapsedTime += Time.deltaTime;

            // UI는 1초 단위로만 갱신
            int seconds = (int)_elapsedTime;
            elapsedText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private void InitGame()
        {
            _todayWord    = WordDatabase.Instance.GetDaily(DateTime.Now);
            _answerTokens = JamoConverter.GetAnswerTokens(_todayWord.word);
            var displayTokens = JamoConverter.GetDisplayTokens(_todayWord.word);

            tokenView.Build(displayTokens);
            historyView.Clear();

            // 오늘 이미 클리어한 경우
            if (_saveData.LastPlayDate == _todayKey && _saveData.IsClearedToday)
            {
                alreadyClearedPanel.SetActive(true);
                inputField.interactable = false;
                return;
            }

            // 날짜가 바뀐 경우 초기화
            if (_saveData.LastPlayDate != _todayKey)
            {
                UpdateStreak();
                _saveData.LastPlayDate  = _todayKey;
                _saveData.IsClearedToday = false;
                _saveData.TodayAttempts = 0;
                _saveData.TodayClearTime = 0;
                SaveManager.SaveDaily(_saveData);
            }

            _elapsedTime = 0;
            _isPlaying   = true;
            UpdateAttemptUI();
        }

        public void OnSubmit()
        {
            string input = inputField.text.Trim();
            if (string.IsNullOrEmpty(input) || !_isPlaying) return;

            var inputTokens = JamoConverter.GetAnswerTokens(input);
            var result      = BaseballJudge.Judge(_answerTokens, inputTokens);

            historyView.AddEntry(input, result);
            inputField.text = "";

            _saveData.TodayAttempts++;
            UpdateAttemptUI();

            if (result.IsCorrect)
                OnClear();
            else
                SaveManager.SaveDaily(_saveData);
        }

        private void OnClear()
        {
            _isPlaying               = false;
            _saveData.IsClearedToday = true;
            _saveData.TodayClearTime = _elapsedTime;
            SaveManager.SaveDaily(_saveData);
            clearPanel.SetActive(true);
            inputField.interactable = false;
        }

        private void UpdateStreak()
        {
            // 어제 날짜와 비교해서 연속 성공일 갱신
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            if (_saveData.LastPlayDate == yesterday && _saveData.IsClearedToday)
                _saveData.StreakDays++;
            else
                _saveData.StreakDays = 0;
        }

        private void UpdateAttemptUI()
            => attemptText.text = $"시도: {_saveData.TodayAttempts}회";
    }
}
