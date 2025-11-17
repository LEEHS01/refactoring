using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.MonitorA.Models;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ⭐ Models.MonitorB의 ObservatoryModel 사용
using ObservatoryModel = Models.MonitorB.ObservatoryModel;

namespace HNS.MonitorA.Repositories
{
    /// <summary>
    /// 지역 알람 차트 Repository
    /// - 12개월 알람 히스토리 조회
    /// - DatabaseService 직접 사용 (레거시 ModelProvider 사용 안 함)
    /// </summary>
    public class AreaAlarmChartRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// 지역 차트 데이터 조회 (12개월 히스토리)
        /// </summary>
        public IEnumerator GetAreaChartData(
            int areaId,
            Action<AreaChartData> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[AreaAlarmChartRepository] ========== 차트 데이터 조회 시작 ==========");
            Debug.Log($"[AreaAlarmChartRepository] 요청 AreaId: {areaId}");

            // 1. GET_OBS로 해당 지역의 관측소 조회
            List<ObservatoryModel> observatories = null;
            bool obsCompleted = false;

            yield return Database.ExecuteProcedure<ObservatoryModel>(
                "GET_OBS",
                null,
                (models) =>
                {
                    observatories = models
                        .Where(o => o.AREAIDX == areaId)
                        .Take(3) // 최대 3개
                        .ToList();
                    obsCompleted = true;
                },
                (error) =>
                {
                    Debug.LogError($"[AreaAlarmChartRepository] GET_OBS 실패: {error}");
                    onError?.Invoke($"관측소 조회 실패: {error}");
                    obsCompleted = true;
                }
            );

            // 완료 대기
            while (!obsCompleted)
                yield return null;

            if (observatories == null || observatories.Count == 0)
            {
                string error = $"관측소를 찾을 수 없습니다: AreaId={areaId}";
                Debug.LogError($"[AreaAlarmChartRepository] {error}");
                onError?.Invoke(error);
                yield break;
            }

            Debug.Log($"[AreaAlarmChartRepository] 관측소 개수: {observatories.Count}");
            for (int i = 0; i < observatories.Count; i++)
            {
                Debug.Log($"[AreaAlarmChartRepository]   [{i}] {observatories[i].OBSNM} (ID: {observatories[i].OBSIDX})");
            }

            // 지역명 추출 (ObservatoryModel의 AREANM 사용)
            string areaName = observatories[0].AREANM ?? "Unknown";
            Debug.Log($"[AreaAlarmChartRepository] 지역명: {areaName}");

            // 2. GET_ALARM_SUMMARY로 12개월 알람 통계 조회
            var parameters = new Dictionary<string, object>
            {
                { "areaid", areaId }
            };

            List<AlarmSummaryDbModel> alarmSummaries = null;
            bool alarmCompleted = false;

            // 일단 ExecuteProcedure로 시도!
            yield return Database.ExecuteProcedure<AlarmSummaryDbModel>(
                "GET_ALARM_SUMMARY",
                parameters,
                (models) =>
                {
                    alarmSummaries = models ?? new List<AlarmSummaryDbModel>();
                    alarmCompleted = true;
                    Debug.Log($"[AreaAlarmChartRepository] 알람 통계 개수: {alarmSummaries.Count}");
                },
                (error) =>
                {
                    Debug.LogWarning($"[AreaAlarmChartRepository] GET_ALARM_SUMMARY 경고: {error}");
                    alarmSummaries = new List<AlarmSummaryDbModel>();
                    alarmCompleted = true;
                }
            );

            // 완료 대기
            while (!alarmCompleted)
                yield return null;

            Debug.Log($"[AreaAlarmChartRepository] 알람 통계 개수: {alarmSummaries.Count}");

            // 3. 데이터 조합
            AreaChartData chartData = BuildChartData(areaId, areaName, observatories, alarmSummaries);

            Debug.Log($"[AreaAlarmChartRepository] ========== 차트 데이터 조회 완료 ==========");
            onSuccess?.Invoke(chartData);
        }

        /// <summary>
        /// 차트 데이터 빌드
        /// </summary>
        private AreaChartData BuildChartData(
            int areaId,
            string areaName,
            List<ObservatoryModel> observatories,
            List<AlarmSummaryDbModel> alarmSummaries)
        {
            var chartData = new AreaChartData
            {
                AreaName = areaName
            };

            // 관측소 이름 (최대 3개)
            chartData.ObservatoryNames = observatories
                .Take(3)
                .Select(obs => obs.OBSNM)
                .ToList();

            // 부족한 경우 빈 문자열로 채우기
            while (chartData.ObservatoryNames.Count < 3)
            {
                chartData.ObservatoryNames.Add("");
            }

            // 현재 월 기준 최근 12개월 생성
            DateTime now = DateTime.Now;
            var recent12Months = new List<(int year, int month)>();
            for (int i = 11; i >= 0; i--)
            {
                var dt = now.AddMonths(-i);
                recent12Months.Add((dt.Year, dt.Month));
            }

            // 월 라벨 생성 (YY/MM 형식)
            chartData.MonthLabels = recent12Months
                .Select(m => $"{m.year.ToString().Substring(2)}/{m.month:D2}")
                .ToList();

            // 각 월별 데이터 생성
            chartData.MonthlyData = new List<MonthlyAlarmData>();

            foreach (var (year, month) in recent12Months)
            {
                var monthData = new MonthlyAlarmData(year, month);

                // 각 관측소별 알람 수 집계
                for (int i = 0; i < Math.Min(3, observatories.Count); i++)
                {
                    int obsId = observatories[i].OBSIDX;

                    // 해당 월의 알람 수 찾기
                    int alarmCount = alarmSummaries
                        .Where(s => s.OBSIDX == obsId && s.YEAR == year && s.MONTH == month)
                        .Sum(s => s.CNT);

                    // 관측소별 할당
                    if (i == 0) monthData.ObsA_Count = alarmCount;
                    else if (i == 1) monthData.ObsB_Count = alarmCount;
                    else if (i == 2) monthData.ObsC_Count = alarmCount;
                }

                chartData.MonthlyData.Add(monthData);
            }

            // 최대값 계산 (Y축 스케일용)
            chartData.MaxAlarmCount = chartData.MonthlyData
                .SelectMany(m => new[] { m.ObsA_Count, m.ObsB_Count, m.ObsC_Count })
                .DefaultIfEmpty(0)
                .Max();

            // 최대값이 0이면 기본값 설정
            if (chartData.MaxAlarmCount == 0)
            {
                chartData.MaxAlarmCount = 10;
            }

            // 정규화 (0.0 ~ 1.0)
            float maxValue = chartData.MaxAlarmCount;
            foreach (var monthData in chartData.MonthlyData)
            {
                monthData.ObsA_Normalized = (maxValue > 0) ? (monthData.ObsA_Count / maxValue) : 0f;
                monthData.ObsB_Normalized = (maxValue > 0) ? (monthData.ObsB_Count / maxValue) : 0f;
                monthData.ObsC_Normalized = (maxValue > 0) ? (monthData.ObsC_Count / maxValue) : 0f;
            }

            Debug.Log($"[AreaAlarmChartRepository] 차트 데이터 빌드 완료");
            Debug.Log($"  - 지역: {chartData.AreaName}");
            Debug.Log($"  - 관측소: {string.Join(", ", chartData.ObservatoryNames)}");
            Debug.Log($"  - 최대값: {chartData.MaxAlarmCount}");

            return chartData;
        }
    }
}