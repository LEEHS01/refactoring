using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Services;
using Models.MonitorB;
using HNS.MonitorA.Models;
using HNS.Common.Models;
using ObservatoryModel = Models.MonitorB.ObservatoryModel;

namespace HNS.MonitorA.Repositories
{
    public class AreaRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        public IEnumerator GetAllAreaObservatoryStatus(
            Action<List<AreaObservatoryStatusData>> onSuccess,
            Action<string> onError)
        {
            Debug.Log("[AreaRepository] 지역별 관측소 현황 조회 시작");

            // ===== 1단계: 관측소 목록 =====
            List<ObservatoryModel> allObs = null;
            bool obsComplete = false;
            string obsError = null;

            yield return Database.ExecuteProcedure(
                "GET_OBS",
                null,
                (List<ObservatoryModel> obs) =>
                {
                    allObs = obs;
                    obsComplete = true;
                    Debug.Log($"[AreaRepository] 관측소 {obs.Count}개 조회 완료");
                },
                (err) =>
                {
                    obsError = err;
                    obsComplete = true;
                }
            );

            while (!obsComplete) yield return null;

            if (!string.IsNullOrEmpty(obsError))
            {
                Debug.LogError($"[AreaRepository] 관측소 조회 실패: {obsError}");
                onError?.Invoke(obsError);
                yield break;
            }

            if (allObs == null || allObs.Count == 0)
            {
                Debug.LogWarning("[AreaRepository] 관측소 데이터가 없습니다.");
                onSuccess?.Invoke(new List<AreaObservatoryStatusData>());
                yield break;
            }

            // ===== 2단계: 현재 활성 알람 =====
            List<Models.AlarmLogModel> activeAlarms = null;  
            bool alarmComplete = false;
            string alarmError = null;

            yield return Database.ExecuteProcedure(
                "GET_CURRENT_ALARM_LOG",
                null,
                (List<Models.AlarmLogModel> alarms) =>  
                {
                    activeAlarms = alarms;
                    alarmComplete = true;
                    Debug.Log($"[AreaRepository] 활성 알람 {alarms.Count}개 조회 완료");
                },
                (err) =>
                {
                    alarmError = err;
                    alarmComplete = true;
                }
            );

            while (!alarmComplete) yield return null;

            if (!string.IsNullOrEmpty(alarmError))
            {
                Debug.LogError($"[AreaRepository] 알람 조회 실패: {alarmError}");
                onError?.Invoke(alarmError);
                yield break;
            }

            if (activeAlarms == null)
            {
                activeAlarms = new List<Models.AlarmLogModel>(); 
            }

            // ===== 3단계: 지역별 집계 =====
            var areaStats = allObs
                .GroupBy(obs => new
                {
                    AreaId = obs.AREAIDX,
                    AreaName = obs.AREANM ?? "Unknown",
                    AreaType = GetAreaType(obs.AREANM)
                })
                .Select(group => new AreaObservatoryStatusData
                {
                    AreaId = group.Key.AreaId,
                    AreaName = group.Key.AreaName,
                    AreaType = (Common.Models.AreaData.AreaType)group.Key.AreaType,
                    GreenCount = group.Count(obs => GetObsStatusFromAlarms(obs.OBSIDX, activeAlarms) == 0),
                    YellowCount = group.Count(obs => GetObsStatusFromAlarms(obs.OBSIDX, activeAlarms) == 1),
                    RedCount = group.Count(obs => GetObsStatusFromAlarms(obs.OBSIDX, activeAlarms) == 2),
                    PurpleCount = group.Count(obs => GetObsStatusFromAlarms(obs.OBSIDX, activeAlarms) == 3)
                })
                .ToList();

            Debug.Log($"[AreaRepository] 집계 완료: {areaStats.Count}개 지역");

            foreach (var stat in areaStats)
            {
                int total = stat.GreenCount + stat.YellowCount + stat.RedCount + stat.PurpleCount;
                Debug.Log($"  - {stat.AreaName} ({stat.AreaType}): " +
                    $"정상={stat.GreenCount}, 경계={stat.YellowCount}, " +
                    $"경보={stat.RedCount}, 설비이상={stat.PurpleCount} (총 {total}개)");
            }

            onSuccess?.Invoke(areaStats);
        }

        /// <summary>
        /// 알람 리스트에서 관측소 상태 판단
        /// </summary>
        private int GetObsStatusFromAlarms(int obsId, List<Models.AlarmLogModel> alarms)  
        {
            var obsAlarms = alarms.Where(a => a.OBSIDX == obsId).ToList();

            if (obsAlarms.Count == 0)
                return 0; // Green

            // 우선순위: 경보(2) > 경계(1) > 설비이상(0)
            if (obsAlarms.Any(a => a.ALACODE == 2))
                return 2; // Red

            if (obsAlarms.Any(a => a.ALACODE == 1))
                return 1; // Yellow

            if (obsAlarms.Any(a => a.ALACODE == 0))
                return 3; // Purple

            return 0; // Green
        }

        private AreaData.AreaType GetAreaType(string areaName)
        {
            if (string.IsNullOrEmpty(areaName))
                return AreaData.AreaType.Ocean;

            if (areaName.Contains("원자력") ||
                areaName.Contains("화력") ||
                areaName.Contains("발전소"))
            {
                return AreaData.AreaType.Nuclear;
            }

            return AreaData.AreaType.Ocean;
        }
    }
}