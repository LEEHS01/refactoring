using Core;
using HNS.Common.ViewModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.Common.Views
{
    /// <summary>
    /// 환경설정 팝업 메인 View (CanvasGroup 방식)
    /// </summary>
    public class PopupSettingView : BaseView
    {
        [Header("Main UI")]
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnTabObs;
        [SerializeField] private Button btnTabSystem;

        [Header("Canvas Group")]
        [SerializeField] private CanvasGroup canvasGroup;  // ⭐ 추가

        [Header("Tab Panels")]
        [SerializeField] private GameObject pnlTabObs;
        [SerializeField] private GameObject pnlTabSystem;

        [Header("Sub Views")]
        [SerializeField] private PopupSettingTabObsView tabObsView;
        [SerializeField] private PopupSettingTabSystemView tabSystemView;

        [Header("Tab Sprites")]
        [SerializeField] private Sprite sprTabOn;
        [SerializeField] private Sprite sprTabOff;

        private bool _isFromObservatory = false;
        private bool _isSubscribed = false;
        private Vector3 _defaultPos;

        #region BaseView Override

        protected override void InitializeUIComponents()
        {
            LogInfo("UI 컴포넌트 초기화");

            // CanvasGroup 자동 찾기/추가
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Inspector에서 연결되지 않은 경우 동적으로 찾기
            if (btnClose == null)
                btnClose = transform.Find("btnClosePopup")?.GetComponent<Button>();

            Transform conTabButtons = transform.Find("conTabButtons");
            if (conTabButtons != null)
            {
                if (btnTabObs == null)
                    btnTabObs = conTabButtons.Find("btnTabObs")?.GetComponent<Button>();
                if (btnTabSystem == null)
                    btnTabSystem = conTabButtons.Find("btnTabSystem")?.GetComponent<Button>();
            }

            Transform conTabPanels = transform.Find("conTabPanels");
            if (conTabPanels != null)
            {
                if (pnlTabObs == null)
                    pnlTabObs = conTabPanels.Find("pnlTabObs")?.gameObject;
                if (pnlTabSystem == null)
                    pnlTabSystem = conTabPanels.Find("pnlTabSystem")?.gameObject;

                if (tabObsView == null && pnlTabObs != null)
                    tabObsView = pnlTabObs.GetComponent<PopupSettingTabObsView>();
                if (tabSystemView == null && pnlTabSystem != null)
                    tabSystemView = pnlTabSystem.GetComponent<PopupSettingTabSystemView>();
            }

            bool isValid = btnClose != null &&
                           btnTabObs != null &&
                           btnTabSystem != null &&
                           pnlTabObs != null &&
                           pnlTabSystem != null &&
                           tabObsView != null &&
                           tabSystemView != null;

            if (!isValid)
            {
                LogError("필수 UI 컴포넌트가 누락되었습니다!");
                if (btnClose == null) LogError("btnClose가 null입니다!");
                if (btnTabObs == null) LogError("btnTabObs가 null입니다!");
                if (btnTabSystem == null) LogError("btnTabSystem가 null입니다!");
                if (pnlTabObs == null) LogError("pnlTabObs가 null입니다!");
                if (pnlTabSystem == null) LogError("pnlTabSystem가 null입니다!");
                if (tabObsView == null) LogError("tabObsView가 null입니다!");
                if (tabSystemView == null) LogError("tabSystemView가 null입니다!");
                return;
            }

            // 스프라이트 로드
            if (sprTabOn == null)
                sprTabOn = Resources.Load<Sprite>("Image/UI/Btn_Search_p");
            if (sprTabOff == null)
                sprTabOff = Resources.Load<Sprite>("Image/UI/Btn_Search_n");

            _defaultPos = transform.position;

            // 초기 상태 (CanvasGroup으로 숨김)
            OnOpenTab(SettingTabType.Observatory);
            HidePopup();  // ⭐ SetActive(false) 대신 CanvasGroup

            LogInfo("UI 컴포넌트 초기화 완료");
        }

        protected override void SetupViewEvents()
        {
            LogInfo("View 이벤트 설정");

            if (btnClose != null)
                btnClose.onClick.AddListener(OnClickClose);
            if (btnTabObs != null)
                btnTabObs.onClick.AddListener(() => OnClickTab(SettingTabType.Observatory));
            if (btnTabSystem != null)
                btnTabSystem.onClick.AddListener(() => OnClickTab(SettingTabType.System));
        }

        protected override void ConnectToViewModel()
        {
            if (_isSubscribed) return;

            if (PopupSettingViewModel.Instance == null)
            {
                LogError("PopupSettingViewModel.Instance가 null입니다!");
                return;
            }

            PopupSettingViewModel.Instance.OnTabChanged += OnTabChanged;
            PopupSettingViewModel.Instance.OnError += OnError;

            _isSubscribed = true;
            LogInfo("ViewModel 이벤트 구독 완료");
        }

        protected override void DisconnectFromViewModel()
        {
            if (!_isSubscribed) return;

            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OnTabChanged -= OnTabChanged;
                PopupSettingViewModel.Instance.OnError -= OnError;
            }

            _isSubscribed = false;
            LogInfo("ViewModel 이벤트 구독 해제");
        }

        protected override void DisconnectViewEvents()
        {
            if (btnClose != null)
                btnClose.onClick.RemoveListener(OnClickClose);
            if (btnTabObs != null)
                btnTabObs.onClick.RemoveAllListeners();
            if (btnTabSystem != null)
                btnTabSystem.onClick.RemoveAllListeners();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 팝업 열기 (CanvasGroup 사용)
        /// </summary>
        public void OpenPopup(int obsId = 0)
        {
            _isFromObservatory = (obsId > 0);

            // 관측소에서 열면 시스템 탭 숨김
            if (btnTabSystem != null)
                btnTabSystem.gameObject.SetActive(!_isFromObservatory);

            // 관측소 탭으로 시작
            if (_isFromObservatory)
            {
                OnOpenTab(SettingTabType.Observatory);
            }

            transform.position = _defaultPos;
            ShowPopup();  // ⭐ CanvasGroup으로 보이기

            // ViewModel에 데이터 로드 요청
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OpenSettings(obsId);
            }

            LogInfo($"팝업 열림: ObsId={obsId}, FromObs={_isFromObservatory}");
        }

        #endregion

        #region ViewModel Event Handlers

        private void OnTabChanged(SettingTabType tab)
        {
            LogInfo($"탭 전환: {tab}");
            OnOpenTab(tab);
        }

        private void OnError(string errorMsg)
        {
            LogError($"오류 발생: {errorMsg}");
        }

        #endregion

        #region UI Event Handlers

        private void OnClickTab(SettingTabType tab)
        {
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.SwitchTab(tab);
            }
        }

        private void OnClickClose()
        {
            HidePopup();  // ⭐ CanvasGroup으로 숨기기
            LogInfo("팝업 닫힘");
        }

        #endregion

        #region UI Update

        /// <summary>
        /// 탭 열기
        /// </summary>
        private void OnOpenTab(SettingTabType tab)
        {
            // 패널 표시/숨김
            if (pnlTabObs != null)
                pnlTabObs.SetActive(tab == SettingTabType.Observatory);
            if (pnlTabSystem != null)
                pnlTabSystem.SetActive(tab == SettingTabType.System);

            // 탭 버튼 UI 업데이트
            UpdateTabButtonVisual(btnTabObs, tab == SettingTabType.Observatory);
            UpdateTabButtonVisual(btnTabSystem, tab == SettingTabType.System);
        }

        private void UpdateTabButtonVisual(Button btn, bool isActive)
        {
            if (btn == null) return;

            // 배경 이미지
            Image imgBtn = btn.GetComponent<Image>();
            if (imgBtn != null)
                imgBtn.sprite = isActive ? sprTabOn : sprTabOff;

            // 텍스트 색상
            TMP_Text txtBtn = btn.GetComponentInChildren<TMP_Text>();
            if (txtBtn != null)
            {
                txtBtn.color = isActive ? Color.white : GetGrayColor();
            }
        }

        private Color GetGrayColor()
        {
            ColorUtility.TryParseHtmlString("#99B1CB", out Color grayColor);
            return grayColor;
        }

        #endregion

        #region CanvasGroup Show/Hide

        /// <summary>
        /// ⭐ CanvasGroup으로 팝업 보이기
        /// </summary>
        private void ShowPopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            LogInfo("팝업 표시 (CanvasGroup)");
        }

        /// <summary>
        /// ⭐ CanvasGroup으로 팝업 숨기기
        /// </summary>
        private void HidePopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            LogInfo("팝업 숨김 (CanvasGroup)");
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[PopupSettingView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PopupSettingView] {message}");
        }

        #endregion
    }
}