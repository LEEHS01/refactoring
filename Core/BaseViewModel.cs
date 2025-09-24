using UnityEngine;
using UnityEngine.Events;

namespace HNS.Core
{
    /// <summary>
    /// MVVM ViewModel의 베이스 클래스
    /// ScriptableObject로 에셋화 가능하며 Inspector에서 UnityEvent 연결을 지원
    /// </summary>
    public abstract class BaseViewModel : ScriptableObject, IViewModel
    {
        [Header("Inspector Unity Events")]
        [Space(10)]

        /// <summary>
        /// ViewModel 초기화 완료 시 발생하는 이벤트 - Inspector에서 연결
        /// </summary>
        public UnityEvent OnInitialized = new UnityEvent();

        /// <summary>
        /// 에러 발생 시 Inspector에서 연결할 이벤트
        /// </summary>
        public UnityEvent<string> OnError = new UnityEvent<string>();

        /// <summary>
        /// 로딩 상태 변경 시 Inspector에서 연결할 이벤트
        /// </summary>
        public UnityEvent<bool> OnLoadingStateChanged = new UnityEvent<bool>();

        [Header("Runtime State")]
        [Space(5)]
        [SerializeField, ReadOnly] private bool _isInitialized = false;
        [SerializeField, ReadOnly] private bool _isLoading = false;

        /// <summary>
        /// ViewModel 초기화 상태
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 로딩 상태
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            protected set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnLoadingStateChanged.Invoke(value);
                }
            }
        }

        /// <summary>
        /// ViewModel 초기화
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized)
            {
                LogWarning("ViewModel이 이미 초기화되었습니다.");
                return;
            }

            try
            {
                LogInfo("ViewModel 초기화 시작...");
                InitializeInternal();
                _isInitialized = true;
                OnInitialized.Invoke();
                LogInfo($"{GetType().Name} ViewModel 초기화 완료");
            }
            catch (System.Exception ex)
            {
                LogError($"ViewModel 초기화 실패: {ex.Message}");
                OnError.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// 서브클래스에서 구현할 초기화 로직
        /// </summary>
        protected abstract void InitializeInternal();

        /// <summary>
        /// ViewModel 정리
        /// </summary>
        public virtual void Cleanup()
        {
            try
            {
                CleanupInternal();
                _isInitialized = false;
                LogInfo($"{GetType().Name} ViewModel 정리 완료");
            }
            catch (System.Exception ex)
            {
                LogError($"ViewModel 정리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 서브클래스에서 구현할 정리 로직
        /// </summary>
        protected virtual void CleanupInternal()
        {
            // 서브클래스에서 필요 시 오버라이드
        }

        /// <summary>
        /// 에러 처리
        /// </summary>
        protected void HandleError(System.Exception exception)
        {
            string errorMessage = $"{GetType().Name}: {exception.Message}";
            LogError(errorMessage);
            OnError.Invoke(errorMessage);
        }

        /// <summary>
        /// 로딩 상태 시작
        /// </summary>
        protected void StartLoading()
        {
            IsLoading = true;
            LogInfo("로딩 시작");
        }

        /// <summary>
        /// 로딩 상태 종료
        /// </summary>
        protected void StopLoading()
        {
            IsLoading = false;
            LogInfo("로딩 종료");
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

        /// <summary>
        /// Unity Editor에서 ViewModel 에셋 검증용
        /// </summary>
        protected virtual void OnValidate()
        {
            // Inspector에서 값 변경 시 검증 로직 (필요 시 오버라이드)
        }
    }

    /// <summary>
    /// ReadOnly attribute for Inspector display
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
        // Unity Inspector에서 읽기 전용으로 표시하기 위한 어트리뷰트
    }
}