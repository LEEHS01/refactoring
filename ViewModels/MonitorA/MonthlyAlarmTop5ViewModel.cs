using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using HNS.Services;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 월간 알람 Top 5 ViewModel
    /// Repository 코루틴 호출 및 랭킹 계산
    /// </summary>
    public class MonthlyAlarmTop5ViewModel : MonoBehaviour
    {
        public static MonthlyAlarmTop5ViewModel Instance { get; private set; }

        private SchedulerService schedulerService;
        private MonthlyAlarmRepository repository;

        public event Action<List<MonthlyAlarmRankingData>> OnRankingUpdated;
        public event Action<string> OnTitleUpdated;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            repository = new MonthlyAlarmRepository();

            Debug.Log("[MonthlyAlarmTop5ViewModel] Awake 완료");
        }

        private void Start()
        {
            // SchedulerService 자동 검색
            schedulerService = FindFirstObjectByType<SchedulerService>();

            if (schedulerService == null)
            {
                Debug.LogWarning("[MonthlyAlarmTop5ViewModel] SchedulerService를 찾을 수 없습니다. 이벤트 구독 건너뜀");
            }
            else
            {
                // 알람 발생 시에만 구독
                schedulerService.OnAlarmDetected += HandleAlarmChange;
                Debug.Log("[MonthlyAlarmTop5ViewModel] SchedulerService 이벤트 구독 완료");
            }

            // 초기 로드
            UpdateTitle();
            LoadMonthlyRanking();
        }

        private void OnDestroy()
        {
            if (schedulerService != null)
            {
                schedulerService.OnAlarmDetected -= HandleAlarmChange;
            }
        }

        private void HandleAlarmChange()
        {
            Debug.Log("[MonthlyAlarmTop5ViewModel] 알람 발생 감지 - 월간 통계 갱신");
            LoadMonthlyRanking();
        }

        private void UpdateTitle()
        {
            int currentMonth = DateTime.Now.Month;
            string title = $"월간 발생 건수 ({currentMonth}월)";
            OnTitleUpdated?.Invoke(title);
        }

        /// <summary>
        /// 월간 랭킹 로드 (현재 월)
        /// </summary>
        public void LoadMonthlyRanking()
        {
            Debug.Log($"[MonthlyAlarmTop5ViewModel] 월간 Top 5 로딩 시작...");

            StartCoroutine(repository.GetCurrentMonthStats(
                onSuccess: (List<MonthlyAlarmStatData> stats) =>
                {
                    CalculateTop5Ranking(stats);
                },
                onError: (string error) =>
                {
                    Debug.LogError($"[MonthlyAlarmTop5ViewModel] 데이터 로드 실패: {error}");
                    OnRankingUpdated?.Invoke(new List<MonthlyAlarmRankingData>());
                }
            ));
        }

        /// <summary>
        /// Top 5 랭킹 계산 및 View 업데이트
        /// </summary>
        private void CalculateTop5Ranking(List<MonthlyAlarmStatData> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                Debug.LogWarning("[MonthlyAlarmTop5ViewModel] 월간 통계 데이터가 없습니다.");
                OnRankingUpdated?.Invoke(new List<MonthlyAlarmRankingData>());
                return;
            }

            // 알람 개수 기준 내림차순 정렬 후 Top 5
            var top5 = stats
                .OrderByDescending(s => s.AlarmCount)
                .Take(5)
                .ToList();

            // 전체 알람 개수 (비율 계산용)
            int totalCount = top5.Sum(s => s.AlarmCount);

            // 랭킹 데이터 생성
            var rankingList = new List<MonthlyAlarmRankingData>();
            for (int i = 0; i < top5.Count; i++)
            {
                var stat = top5[i];
                float percentage = totalCount > 0 ? (stat.AlarmCount / (float)totalCount) * 100f : 0f;

                rankingList.Add(new MonthlyAlarmRankingData(
                    rank: i + 1,
                    areaName: stat.AreaName,
                    alarmCount: stat.AlarmCount,
                    percentage: percentage,
                    obsIdx: stat.ObservatoryIndex
                ));

                Debug.Log($"🏆 {i + 1}위: {stat.AreaName} - {stat.AlarmCount}건 ({percentage:F1}%)");
            }

            // View에 통지
            OnRankingUpdated?.Invoke(rankingList);
        }

        /// <summary>
        /// 수동 갱신 (외부에서 호출 가능)
        /// </summary>
        public void RefreshRanking()
        {
            UpdateTitle();
            LoadMonthlyRanking();
        }
    }
}