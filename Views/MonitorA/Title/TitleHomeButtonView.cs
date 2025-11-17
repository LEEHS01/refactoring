using Assets.Scripts_refactoring.Views.MonitorA;
using HNS.MonitorA.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Views.MonitorA;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// HOME 버튼 View - 메인 화면 복귀
    /// ViewModel 패턴 준수: View를 직접 제어하지 않고 ViewModel을 통해 처리
    /// </summary>
    public class TitleHomeButtonView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button btnHome;
        [SerializeField] private TMP_Text lblText;

        [Header("View References")]
        [SerializeField] private MapNationView mapNationView;

        [Header("Panel References (사용 안 함 - ViewModel이 처리)")]
        [SerializeField] private GameObject areaAlarmChartPanel;
        [SerializeField] private GameObject areaListNuclearPanel;
        [SerializeField] private GameObject areaListOceanPanel;

        private void Awake()
        {
            InitializeComponents();
            SetupButton();
        }

        private void InitializeComponents()
        {
            if (btnHome == null)
                btnHome = GetComponentInChildren<Button>();

            if (lblText == null)
                lblText = GetComponentInChildren<TMP_Text>();

            if (lblText != null)
                lblText.text = "HOME";

            // MapNationView 자동 찾기
            if (mapNationView == null)
                mapNationView = FindObjectOfType<MapNationView>();
        }

        private void SetupButton()
        {
            if (btnHome != null)
                btnHome.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Debug.Log("========================================");
            Debug.Log("HOME 버튼 클릭 - 메인 화면으로 복귀");

            // 1. ViewModel 데이터 초기화 (이벤트 체인 시작)
            //    MapAreaViewModel.ClearAreaData() 호출
            //    → MapAreaViewModel.OnAreaCleared 이벤트 발행
            //    → MapAreaView가 숨김 처리 (CanvasGroup)
            //    → AreaAlarmChartViewModel이 OnAreaCleared 수신
            //    → AreaAlarmChartViewModel.OnAreaExited 발행
            //    → AreaAlarmChartView가 숨김 처리 (CanvasGroup)
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.ClearAreaData();
                Debug.Log("[TitleHomeButtonView] MapAreaViewModel.ClearAreaData() 호출");
                Debug.Log("[TitleHomeButtonView] → MapAreaView 자동 숨김 처리됨 (CanvasGroup)");
                Debug.Log("[TitleHomeButtonView] → AreaAlarmChartView 자동 숨김 처리됨 (CanvasGroup)");
            }
            else
            {
                Debug.LogError("[TitleHomeButtonView] MapAreaViewModel.Instance가 null입니다!");
            }

            // 2. 전국 지도를 전체 화면 모드로
            if (mapNationView != null)
            {
                mapNationView.SwitchToFullscreenMode();
                Debug.Log("[TitleHomeButtonView] 전국 지도 전체 화면 전환");
            }
            else
            {
                Debug.LogError("[TitleHomeButtonView] MapNationView를 찾을 수 없습니다!");
            }

            // 3. 지역 관측소 모니터링 현황 다시 표시
            ShowAreaListViews();

            Debug.Log("========================================");
        }

        /// <summary>
        /// 지역 관측소 모니터링 현황 다시 표시
        /// </summary>
        private void ShowAreaListViews()
        {
            // 방법 1: Inspector에서 연결한 패널 사용
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

            // 방법 2: 모든 AreaListTypeView 찾아서 활성화
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
            else
            {
                Debug.LogWarning("[TitleHomeButtonView] AreaListTypeView를 찾을 수 없습니다!");
            }
        }

        private void OnDestroy()
        {
            if (btnHome != null)
                btnHome.onClick.RemoveListener(OnClick);
        }
    }
}