using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using HNS.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HNS.MonitorA.ViewModels
{
    public class YearlyAlarmTop5ViewModel : MonoBehaviour
    {
        public static YearlyAlarmTop5ViewModel Instance { get; private set; }

        private SchedulerService schedulerService;
        private YearlyAlarmRankingRepository repository;

        public event Action<List<YearlyAlarmRankingData>> OnRankingUpdated;
        public event Action<string> OnTitleUpdated;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            repository = new YearlyAlarmRankingRepository();

            Debug.Log("[YearlyAlarmTop5ViewModel] Awake 완료");
        }

        private void Start()
        {
            schedulerService = FindFirstObjectByType<SchedulerService>();

            if (schedulerService == null)
            {
                Debug.LogWarning("[YearlyAlarmTop5ViewModel] SchedulerService를 찾을 수 없습니다.");
            }
            else
            {
                // 알람 발생 시에만 구독
                schedulerService.OnAlarmDetected += HandleAlarmChange;
                Debug.Log("[YearlyAlarmTop5ViewModel] SchedulerService 이벤트 구독 완료");
            }

            UpdateTitle();
            StartCoroutine(LoadYearlyRanking());
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
            Debug.Log("[YearlyAlarmTop5ViewModel] 알람 변경 감지 - 연간 통계 갱신");
            StartCoroutine(LoadYearlyRanking());
        }

        private void UpdateTitle()
        {
            int currentYear = DateTime.Now.Year % 100;
            string title = $"연간 발생 건수 ({currentYear}년)";
            OnTitleUpdated?.Invoke(title);
        }

        private IEnumerator LoadYearlyRanking()
        {
            yield return repository.GetYearlyAlarmStats(
                stats =>
                {
                    if (stats == null || stats.Count == 0)
                    {
                        Debug.LogWarning("[YearlyAlarmTop5ViewModel] 연간 알람 통계 데이터가 없습니다.");
                        OnRankingUpdated?.Invoke(new List<YearlyAlarmRankingData>());
                        return;
                    }

                    var top5 = stats
                        .OrderByDescending(s => s.TotalCount)
                        .Take(5)
                        .ToList();

                    int totalCount = top5.Sum(s => s.TotalCount);

                    var rankingList = new List<YearlyAlarmRankingData>();
                    for (int i = 0; i < top5.Count; i++)
                    {
                        var stat = top5[i];
                        float percentage = totalCount > 0 ? (stat.TotalCount / (float)totalCount) * 100f : 0f;

                        rankingList.Add(new YearlyAlarmRankingData(
                            rank: i + 1,
                            areaName: stat.AreaName,
                            total: stat.TotalCount,
                            purple: stat.PurpleCount,
                            yellow: stat.YellowCount,
                            red: stat.RedCount,
                            percentage: percentage,
                            obsIdx: stat.ObservatoryIndex
                        ));
                    }

                    OnRankingUpdated?.Invoke(rankingList);
                },
                error => Debug.LogError($"[YearlyAlarmTop5ViewModel] 에러: {error}")
            );
        }

        public void RefreshRanking()
        {
            UpdateTitle();
            StartCoroutine(LoadYearlyRanking());
        }
    }
}