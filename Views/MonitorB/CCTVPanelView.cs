using Onthesys;
using UMP;
using UnityEngine;
using UnityEngine.Events;
using ViewModels.MonitorB;

namespace Views.MonitorB
{
    public class CCTVPanelView : MonoBehaviour
    {
        [Header("Video Players")]
        [SerializeField] private UniversalMediaPlayer videoPlayerA;
        [SerializeField] private UniversalMediaPlayer videoPlayerB;

        [Header("UI Buttons")]
        [SerializeField] private GameObject btnPlayA;
        [SerializeField] private GameObject btnPauseA;
        [SerializeField] private GameObject btnPlayB;
        [SerializeField] private GameObject btnPauseB;

        [Header("Close Button")]
        [SerializeField] private UnityEngine.UI.Button closeButton;

        // 팝업 버튼들
        [Header("Popup Buttons")]
        [SerializeField] private UnityEngine.UI.Button popupButtonA;
        [SerializeField] private UnityEngine.UI.Button popupButtonB;

        // ⭐ 추가: 팝업 창들
        [Header("Popup Windows")]
        [SerializeField] private GameObject popupVideoA;
        [SerializeField] private GameObject popupVideoB;


        private CCTVViewModel viewModel;
        private int currentObsId = -1;

        private void Awake()
        {
            // 버튼 자동 찾기 (Inspector에 할당 안 했을 경우)
            if (btnPlayA == null || btnPauseA == null)
            {
                var buttonsA = transform.Find("Video_Player A/Buttons");
                if (buttonsA != null)
                {
                    btnPlayA = buttonsA.Find("Btn_Play")?.gameObject;
                    btnPauseA = buttonsA.Find("Btn_Pause")?.gameObject;
                }
            }

            if (btnPlayB == null || btnPauseB == null)
            {
                var buttonsB = transform.Find("Video_Player B/Buttons");
                if (buttonsB != null)
                {
                    btnPlayB = buttonsB.Find("Btn_Play")?.gameObject;
                    btnPauseB = buttonsB.Find("Btn_Pause")?.gameObject;
                }
            }

            // ⭐ 팝업 버튼 자동 찾기
            if (popupButtonA == null)
            {
                var popupBtnA = transform.Find("Video_Player A/PopUP_Btn");
                if (popupBtnA != null)
                    popupButtonA = popupBtnA.GetComponent<UnityEngine.UI.Button>();
            }

            if (popupButtonB == null)
            {
                var popupBtnB = transform.Find("Video_Player B/PopUP_Btn");
                if (popupBtnB != null)
                    popupButtonB = popupBtnB.GetComponent<UnityEngine.UI.Button>();
            }
        }

        private void OnEnable()
        {
            Debug.Log("[CCTVPanelView] 패널 열림 - 초기화");

            if (viewModel == null)
                InitializeViewModel();

            // ⭐⭐⭐ 핵심 1: MediaPlayer 컴포넌트 재활성화
            ResetMediaPlayers();

            // ⭐⭐⭐ 핵심 2: UI 버튼 상태 초기화
            ResetButtonStates();

            SetupCloseButton();

            SetupPopupButtons();

            Debug.Log("[CCTVPanelView] 초기화 완료");
        }

        // ⭐ 새로 추가: 팝업 버튼 이벤트 연결
        private void SetupPopupButtons()
        {
            if (popupButtonA != null)
            {
                popupButtonA.onClick.RemoveAllListeners();
                popupButtonA.onClick.AddListener(OnClickPopupA);
            }

            if (popupButtonB != null)
            {
                popupButtonB.onClick.RemoveAllListeners();
                popupButtonB.onClick.AddListener(OnClickPopupB);
            }
        }

        // ⭐ Popup A 열기
        private void OnClickPopupA()
        {
            if (popupVideoA == null)
            {
                Debug.LogError("[CCTVPanelView] PopupVideoA가 할당되지 않았습니다!");
                return;
            }

            Debug.Log("[CCTVPanelView] PopupVideoA 열기");
            popupVideoA.SetActive(true);

            var popupView = popupVideoA.GetComponent<PopupCCTVView>();
            if (popupView != null)
            {
                popupView.LoadCCTV(currentObsId, CCTVType.VideoA);
            }
        }

