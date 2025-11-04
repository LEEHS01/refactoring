using UnityEngine;
using UnityEngine.UI;
using ViewModels.MonitorB;
using Core;

namespace Views.MonitorB
{
    /// <summary>
    /// CCTV 버튼 - 클릭 시 Panel_Video 활성화
    /// </summary>
    public class CCTVButtonView : BaseView
    {
        [Header("References")]
        [SerializeField] private GameObject panelVideo;  // ⭐ Panel_Video 연결
        [SerializeField] private Button button;

        protected override void InitializeUIComponents()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button == null)
            {
                LogError("Button 컴포넌트를 찾을 수 없습니다!");
            }
        }

        protected override void SetupViewEvents()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClickCCTV);
            }
        }

        private void OnClickCCTV()
        {
            if (panelVideo == null)
            {
                LogError("panelVideo가 할당되지 않았습니다!");
                return;
            }

            // ⭐ 현재 선택된 관측소 ID 가져오기
            if (SensorMonitorViewModel.Instance == null)
            {
                LogError("SensorMonitorViewModel.Instance가 null입니다!");
                return;
            }

            int currentObsId = SensorMonitorViewModel.Instance.CurrentObsId;

            if (currentObsId <= 0)
            {
                LogWarning("선택된 관측소가 없습니다!");
                return;
            }

            LogInfo($"[btnCCTV] 버튼 클릭 - 관측소 ID: {currentObsId}");

            // ⭐ Panel_Video 활성화
            panelVideo.SetActive(true);

            // ⭐ Panel_Video에 관측소 ID 전달
            var cctvPanelView = panelVideo.GetComponent<CCTVPanelView>();
            if (cctvPanelView != null)
            {
                cctvPanelView.LoadCCTV(currentObsId);
            }
            else
            {
                LogError("CCTVPanelView 컴포넌트를 찾을 수 없습니다!");
            }
        }
    }
}