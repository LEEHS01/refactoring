using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Services;
using Models;

namespace HNS.MonitorA.Repositories
{
    /// <summary>
    /// 월간 알람 통계 Repository
    /// DB에서 월간 알람 발생 건수 조회
    /// </summary>
    public class MonthlyAlarmRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// GET_ALARM_MONTHLY를 사용한 월간 통계 조회
        /// (현재 월의 지역별 알람 개수)
        /// </summary>
        public IEnumerator GetCurrentMonthStats(
            Action<List<HNS.MonitorA.Models.MonthlyAlarmStatData>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[MonthlyAlarmRepository] 월간 통계 조회 시작");

            // GET_ALARM_MONTHLY 호출 (파라미터 없음)
            yield return Database.ExecuteProcedure<AlarmMonthlyModel>(
                "GET_ALARM_MONTHLY",
                null,  // 파라미터 없음
                (List<AlarmMonthlyModel> monthlyData) =>
                {
                    if (monthlyData == null || monthlyData.Count == 0)
                    {
                        Debug.LogWarning("[MonthlyAlarmRepository] 월간 통계 데이터가 없습니다.");
                        onSuccess?.Invoke(new List<HNS.MonitorA.Models.MonthlyAlarmStatData>());
                        return;
                    }

                    // AlarmMonthlyModel → MonthlyAlarmStatData 변환
                    var statList = new List<HNS.MonitorA.Models.MonthlyAlarmStatData>();
                    var now = DateTime.Now;

                    foreach (var data in monthlyData)
                    {
                        statList.Add(new HNS.MonitorA.Models.MonthlyAlarmStatData
                        {
                            ObservatoryIndex = 0,  // OBSIDX는 없지만 지역명으로 구분 가능
                            AreaName = data.areanm ?? "Unknown",
                            AlarmCount = data.cnt,
                            Year = now.Year,
                            Month = now.Month
                        });
                    }

                    Debug.Log($"[MonthlyAlarmRepository] 집계 완료: {statList.Count}개 지역");
                    foreach (var stat in statList)
                    {
                        Debug.Log($"  - {stat.AreaName}: {stat.AlarmCount}건");
                    }

                    onSuccess?.Invoke(statList);
                },
                onError
            );
        }
    }

    /// <summary>
    /// GET_ALARM_MONTHLY 결과 모델
    /// </summary>
    [Serializable]
    public class AlarmMonthlyModel
    {
        public string areanm;  // 지역명
        public int cnt;        // 알람 개수
    }
}