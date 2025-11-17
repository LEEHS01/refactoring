using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// X-Ray 버튼 View (기본 버전)
    /// </summary>
    public class TitleXrayButtonView : MonoBehaviour
    {
        public enum XrayType
        {
            Equipment,  // 장비
            Structure   // 건물 
        }

        [Header("Settings")]
        [SerializeField] private XrayType xrayType;

        [Header("UI References")]
        [SerializeField] private Button btnXray;
        [SerializeField] private TMP_Text lblText;

        private void Awake()
        {
            InitializeComponents();
            SetupButton();
            UpdateUI();
        }

        private void InitializeComponents()
        {
            if (btnXray == null)
                btnXray = GetComponentInChildren<Button>();

            if (lblText == null)
                lblText = GetComponentInChildren<TMP_Text>();
        }

        private void SetupButton()
        {
            if (btnXray != null)
                btnXray.onClick.AddListener(OnClick);
        }

        private void UpdateUI()
        {
            if (lblText != null)
                lblText.text = xrayType == XrayType.Structure ? "건물 X-Ray" : "장비 X-Ray";
        }

        private void OnClick()
        {
            Debug.Log($" X-Ray 버튼 클릭: {xrayType}");
            // TODO: X-Ray 토글 로직 구현
            // - GetXrayTarget()로 대상 오브젝트 찾기
            // - SetActive() 토글
            // - 센서 표시 제어
        }

        // TODO: 나중에 구현
        // private GameObject GetXrayTarget(XrayType type) { }
        // private void ControlSensorVisibility() { }
        // private void OnNavigateObs(object obj) { }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InitializeComponents();
            UpdateUI();
        }
#endif
    }
}