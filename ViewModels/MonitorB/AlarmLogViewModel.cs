using Models;
using Services;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ViewModels.MonitorB
{
    public class AlarmLogViewModel : MonoBehaviour
    {
        public static AlarmLogViewModel Instance { get; private set; }

        public List<AlarmLogData> AllLogs { get; private set; } = new List<AlarmLogData>();
        public List<AlarmLogData> FilteredLogs { get; private set; } = new List<AlarmLogData>();

        public event System.Action OnLogsChanged;

        // ⭐ 필터 상태 저장
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

        public void LoadAlarmLogs()
        {
            DatabaseService.Instance.ExecuteQuery<List<AlarmLogModel>>(
                "EXEC GET_HISTORICAL_ALARM_LOG;",
                (models) =>
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
                        alarmValue = m.CURRVAL
                    }).ToList();

                    // ⭐ 필터 재적용
                    ApplyFilters();

                    Debug.Log($"[AlarmLogViewModel] 데이터 로드 완료: {AllLogs.Count}개");
                },
                (error) =>
                {
                    Debug.LogError($"[AlarmLogViewModel] 데이터 로드 실패: {error}");
                    AllLogs = new List<AlarmLogData>();
                    FilteredLogs = new List<AlarmLogData>();
                    OnLogsChanged?.Invoke();
                }
            );
        }

        #region Filtering

        /// <summary>
        /// 지역별 필터링
        /// </summary>
        /// <param name="areaName">지역명 (null이면 필터 해제)</param>
        public void FilterByArea(string areaName)
        {
            currentAreaFilter = areaName;
            ApplyFilters();
        }

        /// <summary>
        /// 상태별 필터링
        /// </summary>
        /// <param name="status">상태 코드 (null이면 필터 해제, 0:설비이상, 1:경계, 2:경보)</param>
        public void FilterByStatus(int? status)
        {
            currentStatusFilter = status;
            ApplyFilters();
        }

        /// <summary>
        /// 현재 필터 조건에 맞게 FilteredLogs 업데이트
        /// </summary>
        private void ApplyFilters()
        {
            if (AllLogs == null || AllLogs.Count == 0)
            {
                FilteredLogs = new List<AlarmLogData>();
                OnLogsChanged?.Invoke();
                return;
            }

            // 전체 데이터에서 시작
            IEnumerable<AlarmLogData> filtered = AllLogs;

            // 지역 필터 적용
            if (!string.IsNullOrEmpty(currentAreaFilter))
            {
                filtered = filtered.Where(log => log.areaName == currentAreaFilter);
            }

            // 상태 필터 적용
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