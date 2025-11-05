using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Models;
using Repositories.MonitorB;
using ViewModels.MonitorB;

namespace HNS.Services
{
    /// <summary>
    /// 통합 데이터 서비스
    /// - 스케줄러 이벤트 처리
    /// - 알람 변경 감지 및 UI 업데이트
    /// </summary>
    public class DataService : MonoBehaviour
    {
        [Header("Service Dependencies")]
        [SerializeField] private SchedulerService schedulerService;

        [Header("Update Control")]
        [SerializeField] private float minUpdateIntervalSeconds = 5f; // 최소 업데이트 간격

        // Repository
        private AlarmLogRepository _alarmRepository;

        // 알람 상태 캐싱
        private List<AlarmLogModel> _previousAlarms = new List<AlarmLogModel>();
        private DateTime _lastFullUpdate = DateTime.MinValue;
        private DateTime _lastAlarmCheck = DateTime.MinValue;

        #region Unity 생명주기

        private void Awake()
        {
            _alarmRepository = new AlarmLogRepository();
        }

        private void Start()
        {
            SetupSchedulerEvents();
            Debug.Log("[DataService] 데이터 서비스 시작");
        }

        private void OnDestroy()
        {
            CleanupSchedulerEvents();
            Debug.Log("[DataService] 데이터 서비스 종료");
        }

        #endregion

        #region 스케줄러 이벤트 연결

        /// <summary>
        /// 스케줄러 이벤트 연결
        /// </summary>
        private void SetupSchedulerEvents()
        {
            if (schedulerService == null)
            {
                Debug.LogError("[DataService] SchedulerService가 null입니다! Inspector에서 연결하세요.");
                return;
            }

            // 5초 주기: 실시간 알람 체크
            schedulerService.OnRealtimeCheckTriggered += HandleRealtimeCheckTriggered;

            // 10분 주기: 데이터 동기화
            schedulerService.OnDataSyncTriggered += HandleDataSyncTriggered;

            // 알람 발생/해제 이벤트
            schedulerService.OnAlarmDetected += HandleAlarmDetected;
            schedulerService.OnAlarmCancelled += HandleAlarmCancelled;

            Debug.Log("[DataService] 스케줄러 이벤트 연결 완료");
        }

        /// <summary>
        /// 스케줄러 이벤트 해제
        /// </summary>
        private void CleanupSchedulerEvents()
        {
            if (schedulerService != null)
            {
                schedulerService.OnRealtimeCheckTriggered -= HandleRealtimeCheckTriggered;
                schedulerService.OnDataSyncTriggered -= HandleDataSyncTriggered;
                schedulerService.OnAlarmDetected -= HandleAlarmDetected;
                schedulerService.OnAlarmCancelled -= HandleAlarmCancelled;
            }
        }

        #endregion

        #region 스케줄러 이벤트 핸들러

        /// <summary>
        /// 5초마다: 실시간 알람 체크
        /// </summary>
        private void HandleRealtimeCheckTriggered()
        {
            Debug.Log("[DataService] 실시간 알람 체크 시작");
            StartCoroutine(CheckAlarmsRoutine());
        }

        /// <summary>
        /// 10분마다: 전체 데이터 동기화
        /// </summary>
        private void HandleDataSyncTriggered()
        {
            Debug.Log("[DataService] 10분 주기 데이터 동기화 시작");
            RefreshAllMonitorBViews();
        }

        /// <summary>
        /// 알람 발생 시: 즉시 UI 업데이트
        /// </summary>
        private void HandleAlarmDetected()
        {
            Debug.Log("[DataService] 알람 발생 - 즉시 업데이트");
            RefreshAllMonitorBViews();
        }

        /// <summary>
        /// 알람 해제 시: 즉시 UI 업데이트
        /// </summary>
        private void HandleAlarmCancelled()
        {
            Debug.Log("[DataService] 알람 해제 - 즉시 업데이트");
            RefreshAllMonitorBViews();
        }

