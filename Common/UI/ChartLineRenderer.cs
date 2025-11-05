// Common/UI/ChartLineRenderer.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Common.UI
{
    /// <summary>
    /// 최적화된 차트 라인 렌더러
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ChartLineRenderer : MaskableGraphic
    {
        #region Inspector Fields
        [Header("Line Settings")]
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private Color lineColor = Color.cyan;

        [Header("Point Settings")]
        [SerializeField][Range(0.1f, 2f)] private float dotScale = 0.5f;
        #endregion

        #region Private Fields
        private List<Transform> chartPoints = new List<Transform>();
        private List<Vector2> linePoints = new List<Vector2>();
        private GameObject pointPrefab;
        private RectTransform chartBoundsArea;

        // ⭐ 오브젝트 풀
        private Queue<GameObject> pointPool = new Queue<GameObject>();
        private const int INITIAL_POOL_SIZE = 100;
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

        // ⭐ Update() 제거! - 불필요한 메시 재생성 방지
        #endregion

        #region Public Methods
        public void Initialize(RectTransform chartBounds)
        {
            if (chartBounds == null)
            {
                Debug.LogError("[ChartLineRenderer] chartBounds가 null입니다!");
                return;
            }

            chartBoundsArea = chartBounds;

            // ⭐ 오브젝트 풀 초기화
            InitializePool();

            //Debug.Log($"[ChartLineRenderer] 초기화 완료: {chartBounds.name}");
        }

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

            // ⭐ 포인트 재사용
            SetPointCount(normalizedValues.Count);
            UpdatePointPositions(normalizedValues, bounds);
            UpdateLinePoints();

            // ⭐ 데이터 변경 시에만 메시 재생성
            SetVerticesDirty();
        }

        public void ClearChart()
        {
            ReturnAllPointsToPool();
            linePoints.Clear();
            SetVerticesDirty();
        }

        public List<Transform> GetChartPoints()
        {
            return new List<Transform>(chartPoints);
        }
        #endregion

        #region Private Methods - Object Pooling

        /// <summary>
        /// 오브젝트 풀 초기화
        /// </summary>
        private void InitializePool()
        {
            if (pointPrefab == null)
            {
                pointPrefab = CreateDefaultPointPrefab();
            }

            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                GameObject point = Instantiate(pointPrefab, transform);
                point.SetActive(false);
                pointPool.Enqueue(point);
            }
        }

        /// <summary>
        /// 풀에서 포인트 가져오기
        /// </summary>
        private GameObject GetPointFromPool()
        {
            GameObject point;

            if (pointPool.Count > 0)
            {
                point = pointPool.Dequeue();
            }
            else
            {
                // 풀이 비었으면 새로 생성
                point = Instantiate(pointPrefab, transform);
            }

            point.SetActive(true);
            return point;
        }

        /// <summary>
        /// 포인트를 풀에 반환
        /// </summary>
        private void ReturnPointToPool(GameObject point)
        {
            if (point != null)
            {
                point.SetActive(false);
                pointPool.Enqueue(point);
            }
        }

        /// <summary>
        /// 모든 포인트를 풀에 반환
        /// </summary>
        private void ReturnAllPointsToPool()
        {
            foreach (var point in chartPoints)
            {
                if (point != null)
                {
                    ReturnPointToPool(point.gameObject);
                }
            }
            chartPoints.Clear();
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
            // 현재 포인트 수와 같으면 재사용
            if (chartPoints.Count == count)
            {
                return;
            }

            // 더 많으면 여분 반환
            while (chartPoints.Count > count)
            {
                int lastIndex = chartPoints.Count - 1;
                ReturnPointToPool(chartPoints[lastIndex].gameObject);
                chartPoints.RemoveAt(lastIndex);
            }

            // 부족하면 풀에서 가져오기
            var bounds = GetChartBounds();
            while (chartPoints.Count < count)
            {
                GameObject newPoint = GetPointFromPool();
                newPoint.name = $"Point_{chartPoints.Count}";
                newPoint.transform.localScale = Vector3.one * dotScale;

                int i = chartPoints.Count;
                float xRatio = (count > 1) ? (float)i / (count - 1) : 0.5f;
                float worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, xRatio);
                float worldY = bounds.min.y;
                newPoint.transform.position = new Vector3(worldX, worldY, 0);

                chartPoints.Add(newPoint.transform);
            }
        }

        private GameObject CreateDefaultPointPrefab()
        {
            GameObject point = new GameObject("ChartPoint");
            Image img = point.AddComponent<Image>();
            img.sprite = null;
            img.color = lineColor;

            RectTransform rect = point.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(8, 8);

            point.SetActive(false);
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // ⭐ 정리
            ReturnAllPointsToPool();

            // 풀의 모든 오브젝트 파괴
            while (pointPool.Count > 0)
            {
                GameObject point = pointPool.Dequeue();
                if (point != null)
                {
                    Destroy(point);
                }
            }
        }
        #endregion
    }
}