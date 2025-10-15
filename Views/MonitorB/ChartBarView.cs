using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Common.UI;

namespace Views.MonitorB
{
    /// <summary>
    /// AI 분석 차트 바 - 단일 차트 관리 (리팩토링 버전)
    /// ChartLineRenderer + 시간축/값축 라벨 + 이상치 표시
    /// </summary>
    public class ChartBarView : MonoBehaviour
    {
        [Header("Chart Configuration")]
        [SerializeField] private RectTransform chartBoundsArea;

        // 코드에서 자동으로 찾음
        private ChartLineRenderer chartRenderer;

        [Header("Time Labels (7개)")]
        [SerializeField] private List<TMP_Text> timeLabels;

        [Header("Value Labels (4개)")]
        [SerializeField] private List<TMP_Text> valueLabels;

        private List<Transform> chartPoints = new List<Transform>();

        #region Unity 생명주기

        private void Awake()
        {
            // ChartLineRenderer 자동 탐색
            chartRenderer = GetComponentInChildren<ChartLineRenderer>();

            if (chartRenderer == null)
            {
                Debug.LogError($"[ChartBarView] {gameObject.name}에 ChartLineRenderer가 없습니다!");
            }
        }

        #endregion

        #region Public 메서드

        /// <summary>
        /// 차트 초기화
        /// </summary>
        public void Initialize()
        {
            if (chartRenderer == null || chartBoundsArea == null)
            {
                Debug.LogError("[ChartBarView] ChartRenderer 또는 ChartBoundsArea가 없습니다!");
                return;
            }

            chartRenderer.Initialize(chartBoundsArea);
            Debug.Log($"[ChartBarView] {gameObject.name} 초기화 완료");
        }

        /// <summary>
        /// 차트 업데이트
        /// </summary>
        public void UpdateChart(List<float> normalizedValues, float maxValue, DateTime endTime)
        {
            if (chartRenderer == null)
            {
                Debug.LogError("[ChartBarView] ChartRenderer가 없습니다!");
                return;
            }

            // 차트 그리기
            chartRenderer.UpdateChart(normalizedValues);

            // 차트 포인트 저장
            chartPoints = chartRenderer.GetChartPoints();

            // 축 라벨 업데이트
            UpdateTimeLabels(endTime);
            UpdateValueLabels(maxValue);

            Debug.Log($"[ChartBarView] 차트 업데이트 완료: {normalizedValues.Count}개 포인트");
        }

        /// <summary>
        /// 이상치 빨간점 표시
        /// </summary>
        public void HighlightAnomalousPoints(List<int> anomalousIndices)
        {
            if (anomalousIndices == null || anomalousIndices.Count == 0)
            {
                Debug.Log("[ChartBarView] 이상치 없음");
                return;
            }

            if (chartPoints == null || chartPoints.Count == 0)
            {
                Debug.LogWarning("[ChartBarView] 차트 포인트가 없습니다!");
                return;
            }

            // 모든 포인트를 원래 색으로 초기화
            ResetAllPointColors();

            // 이상치 인덱스의 포인트를 빨간색으로
            foreach (int index in anomalousIndices)
            {
                if (index >= 0 && index < chartPoints.Count && chartPoints[index] != null)
                {
                    Image pointImage = chartPoints[index].GetComponent<Image>();
                    if (pointImage != null)
                    {
                        pointImage.color = Color.red;
                    }
                }
            }

            Debug.Log($"[ChartBarView] {anomalousIndices.Count}개 이상치 빨간점 표시");
        }

        /// <summary>
        /// 차트 포인트 리스트 반환 (툴팁용)
        /// </summary>
        public List<Transform> GetChartPoints()
        {
            return chartPoints;
        }

        #endregion

        #region Private 메서드

        /// <summary>
        /// 시간축 라벨 업데이트 (12시간 범위)
        /// </summary>
        private void UpdateTimeLabels(DateTime endTime)
        {
            if (timeLabels == null || timeLabels.Count == 0) return;

            DateTime startTime = endTime.AddHours(-12);
            double intervalHours = 12.0 / (timeLabels.Count - 1);

            for (int i = 0; i < timeLabels.Count; i++)
            {
                if (timeLabels[i] != null)
                {
                    DateTime labelTime = startTime.AddHours(intervalHours * i);
                    timeLabels[i].text = labelTime.ToString("MM-dd\nHH:mm");
                }
            }
        }

        /// <summary>
        /// 값축 라벨 업데이트
        /// </summary>
        private void UpdateValueLabels(float maxValue)
        {
            if (valueLabels == null || valueLabels.Count == 0) return;

            // +1 여백 추가
            float displayMax = maxValue + 1f;
            float interval = displayMax / (valueLabels.Count - 1);

            for (int i = 0; i < valueLabels.Count; i++)
            {
                if (valueLabels[i] != null)
                {
                    float value = interval * i;
                    valueLabels[i].text = value.ToString("F2");
                }
            }
        }

        /// <summary>
        /// 모든 포인트 색상 초기화 (청록색)
        /// </summary>
        private void ResetAllPointColors()
        {
            if (chartPoints == null) return;

            foreach (Transform point in chartPoints)
            {
                if (point != null)
                {
                    Image pointImage = point.GetComponent<Image>();
                    if (pointImage != null)
                    {
                        pointImage.color = new Color(0, 1, 1); // 청록색
                    }
                }
            }
        }

        #endregion
    }
}