using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 환경설정 버튼 View (기본 버전)
    /// </summary>
    public class TitleSettingButtonView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button btnSetting;
        [SerializeField] private TMP_Text lblText;

        private void Awake()
        {
            InitializeComponents();
            SetupButton();
        }

        private void InitializeComponents()
        {
            if (btnSetting == null)
                btnSetting = GetComponentInChildren<Button>();

            if (lblText == null)
                lblText = GetComponentInChildren<TMP_Text>();

            if (lblText != null)
                lblText.text = "환경설정";
        }

        private void SetupButton()
        {
            if (btnSetting != null)
                btnSetting.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Debug.Log("⚙️ 환경설정 버튼 클릭");
            // TODO: UiManager.Instance.Invoke(UiEventType.PopupSetting, 0);
        }
    }
}
