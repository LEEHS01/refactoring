using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 기계 정보 팝업 View
    /// </summary>
    public class MachineInfoPopupView : BaseView
    {
        [Header("UI Components")]
        [SerializeField] private Button btnClose;
        [SerializeField] private TMP_Text txtStatus;
        [SerializeField] private CanvasGroup canvasGroup; // ⭐⭐⭐ 추가

        private Vector3 defaultPos;
        private bool _isSubscribed = false;

        #region BaseView 구현

        protected override void InitializeUIComponents()
        {
            bool isValid = ValidateComponents(
                (btnClose, "btnClose"),
                (txtStatus, "txtStatus")
            );

            if (!isValid)
            {
                LogError("필수 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            // ⭐⭐⭐ CanvasGroup 자동 추가
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    LogInfo("CanvasGroup 자동 추가");
                }
            }

            defaultPos = transform.position;

            // ⭐⭐⭐ GameObject는 활성화 유지, CanvasGroup으로 숨김
            HidePopup();

            LogInfo("기계 정보 팝업 초기화 완료");
        }

        protected override void SetupViewEvents()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(OnClickClose);
            }
        }

        protected override void ConnectToViewModel()
        {
            SubscribeToViewModel();
        }

        protected override void DisconnectViewEvents()
        {
            if (btnClose != null)
            {
                btnClose.onClick.RemoveListener(OnClickClose);
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
            if (_isSubscribed)
            {
                LogInfo("이미 구독되어 있음");
                return;
            }

            if (MachineInfoViewModel.Instance == null)
            {
                LogError("MachineInfoViewModel.Instance가 null입니다!");
                return;
            }

            MachineInfoViewModel.Instance.OnMachineInfoOpened.AddListener(OnMachineInfoOpened);
            MachineInfoViewModel.Instance.OnMachineInfoClosed.AddListener(OnMachineInfoClosed);

            _isSubscribed = true;
            LogInfo("MachineInfoViewModel 구독 완료");
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

        #region ViewModel 이벤트 핸들러

        private void OnMachineInfoOpened(string stcd)
        {
            LogInfo($"========================================");
            LogInfo($"이벤트 수신: OnMachineInfoOpened, STCD={stcd}");

            transform.position = defaultPos;
            transform.SetAsLastSibling();

            ShowPopup();
            UpdateStatusText(stcd);

            LogInfo($"팝업 표시 완료");
            LogInfo($"========================================");
        }

        private void OnMachineInfoClosed()
        {
            LogInfo("이벤트 수신: OnMachineInfoClosed");
            HidePopup();
            LogInfo("팝업 숨김 완료");
        }

        #endregion

        #region UI 이벤트 핸들러

        private void OnClickClose()
        {
            LogInfo("닫기 버튼 클릭");

            if (MachineInfoViewModel.Instance != null)
            {
                MachineInfoViewModel.Instance.CloseMachineInfo();
            }
        }

        #endregion

        #region Private Methods - CanvasGroup 제어

        /// <summary>
        /// ⭐⭐⭐ 팝업 표시 (CanvasGroup 사용)
        /// </summary>
        private void ShowPopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                LogInfo("CanvasGroup으로 팝업 표시");
            }
        }

        /// <summary>
        /// 팝업 숨김 (CanvasGroup 사용)
        /// </summary>
        private void HidePopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                LogInfo("CanvasGroup으로 팝업 숨김");
            }
        }

        private void UpdateStatusText(string stcd)
        {
            if (txtStatus != null && MachineInfoViewModel.Instance != null)
            {
                string statusText = MachineInfoViewModel.Instance.GetStcdDescription(stcd);
                txtStatus.text = statusText;
                LogInfo($"상태 텍스트 업데이트: {statusText}");
            }
        }

        #endregion
    }
}