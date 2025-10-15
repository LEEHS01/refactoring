// Repositories/MonitorB/AIAnalysisRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Models;
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

        /// <summary>
        /// AI 분석 데이터 조회 (GET_CHARTVALUE 프로시저 사용)
        /// </summary>
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

                // ⭐ 시간 범위 계산 (현재 시간 기준 12시간)
                DateTime now = DateTime.Now;
                DateTime endTime = new DateTime(
                    now.Year, now.Month, now.Day,
                    now.Hour,
                    (now.Minute / 10) * 10,  // 10분 단위로 내림
                    0
                );
                DateTime startTime = endTime.AddHours(-12);

                Debug.Log($"[AIAnalysisRepository] 조회 시간: {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}");

                // ⭐ GET_CHARTVALUE 프로시저 호출 (필수 파라미터 포함)
                string query = $@"
                    EXEC GET_CHARTVALUE 
                    @obsidx={obsId}, 
                    @boardidx={boardId}, 
                    @hnsidx={hnsId},
                    @start_dt='{startTime:yyyyMMddHHmmss}',
                    @end_dt='{endTime:yyyyMMddHHmmss}',
                    @interval=10
                ";

                string result = await DatabaseService.Instance.ExecuteQueryAsync(query);

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning("[AIAnalysisRepository] 데이터가 없습니다.");
                    return null;
                }

                Debug.Log($"[AIAnalysisRepository] 쿼리 결과 길이: {result.Length}자");

                // JSON 파싱
                var wrappedJson = "{\"items\":" + result + "}";
                var response = JsonUtility.FromJson<ChartDataResponse>(wrappedJson);

                if (response?.items == null || response.items.Count == 0)
                {
                    Debug.LogWarning("[AIAnalysisRepository] 파싱된 데이터가 없습니다.");
                    return null;
                }

                Debug.Log($"[AIAnalysisRepository] {response.items.Count}개 데이터 로드 완료");

                // ⭐ 실제 데이터의 시작/종료 시간 계산
                DateTime actualStartTime = DateTime.MaxValue;
                DateTime actualEndTime = DateTime.MinValue;

                // AI값, 측정값, 편차값 리스트
                List<float> aiValues = new List<float>();
                List<float> measuredValues = new List<float>();
                List<float> differenceValues = new List<float>();

                foreach (var item in response.items)
                {
                    // 측정값 (val)
                    float measuredValue = item.val;

                    // AI값 (aival)
                    float aiValue = item.aival;

                    // 편차값 (측정값 - AI값)
                    float differenceValue = measuredValue - aiValue;

                    // 리스트에 추가
                    measuredValues.Add(measuredValue);
                    aiValues.Add(aiValue);
                    differenceValues.Add(differenceValue);

                    // 실제 측정 시간 업데이트
                    if (!string.IsNullOrEmpty(item.obsdt))
                    {
                        if (DateTime.TryParse(item.obsdt, out DateTime obsTime))
                        {
                            if (obsTime < actualStartTime) actualStartTime = obsTime;
                            if (obsTime > actualEndTime) actualEndTime = obsTime;
                        }
                    }
                }

                // 실제 시간이 없으면 조회 시간 사용
                if (actualStartTime == DateTime.MaxValue || actualEndTime == DateTime.MinValue)
                {
                    actualStartTime = startTime;
                    actualEndTime = endTime;
                    Debug.LogWarning("[AIAnalysisRepository] 실제 측정 시간 없음 - 조회 시간 사용");
                }

                var data = new AIAnalysisData
                {
                    AIValues = aiValues,
                    MeasuredValues = measuredValues,
                    DifferenceValues = differenceValues,
                    WarningThreshold = 100f, // TODO: 실제 임계값으로 교체
                    StartTime = actualStartTime,
                    EndTime = actualEndTime
                };

                Debug.Log($"[AIAnalysisRepository] 데이터 처리 완료 - 시간 범위: {actualStartTime:yyyy-MM-dd HH:mm} ~ {actualEndTime:yyyy-MM-dd HH:mm}");

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIAnalysisRepository] 에러: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        // JSON 파싱용 헬퍼 클래스
        [Serializable]
        private class ChartDataResponse
        {
            public List<ChartDataModel> items;
        }
    }
}