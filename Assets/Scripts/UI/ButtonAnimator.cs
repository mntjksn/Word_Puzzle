using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordPuzzle.UI
{
    // 버튼 누를 때 스케일 펀치 애니메이션
    public class ButtonAnimator : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressScale  = 0.92f;
        [SerializeField] private float bounceScale = 1.06f;
        [SerializeField] private float duration    = 0.08f;

        private Vector3 _normalScale;
        private Coroutine _tween;

        private void Awake() => _normalScale = transform.localScale;

        public void OnPointerDown(PointerEventData e) => Play(pressScale);

        public void OnPointerUp(PointerEventData e)   => StartCoroutine(Bounce());

        // 드래그로 빠져나갈 때도 복원
        public void OnPointerExit(PointerEventData e)  => Play(1f);

        private void Play(float targetScale)
        {
            if (_tween != null) StopCoroutine(_tween);
            _tween = StartCoroutine(ScaleTo(_normalScale * targetScale, duration));
        }

        // 손 뗄 때 바운스: 눌림 → 살짝 커짐 → 원래 크기
        private IEnumerator Bounce()
        {
            if (_tween != null) StopCoroutine(_tween);
            yield return StartCoroutine(ScaleTo(_normalScale * bounceScale, duration));
            yield return StartCoroutine(ScaleTo(_normalScale, duration));
        }

        private IEnumerator ScaleTo(Vector3 target, float dur)
        {
            var start = transform.localScale;
            float t   = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, target, t / dur);
                yield return null;
            }
            transform.localScale = target;
        }
    }
}
