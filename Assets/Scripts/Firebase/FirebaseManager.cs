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
                SyncFirebaseToLocal();
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
                SyncFirebaseToLocal();
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

        // 닉네임 중복 확인 후 저장 — nicknames/{key} 인덱스 활용
        public void CheckAndSaveNickname(string nickname, System.Action<bool, string> callback)
        {
            if (!CheckReady()) { callback?.Invoke(false, "연결 오류"); return; }

            string key     = nickname.ToLower().Replace(" ", "_");
            string oldKey  = PlayerPrefs.GetString("PlayerNickname", "").ToLower().Replace(" ", "_");

            _db.Child("nicknames").Child(key).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    callback?.Invoke(false, "서버 오류");
                    return;
                }

                var snap = task.Result;
                if (snap.Exists && snap.Value?.ToString() != UserId)
                {
                    callback?.Invoke(false, "이미 사용 중인 닉네임입니다.");
                    return;
                }

                var batch = new System.Collections.Generic.Dictionary<string, object>
                {
                    [$"nicknames/{key}"]        = UserId,
                    [$"users/{UserId}/nickname"] = nickname
                };
                if (!string.IsNullOrEmpty(oldKey) && oldKey != key)
                    batch[$"nicknames/{oldKey}"] = null;

                _db.UpdateChildrenAsync(batch).ContinueWithOnMainThread(t =>
                {
                    if (t.IsFaulted) callback?.Invoke(false, "저장 실패");
                    else
                    {
                        Debug.Log($"[Firebase] 닉네임 설정: {nickname}");
                        callback?.Invoke(true, "");
                    }
                });
            });
        }

        // 중복검사 없이 직접 저장 (내부 동기화용)
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

        // ── 로그인 후 동기화 ─────────────────────────────────────────────────

        // Firebase → 로컬 캐시 복원 (재설치 후 데이터 복구)
        // Firebase에 데이터가 없으면 반대 방향(로컬 → Firebase) 초기 업로드
        private void SyncFirebaseToLocal()
        {
            LoadUserData(snap =>
            {
                if (snap == null || !snap.Exists)
                {
                    PushLocalToFirebase();
                    return;
                }

                // 닉네임
                string nick = ParseStr(snap, "nickname");
                if (!string.IsNullOrEmpty(nick))
                    PlayerPrefs.SetString("PlayerNickname", nick);

                // 멀티 전적
                var multi         = SaveManager.LoadMulti();
                multi.WinCount    = ParseInt(snap, "multi/win");
                multi.LoseCount   = ParseInt(snap, "multi/lose");
                multi.PlayCount   = ParseInt(snap, "multi/playCount");
                SaveManager.WriteMultiLocal(multi);

                // 일일 도전
                var daily            = SaveManager.LoadDaily();
                daily.LastPlayDate   = ParseStr(snap,  "daily/lastPlayDate");
                daily.IsClearedToday = ParseBool(snap, "daily/isClearedToday");
                daily.StreakDays     = ParseInt(snap,  "daily/streakDays");
                daily.TodayAttempts  = ParseInt(snap,  "daily/todayAttempts");
                SaveManager.WriteDailyLocal(daily);

                // 싱글: ClearCountByLength·힌트·포인트 (ClearedWordIds는 로컬 전용)
                var single = SaveManager.LoadSingle();
                for (int len = 2; len <= 6; len++)
                    single.ClearCountByLength[len] = ParseInt(snap, $"single/clearByLength/{len}");
                single.TotalHintsUsed = ParseInt(snap, "single/totalHintsUsed");
                single.Points         = ParseInt(snap, "single/points");
                SaveManager.WriteSingleLocal(single);

                PlayerPrefs.Save();
                Debug.Log("[Firebase] 로컬 캐시 복원 완료");
            });
        }

        // 신규 사용자: 로컬 → Firebase 초기 업로드
        private void PushLocalToFirebase()
        {
            SaveSingleData(SaveManager.LoadSingle());
            SaveDailyData(SaveManager.LoadDaily());
            SyncMultiData(SaveManager.LoadMulti());
            string nick = PlayerPrefs.GetString("PlayerNickname", "");
            if (!string.IsNullOrEmpty(nick)) SaveNickname(nick);
            Debug.Log("[Firebase] 신규 사용자: 로컬 → Firebase 초기 업로드");
        }

        // ── 헬퍼 ────────────────────────────────────────────────────────────

        private static int ParseInt(DataSnapshot snap, string path)
        {
            var node = snap;
            foreach (string key in path.Split('/'))
            {
                if (!node.HasChild(key)) return 0;
                node = node.Child(key);
            }
            return int.TryParse(node.Value?.ToString(), out int v) ? v : 0;
        }

        private static bool ParseBool(DataSnapshot snap, string path)
        {
            var node = snap;
            foreach (string key in path.Split('/'))
            {
                if (!node.HasChild(key)) return false;
                node = node.Child(key);
            }
            var val = node.Value;
            if (val is bool b) return b;
            return bool.TryParse(val?.ToString(), out bool result) && result;
        }

        private static string ParseStr(DataSnapshot snap, string path)
        {
            var node = snap;
            foreach (string key in path.Split('/'))
            {
                if (!node.HasChild(key)) return "";
                node = node.Child(key);
            }
            return node.Value?.ToString() ?? "";
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

        [ContextMenu("테스트: 닉네임 저장")]
        private void TestSave() => SaveNickname("TestUser");

        [ContextMenu("테스트: Firebase → 로컬 동기화")]
        private void TestSync() => SyncFirebaseToLocal();
    }
}