        #endregion

        #region 알람 체크 로직

        /// <summary>
        /// 알람 변경사항 체크 (5초마다)
        /// </summary>
        private IEnumerator CheckAlarmsRoutine()
        {
            DateTime now = DateTime.Now;
            DateTime checkFrom = _lastAlarmCheck == DateTime.MinValue
                ? now.AddSeconds(-5)
                : _lastAlarmCheck;

            bool alarmsChanged = false;
            List<AlarmLogModel> changedLogs = null;

            // 알람 변경사항 조회
            yield return _alarmRepository.GetHistoricalAlarmLogs(
                logs =>
                {
                    if (logs != null && logs.Count > 0)
                    {
                        // 이전 알람과 비교하여 변경사항 감지
                        var currentAlarmIds = logs.Select(l => l.ALAIDX).ToHashSet();
                        var previousAlarmIds = _previousAlarms.Select(l => l.ALAIDX).ToHashSet();

                        // 신규 알람 (이전에 없던 ALAIDX)
                        var newAlarms = logs.Where(l => !previousAlarmIds.Contains(l.ALAIDX)).ToList();

                        // 해제된 알람 (TURNOFF_FLAG가 null/N → Y로 변경된 것만)
                        var cancelledAlarms = logs.Where(l =>
                        {
                            // 이전 알람 찾기
                            var prevAlarm = _previousAlarms.FirstOrDefault(p => p.ALAIDX == l.ALAIDX);

                            if (prevAlarm == null) return false; // 신규 알람은 제외

                            // 이전 상태: 활성 (null 또는 N)
                            bool wasActive = string.IsNullOrEmpty(prevAlarm.TURNOFF_FLAG) ||
                                           prevAlarm.TURNOFF_FLAG.Trim() != "Y";

                            // 현재 상태: 해제됨 (Y)
                            bool isNowCancelled = !string.IsNullOrEmpty(l.TURNOFF_FLAG) &&
                                                l.TURNOFF_FLAG.Trim() == "Y";

                            // 상태가 변경된 경우만 (활성 → 해제)
                            return wasActive && isNowCancelled;
                        }).ToList();

                        if (newAlarms.Count > 0)
                        {
                            Debug.Log($"[DataService] 신규 알람 {newAlarms.Count}개 발생");
                            alarmsChanged = true;
                            schedulerService?.TriggerAlarmDetected();
                        }

                        if (cancelledAlarms.Count > 0)
                        {
                            Debug.Log($"[DataService] 알람 {cancelledAlarms.Count}개 해제");
                            alarmsChanged = true;
                            schedulerService?.TriggerAlarmCancelled();
                        }

                        // 현재 알람 상태를 저장 (다음 비교를 위해)
                        _previousAlarms = logs.ToList(); // ⭐ ToList()로 복사
                        changedLogs = newAlarms.Concat(cancelledAlarms).ToList();
                    }
                },
                error =>
                {
                    Debug.LogError($"[DataService] 알람 체크 실패: {error}");
                }
            );

            _lastAlarmCheck = now;

            // 알람 변경이 있으면 즉시 UI 업데이트 (이벤트는 이미 발생했으므로 여기서는 로그만)
            if (alarmsChanged && changedLogs != null)
            {
                Debug.Log($"[DataService] 알람 변경 감지: {changedLogs.Count}개");
            }
        }

        #endregion

        #region UI 업데이트

