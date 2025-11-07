using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using HNS.MonitorA.Models;
using HNS.MonitorA.ViewModels;
using Core;

namespace Views.MonitorA  // ✅ 이렇게 수정!
{
    /// <summary>
    /// 전국 지도 View
    /// - 드래그/줌 기능
    /// - 마커 색상 업데이트
    /// </summary>
    public class MapNationView : BaseView,
        IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
    {
        [Header("UI References")]
        [SerializeField] private Image imgBackground;
        [SerializeField] private Transform markerListParent;

        [Header("Drag/Zoom Settings")]
        [SerializeField] private float scrollSpeed = 0.1f;
        [SerializeField] private float maxHorizontalMoveRange = 300f;
        [SerializeField] private float maxVerticalMoveRange = 500f;
        [SerializeField] private float minScale = 0.7f;
        [SerializeField] private float maxScale = 2f;

        private RectTransform _rectTransform;
        private List<MapNationMarkerView> _markerViews;
        private Vector3 _originalPosition = new Vector3(-50, -100, 0);


        #region BaseView 구현

        protected override void InitializeUIComponents()
        {
            _rectTransform = GetComponent<RectTransform>();

            // Inspector 연결 검증
            bool isValid = ValidateComponents(
                (imgBackground, "imgBackground"),
                (markerListParent, "markerListParent")
            );

            if (!isValid)
            {
                LogError("일부 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            // 마커 View들 수집
            _markerViews = markerListParent
                .GetComponentsInChildren<MapNationMarkerView>(true)
                .ToList();

            LogInfo($"마커 View {_markerViews.Count}개 발견");
        }

        protected override void SetupViewEvents()
        {
            // TODO: 마커 클릭 이벤트 (NavigateArea 구현 시)
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
            // TODO: 마커 클릭 이벤트 해제
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        /// <summary>
        /// 마커 데이터 업데이트
        /// </summary>
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
            // TODO: 에러 팝업 표시
        }

        #endregion

        #region 드래그/줌 구현

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 newPos = _rectTransform.localPosition +
                new Vector3(eventData.delta.x, eventData.delta.y, 0);
            _rectTransform.localPosition = ClampPosition(newPos);
        }

        public void OnScroll(PointerEventData eventData)
        {
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
    }
}