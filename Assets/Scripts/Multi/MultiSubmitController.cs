#if PHOTON_UNITY_NETWORKING
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
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
        private bool         _hiddenUsed;
        private int          _currentTurn;

        private void Start()
        {
            if (submitButton) submitButton.onClick.AddListener(OnSubmitButtonClicked);
            if (hiddenButton) hiddenButton.onClick.AddListener(OnHiddenButtonClicked);
            // 키보드 Enter 로도 제출
            if (inputField) inputField.onSubmit.AddListener(_ => OnSubmitButtonClicked());
        }

        public void ResetForGame()
        {
            _hiddenUsed               = false;
            _useHidden                = false;
            if (hiddenButton) hiddenButton.interactable = false;
            UpdateHiddenButtonUI();
        }

        // 새 턴: isMyTurn이 true일 때만 입력 활성화
        public void Setup(List<string> answerTokens, int turnIndex, bool isMyTurn)
        {
            _answerTokens  = answerTokens;
            _currentTurn   = turnIndex;
            _hasSubmitted  = false;
            _useHidden     = false;

            if (inputField)    { inputField.text = "";  inputField.interactable   = isMyTurn; }
            if (submitButton)    submitButton.interactable = isMyTurn;
            if (hiddenButton)    hiddenButton.interactable = isMyTurn && !_hiddenUsed;
            UpdateHiddenButtonUI();

            if (isMyTurn && inputField) StartCoroutine(ActivateInputNextFrame());
        }

        private IEnumerator ActivateInputNextFrame()
        {
            yield return null; // 레이아웃 완료 대기
            yield return null; // SelectionPanel 비활성화 완료 보장
            if (inputField == null || !inputField.interactable || !inputField.gameObject.activeInHierarchy)
                yield break;
            // EventSystem에 명시적으로 등록 후 활성화 (포커스 누락 방지)
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(inputField.gameObject);
            inputField.ActivateInputField();
        }

        public void OnHiddenButtonClicked()
        {
            if (_hiddenUsed || _hasSubmitted) return;
            _useHidden = !_useHidden;
            UpdateHiddenButtonUI();
        }

        public void OnSubmitButtonClicked()
        {
            if (_hasSubmitted) return;
            string input = inputField != null ? inputField.text.Trim() : "";
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

            if (_useHidden)
            {
                _hiddenUsed = true;
                _useHidden  = false;
                if (hiddenButton) hiddenButton.interactable = false;

                // 히든: 나 자신에게는 원본(내 기록에서 결과 확인 가능), 상대에게만 히든 버전
                PhotonNetwork.RaiseEvent(
                    MultiNetworkEvents.SubmitTurn,
                    submit.Serialize(),
                    new RaiseEventOptions
                    {
                        TargetActors = new int[] { PhotonNetwork.LocalPlayer.ActorNumber }
                    },
                    MultiNetworkEvents.Reliable);
                PhotonNetwork.RaiseEvent(
                    MultiNetworkEvents.SubmitTurn,
                    submit.ToHiddenView().Serialize(),
                    MultiNetworkEvents.Others,
                    MultiNetworkEvents.Reliable);
            }
            else
            {
                PhotonNetwork.RaiseEvent(
                    MultiNetworkEvents.SubmitTurn,
                    submit.Serialize(),
                    MultiNetworkEvents.All,
                    MultiNetworkEvents.Reliable);
            }

            _hasSubmitted = true;
            if (inputField)   inputField.interactable   = false;
            if (submitButton) submitButton.interactable = false;
            UpdateHiddenButtonUI();
        }

        private void UpdateHiddenButtonUI()
        {
            if (hiddenButtonText == null) return;
            if (_hiddenUsed)
                hiddenButtonText.text = "히든 사용됨";
            else
                hiddenButtonText.text = _useHidden ? "히든 ON" : "히든 아이템";
        }
    }
}
#endif
