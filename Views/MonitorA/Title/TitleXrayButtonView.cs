using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
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
        private bool _isButtonEventConnected = false;

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
            if (_isButtonEventConnected) return;

            if (btnXray != null)
            {
                btnXray.onClick.RemoveAllListeners();
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

            // ⭐⭐⭐ XrayViewModel 이벤트 구독 추가!
            if (XrayViewModel.Instance != null)
            {
                if (xrayType == XrayType.Structure)
                {
                    XrayViewModel.Instance.OnStructureXrayChanged.AddListener(OnXrayStateChanged);
                }
                else
                {
                    XrayViewModel.Instance.OnEquipmentXrayChanged.AddListener(OnXrayStateChanged);
                }
            }

            _isSubscribed = true;
            LogInfo("✅ ViewModel 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (!_isSubscribed) return;

            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.RemoveListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }

            // ⭐⭐⭐ XrayViewModel 이벤트 구독 해제
            if (XrayViewModel.Instance != null)
            {
                if (xrayType == XrayType.Structure)
                {
                    XrayViewModel.Instance.OnStructureXrayChanged.RemoveListener(OnXrayStateChanged);
                }
                else
                {
                    XrayViewModel.Instance.OnEquipmentXrayChanged.RemoveListener(OnXrayStateChanged);
                }
            }

            _isSubscribed = false;
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        private void OnObservatoryLoaded(int obsId, string areaName, string obsName)
        {
            LogInfo($"관측소 로드: ObsId={obsId}");
            ShowButton();

            // ⭐⭐⭐ 버튼 UI 상태 초기화!
            UpdateButtonVisual(false);
        }

        private void OnObservatoryClosed()
        {
            LogInfo("관측소 닫기");
            HideButton();

            // ⭐⭐⭐ 버튼 UI 상태 초기화!
            UpdateButtonVisual(false);
        }

        // ⭐⭐⭐ X-Ray 상태 변경 시 버튼 UI 업데이트
        private void OnXrayStateChanged(bool isActive)
        {
            UpdateButtonVisual(isActive);
            LogInfo($"{xrayType} X-Ray 상태 변경: {isActive}");
        }

        #endregion

        #region UI 이벤트 핸들러

        private void OnButtonClick()
        {
            LogInfo($"========================================");
            LogInfo($"🖱️ {xrayType} X-Ray 버튼 클릭!");

            if (XrayViewModel.Instance == null)
            {
                LogError("XrayViewModel.Instance가 null입니다!");
                return;
            }

            LogInfo($"클릭 전 상태 - Structure: {XrayViewModel.Instance.IsStructureXrayActive}, Equipment: {XrayViewModel.Instance.IsEquipmentXrayActive}");

            if (xrayType == XrayType.Structure)
            {
                XrayViewModel.Instance.ToggleStructureXray();
            }
            else if (xrayType == XrayType.Equipment)
            {
                XrayViewModel.Instance.ToggleEquipmentXray();
            }

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

        // ⭐⭐⭐ 버튼 활성화 상태 시각적 업데이트
        private void UpdateButtonVisual(bool isActive)
        {
            // 버튼 색상 변경 등 시각적 피드백
            if (btnXray != null)
            {
                var colors = btnXray.colors;
                colors.normalColor = isActive ? Color.green : Color.white;
                btnXray.colors = colors;
            }

            // 텍스트 업데이트 (선택사항)
            if (lblText != null)
            {
                string baseName = xrayType == XrayType.Structure ? "건물 X-Ray" : "장비 X-Ray";
                lblText.text = isActive ? $"{baseName} (ON)" : baseName;
            }

            LogInfo($"{xrayType} 버튼 UI 업데이트: {isActive}");
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