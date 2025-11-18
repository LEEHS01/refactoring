// Common/UI/ChartLineRenderer2.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Common.UI
{
    /// <summary>
    /// 성능 최적화된 차트 라인 렌더러 (Prefab Point 재활용)
    /// - 오브젝트 풀 제거
    /// - Instantiate/Destroy 제거
    /// - 기존 Point 1~24 재활용
    /// - 27개 차트 동시 업데이트 대응
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ChartLineRenderer2 : MaskableGraphic
    {
        #region Inspector Fields
        [Header("Line Settings")]
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private Color lineColor = Color.cyan;

        [Header("Point Settings")]
        [SerializeField][Range(0.1f, 2f)] private float dotScale = 0.1f;
        #endregion

        #region Private Fields
        private List<Transform> cachedPoints = new List<Transform>(); // ⭐ Prefab에서 캐싱
        private List<Vector2> linePoints = new List<Vector2>();
        private RectTransform chartBoundsArea;
        private int activePointCount = 0; // ⭐ 현재 활성화된 포인트 수
        #endregion

        #region Properties
        public int PointCount => activePointCount;
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
        #endregion

        #region Public Methods
        /// <summary>
        /// 초기화 - Prefab의 기존 Point들을 캐싱
        /// </summary>
        public void Initialize(RectTransform chartBounds)
        {
            if (chartBounds == null)
            {
                Debug.LogError("[ChartLineRenderer2] chartBounds가 null입니다!");
                return;
            }

            chartBoundsArea = chartBounds;

            // ⭐ 기존 Point 1~24 찾아서 캐싱 (한 번만!)
            CacheExistingPoints();

            //Debug.Log($"[ChartLineRenderer2] 초기화 완료: {cachedPoints.Count}개 포인트 캐싱됨");
        }

        /// <summary>
        /// 차트 업데이트 - 위치만 갱신 (생성/파괴 X)
        /// </summary>
        public void UpdateChart(List<float> normalizedValues)
        {
            if (chartBoundsArea == null)
            {
                Debug.LogError("[ChartLineRenderer2] Initialize()를 먼저 호출하세요!");
                return;
            }

            if (normalizedValues == null || normalizedValues.Count == 0)
            {
                Debug.LogWarning("[ChartLineRenderer2] 빈 데이터입니다.");
                ClearChart();
                return;
            }

            var bounds = GetChartBounds();
            if (bounds.max.x - bounds.min.x < 0.1f)
            {
                Debug.LogWarning("[ChartLineRenderer2] 차트 영역이 너무 작습니다.");
                return;
            }

            // ⭐ 필요한 개수만큼만 활성화 (SetActive만 사용)
            int requiredCount = Mathf.Min(normalizedValues.Count, cachedPoints.Count);
            SetActivePoints(requiredCount);

            // ⭐ 활성화된 포인트들의 위치만 업데이트
            UpdatePointPositions(normalizedValues, bounds);
            UpdateLinePoints();

            // ⭐ 데이터 변경 시에만 메시 재생성
            SetVerticesDirty();
        }

        public void ClearChart()
        {
            SetActivePoints(0);
            linePoints.Clear();
            SetVerticesDirty();
        }

        public List<Transform> GetChartPoints()
        {
            return new List<Transform>(cachedPoints);
        }
        #endregion

        #region Private Methods - Point Management

        /// <summary>
        /// Prefab에 이미 존재하는 Point들을 찾아서 캐싱 (한 번만)
        /// </summary>
        private void CacheExistingPoints()
        {
            cachedPoints.Clear();

            // ⭐ 자식 오브젝트 중 "Point"로 시작하는 것들만 찾기
            Transform[] children = transform.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child != transform && child.name.StartsWith("Point"))
                {
                    cachedPoints.Add(child);
                    child.gameObject.SetActive(false); // 초기에는 비활성화
                }
            }

            // 이름 순으로 정렬 (Point 1, Point 2, ... Point 24)
            cachedPoints.Sort((a, b) => string.Compare(a.name, b.name));

            if (cachedPoints.Count == 0)
            {
                Debug.LogWarning("[ChartLineRenderer2] Prefab에 Point가 없습니다! Point 1~24를 추가하세요.");
            }
        }

        /// <summary>
        /// 필요한 개수만큼만 포인트 활성화 (나머지는 비활성화)
        /// </summary>
        private void SetActivePoints(int count)
        {
            count = Mathf.Clamp(count, 0, cachedPoints.Count);
            activePointCount = count;

            for (int i = 0; i < cachedPoints.Count; i++)
            {
                if (cachedPoints[i] != null)
                {
                    cachedPoints[i].gameObject.SetActive(i < count);
                }
            }
        }

        /// <summary>
        /// 활성화된 포인트들의 위치만 업데이트
        /// </summary>
        private void UpdatePointPositions(List<float> normalizedValues, (Vector2 min, Vector2 max) bounds)
        {
            for (int i = 0; i < activePointCount && i < normalizedValues.Count; i++)
            {
                Transform point = cachedPoints[i];
                if (point == null || !point.gameObject.activeSelf) continue;

                // X 위치 계산
                float xRatio = (normalizedValues.Count > 1)
                    ? (float)i / (normalizedValues.Count - 1)
                    : 0.5f;
                float worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, xRatio);

                // Y 위치 계산
                float normalizedY = Mathf.Clamp01(normalizedValues[i]);
                float worldY = Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY);

                // ⭐ 위치만 업데이트 (매우 빠름)
                point.position = new Vector3(worldX, worldY, point.position.z);
            }
        }

        /// <summary>
        /// 라인 그리기를 위한 포인트 좌표 업데이트
        /// </summary>
        private void UpdateLinePoints()
        {
            linePoints.Clear();

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform rectTransform = GetComponent<RectTransform>();

            for (int i = 0; i < activePointCount; i++)
            {
                Transform point = cachedPoints[i];
                if (point == null || !point.gameObject.activeSelf) continue;

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

        #region Cleanup
        protected override void OnDestroy()
        {
            base.OnDestroy();

            // ⭐ 캐싱된 포인트 리스트만 클리어 (오브젝트는 Prefab이 관리)
            cachedPoints.Clear();
            linePoints.Clear();
        }
        #endregion
    }
}