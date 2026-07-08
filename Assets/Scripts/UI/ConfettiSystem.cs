using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    // 결과창 이펙트 — 승리: 다색 폭죽 낙하, 패배: 잔잔한 어두운 입자
    public class ConfettiSystem : MonoBehaviour
    {
        [SerializeField] private bool  lossMode      = false;
        [SerializeField] private int   burstCount    = 80;
        [SerializeField] private float burstRate     = 0.035f;
        [SerializeField] private int   maxPieces     = 110;
        [SerializeField] private float winFallSpeed  = 460f;
        [SerializeField] private float lossFallSpeed = 85f;

        private static readonly Color[] WinColors =
        {
            new Color(1.00f, 0.86f, 0.20f, 1f), // gold
            new Color(0.18f, 0.72f, 0.64f, 1f), // teal
            new Color(0.45f, 0.72f, 1.00f, 1f), // sky blue
            new Color(1.00f, 1.00f, 1.00f, 1f), // white
            new Color(0.82f, 0.35f, 1.00f, 1f), // purple
            new Color(0.40f, 1.00f, 0.55f, 1f), // green
            new Color(1.00f, 0.42f, 0.62f, 1f), // pink
        };

        private static readonly Color[] LossColors =
        {
            new Color(0.40f, 0.50f, 0.70f, 0.45f),
            new Color(0.30f, 0.35f, 0.55f, 0.40f),
            new Color(0.25f, 0.30f, 0.45f, 0.35f),
        };

        private RectTransform _rt;
        private int           _active;
        private Coroutine     _spawnCoroutine;

        private void Awake() => _rt = GetComponent<RectTransform>();

        private void OnEnable()
        {
            _active = 0;
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void OnDisable()
        {
            if (_spawnCoroutine != null) { StopCoroutine(_spawnCoroutine); _spawnCoroutine = null; }
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            _active = 0;
        }

        // MultiResultScreen 에서 panel.SetActive 전에 호출
        public void SetLossMode(bool isLoss) => lossMode = isLoss;

        private IEnumerator SpawnRoutine()
        {
            if (!lossMode)
            {
                // 초기 버스트
                for (int i = 0; i < burstCount; i++)
                {
                    if (_active < maxPieces) StartCoroutine(PieceLife(false));
                    yield return new WaitForSeconds(burstRate);
                }
                // 지속 낙하
                while (true)
                {
                    if (_active < maxPieces) StartCoroutine(PieceLife(false));
                    yield return new WaitForSeconds(0.14f);
                }
            }
            else
            {
                // 패배: 천천히 떨어지는 어두운 입자
                while (true)
                {
                    if (_active < 28) StartCoroutine(PieceLife(true));
                    yield return new WaitForSeconds(0.28f);
                }
            }
        }

        private IEnumerator PieceLife(bool isLoss)
        {
            _active++;

            var go = new GameObject("P", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;

            Rect area = _rt.rect;
            if (area.width < 1f) area = new Rect(-540f, -960f, 1080f, 1920f);

            Color col;
            float fallSpeed;
            float rotSpeed;
            float w, h;

            if (!isLoss)
            {
                w         = Random.Range(8f, 20f);
                h         = Random.Range(5f, 13f);
                col       = WinColors[Random.Range(0, WinColors.Length)];
                fallSpeed = winFallSpeed * Random.Range(0.65f, 1.45f);
                rotSpeed  = Random.Range(-320f, 320f);
            }
            else
            {
                float s = Random.Range(3f, 7f);
                w = s; h = s;
                col       = LossColors[Random.Range(0, LossColors.Length)];
                fallSpeed = lossFallSpeed * Random.Range(0.6f, 1.4f);
                rotSpeed  = 0f;
            }

            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(
                Random.Range(area.xMin, area.xMax),
                area.yMax + 30f);

            var img = go.AddComponent<Image>();
            img.color = col;

            float drift   = Random.Range(-85f, 85f);
            float lifetime = (area.height + 80f) / fallSpeed;
            float elapsed  = 0f;
            float fadeStart = lifetime * 0.78f;

            while (elapsed < lifetime && go != null)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                var p = rt.anchoredPosition;
                p.y -= fallSpeed * dt;
                p.x += drift * dt;
                rt.anchoredPosition = p;
                if (rotSpeed != 0f) go.transform.Rotate(0f, 0f, rotSpeed * dt);

                if (elapsed > fadeStart)
                {
                    Color c = col;
                    c.a = Mathf.Lerp(col.a, 0f, (elapsed - fadeStart) / (lifetime - fadeStart));
                    img.color = c;
                }
                yield return null;
            }

            if (go != null) Destroy(go);
            _active--;
        }
    }
}
