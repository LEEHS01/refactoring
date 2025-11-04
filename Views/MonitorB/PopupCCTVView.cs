using System;
using System.Net.Http;
using System.Text;
using TMPro;
using UMP;
using UnityEngine;
using UnityEngine.UI;
using ViewModels.MonitorB;

namespace Views.MonitorB
{
    /// <summary>
    /// CCTV 타입 (A 또는 B)
    /// </summary>
    public enum CCTVType
    {
        VideoA,
        VideoB
    }

    /// <summary>
    /// 개별 CCTV 팝업 - PTZ 제어 포함
    /// </summary>
    public class PopupCCTVView : MonoBehaviour
    {
        [Header("CCTV Info")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private UniversalMediaPlayer mediaPlayer;

        [Header("UI Buttons")]
        [SerializeField] private GameObject btnPlay;
        [SerializeField] private GameObject btnPause;
        [SerializeField] private Button closeButton;

        [Header("PTZ Control Buttons")]
        [SerializeField] private Button btnCamUp;
        [SerializeField] private Button btnCamDown;
        [SerializeField] private Button btnCamLeft;
        [SerializeField] private Button btnCamRight;
        [SerializeField] private Button btnCamIn;   // 줌인
        [SerializeField] private Button btnCamOut;  // 줌아웃

        [Header("Loading")]
        [SerializeField] private GameObject spinner;

        private CCTVViewModel viewModel;
        private CCTVType cctvType;
        private string cctvIP;  // PTZ 제어용 IP
        private int currentObsId;

        private void Awake()
        {
            // 자동 찾기
            if (btnPlay == null)
                btnPlay = transform.Find("Buttons/Btn_Play")?.gameObject;
            if (btnPause == null)
                btnPause = transform.Find("Buttons/Btn_Pause")?.gameObject;
            if (closeButton == null)
                closeButton = transform.Find("Buttons/Btn_Close")?.GetComponent<Button>();

            // PTZ 버튼 자동 찾기
            if (btnCamUp == null)
                btnCamUp = transform.Find("Buttons/Btn_Cam_Up")?.GetComponent<Button>();
            if (btnCamDown == null)
                btnCamDown = transform.Find("Buttons/Btn_Cam_Down")?.GetComponent<Button>();
            if (btnCamLeft == null)
                btnCamLeft = transform.Find("Buttons/Btn_Cam_Left")?.GetComponent<Button>();
            if (btnCamRight == null)
                btnCamRight = transform.Find("Buttons/Btn_Cam_Right")?.GetComponent<Button>();
            if (btnCamIn == null)
                btnCamIn = transform.Find("Buttons/Btn_Cam_In")?.GetComponent<Button>();
            if (btnCamOut == null)
                btnCamOut = transform.Find("Buttons/Btn_Cam_Out")?.GetComponent<Button>();

            if (spinner == null)
                spinner = transform.Find("Buttons/Spinner")?.gameObject;
        }

        private void OnEnable()
        {
            Debug.Log("[PopupCCTVView] 팝업 열림 - 초기화");

            // 버튼 상태 초기화
            if (btnPlay != null) btnPlay.SetActive(true);
            if (btnPause != null) btnPause.SetActive(false);
            if (spinner != null) spinner.SetActive(false);

            SetupButtons();

            Debug.Log("[PopupCCTVView] 초기화 완료");
        }

        private void SetupButtons()
        {
            // Close 버튼
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ClosePopup);
            }

            // PTZ 버튼들
            if (btnCamUp != null)
            {
                btnCamUp.onClick.RemoveAllListeners();
                btnCamUp.onClick.AddListener(() => OnPTZCommand("up", 2));
            }
            if (btnCamDown != null)
            {
                btnCamDown.onClick.RemoveAllListeners();
                btnCamDown.onClick.AddListener(() => OnPTZCommand("down", 2));
            }
            if (btnCamLeft != null)
            {
                btnCamLeft.onClick.RemoveAllListeners();
                btnCamLeft.onClick.AddListener(() => OnPTZCommand("left", 2));
            }
            if (btnCamRight != null)
            {
                btnCamRight.onClick.RemoveAllListeners();
                btnCamRight.onClick.AddListener(() => OnPTZCommand("right", 2));
            }
            if (btnCamIn != null)
            {
                btnCamIn.onClick.RemoveAllListeners();
                btnCamIn.onClick.AddListener(() => OnPTZCommand("zoomin", 6));
            }
            if (btnCamOut != null)
            {
                btnCamOut.onClick.RemoveAllListeners();
                btnCamOut.onClick.AddListener(() => OnPTZCommand("zoomout", 6));
            }
        }

