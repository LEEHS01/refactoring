using System;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 센서 단계별 정보 관리 ViewModel (Singleton)
    /// X-Ray 장비 활성화 시 센서 단계 정보 팝업 표시
    /// </summary>
    public class SensorStepViewModel : MonoBehaviour
    {
        #region Singleton

        public static SensorStepViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[SensorStepViewModel] Singleton 등록 완료");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 센서 단계 변경 이벤트 (step: 1, 2, 3...)
        /// </summary>
        [Serializable]
        public class StepChangedEvent : UnityEvent<int> { }
        [HideInInspector] public StepChangedEvent OnStepChanged = new StepChangedEvent();

        #endregion

        #region Private Fields

        private int _currentStep = -1;  // -1 = 팝업 숨김

        #endregion

        #region Properties

        public int CurrentStep
        {
            get => _currentStep;
            private set
            {
                if (_currentStep != value)
                {
                    int previousStep = _currentStep;
                    _currentStep = value;
                    OnStepChanged?.Invoke(value);
                    Debug.Log($"[SensorStepViewModel] 센서 단계 변경: {previousStep} → {value}");
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Area3DViewModel 이벤트 구독
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.AddListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
                Debug.Log("[SensorStepViewModel] ✅ Area3DViewModel 구독 완료");
            }

            // XrayViewModel 이벤트 구독 (센서 표시 여부에 따라 팝업 표시)
            if (XrayViewModel.Instance != null)
            {
                XrayViewModel.Instance.OnSensorsVisibilityChanged.AddListener(OnSensorsVisibilityChanged);
                Debug.Log("[SensorStepViewModel] ✅ XrayViewModel 구독 완료");
            }
        }

        private void OnDestroy()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.RemoveListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }

            if (XrayViewModel.Instance != null)
            {
                XrayViewModel.Instance.OnSensorsVisibilityChanged.RemoveListener(OnSensorsVisibilityChanged);
            }
        }

        #endregion

        #region Event Handlers

        private void OnObservatoryLoaded(int obsId, string areaName, string obsName)
        {
            Debug.Log($"[SensorStepViewModel] 관측소 로드: 단계 초기화");
            CurrentStep = -1;  // 팝업 숨김
        }

        private void OnObservatoryClosed()
        {
            Debug.Log("[SensorStepViewModel] 관측소 닫기: 단계 초기화");
            CurrentStep = -1;  // 팝업 숨김
        }

        /// <summary>
        /// 센서 표시 여부 변경 시 (두 X-Ray 모두 활성화되면 단계 1 표시)
        /// </summary>
        private void OnSensorsVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                // 센서가 보이면 단계 1 팝업 표시
                CurrentStep = 1;
                Debug.Log("[SensorStepViewModel] 센서 표시 → 단계 1 팝업 표시");
            }
            else
            {
                // 센서가 숨겨지면 팝업 숨김
                CurrentStep = -1;
                Debug.Log("[SensorStepViewModel] 센서 숨김 → 팝업 숨김");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 센서 단계 설정 (외부에서 호출 가능)
        /// </summary>
        public void SetStep(int step)
        {
            CurrentStep = step;
        }

        /// <summary>
        /// 다음 단계로 이동
        /// </summary>
        public void NextStep()
        {
            if (_currentStep > 0)
            {
                CurrentStep = _currentStep + 1;
            }
        }

        /// <summary>
        /// 이전 단계로 이동
        /// </summary>
        public void PreviousStep()
        {
            if (_currentStep > 1)
            {
                CurrentStep = _currentStep - 1;
            }
        }

        #endregion
    }
}