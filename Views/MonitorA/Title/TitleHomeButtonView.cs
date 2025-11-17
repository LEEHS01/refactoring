using Assets.Scripts_refactoring.Models.MonitorA;
using Assets.Scripts_refactoring.Views.MonitorA;
using HNS.MonitorA.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Views.MonitorA;

namespace HNS.MonitorA.Views
{
    public class TitleHomeButtonView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button btnHome;
        [SerializeField] private TMP_Text lblText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("View References")]
        [SerializeField] private MapNationView mapNationView;

        [Header("Panel References")]
        [SerializeField] private GameObject areaAlarmChartPanel;
        [SerializeField] private GameObject areaListNuclearPanel;
        [SerializeField] private GameObject areaListOceanPanel;

        private void Awake()
        {
            InitializeComponents();
            SetupButton();
        }

        private void Start()
        {
            SubscribeToViewModels();
            HideButton();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModels();

            if (btnHome != null)
                btnHome.onClick.RemoveListener(OnClick);
        }

        private void InitializeComponents()
        {
            if (btnHome == null)
                btnHome = GetComponentInChildren<Button>();

            if (lblText == null)
                lblText = GetComponentInChildren<TMP_Text>();

            if (lblText != null)
                lblText.text = "HOME";

            if (mapNationView == null)
                mapNationView = FindObjectOfType<MapNationView>();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    Debug.Log("[TitleHomeButtonView] CanvasGroup 자동 추가");
                }
            }
        }

        private void SetupButton()
        {
            if (btnHome != null)
                btnHome.onClick.AddListener(OnClick);
        }

        #region ViewModel 이벤트 구독

        private void SubscribeToViewModels()
        {
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.OnAreaInfoLoaded.AddListener(OnAreaEntered);
                MapAreaViewModel.Instance.OnAreaCleared.AddListener(OnHomeReturned);
                Debug.Log("[TitleHomeButtonView] MapAreaViewModel 이벤트 구독");
            }

            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.AddListener(OnObservatoryEntered);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
                Debug.Log("[TitleHomeButtonView] Area3DViewModel 이벤트 구독");
            }
        }

        private void UnsubscribeFromViewModels()
        {
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.OnAreaInfoLoaded.RemoveListener(OnAreaEntered);
                MapAreaViewModel.Instance.OnAreaCleared.RemoveListener(OnHomeReturned);
            }

            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.RemoveListener(OnObservatoryEntered);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }
        }

        #endregion

        #region 이벤트 핸들러

        private void OnAreaEntered(AreaInfoData areaInfo)
        {
            ShowButton();
            Debug.Log($"[TitleHomeButtonView] 지역 진입 ({areaInfo.AreaName}) - HOME 버튼 표시");
        }

        private void OnObservatoryEntered(int obsId)
        {
            ShowButton();
            Debug.Log($"[TitleHomeButtonView] 관측소 진입 (ObsId={obsId}) - HOME 버튼 표시");
        }

        private void OnObservatoryClosed()
        {
            ShowButton();
            Debug.Log($"[TitleHomeButtonView] 관측소 닫기 - HOME 버튼 유지");
        }

        private void OnHomeReturned()
        {
            HideButton();

            // ✅ HOME 복귀 시 AreaListTypeView 활성화
            ShowAreaListViews();

            Debug.Log("[TitleHomeButtonView] HOME 복귀 - HOME 버튼 숨김, AreaListView 표시");
        }

        #endregion

        #region 버튼 표시/숨김

        private void ShowButton()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void HideButton()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        #endregion

        private void OnClick()
        {
            Debug.Log("========================================");
            Debug.Log("HOME 버튼 클릭 - 메인 화면으로 복귀");

            // 1. 지역 데이터 초기화 (OnAreaCleared → OnHomeReturned → ShowAreaListViews)
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.ClearAreaData();
                Debug.Log("[TitleHomeButtonView] MapAreaViewModel.ClearAreaData() 호출");
            }

            // 2. 전국 지도를 전체 화면 모드로
            if (mapNationView != null)
            {
                mapNationView.SwitchToFullscreenMode();
                Debug.Log("[TitleHomeButtonView] 전국 지도 전체 화면 전환");
            }

            Debug.Log("========================================");
        }

        private void ShowAreaListViews()
        {
            if (areaListNuclearPanel != null)
            {
                areaListNuclearPanel.SetActive(true);
                Debug.Log("[TitleHomeButtonView] AreaListTypeNuclear 활성화");
            }

            if (areaListOceanPanel != null)
            {
                areaListOceanPanel.SetActive(true);
                Debug.Log("[TitleHomeButtonView] AreaListTypeOcean 활성화");
            }

            var areaListViews = FindObjectsByType<AreaListTypeView>(FindObjectsSortMode.None);
            if (areaListViews != null && areaListViews.Length > 0)
            {
                foreach (var view in areaListViews)
                {
                    if (view != null)
                    {
                        view.gameObject.SetActive(true);
                        Debug.Log($"[TitleHomeButtonView] AreaListTypeView 활성화: {view.gameObject.name}");
                    }
                }
            }
        }
    }
}