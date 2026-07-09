using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace WordPuzzle.Data
{
    // words.json을 로드하고 단어 검색 기능 제공
    public class WordDatabase : MonoBehaviour
    {
        public static WordDatabase Instance { get; private set; }

        private List<WordData> _words = new List<WordData>();
        private bool _isLoaded;

        public bool IsLoaded => _isLoaded;

        [Serializable]
        private class WordList { public List<WordData> words; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartCoroutine(LoadWords());

        private IEnumerator LoadWords()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "words.json");
            using var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<WordList>(req.downloadHandler.text);
                _words   = data?.words ?? new List<WordData>();
                _isLoaded = true;
            }
            else
            {
                Debug.LogError($"[WordDatabase] 로드 실패: {req.error}");
            }
        }

        public WordData GetById(int id)
            => _words.Find(w => w.id == id);

        public WordData GetByWord(string word)
            => _words.Find(w => w.word == word);

        public List<WordData> GetByLength(int length)
            => _words.FindAll(w => w.length == length);

        // 글자 수 기준 랜덤 단어 반환
        public WordData GetRandom(int length)
        {
            var list = GetByLength(length);
            if (list.Count == 0) return null;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        // 날짜를 seed로 사용해 오늘의 단어 반환
        public WordData GetDaily(DateTime date)
        {
            if (_words.Count == 0) return null;
            int seed  = int.Parse(date.ToString("yyyyMMdd"));
            int index = Math.Abs(seed) % _words.Count;
            return _words[index];
        }

        // 2~6글자 단어 풀에서 날짜 seed로 일일 도전 단어 반환
        public WordData GetDailyChallenge(DateTime date)
        {
            var pool = _words.FindAll(w => w.length >= 2 && w.length <= 6);
            if (pool.Count == 0) return null;
            int seed  = int.Parse(date.ToString("yyyyMMdd"));
            int index = Math.Abs(seed) % pool.Count;
            return pool[index];
        }
    }
}
