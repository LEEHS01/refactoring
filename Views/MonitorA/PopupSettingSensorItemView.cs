using HNS.Common.Models;
using HNS.Common.ViewModels;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.Common.Views
{
    /// <summary>
    /// 센서 설정 아이템 View (화학물질/수질용)
    /// 독성도는 별도로 처리 (아이템 없음)
    /// </summary>
    public class PopupSettingSensorItemView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Toggle tglVisibility;        // Check_Box
        [SerializeField] private TMP_Text lblSensorName;      // Background/Label
        [SerializeField] private TMP_InputField hiField;      // ThresholdContainer/hiField (경계값)
        [SerializeField] private TMP_InputField hihiField;    // ThresholdContainer/hihiField (경보값)

        private int _obsId;
        private int _boardId;
        private int _sensorId;
        private bool _isValid = false;

        public string SensorName => lblSensorName?.text ?? "";
        public bool IsValid => _isValid;

        #region Unity Lifecycle

        private void Awake()
        {
            // Inspector에서 연결 안 된 경우 자동 탐색
            if (tglVisibility == null)
                tglVisibility = transform.Find("Check_Box")?.GetComponent<Toggle>();

            if (lblSensorName == null)
            {
                Transform background = transform.Find("Background");
                if (background != null)
                    lblSensorName = background.Find("Label")?.GetComponent<TMP_Text>();
            }

            if (hiField == null || hihiField == null)
            {
                Transform background = transform.Find("Background");
                if (background != null)
                {
                    Transform container = background.Find("ThresholdContainer");
                    if (container != null)
                    {
                        if (hiField == null)
                            hiField = container.Find("hiField")?.GetComponent<TMP_InputField>();
                        if (hihiField == null)
                            hihiField = container.Find("hihiField")?.GetComponent<TMP_InputField>();
                    }
                }
            }
        }

        private void Start()
        {
            SetupUIEvents();
        }

        #endregion

        #region Initialization

        private void SetupUIEvents()
        {
            if (tglVisibility != null)
                tglVisibility.onValueChanged.AddListener(OnVisibilityToggled);

            // 경계값 (ALAHIVAL)
            if (hiField != null)
                hiField.onEndEdit.AddListener(OnHiValueChanged);

            // 경보값 (ALAHIHIVAL)
            if (hihiField != null)
                hihiField.onEndEdit.AddListener(OnHiHiValueChanged);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 센서 정보 설정 (원본 SetItem과 동일)
        /// </summary>
        public void SetItem(int obsId, int boardId, int hnsId, string sensorName,
            bool isVisible, float hiValue, float hihiValue)
        {
            _obsId = obsId;
            _boardId = boardId;
            _sensorId = hnsId;
            _isValid = hnsId > 0; // 더미 데이터(-1) 제외

            // ⭐ 로그: 받은 값 확인
            Debug.Log($"[SetItem] {sensorName} 설정 중...");
            Debug.Log($"  - ObsId={obsId}, BoardId={boardId}, HnsId={hnsId}");
            Debug.Log($"  - IsVisible={isVisible}");
            Debug.Log($"  - hiValue(경계)={hiValue}");
            Debug.Log($"  - hihiValue(경보)={hihiValue}");

            if (lblSensorName != null)
                lblSensorName.text = sensorName;

            if (tglVisibility != null)
                tglVisibility.SetIsOnWithoutNotify(isVisible);

            // ⭐ 경계값 (ALAHIVAL)
            if (hiField != null)
            {
                hiField.SetTextWithoutNotify(hiValue.ToString("F1"));
                Debug.Log($"  ✅ hiField 설정 완료: {hiValue:F1}");
            }
            else
            {
                Debug.LogError($"  ❌ hiField가 null입니다! GameObject={gameObject.name}");
            }

            // ⭐ 경보값 (ALAHIHIVAL)
            if (hihiField != null)
            {
                hihiField.SetTextWithoutNotify(hihiValue.ToString("F1"));
                Debug.Log($"  ✅ hihiField 설정 완료: {hihiValue:F1}");
            }
            else
            {
                Debug.LogError($"  ❌ hihiField가 null입니다! GameObject={gameObject.name}");
            }
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// 센서 표시/숨김 토글
        /// </summary>
        private void OnVisibilityToggled(bool isVisible)
        {
            if (!_isValid) return;

            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.ToggleSensorUsing(
                    _boardId, _sensorId, isVisible);
            }
        }

        /// <summary>
        /// 경계값(ALAHIVAL) 변경
        /// </summary>
        private void OnHiValueChanged(string value)
        {
            if (!_isValid) return;

            if (float.TryParse(value, out float newHi))
            {
                if (PopupSettingViewModel.Instance != null)
                {
                    StartCoroutine(PopupSettingViewModel.Instance.UpdateSensorThreshold(
                        _obsId, _boardId, _sensorId, "ALAHIVAL", newHi));
                }
            }
        }

        /// <summary>
        /// 경보값(ALAHIHIVAL) 변경
        /// </summary>
        private void OnHiHiValueChanged(string value)
        {
            if (!_isValid) return;

            if (float.TryParse(value, out float newHiHi))
            {
                if (PopupSettingViewModel.Instance != null)
                {
                    StartCoroutine(PopupSettingViewModel.Instance.UpdateSensorThreshold(
                        _obsId, _boardId, _sensorId, "ALAHIHIVAL", newHiHi));
                }
            }
        }

        #endregion
    }
}
