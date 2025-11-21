using HNS.Common.Models;
using HNS.Services;
using Models;
using Repositories.MonitorB;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AlarmLogModel = Models.AlarmLogModel;

namespace ViewModels.Common
{
    /// <summary>
    /// 알람 팝업 ViewModel
    /// v2.0: 초기 알람 로드 + 최근 알람만 팝업 표시
    /// </summary>
    public class PopupAlarmViewModel : MonoBehaviour
    {
        #region Singleton
        public static PopupAlarmViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _repository = new AlarmLogRepository();

            Debug.Log("[PopupAlarmViewModel] 초기화 완료");
        }

        private void OnDestroy()
        {
            UnsubscribeFromScheduler();

            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Repository & Services
        private AlarmLogRepository _repository;
        private SchedulerService _schedulerService;
        #endregion

        #region Current Data
        private List<AlarmLogData> _previousAlarms = new List<AlarmLogData>();
        private AlarmLogData _currentAlarm = null;
        private bool _isInitialLoadComplete = false;  // ✅ 초기 로드 완료 플래그
        #endregion

        #region Properties
        public AlarmLogData CurrentAlarm => _currentAlarm;
        #endregion

        #region Events
        public event Action<AlarmLogData> OnNewAlarmDetected;
        public event Action<string> OnAlarmTimeUpdated;
        public event Action OnAlarmCleared;
        public event Action<string> OnError;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // ✅ 1. 먼저 초기 알람 로드
            StartCoroutine(LoadInitialAlarms());

            // ✅ 2. 스케줄러 구독
            SubscribeToScheduler();
        }
        #endregion

        #region Scheduler Events
        private void SubscribeToScheduler()
        {
            _schedulerService = FindFirstObjectByType<SchedulerService>();

            if (_schedulerService == null)
            {
                Debug.LogWarning("[PopupAlarmViewModel] SchedulerService를 찾을 수 없습니다!");
                return;
            }

            _schedulerService.OnRealtimeCheckTriggered += OnRealtimeCheck;
            Debug.Log("[PopupAlarmViewModel] ✅ SchedulerService 구독 완료");
        }

        private void UnsubscribeFromScheduler()
        {
            if (_schedulerService != null)
            {
                _schedulerService.OnRealtimeCheckTriggered -= OnRealtimeCheck;
            }
        }

        private void OnRealtimeCheck()
        {
            // ✅ 초기 로드 완료 전에는 체크 안 함
            if (!_isInitialLoadComplete)
            {
                Debug.Log("[PopupAlarmViewModel] 초기 로드 대기 중...");
                return;
            }

            Debug.Log($"[PopupAlarmViewModel] ⭐ OnRealtimeCheck 호출됨 - {DateTime.Now:HH:mm:ss}");
            StartCoroutine(CheckNewAlarms());
        }
        #endregion

        #region Initial Load
        /// <summary>
        /// 초기 알람 목록 로드 (앱 시작 시 과거 알람 필터링용)
        /// </summary>
        private System.Collections.IEnumerator LoadInitialAlarms()
        {
            Debug.Log("[PopupAlarmViewModel] ✅ 초기 알람 로드 시작...");

            bool isSuccess = false;
            List<AlarmLogModel> models = null;

            yield return _repository.GetHistoricalAlarmLogs(
                onSuccess: (m) =>
                {
                    models = m;
                    isSuccess = true;
                },
                onError: (error) =>
                {
                    Debug.LogError($"[PopupAlarmViewModel] 초기 알람 로드 실패: {error}");
                }
            );

            if (isSuccess && models != null)
            {
                _previousAlarms = models.Select(m => AlarmLogData.FromModel(m)).ToList();
                Debug.Log($"[PopupAlarmViewModel] ✅ 초기 알람 {_previousAlarms.Count}개 로드 완료");
            }

            _isInitialLoadComplete = true;
            Debug.Log("[PopupAlarmViewModel] ✅ 초기 로드 완료 - 스케줄러 체크 활성화");
        }
        #endregion

