using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.MonitorB;
using Repositories.MonitorB;

namespace ViewModels.MonitorB
{
    public class AIAnalysisViewModel : MonoBehaviour
    {
        public static AIAnalysisViewModel Instance { get; private set; }

        public event Action<ProcessedChartData, ProcessedChartData, ProcessedChartData, DateTime, DateTime> OnDataLoaded;
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[AIAnalysisViewModel] Instance 설정 완료");
        }

        public async void LoadAIAnalysis(int obsId, int boardId, int hnsId)
        {
            try
            {
                Debug.Log($"[AIAnalysisViewModel] AI 분석 로드 시작: obs={obsId}, board={boardId}, hns={hnsId}");

                var data = await AIAnalysisRepository.Instance.GetAIAnalysisDataAsync(obsId, boardId, hnsId);

                if (data == null || data.AIValues.Count == 0)
                {
                    OnError?.Invoke("AI 분석 데이터가 없습니다.");
                    return;
                }

                Debug.Log($"[AIAnalysisViewModel] 원본 데이터 - AI값: {data.AIValues.Count}개, 측정값: {data.MeasuredValues.Count}개");
                Debug.Log($"[AIAnalysisViewModel] AI값 샘플: {string.Join(", ", data.AIValues.Take(5))}");
                Debug.Log($"[AIAnalysisViewModel] 측정값 샘플: {string.Join(", ", data.MeasuredValues.Take(5))}");

                // 1단계: 이상치 찾기 및 MaxValue 계산 (정규화 안 함)
                var aiProcessed = FindAnomaliesAndMaxValue(data.AIValues);
                var measuredProcessed = FindAnomaliesAndMaxValue(data.MeasuredValues);
                var differenceProcessed = FindAnomaliesAndMaxValue(data.DifferenceValues);

                Debug.Log($"[AIAnalysisViewModel] AI MaxValue: {aiProcessed.MaxValue}, 측정 MaxValue: {measuredProcessed.MaxValue}");

                // 2단계: AI값과 측정값은 동일한 스케일 사용
                float commonMaxValue = Mathf.Max(aiProcessed.MaxValue, measuredProcessed.MaxValue);

                Debug.Log($"[AIAnalysisViewModel] 공통 MaxValue: {commonMaxValue}");

                // 3단계: 각각 정규화 (0~1 범위)
                NormalizeValues(aiProcessed, commonMaxValue);
                NormalizeValues(measuredProcessed, commonMaxValue);
                NormalizeValues(differenceProcessed, differenceProcessed.MaxValue);  // 편차는 독립적

                // MaxValue 업데이트
                aiProcessed.MaxValue = commonMaxValue;
                measuredProcessed.MaxValue = commonMaxValue;

                Debug.Log($"[AIAnalysisViewModel] 정규화 완료 - AI: {aiProcessed.ProcessedValues.Count}개");
                Debug.Log($"[AIAnalysisViewModel] 정규화된 AI값 샘플: {string.Join(", ", aiProcessed.ProcessedValues.Take(5).Select(v => v.ToString("F3")))}");

                // Repository에서 받은 실제 시간 사용
                OnDataLoaded?.Invoke(
                    aiProcessed,
                    measuredProcessed,
                    differenceProcessed,
                    data.StartTime,
                    data.EndTime
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIAnalysisViewModel] 에러: {ex.Message}\n{ex.StackTrace}");
                OnError?.Invoke($"AI 분석 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 이상치 찾기 및 MaxValue 계산 (정규화 없음)
        /// </summary>
        private ProcessedChartData FindAnomaliesAndMaxValue(List<float> values)
        {
            var processedData = new ProcessedChartData
            {
                ProcessedValues = new List<float>(values),  // 원본 값 복사
                AnomalousIndices = new List<int>(),
                MaxValue = 0f
            };

            // 이상치 찾기 및 최댓값 계산
            for (int i = 0; i < values.Count; i++)
            {
                float val = values[i];

                // 이상치 확인
                if (val == -9999f || val == 9999f)
                {
                    processedData.AnomalousIndices.Add(i);
                }

                // 최댓값 업데이트 (이상치 제외)
                if (val != -9999f && val != 9999f)
                {
                    processedData.MaxValue = Mathf.Max(processedData.MaxValue, val);
                }
            }

            // 최댓값이 0이면 기본값 설정
            if (processedData.MaxValue <= 0f)
            {
                processedData.MaxValue = 100f;
            }

            return processedData;
        }

        /// <summary>
        /// 값들을 0~1 범위로 정규화
        /// </summary>
        private void NormalizeValues(ProcessedChartData data, float maxValue)
        {
            if (maxValue <= 0f)
            {
                Debug.LogWarning("[AIAnalysisViewModel] maxValue가 0 이하입니다!");
                maxValue = 1f;
            }

            // 정규화된 값으로 교체
            for (int i = 0; i < data.ProcessedValues.Count; i++)
            {
                float val = data.ProcessedValues[i];

                // 이상치는 0으로
                if (val == -9999f || val == 9999f)
                {
                    data.ProcessedValues[i] = 0f;
                }
                else
                {
                    // 0~1 범위로 정규화
                    data.ProcessedValues[i] = val / maxValue;
                }
            }
        }
    }
}