#if PHOTON_UNITY_NETWORKING
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Core;

namespace WordPuzzle.Multi
{
    // 플레이어 입력 제출, 히든 처리, RaiseEvent 전송
    public class MultiSubmitController : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TMP_InputField  inputField;
        [SerializeField] private Button          submitButton;
        [SerializeField] private Button          hiddenButton;
        [SerializeField] private TextMeshProUGUI hiddenButtonText;

        private List<string> _answerTokens;
        private bool         _hasSubmitted;
        private bool         _useHidden;
        private bool         _hiddenUsed;   // 게임당 1회 제한
        private int          _currentTurn;

        // 게임 시작 시 히든 초기화
        public void ResetForGame()
        {
            _hiddenUsed           = false;
            hiddenButton.interactable = true;
            UpdateHiddenButtonUI();
        }

        // 새 턴 시작 시 UI 초기화
        public void Setup(List<string> answerTokens, int turnIndex)
        {
            _answerTokens             = answerTokens;
            _currentTurn              = turnIndex;
            _hasSubmitted             = false;
            _useHidden                = false;
            inputField.text           = "";
            inputField.interactable   = true;
            submitButton.interactable = true;
            hiddenButton.interactable = !_hiddenUsed;
            UpdateHiddenButtonUI();
        }

        // 히든 버튼 토글
        public void OnHiddenButtonClicked()
        {
            if (_hiddenUsed || _hasSubmitted) return;
            _useHidden = !_useHidden;
            UpdateHiddenButtonUI();
        }

        // 제출 버튼
        public void OnSubmitButtonClicked()
        {
            if (_hasSubmitted) return;

            string input = inputField.text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            var inputTokens = JamoConverter.GetAnswerTokens(input);
            var result      = BaseballJudge.Judge(_answerTokens, inputTokens);

            var submit = new SubmitData
            {
                ActorNumber = PhotonNetwork.LocalPlayer.ActorNumber,
                TurnIndex   = _currentTurn,
                InputWord   = input,
                Strike      = result.Strike,
                Ball        = result.Ball,
                OutCount    = result.OutCount,
                IsCorrect   = result.IsCorrect,
                SubmitTime  = PhotonNetwork.Time,
                UseHidden   = _useHidden,
            };

            // 히든 사용 시 상대에게는 숨긴 데이터 전송
            var sendData = _useHidden ? submit.ToHiddenView() : submit;

            if (_useHidden)
            {
                _hiddenUsed               = true;
                _useHidden                = false;
                hiddenButton.interactable = false;
            }

            PhotonNetwork.RaiseEvent(
                MultiNetworkEvents.SubmitTurn,
                sendData.Serialize(),
                MultiNetworkEvents.All,
                MultiNetworkEvents.Reliable);

            _hasSubmitted             = true;
            inputField.interactable   = false;
            submitButton.interactable = false;
        }

        private void UpdateHiddenButtonUI()
        {
            if (_hiddenUsed)
                hiddenButtonText.text = "히든 사용됨";
            else
                hiddenButtonText.text = _useHidden ? "히든 ON" : "히든 OFF";
        }
    }
}
#endif
