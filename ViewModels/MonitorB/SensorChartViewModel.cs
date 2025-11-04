// ViewModels/MonitorB/SensorChartViewModel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Models.MonitorB;
using Repositories.MonitorB;

namespace ViewModels.MonitorB
{
    public class SensorChartViewModel : MonoBehaviour
    {
        public static SensorChartViewModel Instance { get; private set; }

        public event Action<ChartData> OnChartDataLoaded;
        public event Action<string> OnError;

        public ChartData currentChartData { get; private set; }

        private int currentBoardId = -1;
        private int currentHnsId = -1;

        public ChartData CurrentChartData => currentChartData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SensorChartViewModel] 초기화 완료");
        }

        /// <summary>
        /// 특정 센서의 차트 데이터 로드
        /// </summary>
        public async void LoadChartData(int obsId, int boardId, int hnsId)
        {
            if (obsId <= 0 || boardId <= 0 || hnsId <= 0)
            {
                Debug.LogError($"[SensorChartViewModel] 잘못된 파라미터");
                OnError?.Invoke("잘못된 센서 정보입니다.");
                return;
            }

            currentBoardId = boardId;
            currentHnsId = hnsId;

            Debug.Log($"[SensorChartViewModel] 차트 데이터 로드: Board={boardId}, HNS={hnsId}");

            try
            {
                var chartData = await ChartRepository.Instance.GetSensorChartDataAsync(obsId, boardId, hnsId);

                if (chartData == null)
                {
                    Debug.LogWarning($"[SensorChartViewModel] 차트 데이터 없음");
                    OnError?.Invoke("차트 데이터를 불러올 수 없습니다.");
                    return;
                }

                currentChartData = chartData;
                Debug.Log($"[SensorChartViewModel] 차트 데이터 로드 완료: {chartData.values.Count}개 포인트");

                OnChartDataLoaded?.Invoke(chartData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SensorChartViewModel] 차트 로드 실패: {ex.Message}");
                OnError?.Invoke($"차트 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 정규화된 차트 값 반환 (0~1 범위)
        /// </summary>
        public List<float> GetNormalizedValues()
        {
            if (currentChartData == null || currentChartData.values.Count == 0)
                return new List<float>();

            float max = currentChartData.maxValue;

            // 최댓값에 +1 여백 추가 (상단 공간 확보)
            float displayMax = max + 1f;

            // 기존 코드 (여백 없음)
            // float displayMax = max;

            if (displayMax <= 0)
            {
                return currentChartData.values.Select(_ => 0f).ToList();
            }

            return currentChartData.values.Select(v => v / displayMax).ToList();
        }

        /// <summary>
        /// 세로축 라벨 값 계산 (6개)
        /// </summary>
        public List<float> GetVerticalLabels(int labelCount = 6)
        {
            List<float> labels = new List<float>();

            if (currentChartData == null)
            {
                for (int i = 0; i < labelCount; i++)
                    labels.Add(0);
                return labels;
            }

            // 최댓값에 +1 여백 추가
            float displayMax = currentChartData.maxValue + 1f;

            // 기존 코드 (여백 없음)
            // float displayMax = currentChartData.maxValue + 1;

            float interval = displayMax / (labelCount - 1);

            for (int i = 0; i < labelCount; i++)
            {
                labels.Add(interval * i);
            }

            return labels;
        }
    }
}