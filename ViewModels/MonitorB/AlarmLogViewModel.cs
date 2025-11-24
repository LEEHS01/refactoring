using Models;
using Services;
using Repositories.MonitorB;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;  // ⭐ 추가
using System;              // ⭐ 추가

namespace ViewModels.MonitorB
{
    public class AlarmLogViewModel : MonoBehaviour
    {
        public static AlarmLogViewModel Instance { get; private set; }

        // Repository 추가
        private AlarmLogRepository repository = new AlarmLogRepository();

        public List<AlarmLogData> AllLogs { get; private set; } = new List<AlarmLogData>();
        public List<AlarmLogData> FilteredLogs { get; private set; } = new List<AlarmLogData>();

        public event System.Action OnLogsChanged;

        // ⭐⭐⭐ 추가: 알람 선택 이벤트 (Monitor A/B 상호작용용)
        [Serializable]
        public class AlarmSelectedEvent : UnityEvent<int> { }  // obsId 전달

        [HideInInspector]
        public AlarmSelectedEvent OnAlarmSelected = new AlarmSelectedEvent();

        // 필터 상태 저장
        private string currentAreaFilter = null;
        private int? currentStatusFilter = null;

        private void Awake()
        {
            Debug.Log("[AlarmLogViewModel] Awake 시작");

            if (Instance != null && Instance != this)
            {
                Debug.Log("[AlarmLogViewModel] 중복 인스턴스 제거");
                Destroy(this);
                return;
            }

            Instance = this;
            Debug.Log("[AlarmLogViewModel] Instance 설정 완료");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ⭐⭐⭐ 추가: 알람 선택 메서드 (View에서 호출)
        public void SelectAlarm(int alarmIdx)
        {
            var alarm = AllLogs.Find(a => a.logId == alarmIdx);

            if (alarm == null)
            {
                Debug.LogWarning($"[AlarmLogViewModel] 알람을 찾을 수 없음: logId={alarmIdx}");
                return;
            }

            Debug.Log($"[AlarmLogViewModel] ✅ 알람 선택: ObsId={alarm.obsId}, Sensor={alarm.sensorName}");

            // ⭐ 이벤트 발생 (Monitor A/B 모두 자동 업데이트!)
            OnAlarmSelected?.Invoke(alarm.obsId);
        }

        // 기존 코드 그대로...
        public void LoadAlarmLogs()
        {
            if (DatabaseService.Instance == null)
            {
                Debug.LogError("[AlarmLogViewModel] DatabaseService가 null입니다.");
                AllLogs = new List<AlarmLogData>();
                FilteredLogs = new List<AlarmLogData>();
                OnLogsChanged?.Invoke();
                return;
            }

            Debug.Log("[AlarmLogViewModel] 알람 로그 로드 시작");

            StartCoroutine(repository.GetHistoricalAlarmLogs(OnLoadSuccess, OnLoadFailed));
        }

        private void OnLoadSuccess(List<AlarmLogModel> models)
        {
            if (models == null)
            {
                Debug.LogWarning("[AlarmLogViewModel] 받은 데이터가 null입니다.");
                AllLogs = new List<AlarmLogData>();
                FilteredLogs = new List<AlarmLogData>();
                OnLogsChanged?.Invoke();
                return;
            }

            AllLogs = models.Select(m => new AlarmLogData
            {
                logId = m.ALAIDX,
                obsId = m.OBSIDX,
                sensorId = m.HNSIDX,
                boardId = m.BOARDIDX,
                status = m.ALACODE,
                time = m.ALADT,
                cancelTime = m.TURNOFF_DT,
                isCancelled = !string.IsNullOrEmpty(m.TURNOFF_FLAG) && m.TURNOFF_FLAG.Trim() == "Y",
                areaName = m.AREANM ?? "",
                obsName = m.OBSNM ?? "",
                sensorName = m.HNSNM ?? "",
                alarmValue = m.CURRVAL,
                warningThreshold = m.ALAHIVAL,      
                criticalThreshold = m.ALAHIHIVAL   
            }).ToList();

            ApplyFilters();

            Debug.Log($"[AlarmLogViewModel] 데이터 로드 완료: {AllLogs.Count}개");
        }

        private void OnLoadFailed(string error)
        {
            Debug.LogError($"[AlarmLogViewModel] 데이터 로드 실패: {error}");
            AllLogs = new List<AlarmLogData>();
            FilteredLogs = new List<AlarmLogData>();
            OnLogsChanged?.Invoke();
        }

        #region Filtering
        // 기존 코드 그대로...
        public void FilterByArea(string areaName)
        {
            currentAreaFilter = areaName;
            ApplyFilters();
        }

        public void FilterByStatus(int? status)
        {
            currentStatusFilter = status;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (AllLogs == null || AllLogs.Count == 0)
            {
                FilteredLogs = new List<AlarmLogData>();
                OnLogsChanged?.Invoke();
                return;
            }

            IEnumerable<AlarmLogData> filtered = AllLogs;

            if (!string.IsNullOrEmpty(currentAreaFilter))
            {
                filtered = filtered.Where(log => log.areaName == currentAreaFilter);
            }

            if (currentStatusFilter.HasValue)
            {
                filtered = filtered.Where(log => log.status == currentStatusFilter.Value);
            }

            FilteredLogs = filtered.ToList();

            Debug.Log($"[AlarmLogViewModel] 필터 적용 완료 - 지역: {currentAreaFilter ?? "전체"}, 상태: {currentStatusFilter?.ToString() ?? "전체"}, 결과: {FilteredLogs.Count}개");

            OnLogsChanged?.Invoke();
        }
        #endregion

        #region Sorting
        // 기존 코드 그대로...
        public void SortByTime(bool ascending)
        {
            if (FilteredLogs == null || FilteredLogs.Count == 0) return;
            FilteredLogs = ascending ? FilteredLogs.OrderBy(x => x.time).ToList() : FilteredLogs.OrderByDescending(x => x.time).ToList();
            OnLogsChanged?.Invoke();
        }

        public void SortByContent(bool ascending)
        {
            if (FilteredLogs == null || FilteredLogs.Count == 0) return;
            FilteredLogs = ascending ? FilteredLogs.OrderBy(x => x.sensorName).ToList() : FilteredLogs.OrderByDescending(x => x.sensorName).ToList();
            OnLogsChanged?.Invoke();
        }

        public void SortByArea(bool ascending)
        {
            if (FilteredLogs == null || FilteredLogs.Count == 0) return;
            FilteredLogs = ascending ? FilteredLogs.OrderBy(x => x.areaName).ToList() : FilteredLogs.OrderByDescending(x => x.areaName).ToList();
            OnLogsChanged?.Invoke();
        }

        public void SortByObservatory(bool ascending)
        {
            if (FilteredLogs == null || FilteredLogs.Count == 0) return;
            FilteredLogs = ascending ? FilteredLogs.OrderBy(x => x.obsName).ToList() : FilteredLogs.OrderByDescending(x => x.obsName).ToList();
            OnLogsChanged?.Invoke();
        }

        public void SortByStatus(bool ascending)
        {
            if (FilteredLogs == null || FilteredLogs.Count == 0) return;
            FilteredLogs = ascending ? FilteredLogs.OrderBy(x => x.status).ToList() : FilteredLogs.OrderByDescending(x => x.status).ToList();
            OnLogsChanged?.Invoke();
        }
        #endregion
    }
}