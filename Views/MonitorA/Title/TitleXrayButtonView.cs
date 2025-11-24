using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// X-Ray 버튼 UI View
    /// </summary>
    public class TitleXrayButtonView : BaseView
    {
        public enum XrayType
        {
            Equipment,  // 장비
            Structure   // 건물 
        }

        [Header("Settings")]
        [SerializeField] private XrayType xrayType;

        [Header("UI Components")]
        [SerializeField] private Button btnXray;
        [SerializeField] private TMP_Text lblText;
        [SerializeField] private CanvasGroup canvasGroup;

        private bool _isSubscribed = false;
        private bool _isButtonEventConnected = false; // ⭐ 추가

        #region BaseView 구현

        protected override void InitializeUIComponents()
        {
            if (btnXray == null)
            {
                btnXray = GetComponent<Button>();
                if (btnXray == null)
                {
                    btnXray = GetComponentInChildren<Button>();
                }
            }

            if (lblText == null)
            {
                lblText = GetComponentInChildren<TMP_Text>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    LogInfo("CanvasGroup 자동 추가");
                }
            }

            bool isValid = ValidateComponents(
                (btnXray, "btnXray"),
                (lblText, "lblText"),
                (canvasGroup, "canvasGroup")
            );

            if (!isValid)
            {
                LogError("필수 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            lblText.text = xrayType == XrayType.Structure ? "건물 X-Ray" : "장비 X-Ray";
            HideButton();

            LogInfo($"{xrayType} 버튼 초기화 완료");
        }

        protected override void SetupViewEvents()
        {
            // ⭐⭐⭐ 중복 연결 방지
            if (_isButtonEventConnected) return;

            if (btnXray != null)
            {
                // ⭐⭐⭐ 기존 리스너 모두 제거
                btnXray.onClick.RemoveAllListeners();

                // ⭐⭐⭐ 새로 연결
                btnXray.onClick.AddListener(OnButtonClick);

                _isButtonEventConnected = true;
                LogInfo($"버튼 이벤트 연결 완료: {xrayType}");
            }
        }

        protected override void ConnectToViewModel()
        {
            SubscribeToViewModel();
        }

        protected override void DisconnectViewEvents()
        {
            if (_isButtonEventConnected && btnXray != null)
            {
                btnXray.onClick.RemoveListener(OnButtonClick);
                _isButtonEventConnected = false;
                LogInfo($"버튼 이벤트 해제: {xrayType}");
            }
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

            if (Area3DViewModel.Instance == null)
            {
                LogError("Area3DViewModel.Instance가 null입니다!");
                return;
            }

            Area3DViewModel.Instance.OnObservatoryLoadedWithNames.AddListener(OnObservatoryLoaded);
            Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);

            _isSubscribed = true;
            LogInfo("✅ Area3DViewModel 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.RemoveListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }

            _isSubscribed = false;
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        private void OnObservatoryLoaded(int obsId, string areaName, string obsName)
        {
            LogInfo($"관측소 로드: ObsId={obsId}");
            ShowButton();
        }

        private void OnObservatoryClosed()
        {
            LogInfo("관측소 닫기");
            HideButton();
        }

        #endregion

        #region UI 이벤트 핸들러

        /// <summary>
        /// 버튼 클릭 핸들러
        /// </summary>
        private void OnButtonClick()
        {
            LogInfo($"========================================");
            LogInfo($"🖱️ {xrayType} X-Ray 버튼 클릭!");

            if (XrayViewModel.Instance == null)
            {
                LogError("XrayViewModel.Instance가 null입니다!");
                return;
            }

            // ⭐⭐⭐ 현재 상태 출력
            LogInfo($"클릭 전 상태 - Structure: {XrayViewModel.Instance.IsStructureXrayActive}, Equipment: {XrayViewModel.Instance.IsEquipmentXrayActive}");

            if (xrayType == XrayType.Structure)
            {
                XrayViewModel.Instance.ToggleStructureXray();
            }
            else if (xrayType == XrayType.Equipment)
            {
                XrayViewModel.Instance.ToggleEquipmentXray();
            }

            // ⭐⭐⭐ 변경 후 상태 출력
            LogInfo($"클릭 후 상태 - Structure: {XrayViewModel.Instance.IsStructureXrayActive}, Equipment: {XrayViewModel.Instance.IsEquipmentXrayActive}");
            LogInfo($"========================================");
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

        #region Inspector 검증

        private void OnValidate()
        {
            if (btnXray == null)
            {
                btnXray = GetComponent<Button>();
                if (btnXray == null)
                {
                    btnXray = GetComponentInChildren<Button>();
                }
            }

            if (lblText == null)
            {
                lblText = GetComponentInChildren<TMP_Text>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (lblText != null)
            {
                lblText.text = xrayType == XrayType.Structure ? "건물 X-Ray" : "장비 X-Ray";
            }
        }

        #endregion
    }
}