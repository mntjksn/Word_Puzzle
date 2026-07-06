using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using WordPuzzle.Core;
using WordPuzzle.Data;
using WordPuzzle.Save;
using WordPuzzle.UI;

namespace WordPuzzle.Single
{
    public class SingleGameController : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TokenView        tokenView;
        [SerializeField] private InputHistoryView historyView;
        [SerializeField] private HintController   hintController;
        [SerializeField] private TMP_InputField   inputField;
        [SerializeField] private TextMeshProUGUI  difficultyText;
        [SerializeField] private TextMeshProUGUI  attemptText;
        [SerializeField] private TextMeshProUGUI  hintLabel;
        [SerializeField] private GameObject       clearPanel;
        [SerializeField] private TextMeshProUGUI  clearWordText;
        [SerializeField] private TextMeshProUGUI  clearAttemptText;

        private readonly List<string>  _hintMessages      = new List<string>();
        private readonly HashSet<int>  _revealedPositions = new HashSet<int>();

        private WordData       _currentWord;
        private List<string>   _answerTokens;
        private SingleSaveData _saveData;
        private int            _attempts;

        private void Start()
        {
            _saveData = SaveManager.LoadSingle();
            int length = PlayerPrefs.GetInt(WordLengthSelectPopup.PrefKey, 4);
            StartCoroutine(WaitAndStart(length));
        }

        private void Update()
        {
            if (inputField != null && inputField.interactable && inputField.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnSubmit();
            }
        }

        private IEnumerator WaitAndStart(int length)
        {
            while (WordDatabase.Instance == null || !WordDatabase.Instance.IsLoaded)
                yield return null;
            StartGame(length);
        }

        public void StartGame(int letterCount)
        {
            _currentWord = WordDatabase.Instance.GetRandom(letterCount);
            if (_currentWord == null)
            {
                Debug.LogWarning($"[Single] {letterCount}글자 단어 없음");
                return;
            }

            _answerTokens = JamoConverter.GetAnswerTokens(_currentWord.word);
            var displayTokens = JamoConverter.GetDisplayTokens(_currentWord.word);
            ShuffleList(displayTokens);
            _attempts = 0;
            _revealedPositions.Clear();

            difficultyText.text = $"{letterCount}글자";
            UpdateAttemptText();
            tokenView.Build(displayTokens);
            hintController.Setup(_answerTokens, OnHintRevealed, _revealedPositions);
            historyView.Clear();
            _hintMessages.Clear();
            if (hintLabel != null) { hintLabel.text = ""; hintLabel.gameObject.SetActive(false); }
            inputField.text = "";
            inputField.interactable = true;
            clearPanel.SetActive(false);
        }

        public void OnSubmit()
        {
            string input = inputField.text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            _attempts++;
            UpdateAttemptText();
            inputField.text = "";
            inputField.ActivateInputField();

            // 글자 수 불일치(초과/미달) → 에러 표기
            if (input.Length != _currentWord.length)
            {
                historyView.AddErrorEntry(input);
                return;
            }

            var inputTokens = JamoConverter.GetAnswerTokens(input);
            var result      = BaseballJudge.Judge(_answerTokens, inputTokens);
            historyView.AddEntry(input, result, _answerTokens.Count);

            // 새로 맞춘 스트라이크 위치: 보기에서 제거 → 정답 슬롯에 표시
            if (result.Hits != null)
            {
                for (int i = 0; i < result.Hits.Length; i++)
                {
                    if (result.Hits[i] == TokenHit.Strike && !_revealedPositions.Contains(i))
                    {
                        _revealedPositions.Add(i);
                        tokenView.LockPosition(i, _answerTokens[i],
                            JamoConverter.ToDisplayJamo(_answerTokens[i]));
                    }
                }
            }

            if (result.IsCorrect) OnClear();
        }

        private void OnClear()
        {
            clearPanel.SetActive(true);
            inputField.interactable = false;

            if (clearWordText  != null) clearWordText.text  = _currentWord.word;
            if (clearAttemptText != null) clearAttemptText.text = $"{_attempts}번 만에 성공!";

            if (!_saveData.ClearedWordIds.Contains(_currentWord.id))
                _saveData.ClearedWordIds.Add(_currentWord.id);
            _saveData.IncrementClear(_currentWord.length);
            SaveManager.SaveSingle(_saveData);
        }

        private void OnHintRevealed(int position, string token)
        {
            int idx = position - 1; // 0-indexed
            if (_revealedPositions.Add(idx))
                tokenView.LockPosition(idx, token, JamoConverter.ToDisplayJamo(token));

            _hintMessages.Add($"{position}번째 칸: {token}");
            if (hintLabel != null)
            {
                hintLabel.text = string.Join("  ", _hintMessages);
                hintLabel.gameObject.SetActive(true);
            }
        }

        public void OnPlayAgain() => StartGame(_currentWord.length);

        public void OnBackButton() => SceneManager.LoadScene("Intro");

        private static void ShuffleList(System.Collections.Generic.List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        private void UpdateAttemptText()
        {
            if (attemptText != null)
                attemptText.text = _attempts == 0 ? "도전 중..." : $"{_attempts}번째 시도";
        }
    }
}
