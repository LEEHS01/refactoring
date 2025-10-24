using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViewModels.MonitorB;
using Models.MonitorB;
using Common.UI;

namespace Views.MonitorB
{
    /// <summary>
    /// 실시간 센서 상세 팝업 View
    /// 센서의 현재값, 임계값, 상태, 12시간 트렌드 차트 표시
    /// </summary>
    public class PopUpToxinDetail2View : MonoBehaviour
    {
        [Header("센서 정보 UI")]
        [SerializeField] private TMP_Text txtName;      // 센서명
        [SerializeField] private TMP_Text txtCurrent;   // 현재 측정값
        [SerializeField] private TMP_Text txtUnit;      // 단위 (선택사항)
        [SerializeField] private Button btnClose;       // 닫기 버튼

        [Header("차트 UI")]
        [SerializeField] private ChartLineRenderer chartLineRenderer;
        [SerializeField] private RectTransform chartBoundsArea;

        [Header("시간축 라벨 (hours)")]
        [SerializeField] private List<TMP_Text> txtTimeLabels;  // 시간 라벨들

        [Header("세로축 라벨 (verticals)")]
        [SerializeField] private List<TMP_Text> txtVerticalLabels;  // 값 라벨들

        [Header("상태 아이콘")]
        [SerializeField] private GameObject statusGreen;   // 정상 상태 아이콘
        [SerializeField] private GameObject statusYellow;  // 경계 상태 아이콘
        [SerializeField] private GameObject statusRed;     // 경보 상태 아이콘
        [SerializeField] private GameObject statusPurple;  // 설비이상 상태 아이콘

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePopup);
            }

            // 차트 초기화
            if (chartLineRenderer != null && chartBoundsArea != null)
            {
                chartLineRenderer.Initialize(chartBoundsArea);
            }

            ConnectToViewModel();
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
            if (PopUpToxinDetail2ViewModel.Instance != null)
            {
                PopUpToxinDetail2ViewModel.Instance.OnDataLoaded += OnDataLoaded;
                PopUpToxinDetail2ViewModel.Instance.OnError += OnError;
            }
        }

        private void DisconnectFromViewModel()
        {
            if (PopUpToxinDetail2ViewModel.Instance != null)
            {
                PopUpToxinDetail2ViewModel.Instance.OnDataLoaded -= OnDataLoaded;
                PopUpToxinDetail2ViewModel.Instance.OnError -= OnError;
            }
        }

        /// <summary>
        /// 팝업 오픈
        /// </summary>
        public void OpenPopup(int obsId, int boardId, int hnsId)
        {
            Debug.Log($"[PopUpToxinDetail2View] 팝업 오픈: obs={obsId}, board={boardId}, hns={hnsId}");

            gameObject.SetActive(true);

            // ViewModel에 데이터 로드 요청
            PopUpToxinDetail2ViewModel.Instance?.LoadSensorDetail(obsId, boardId, hnsId);
        }

        /// <summary>
        /// 데이터 로드 완료 시 - UI 업데이트
        /// </summary>
        private void OnDataLoaded(SensorInfoData sensorData, ChartData chartData)
        {
            Debug.Log($"[PopUpToxinDetail2View] OnDataLoaded 호출! - {sensorData.sensorName}");

            // 센서 기본 정보
            UpdateSensorInfo(sensorData);

            // 상태 아이콘
            SetStatusIcon(DetermineStatus(sensorData));

            // 차트
            UpdateChart(chartData);

            // 시간축 라벨
            UpdateTimeLabels();

            // 세로축 라벨
            UpdateVerticalLabels();

            Debug.Log($"[PopUpToxinDetail2View] UI 업데이트 완료!");
        }

        /// <summary>
        /// 센서 기본 정보 업데이트
        /// </summary>
        private void UpdateSensorInfo(SensorInfoData data)
        {
            if (txtName != null)
                txtName.text = data.sensorName;

            if (txtCurrent != null)
                txtCurrent.text = data.currentValue.ToString("F2");

            if (txtUnit != null)
                txtUnit.text = data.unit ?? "";

            Debug.Log($"[PopUpToxinDetail2View] 센서 정보 업데이트: {data.sensorName}, 현재값={data.currentValue}");
        }

        /// <summary>
        /// 상태 아이콘 설정
        /// </summary>
        private void SetStatusIcon(SensorStatus status)
        {
            // 모두 비활성화
            if (statusGreen != null) statusGreen.SetActive(false);
            if (statusYellow != null) statusYellow.SetActive(false);
            if (statusRed != null) statusRed.SetActive(false);
            if (statusPurple != null) statusPurple.SetActive(false);

            // 현재 상태만 활성화
            switch (status)
            {
                case SensorStatus.Normal:
                    if (statusGreen != null) statusGreen.SetActive(true);
                    break;
                case SensorStatus.Warning:
                    if (statusYellow != null) statusYellow.SetActive(true);
                    break;
                case SensorStatus.Critical:
                    if (statusRed != null) statusRed.SetActive(true);
                    break;
                case SensorStatus.Error:
                    if (statusPurple != null) statusPurple.SetActive(true);
                    break;
            }

            Debug.Log($"[PopUpToxinDetail2View] 상태 아이콘: {status}");
        }

        /// <summary>
        /// 차트 업데이트
        /// </summary>
        private void UpdateChart(ChartData chartData)
        {
            if (chartLineRenderer == null)
            {
                Debug.LogError("[PopUpToxinDetail2View] ChartLineRenderer가 null!");
                return;
            }

            if (chartData == null || chartData.values.Count == 0)
            {
                Debug.LogWarning("[PopUpToxinDetail2View] 차트 데이터 없음");
                return;
            }

            var normalizedValues = PopUpToxinDetail2ViewModel.Instance.GetNormalizedChartValues();

            chartLineRenderer.UpdateChart(normalizedValues);

            Debug.Log($"[PopUpToxinDetail2View] 차트 그리기 완료: {normalizedValues.Count}개 포인트");
        }

        /// <summary>
        /// 시간축 라벨 업데이트 (12시간 구간)
        /// </summary>
        private void UpdateTimeLabels()
        {
            if (txtTimeLabels == null || txtTimeLabels.Count == 0)
            {
                Debug.LogWarning("[PopUpToxinDetail2View] 시간 라벨이 없습니다!");
                return;
            }

            var chartData = PopUpToxinDetail2ViewModel.Instance.CurrentChartData;
            if (chartData == null) return;

            DateTime endTime = chartData.endTime;
            DateTime startTime = chartData.startTime;

            // 시간 간격 계산
            double totalHours = (endTime - startTime).TotalHours;
            double intervalHours = totalHours / (txtTimeLabels.Count - 1);

            for (int i = 0; i < txtTimeLabels.Count; i++)
            {
                if (txtTimeLabels[i] != null)
                {
                    DateTime labelTime = startTime.AddHours(intervalHours * i);
                    txtTimeLabels[i].text = labelTime.ToString("HH:mm");
                }
            }

            Debug.Log($"[PopUpToxinDetail2View] 시간 라벨 업데이트: {startTime:HH:mm} ~ {endTime:HH:mm}");
        }

        /// <summary>
        /// 세로축 라벨 업데이트
        /// </summary>
        private void UpdateVerticalLabels()
        {
            if (txtVerticalLabels == null || txtVerticalLabels.Count == 0)
            {
                Debug.LogWarning("[PopUpToxinDetail2View] 세로 라벨이 없습니다!");
                return;
            }

            var labels = PopUpToxinDetail2ViewModel.Instance.GetVerticalLabels(txtVerticalLabels.Count);

            for (int i = 0; i < txtVerticalLabels.Count; i++)
            {
                if (txtVerticalLabels[i] != null)
                {
                    txtVerticalLabels[i].text = labels[i].ToString("F2");
                }
            }

            Debug.Log($"[PopUpToxinDetail2View] 세로 라벨 업데이트 완료");
        }

        /// <summary>
        /// 센서 상태 판정
        /// </summary>
        private SensorStatus DetermineStatus(SensorInfoData data)
        {
            float currentValue = data.currentValue;

            if (currentValue >= data.criticalThreshold)
                return SensorStatus.Critical;
            if (currentValue >= data.warningThreshold)
                return SensorStatus.Warning;
            return SensorStatus.Normal;
        }

        /// <summary>
        /// 에러 발생 시
        /// </summary>
        private void OnError(string errorMessage)
        {
            Debug.LogError($"[PopUpToxinDetail2View] 에러: {errorMessage}");
            // TODO: 에러 팝업 표시
        }

        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void ClosePopup()
        {
            gameObject.SetActive(false);
            Debug.Log("[PopUpToxinDetail2View] 팝업 닫힘");
        }
    }
}