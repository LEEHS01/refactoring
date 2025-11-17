using Core;
using HNS.MonitorA.ViewModels;
using UnityEngine;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 3D 관측소 화면 View
    /// Area_3D GameObject에 부착
    /// 원본 프로젝트의 GraphicHider 패턴을 MVVM으로 리팩토링
    /// </summary>
    public class Area3DView : BaseView
    {
        #region Inspector 설정
        [Header("3D Scene Components")]
        [SerializeField] private Camera camera3D;          // 3DCamera
        [SerializeField] private GameObject observatory;   // Observatory GameObject
        [SerializeField] private GameObject terrain;       // Terrain GameObject (옵션)

        [Header("Canvas Control")]
        [SerializeField] private CanvasGroup canvasGroup;  // 화면 표시 제어 (자동 추가)

        [Header("Navigation")]
        [SerializeField] private MapAreaView mapAreaView;  // 지도 화면 (전환 시 필요)
        #endregion

        #region Private Fields
        private int _currentObsId = -1;
        #endregion

        #region BaseView 구현
        protected override void InitializeUIComponents()
        {
            // Inspector 연결 검증 - Component만 ValidateComponents 사용
            bool isValid = ValidateComponents(
                (camera3D, "camera3D")
            );

            if (!isValid)
            {
                LogError("필수 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            // GameObject는 수동 null 체크
            if (observatory == null)
            {
                LogError("observatory가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            // terrain은 옵션
            if (terrain == null)
            {
                LogInfo("Terrain이 연결되지 않음 (옵션)");
            }

            // CanvasGroup 자동 추가 또는 가져오기
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    LogInfo("CanvasGroup 자동 추가");
                }
            }

            LogInfo("컴포넌트 초기화 완료");

            // 초기 상태: 보이지 않게 (GameObject는 활성화 유지!)
            Hide3DScene();
        }

        protected override void SetupViewEvents()
        {
            // 이 View는 별도 UI 이벤트가 없음
            LogInfo("View 이벤트 설정 완료 (없음)");
        }

        protected override void ConnectToViewModel()
        {
            if (Area3DViewModel.Instance == null)
            {
                LogError("Area3DViewModel.Instance가 null입니다!");
                return;
            }

            // ViewModel 이벤트 구독
            Area3DViewModel.Instance.OnObservatoryLoaded.AddListener(OnObservatoryLoaded);
            Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
            Area3DViewModel.Instance.OnError.AddListener(OnError);

            LogInfo("ViewModel 이벤트 구독 완료");
        }

        protected override void DisconnectFromViewModel()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.RemoveListener(OnObservatoryLoaded);
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
        /// <summary>
        /// 관측소 로드 완료 - 3D 화면 표시
        /// </summary>
        private void OnObservatoryLoaded(int obsId)
        {
            _currentObsId = obsId;

            LogInfo($"3D 관측소 표시: ObsId={obsId}");

            // 🎯 3D 화면 표시 (CanvasGroup 사용)
            Show3DScene();

            LogInfo("3D 화면 활성화 완료");
        }

        /// <summary>
        /// 관측소 닫기 - 지도 화면 복귀
        /// </summary>
        private void OnObservatoryClosed()
        {
            LogInfo("3D 관측소 숨김");

            // 🎯 3D 화면 숨김 (CanvasGroup 사용)
            Hide3DScene();

            // 지도 화면 다시 표시
            if (mapAreaView != null)
            {
                // MapArea 복원 (CanvasGroup 사용)
                mapAreaView.RestoreMapArea();
                LogInfo("MapArea 화면 복귀");
            }
            else
            {
                LogWarning("MapAreaView가 연결되지 않아 복귀할 수 없습니다!");
            }
        }

        /// <summary>
        /// 에러 처리
        /// </summary>
        private void OnError(string errorMessage)
        {
            LogError($"ViewModel 에러: {errorMessage}");
        }
        #endregion

        #region 공개 메서드 (다른 View에서 호출 가능)
        /// <summary>
        /// 3D 관측소 화면 표시
        /// </summary>
        /// <param name="obsId">관측소 ID</param>
        public void ShowObservatory(int obsId)
        {
            LogInfo($"ShowObservatory 호출: ObsId={obsId}");

            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.LoadObservatory(obsId);
            }
            else
            {
                LogError("Area3DViewModel.Instance가 null입니다!");
            }
        }

        /// <summary>
        /// 3D 관측소 화면 숨김
        /// </summary>
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
        /// <summary>
        /// 3D 화면 표시 (CanvasGroup 사용)
        /// </summary>
        private void Show3DScene()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                LogInfo("3D 화면 표시");
            }

            // 3D 컴포넌트 활성화
            if (camera3D != null)
                camera3D.gameObject.SetActive(true);

            if (observatory != null)
                observatory.SetActive(true);

            if (terrain != null)
                terrain.SetActive(true);
        }

        /// <summary>
        /// 3D 화면 숨김 (CanvasGroup 사용)
        /// </summary>
        private void Hide3DScene()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                LogInfo("3D 화면 숨김");
            }

            // 3D 컴포넌트 비활성화
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