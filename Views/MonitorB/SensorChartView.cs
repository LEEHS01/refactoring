// Views/MonitorB/SensorChartView.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Common.UI;
using Models.MonitorB;
using ViewModels.MonitorB;

namespace Views.MonitorB
{
    public class SensorChartView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtSensorName;
        [SerializeField] private TMP_Text txtChartTimeRange;

        [Header("Chart Configuration")]
        [SerializeField] private RectTransform chartBoundsArea;

        [Header("Time Labels (7개)")]
        [SerializeField] private List<TMP_Text> timeLabels;

        [Header("Value Labels (6개)")]
        [SerializeField] private List<TMP_Text> valueLabels;

        // 코드에서 찾아서 사용
        private ChartLineRenderer chartRenderer;

        private void Awake()
        {
            // 같은 GameObject 또는 자식에서 찾기
            chartRenderer = GetComponentInChildren<ChartLineRenderer>();

            if (chartRenderer == null)
            {
                Debug.LogError("[SensorChartView] ChartLineRenderer를 찾을 수 없습니다!");
            }
        }

        private void Start()
        {
            InitializeChart();
            ConnectToViewModel();
        }

        private void OnDestroy()
        {
            DisconnectFromViewModel();
        }

        private void InitializeChart()
        {
            if (chartRenderer == null)
            {
                Debug.LogError("[SensorChartView] ChartLineRenderer가 없습니다!");
                return;
            }

            if (chartBoundsArea == null)
            {
                Debug.LogError("[SensorChartView] Chart Bounds Area가 할당되지 않았습니다!");
                return;
            }

            // Chart_Dots 정보를 전달
            chartRenderer.Initialize(chartBoundsArea);
            Debug.Log("[SensorChartView] 차트 초기화 완료");
        }

        private void ConnectToViewModel()
        {
            if (SensorChartViewModel.Instance != null)
            {
                SensorChartViewModel.Instance.OnChartDataLoaded += OnChartDataLoaded;
                SensorChartViewModel.Instance.OnError += OnError;
            }
        }

        private void DisconnectFromViewModel()
        {
            if (SensorChartViewModel.Instance != null)
            {
                SensorChartViewModel.Instance.OnChartDataLoaded -= OnChartDataLoaded;
                SensorChartViewModel.Instance.OnError -= OnError;
            }
        }

        private void OnChartDataLoaded(ChartData chartData)
        {
            Debug.Log($"[SensorChartView] 차트 업데이트: {chartData.values.Count}개 포인트");
            UpdateChartTimeRangeLabels(chartData);
            UpdateTimeLabels(chartData.timeLabels);
            UpdateValueLabels();
            UpdateChart();
        }
        /// <summary>
        /// 차트 조회 시점 표시
        /// </summary>
        private void UpdateChartTimeRangeLabels(ChartData chartData)
        {
            if (txtChartTimeRange == null)
            {
                Debug.LogWarning("[SensorChartView] txtChartTimeRange가 null입니다!");
                return;
            }

            // 날짜가 같으면 날짜 한 번만, 다르면 둘 다 표시
            if (chartData.startTime.Date == chartData.endTime.Date)
            {
                // 같은 날: "2025-10-14  04:40 ~ 16:40"
                txtChartTimeRange.text = $"차트 조회 시점: {chartData.startTime:yyyy-MM-dd}  {chartData.startTime:HH:mm} ~ {chartData.endTime:HH:mm}";
            }
            else
            {
                // 다른 날: "2025-10-14 04:40 ~ 2025-10-15 16:40"
                txtChartTimeRange.text = $"차트 조회 시점: {chartData.startTime:yyyy-MM-dd HH:mm} ~ {chartData.endTime:yyyy-MM-dd HH:mm}";
            }
        }

        private void UpdateTimeLabels(List<System.DateTime> times)
        {
            for (int i = 0; i < timeLabels.Count && i < times.Count; i++)
            {
                if (timeLabels[i] != null)
                {
                    timeLabels[i].text = times[i].ToString("MM-dd\nHH:mm");
                }
            }
        }

        private void UpdateValueLabels()
        {
            var labels = SensorChartViewModel.Instance.GetVerticalLabels(valueLabels.Count);

            for (int i = 0; i < valueLabels.Count; i++)
            {
                if (valueLabels[i] != null)
                {
                    int reverseIndex = valueLabels.Count - 1 - i;
                    valueLabels[i].text = Mathf.Round(labels[reverseIndex]).ToString();
                }
            }
        }

        private void UpdateChart()
        {
            var normalizedValues = SensorChartViewModel.Instance.GetNormalizedValues();

            if (normalizedValues.Count > 0 && chartRenderer != null)
            {
                chartRenderer.UpdateChart(normalizedValues);
                Debug.Log($"[SensorChartView] 차트 그리기 완료");
            }
        }

        private void OnError(string errorMessage)
        {
            Debug.LogError($"[SensorChartView] 에러: {errorMessage}");
        }

        public void LoadSensorChart(int obsId, int boardId, int hnsId, string sensorName)
        {
            if (txtSensorName != null)
            {
                txtSensorName.text = sensorName;
            }

            SensorChartViewModel.Instance.LoadChartData(obsId, boardId, hnsId);
        }
    }
}