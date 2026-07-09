using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    public class StarParticleSystem : MonoBehaviour
    {
        [SerializeField] private int   maxStars  = 50;
        [SerializeField] private float spawnRate = 0.2f;
        [SerializeField] private float minSize   = 20f;
        [SerializeField] private float maxSize   = 55f;
        [SerializeField] private float minFadeIn = 0.8f;
        [SerializeField] private float maxFadeIn = 2.0f;
        [SerializeField] private float minStay   = 1.5f;
        [SerializeField] private float maxStay   = 4.0f;
        [SerializeField] private float minFadeOut= 1.0f;
        [SerializeField] private float maxFadeOut= 2.0f;
        [SerializeField] private float driftSpeed= 12f;
        [SerializeField] private int   preSpawn  = 25;

        private RectTransform _rt;
        private int _active;

        private void Awake() => _rt = GetComponent<RectTransform>();

        private void Start()
        {
            for (int i = 0; i < preSpawn; i++)
                StartCoroutine(DelayedSpawn(Random.Range(0f, 2.5f)));
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator DelayedSpawn(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_active < maxStars) StartCoroutine(StarLife());
        }

        private IEnumerator SpawnLoop()
        {
            // 매 반복 new WaitForSeconds 대신 캐시된 인스턴스 재사용(GC 절감)
            var wait = new WaitForSeconds(spawnRate);
            while (true)
            {
                if (_active < maxStars) StartCoroutine(StarLife());
                yield return wait;
            }
        }

        private static RectTransform MakeUIChild(string name, Transform parent)
        {
            // new GameObject(name, typeof(RectTransform)) 으로 생성하면 처음부터 RectTransform
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private IEnumerator StarLife()
        {
            _active++;

            // 루트: 위치/크기 컨테이너
            var starRT = MakeUIChild("Star", transform);
            float size = Random.Range(minSize, maxSize);
            starRT.sizeDelta = new Vector2(size, size);

            Rect area = _rt.rect;
            if (area.width < 1f) area = new Rect(-540f, -960f, 1080f, 1920f);
            starRT.anchoredPosition = new Vector2(
                Random.Range(area.xMin, area.xMax),
                Random.Range(area.yMin, area.yMax)
            );

            // 글로우 (큰 원, 낮은 투명도)
            var glowRT  = MakeUIChild("Glow", starRT);
            glowRT.sizeDelta        = new Vector2(size * 3f, size * 3f);
            glowRT.anchoredPosition = Vector2.zero;
            var glowImg = glowRT.gameObject.AddComponent<Image>();

            // 코어 (작은 원, 밝음)
            var coreRT  = MakeUIChild("Core", starRT);
            coreRT.sizeDelta        = new Vector2(size, size);
            coreRT.anchoredPosition = Vector2.zero;
            var coreImg = coreRT.gameObject.AddComponent<Image>();

            float b   = Random.Range(0.85f, 1f);
            Color col = new Color(b, b, b + 0.04f, 0f);
            Color gcl = new Color(0.65f, 0.72f, 1f,  0f);
            coreImg.color = col;
            glowImg.color = gcl;

            float fadeIn  = Random.Range(minFadeIn, maxFadeIn);
            float stay    = Random.Range(minStay,   maxStay);
            float fadeOut = Random.Range(minFadeOut, maxFadeOut);
            float twinkle = Random.Range(1.5f, 4f);
            Vector2 drift = new Vector2(Random.Range(-5f, 5f), driftSpeed);

            // 페이드 인
            for (float t = 0f; t < fadeIn; t += Time.deltaTime)
            {
                if (starRT == null) yield break;
                float a   = Mathf.SmoothStep(0f, 1f, t / fadeIn);
                col.a = a; gcl.a = a * 0.28f;
                coreImg.color = col; glowImg.color = gcl;
                starRT.anchoredPosition += drift * Time.deltaTime;
                yield return null;
            }

            // 유지 + 반짝임
            for (float t = 0f; t < stay; t += Time.deltaTime)
            {
                if (starRT == null) yield break;
                float a   = 0.8f + 0.2f * Mathf.Sin(t * twinkle);
                col.a = a; gcl.a = a * 0.28f;
                coreImg.color = col; glowImg.color = gcl;
                starRT.anchoredPosition += drift * Time.deltaTime;
                yield return null;
            }

            // 페이드 아웃
            for (float t = 0f; t < fadeOut; t += Time.deltaTime)
            {
                if (starRT == null) yield break;
                float a   = Mathf.SmoothStep(1f, 0f, t / fadeOut);
                col.a = a; gcl.a = a * 0.28f;
                coreImg.color = col; glowImg.color = gcl;
                starRT.anchoredPosition += drift * Time.deltaTime;
                yield return null;
            }

            if (starRT != null) Destroy(starRT.gameObject);
            _active--;
        }
    }
}
