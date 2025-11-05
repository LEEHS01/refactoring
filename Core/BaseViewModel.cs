using System;
using UnityEngine;


namespace Core
{
    /// <summary>
    /// 모든 ViewModel의 베이스 클래스
    /// ScriptableObject로 구현하여 Inspector에서 관리 가능
    /// 공통 로직(로그, 상태 관리)을 제공
    /// </summary>
    public abstract class BaseViewModel : ScriptableObject, IViewModel
    {
        [Header("ViewModel State")]
        [SerializeField] protected bool _isInitialized = false;
        [SerializeField] protected bool _isLoading = false;

        #region IViewModel 구현

        public bool IsInitialized => _isInitialized;
        public bool IsLoading => _isLoading;

        /// <summary>
        /// ViewModel 초기화 (가상 메서드)
        /// 서브클래스에서 override 가능
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized)
            {
                LogWarning("이미 초기화되었습니다.");
                return;
            }

            try
            {
                LogInfo("초기화 시작...");

                // 서브클래스의 초기화 로직 호출
                OnInitialize();

                _isInitialized = true;
                LogInfo("초기화 완료");
            }
            catch (Exception ex)
            {
                LogError($"초기화 실패: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// ViewModel 정리 (가상 메서드)
        /// 서브클래스에서 override 가능
        /// </summary>
        public virtual void Cleanup()
        {
            if (!_isInitialized)
            {
                LogWarning("초기화되지 않은 상태에서 Cleanup 호출됨");
                return;
            }

            try
            {
                LogInfo("정리 시작...");

                // 서브클래스의 정리 로직 호출
                OnCleanup();

                _isInitialized = false;
                _isLoading = false;
                LogInfo("정리 완료");
            }
            catch (Exception ex)
            {
                LogError($"정리 실패: {ex.Message}");
            }
        }

        #endregion

        #region 서브클래스에서 구현할 메서드

        /// <summary>
        /// 서브클래스에서 구현할 초기화 로직
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 서브클래스에서 override하여 구현
        }

        /// <summary>
        /// 서브클래스에서 구현할 정리 로직
        /// </summary>
        protected virtual void OnCleanup()
        {
            // 서브클래스에서 override하여 구현
        }

        #endregion

        #region 로딩 상태 관리

        /// <summary>
        /// 로딩 시작
        /// </summary>
        protected void StartLoading()
        {
            _isLoading = true;
            LogInfo("로딩 시작");
        }

        /// <summary>
        /// 로딩 종료
        /// </summary>
        protected void StopLoading()
        {
            _isLoading = false;
            LogInfo("로딩 완료");
        }

        #endregion

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

        #region Unity 생명주기

        /// <summary>
        /// ScriptableObject가 생성될 때 호출
        /// </summary>
        protected virtual void OnEnable()
        {
            // ScriptableObject가 로드될 때 실행
        }

        /// <summary>
        /// ScriptableObject가 파괴될 때 호출
        /// </summary>
        protected virtual void OnDisable()
        {
            // ScriptableObject가 언로드될 때 실행
        }

        #endregion
    }
}