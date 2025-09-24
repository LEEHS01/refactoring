using UnityEngine;
using UnityEngine.Events;

namespace HNS.Core
{
    /// <summary>
    /// MVVM View의 베이스 클래스
    /// Inspector에서 ViewModel과 UI 컴포넌트를 연결하는 완전 Inspector 기반 구조
    /// </summary>
    public abstract class BaseView : MonoBehaviour
    {
        [Header("ViewModel Connection")]
        [Tooltip("Inspector에서 ViewModel ScriptableObject 에셋을 드래그하여 연결")]
        [SerializeField] protected BaseViewModel _viewModel;

        [Header("View Unity Events")]
        [Space(10)]

        /// <summary>
        /// View 초기화 완료 시 발생 - Inspector에서 연결
        /// </summary>
        public UnityEvent OnViewInitialized = new UnityEvent();

        /// <summary>
        /// View 정리 시 발생 - Inspector에서 연결  
        /// </summary>
        public UnityEvent OnViewCleaned = new UnityEvent();

        /// <summary>
        /// 에러 발생 시 발생 - Inspector에서 연결
        /// </summary>
        public UnityEvent<string> OnViewError = new UnityEvent<string>();

        [Header("Runtime State")]
        [Space(5)]
        [SerializeField, ReadOnly] private bool _isViewInitialized = false;

        /// <summary>
        /// 연결된 ViewModel
        /// </summary>
        public BaseViewModel ViewModel => _viewModel;

        /// <summary>
        /// View 초기화 상태
        /// </summary>
        public bool IsViewInitialized => _isViewInitialized;

        protected virtual void Start()
        {
            InitializeView();
        }

        protected virtual void OnDestroy()
        {
            CleanupView();
        }

        /// <summary>
        /// View 초기화 (Unity Start에서 자동 호출)
        /// </summary>
        protected virtual void InitializeView()
        {
            if (_isViewInitialized)
            {
                LogWarning("View가 이미 초기화되었습니다.");
                return;
            }

            try
            {
                LogInfo("View 초기화 시작...");

                // ViewModel 유효성 검사
                if (_viewModel == null)
                {
                    throw new System.Exception("ViewModel이 Inspector에서 연결되지 않았습니다. ScriptableObject 에셋을 드래그하여 연결하세요.");
                }

                // ViewModel 초기화
                if (!_viewModel.IsInitialized)
                {
                    _viewModel.Initialize();
                }

                // UI 컴포넌트 초기화
                InitializeUIComponents();

                // 이벤트 연결 설정
                SetupViewEvents();

                _isViewInitialized = true;
                OnViewInitialized.Invoke();
                LogInfo($"{GetType().Name} View 초기화 완료");
            }
            catch (System.Exception ex)
            {
                string errorMessage = $"View 초기화 실패: {ex.Message}";
                LogError(errorMessage);
                OnViewError.Invoke(errorMessage);
            }
        }

        /// <summary>
        /// UI 컴포넌트들을 초기화 - 서브클래스에서 구현
        /// Inspector에서 연결된 UI 컴포넌트들의 초기 설정
        /// </summary>
        protected abstract void InitializeUIComponents();

        /// <summary>
        /// View 이벤트 설정 - 서브클래스에서 구현
        /// Button 클릭 등의 UI 이벤트를 ViewModel로 전달하는 연결 설정
        /// </summary>
        protected abstract void SetupViewEvents();

        /// <summary>
        /// View 정리
        /// </summary>
        protected virtual void CleanupView()
        {
            try
            {
                CleanupViewInternal();

                if (_viewModel != null && _viewModel.IsInitialized)
                {
                    _viewModel.Cleanup();
                }

                _isViewInitialized = false;
                OnViewCleaned.Invoke();
                LogInfo($"{GetType().Name} View 정리 완료");
            }
            catch (System.Exception ex)
            {
                LogError($"View 정리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 서브클래스에서 구현할 View 정리 로직
        /// </summary>
        protected virtual void CleanupViewInternal()
        {
            // 서브클래스에서 필요 시 오버라이드
        }

        /// <summary>
        /// Inspector에서 ViewModel 연결 상태 검증
        /// </summary>
        protected virtual void OnValidate()
        {
            if (_viewModel == null)
            {
                LogWarning("ViewModel이 연결되지 않았습니다. Inspector에서 ScriptableObject 에셋을 드래그하여 연결하세요.");
            }
        }

        #region 로깅 헬퍼 메서드

        protected void LogInfo(string message)
        {
            Debug.Log($"[{GetType().Name}] {message}");
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }

        protected void LogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        #endregion

        #region Inspector 헬퍼 메서드

        /// <summary>
        /// Inspector에서 UI 컴포넌트 필드가 올바르게 연결되었는지 검증
        /// </summary>
        protected bool ValidateUIComponent<T>(T component, string componentName) where T : Component
        {
            if (component == null)
            {
                LogError($"{componentName}이 Inspector에서 연결되지 않았습니다.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 여러 UI 컴포넌트들을 한번에 검증
        /// </summary>
        protected bool ValidateUIComponents(params (Component component, string name)[] components)
        {
            bool allValid = true;
            foreach (var (component, name) in components)
            {
                if (!ValidateUIComponent(component, name))
                {
                    allValid = false;
                }
            }
            return allValid;
        }

        #endregion
    }
}