using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 3D 관측소 화면 ViewModel
    /// 관측소 선택 및 3D 화면 표시 상태 관리
    /// </summary>
    public class Area3DViewModel : MonoBehaviour
    {
        #region Singleton
        public static Area3DViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[Area3DViewModel] 인스턴스 생성");
            }
            else
            {
                Destroy(gameObject);
                Debug.LogWarning("[Area3DViewModel] 중복 인스턴스 제거");
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// 관측소 로드 완료 이벤트 (obsId 전달)
        /// </summary>
        [HideInInspector] public UnityEvent<int> OnObservatoryLoaded = new UnityEvent<int>();

        /// <summary>
        /// 3D 화면 닫기 이벤트
        /// </summary>
        [HideInInspector] public UnityEvent OnObservatoryClosed = new UnityEvent();

        /// <summary>
        /// 에러 발생 이벤트
        /// </summary>
        [HideInInspector] public UnityEvent<string> OnError = new UnityEvent<string>();
        #endregion

        #region Private Fields
        private int _currentObsId = -1;
        private bool _isObservatoryActive = false;
        #endregion

        #region Properties
        /// <summary>
        /// 현재 선택된 관측소 ID
        /// </summary>
        public int CurrentObsId => _currentObsId;

        /// <summary>
        /// 3D 관측소 화면 활성화 여부
        /// </summary>
        public bool IsObservatoryActive => _isObservatoryActive;
        #endregion

        #region Public Methods
        /// <summary>
        /// 관측소 3D 화면 로드
        /// </summary>
        /// <param name="obsId">관측소 ID</param>
        public void LoadObservatory(int obsId)
        {
            if (obsId <= 0)
            {
                OnError?.Invoke($"잘못된 관측소 ID: {obsId}");
                return;
            }

            _currentObsId = obsId;
            _isObservatoryActive = true;

            Debug.Log($"[Area3DViewModel] 관측소 로드: ObsId={obsId}");

            // View에 알림
            OnObservatoryLoaded?.Invoke(obsId);
        }

        /// <summary>
        /// 3D 관측소 화면 닫기 (HOME 복귀)
        /// </summary>
        public void CloseObservatory()
        {
            Debug.Log($"[Area3DViewModel] 관측소 닫기: ObsId={_currentObsId}");

            _currentObsId = -1;
            _isObservatoryActive = false;

            OnObservatoryClosed?.Invoke();
        }
        #endregion
    }
}