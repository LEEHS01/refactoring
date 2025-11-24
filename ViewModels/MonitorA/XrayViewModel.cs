using System;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// X-Ray 기능 상태 관리 ViewModel (Singleton)
    /// </summary>
    public class XrayViewModel : MonoBehaviour
    {
        #region Singleton

        public static XrayViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[XrayViewModel] Singleton 등록 완료");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 건물 X-Ray 상태 변경 이벤트 (true = X-Ray 활성화, 외벽 숨김)
        /// </summary>
        [Serializable]
        public class StructureXrayChangedEvent : UnityEvent<bool> { }
        [HideInInspector] public StructureXrayChangedEvent OnStructureXrayChanged = new StructureXrayChangedEvent();

        /// <summary>
        /// 장비 X-Ray 상태 변경 이벤트 (true = X-Ray 활성화, 장비 숨김)
        /// </summary>
        [Serializable]
        public class EquipmentXrayChangedEvent : UnityEvent<bool> { }
        [HideInInspector] public EquipmentXrayChangedEvent OnEquipmentXrayChanged = new EquipmentXrayChangedEvent();

        /// <summary>
        /// 센서 표시 상태 변경 이벤트 (두 X-Ray 모두 활성화되어야 true)
        /// </summary>
        [Serializable]
        public class SensorsVisibilityChangedEvent : UnityEvent<bool> { }
        [HideInInspector] public SensorsVisibilityChangedEvent OnSensorsVisibilityChanged = new SensorsVisibilityChangedEvent();

        #endregion

        #region Private Fields

        private bool _isStructureXrayActive = false;
        private bool _isEquipmentXrayActive = false;

        #endregion

        #region Properties

        public bool IsStructureXrayActive
        {
            get => _isStructureXrayActive;
            private set
            {
                if (_isStructureXrayActive != value)
                {
                    _isStructureXrayActive = value;
                    OnStructureXrayChanged?.Invoke(value);
                    UpdateSensorsVisibility();
                    Debug.Log($"[XrayViewModel] 건물 X-Ray: {value}");
                }
            }
        }

        public bool IsEquipmentXrayActive
        {
            get => _isEquipmentXrayActive;
            private set
            {
                if (_isEquipmentXrayActive != value)
                {
                    _isEquipmentXrayActive = value;
                    OnEquipmentXrayChanged?.Invoke(value);
                    UpdateSensorsVisibility();
                    Debug.Log($"[XrayViewModel] 장비 X-Ray: {value}");
                }
            }
        }

        /// <summary>
        /// 센서 표시 여부 (두 X-Ray가 모두 활성화되어야 함)
        /// </summary>
        public bool ShouldShowSensors => _isStructureXrayActive && _isEquipmentXrayActive;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Area3DViewModel 이벤트 구독
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.AddListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
                Debug.Log("[XrayViewModel] ✅ Area3DViewModel 구독 완료");
            }
        }

        private void OnDestroy()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.RemoveListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 관측소 로드 시 X-Ray 초기화
        /// </summary>
        private void OnObservatoryLoaded(int obsId, string areaName, string obsName)
        {
            Debug.Log($"[XrayViewModel] 관측소 로드: ObsId={obsId}, X-Ray 초기화");
            ResetXrayState();
        }

        /// <summary>
        /// 관측소 닫기 시 X-Ray 초기화
        /// </summary>
        private void OnObservatoryClosed()
        {
            Debug.Log("[XrayViewModel] 관측소 닫기: X-Ray 초기화");
            ResetXrayState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 건물 X-Ray 토글
        /// </summary>
        public void ToggleStructureXray()
        {
            IsStructureXrayActive = !IsStructureXrayActive;
        }

        /// <summary>
        /// 장비 X-Ray 토글
        /// </summary>
        public void ToggleEquipmentXray()
        {
            IsEquipmentXrayActive = !IsEquipmentXrayActive;
        }

        /// <summary>
        /// X-Ray 상태 완전 초기화
        /// </summary>
        public void ResetXrayState()
        {
            IsStructureXrayActive = false;
            IsEquipmentXrayActive = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 센서 표시 상태 업데이트
        /// </summary>
        private void UpdateSensorsVisibility()
        {
            OnSensorsVisibilityChanged?.Invoke(ShouldShowSensors);
            Debug.Log($"[XrayViewModel] 센서 표시: {ShouldShowSensors}");
        }

        #endregion
    }
}