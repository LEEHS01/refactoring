using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Models.MonitorB;

namespace Common.UI
{
    /// <summary>
    /// 차트 노드 호버 시 툴팁을 표시하는 핸들러 (멀티 디스플레이 지원)
    /// </summary>
    public class ChartTooltipHandler : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject tooltipObject;
        [SerializeField] private TMP_Text txtTime;
        [SerializeField] private TMP_Text txtValue;
        [SerializeField] private RectTransform chartBounds; // ChartDots 영역

        [Header("Settings")]
        [SerializeField] private float detectionRadius = 20f;
        [SerializeField] private int edgeNodeCount = 3;
        [SerializeField] private float chartAreaExpansion = 30f; // 오른쪽 여백 확장

        // 데이터
        private List<Transform> chartPoints;
        private ChartData chartData;
        private Canvas canvas;
        private RectTransform tooltipRect;

        // 상태
        private bool wasMouseInChartArea = false;
        private int currentHoveredIndex = -1;

        #region Unity Lifecycle
        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (tooltipObject != null)
            {
                tooltipRect = tooltipObject.GetComponent<RectTransform>();
                tooltipObject.SetActive(false);
            }
        }

        void Update()
        {
            bool isInChart = IsMouseInChartArea();

            // 차트 진입/퇴장 로그
            if (isInChart != wasMouseInChartArea)
            {
                Debug.Log(isInChart ? "🟢 차트 진입!" : "🔴 차트 퇴장!");
                wasMouseInChartArea = isInChart;
            }

            // 차트 안에 있으면 호버 체크
            if (isInChart)
            {
                CheckMouseHover();
            }
            else if (tooltipObject != null && tooltipObject.activeSelf)
            {
                HideTooltip();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 차트 데이터와 포인트 전달
        /// </summary>
        public void Initialize(List<Transform> points, ChartData data)
        {
            chartPoints = points;
            chartData = data;

            Debug.Log($"[ChartTooltipHandler] 초기화: {points?.Count ?? 0}개 포인트");
        }

        /// <summary>
        /// 툴팁 숨기기
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipObject != null)
            {
                tooltipObject.SetActive(false);
            }
            currentHoveredIndex = -1;
        }
        #endregion

        #region Private Methods - Mouse Detection
        /// <summary>
        /// 마우스가 차트 영역 내에 있는지 확인 (약간 확장된 영역)
        /// </summary>
        private bool IsMouseInChartArea()
        {
            if (chartBounds == null) return false;

            if (!TryGetPointerOnCanvas(canvas, out var screenPos))
                return false;

            Vector2 localMousePos;
            bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                chartBounds, screenPos, canvas.worldCamera, out localMousePos);

            if (!ok) return false;

            // 차트 영역 약간 확장 (오른쪽 여백)
            Rect expanded = chartBounds.rect;
            expanded.xMax += chartAreaExpansion;

            return expanded.Contains(localMousePos);
        }

        /// <summary>
        /// 마우스 호버 체크 및 툴팁 표시
        /// </summary>
        private void CheckMouseHover()
        {
            if (chartPoints == null || chartPoints.Count == 0) return;
            if (chartData == null) return;

            // 마우스 위치 → ChartBounds 로컬 좌표
            Vector2 localMousePos;
            Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                chartBounds, Input.mousePosition, cam, out localMousePos))
            {
                HideTooltip();
                return;
            }

            // 차트 영역 확인
            Rect expanded = chartBounds.rect;
            expanded.xMax += chartAreaExpansion;

            if (expanded.Contains(localMousePos))
            {
                int closestIndex = FindClosestPointIndex(localMousePos);

                if (closestIndex >= 0)
                {
                    ShowTooltip(closestIndex);
                }
                else
                {
                    HideTooltip();
                }
            }
            else
            {
                HideTooltip();
            }
        }

        /// <summary>
        /// 멀티 디스플레이 환경에서 정확한 마우스 좌표 계산
        /// </summary>
        private bool TryGetPointerOnCanvas(Canvas canvas, out Vector2 screenPos)
        {
            screenPos = Input.mousePosition;

            // Overlay 모드거나 Editor에서는 바로 반환
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return true;

#if UNITY_EDITOR
            return true;
#else
    // 이하 멀티 디스플레이 로직 (기존 코드 유지)
    int target = canvas.targetDisplay;
    // ... 생략 (기존 코드와 동일)
#endif
        }
        #endregion

        #region Private Methods - Point Detection
        /// <summary>
        /// 마우스 위치에서 가장 가까운 포인트 찾기
        /// </summary>
        private int FindClosestPointIndex(Vector2 localMousePos)
        {
            float minDistance = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < chartPoints.Count; i++)
            {
                if (chartPoints[i] == null) continue;

                // 포인트의 월드 좌표 → ChartBounds 로컬 좌표
                Vector3 pointWorldPos = chartPoints[i].position;
                Vector3 pointLocalPos = chartBounds.InverseTransformPoint(pointWorldPos);

                float distance = Vector2.Distance(localMousePos, new Vector2(pointLocalPos.x, pointLocalPos.y));

                if (distance < detectionRadius && distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }
        #endregion

        #region Private Methods - Tooltip Display
        /// <summary>
        /// 툴팁 표시
        /// </summary>
        private void ShowTooltip(int index)
        {
            if (tooltipObject == null) return;

            // 같은 노드면 업데이트 생략
            if (index == currentHoveredIndex && tooltipObject.activeSelf)
                return;

            currentHoveredIndex = index;

            if (!tooltipObject.activeSelf)
            {
                tooltipObject.SetActive(true);
            }

            UpdateTooltipContent(index);
            UpdateTooltipPosition(index, Vector2.zero); // screenPos 불필요
        }

        /// <summary>
        /// 툴팁 텍스트 업데이트
        /// </summary>
        private void UpdateTooltipContent(int index)
        {
            if (chartData == null || index < 0 || index >= chartData.values.Count)
                return;

            // 시간 계산: startTime + (10분 * index)
            System.DateTime pointTime = chartData.startTime.AddMinutes(10 * index);
            float value = chartData.values[index];

            // 텍스트 업데이트
            if (txtTime != null)
            {
                txtTime.text = pointTime.ToString("yy.MM.dd HH:mm");
            }

            if (txtValue != null)
            {
                txtValue.text = value.ToString("F2");
            }

            Debug.Log($"[Tooltip] [{index}] {pointTime:yyyy-MM-dd HH:mm} = {value:F2}");
        }

        /// <summary>
        /// 툴팁 위치 계산 (양끝 보정 포함)
        /// </summary>
        /// <summary>
        /// 툴팁 위치 계산 (ChartBounds 기준 - 차트 그리듯이)
        /// </summary>
        private void UpdateTooltipPosition(int index, Vector2 screenPos)
        {
            if (tooltipRect == null || chartBounds == null || chartPoints == null) return;
            if (index < 0 || index >= chartPoints.Count) return;

            Transform pointTransform = chartPoints[index];
            if (pointTransform == null) return;

            // 1. 포인트의 월드 좌표 가져오기
            Vector3 pointWorldPos = pointTransform.position;

            // 2. 월드 좌표 → ChartBounds 로컬 좌표
            Vector3 localPos = chartBounds.InverseTransformPoint(pointWorldPos);

            // 3. 툴팁 위치 조정 (포인트 위)
            localPos.y += 100f; 

            // 4. 양끝 노드 X축 보정
            int totalPoints = chartPoints.Count;

            if (index < edgeNodeCount) // 왼쪽 끝
            {
                localPos.x += 100f; // 오른쪽으로
            }
            else if (index >= totalPoints - edgeNodeCount) // 오른쪽 끝
            {
                localPos.x -= 100f; // 왼쪽으로
            }

            // 5. 최종 위치 적용
            tooltipRect.anchoredPosition = localPos;
        }
        #endregion
    }
}