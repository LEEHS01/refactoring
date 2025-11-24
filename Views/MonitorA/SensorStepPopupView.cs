using UnityEngine;
using Core;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 센서 단계별 팝업 표시 View
    /// X-Ray 버튼 둘 다 활성화된 상태에서만 팝업 표시
    /// </summary>
    public class SensorStepPopupView : BaseView
    {
        private bool _isSubscribed = false;
        private int _currentStep = -1; // 현재 활성화된 단계 저장

        #region BaseView 구현
        protected override void InitializeUIComponents()
        {
            int popupCount = 0;
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Popup_Info_Step_"))
                {
                    popupCount++;
                    child.gameObject.SetActive(false);
                }
            }
            LogInfo($"센서 단계 팝업 초기화 완료: {popupCount}개 발견");
        }

        protected override void SetupViewEvents()
        {
            LogInfo("View 이벤트 설정 완료");
        }

        protected override void ConnectToViewModel()
        {
            SubscribeToViewModel();
        }

        protected override void DisconnectViewEvents()
        {
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
        /// 단계 변경 시 해당 팝업 자동 표시
        /// 단, X-Ray 둘 다 활성화된 경우에만
        /// </summary>
        private void OnStepChanged(int step)
        {
            _currentStep = step;

            // X-Ray 둘 다 활성화된 경우에만 팝업 자동 표시
            if (IsBothXRayActive())
            {
                UpdatePopupVisibility(step);
                LogInfo($"🔔 알람 발생 - 단계 변경: Step {step}, 팝업 자동 표시");
            }
            else
            {
                LogInfo($"🔔 알람 발생 - 단계 변경: Step {step}, X-Ray 미활성화로 팝업 표시 안 함");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 현재 단계 팝업 표시 (외부에서 호출 - Sensors 클릭 시)
        /// X-Ray 둘 다 활성화 상태에서만 동작
        /// </summary>
        public void ShowCurrentStepPopup()
        {
            // X-Ray 둘 다 활성화 확인
            if (!IsBothXRayActive())
            {
                LogInfo("❌ X-Ray 버튼이 둘 다 활성화되지 않아 팝업을 표시할 수 없습니다.");
                return;
            }

            if (_currentStep > 0)
            {
                UpdatePopupVisibility(_currentStep);
                LogInfo($"👆 Sensors 클릭 - 현재 단계 팝업 표시: Step {_currentStep}");
            }
            else
            {
                LogInfo("현재 활성화된 단계가 없습니다.");
            }
        }

        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void HideAllPopups()
        {
            UpdatePopupVisibility(-1);
            LogInfo("팝업 닫기");
        }

        /// <summary>
        /// X-Ray 상태 변경 시 호출
        /// X-Ray가 하나라도 비활성화되면 팝업 닫기
        /// </summary>
        public void OnXRayStateChanged()
        {
            if (!IsBothXRayActive())
            {
                // X-Ray 하나라도 꺼지면 팝업 닫기
                HideAllPopups();
                LogInfo("❌ X-Ray 비활성화로 팝업 닫기");
            }
        }

        /// <summary>
        /// 현재 활성화된 단계 반환
        /// </summary>
        public int GetCurrentStep()
        {
            return _currentStep;
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
        }

        /// <summary>
        /// X-Ray 버튼 둘 다 활성화되었는지 확인
        /// </summary>
        private bool IsBothXRayActive()
        {
            // ⭐ XrayViewModel에서 직접 확인!
            if (XrayViewModel.Instance != null)
            {
                return XrayViewModel.Instance.IsStructureXrayActive &&
                       XrayViewModel.Instance.IsEquipmentXrayActive;
            }
            return false;
        }
        #endregion
    }
}