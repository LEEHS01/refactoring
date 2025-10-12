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

        private void Awake()
        {
            Debug.Log("[AlarmLogViewModel] Awake ����");

            if (Instance != null && Instance != this)
            {
                Debug.Log("[AlarmLogViewModel] �ߺ� �ν��Ͻ� ����");
                Destroy(this);
                return;
            }

            Instance = this;
            Debug.Log("[AlarmLogViewModel] Instance ���� �Ϸ�");
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
                        Debug.LogWarning("[AlarmLogViewModel] ���� �����Ͱ� null�Դϴ�.");
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
                        areaName = m.AREANM ?? "�� �� ����",
                        obsName = m.OBSNM ?? "�� �� ����",
                        sensorName = m.HNSNM ?? "�� �� ����",
                        alarmValue = m.CURRVAL
                    }).ToList();

                    FilteredLogs = new List<AlarmLogData>(AllLogs);
                    Debug.Log($"[AlarmLogViewModel] ������ �ε� �Ϸ�: {AllLogs.Count}��");
                    OnLogsChanged?.Invoke();
                },
                (error) =>
                {
                    Debug.LogError($"[AlarmLogViewModel] ������ �ε� ����: {error}");
                    AllLogs = new List<AlarmLogData>();
                    FilteredLogs = new List<AlarmLogData>();
                    OnLogsChanged?.Invoke();
                }
            );
        }

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
    }
}