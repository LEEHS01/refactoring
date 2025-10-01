using UnityEngine;

namespace Services
{
    /// <summary>
    /// �ػ� ����
    /// �ػ� ������ �����ϰ� �ػ� ������ �����ϴ� �̱��� ����
    /// FHD+ ������ ���� ������ ��� ����
    /// </summary>
    public class ResolutionService : MonoBehaviour
    {
        #region Singleton

        private static ResolutionService _instance;
        public static ResolutionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResolutionService>();

                    if (_instance == null)
                    {
                        Debug.LogError("[ResolutionService] ���� ResolutionService�� �����ϴ�!");
                    }
                }
                return _instance;
            }
        }

        #endregion

        [Header("Base Resolution")]
        [SerializeField] private Vector2 _baseResolution = new Vector2(1920, 1080);

        [Header("Runtime Status")]
        [SerializeField] private Vector2 _currentResolution;
        [SerializeField] private float _widthScale = 1f;
        [SerializeField] private float _heightScale = 1f;
        [SerializeField] private float _uniformScale = 1f;
        [SerializeField] private float _aspectRatio = 16f / 9f;

        // ���� �ػ� (���� ������)
        private Vector2 _previousResolution;

        #region Properties

        public Vector2 BaseResolution => _baseResolution;
        public Vector2 CurrentResolution => _currentResolution;
        public float WidthScale => _widthScale;
        public float HeightScale => _heightScale;
        public float UniformScale => _uniformScale;
        public float AspectRatio => _aspectRatio;

        #endregion

        #region Unity �����ֱ�

        private void Awake()
        {
            // �̱��� ����
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Update()
        {
            // �ػ� ���� ����
            CheckResolutionChange();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region �ʱ�ȭ

        private void Initialize()
        {
            Debug.Log("[ResolutionService] �ʱ�ȭ ����");

            UpdateResolutionInfo();

            Debug.Log($"[ResolutionService] �ʱ�ȭ �Ϸ� - " +
                      $"����: {_currentResolution}, " +
                      $"����: {_uniformScale:F2}x");
        }

        #endregion

        #region �ػ� ���� ������Ʈ

        /// <summary>
        /// ���� �ػ� ���� ����
        /// </summary>
        private void UpdateResolutionInfo()
        {
            _currentResolution = new Vector2(Screen.width, Screen.height);
            _previousResolution = _currentResolution;

            // ����/���� ������ ������ ���
            _widthScale = _currentResolution.x / _baseResolution.x;
            _heightScale = _currentResolution.y / _baseResolution.y;

            // �յ� ������ (���� �� ����)
            _uniformScale = Mathf.Min(_widthScale, _heightScale);

            // ��Ⱦ�� ���
            _aspectRatio = _currentResolution.x / _currentResolution.y;

            Debug.Log($"[ResolutionService] �ػ� ���� ���� - " +
                      $"�ػ�: {_currentResolution}, " +
                      $"Width Scale: {_widthScale:F2}, " +
                      $"Height Scale: {_heightScale:F2}, " +
                      $"Uniform Scale: {_uniformScale:F2}, " +
                      $"Aspect: {_aspectRatio:F2}");
        }

        /// <summary>
        /// �ػ� ���� ����
        /// </summary>
        private void CheckResolutionChange()
        {
            Vector2 currentScreenResolution = new Vector2(Screen.width, Screen.height);

            if (currentScreenResolution != _previousResolution)
            {
                Debug.Log($"[ResolutionService] �ػ� ���� ���� - " +
                          $"{_previousResolution} �� {currentScreenResolution}");

                UpdateResolutionInfo();
            }
        }

        #endregion

        #region ���� �޼���

        /// <summary>
        /// Ư�� �ػ� �������� ������ ���
        /// </summary>
        public float GetScaleForResolution(Vector2 targetResolution)
        {
            float widthScale = targetResolution.x / _baseResolution.x;
            float heightScale = targetResolution.y / _baseResolution.y;
            return Mathf.Min(widthScale, heightScale);
        }

        /// <summary>
        /// ���� �ػ󵵰� ���� �ػ󵵺��� ū�� Ȯ��
        /// </summary>
        public bool IsHigherThanBase()
        {
            return _currentResolution.x > _baseResolution.x ||
                   _currentResolution.y > _baseResolution.y;
        }

        /// <summary>
        /// �ػ� ī�װ� ��ȯ (������)
        /// </summary>
        public string GetResolutionCategory()
        {
            int width = (int)_currentResolution.x;
            int height = (int)_currentResolution.y;

            if (width >= 3840 && height >= 2160)
                return "UHD (4K)";
            else if (width >= 2560 && height >= 1440)
                return "QHD (2K)";
            else if (width >= 1920 && height >= 1080)
                return "FHD";
            else if (width >= 1280 && height >= 720)
                return "HD";
            else
                return "SD";
        }

        /// <summary>
        /// ���� �ػ� ���� (��Ÿ��)
        /// </summary>
        public void SetBaseResolution(Vector2 newBaseResolution)
        {
            _baseResolution = newBaseResolution;
            Debug.Log($"[ResolutionService] ���� �ػ� ����: {_baseResolution}");
            UpdateResolutionInfo();
        }

        /// <summary>
        /// ���� �ػ� ���� ����
        /// </summary>
        public void ForceUpdate()
        {
            UpdateResolutionInfo();
        }

        #endregion

        #region ��ƿ��Ƽ �޼���

        /// <summary>
        /// ���� ���� �����Ͽ� �°� ����
        /// </summary>
        public float ScaleValue(float value)
        {
            return value * _uniformScale;
        }

        /// <summary>
        /// Vector2�� ���� �����Ͽ� �°� ����
        /// </summary>
        public Vector2 ScaleVector2(Vector2 value)
        {
            return value * _uniformScale;
        }

        /// <summary>
        /// Vector3�� ���� �����Ͽ� �°� ����
        /// </summary>
        public Vector3 ScaleVector3(Vector3 value)
        {
            return value * _uniformScale;
        }

        #endregion

        #region Inspector �����

#if UNITY_EDITOR
        [ContextMenu("�ػ� ���� ���")]
        private void DebugPrintResolutionInfo()
        {
            Debug.Log($"=== �ػ� ���� ===\n" +
                      $"���� �ػ�: {_baseResolution}\n" +
                      $"���� �ػ�: {_currentResolution}\n" +
                      $"ī�װ�: {GetResolutionCategory()}\n" +
                      $"Width Scale: {_widthScale:F2}\n" +
                      $"Height Scale: {_heightScale:F2}\n" +
                      $"Uniform Scale: {_uniformScale:F2}\n" +
                      $"Aspect Ratio: {_aspectRatio:F2}");
        }

        [ContextMenu("���� ����")]
        private void DebugForceUpdate()
        {
            UpdateResolutionInfo();
            Debug.Log("[ResolutionService] ���� ���� �Ϸ�");
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateResolutionInfo();
            }
        }
#endif

        #endregion
    }
}