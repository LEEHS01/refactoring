using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System;
using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 월간 알람 Top 5 ViewModel
    /// Repository 코루틴 호출 및 랭킹 계산
    /// </summary>
    public class MonthlyAlarmTop5ViewModel : MonoBehaviour
    {
        public static MonthlyAlarmTop5ViewModel Instance { get; private set; }

        [Header("Events")]
        public UnityEvent<List<MonthlyAlarmRankingData>> OnRankingUpdated;

        [Header("Settings")]
        [SerializeField] private float autoRefreshInterval = 60f; // 자동 갱신 간격 (초)

        private MonthlyAlarmRepository repository;
        private float refreshTimer;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            repository = new MonthlyAlarmRepository();

            // UnityEvent 초기화 (중요!)
            if (OnRankingUpdated == null)
                OnRankingUpdated = new UnityEvent<List<MonthlyAlarmRankingData>>();
        }

        private void Start()
        {
            // 초기 로드
            LoadMonthlyRanking();
        }

        private void Update()
        {
            // 자동 갱신
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= autoRefreshInterval)
            {
                refreshTimer = 0f;
                LoadMonthlyRanking();
            }
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
            refreshTimer = 0f;
            LoadMonthlyRanking();
        }
    }
}