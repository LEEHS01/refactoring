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

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePopup);
            }

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
            Debug.Log($"[PopupAIAnalysisView] 팝업 열기: obs={obsId}, board={boardId}, hns={hnsId}");

            gameObject.SetActive(true);

            // 센서명 표시 (임시)
            if (txtSensorName != null)
            {
                txtSensorName.text = $"센서 {hnsId}";
            }

            // ⭐ 시간 범위 초기화 (로딩 중)
            if (txtTimeRange != null)
            {
                txtTimeRange.text = "조회 중...";
            }

            // 데이터 로드
            AIAnalysisViewModel.Instance.LoadAIAnalysis(obsId, boardId, hnsId);
        }

        // ⭐ 실제 시간 정보를 받아서 표시
        private void OnDataLoaded(
            Models.MonitorB.ProcessedChartData aiData,
            Models.MonitorB.ProcessedChartData measuredData,
            Models.MonitorB.ProcessedChartData differenceData,
            DateTime startTime,    // ⭐ 실제 시작 시간
            DateTime endTime)      // ⭐ 실제 종료 시간
        {
            Debug.Log($"[PopupAIAnalysisView] 데이터 로드 완료 - 시간: {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}");

            // ⭐ 실제 시간 범위 표시
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

        // ⭐ 시간 범위 표시 업데이트
        private void UpdateTimeRangeLabel(DateTime startTime, DateTime endTime)
        {
            if (txtTimeRange == null) return;

            // 날짜가 같으면 날짜 한 번만, 다르면 둘 다 표시
            if (startTime.Date == endTime.Date)
            {
                // 같은 날: "2025-10-14  04:40 ~ 16:40"
                txtTimeRange.text = $"AI 분석 조회 시점: {startTime:yyyy-MM-dd}  {startTime:HH:mm} ~ {endTime:HH:mm}";
            }
            else
            {
                // 다른 날: "2025-10-14 04:40 ~ 2025-10-15 16:40"
                txtTimeRange.text = $"AI 분석 조회 시점: {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}";
            }

            Debug.Log($"[PopupAIAnalysisView] 시간 범위: {txtTimeRange.text}");
        }

        private void OnError(string errorMessage)
        {
            Debug.LogError($"[PopupAIAnalysisView] 에러: {errorMessage}");

            // ⭐ 에러 시 시간 범위도 에러 표시
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