// ViewModels/MonitorB/AIAnalysisViewModel.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using Models.MonitorB;
using Repositories.MonitorB;

namespace ViewModels.MonitorB
{
    public class AIAnalysisViewModel : MonoBehaviour
    {
        public static AIAnalysisViewModel Instance { get; private set; }

        // ⭐ 이벤트에 시작/종료 시간 추가
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

                // 데이터 처리
                var aiProcessed = ProcessChartData(data.AIValues, data.WarningThreshold);
                var measuredProcessed = ProcessChartData(data.MeasuredValues, data.WarningThreshold);
                var differenceProcessed = ProcessChartData(data.DifferenceValues, data.WarningThreshold);

                // AI값과 측정값은 동일한 스케일 사용
                float commonMaxValue = Mathf.Max(aiProcessed.MaxValue, measuredProcessed.MaxValue);
                aiProcessed.MaxValue = commonMaxValue;
                measuredProcessed.MaxValue = commonMaxValue;

                Debug.Log($"[AIAnalysisViewModel] 데이터 처리 완료 - AI: {aiProcessed.ProcessedValues.Count}개");

                // ⭐ Repository에서 받은 실제 시간 사용
                OnDataLoaded?.Invoke(
                    aiProcessed,
                    measuredProcessed,
                    differenceProcessed,
                    data.StartTime,  // ⭐ 실제 시작 시간
                    data.EndTime     // ⭐ 실제 종료 시간
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIAnalysisViewModel] 에러: {ex.Message}");
                OnError?.Invoke($"AI 분석 로드 실패: {ex.Message}");
            }
        }

        private ProcessedChartData ProcessChartData(List<float> values, float warningThreshold)
        {
            var processedData = new ProcessedChartData
            {
                ProcessedValues = new List<float>(values),
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
    }
}