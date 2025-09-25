using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using HNS.Core;

namespace HNS.Services
{
    /// <summary>
    /// 스케줄러 서비스 - 순수 스케줄링 전담
    /// </summary>
    public class SchedulerService : MonoBehaviour, ISchedulerService
    {
        [Header("Scheduler Configuration")]
        [SerializeField] private SchedulerConfig _config;

        [Header("Service Dependencies")]
        [SerializeField] private DataService _dataService;

        [Header("Runtime Status")]
        [SerializeField, ReadOnly] private bool _isInitialized = false;
        [SerializeField, ReadOnly] private bool _isRealtimeRunning = false;
        [SerializeField, ReadOnly] private bool _isDataSyncRunning = false;
        [SerializeField, ReadOnly] private bool _isLoading = false;

        // 코루틴 참조
        private Coroutine _realtimeCoroutine;
        private Coroutine _dataSyncCoroutine;

        // 이벤트
        public event Action OnRealtimeCheckTriggered;
        public event Action OnDataSyncTriggered;

        /// <summary>
        /// 초기화 상태
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 로딩 상태
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// 실시간 체크 실행 상태
        /// </summary>
        public bool IsRealtimeRunning => _isRealtimeRunning;

        /// <summary>
        /// 데이터 동기화 실행 상태
        /// </summary>
        public bool IsDataSyncRunning => _isDataSyncRunning;

        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[SchedulerService] 이미 초기화되었습니다.");
                return true;
            }

            try
            {
                Debug.Log("[SchedulerService] 초기화 시작...");
                _isLoading = true;

                // TODO: 설정 유효성 검사
                // TODO: 의존성 서비스 확인 및 대기

                _isInitialized = true;
                Debug.Log("[SchedulerService] 초기화 완료");

                // TODO: 자동 시작 설정 확인 후 스케줄러 시작

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SchedulerService] 초기화 실패: {ex.Message}");
                return false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 실시간 체크 시작
        /// </summary>
        public void StartRealtimeCheck()
        {
            if (!_isInitialized || _isRealtimeRunning) return;

            Debug.Log($"[SchedulerService] 실시간 체크 시작 - {_config.RealtimeInterval}초 주기");
            _realtimeCoroutine = StartCoroutine(RealtimeCheckLoop());
            _isRealtimeRunning = true;
        }

        /// <summary>
        /// 데이터 동기화 시작
        /// </summary>
        public void StartDataSync()
        {
            if (!_isInitialized || _isDataSyncRunning) return;

            Debug.Log($"[SchedulerService] 데이터 동기화 시작 - {_config.DataSyncInterval}분 주기");
            _dataSyncCoroutine = StartCoroutine(DataSyncLoop());
            _isDataSyncRunning = true;
        }

        /// <summary>
        /// 모든 스케줄러 정지
        /// </summary>
        public void StopAll()
        {
            StopRealtimeCheck();
            StopDataSync();
            Debug.Log("[SchedulerService] 모든 스케줄러 정지");
        }

        /// <summary>
        /// 실시간 체크 정지
        /// </summary>
        public void StopRealtimeCheck()
        {
            if (_realtimeCoroutine != null)
            {
                StopCoroutine(_realtimeCoroutine);
                _realtimeCoroutine = null;
                _isRealtimeRunning = false;
            }
        }

        /// <summary>
        /// 데이터 동기화 정지
        /// </summary>
        public void StopDataSync()
        {
            if (_dataSyncCoroutine != null)
            {
                StopCoroutine(_dataSyncCoroutine);
                _dataSyncCoroutine = null;
                _isDataSyncRunning = false;
            }
        }

        /// <summary>
        /// 실시간 체크 루프
        /// </summary>
        private IEnumerator RealtimeCheckLoop()
        {
            while (_isRealtimeRunning)
            {
                try
                {
                    Debug.Log($"[SchedulerService] 실시간 체크 트리거 - {DateTime.Now:HH:mm:ss}");

                    // TODO: DataService에게 실시간 체크 요청
                    OnRealtimeCheckTriggered?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SchedulerService] 실시간 체크 오류: {ex.Message}");
                }

                yield return new WaitForSeconds(_config.RealtimeInterval);
            }
        }

        /// <summary>
        /// 데이터 동기화 루프
        /// </summary>
        private IEnumerator DataSyncLoop()
        {
            while (_isDataSyncRunning)
            {
                try
                {
                    Debug.Log($"[SchedulerService] 데이터 동기화 트리거 - {DateTime.Now:HH:mm:ss}");

                    // TODO: DataService에게 데이터 동기화 요청
                    OnDataSyncTriggered?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SchedulerService] 데이터 동기화 오류: {ex.Message}");
                }

                float waitSeconds = _config.DataSyncInterval * 60f;
                yield return new WaitForSeconds(waitSeconds);
            }
        }

        /// <summary>
        /// 서비스 정리
        /// </summary>
        public void Cleanup()
        {
            StopAll();
            _isInitialized = false;
            _isLoading = false;
            Debug.Log("[SchedulerService] 서비스 정리 완료");
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnValidate()
        {
            if (_config == null)
            {
                Debug.LogWarning("[SchedulerService] SchedulerConfig가 설정되지 않았습니다.");
            }
        }
    }

    /// <summary>
    /// 스케줄러 설정 클래스
    /// </summary>
    [System.Serializable]
    public class SchedulerConfig
    {
        [Header("실시간 체크 설정")]
        [Range(1f, 5f)]
        public float RealtimeInterval = 2f;

        [Header("데이터 동기화 설정")]
        [Range(1f, 60f)]
        public float DataSyncInterval = 10f;

        [Header("자동 시작")]
        public bool AutoStartRealtimeCheck = true;
        public bool AutoStartDataSync = true;
    }
}