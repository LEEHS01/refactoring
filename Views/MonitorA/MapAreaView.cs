using Assets.Scripts_refactoring.Models.MonitorA;
using Core;
using HNS.MonitorA.ViewModels;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 지역 상세 지도 View
    /// Monitor B 패턴 적용
    /// </summary>
    public class MapAreaView : BaseView
    {
        #region Inspector 설정
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtAreaTitle;
        [SerializeField] private Image imgAreaBackground;
        [SerializeField] private Transform markerListParent;
        [SerializeField] private CanvasGroup canvasGroup;  // ✅ 추가
        #endregion

        #region Private Fields
        private List<MapAreaMarkerView> _markerViews;
        #endregion

        #region BaseView 구현
        protected override void InitializeUIComponents()
        {
            // Inspector 연결 검증
            bool isValid = ValidateComponents(
                (txtAreaTitle, "txtAreaTitle"),
                (imgAreaBackground, "imgAreaBackground"),
                (markerListParent, "markerListParent")
            );

            if (!isValid)
            {
                LogError("일부 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            // ✅ CanvasGroup 자동 추가
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 마커 View들 수집
            _markerViews = markerListParent
                .GetComponentsInChildren<MapAreaMarkerView>(true)
                .ToList();

            LogInfo($"마커 View {_markerViews.Count}개 수집 완료");

            // ✅ 초기 숨김 (GameObject는 활성화 유지)
            HideView();
        }

        protected override void SetupViewEvents()
        {
            foreach (var markerView in _markerViews)
            {
                if (markerView != null)
                {
                    markerView.OnObsClicked += OnObsMarkerClicked;
                }
            }

            LogInfo("마커 이벤트 구독 완료");
        }

        protected override void ConnectToViewModel()
        {
            if (MapAreaViewModel.Instance == null)
            {
                LogError("MapAreaViewModel.Instance가 null입니다!");
                return;
            }

            MapAreaViewModel.Instance.OnAreaInfoLoaded.AddListener(OnAreaInfoLoaded);
            MapAreaViewModel.Instance.OnObservatoriesLoaded.AddListener(OnObservatoriesLoaded);
            MapAreaViewModel.Instance.OnError.AddListener(OnError);
            MapAreaViewModel.Instance.OnAreaCleared.AddListener(OnAreaCleared);

            LogInfo("ViewModel 이벤트 구독 완료");
        }

        protected override void DisconnectFromViewModel()
        {
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.OnAreaInfoLoaded.RemoveListener(OnAreaInfoLoaded);
                MapAreaViewModel.Instance.OnObservatoriesLoaded.RemoveListener(OnObservatoriesLoaded);
                MapAreaViewModel.Instance.OnError.RemoveListener(OnError);
                MapAreaViewModel.Instance.OnAreaCleared.RemoveListener(OnAreaCleared);
            }
        }

        protected override void DisconnectViewEvents()
        {
            foreach (var markerView in _markerViews)
            {
                if (markerView != null)
                {
                    markerView.OnObsClicked -= OnObsMarkerClicked;
                }
            }
        }
        #endregion

        #region ViewModel 이벤트 핸들러
        /// <summary>
        /// 지역 정보 로드 완료
        /// </summary>
        private void OnAreaInfoLoaded(AreaInfoData data)
        {
            LogInfo($"지역 정보 수신: {data.AreaName}");

            // ✅ 모든 AreaListTypeView 비활성화
            var areaListViews = FindObjectsByType<AreaListTypeView>(FindObjectsSortMode.None);
            foreach (var view in areaListViews)
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    LogInfo($"AreaListTypeView 비활성화: {view.gameObject.name}");
                }
            }

            // 지역명 표시
            if (txtAreaTitle != null)
            {
                txtAreaTitle.text = data.AreaName;
            }

            // 배경 이미지 변경
            if (imgAreaBackground != null)
            {
                Sprite areaSprite = Resources.Load<Sprite>(data.ImagePath);

                if (areaSprite != null)
                {
                    imgAreaBackground.sprite = areaSprite;
                    LogInfo($"배경 이미지 로드 성공: {data.ImagePath}");
                }
                else
                {
                    LogError($"배경 이미지 로드 실패: {data.ImagePath}");
                }
            }

            // ✅ View 표시 (GameObject는 계속 active)
            ShowView();
            LogInfo("MapAreaView 표시");
        }

        /// <summary>
        /// 관측소 마커 데이터 로드 완료
        /// </summary>
        private void OnObservatoriesLoaded(List<ObsMarkerData> observatories)
        {
            LogInfo($"관측소 마커 렌더링: {observatories.Count}개");

            // Object Pooling
            for (int i = 0; i < _markerViews.Count; i++)
            {
                if (i < observatories.Count)
                {
                    _markerViews[i].gameObject.SetActive(true);
                    _markerViews[i].Bind(observatories[i]);
                }
                else
                {
                    _markerViews[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 에러 처리
        /// </summary>
        private void OnError(string errorMessage)
        {
            LogError($"ViewModel 에러: {errorMessage}");
        }

        /// <summary>
        /// HOME 복귀 시 지역 지도 숨김
        /// </summary>
        private void OnAreaCleared()
        {
            LogInfo("HOME 복귀: 지역 지도 숨김");
            HideView();
        }
        #endregion

        #region View 표시/숨김
        /// <summary>
        /// View 표시 (CanvasGroup 사용)
        /// </summary>
        private void ShowView()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// View 숨김 (CanvasGroup 사용)
        /// </summary>
        private void HideView()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
        #endregion

        #region View 이벤트 핸들러
        /// <summary>
        /// 관측소 마커 클릭
        /// </summary>
        private void OnObsMarkerClicked(int obsId)
        {
            LogInfo($"관측소 클릭: ObsId={obsId}");
            // TODO: Observatory3DViewModel.Instance.LoadObsData(obsId);
        }
        #endregion

        #region 로깅
        private void LogInfo(string message)
        {
            Debug.Log($"[MapAreaView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MapAreaView] {message}");
        }
        #endregion
    }
}