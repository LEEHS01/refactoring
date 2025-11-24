using Core;
using HNS.MonitorA.ViewModels;
using HNS.MonitorA.Views;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.MonitorA.Views
{
    public class MachineInfoPopupView : BaseView
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;  // ⭐ 자식 Panel을 연결!
        [SerializeField] private Button closeButton;

        private bool _isSubscribed = false;

        #region BaseView 구현
        protected override void InitializeUIComponents()
        {
            // ⭐ popupPanel이 null이면 첫 번째 자식 찾기
            if (popupPanel == null)
            {
                if (transform.childCount > 0)
                {
                    popupPanel = transform.GetChild(0).gameObject;
                    LogInfo($"popupPanel 자동 할당: {popupPanel.name}");
                }
            }

            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            LogInfo("장비 정보 팝업 초기화 완료");
        }

        protected override void SetupViewEvents()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        protected override void ConnectToViewModel()
        {
            SubscribeToViewModel();
        }

        protected override void DisconnectViewEvents()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        protected override void DisconnectFromViewModel()
        {
            UnsubscribeFromViewModel();
        }
        #endregion

        #region ViewModel 구독
        private void SubscribeToViewModel()
        {
            if (_isSubscribed) return;

            if (MachineInfoViewModel.Instance == null)
            {
                LogError("MachineInfoViewModel.Instance가 null입니다!");
                return;
            }

            MachineInfoViewModel.Instance.OnMachineInfoOpened.AddListener(OnMachineInfoOpened);
            MachineInfoViewModel.Instance.OnMachineInfoClosed.AddListener(OnMachineInfoClosed);

            _isSubscribed = true;
            LogInfo("✅ MachineInfoViewModel 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (MachineInfoViewModel.Instance != null)
            {
                MachineInfoViewModel.Instance.OnMachineInfoOpened.RemoveListener(OnMachineInfoOpened);
                MachineInfoViewModel.Instance.OnMachineInfoClosed.RemoveListener(OnMachineInfoClosed);
            }

            _isSubscribed = false;
        }
        #endregion

        #region 이벤트 핸들러
        private void OnMachineInfoOpened(string stcd)
        {
            Debug.Log($"[MachineInfoPopupView] OnMachineInfoOpened 호출됨: STCD={stcd}");

            if (XrayViewModel.Instance != null)
            {
                bool buildingOn = XrayViewModel.Instance.IsStructureXrayActive;
                bool equipmentOn = XrayViewModel.Instance.IsEquipmentXrayActive;

                Debug.Log($"[MachineInfoPopupView] X-Ray 상태: 건물={buildingOn}, 장비={equipmentOn}");

                if (buildingOn && !equipmentOn)
                {
                    ShowPopup(stcd);
                    return;
                }
            }

            LogInfo("❌ 건물 X-Ray만 활성화된 상태가 아니므로 팝업 표시 안 함");
        }

        private void OnMachineInfoClosed()
        {
            HidePopup();
        }

        private void OnCloseButtonClicked()
        {
            MachineInfoViewModel.Instance?.CloseMachineInfo();
        }

        public void OnXRayStateChanged()
        {
            if (XrayViewModel.Instance != null)
            {
                bool buildingOn = XrayViewModel.Instance.IsStructureXrayActive;
                bool equipmentOn = XrayViewModel.Instance.IsEquipmentXrayActive;

                if (buildingOn && equipmentOn)
                {
                    HidePopup();
                    LogInfo("❌ 둘 다 활성화 → 장비 팝업 닫기");
                }
                else if (!buildingOn)
                {
                    HidePopup();
                    LogInfo("❌ 건물 X-Ray 비활성화 → 장비 팝업 닫기");
                }
            }
        }
        #endregion

        #region Public Methods
        public void ShowPopup(string stcd)
        {
            Debug.Log($"[MachineInfoPopupView] ShowPopup 호출: STCD={stcd}");

            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                string description = MachineInfoViewModel.Instance?.GetStcdDescription(stcd) ?? "알 수 없음";
                LogInfo($"장비 정보 팝업 표시: STCD={stcd} ({description})");
            }
            else
            {
                LogError("popupPanel이 null입니다!");
            }
        }

        public void HidePopup()
        {
            Debug.Log($"[MachineInfoPopupView] HidePopup 호출");

            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
                LogInfo("장비 정보 팝업 닫기");
            }
        }
        #endregion
    }
}
