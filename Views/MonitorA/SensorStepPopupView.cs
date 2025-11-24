using UnityEngine;
using Core;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 센서 단계별 팝업 표시 View
    /// PopupStepPanel 하위에 Popup_Info_Step_01, Popup_Info_Step_02, Popup_Info_Step_03 등이 있어야 함
    /// </summary>
    public class SensorStepPopupView : BaseView
    {
        private bool _isSubscribed = false;

        #region BaseView 구현

        protected override void InitializeUIComponents()
        {
            // 자식 팝업들이 올바른 이름으로 있는지 확인
            int popupCount = 0;
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Popup_Info_Step_"))
                {
                    popupCount++;
                    // 초기에는 모두 숨김
                    child.gameObject.SetActive(false);
                }
            }

            LogInfo($"센서 단계 팝업 초기화 완료: {popupCount}개 발견");
        }

        protected override void SetupViewEvents()
        {
            LogInfo("View 이벤트 설정 완료 (없음)");
        }

        protected override void ConnectToViewModel()
        {
            SubscribeToViewModel();
        }

        protected override void DisconnectViewEvents()
        {
            // 별도 View 이벤트 없음
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

            if (SensorStepViewModel.Instance == null)
            {
                LogError("SensorStepViewModel.Instance가 null입니다!");
                return;
            }

            SensorStepViewModel.Instance.OnStepChanged.AddListener(OnStepChanged);

            _isSubscribed = true;
            LogInfo("✅ SensorStepViewModel 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (SensorStepViewModel.Instance != null)
            {
                SensorStepViewModel.Instance.OnStepChanged.RemoveListener(OnStepChanged);
            }

            _isSubscribed = false;
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        /// <summary>
        /// 단계 변경 시 해당 팝업만 활성화
        /// </summary>
        private void OnStepChanged(int step)
        {
            UpdatePopupVisibility(step);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 해당 단계 팝업만 활성화, 나머지 비활성화
        /// </summary>
        private void UpdatePopupVisibility(int step)
        {
            string targetPopupName = step > 0 ? $"Popup_Info_Step_0{step}" : "";

            foreach (Transform child in transform)
            {
                bool shouldShow = child.name == targetPopupName;
                child.gameObject.SetActive(shouldShow);
            }

            LogInfo($"팝업 표시: Step {step}");
        }

        #endregion
    }
}