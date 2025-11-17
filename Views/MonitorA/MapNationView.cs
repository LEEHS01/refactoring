using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HNS.MonitorA.Models;
using HNS.MonitorA.ViewModels;
using Core;

namespace Views.MonitorA
{
    public class MapNationView : BaseView,
        IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
    {
        [Header("UI References")]
        [SerializeField] private Image imgBackground;
        [SerializeField] private Transform markerListParent;
        [SerializeField] private Canvas parentCanvas;  // 추가: Canvas 참조

        [Header("Drag/Zoom Settings")]
        [SerializeField] private float scrollSpeed = 0.1f;
        [SerializeField] private float maxHorizontalMoveRange = 300f;
        [SerializeField] private float maxVerticalMoveRange = 500f;
        [SerializeField] private float minScale = 0.7f;
        [SerializeField] private float maxScale = 2f;

        [Header("Minimap Settings (Relative)")]
        [SerializeField] private Vector2 minimapOffsetFromCenter = new Vector2(700, 200);  // 중앙 기준 오프셋
        [SerializeField] private float minimapScale = 0.6f;  // 60%
        [SerializeField] private float minimapBackgroundAlpha = 0f;  // 투명
        [SerializeField] private float fullscreenBackgroundAlpha = 0.4f;  // 40%

        private RectTransform _rectTransform;
        private List<MapNationMarkerView> _markerViews;

        private Vector3 _originalPosition = new Vector3(-50, -100, 0);
        private Vector3 _originalScale = new Vector3(1, 1, 1);
        private bool _isDraggable = true;

        #region BaseView 구현

        protected override void InitializeUIComponents()
        {
            _rectTransform = GetComponent<RectTransform>();

            // Canvas 자동 찾기
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            bool isValid = ValidateComponents(
                (imgBackground, "imgBackground"),
                (markerListParent, "markerListParent"),
                (parentCanvas, "parentCanvas")
            );

            if (!isValid)
            {
                LogError("일부 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            _markerViews = markerListParent
                .GetComponentsInChildren<MapNationMarkerView>(true)
                .ToList();

            LogInfo($"마커 View {_markerViews.Count}개 발견");

            // 초기 위치/크기 설정
            _rectTransform.localPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;
        }

        protected override void SetupViewEvents()
        {
            foreach (var markerView in _markerViews)
            {
                if (markerView != null)
                {
                    markerView.OnAreaClicked += OnAreaClicked;
                }
            }

            LogInfo("마커 클릭 이벤트 구독 완료");
        }

        protected override void ConnectToViewModel()
        {
            if (MapNationViewModel.Instance == null)
            {
                LogError("MapNationViewModel.Instance가 null입니다!");
                return;
            }

            MapNationViewModel.Instance.OnMarkersUpdated.AddListener(UpdateMarkers);
            MapNationViewModel.Instance.OnError.AddListener(HandleError);

            LogInfo("ViewModel 이벤트 구독 완료");
        }

        protected override void DisconnectFromViewModel()
        {
            if (MapNationViewModel.Instance != null)
            {
                MapNationViewModel.Instance.OnMarkersUpdated.RemoveListener(UpdateMarkers);
                MapNationViewModel.Instance.OnError.RemoveListener(HandleError);
            }
        }

        protected override void DisconnectViewEvents()
        {
            foreach (var markerView in _markerViews)
            {
                if (markerView != null)
                {
                    markerView.OnAreaClicked -= OnAreaClicked;
                }
            }
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        private void UpdateMarkers(List<MapMarkerData> markerDataList)
        {
            if (markerDataList.Count != _markerViews.Count)
            {
                LogError($"마커 수 불일치! Data={markerDataList.Count}, View={_markerViews.Count}");
                return;
            }

            for (int i = 0; i < _markerViews.Count; i++)
            {
                _markerViews[i].UpdateData(markerDataList[i]);
            }

            LogInfo($"마커 {markerDataList.Count}개 색상 업데이트 완료");
        }

        private void HandleError(string errorMessage)
        {
            LogError($"에러: {errorMessage}");
        }

        #endregion

        #region View 이벤트 핸들러

        private void OnAreaClicked(int areaId)
        {
            LogInfo($"지역 클릭: AreaId={areaId}");

            SwitchToMinimapMode();

            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.LoadAreaData(areaId);
            }
            else
            {
                LogError("MapAreaViewModel.Instance가 null입니다!");
            }
        }

        #endregion

        #region 미니맵 모드 (Screen 해상도 기준)

        /// <summary>
        /// 미니맵 모드로 전환 (Screen 해상도 완전 대응)
        /// </summary>
        public void SwitchToMinimapMode()
        {
            _isDraggable = false;

            // FHD 기준 해상도
            Vector2 referenceFHD = new Vector2(1920f, 1080f);

            // 실제 화면 해상도
            Vector2 actualScreenSize = new Vector2(Screen.width, Screen.height);

            // 해상도 비율 계산
            Vector2 resolutionScale = new Vector2(
                actualScreenSize.x / referenceFHD.x,
                actualScreenSize.y / referenceFHD.y
            );

            // FHD 기준 오프셋에 비율 적용
            Vector2 scaledOffset = new Vector2(
                minimapOffsetFromCenter.x * resolutionScale.x,
                minimapOffsetFromCenter.y * resolutionScale.y
            );

            // Screen 해상도 기준 중앙 좌표 (Canvas가 아님!)
            Vector3 centerPosition = new Vector3(actualScreenSize.x / 2f, actualScreenSize.y / 2f, 0);

            // 최종 목표 위치
            Vector3 targetPosition = centerPosition + new Vector3(scaledOffset.x, scaledOffset.y, 0);

            LogInfo($"실제 화면: {actualScreenSize}");
            LogInfo($"해상도 비율: {resolutionScale}, 스케일된 오프셋: {scaledOffset}");
            LogInfo($"중앙 좌표: {centerPosition}, 목표 위치: {targetPosition}");

            StopAllCoroutines();
            StartCoroutine(AnimateToMinimap(
                minimapBackgroundAlpha,
                targetPosition,
                minimapScale,
                1f
            ));

            LogInfo("미니맵 모드로 전환");
        }

        /// <summary>
        /// 전체 화면 모드로 복귀
        /// </summary>
        public void SwitchToFullscreenMode()
        {
            _isDraggable = true;

            // Screen 해상도 기준 중앙
            Vector2 actualScreenSize = new Vector2(Screen.width, Screen.height);
            Vector3 centerPosition = new Vector3(actualScreenSize.x / 2f, actualScreenSize.y / 2f, 0);

            LogInfo($"전체 화면 중앙: {centerPosition}");

            StopAllCoroutines();
            StartCoroutine(AnimateToMinimap(
                fullscreenBackgroundAlpha,
                centerPosition,
                1f,
                1f
            ));

            // 원본 위치/크기로 복귀
            _rectTransform.localPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;

            LogInfo("전체 화면 모드로 전환");
        }

        /// <summary>
        /// 애니메이션 코루틴
        /// </summary>
        private IEnumerator AnimateToMinimap(float targetAlpha, Vector3 targetPosition, float targetScale, float duration)
        {
            Color startColor = imgBackground.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

            Vector3 startPosition = _rectTransform.position;
            Vector3 startScale = _rectTransform.localScale;
            Vector3 endScale = Vector3.one * targetScale;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 배경 알파 (절반 시간만 사용)
                if (elapsed < duration / 2f)
                {
                    float alphaT = (elapsed / (duration / 2f));
                    imgBackground.color = Color.Lerp(startColor, targetColor, alphaT);
                }

                // 위치
                _rectTransform.position = Vector3.Lerp(startPosition, targetPosition, t);

                // 스케일
                _rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            // 최종값 보정
            imgBackground.color = targetColor;
            _rectTransform.position = targetPosition;
            _rectTransform.localScale = endScale;
        }

        #endregion

        #region 드래그/줌 구현

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDraggable) return;

            Vector3 newPos = _rectTransform.localPosition +
                new Vector3(eventData.delta.x, eventData.delta.y, 0);
            _rectTransform.localPosition = ClampPosition(newPos);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!_isDraggable) return;

            Vector3 newScale = _rectTransform.localScale +
                Vector3.one * eventData.scrollDelta.y * scrollSpeed;
            newScale = ClampScale(newScale);
            newScale.z = 1f;

            _rectTransform.localScale = newScale;
            _rectTransform.localPosition = ClampPosition(_rectTransform.localPosition);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            float scale = (_rectTransform.localScale.x - minScale) / (maxScale - minScale);
            float horizontalRange = Mathf.Lerp(0, maxHorizontalMoveRange, scale);
            float verticalRange = Mathf.Lerp(0, maxVerticalMoveRange, scale);

            position.x = Mathf.Clamp(position.x,
                _originalPosition.x - horizontalRange,
                _originalPosition.x + horizontalRange);
            position.y = Mathf.Clamp(position.y,
                _originalPosition.y - verticalRange,
                _originalPosition.y + verticalRange);
            return position;
        }

        private Vector3 ClampScale(Vector3 scale)
        {
            scale.x = Mathf.Clamp(scale.x, minScale, maxScale);
            scale.y = Mathf.Clamp(scale.y, minScale, maxScale);
            scale.z = 1f;
            return scale;
        }

        public void OnBeginDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData) { }

        #endregion

        private void LogInfo(string message)
        {
            Debug.Log($"[MapNationView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MapNationView] {message}");
        }
    }
}