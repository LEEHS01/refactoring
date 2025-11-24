using HNS.MonitorA.ViewModels;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HNS.MonitorA.Components
{
    /// <summary>
    /// 센서 단계별 깜박임 애니메이션 관리
    /// 건물 X-Ray와 장비 X-Ray 둘 다 활성화되어야 동작
    /// </summary>
    public class ObsSensorStepAnimator : MonoBehaviour
    {
        Dictionary<int, List<string>> stepSensorsDic;
        Coroutine coroutine = null;
        Transform stepIndicatorParent;

        private int _currentStep = -1;
        private bool _isBuildingXRayActive = false;
        private bool _isEquipmentXRayActive = false;
        private bool _isSubscribed = false;

        private void Awake()
        {
            stepSensorsDic = new()
            {
                { 1, new(){
                    "Sensor_A", "Sensor_B", "Sensor_C", "Sensor_D", "Sensor_E",
                    "Sensor_F", "Sensor_G", "Sensor_H", "Sensor_I", "Sensor_J", "Sensor_K",
                } },
                { 2, new(){
                    "Sensor_I", "Sensor_C", "Sensor_J", "Sensor_E", "Sensor_F",
                } },
                { 3, new(){
                    "Sensor_J", "Sensor_D", "Sensor_K",
                } },
                { 4, new(){
                    "Sensor_A", "Sensor_B", "Sensor_D", "Sensor_K",
                } },
                { 5, new() },
            };
        }

        private void Start()
        {
            stepIndicatorParent = transform.Find("Canvas");
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            ResetStateInternal();
            Debug.Log("🔄 ObsSensorStepAnimator - OnDisable로 자동 초기화");
        }

        private void OnEnable()
        {
            if (_isSubscribed == false)
            {
                SubscribeToViewModel();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
            StopAnimation();
        }

        #region ViewModel 구독
        private void SubscribeToViewModel()
        {
            if (_isSubscribed) return;

            // SensorStepViewModel 구독
            if (SensorStepViewModel.Instance != null)
            {
                SensorStepViewModel.Instance.OnStepChanged.AddListener(OnChangeSensorStep);
                Debug.Log("✅ ObsSensorStepAnimator - SensorStepViewModel 구독 완료");
            }
            else
            {
                Debug.LogError("SensorStepViewModel.Instance가 null입니다!");
            }

            // ⭐⭐⭐ XrayViewModel 구독 추가!
            if (XrayViewModel.Instance != null)
            {
                XrayViewModel.Instance.OnStructureXrayChanged.AddListener(OnStructureXrayChanged);
                XrayViewModel.Instance.OnEquipmentXrayChanged.AddListener(OnEquipmentXrayChanged);
                Debug.Log("✅ ObsSensorStepAnimator - XrayViewModel 구독 완료");
            }
            else
            {
                Debug.LogError("XrayViewModel.Instance가 null입니다!");
            }

            _isSubscribed = true;
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (SensorStepViewModel.Instance != null)
            {
                SensorStepViewModel.Instance.OnStepChanged.RemoveListener(OnChangeSensorStep);
            }

            // ⭐⭐⭐ XrayViewModel 구독 해제
            if (XrayViewModel.Instance != null)
            {
                XrayViewModel.Instance.OnStructureXrayChanged.RemoveListener(OnStructureXrayChanged);
                XrayViewModel.Instance.OnEquipmentXrayChanged.RemoveListener(OnEquipmentXrayChanged);
            }

            _isSubscribed = false;
        }
        #endregion

        #region 이벤트 핸들러
        private void OnChangeSensorStep(int step)
        {
            _currentStep = step;
            SetStepIndicator(step);
            ResetAllOutlines();

            if (IsBothXRayActive() && step > 0)
            {
                StartAnimation(step);
                Debug.Log($"🔔 알람 발생 - Step {step} 애니메이션 시작");
            }
            else
            {
                Debug.Log($"🔔 알람 발생 - Step {step}, X-Ray 미활성화로 애니메이션 시작 안 함");
            }

            NotifyPopupViews();
        }

        // ⭐⭐⭐ XrayViewModel 이벤트 핸들러 추가
        private void OnStructureXrayChanged(bool isActive)
        {
            _isBuildingXRayActive = isActive;
            UpdateXRayState();
            Debug.Log($"🏢 건물 X-Ray 변경: {isActive}");
        }

        private void OnEquipmentXrayChanged(bool isActive)
        {
            _isEquipmentXRayActive = isActive;
            UpdateXRayState();
            Debug.Log($"⚙️ 장비 X-Ray 변경: {isActive}");
        }
        #endregion

        #region Public Methods
        public bool IsBothXRayActive()
        {
            return _isBuildingXRayActive && _isEquipmentXRayActive;
        }

        public bool IsBuildingXRayActive()
        {
            return _isBuildingXRayActive;
        }

        public bool IsEquipmentXRayActive()
        {
            return _isEquipmentXRayActive;
        }

        public bool IsBuildingXRayOnlyActive()
        {
            return _isBuildingXRayActive && !_isEquipmentXRayActive;
        }

        public int GetCurrentStep()
        {
            return _currentStep;
        }

        public void ResetState()
        {
            ResetStateInternal();
            Debug.Log("🔄 ObsSensorStepAnimator - 명시적 초기화 호출");
        }
        #endregion

        #region Private Methods
        private void ResetStateInternal()
        {
            _currentStep = -1;
            _isBuildingXRayActive = false;
            _isEquipmentXRayActive = false;
            SetStepIndicator(-1);
            ResetAllOutlines();
            StopAnimation();

            NotifyPopupViews();
        }

        private void UpdateXRayState()
        {
            if (IsBothXRayActive() && _currentStep > 0)
            {
                StartAnimation(_currentStep);
                Debug.Log($"✅ X-Ray 둘 다 활성화 - Step {_currentStep} 애니메이션 시작");
            }
            else
            {
                StopAnimation();
                Debug.Log("❌ X-Ray 비활성화 - 애니메이션 중지");
            }

            NotifyPopupViews();
        }

        private void StartAnimation(int step)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            coroutine = StartCoroutine(AnimateOutlineWidth(step));
        }

        private void StopAnimation()
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
            ResetAllOutlines();
        }

        void ResetAllOutlines()
        {
            IEnumerable<Outline> allOutlines = transform.GetComponentsInChildren<Outline>();
            foreach (var outline in allOutlines)
            {
                outline.OutlineWidth = 0;
                outline.enabled = false;
                outline.enabled = true;
            }
        }

        IEnumerator AnimateOutlineWidth(int stage)
        {
            float duration = 1f;
            float elapsedTime = 0f;
            float minOutlineWidth = 0f;
            float maxOutlineWidth = 20f;

            List<string> stepSensorNames = stepSensorsDic[stage];
            IEnumerable<Outline> allOutlines = transform.GetComponentsInChildren<Outline>();
            IEnumerable<Outline> stepOutlines = allOutlines.Where(outline => stepSensorNames.Contains(outline.name));

            while (true)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.PingPong(elapsedTime / duration, 1f);
                float outlineWidth = Mathf.Lerp(minOutlineWidth, maxOutlineWidth, t);

                foreach (Outline outline in stepOutlines)
                {
                    outline.OutlineWidth = outlineWidth;
                    outline.enabled = true;
                    outline.OutlineMode = Outline.Mode.OutlineAll;
                }

                yield return null;
            }
        }

        void SetStepIndicator(int step)
        {
            if (stepIndicatorParent == null) return;

            foreach (Transform child in stepIndicatorParent)
                child.gameObject.SetActive(child.name == ("Step_0" + step));
        }

        private void NotifyPopupViews()
        {
            // 센서 단계 팝업
            var sensorStepPopup = FindObjectOfType<HNS.MonitorA.Views.SensorStepPopupView>();
            if (sensorStepPopup != null)
            {
                sensorStepPopup.OnXRayStateChanged();
            }

            // 장비 정보 팝업
            var machineInfoPopup = FindObjectOfType<HNS.MonitorA.Views.MachineInfoPopupView>();
            if (machineInfoPopup != null)
            {
                machineInfoPopup.OnXRayStateChanged();
            }
        }
        #endregion
    }
}
