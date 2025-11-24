using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using HNS.MonitorA.ViewModels;
using Repositories.MonitorA;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 센서 단계별 정보 관리 ViewModel (Singleton)
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
        [Serializable]
        public class StepChangedEvent : UnityEvent<int> { }
        [HideInInspector] public StepChangedEvent OnStepChanged = new StepChangedEvent();
        #endregion

        #region Private Fields
        private int _currentStep = -1;
        private int _currentObsId = -1;  // 현재 관측소 ID 저장
        private ObsMonitoringRepository _repository = new ObsMonitoringRepository();
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

            // XrayViewModel 이벤트 구독
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
            Debug.Log($"[SensorStepViewModel] 관측소 로드: ObsId={obsId}");
            _currentObsId = obsId;
            CurrentStep = -1;  // 초기화

            // ✅ DB에서 실제 단계 가져오기
            LoadSensorStepFromDB();
        }

        private void OnObservatoryClosed()
        {
            Debug.Log("[SensorStepViewModel] 관측소 닫기: 단계 초기화");
            _currentObsId = -1;
            CurrentStep = -1;
        }

        /// <summary>
        /// 센서 표시 여부 변경 시
        /// </summary>
        private void OnSensorsVisibilityChanged(bool isVisible)
        {
            if (isVisible && _currentObsId > 0)
            {
                // ✅ DB에서 실제 단계 가져오기
                Debug.Log("[SensorStepViewModel] 센서 표시 → DB에서 단계 로드");
                LoadSensorStepFromDB();
            }
            else
            {
                // 센서가 숨겨지면 팝업 숨김
                CurrentStep = -1;
                Debug.Log("[SensorStepViewModel] 센서 숨김 → 팝업 숨김");
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// DB에서 센서 단계 로드 (GET_SENSOR_STEP)
        /// </summary>
        private void LoadSensorStepFromDB()
        {
            if (_currentObsId <= 0)
            {
                Debug.LogWarning("[SensorStepViewModel] 관측소 ID가 유효하지 않음");
                return;
            }

            StartCoroutine(_repository.GetSensorStep(
                _currentObsId,
                (step) =>
                {
                    CurrentStep = step;
                    Debug.Log($"[SensorStepViewModel] ✅ DB에서 단계 로드 완료: Step {step}");
                },
                (error) =>
                {
                    Debug.LogError($"[SensorStepViewModel] ❌ 단계 로드 실패: {error}");
                    // 실패 시 기본값 1 설정
                    CurrentStep = 1;
                }
            ));
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
        /// 센서 단계 강제 새로고침 (DB에서 다시 로드)
        /// </summary>
        public void RefreshStep()
        {
            if (_currentObsId > 0)
            {
                LoadSensorStepFromDB();
            }
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