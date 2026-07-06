using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private readonly List<string>           _hintMessages      = new List<string>();
        private readonly HashSet<int>           _revealedPositions = new HashSet<int>();
        private readonly List<HistoryEntryData> _historyLog        = new List<HistoryEntryData>();

        private WordData       _currentWord;
        private List<string>   _answerTokens;
        private SingleSaveData _saveData;
        private int            _attempts;

        private const string ContinueKey = "IsContinue";

        private void Start()
        {
            _saveData = SaveManager.LoadSingle();
            if (PlayerPrefs.GetInt(ContinueKey, 0) == 1)
            {
                PlayerPrefs.DeleteKey(ContinueKey);
                StartCoroutine(WaitAndRestore());
            }
            else
            {
                int length = PlayerPrefs.GetInt(WordLengthSelectPopup.PrefKey, 4);
                StartCoroutine(WaitAndStart(length));
            }
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

        private IEnumerator WaitAndRestore()
        {
            while (WordDatabase.Instance == null || !WordDatabase.Instance.IsLoaded)
                yield return null;
            var save = SaveManager.LoadMidGame();
            if (save == null)
            {
                StartGame(PlayerPrefs.GetInt(WordLengthSelectPopup.PrefKey, 4));
                yield break;
            }
            RestoreGame(save);
        }

        public void StartGame(int letterCount)
        {
            SaveManager.ClearMidGame();

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
            _hintMessages.Clear();
            _historyLog.Clear();

            difficultyText.text = $"{letterCount}글자";
            UpdateAttemptText();
            tokenView.Build(displayTokens);
            hintController.Setup(_answerTokens, OnHintRevealed, _revealedPositions);
            historyView.Clear();
            if (hintLabel != null) { hintLabel.text = ""; hintLabel.gameObject.SetActive(false); }
            inputField.text = "";
            inputField.interactable = true;
            clearPanel.SetActive(false);
        }

        private void RestoreGame(MidGameSave save)
        {
            _currentWord = WordDatabase.Instance.GetById(save.wordId)
                        ?? WordDatabase.Instance.GetByWord(save.word);
            if (_currentWord == null)
            {
                // 단어 DB에 없으면 새 게임
                SaveManager.ClearMidGame();
                StartGame(save.wordLength);
                return;
            }

            _answerTokens = JamoConverter.GetAnswerTokens(_currentWord.word);
            _attempts     = save.attempts;

            _revealedPositions.Clear();
            if (save.revealedPositions != null)
                foreach (int p in save.revealedPositions)
                    _revealedPositions.Add(p);

            _hintMessages.Clear();
            if (save.hintMessages != null)
                _hintMessages.AddRange(save.hintMessages);

            _historyLog.Clear();
            if (save.history != null)
                _historyLog.AddRange(save.history);

            difficultyText.text = $"{save.wordLength}글자";
            UpdateAttemptText();

            // TokenView 복원
            var fixedPos = new Dictionary<int, string>();
            if (save.fixedPosKeys != null)
                for (int i = 0; i < save.fixedPosKeys.Length; i++)
                    fixedPos[save.fixedPosKeys[i]] = save.fixedPosValues[i];
            var pool = save.unfixedPool != null ? new List<string>(save.unfixedPool) : new List<string>();
            tokenView.BuildFromRestore(_answerTokens.Count, fixedPos, pool);

            // HintController 복원 (힌트 사용 횟수 = hintMessages 수)
            hintController.Setup(_answerTokens, OnHintRevealed, _revealedPositions, _hintMessages.Count);

            // 힌트 라벨 복원
            if (hintLabel != null)
            {
                if (_hintMessages.Count > 0)
                {
                    hintLabel.text = string.Join("  ", _hintMessages);
                    hintLabel.gameObject.SetActive(true);
                }
                else
                {
                    hintLabel.gameObject.SetActive(false);
                }
            }

            // 히스토리 복원
            historyView.RestoreEntries(_historyLog);

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

            if (input.Length != _currentWord.length)
            {
                historyView.AddErrorEntry(input);
                _historyLog.Add(new HistoryEntryData { word = input, isError = true });
                return;
            }

            var inputTokens = JamoConverter.GetAnswerTokens(input);
            var result      = BaseballJudge.Judge(_answerTokens, inputTokens);
            historyView.AddEntry(input, result, _answerTokens.Count);
            _historyLog.Add(new HistoryEntryData
            {
                word               = input,
                isError            = false,
                hits               = result.Hits?.Select(h => (int)h).ToArray(),
                expectedTokenCount = _answerTokens.Count
            });

            if (result.Hits != null)
            {
                for (int i = 0; i < result.Hits.Length; i++)
                {
                    if (result.Hits[i] == TokenHit.Strike && _revealedPositions.Add(i))
                        tokenView.LockPosition(i, _answerTokens[i],
                            JamoConverter.ToDisplayJamo(_answerTokens[i]));
                }
            }

            if (result.IsCorrect) OnClear();
        }

        private void OnClear()
        {
            SaveManager.ClearMidGame();
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
            int idx = position - 1;
            if (_revealedPositions.Add(idx))
                tokenView.LockPosition(idx, token, JamoConverter.ToDisplayJamo(token));

            _hintMessages.Add($"{position}번째 칸: {token}");
            if (hintLabel != null)
            {
                hintLabel.text = string.Join("  ", _hintMessages);
                hintLabel.gameObject.SetActive(true);
            }
        }

        private void SaveMidGame()
        {
            if (_currentWord == null) return;

            var fixedPos  = tokenView.GetFixedPositions();
            var unfixPool = tokenView.GetUnfixedPool();

            var save = new MidGameSave
            {
                word               = _currentWord.word,
                wordId             = _currentWord.id,
                wordLength         = _currentWord.length,
                attempts           = _attempts,
                revealedPositions  = _revealedPositions.ToArray(),
                hintMessages       = _hintMessages.ToArray(),
                fixedPosKeys       = fixedPos.Keys.ToArray(),
                fixedPosValues     = fixedPos.Values.ToArray(),
                unfixedPool        = unfixPool.ToArray(),
                history            = new List<HistoryEntryData>(_historyLog)
            };
            SaveManager.SaveMidGame(save);
        }

        public void OnPlayAgain() => StartGame(_currentWord.length);

        public void OnBackButton()
        {
            SaveMidGame();
            SceneManager.LoadScene("Intro");
        }

        private static void ShuffleList(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
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
