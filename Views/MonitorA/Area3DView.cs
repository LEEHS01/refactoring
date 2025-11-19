using Core;
using HNS.MonitorA.ViewModels;
using UnityEngine;
using Views.MonitorA;  // ⭐ ObsMonitoringView 사용

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 3D 관측소 화면 View
    /// Area_3D GameObject에 부착
    /// </summary>
    public class Area3DView : BaseView
    {
        #region Inspector 설정
        [Header("3D Scene Components")]
        [SerializeField] private Camera camera3D;
        [SerializeField] private GameObject observatory;
        [SerializeField] private GameObject terrain;

        [Header("Canvas Control")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Navigation")]
        [SerializeField] private MapAreaView mapAreaView;

        [Header("Monitoring Panel")]
        [SerializeField] private ObsMonitoringView obsMonitoringView;  // ⭐ ObsMonitoring 패널
        #endregion

        #region Private Fields
        private int _currentObsId = -1;
        #endregion

        #region Unity Lifecycle Override

        // ⭐ BaseView의 OnDisable을 오버라이드하여 이벤트 구독 유지
        protected override void OnDisable()
        {
            LogInfo("OnDisable 호출 - 이벤트 구독 유지 (오버라이드)");
            // base.OnDisable() 호출하지 않음!
            // 이벤트 구독을 해제하지 않고 유지
        }

        #endregion

        #region BaseView 구현
        protected override void InitializeUIComponents()
        {
            bool isValid = ValidateComponents(
                (camera3D, "camera3D")
            );

            if (!isValid)
            {
                LogError("필수 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            if (observatory == null)
            {
                LogError("observatory가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            if (terrain == null)
            {
                LogInfo("Terrain이 연결되지 않음 (옵션)");
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

            // ⭐ ObsMonitoringView 자동 찾기
            if (obsMonitoringView == null)
            {
                obsMonitoringView = FindObjectOfType<ObsMonitoringView>(true);
                if (obsMonitoringView != null)
                {
                    LogInfo("ObsMonitoringView 자동으로 찾음");
                }
                else
                {
                    LogWarning("ObsMonitoringView를 찾을 수 없습니다! Inspector에서 연결하세요.");
                }
            }

            LogInfo("컴포넌트 초기화 완료");

            Hide3DScene();
        }

        protected override void SetupViewEvents()
        {
            LogInfo("View 이벤트 설정 완료 (없음)");
        }

        protected override void ConnectToViewModel()
        {
            if (Area3DViewModel.Instance == null)
            {
                LogError("Area3DViewModel.Instance가 null입니다!");
                return;
            }

            // ⭐⭐⭐ 기존 이벤트 대신 새 이벤트 구독!
            Area3DViewModel.Instance.OnObservatoryLoadedWithNames.AddListener(OnObservatoryLoaded);
            //                       ^^^^^^^^^^^^^^^^^^^^^^ 이름 변경!

            Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
            Area3DViewModel.Instance.OnError.AddListener(OnError);

            LogInfo("ViewModel 이벤트 구독 완료");
        }

        protected override void DisconnectFromViewModel()
        {
            if (Area3DViewModel.Instance != null)
            {
                // ⭐⭐⭐ 새 이벤트 구독 해제!
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.RemoveListener(OnObservatoryLoaded);

                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
                Area3DViewModel.Instance.OnError.RemoveListener(OnError);
            }
        }

        protected override void DisconnectViewEvents()
        {
            // 별도 View 이벤트 없음
        }
        #endregion

        #region ViewModel 이벤트 핸들러

        private void OnObservatoryLoaded(int obsId, string areaName, string obsName)
        {
            _currentObsId = obsId;

            LogInfo($"========================================");
            LogInfo($"3D 관측소 표시: ObsId={obsId}, Area={areaName}, Obs={obsName}");

            Show3DScene();

            // ⭐ ObsMonitoring 패널 표시
            if (obsMonitoringView != null)
            {
                obsMonitoringView.Show(obsId);
                LogInfo($"ObsMonitoring 패널 표시: ObsId={obsId}");
            }
            else
            {
                LogWarning("ObsMonitoringView가 null입니다!");
            }

            LogInfo("3D 화면 활성화 완료");
            LogInfo("========================================");
        }
        private void OnObservatoryClosed()
        {
            LogInfo("========================================");
            LogInfo("3D 관측소 닫기");

            Hide3DScene();

            // ⭐ ObsMonitoring 패널 숨김
            if (obsMonitoringView != null)
            {
                obsMonitoringView.Hide();
                LogInfo("ObsMonitoring 패널 숨김");
            }

            LogInfo("3D 관측소 정리 완료");
            LogInfo("========================================");
        }

        private void OnError(string errorMessage)
        {
            LogError($"ViewModel 에러: {errorMessage}");
        }

        #endregion

        #region 공개 메서드

        public void ShowObservatory(int obsId, string areaName, string obsName)
        {
            LogInfo($"ShowObservatory 호출: ObsId={obsId}, Area={areaName}, Obs={obsName}");

            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.LoadObservatory(obsId, areaName, obsName);  // ⭐ 3개 전달!
            }
            else
            {
                LogError("Area3DViewModel.Instance가 null입니다!");
            }
        }

        public void HideObservatory()
        {
            LogInfo("HideObservatory 호출");

            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.CloseObservatory();
            }
            else
            {
                LogError("Area3DViewModel.Instance가 null입니다!");
            }
        }
        #endregion

        #region Helper Methods

        private void Show3DScene()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                LogInfo("3D 화면 표시");
            }

            if (camera3D != null)
                camera3D.gameObject.SetActive(true);

            if (observatory != null)
                observatory.SetActive(true);

            if (terrain != null)
                terrain.SetActive(true);
        }

        private void Hide3DScene()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                LogInfo("3D 화면 숨김");
            }

            if (camera3D != null)
                camera3D.gameObject.SetActive(false);

            if (observatory != null)
                observatory.SetActive(false);

            if (terrain != null)
                terrain.SetActive(false);
        }
        #endregion

        #region 로깅
        private void LogInfo(string message)
        {
            Debug.Log($"[Area3DView] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[Area3DView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[Area3DView] {message}");
        }
        #endregion
    }
}