        #region Alarm Detection
        private System.Collections.IEnumerator CheckNewAlarms()
        {
            Debug.Log("[PopupAlarmViewModel] CheckNewAlarms 시작");
            Debug.Log($"[PopupAlarmViewModel] 이전 알람 개수: {_previousAlarms.Count}");

            bool isSuccess = false;
            List<AlarmLogModel> currentAlarmModels = null;

            yield return _repository.GetHistoricalAlarmLogs(
                onSuccess: (models) =>
                {
                    currentAlarmModels = models;
                    isSuccess = true;
                },
                onError: (error) =>
                {
                    Debug.LogError($"[PopupAlarmViewModel] 알람 조회 실패: {error}");
                    OnError?.Invoke(error);
                }
            );

            if (!isSuccess || currentAlarmModels == null)
            {
                Debug.LogWarning("[PopupAlarmViewModel] 알람 조회 실패 또는 데이터 없음");
                yield break;
            }

            List<AlarmLogData> currentAlarms = currentAlarmModels
                .Select(m => AlarmLogData.FromModel(m))
                .ToList();

            Debug.Log($"[PopupAlarmViewModel] 현재 알람 개수: {currentAlarms.Count}");

            // 신규 알람 감지
            List<AlarmLogData> newAlarms = currentAlarms
                .Where(current => !_previousAlarms.Any(prev => prev.logId == current.logId))
                .ToList();

            Debug.Log($"[PopupAlarmViewModel] 신규 알람 개수: {newAlarms.Count}");

            if (newAlarms.Count > 0)
            {
                foreach (var alarm in newAlarms)
                {
                    Debug.Log($"[PopupAlarmViewModel] 신규 알람 ID: {alarm.logId}, 시간: {alarm.time:HH:mm:ss}");
                }

                bool alarmShown = false;

                // 최신 알람부터 역순으로 검사
                for (int i = newAlarms.Count - 1; i >= 0; i--)
                {
                    if (TryShowAlarm(newAlarms[i]))
                    {
                        _previousAlarms.Add(newAlarms[i]);
                        alarmShown = true;
                        break;
                    }
                }

                // 표시 안 된 알람들도 추가
                if (!alarmShown)
                {
                    foreach (var alarm in newAlarms)
                    {
                        if (!_previousAlarms.Any(prev => prev.logId == alarm.logId))
                        {
                            _previousAlarms.Add(alarm);
                        }
                    }
                }
            }
        }

        private bool TryShowAlarm(AlarmLogData alarmData)
        {
            // ✅ 1. 최근 5분 이내 알람만 표시 (과거 알람 필터링)
            TimeSpan elapsed = DateTime.Now - alarmData.time;
            if (elapsed.TotalMinutes > 5)
            {
                Debug.Log($"[PopupAlarmViewModel] 오래된 알람 ({elapsed.TotalMinutes:F1}분 전), 팝업 표시 안함");
                return false;
            }

            // ✅ 2. 상태 확인
            ToxinStatus toxinStatus = GetToxinStatus(alarmData.status);
            if (toxinStatus < ToxinStatus.Yellow)
            {
                Debug.Log($"[PopupAlarmViewModel] 알람 레벨 낮음 ({toxinStatus}), 팝업 표시 안함");
                return false;
            }

            // ✅ 3. 중복 체크
            if (_currentAlarm != null && _currentAlarm.logId == alarmData.logId)
            {
                Debug.Log($"[PopupAlarmViewModel] 이미 표시중인 알람, 무시");
                return false;
            }

            // ✅ 4. 알람 표시
            _currentAlarm = alarmData;
            Debug.Log($"[PopupAlarmViewModel] ✅ 신규 알람 팝업 표시: {_currentAlarm.sensorName} ({toxinStatus})");
            OnNewAlarmDetected?.Invoke(_currentAlarm);

            return true;
        }

        private ToxinStatus GetToxinStatus(int status)
        {
            switch (status)
            {
                case 0: return ToxinStatus.Purple;
                case 1: return ToxinStatus.Yellow;
                case 2: return ToxinStatus.Red;
                default: return ToxinStatus.Green;
            }
        }
        #endregion

        #region Time Formatting
        public string GetFormattedTimeAgo(DateTime alarmTime)
        {
            TimeSpan elapsed = DateTime.Now - alarmTime;

            if (elapsed.TotalMinutes < 1)
                return "방금 전";
            else if (elapsed.TotalMinutes < 60)
                return $"{(int)elapsed.TotalMinutes}분 전";
            else if (elapsed.TotalHours < 24)
                return $"{(int)elapsed.TotalHours}시간 전";
            else
                return $"{(int)elapsed.TotalDays}일 전";
        }

        public void UpdateCurrentAlarmTime()
        {
            if (_currentAlarm == null)
                return;

            string timeAgo = GetFormattedTimeAgo(_currentAlarm.time);
            Debug.Log($"[PopupAlarmViewModel] 경과 시간 업데이트: {timeAgo}");
            OnAlarmTimeUpdated?.Invoke(timeAgo);
        }
        #endregion

        #region Public Methods
        public void SelectCurrentAlarm()
        {
            if (_currentAlarm == null)
            {
                Debug.LogWarning("[PopupAlarmViewModel] 현재 알람이 없습니다!");
                return;
            }

            Debug.Log($"[PopupAlarmViewModel] ✅ 알람 선택: LogId={_currentAlarm.logId}, ObsId={_currentAlarm.obsId}");

            if (ViewModels.MonitorB.AlarmLogViewModel.Instance != null)
            {
                ViewModels.MonitorB.AlarmLogViewModel.Instance.SelectAlarm(_currentAlarm.logId);
            }
            else
            {
                Debug.LogWarning("[PopupAlarmViewModel] AlarmLogViewModel.Instance가 null!");
            }
        }

        public void ClearCurrentAlarm()
        {
            Debug.Log("[PopupAlarmViewModel] 알람 클리어");
            _currentAlarm = null;
            OnAlarmCleared?.Invoke();
        }
        #endregion
    }
}