using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Models.MonitorB;
using Services;

namespace Repositories.MonitorB
{
    public class AIAnalysisRepository
    {
        private static AIAnalysisRepository _instance;
        public static AIAnalysisRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AIAnalysisRepository();
                }
                return _instance;
            }
        }

        public async Task<AIAnalysisData> GetAIAnalysisDataAsync(int obsId, int boardId, int hnsId)
        {
            if (DatabaseService.Instance == null)
            {
                Debug.LogError("[AIAnalysisRepository] DatabaseService가 null입니다.");
                return null;
            }

            try
            {
                Debug.Log($"[AIAnalysisRepository] 데이터 조회 시작: obs={obsId}, board={boardId}, hns={hnsId}");

                // 시간 범위 계산
                DateTime now = DateTime.Now;
                DateTime endTime = new DateTime(
                    now.Year, now.Month, now.Day,
                    now.Hour,
                    (now.Minute / 10) * 10,
                    0
                );
                DateTime startTime = endTime.AddHours(-12);

                Debug.Log($"[AIAnalysisRepository] 조회 시간: {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}");

                // GET_CHARTVALUE 프로시저 호출
                string query = $@"
                    EXEC GET_CHARTVALUE 
                    @obsidx={obsId}, 
                    @start_dt='{startTime:yyyyMMddHHmmss}',
                    @end_dt='{endTime:yyyyMMddHHmmss}',
                    @interval=10
                ";

                string result = await DatabaseService.Instance.ExecuteQueryAsync(query);

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning("[AIAnalysisRepository] 쿼리 결과가 비어있습니다.");
                    return null;
                }

                Debug.Log($"[AIAnalysisRepository] 쿼리 결과 길이: {result.Length}자");

                // JSON 파싱 (ChartDataPoint 사용)
                var wrappedJson = "{\"items\":" + result + "}";
                var response = JsonUtility.FromJson<ChartDataResponse>(wrappedJson);

                if (response?.items == null || response.items.Count == 0)
                {
                    Debug.LogWarning("[AIAnalysisRepository] 파싱된 데이터가 없습니다.");
                    return null;
                }

                Debug.Log($"[AIAnalysisRepository] 전체 데이터: {response.items.Count}개");

                // boardIdx, hnsIdx로 필터링 (PascalCase 프로퍼티 사용)
                var sensorData = response.items
                    .Where(d => d.boardIdx == boardId && d.hnsIdx == hnsId)
                    .OrderBy(d => d.time)
                    .ToList();

                Debug.Log($"[AIAnalysisRepository] Board={boardId}, HNS={hnsId} 필터링 결과: {sensorData.Count}개");

                if (sensorData.Count == 0)
                {
                    Debug.LogWarning($"[AIAnalysisRepository] Board={boardId}, HNS={hnsId}에 해당하는 데이터가 없습니다!");
                    return null;
                }

                // 실제 데이터의 시작/종료 시간 계산
                DateTime actualStartTime = sensorData.Min(d => d.time);
                DateTime actualEndTime = sensorData.Max(d => d.time);

                // AI값, 측정값, 편차값 리스트
                List<float> aiValues = new List<float>();
                List<float> measuredValues = new List<float>();
                List<float> differenceValues = new List<float>();

                foreach (var item in sensorData)
                {
                    // value, aiValue 프로퍼티 사용
                    float measuredValue = item.value;
                    float aiValue = item.aiValue;
                    float differenceValue = measuredValue - aiValue;

                    measuredValues.Add(measuredValue);
                    aiValues.Add(aiValue);
                    differenceValues.Add(differenceValue);
                }

                var data = new AIAnalysisData
                {
                    AIValues = aiValues,
                    MeasuredValues = measuredValues,
                    DifferenceValues = differenceValues,
                    WarningThreshold = 100f,
                    StartTime = actualStartTime,
                    EndTime = actualEndTime
                };

                Debug.Log($"[AIAnalysisRepository] 데이터 처리 완료 - {aiValues.Count}개");
                Debug.Log($"[AIAnalysisRepository] 시간 범위: {actualStartTime:yyyy-MM-dd HH:mm} ~ {actualEndTime:yyyy-MM-dd HH:mm}");
                /*Debug.Log($"[AIAnalysisRepository] 쿼리 결과 길이: {result.Length}자");
                Debug.Log($"[AIAnalysisRepository] 전체 데이터: {response.items.Count}개");
                Debug.Log($"[AIAnalysisRepository] Board={boardId}, HNS={hnsId} 필터링 결과: {sensorData.Count}개");

                // AI값 확인하려면 이 부분에 추가:
                foreach (var item in sensorData)
                {
                    Debug.Log($"시간: {item.time}, 측정값: {item.value}, AI값: {item.aiValue}");
                }
*/
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIAnalysisRepository] 에러: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        // ChartDataPoint 사용
        [Serializable]
        private class ChartDataResponse
        {
            public List<ChartDataPoint> items;
        }
    }
}