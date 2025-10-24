// Views/MonitorB/PopupAIAnalysisView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViewModels.MonitorB;
using System;

namespace Views.MonitorB
{
    public class PopupAIAnalysisView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtSensorName;
        [SerializeField] private TMP_Text txtTimeRange;
        [SerializeField] private Button btnClose;

        [Header("Chart Views")]
        [SerializeField] private ChartBarView chartAI;
        [SerializeField] private ChartBarView chartMeasured;
        [SerializeField] private ChartBarView chartDifference;

        // ⭐ 현재 센서 정보 저장
        private int currentObsId;
        private int currentBoardId;
        private int currentHnsId;

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePopup);
            }

            // 3개 차트 모두 초기화
            InitializeCharts();

            ConnectToViewModel();

            // 초기 비활성화
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DisconnectFromViewModel();

            if (btnClose != null)
            {
                btnClose.onClick.RemoveListener(ClosePopup);
            }
        }

        // ⭐ 차트 초기화
        private void InitializeCharts()
        {
            if (chartAI != null)
            {
                chartAI.Initialize();
            }

            if (chartMeasured != null)
            {
                chartMeasured.Initialize();
            }

            if (chartDifference != null)
            {
                chartDifference.Initialize();
            }

            Debug.Log("[PopupAIAnalysisView] 3개 차트 초기화 완료");
        }

        private void ConnectToViewModel()
        {
            if (AIAnalysisViewModel.Instance != null)
            {
                AIAnalysisViewModel.Instance.OnDataLoaded += OnDataLoaded;
                AIAnalysisViewModel.Instance.OnError += OnError;
            }
        }

        private void DisconnectFromViewModel()
        {
            if (AIAnalysisViewModel.Instance != null)
            {
                AIAnalysisViewModel.Instance.OnDataLoaded -= OnDataLoaded;
                AIAnalysisViewModel.Instance.OnError -= OnError;
            }
        }

        public void OpenPopup(int obsId, int boardId, int hnsId)
        {
            // 현재 센서 정보 저장
            currentObsId = obsId;
            currentBoardId = boardId;
            currentHnsId = hnsId;

            Debug.Log($"[PopupAIAnalysisView] 팝업 열기: obs={obsId}, board={boardId}, hns={hnsId}");

            gameObject.SetActive(true);

            // 센서명 가져오기 (임시 → 실제)
            LoadSensorName();

            // 시간 범위 초기화 (로딩 중)
            if (txtTimeRange != null)
            {
                txtTimeRange.text = "조회 중...";
            }

            // 데이터 로드
            AIAnalysisViewModel.Instance.LoadAIAnalysis(obsId, boardId, hnsId);
        }

        // ⭐ 센서명 가져오기
        private void LoadSensorName()
        {
            if (txtSensorName == null) return;

            // SensorMonitorViewModel에서 센서 정보 찾기
            if (ViewModels.MonitorB.SensorMonitorViewModel.Instance != null)
            {
                var allSensors = ViewModels.MonitorB.SensorMonitorViewModel.Instance.AllSensors;
                var sensor = allSensors.Find(s =>
                    s.boardIdx == currentBoardId &&
                    s.hnsIdx == currentHnsId
                );

                if (sensor != null)
                {
                    txtSensorName.text = sensor.sensorName;
                }
                else
                {
                    txtSensorName.text = $"센서 {currentHnsId}";
                }
            }
            else
            {
                txtSensorName.text = $"센서 {currentHnsId}";
            }
        }

        private void OnDataLoaded(
            Models.MonitorB.ProcessedChartData aiData,
            Models.MonitorB.ProcessedChartData measuredData,
            Models.MonitorB.ProcessedChartData differenceData,
            DateTime startTime,
            DateTime endTime)
        {
            Debug.Log($"[PopupAIAnalysisView] 데이터 로드 완료 - 시간: {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}");

            // 시간 범위 표시
            UpdateTimeRangeLabel(startTime, endTime);

            // AI값 차트
            if (chartAI != null)
            {
                chartAI.UpdateChart(
                    aiData.ProcessedValues,
                    aiData.MaxValue,
                    endTime
                );
                chartAI.HighlightAnomalousPoints(aiData.AnomalousIndices);
            }

            // 측정값 차트
            if (chartMeasured != null)
            {
                chartMeasured.UpdateChart(
                    measuredData.ProcessedValues,
                    measuredData.MaxValue,
                    endTime
                );
                chartMeasured.HighlightAnomalousPoints(measuredData.AnomalousIndices);
            }

            // 편차값 차트
            if (chartDifference != null)
            {
                chartDifference.UpdateChart(
                    differenceData.ProcessedValues,
                    differenceData.MaxValue,
                    endTime
                );
                chartDifference.HighlightAnomalousPoints(differenceData.AnomalousIndices);
            }
        }

        private void UpdateTimeRangeLabel(DateTime startTime, DateTime endTime)
        {
            if (txtTimeRange == null) return;

            // 날짜가 같으면 날짜 한 번만, 다르면 둘 다 표시
            if (startTime.Date == endTime.Date)
            {
                txtTimeRange.text = $"AI 분석 조회 시점: {startTime:yyyy-MM-dd}  {startTime:HH:mm} ~ {endTime:HH:mm}";
            }
            else
            {
                txtTimeRange.text = $"AI 분석 조회 시점: {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}";
            }

            Debug.Log($"[PopupAIAnalysisView] 시간 범위: {txtTimeRange.text}");
        }

        private void OnError(string errorMessage)
        {
            Debug.LogError($"[PopupAIAnalysisView] 에러: {errorMessage}");

            if (txtTimeRange != null)
            {
                txtTimeRange.text = "데이터 조회 실패";
            }
        }

        private void ClosePopup()
        {
            Debug.Log("[PopupAIAnalysisView] 팝업 닫기");
            gameObject.SetActive(false);
        }
    }
}