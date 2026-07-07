using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

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

        // 익명 로그인 — 이미 로그인된 경우 재사용
        public void SignInAnonymously()
        {
            if (!IsReady) return;

            if (_auth.CurrentUser != null)
            {
                UserId = _auth.CurrentUser.UserId;
                Debug.Log($"[Firebase] 기존 세션 재사용 uid={UserId}");
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
            });
        }

        // users/{uid}/nickname 저장
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

        // users/{uid} 아래 win/lose/score/updatedAt 누적 저장
        public void SaveMatchResult(bool isWin, int score)
        {
            if (!CheckReady()) return;

            var userRef = _db.Child("users").Child(UserId);

            userRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[Firebase] 데이터 읽기 실패: {task.Exception?.GetBaseException().Message}");
                    return;
                }

                var snap  = task.Result;
                int win   = ParseInt(snap, "win");
                int lose  = ParseInt(snap, "lose");
                int total = ParseInt(snap, "score");

                if (isWin) win++; else lose++;
                total += score;

                var updates = new Dictionary<string, object>
                {
                    ["win"]       = win,
                    ["lose"]      = lose,
                    ["score"]     = total,
                    ["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                userRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(t =>
                {
                    if (t.IsFaulted)
                        Debug.LogError($"[Firebase] 매치 결과 저장 실패: {t.Exception?.GetBaseException().Message}");
                    else
                        Debug.Log($"[Firebase] 매치 결과 저장 완료 — win:{win} lose:{lose} score:{total}");
                });
            });
        }

        private static int ParseInt(DataSnapshot snap, string key)
        {
            if (!snap.HasChild(key)) return 0;
            return int.TryParse(snap.Child(key).Value?.ToString(), out int v) ? v : 0;
        }

        private bool CheckReady()
        {
            if (!IsReady || string.IsNullOrEmpty(UserId))
            {
                Debug.LogWarning("[Firebase] 아직 준비되지 않음 (IsReady 또는 UserId 없음)");
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