        /// <summary>
        /// CCTV 로드
        /// </summary>
        public void LoadCCTV(int obsId, CCTVType type)
        {
            currentObsId = obsId;
            cctvType = type;

            if (viewModel == null)
            {
                viewModel = CCTVViewModel.Instance;
                viewModel.OnCCTVUrlsLoaded += OnCCTVUrlsLoaded;
            }

            Debug.Log($"[PopupCCTVView] CCTV 로드: 관측소 {obsId}, 타입: {type}");
            viewModel.LoadCCTVUrls(obsId);
        }

        private void OnCCTVUrlsLoaded(string video1Url, string video2Url)
        {
            if (!gameObject.activeInHierarchy) return;

            // 타입에 따라 URL 선택
            string rtspUrl = cctvType == CCTVType.VideoA ? video1Url : video2Url;

            if (string.IsNullOrEmpty(rtspUrl))
            {
                Debug.LogWarning($"[PopupCCTVView] CCTV URL이 비어있습니다! (Type: {cctvType})");
                return;
            }

            Debug.Log($"[PopupCCTVView] CCTV URL 설정: {rtspUrl}");

            // PTZ 제어용 IP 추출
            ExtractCCTVIP(rtspUrl);

            // MediaPlayer에 URL 설정
            if (mediaPlayer != null)
            {
                mediaPlayer.Stop();
                mediaPlayer.Path = "";
                mediaPlayer.enabled = false;
                mediaPlayer.enabled = true;

                mediaPlayer.Path = rtspUrl;
            }

            // 제목 설정 (옵션)
            if (titleText != null)
            {
                titleText.text = $"CCTV {(cctvType == CCTVType.VideoA ? "외부모니터" : "내부모니터")}";
            }
        }

        /// <summary>
        /// RTSP URL에서 IP 추출
        /// </summary>
        private void ExtractCCTVIP(string rtspUrl)
        {
            if (string.IsNullOrEmpty(rtspUrl))
            {
                Debug.LogWarning("[PopupCCTVView] RTSP URL이 비어있습니다!");
                return;
            }

            // rtsp://admin:password@115.91.85.42:554/video1
            var match = System.Text.RegularExpressions.Regex.Match(
                rtspUrl, @"@([\d\.]+):(\d+)");

            if (match.Success)
            {
                string ip = match.Groups[1].Value;
                string port = match.Groups[2].Value;

                // RTSP 포트에 따라 PTZ 포트 매핑
                string ptzPort = "50080";  // 기본값

                if (port == "554")
                    ptzPort = "50080";  // Video A
                else if (port == "50556")
                    ptzPort = "50081";  // Video B

                cctvIP = $"{ip}:{ptzPort}";

                Debug.Log($"[PopupCCTVView] PTZ 제어 IP: {cctvIP} (RTSP 포트: {port})");
            }
            else
            {
                Debug.LogError($"[PopupCCTVView] IP 추출 실패: {rtspUrl}");
            }
        }

        /// <summary>
        /// PTZ 제어 명령
        /// </summary>
        private async void OnPTZCommand(string direction, int speed)
        {
            if (string.IsNullOrEmpty(cctvIP))
            {
                Debug.LogError("[PopupCCTVView] cctvIP가 설정되지 않았습니다!");
                return;
            }

            int timeout = 1000;  // 1초

            // ⭐ 원본과 동일한 URL 형식!
            string requestUrl = $"http://{cctvIP}/httpapi/SendPTZ?action=sendptz&PTZ_CHANNEL=1&PTZ_MOVE={direction},{speed}&PTZ_TIMEOUT={timeout}";

            Debug.Log($"[PopupCCTVView] PTZ 제어 요청: {direction} - IP: {cctvIP}");
            Debug.Log($"[PopupCCTVView] 요청 URL: {requestUrl}");

            // ⭐ 실제 HTTP 요청!
            HttpClient client = new HttpClient();

            // ⭐ Basic 인증 추가!
            var byteArray = Encoding.ASCII.GetBytes("admin:HNS_qhdks_!Q@W3");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(byteArray));

            try
            {
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log($"[PopupCCTVView] HttpRequest 성공: {responseBody}");
            }
            catch (HttpRequestException httpEx)
            {
                Debug.LogError($"[PopupCCTVView] HttpRequestException: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupCCTVView] PTZ 제어 오류: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        private void ClosePopup()
        {
            Debug.Log("[PopupCCTVView] 팝업 닫기");

            if (mediaPlayer != null && mediaPlayer.IsPlaying)
                mediaPlayer.Stop();

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (viewModel != null)
                viewModel.OnCCTVUrlsLoaded -= OnCCTVUrlsLoaded;
        }
    }
}
