using Onthesys;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Models.MonitorA;

namespace Repositories.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 데이터 Repository
    /// ✅ GET_SENSOR_INFO 통합 프로시저 사용 (Monitor B와 동일)
    /// </summary>
    public class ObsMonitoringRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// 센서 정보 + 현재값 통합 조회 (GET_SENSOR_INFO)
        /// ✅ Monitor B와 동일한 프로시저 사용 - 데이터 일관성 보장!
        /// </summary>
        public IEnumerator GetSensorInfo(
            int obsId,
            Action<List<SensorInfoModelA>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[ObsMonitoringRepository] GetSensorInfo: ObsId={obsId}");

            var parameters = new Dictionary<string, object>
            {
                { "OBSIDX", obsId }
            };

            yield return Database.ExecuteProcedure<SensorInfoModelA>(
                "GET_SENSOR_INFO",
                parameters,
                models =>
                {
                    if (models == null || models.Count == 0)
                    {
                        onError?.Invoke("센서 데이터가 없습니다.");
                        return;
                    }

                    Debug.Log($"[ObsMonitoringRepository] GetSensorInfo 성공: {models.Count}개");
                    onSuccess?.Invoke(models);
                },
                onError
            );
        }

        /// <summary>
        /// 차트 데이터 조회 (GET_CHARTVALUE)
        /// </summary>
        public IEnumerator GetChartValue(
            int obsId,
            DateTime startTime,
            DateTime endTime,
            int intervalMin,
            Action<List<ChartDataModel>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[ObsMonitoringRepository] GetChartValue: ObsId={obsId}");

            var parameters = new Dictionary<string, object>
            {
                { "obsidx", obsId },
                { "start_dt", startTime.ToString("yyyyMMddHHmmss") },
                { "end_dt", endTime.ToString("yyyyMMddHHmmss") },
                { "interval", intervalMin }
            };

            yield return Database.ExecuteProcedure<ChartDataModel>(
                "GET_CHARTVALUE",
                parameters,
                models =>
                {
                    if (models == null || models.Count == 0)
                    {
                        Debug.LogWarning("[ObsMonitoringRepository] GetChartValue 결과 없음");
                        onSuccess?.Invoke(new List<ChartDataModel>());
                        return;
                    }

                    Debug.Log($"[ObsMonitoringRepository] GetChartValue 성공: {models.Count}개");
                    onSuccess?.Invoke(models);
                },
                onError
            );
        }

        /// <summary>
        /// 센서 진행 상태 조회 (GET_SENSOR_STEP)
        /// </summary>
        public IEnumerator GetSensorStep(
            int obsId,
            Action<int> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[ObsMonitoringRepository] GetSensorStep: ObsId={obsId}");

            var parameters = new Dictionary<string, object>
            {
                { "obsid", obsId }
            };

            yield return Database.ExecuteProcedure<SensorStepModel>(
                "GET_SENSOR_STEP",
                parameters,
                results =>
                {
                    if (results == null || results.Count == 0)
                    {
                        Debug.LogWarning("[ObsMonitoringRepository] GetSensorStep 결과 없음");
                        onSuccess?.Invoke(5);
                        return;
                    }

                    string stepStr = results[0].toxistep;
                    int step = ConvertStepToInt(stepStr);

                    Debug.Log($"[ObsMonitoringRepository] GetSensorStep 성공: {step}");
                    onSuccess?.Invoke(step);
                },
                error =>
                {
                    Debug.LogWarning($"[ObsMonitoringRepository] GetSensorStep 실패");
                    onSuccess?.Invoke(5);
                }
            );
        }

        private int ConvertStepToInt(string stepStr)
        {
            if (string.IsNullOrEmpty(stepStr))
                return 5;

            if (int.TryParse(stepStr, out int stepValue))
            {
                if (stepValue >= 20 && stepValue <= 25)
                    return stepValue - 20;

                if (stepValue >= 0 && stepValue <= 5)
                    return stepValue;
            }

            return 5;
        }
    }
}