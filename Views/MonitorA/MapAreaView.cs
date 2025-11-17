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
        [SerializeField] private TMP_Text txtAreaTitle;     // TitleObs
        [SerializeField] private Image imgAreaBackground;   // MapImage
        [SerializeField] private Transform markerListParent; // MarkerList

        [Header("Navigation")]
        [SerializeField] private Area3DView area3DView;      // 3D 관측소 화면

        [Header("Canvas Control")]
        [SerializeField] private CanvasGroup canvasGroup;    // 화면 표시 제어
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

            // area3DView는 경고만 (선택적)
            if (area3DView == null)
            {
                LogWarning("area3DView가 연결되지 않았습니다. 3D 화면 전환이 작동하지 않습니다!");
            }

            // CanvasGroup 자동 추가 또는 가져오기
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    LogInfo("CanvasGroup 자동 추가");
                }
            }

            // 마커 View들 수집 (Object Pool)
            _markerViews = markerListParent
                .GetComponentsInChildren<MapAreaMarkerView>(true)
                .ToList();

            LogInfo($"마커 View {_markerViews.Count}개 수집 완료");

            // 초기 상태: 보이지 않게 (GameObject는 활성화 유지!)
            HideMapArea();
        }

        protected override void SetupViewEvents()
        {
            // 각 마커의 클릭 이벤트 구독
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

            // ViewModel 이벤트 구독
            MapAreaViewModel.Instance.OnAreaInfoLoaded.AddListener(OnAreaInfoLoaded);
            MapAreaViewModel.Instance.OnObservatoriesLoaded.AddListener(OnObservatoriesLoaded);
            MapAreaViewModel.Instance.OnError.AddListener(OnError);

            LogInfo("ViewModel 이벤트 구독 완료");
        }

        protected override void DisconnectFromViewModel()
        {
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.OnAreaInfoLoaded.RemoveListener(OnAreaInfoLoaded);
                MapAreaViewModel.Instance.OnObservatoriesLoaded.RemoveListener(OnObservatoriesLoaded);
                MapAreaViewModel.Instance.OnError.RemoveListener(OnError);
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

            // ✅ 모든 AreaListTypeView 비활성화 (Unity 2023+)
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

            // MapArea 표시
            ShowMapArea();
        }

        /// <summary>
        /// 관측소 마커 데이터 로드 완료
        /// </summary>
        private void OnObservatoriesLoaded(List<ObsMarkerData> observatories)
        {
            LogInfo($"관측소 마커 렌더링: {observatories.Count}개");

            // Object Pooling: 활성화/비활성화 + 데이터 바인딩
            for (int i = 0; i < _markerViews.Count; i++)
            {
                if (i < observatories.Count)
                {
                    // 데이터 있음 → 활성화 + 바인딩
                    _markerViews[i].gameObject.SetActive(true);
                    _markerViews[i].Bind(observatories[i]);
                }
                else
                {
                    // 데이터 없음 → 비활성화
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
        #endregion

        #region View 이벤트 핸들러
        /// <summary>
        /// 관측소 마커 클릭 - 3D 화면으로 전환
        /// </summary>
        private void OnObsMarkerClicked(int obsId)
        {
            LogInfo($"관측소 클릭: ObsId={obsId}");

            // 3D 화면 연결 확인
            if (area3DView == null)
            {
                LogError("area3DView가 연결되지 않았습니다! Inspector에서 Area_3D를 연결해주세요.");
                return;
            }

            // 🎯 1단계: 지도 화면 숨김 (CanvasGroup 사용)
            HideMapArea();
            LogInfo("MapArea 화면 숨김 완료");

            // 🎯 2단계: 3D 관측소 화면 표시
            area3DView.ShowObservatory(obsId);
            LogInfo($"3D 관측소 화면 전환 요청: ObsId={obsId}");
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// MapArea 표시 (CanvasGroup 사용)
        /// </summary>
        private void ShowMapArea()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                LogInfo("MapArea 표시");
            }
        }

        /// <summary>
        /// MapArea 숨김 (CanvasGroup 사용)
        /// </summary>
        private void HideMapArea()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                LogInfo("MapArea 숨김");
            }
        }

        /// <summary>
        /// 외부에서 MapArea 복귀 시 호출
        /// </summary>
        public void RestoreMapArea()
        {
            ShowMapArea();
        }
        #endregion

        #region 로깅
        private void LogInfo(string message)
        {
            Debug.Log($"[MapAreaView] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[MapAreaView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MapAreaView] {message}");
        }
        #endregion
    }
}