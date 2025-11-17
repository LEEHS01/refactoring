using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// X-Ray 버튼 View
    /// 3D 관측소 화면에서만 표시
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
        [SerializeField] private CanvasGroup canvasGroup; // ⭐ 추가

        private void Awake()
        {
            InitializeComponents();
            SetupButton();
            UpdateUI();
        }

        private void Start()
        {
            // ViewModel 이벤트 구독
            SubscribeToViewModel();

            // ⭐ 초기 상태: 숨김 (GameObject는 활성화 유지!)
            HideButton();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();

            if (btnXray != null)
                btnXray.onClick.RemoveListener(OnClick);
        }

        private void InitializeComponents()
        {
            if (btnXray == null)
                btnXray = GetComponentInChildren<Button>();

            if (lblText == null)
                lblText = GetComponentInChildren<TMP_Text>();

            // ⭐ CanvasGroup 자동 추가
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    Debug.Log($"[TitleXrayButtonView] {xrayType} - CanvasGroup 자동 추가");
                }
            }
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

        #region ViewModel 이벤트 구독

        private void SubscribeToViewModel()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.AddListener(OnObservatoryEntered);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
                Debug.Log($"[TitleXrayButtonView] {xrayType} - Area3DViewModel 이벤트 구독");
            }
            else
            {
                Debug.LogWarning($"[TitleXrayButtonView] {xrayType} - Area3DViewModel.Instance가 null!");
            }
        }

        private void UnsubscribeFromViewModel()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.RemoveListener(OnObservatoryEntered);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }
        }

        #endregion

        #region 이벤트 핸들러

        private void OnObservatoryEntered(int obsId)
        {
            ShowButton();
            Debug.Log($"[TitleXrayButtonView] {xrayType} - 관측소 진입 (ObsId={obsId}), 버튼 표시");
        }

        private void OnObservatoryClosed()
        {
            HideButton();
            Debug.Log($"[TitleXrayButtonView] {xrayType} - 관측소 닫기, 버튼 숨김");
        }

        #endregion

        #region 버튼 표시/숨김

        private void ShowButton()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void HideButton()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        #endregion

        private void OnClick()
        {
            Debug.Log($"[TitleXrayButtonView] X-Ray 버튼 클릭: {xrayType}");
            // TODO: X-Ray 토글 로직 구현
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InitializeComponents();
            UpdateUI();
        }
#endif
    }
}