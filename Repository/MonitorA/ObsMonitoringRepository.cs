using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Models.MonitorA;
using Models.MonitorB;
using Repositories.MonitorB;
using System.Threading.Tasks;

namespace Repositories.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 데이터 Repository
    /// SensorRepository 데이터 재사용 (Monitor B와 동일한 데이터 보장)
    /// </summary>
    public class ObsMonitoringRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// 센서 정보 조회 - SensorRepository 통해서 가져오기!
        /// Monitor B와 동일한 데이터 보장
        /// </summary>
        public IEnumerator GetSensorInfo(
            int obsId,
            Action<List<SensorInfoData>> onSuccess,  // ⭐ SensorInfoData로 변경!
            Action<string> onError)
        {
            Debug.Log($"[ObsMonitoringRepository] SensorRepository를 통해 데이터 로드: ObsId={obsId}");

            bool isCompleted = false;
            List<SensorInfoData> result = null;
            string errorMsg = null;

            // SensorRepository의 비동기 메서드 호출
            Task<List<SensorInfoData>> task = SensorRepository.Instance.GetSensorsByObservatoryAsync(obsId);

            // Task 완료 대기
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    errorMsg = t.Exception?.GetBaseException()?.Message ?? "센서 데이터 로드 실패";
                    Debug.LogError($"[ObsMonitoringRepository] 에러: {errorMsg}");
                }
                else
                {
                    result = t.Result;
                    Debug.Log($"[ObsMonitoringRepository] SensorRepository 데이터 수신: {result?.Count ?? 0}개");
                }
                isCompleted = true;
            });

            // 완료 대기
            yield return new WaitUntil(() => isCompleted);

            if (errorMsg != null)
            {
                onError?.Invoke(errorMsg);
                yield break;
            }

            if (result == null || result.Count == 0)
            {
                onError?.Invoke("센서 데이터가 없습니다.");
                yield break;
            }

            Debug.Log($"[ObsMonitoringRepository] 센서 데이터 반환: {result.Count}개 (Monitor B와 동일 데이터!)");
            onSuccess?.Invoke(result);
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
                        onSuccess?.Invoke(5); // 기본값 Step 5
                        return;
                    }

                    var result = results[0];
                    // toxistep 또는 chemistep 사용 (둘 다 같은 값)
                    int step = ConvertToDisplayStep(result.toxistep);

                    Debug.Log($"[ObsMonitoringRepository] GetSensorStep 성공: DB값={result.toxistep} → Step {step}");
                    onSuccess?.Invoke(step);
                },
                onError
            );
        }

        /// <summary>
        /// DB 값을 화면 단계로 변환 (원본 코드 로직)
        /// </summary>
        private int ConvertToDisplayStep(string dbValue)
        {
            if (string.IsNullOrEmpty(dbValue)) return 5;

            return dbValue.Trim() switch
            {
                "0020" => 1,
                "0021" => 2,
                "0023" => 3,
                "0024" => 4,
                "0025" => 5,
                "20" => 1,   // 짧은 형식도 지원
                "21" => 2,
                "23" => 3,
                "24" => 4,
                "25" => 5,
                _ => 5       // 기본값
            };
        }
    }
}