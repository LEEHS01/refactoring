using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Services;
using Models.MonitorB;

namespace Repositories.MonitorB
{
    /// <summary>
    /// 센서 데이터 Repository
    /// GET_SENSOR_INFO 프로시저 실행
    /// </summary>
    public class SensorRepository
    {
        private static SensorRepository _instance;
        public static SensorRepository Instance => _instance ??= new SensorRepository();

        private SensorRepository() { }

        /// <summary>
        /// 관측소별 센서 데이터 조회 (비동기)
        /// </summary>
        public async Task<List<SensorInfoData>> GetSensorsByObservatoryAsync(int obsId)
        {
            if (obsId <= 0)
            {
                Debug.LogError("[SensorRepository] 잘못된 obsId");
                return new List<SensorInfoData>();
            }

            try
            {
                Debug.Log($"[SensorRepository] GET_SENSOR_INFO 실행: obsId={obsId}");

                string query = $"EXEC GET_SENSOR_INFO @OBSIDX={obsId}";

                // 비동기 메서드 사용
                string result = await DatabaseService.Instance.ExecuteQueryAsync(query);

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning($"[SensorRepository] 센서 데이터 없음: obsId={obsId}");
                    return new List<SensorInfoData>();
                }

                Debug.Log($"[SensorRepository] 쿼리 성공: {result.Length}자");

                // JSON 파싱
                var wrappedJson = "{\"items\":" + result + "}";
                var response = JsonUtility.FromJson<SensorDataResponse>(wrappedJson);

                if (response?.items == null || response.items.Count == 0)
                {
                    Debug.LogWarning("[SensorRepository] 파싱된 데이터 없음");
                    return new List<SensorInfoData>();
                }

                Debug.Log($"[SensorRepository] 센서 {response.items.Count}개 파싱 완료");

                return response.items;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SensorRepository] 센서 데이터 조회 실패: {ex.Message}");
                return new List<SensorInfoData>();
            }
        }

        #region Helper Classes

        [Serializable]
        private class SensorDataResponse
        {
            public List<SensorInfoData> items;
        }

        #endregion
    }
}