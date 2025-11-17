using UnityEngine;
using System;

namespace Core
{
    /// <summary>
    /// 모든 View의 베이스 클래스
    /// MonoBehaviour로 구현하여 GameObject에 부착
    /// UI 초기화, 이벤트 연결, 검증 등의 공통 로직 제공
    /// 
    /// ⭐ 최종 완벽 버전: 모든 타이밍 문제 해결
    /// </summary>
    public abstract class BaseView : MonoBehaviour
    {
        [Header("View State")]
        [SerializeField] protected bool _isViewInitialized = false;

        public bool IsViewInitialized => _isViewInitialized;

        #region Unity 생명주기

        /// <summary>
        /// Start에서 초기화 (GameObject가 처음부터 활성화된 경우)
        /// </summary>
        protected virtual void Start()
        {
            // 이미 초기화되었으면 스킵
            if (_isViewInitialized)
                return;

            TryInitialize();
        }

        /// <summary>
        /// GameObject가 활성화될 때 호출
        /// ⭐ 핵심: 항상 초기화 시도
        /// </summary>
        protected virtual void OnEnable()
        {
            // 초기화 시도 (아직 안 되었으면 초기화, 이미 되었으면 이벤트 재구독)
            if (!_isViewInitialized)
            {
                TryInitialize();
            }
            else
            {
                ReconnectEvents();
            }
        }

        /// <summary>
        /// GameObject가 비활성화될 때 호출
        /// </summary>
        protected virtual void OnDisable()
        {
            // 초기화가 완료된 경우에만 이벤트 해제
            if (_isViewInitialized)
            {
                DisconnectEvents();
            }
        }

        /// <summary>
        /// GameObject가 파괴될 때 호출
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_isViewInitialized)
            {
                CleanupView();
            }
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 초기화 시도
        /// </summary>
        private void TryInitialize()
        {
            // AppInitializer가 준비되었으면 바로 초기화
            if (AppInitializer.IsInitialized)
            {
                InitializeView();
            }
            else
            {
                // AppInitializer 완료 대기
                AppInitializer.OnInitializationComplete += OnAppInitialized;
            }
        }

        private void OnAppInitialized()
        {
            AppInitializer.OnInitializationComplete -= OnAppInitialized;
            InitializeView();
        }

        /// <summary>
        /// View 초기화 (1회만)
        /// </summary>
        protected virtual void InitializeView()
        {
            try
            {
                LogInfo("View 초기화 시작...");

                // 1. UI 컴포넌트 초기화
                InitializeUIComponents();

                _isViewInitialized = true;
                LogInfo("View 초기화 완료");

                // 2. 이벤트 연결
                ReconnectEvents();
            }
            catch (Exception ex)
            {
                LogError($"View 초기화 실패: {ex.Message}\n{ex.StackTrace}");
                _isViewInitialized = false;
            }
        }

        /// <summary>
        /// 이벤트 재연결 (OnEnable에서 호출)
        /// </summary>
        protected virtual void ReconnectEvents()
        {
            try
            {
                LogInfo("이벤트 재연결 시작...");

                // 1. View 이벤트 연결
                SetupViewEvents();

                // 2. ViewModel 연결
                ConnectToViewModel();

                LogInfo("이벤트 재연결 완료");
            }
            catch (Exception ex)
            {
                LogError($"이벤트 재연결 실패: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 이벤트 연결 해제 (OnDisable에서 호출)
        /// </summary>
        protected virtual void DisconnectEvents()
        {
            try
            {
                LogInfo("이벤트 해제 시작...");

                // 1. View 이벤트 연결 해제
                DisconnectViewEvents();

                // 2. ViewModel 연결 해제
                DisconnectFromViewModel();

                LogInfo("이벤트 해제 완료");
            }
            catch (Exception ex)
            {
                LogError($"이벤트 해제 실패: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// View 정리 (OnDestroy에서 호출)
        /// </summary>
        protected virtual void CleanupView()
        {
            try
            {
                LogInfo("View 정리 시작...");

                // AppInitializer 이벤트 해제
                AppInitializer.OnInitializationComplete -= OnAppInitialized;

                _isViewInitialized = false;
                LogInfo("View 정리 완료");
            }
            catch (Exception ex)
            {
                LogError($"View 정리 실패: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region 서브클래스에서 구현할 추상 메서드

        /// <summary>
        /// UI 컴포넌트 초기화 - 서브클래스에서 반드시 구현
        /// Inspector에서 연결된 UI 요소들을 초기화
        /// </summary>
        protected abstract void InitializeUIComponents();

        /// <summary>
        /// View 이벤트 설정 - 서브클래스에서 반드시 구현
        /// Button 클릭 등의 UI 이벤트 리스너 등록
        /// </summary>
        protected abstract void SetupViewEvents();

        /// <summary>
        /// ViewModel과 연결 - 서브클래스에서 구현
        /// ViewModel의 데이터 변경 이벤트를 구독
        /// </summary>
        protected virtual void ConnectToViewModel()
        {
            // 서브클래스에서 override하여 구현
        }

        #endregion

        #region 서브클래스에서 구현 가능한 가상 메서드

        /// <summary>
        /// View 이벤트 연결 해제 - 서브클래스에서 override 가능
        /// </summary>
        protected virtual void DisconnectViewEvents()
        {
            // 서브클래스에서 override하여 구현
        }

        /// <summary>
        /// ViewModel 연결 해제 - 서브클래스에서 override 가능
        /// </summary>
        protected virtual void DisconnectFromViewModel()
        {
            // 서브클래스에서 override하여 구현
        }

        #endregion

        #region Inspector 검증 헬퍼 메서드

        /// <summary>
        /// 단일 컴포넌트 검증
        /// </summary>
        protected bool ValidateComponent<T>(T component, string componentName) where T : Component
        {
            if (component == null)
            {
                LogError($"{componentName}이 Inspector에서 연결되지 않았습니다!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 여러 컴포넌트 일괄 검증
        /// </summary>
        protected bool ValidateComponents(params (Component component, string name)[] components)
        {
            bool allValid = true;

            foreach (var (component, name) in components)
            {
                if (!ValidateComponent(component, name))
                {
                    allValid = false;
                }
            }

            return allValid;
        }

        /// <summary>
        /// Object 검증 (GameObject, ScriptableObject 등)
        /// </summary>
        protected bool ValidateObject<T>(T obj, string objectName) where T : UnityEngine.Object
        {
            if (obj == null)
            {
                LogError($"{objectName}이 Inspector에서 연결되지 않았습니다!");
                return false;
            }
            return true;
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

        #region Unity Editor 전용

#if UNITY_EDITOR
        /// <summary>
        /// Inspector에서 값이 변경될 때 호출 (에디터 전용)
        /// </summary>
        protected virtual void OnValidate()
        {
            // 서브클래스에서 override하여 구현
            // Inspector 설정 검증용
        }
#endif

        #endregion
    }
}