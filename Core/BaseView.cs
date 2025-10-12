using UnityEngine;
using System;

namespace Core
{
    /// <summary>
    /// ��� View�� ���̽� Ŭ����
    /// MonoBehaviour�� �����Ͽ� GameObject�� ����
    /// UI �ʱ�ȭ, �̺�Ʈ ����, ���� ���� ���� ���� ����
    /// </summary>
    public abstract class BaseView : MonoBehaviour
    {
        [Header("View State")]
        [SerializeField] protected bool _isViewInitialized = false;

        public bool IsViewInitialized => _isViewInitialized;

        #region Unity �����ֱ�

        /// <summary>
        /// GameObject�� Ȱ��ȭ�� �� ȣ��
        /// </summary>
        protected virtual void OnEnable()
        {
            if (AppInitializer.IsInitialized)
            {
                if (!_isViewInitialized)
                    InitializeView();
            }
            else
            {
                AppInitializer.OnInitializationComplete += OnAppInitialized;
            }
        }

        private void OnAppInitialized()
        {
            AppInitializer.OnInitializationComplete -= OnAppInitialized;
            if (!_isViewInitialized)
                InitializeView();
        }

        /// <summary>
        /// GameObject�� ��Ȱ��ȭ�� �� ȣ��
        /// </summary>
        protected virtual void OnDisable()
        {
            AppInitializer.OnInitializationComplete -= OnAppInitialized;

            if (_isViewInitialized)
                CleanupView();
        }

        #endregion

        #region View �ʱ�ȭ �� ����

        /// <summary>
        /// View �ʱ�ȭ
        /// </summary>
        protected virtual void InitializeView()
        {
            try
            {
                LogInfo("View �ʱ�ȭ ����...");

                // 1. UI ������Ʈ �ʱ�ȭ
                InitializeUIComponents();

                // 2. �̺�Ʈ ����
                SetupViewEvents();

                // 3. ViewModel ���� (����Ŭ�������� ����)
                ConnectToViewModel();

                _isViewInitialized = true;
                LogInfo("View �ʱ�ȭ �Ϸ�");
            }
            catch (Exception ex)
            {
                LogError($"View �ʱ�ȭ ����: {ex.Message}");
                _isViewInitialized = false;
            }
        }

        /// <summary>
        /// View ����
        /// </summary>
        protected virtual void CleanupView()
        {
            try
            {
                LogInfo("View ���� ����...");

                // 1. �̺�Ʈ ���� ����
                DisconnectViewEvents();

                // 2. ViewModel ���� ����
                DisconnectFromViewModel();

                _isViewInitialized = false;
                LogInfo("View ���� �Ϸ�");
            }
            catch (Exception ex)
            {
                LogError($"View ���� ����: {ex.Message}");
            }
        }

        #endregion

        #region ����Ŭ�������� ������ �߻� �޼���

        /// <summary>
        /// UI ������Ʈ �ʱ�ȭ - ����Ŭ�������� �ݵ�� ����
        /// Inspector���� ����� UI ��ҵ��� �ʱ�ȭ
        /// </summary>
        protected abstract void InitializeUIComponents();

        /// <summary>
        /// View �̺�Ʈ ���� - ����Ŭ�������� �ݵ�� ����
        /// Button Ŭ�� ���� UI �̺�Ʈ ������ ���
        /// </summary>
        protected abstract void SetupViewEvents();

        /// <summary>
        /// ViewModel�� ���� - ����Ŭ�������� ����
        /// ViewModel�� ������ ���� �̺�Ʈ�� ����
        /// </summary>
        protected virtual void ConnectToViewModel()
        {
            // ����Ŭ�������� override�Ͽ� ����
        }

        #endregion

        #region ����Ŭ�������� ���� ������ ���� �޼���

        /// <summary>
        /// View �̺�Ʈ ���� ���� - ����Ŭ�������� override ����
        /// </summary>
        protected virtual void DisconnectViewEvents()
        {
            // ����Ŭ�������� override�Ͽ� ����
        }

        /// <summary>
        /// ViewModel ���� ���� - ����Ŭ�������� override ����
        /// </summary>
        protected virtual void DisconnectFromViewModel()
        {
            // ����Ŭ�������� override�Ͽ� ����
        }

        #endregion

        #region Inspector ���� ���� �޼���

        /// <summary>
        /// ���� ������Ʈ ����
        /// </summary>
        protected bool ValidateComponent<T>(T component, string componentName) where T : Component
        {
            if (component == null)
            {
                LogError($"{componentName}�� Inspector���� ������� �ʾҽ��ϴ�!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// ���� ������Ʈ �ϰ� ����
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
        /// Object ���� (GameObject, ScriptableObject ��)
        /// </summary>
        protected bool ValidateObject<T>(T obj, string objectName) where T : UnityEngine.Object
        {
            if (obj == null)
            {
                LogError($"{objectName}�� Inspector���� ������� �ʾҽ��ϴ�!");
                return false;
            }
            return true;
        }

        #endregion

        #region �α� ���� �޼���

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

        #region Unity Editor ����

#if UNITY_EDITOR
        /// <summary>
        /// Inspector���� ���� ����� �� ȣ�� (������ ����)
        /// </summary>
        protected virtual void OnValidate()
        {
            // ����Ŭ�������� override�Ͽ� ����
            // Inspector ���� ������
        }
#endif

        #endregion
    }
}