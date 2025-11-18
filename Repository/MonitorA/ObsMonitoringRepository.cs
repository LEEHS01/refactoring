using Onthesys;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Models.MonitorA;
using ChartDataModel = Models.MonitorA.ChartDataModel;

namespace Repositories.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 데이터 Repository
    /// DatabaseService.ExecuteProcedure 사용 (리팩토링된 코드)
    /// </summary>
    public class ObsMonitoringRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// 센서 정보 조회 (GET_SETTING)
        /// </summary>
        public IEnumerator GetToxinData(
            int obsId,
            Action<List<ToxinData>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[ObsMonitoringRepository] GetToxinData: ObsId={obsId}");

            var parameters = new Dictionary<string, object>
            {
                { "obsidx", obsId }
            };

            yield return Database.ExecuteProcedure<HnsResourceModel>(
                "GET_SETTING",
                parameters,
                models =>
                {
                    if (models == null || models.Count == 0)
                    {
                        onError?.Invoke("센서 데이터가 없습니다.");
                        return;
                    }

                    // HnsResourceModel → ToxinData 변환
                    var toxinDataList = new List<ToxinData>();
                    foreach (var model in models)
                    {
                        toxinDataList.Add(new ToxinData(model, model.unit ?? ""));
                    }

                    Debug.Log($"[ObsMonitoringRepository] GetToxinData 성공: {toxinDataList.Count}개");
                    onSuccess?.Invoke(toxinDataList);
                },
                onError
            );
        }

        /// <summary>
        /// 최신 측정값 조회 (GET_CURRENT_TOXI)
        /// </summary>
        public IEnumerator GetToxinValueLast(
            int obsId,
            Action<List<CurrentDataModel>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[ObsMonitoringRepository] GetToxinValueLast: ObsId={obsId}");

            var parameters = new Dictionary<string, object>
            {
                { "obsidx", obsId }
            };

            yield return Database.ExecuteProcedure<CurrentDataModel>(
                "GET_CURRENT_TOXI",
                parameters,
                models =>
                {
                    if (models == null || models.Count == 0)
                    {
                        onError?.Invoke("측정값 데이터가 없습니다.");
                        return;
                    }

                    Debug.Log($"[ObsMonitoringRepository] GetToxinValueLast 성공: {models.Count}개");
                    onSuccess?.Invoke(models);
                },
                onError
            );
        }

        /// <summary>
        /// 차트 데이터 조회 (GET_CHARTVALUE) ⭐ 추가!
        /// </summary>
        public IEnumerator GetChartValue(
            int obsId,
            DateTime startTime,
            DateTime endTime,
            int intervalMin,
            Action<List<ChartDataModel>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[ObsMonitoringRepository] GetChartValue: ObsId={obsId}, {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}");

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
                        Debug.LogWarning("[ObsMonitoringRepository] GetSensorStep 결과 없음 - 기본값 5 반환");
                        onSuccess?.Invoke(5); // 기본값: 정상 운영 중
                        return;
                    }

                    // toxistep을 int로 변환
                    string stepStr = results[0].toxistep;
                    int step = ConvertStepToInt(stepStr);

                    Debug.Log($"[ObsMonitoringRepository] GetSensorStep 성공: {step}");
                    onSuccess?.Invoke(step);
                },
                error =>
                {
                    Debug.LogWarning($"[ObsMonitoringRepository] GetSensorStep 실패 - 기본값 5 반환: {error}");
                    onSuccess?.Invoke(5); // 에러 시에도 기본값 반환
                }
            );
        }

        /// <summary>
        /// Step 문자열을 int로 변환
        /// "25" → 5, "20" → 0, "21" → 1, ...
        /// </summary>
        private int ConvertStepToInt(string stepStr)
        {
            if (string.IsNullOrEmpty(stepStr))
                return 5;

            if (int.TryParse(stepStr, out int stepValue))
            {
                // "25" → 5, "20" → 0, "21" → 1
                if (stepValue >= 20 && stepValue <= 25)
                    return stepValue - 20;

                // 이미 0~5 범위면 그대로 반환
                if (stepValue >= 0 && stepValue <= 5)
                    return stepValue;
            }

            return 5; // 기본값
        }
    }
}