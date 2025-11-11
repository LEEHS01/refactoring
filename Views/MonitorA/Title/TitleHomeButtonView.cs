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
    /// </summary>
    public class TitleHomeButtonView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button btnHome;
        [SerializeField] private TMP_Text lblText;

        [Header("View References")]
        [SerializeField] private MapNationView mapNationView;

        [Header("Panel References (Optional)")]
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

            // ✅ MapNationView 자동 찾기
            if (mapNationView == null)
                mapNationView = FindObjectOfType<MapNationView>();

            // ✅ 패널 자동 찾기 (Inspector 연결이 없을 경우)
            if (areaAlarmChartPanel == null)
            {
                areaAlarmChartPanel = GameObject.Find("PanelAlarmChart");
                if (areaAlarmChartPanel == null)
                    areaAlarmChartPanel = GameObject.Find("AreaAlarmChart");
            }

            if (areaListNuclearPanel == null)
            {
                areaListNuclearPanel = GameObject.Find("AreaListTypeNuclear");
            }

            if (areaListOceanPanel == null)
            {
                areaListOceanPanel = GameObject.Find("AreaListTypeOcean");
            }
        }

        private void SetupButton()
        {
            if (btnHome != null)
                btnHome.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Debug.Log("🏠 HOME 버튼 클릭 - 메인 화면으로 복귀");

            // ✅ 1. ViewModel 데이터 초기화 먼저 (OnAreaCleared 발생 → MapAreaView 자동 비활성화)
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.ClearAreaData();
                Debug.Log("[TitleHomeButtonView] ViewModel 데이터 초기화");
            }

            // ✅ 2. 전국 지도를 전체 화면 모드로
            if (mapNationView != null)
            {
                mapNationView.SwitchToFullscreenMode();
                Debug.Log("[TitleHomeButtonView] 전국 지도 전체 화면 전환");
            }
            else
            {
                Debug.LogError("[TitleHomeButtonView] MapNationView를 찾을 수 없습니다!");
            }

            // ✅ 3. 알람 막대 그래프 숨기기
            HideAlarmChart();

            // ✅ 4. 지역 관측소 모니터링 현황 다시 표시
            ShowAreaListViews();
        }

        /// <summary>
        /// 지역 관측소 모니터링 현황 다시 표시
        /// </summary>
        private void ShowAreaListViews()
        {
            // ✅ 방법 1: Inspector에서 연결한 패널 사용
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

            // ✅ 방법 2: 모든 AreaListTypeView 찾아서 활성화
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

        /// <summary>
        /// 알람 막대 그래프 숨기기
        /// </summary>
        private void HideAlarmChart()
        {
            // ✅ Inspector에서 연결한 패널 사용
            if (areaAlarmChartPanel != null)
            {
                areaAlarmChartPanel.SetActive(false);
                Debug.Log("[TitleHomeButtonView] 알람 차트 비활성화");
                return;
            }

            // ✅ 이름으로 찾기 (여러 가능한 이름 시도)
            string[] possibleNames = {
                "PanelAlarmChart",
                "AreaAlarmChart",
                "AlarmChart",
                "ChartPanel",
                "PanelChart"
            };

            foreach (string name in possibleNames)
            {
                GameObject chart = GameObject.Find(name);
                if (chart != null)
                {
                    chart.SetActive(false);
                    Debug.Log($"[TitleHomeButtonView] 알람 차트 비활성화: {name}");
                    return;
                }
            }

            Debug.LogWarning("[TitleHomeButtonView] 알람 차트를 찾을 수 없습니다!");
        }

        private void OnDestroy()
        {
            if (btnHome != null)
                btnHome.onClick.RemoveListener(OnClick);
        }
    }
}