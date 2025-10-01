using UnityEngine;

namespace Services
{
    /// <summary>
    /// 해상도 서비스
    /// 해상도 정보를 제공하고 해상도 변경을 감지하는 싱글톤 서비스
    /// FHD+ 대응을 위한 스케일 계산 제공
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
                        Debug.LogError("[ResolutionService] 씬에 ResolutionService가 없습니다!");
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

        // 이전 해상도 (변경 감지용)
        private Vector2 _previousResolution;

        #region Properties

        public Vector2 BaseResolution => _baseResolution;
        public Vector2 CurrentResolution => _currentResolution;
        public float WidthScale => _widthScale;
        public float HeightScale => _heightScale;
        public float UniformScale => _uniformScale;
        public float AspectRatio => _aspectRatio;

        #endregion

        #region Unity 생명주기

        private void Awake()
        {
            // 싱글톤 설정
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
            // 해상도 변경 감지
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

        #region 초기화

        private void Initialize()
        {
            Debug.Log("[ResolutionService] 초기화 시작");

            UpdateResolutionInfo();

            Debug.Log($"[ResolutionService] 초기화 완료 - " +
                      $"현재: {_currentResolution}, " +
                      $"배율: {_uniformScale:F2}x");
        }

        #endregion

        #region 해상도 정보 업데이트

        /// <summary>
        /// 현재 해상도 정보 갱신
        /// </summary>
        private void UpdateResolutionInfo()
        {
            _currentResolution = new Vector2(Screen.width, Screen.height);
            _previousResolution = _currentResolution;

            // 가로/세로 각각의 스케일 계산
            _widthScale = _currentResolution.x / _baseResolution.x;
            _heightScale = _currentResolution.y / _baseResolution.y;

            // 균등 스케일 (작은 쪽 기준)
            _uniformScale = Mathf.Min(_widthScale, _heightScale);

            // 종횡비 계산
            _aspectRatio = _currentResolution.x / _currentResolution.y;

            Debug.Log($"[ResolutionService] 해상도 정보 갱신 - " +
                      $"해상도: {_currentResolution}, " +
                      $"Width Scale: {_widthScale:F2}, " +
                      $"Height Scale: {_heightScale:F2}, " +
                      $"Uniform Scale: {_uniformScale:F2}, " +
                      $"Aspect: {_aspectRatio:F2}");
        }

        /// <summary>
        /// 해상도 변경 감지
        /// </summary>
        private void CheckResolutionChange()
        {
            Vector2 currentScreenResolution = new Vector2(Screen.width, Screen.height);

            if (currentScreenResolution != _previousResolution)
            {
                Debug.Log($"[ResolutionService] 해상도 변경 감지 - " +
                          $"{_previousResolution} → {currentScreenResolution}");

                UpdateResolutionInfo();
            }
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 특정 해상도 기준으로 스케일 계산
        /// </summary>
        public float GetScaleForResolution(Vector2 targetResolution)
        {
            float widthScale = targetResolution.x / _baseResolution.x;
            float heightScale = targetResolution.y / _baseResolution.y;
            return Mathf.Min(widthScale, heightScale);
        }

        /// <summary>
        /// 현재 해상도가 기준 해상도보다 큰지 확인
        /// </summary>
        public bool IsHigherThanBase()
        {
            return _currentResolution.x > _baseResolution.x ||
                   _currentResolution.y > _baseResolution.y;
        }

        /// <summary>
        /// 해상도 카테고리 반환 (디버깅용)
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
        /// 기준 해상도 변경 (런타임)
        /// </summary>
        public void SetBaseResolution(Vector2 newBaseResolution)
        {
            _baseResolution = newBaseResolution;
            Debug.Log($"[ResolutionService] 기준 해상도 변경: {_baseResolution}");
            UpdateResolutionInfo();
        }

        /// <summary>
        /// 강제 해상도 정보 갱신
        /// </summary>
        public void ForceUpdate()
        {
            UpdateResolutionInfo();
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 값을 현재 스케일에 맞게 조정
        /// </summary>
        public float ScaleValue(float value)
        {
            return value * _uniformScale;
        }

        /// <summary>
        /// Vector2를 현재 스케일에 맞게 조정
        /// </summary>
        public Vector2 ScaleVector2(Vector2 value)
        {
            return value * _uniformScale;
        }

        /// <summary>
        /// Vector3를 현재 스케일에 맞게 조정
        /// </summary>
        public Vector3 ScaleVector3(Vector3 value)
        {
            return value * _uniformScale;
        }

        #endregion

        #region Inspector 디버깅

#if UNITY_EDITOR
        [ContextMenu("해상도 정보 출력")]
        private void DebugPrintResolutionInfo()
        {
            Debug.Log($"=== 해상도 정보 ===\n" +
                      $"기준 해상도: {_baseResolution}\n" +
                      $"현재 해상도: {_currentResolution}\n" +
                      $"카테고리: {GetResolutionCategory()}\n" +
                      $"Width Scale: {_widthScale:F2}\n" +
                      $"Height Scale: {_heightScale:F2}\n" +
                      $"Uniform Scale: {_uniformScale:F2}\n" +
                      $"Aspect Ratio: {_aspectRatio:F2}");
        }

        [ContextMenu("강제 갱신")]
        private void DebugForceUpdate()
        {
            UpdateResolutionInfo();
            Debug.Log("[ResolutionService] 강제 갱신 완료");
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