        /// <summary>
        /// 모니터 B의 모든 View 새로고침
        /// - SensorMonitorView
        /// - AlarmLogView
        /// - (SensorChartView는 제외: 사용자가 선택한 차트만 업데이트)
        /// </summary>
        private void RefreshAllMonitorBViews()
        {
            // 너무 잦은 업데이트 방지 (최소 5초 간격)
            var timeSinceLastUpdate = (DateTime.Now - _lastFullUpdate).TotalSeconds;

            if (timeSinceLastUpdate < minUpdateIntervalSeconds)
            {
                Debug.Log($"[DataService] 업데이트 스킵 (마지막 업데이트로부터 {timeSinceLastUpdate:F1}초 경과)");
                return;
            }

            _lastFullUpdate = DateTime.Now;

            Debug.Log("[DataService] 전체 View 업데이트 시작...");

            // 1. 센서 모니터링 View 업데이트
            if (SensorMonitorViewModel.Instance != null && SensorMonitorViewModel.Instance.CurrentObsId > 0)
            {
                Debug.Log($"[DataService] 센서 데이터 새로고침: ObsId={SensorMonitorViewModel.Instance.CurrentObsId}");
                SensorMonitorViewModel.Instance.RefreshSensors();
            }
            else
            {
                Debug.LogWarning("[DataService] SensorMonitorViewModel이 null이거나 관측소가 선택되지 않았습니다.");
            }

            // 2. 알람 로그 View 업데이트
            if (AlarmLogViewModel.Instance != null)
            {
                Debug.Log("[DataService] 알람 로그 새로고침");
                AlarmLogViewModel.Instance.LoadAlarmLogs();
            }
            else
            {
                Debug.LogWarning("[DataService] AlarmLogViewModel이 null입니다.");
            }

            // 3. 차트는 제외 (사용자가 현재 보고 있는 차트만 업데이트되어야 함)
            // SensorChartView는 센서 선택 시에만 업데이트되므로 여기서는 제외

            Debug.Log("[DataService] 전체 View 업데이트 완료");
        }

        /// <summary>
        /// 특정 관측소의 센서 데이터만 업데이트
        /// </summary>
        public void RefreshObservatorySensors(int obsId)
        {
            if (obsId <= 0)
            {
                Debug.LogWarning($"[DataService] 잘못된 관측소 ID: {obsId}");
                return;
            }

            Debug.Log($"[DataService] 관측소 {obsId} 센서 데이터 업데이트");

            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.LoadSensorsByObservatory(obsId);
            }
        }

        /// <summary>
        /// 차트 캐시 무효화 (알람 발생 시)
        /// </summary>
        public void InvalidateChartCache()
        {
            Debug.Log("[DataService] 차트 캐시 무효화");

            if (SensorChartViewModel.Instance != null)
            {
                SensorChartViewModel.Instance.InvalidateCache();
            }
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 수동 전체 데이터 새로고침
        /// </summary>
        public void ManualRefresh()
        {
            Debug.Log("[DataService] 수동 새로고침 요청");
            _lastFullUpdate = DateTime.MinValue; // 간격 제한 무시
            RefreshAllMonitorBViews();
        }

        /// <summary>
        /// 최소 업데이트 간격 설정
        /// </summary>
        public void SetMinUpdateInterval(float seconds)
        {
            if (seconds < 1f || seconds > 60f)
            {
                Debug.LogWarning($"[DataService] 잘못된 최소 업데이트 간격: {seconds}초. 1~60초 범위여야 합니다.");
                return;
            }

            minUpdateIntervalSeconds = seconds;
            Debug.Log($"[DataService] 최소 업데이트 간격 설정: {seconds}초");
        }

        #endregion

        #region 디버그/모니터링

        /// <summary>
        /// 현재 상태 출력
        /// </summary>
        [ContextMenu("Print Status")]
        public void PrintStatus()
        {
            Debug.Log("===== DataService Status =====");
            Debug.Log($"마지막 전체 업데이트: {_lastFullUpdate:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"마지막 알람 체크: {_lastAlarmCheck:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"이전 알람 수: {_previousAlarms.Count}개");
            Debug.Log($"최소 업데이트 간격: {minUpdateIntervalSeconds}초");
            Debug.Log("=============================");
        }

        #endregion
    }
}