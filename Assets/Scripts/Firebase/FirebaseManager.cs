using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using WordPuzzle.Save;

namespace WordPuzzle.Firebase
{
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        public bool   IsReady { get; private set; }
        public string UserId  { get; private set; }

        private const string DbUrl = "https://word-puzzle-d492d-default-rtdb.asia-southeast1.firebasedatabase.app";

        private FirebaseAuth      _auth;
        private DatabaseReference _db;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[Firebase] 의존성 오류: {task.Result}");
                    return;
                }

                _auth = FirebaseAuth.DefaultInstance;
                _db   = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, DbUrl).RootReference;

                IsReady = true;
                Debug.Log("[Firebase] 초기화 완료");
                SignInAnonymously();
            });
        }

        // ── 인증 ────────────────────────────────────────────────────────────

        public void SignInAnonymously()
        {
            if (!IsReady) return;

            if (_auth.CurrentUser != null)
            {
                UserId = _auth.CurrentUser.UserId;
                Debug.Log($"[Firebase] 기존 세션 재사용 uid={UserId}");
                SyncLocalToFirebase();
                return;
            }

            _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"[Firebase] 익명 로그인 실패: {task.Exception?.GetBaseException().Message}");
                    return;
                }
                UserId = _auth.CurrentUser?.UserId;
                Debug.Log($"[Firebase] 익명 로그인 성공 uid={UserId}");
                SyncLocalToFirebase();
            });
        }

        // ── 로드 ────────────────────────────────────────────────────────────

        // 전체 유저 데이터 로드 후 콜백 반환
        public void LoadUserData(Action<DataSnapshot> onLoaded)
        {
            if (!CheckReady()) return;
            _db.Child("users").Child(UserId)
               .GetValueAsync().ContinueWithOnMainThread(task =>
               {
                   if (task.IsFaulted)
                       Debug.LogError($"[Firebase] 데이터 로드 실패: {task.Exception?.GetBaseException().Message}");
                   else
                       onLoaded?.Invoke(task.Result);
               });
        }

        // ── 저장 ────────────────────────────────────────────────────────────

        public void SaveNickname(string nickname)
        {
            if (!CheckReady()) return;
            _db.Child("users").Child(UserId).Child("nickname")
               .SetValueAsync(nickname)
               .ContinueWithOnMainThread(task =>
               {
                   if (task.IsFaulted)
                       Debug.LogError($"[Firebase] 닉네임 저장 실패: {task.Exception?.GetBaseException().Message}");
                   else
                       Debug.Log($"[Firebase] 닉네임 저장 완료: {nickname}");
               });
        }

        // 멀티 매치 결과 — win/lose/score 누적
        public void SaveMatchResult(bool isWin, int score)
        {
            if (!CheckReady()) return;

            var multiRef = _db.Child("users").Child(UserId).Child("multi");
            multiRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[Firebase] 매치 데이터 읽기 실패: {task.Exception?.GetBaseException().Message}");
                    return;
                }

                var snap  = task.Result;
                int win   = ParseInt(snap, "win");
                int lose  = ParseInt(snap, "lose");
                int total = ParseInt(snap, "score");

                if (isWin) win++; else lose++;
                total += score;

                multiRef.UpdateChildrenAsync(new Dictionary<string, object>
                {
                    ["win"]       = win,
                    ["lose"]      = lose,
                    ["score"]     = total,
                    ["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }).ContinueWithOnMainThread(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"[Firebase] 매치 결과 저장 실패: {t.Exception?.GetBaseException().Message}");
                    else
                        Debug.Log($"[Firebase] 매치 결과 저장 완료 win:{win} lose:{lose} score:{total}");
                });
            });
        }

        // 싱글 플레이 데이터 저장
        public void SaveSingleData(SingleSaveData data)
        {
            if (!CheckReady()) return;

            var clearByLen = new Dictionary<string, object>();
            for (int i = 2; i < data.ClearCountByLength.Length; i++)
                clearByLen[i.ToString()] = data.ClearCountByLength[i];

            _db.Child("users").Child(UserId).Child("single")
               .UpdateChildrenAsync(new Dictionary<string, object>
               {
                   ["clearByLength"]  = clearByLen,
                   ["clearedWords"]   = data.ClearedWordIds.Count,
                   ["totalHintsUsed"] = data.TotalHintsUsed,
                   ["points"]         = data.Points,
                   ["updatedAt"]      = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
               }).ContinueWithOnMainThread(t =>
               {
                   if (t.IsFaulted)
                       Debug.LogError($"[Firebase] 싱글 데이터 저장 실패: {t.Exception?.GetBaseException().Message}");
                   else
                       Debug.Log("[Firebase] 싱글 데이터 저장 완료");
               });
        }

        // 일일 도전 데이터 저장
        public void SaveDailyData(DailySaveData data)
        {
            if (!CheckReady()) return;

            _db.Child("users").Child(UserId).Child("daily")
               .UpdateChildrenAsync(new Dictionary<string, object>
               {
                   ["lastPlayDate"]    = data.LastPlayDate,
                   ["isClearedToday"] = data.IsClearedToday,
                   ["streakDays"]     = data.StreakDays,
                   ["todayAttempts"]  = data.TodayAttempts,
                   ["updatedAt"]      = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
               }).ContinueWithOnMainThread(t =>
               {
                   if (t.IsFaulted)
                       Debug.LogError($"[Firebase] 일일 데이터 저장 실패: {t.Exception?.GetBaseException().Message}");
                   else
                       Debug.Log("[Firebase] 일일 데이터 저장 완료");
               });
        }

        // 멀티 전체 데이터 덮어쓰기 (SaveManager.SaveMulti 호출 시)
        public void SyncMultiData(MultiSaveData data)
        {
            if (!CheckReady()) return;
            _db.Child("users").Child(UserId).Child("multi")
               .UpdateChildrenAsync(new Dictionary<string, object>
               {
                   ["win"]       = data.WinCount,
                   ["lose"]      = data.LoseCount,
                   ["playCount"] = data.PlayCount,
                   ["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
               }).ContinueWithOnMainThread(t =>
               {
                   if (t.IsFaulted)
                       Debug.LogError($"[Firebase] 멀티 데이터 저장 실패: {t.Exception?.GetBaseException().Message}");
                   else
                       Debug.Log("[Firebase] 멀티 데이터 저장 완료");
               });
        }

        // 로그인 후 로컬 → Firebase 한 번 동기화
        private void SyncLocalToFirebase()
        {
            SaveSingleData(SaveManager.LoadSingle());
            SaveDailyData(SaveManager.LoadDaily());
            SyncMultiData(SaveManager.LoadMulti());

            string nick = PlayerPrefs.GetString("PlayerNickname", "");
            if (!string.IsNullOrEmpty(nick)) SaveNickname(nick);
        }

        // ── 헬퍼 ────────────────────────────────────────────────────────────

        private static int ParseInt(DataSnapshot snap, string key)
        {
            if (!snap.HasChild(key)) return 0;
            return int.TryParse(snap.Child(key).Value?.ToString(), out int v) ? v : 0;
        }

        private bool CheckReady()
        {
            if (!IsReady || string.IsNullOrEmpty(UserId))
            {
                Debug.LogWarning("[Firebase] 아직 준비되지 않음");
                return false;
            }
            return true;
        }

        [ContextMenu("테스트: 닉네임·승리·점수 저장")]
        private void TestSave()
        {
            SaveNickname("TestUser");
            SaveMatchResult(isWin: true, score: 100);
        }
    }
}
