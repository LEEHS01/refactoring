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
    /// </summary>
    public class MapAreaView : BaseView
    {
        #region Inspector 설정
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtAreaTitle;           // TitleAreaName > Text
        [SerializeField] private GameObject titleAreaNamePanel;   // TitleAreaName GameObject
        [SerializeField] private GameObject titleObsNamePanel;    // TitleObsName GameObject
        [SerializeField] private Image imgAreaBackground;
        [SerializeField] private Transform markerListParent;

        [Header("Navigation")]
        [SerializeField] private Area3DView area3DView;

        [Header("Hide on Area Enter")]
        [SerializeField] private List<GameObject> hideOnAreaEnter = new List<GameObject>();

        [Header("Canvas Control")]
        [SerializeField] private CanvasGroup canvasGroup;
        #endregion

        #region Private Fields
        private List<MapAreaMarkerView> _markerViews;
        #endregion

        #region Unity Lifecycle Override

        // ⭐ BaseView의 OnDisable을 오버라이드하여 이벤트 구독 유지
        protected override void OnDisable()
        {
            LogInfo("OnDisable 호출 - 이벤트 구독 유지 (오버라이드)");
            // ❌ base.OnDisable() 호출하지 않음!
            // 이벤트 구독을 해제하지 않고 유지
        }

        #endregion

        #region BaseView 구현
        protected override void InitializeUIComponents()
        {
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

            if (area3DView == null)
            {
                LogWarning("area3DView가 연결되지 않았습니다. 3D 화면 전환이 작동하지 않습니다!");
            }

            // ⭐ GameObject 먼저 활성화
            if (titleAreaNamePanel != null)
            {
                titleAreaNamePanel.SetActive(true);
                LogInfo("titleAreaNamePanel 활성화");
            }
            else
            {
                LogWarning("titleAreaNamePanel이 연결되지 않았습니다.");
            }

            if (titleObsNamePanel != null)
            {
                titleObsNamePanel.SetActive(true);
                LogInfo("titleObsNamePanel 활성화");
            }
            else
            {
                LogWarning("titleObsNamePanel이 연결되지 않았습니다.");
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    LogInfo("CanvasGroup 자동 추가");
                }
            }

            _markerViews = markerListParent
                .GetComponentsInChildren<MapAreaMarkerView>(true)
                .ToList();

            LogInfo($"마커 View {_markerViews.Count}개 수집 완료");

            // ⭐ GameObject 활성화 후 CanvasGroup으로 숨김
            HideMapArea();
            HideTitleAreaName();
            HideTitleObsName();
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
            MapAreaViewModel.Instance.OnAreaCleared.AddListener(OnAreaCleared);  // ⭐ 추가!
            MapAreaViewModel.Instance.OnError.AddListener(OnError);

            LogInfo("MapAreaViewModel 이벤트 구독 완료");

            // ⭐ Area3DViewModel 이벤트도 구독
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.AddListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
                LogInfo("Area3DViewModel 이벤트 구독 완료");
            }
            else
            {
                LogWarning("Area3DViewModel.Instance가 null입니다!");
            }
        }

        protected override void DisconnectFromViewModel()
        {
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.OnAreaInfoLoaded.RemoveListener(OnAreaInfoLoaded);
                MapAreaViewModel.Instance.OnObservatoriesLoaded.RemoveListener(OnObservatoriesLoaded);
                MapAreaViewModel.Instance.OnAreaCleared.RemoveListener(OnAreaCleared);  // ⭐ 추가!
                MapAreaViewModel.Instance.OnError.RemoveListener(OnError);
            }

            // ⭐ Area3DViewModel 이벤트 구독 해제
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.RemoveListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
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

        private void OnAreaInfoLoaded(AreaInfoData data)
        {
            LogInfo("========================================");
            LogInfo($"지역 정보 수신: {data.AreaName}");

            // ✅ Inspector에서 연결된 오브젝트들 숨김 (AreaListTypeView 등)
            if (hideOnAreaEnter != null && hideOnAreaEnter.Count > 0)
            {
                LogInfo($"숨길 오브젝트 개수: {hideOnAreaEnter.Count}");
                foreach (var obj in hideOnAreaEnter)
                {
                    if (obj != null)
                    {
                        bool wasActive = obj.activeSelf;
                        obj.SetActive(false);
                        LogInfo($"오브젝트 비활성화: {obj.name} (이전 상태: {wasActive})");
                    }
                }
            }
            else
            {
                LogWarning("hideOnAreaEnter가 비어있습니다! Inspector에서 AreaListTypeView GameObject들을 연결하세요.");
            }

            // 지역명 표시
            if (txtAreaTitle != null)
            {
                txtAreaTitle.text = data.AreaName;
                LogInfo($"지역명 설정: {data.AreaName}");
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

            // ⭐ 지역 지도 화면: 지역명만 표시
            ShowMapArea();
            ShowTitleAreaName();    // ⭐ 지역명 표시
            HideTitleObsName();     // ⭐ 관측소명 숨김

            LogInfo("MapArea 표시 완료!");
            LogInfo("========================================");
        }

        private void OnObservatoriesLoaded(List<ObsMarkerData> observatories)
        {
            LogInfo($"관측소 마커 렌더링: {observatories.Count}개");

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

        // ⭐ HOME 복귀 시
        private void OnAreaCleared()
        {
            LogInfo("========================================");
            LogInfo("HOME 복귀 - 모든 Title 숨김");

            HideTitleAreaName();    // ⭐ 지역명 숨김
            HideTitleObsName();     // ⭐ 관측소명 숨김

            LogInfo("HOME 복귀 완료!");
            LogInfo("========================================");
        }

        // ⭐ 3D 관측소 진입 시
        private void OnObservatoryLoaded(int obsId)
        {
            LogInfo($"========================================");
            LogInfo($"3D 관측소 진입: ObsId={obsId}");

            // ⭐ 관측소 화면: 지역명 + 관측소명 모두 표시
            ShowTitleAreaName();        // ⭐ 지역명 표시 (인천)
            ShowTitleObsName(obsId);    // ⭐ 관측소명 표시 (지역1)

            LogInfo($"관측소 화면 표시 완료!");
            LogInfo($"========================================");
        }

        // ⭐ 3D 관측소 닫기 시 (지역 지도로 복귀)
        private void OnObservatoryClosed()
        {
            LogInfo("========================================");
            LogInfo("3D 관측소 닫기 - 지역 지도로 복귀");

            ShowTitleAreaName();    // ⭐ 지역명 유지
            HideTitleObsName();     // ⭐ 관측소명 숨김

            LogInfo("지역 지도 복귀 완료!");
            LogInfo("========================================");
        }

        private void OnError(string errorMessage)
        {
            LogError($"ViewModel 에러: {errorMessage}");
        }
        #endregion

        #region View 이벤트 핸들러

        private void OnObsMarkerClicked(int obsId)
        {
            LogInfo($"관측소 클릭: ObsId={obsId}");

            if (area3DView == null)
            {
                LogError("area3DView가 연결되지 않았습니다! Inspector에서 Area_3D를 연결해주세요.");
                return;
            }

            HideMapArea();
            LogInfo("MapArea 화면 숨김 완료");

            area3DView.ShowObservatory(obsId);
            LogInfo($"3D 관측소 화면 전환 요청: ObsId={obsId}");
        }
        #endregion

        #region Helper Methods

        private void ShowMapArea()
        {
            // ⭐ GameObject가 비활성화되어 있으면 먼저 활성화
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                LogInfo("MapArea GameObject 강제 활성화");
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                LogInfo($"MapArea 표시 (CanvasGroup)");
            }
            else
            {
                LogError("CanvasGroup이 null입니다!");
            }
        }

        private void HideMapArea()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                LogInfo("MapArea 숨김 (CanvasGroup) - GameObject는 활성화 유지");
            }
        }

        // ⭐ 지역명 표시
        private void ShowTitleAreaName()
        {
            if (titleAreaNamePanel != null)
            {
                // GameObject는 항상 활성화 상태 유지
                if (!titleAreaNamePanel.activeSelf)
                {
                    titleAreaNamePanel.SetActive(true);
                }

                // CanvasGroup으로 표시
                var cg = titleAreaNamePanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                LogInfo("TitleAreaName 표시");
            }
        }

        // ⭐ 지역명 숨김
        private void HideTitleAreaName()
        {
            if (titleAreaNamePanel != null)
            {
                // GameObject는 활성화 유지하고 CanvasGroup으로만 숨김
                var cg = titleAreaNamePanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }

                LogInfo("TitleAreaName 숨김");
            }
        }

        // ⭐ 관측소명 표시
        private void ShowTitleObsName(int obsId)
        {
            if (titleObsNamePanel != null)
            {
                // GameObject는 항상 활성화 상태 유지
                if (!titleObsNamePanel.activeSelf)
                {
                    titleObsNamePanel.SetActive(true);
                }

                // 텍스트 설정
                var txtObsName = titleObsNamePanel.GetComponentInChildren<TMP_Text>();
                if (txtObsName != null)
                {
                    txtObsName.text = $"지역{obsId}";
                }

                // CanvasGroup으로 표시
                var cg = titleObsNamePanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                LogInfo($"TitleObsName 표시: 지역{obsId}");
            }
        }

        // ⭐ 관측소명 숨김
        private void HideTitleObsName()
        {
            if (titleObsNamePanel != null)
            {
                // GameObject는 활성화 유지하고 CanvasGroup으로만 숨김
                var cg = titleObsNamePanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }

                LogInfo("TitleObsName 숨김");
            }
        }

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