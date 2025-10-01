using System;
using UnityEngine;
using static UnityEditor.Profiling.HierarchyFrameDataView;

namespace Core
{
    /// <summary>
    /// ��� ViewModel�� ���̽� Ŭ����
    /// ScriptableObject�� �����Ͽ� Inspector���� ���� ����
    /// ���� ����(�α�, ���� ����)�� ����
    /// </summary>
    public abstract class BaseViewModel : ScriptableObject, IViewModel
    {
        [Header("ViewModel State")]
        [SerializeField] protected bool _isInitialized = false;
        [SerializeField] protected bool _isLoading = false;

        #region IViewModel ����

        public bool IsInitialized => _isInitialized;
        public bool IsLoading => _isLoading;

        /// <summary>
        /// ViewModel �ʱ�ȭ (���� �޼���)
        /// ����Ŭ�������� override ����
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized)
            {
                LogWarning("�̹� �ʱ�ȭ�Ǿ����ϴ�.");
                return;
            }

            try
            {
                LogInfo("�ʱ�ȭ ����...");

                // ����Ŭ������ �ʱ�ȭ ���� ȣ��
                OnInitialize();

                _isInitialized = true;
                LogInfo("�ʱ�ȭ �Ϸ�");
            }
            catch (Exception ex)
            {
                LogError($"�ʱ�ȭ ����: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// ViewModel ���� (���� �޼���)
        /// ����Ŭ�������� override ����
        /// </summary>
        public virtual void Cleanup()
        {
            if (!_isInitialized)
            {
                LogWarning("�ʱ�ȭ���� ���� ���¿��� Cleanup ȣ���");
                return;
            }

            try
            {
                LogInfo("���� ����...");

                // ����Ŭ������ ���� ���� ȣ��
                OnCleanup();

                _isInitialized = false;
                _isLoading = false;
                LogInfo("���� �Ϸ�");
            }
            catch (Exception ex)
            {
                LogError($"���� ����: {ex.Message}");
            }
        }

        #endregion

        #region ����Ŭ�������� ������ �޼���

        /// <summary>
        /// ����Ŭ�������� ������ �ʱ�ȭ ����
        /// </summary>
        protected virtual void OnInitialize()
        {
            // ����Ŭ�������� override�Ͽ� ����
        }

        /// <summary>
        /// ����Ŭ�������� ������ ���� ����
        /// </summary>
        protected virtual void OnCleanup()
        {
            // ����Ŭ�������� override�Ͽ� ����
        }

        #endregion

        #region �ε� ���� ����

        /// <summary>
        /// �ε� ����
        /// </summary>
        protected void StartLoading()
        {
            _isLoading = true;
            LogInfo("�ε� ����");
        }

        /// <summary>
        /// �ε� ����
        /// </summary>
        protected void StopLoading()
        {
            _isLoading = false;
            LogInfo("�ε� �Ϸ�");
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

        #region Unity �����ֱ�

        /// <summary>
        /// ScriptableObject�� ������ �� ȣ��
        /// </summary>
        protected virtual void OnEnable()
        {
            // ScriptableObject�� �ε�� �� ����
        }

        /// <summary>
        /// ScriptableObject�� �ı��� �� ȣ��
        /// </summary>
        protected virtual void OnDisable()
        {
            // ScriptableObject�� ��ε�� �� ����
        }

        #endregion
    }
}