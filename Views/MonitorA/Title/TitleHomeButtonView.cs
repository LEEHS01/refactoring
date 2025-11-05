using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// HOME 버튼 View (기본 버전)
    /// </summary>
    public class TitleHomeButtonView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button btnHome;
        [SerializeField] private TMP_Text lblText;

        private void Awake()
        {
            InitializeComponents();
            SetupButton();
        }

        private void InitializeComponents()
        {
            if (btnHome == null)
                btnHome = GetComponentInChildren<Button>();

            if (lblText == null)
                lblText = GetComponentInChildren<TMP_Text>();

            if (lblText != null)
                lblText.text = "HOME";
        }

        private void SetupButton()
        {
            if (btnHome != null)
                btnHome.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Debug.Log("🏠 HOME 버튼 클릭");
            // TODO: UiManager.Instance.Invoke(UiEventType.NavigateHome);
        }

        // TODO: 나중에 UiManager 이벤트 구독
        // private void OnNavigateHome(object obj) => gameObject.SetActive(false);
        // private void OnNavigateObs(object obj) => gameObject.SetActive(true);
        // private void OnNavigateArea(object obj) => gameObject.SetActive(true);
    }
}