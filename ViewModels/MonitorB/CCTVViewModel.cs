using System;
using UnityEngine;
using Repositories.MonitorB;

namespace ViewModels.MonitorB
{
    /// <summary>
    /// CCTV ViewModel
    /// </summary>
    public class CCTVViewModel : MonoBehaviour
    {
        public static CCTVViewModel Instance { get; private set; }

        private ObservatoryRepository repository = new ObservatoryRepository();

        // 이벤트
        public event Action<string, string> OnCCTVUrlsLoaded; // video1, video2
        public event Action<string> OnError;

        // 현재 로드된 CCTV URL
        public string CurrentVideo1Url { get; private set; }
        public string CurrentVideo2Url { get; private set; }
        public int CurrentObsId { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[CCTVViewModel] 초기화 완료");
        }

        /// <summary>
        /// 관측소 CCTV URL 로드
        /// </summary>
        public void LoadCCTVUrls(int obsId)
        {
            if (obsId <= 0)
            {
                Debug.LogError($"[CCTVViewModel] 잘못된 obsId: {obsId}");
                OnError?.Invoke("잘못된 관측소 ID입니다.");
                return;
            }

            CurrentObsId = obsId;
            Debug.Log($"[CCTVViewModel] 관측소 {obsId} CCTV URL 로드 시작...");

            StartCoroutine(repository.GetObservatoryCCTV(
                obsId,
                OnLoadSuccess,
                OnLoadFailed
            ));
        }

        private void OnLoadSuccess(Models.MonitorB.ObservatoryModel obs)  // Models. 명시
        {
            CurrentVideo1Url = obs.OUT_CCTVURL; // 외부
            CurrentVideo2Url = obs.IN_CCTVURL;  // 내부

            Debug.Log($"[CCTVViewModel] CCTV URL 로드 성공");
            Debug.Log($"[CCTVViewModel] Video1 (외부): {CurrentVideo1Url}");
            Debug.Log($"[CCTVViewModel] Video2 (내부): {CurrentVideo2Url}");

            OnCCTVUrlsLoaded?.Invoke(CurrentVideo1Url, CurrentVideo2Url);
        }

        private void OnLoadFailed(string error)
        {
            Debug.LogError($"[CCTVViewModel] CCTV URL 로드 실패: {error}");
            OnError?.Invoke(error);
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        public void Refresh()
        {
            if (CurrentObsId > 0)
            {
                LoadCCTVUrls(CurrentObsId);
            }
        }
    }
}