        // ⭐ Popup B 열기
        private void OnClickPopupB()
        {
            if (popupVideoB == null)
            {
                Debug.LogError("[CCTVPanelView] PopupVideoB가 할당되지 않았습니다!");
                return;
            }

            Debug.Log("[CCTVPanelView] PopupVideoB 열기");
            popupVideoB.SetActive(true);

            var popupView = popupVideoB.GetComponent<PopupCCTVView>();
            if (popupView != null)
            {
                popupView.LoadCCTV(currentObsId, CCTVType.VideoB);
            }
        }

        public void LoadCCTV(int obsId)
        {
            Debug.Log($"[CCTVPanelView] CCTV 로드 요청: {obsId}");
            currentObsId = obsId;  // ⭐ 저장
            ResetMediaPlayers();
            ResetButtonStates();
            viewModel?.LoadCCTVUrls(obsId);
        }


        /// <summary>
        /// MediaPlayer 컴포넌트 완전 초기화
        /// </summary>
        private void ResetMediaPlayers()
        {
            if (videoPlayerA != null)
            {
                videoPlayerA.Stop();
                videoPlayerA.Path = "";
                videoPlayerA.enabled = false;
                videoPlayerA.enabled = true;  // ← 이게 핵심!
            }

            if (videoPlayerB != null)
            {
                videoPlayerB.Stop();
                videoPlayerB.Path = "";
                videoPlayerB.enabled = false;
                videoPlayerB.enabled = true;  // ← 이게 핵심!
            }
        }

        /// <summary>
        /// UI 버튼 상태 명시적 초기화
        /// </summary>
        private void ResetButtonStates()
        {
            // Video A 버튼
            if (btnPlayA != null) btnPlayA.SetActive(true);
            if (btnPauseA != null) btnPauseA.SetActive(false);

            // Video B 버튼
            if (btnPlayB != null) btnPlayB.SetActive(true);
            if (btnPauseB != null) btnPauseB.SetActive(false);

            Debug.Log("[CCTVPanelView] 버튼 상태 초기화 완료");
        }

        private void InitializeViewModel()
        {
            viewModel = CCTVViewModel.Instance;
            viewModel.OnCCTVUrlsLoaded += OnCCTVUrlsLoaded;
        }

        private void SetupCloseButton()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ClosePanel);
            }
        }


        private void OnCCTVUrlsLoaded(string video1Url, string video2Url)
        {
            if (!gameObject.activeInHierarchy) return;

            Debug.Log($"[CCTVPanelView] URL 로드 완료");
            Debug.Log($"[CCTVPanelView] Video A: {video1Url}");
            Debug.Log($"[CCTVPanelView] Video B: {video2Url}");

            // 원본처럼 단순하게 - Prepare() 없이!
            if (!string.IsNullOrEmpty(video1Url))
            {
                videoPlayerA.Path = video1Url;
            }

            if (!string.IsNullOrEmpty(video2Url))
            {
                videoPlayerB.Path = video2Url;
            }
        }

        private void ClosePanel()
        {
            Debug.Log("[CCTVPanelView] 패널 닫기 시작");

            // 정리
            if (videoPlayerA != null && videoPlayerA.IsPlaying)
                videoPlayerA.Stop();

            if (videoPlayerB != null && videoPlayerB.IsPlaying)
                videoPlayerB.Stop();

            ResetButtonStates();

            gameObject.SetActive(false);
            Debug.Log("[CCTVPanelView] 패널 닫기 완료");
        }

        private void OnDisable()
        {
            Debug.Log("[CCTVPanelView] OnDisable 호출");
        }

        private void OnDestroy()
        {
            if (viewModel != null)
                viewModel.OnCCTVUrlsLoaded -= OnCCTVUrlsLoaded;
        }
    }
}