using HNS.MonitorA.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Services;

namespace HNS.MonitorA.Repositories 
{
    public class YearlyAlarmRankingRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        public IEnumerator GetYearlyAlarmStats(
            Action<List<YearlyAlarmStatData>> onSuccess,
            Action<string> onError)
        {
            Debug.Log("[YearlyAlarmRankingRepository] 연간 통계 조회 시작");

            yield return Database.ExecuteProcedure<AlarmYearlyModel>(
                "GET_ALARM_YEARLY",
                null,
                (List<AlarmYearlyModel> yearlyData) =>
                {
                    if (yearlyData == null || yearlyData.Count == 0)
                    {
                        Debug.LogWarning("[YearlyAlarmRankingRepository] 연간 통계 데이터가 없습니다.");
                        onSuccess?.Invoke(new List<YearlyAlarmStatData>());
                        return;
                    }

                    var statList = new List<YearlyAlarmStatData>();
                    int currentYear = DateTime.Now.Year;

                    foreach (var data in yearlyData)
                    {
                        int total = data.ala0 + data.ala1 + data.ala2;

                        statList.Add(new YearlyAlarmStatData
                        {
                            ObservatoryIndex = 0,
                            AreaName = data.areanm ?? "Unknown",
                            TotalCount = total,
                            PurpleCount = data.ala0,
                            YellowCount = data.ala1,
                            RedCount = data.ala2,
                            Year = currentYear
                        });
                    }

                    Debug.Log($"[YearlyAlarmRankingRepository] 집계 완료: {statList.Count}개 지역");
                    onSuccess?.Invoke(statList);
                },
                onError
            );
        }
    }
}