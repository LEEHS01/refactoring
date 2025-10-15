// Assets/Scripts_refactoring/Common/UI/ChartLineRenderer.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace Common.UI
{
    /// <summary>
    /// 차트 라인을 그리는 커스텀 UI 컴포넌트 (순수 렌더링만 담당)
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ChartLineRenderer : MaskableGraphic
    {
        #region Inspector Fields
        [Header("Line Settings")]
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private Color lineColor = Color.cyan;
        #endregion

        #region Private Fields
        private List<Transform> chartPoints = new List<Transform>();
        private List<Vector2> linePoints = new List<Vector2>();
        private GameObject pointPrefab;
        private RectTransform chartBoundsArea; // View에서 설정받음
        #endregion

        #region Properties
        public int PointCount => chartPoints.Count;
        public Color LineColor
        {
            get => lineColor;
            set
            {
                lineColor = value;
                SetVerticesDirty();
            }
        }
        #endregion

        #region Unity Lifecycle
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (linePoints.Count < 2)
            {
                return;
            }

            DrawLines(vh, linePoints);
        }

        void Update()
        {
            SetVerticesDirty();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 차트 영역 설정 (View에서 호출)
        /// </summary>
        public void Initialize(RectTransform chartBounds)
        {
            if (chartBounds == null)
            {
                Debug.LogError("[ChartLineRenderer] chartBounds가 null입니다!");
                return;
            }

            chartBoundsArea = chartBounds;
            Debug.Log($"[ChartLineRenderer] 초기화 완료: {chartBounds.name}");
        }

        /// <summary>
        /// 정규화된 값들(0~1)로 차트 업데이트
        /// </summary>
        public void UpdateChart(List<float> normalizedValues)
        {
            if (chartBoundsArea == null)
            {
                Debug.LogError("[ChartLineRenderer] Initialize()를 먼저 호출하세요!");
                return;
            }

            if (normalizedValues == null || normalizedValues.Count == 0)
            {
                Debug.LogWarning("[ChartLineRenderer] 빈 데이터입니다.");
                ClearChart();
                return;
            }

            var bounds = GetChartBounds();
            if (bounds.max.x - bounds.min.x < 0.1f)
            {
                Debug.LogWarning("[ChartLineRenderer] 차트 영역이 너무 작습니다.");
                return;
            }

            // 점 개수 조정
            if (chartPoints.Count != normalizedValues.Count)
            {
                SetPointCount(normalizedValues.Count);
            }

            // 점 위치 계산 및 배치
            UpdatePointPositions(normalizedValues, bounds);

            // 라인 포인트 업데이트
            UpdateLinePoints();

            // 메쉬 갱신
            SetVerticesDirty();
        }

        /// <summary>
        /// 차트 초기화
        /// </summary>
        public void ClearChart()
        {
            ClearAllPoints();
            linePoints.Clear();
            SetVerticesDirty();
        }

        /// <summary>
        /// 차트 포인트 리스트 반환 (툴팁용)
        /// </summary>
        public List<Transform> GetChartPoints()
        {
            return new List<Transform>(chartPoints);
        }
        #endregion

        #region Private Methods - Chart Bounds
        private (Vector2 min, Vector2 max) GetChartBounds()
        {
            Rect rect = chartBoundsArea.rect;

            Vector3 worldMin = chartBoundsArea.TransformPoint(new Vector3(rect.xMin, rect.yMin, 0));
            Vector3 worldMax = chartBoundsArea.TransformPoint(new Vector3(rect.xMax, rect.yMax, 0));

            return (
                new Vector2(worldMin.x, worldMin.y),
                new Vector2(worldMax.x, worldMax.y)
            );
        }
        #endregion

        #region Private Methods - Point Management
        private void SetPointCount(int count)
        {
            ClearAllPoints();

            if (pointPrefab == null)
            {
                pointPrefab = CreateDefaultPointPrefab();
            }

            var bounds = GetChartBounds();
            for (int i = 0; i < count; i++)
            {
                GameObject newPoint = Instantiate(pointPrefab, transform);
                newPoint.name = $"Point_{i}";
                newPoint.transform.localScale = Vector3.one * 0.5f;
                newPoint.SetActive(true);

                float xRatio = (count > 1) ? (float)i / (count - 1) : 1.5f;
                float worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, xRatio);
                float worldY = bounds.min.y;
                newPoint.transform.position = new Vector3(worldX, worldY, 0);

                chartPoints.Add(newPoint.transform);
            }
        }

        private void ClearAllPoints()
        {
            foreach (var point in chartPoints)
            {
                if (point != null)
                {
                    Destroy(point.gameObject);
                }
            }
            chartPoints.Clear();
        }

        private GameObject CreateDefaultPointPrefab()
        {
            GameObject point = new GameObject("ChartPoint");
            Image img = point.AddComponent<Image>();
            img.sprite = null;
            img.color = lineColor;

            RectTransform rect = point.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(16, 16);

            return point;
        }

        private void UpdatePointPositions(List<float> normalizedValues, (Vector2 min, Vector2 max) bounds)
        {
            for (int i = 0; i < chartPoints.Count && i < normalizedValues.Count; i++)
            {
                Transform point = chartPoints[i];
                if (point == null) continue;

                float xRatio = (normalizedValues.Count > 1)
                    ? (float)i / (normalizedValues.Count - 1)
                    : 0.5f;
                float worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, xRatio);

                float normalizedY = Mathf.Clamp01(normalizedValues[i]);
                float worldY = Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY);

                point.position = new Vector3(worldX, worldY, point.position.z);
            }
        }

        private void UpdateLinePoints()
        {
            linePoints.Clear();

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform rectTransform = GetComponent<RectTransform>();

            foreach (var point in chartPoints)
            {
                if (point == null) continue;

                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    canvas.worldCamera, point.position);

                Vector2 localPoint;
                bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPoint, canvas.worldCamera, out localPoint);

                if (success)
                {
                    linePoints.Add(localPoint);
                }
            }
        }
        #endregion

        #region Private Methods - Line Drawing
        private void DrawLines(VertexHelper vh, List<Vector2> points)
        {
            if (points.Count < 2) return;

            for (int i = 0; i < points.Count - 1; i++)
            {
                DrawLineSegment(vh, points[i], points[i + 1]);
            }
        }

        private void DrawLineSegment(VertexHelper vh, Vector2 start, Vector2 end)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (lineThickness / 2f);

            Vector2 v0 = start - perpendicular;
            Vector2 v1 = start + perpendicular;
            Vector2 v2 = end + perpendicular;
            Vector2 v3 = end - perpendicular;

            int vertexStartIndex = vh.currentVertCount;

            vh.AddVert(v0, lineColor, Vector2.zero);
            vh.AddVert(v1, lineColor, Vector2.zero);
            vh.AddVert(v2, lineColor, Vector2.zero);
            vh.AddVert(v3, lineColor, Vector2.zero);

            vh.AddTriangle(vertexStartIndex + 0, vertexStartIndex + 1, vertexStartIndex + 2);
            vh.AddTriangle(vertexStartIndex + 2, vertexStartIndex + 3, vertexStartIndex + 0);
        }
        #endregion
    }
}