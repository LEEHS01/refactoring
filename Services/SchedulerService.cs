using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.Services
{
    /// <summary>
    /// 스케줄러 서비스 - 순수 스케줄링 전담
    /// Inspector 필드를 코드 연결로 변경
    /// </summary>
    public class SchedulerService : MonoBehaviour
    {
        [Header("Scheduler Configuration")]
        [Range(1f, 5f)]
        [SerializeField] private float _realtimeInterval = 2f;

        [Range(1f, 60f)]
        [SerializeField] private float _dataSyncInterval = 10f;

        [SerializeField] private bool _autoStartRealtimeCheck = true;
        [SerializeField] private bool _autoStartDataSync = true;

        [Header("Runtime Status")]
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private bool _isRealtimeRunning = false;
        [SerializeField] private bool _isDataSyncRunning = false;
        [SerializeField] private bool _isLoading = false;

        [Header("Unity Events - Inspector에서 DataService와 연결")]
        public UnityEvent OnRealtimeCheckTriggered = new UnityEvent();
        public UnityEvent OnDataSyncTriggered = new UnityEvent();

        // 코드 기반 의존성 - Inspector 필드 대신
        private DataService _dataService;

        // 코루틴 참조
        private Coroutine _realtimeCoroutine;
        private Coroutine _dataSyncCoroutine;

        // Properties
        public bool IsInitialized => _isInitialized;
        public bool IsLoading => _isLoading;
        public bool IsRealtimeRunning => _isRealtimeRunning;
        public bool IsDataSyncRunning => _isDataSyncRunning;

        private void Awake()
        {
            // 코드 기반 의존성 해결 - Inspector 대신
            _dataService = FindObjectOfType<DataService>();
            if (_dataService == null)
                Debug.LogError("[SchedulerService] DataService를 찾을 수 없습니다.");
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

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

                // 자동 시작 설정
                if (_autoStartRealtimeCheck)
                    StartRealtimeCheck();

                if (_autoStartDataSync)
                    StartDataSync();

                _isInitialized = true;
                Debug.Log("[SchedulerService] 초기화 완료");
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

        public void StartRealtimeCheck()
        {
            if (_isRealtimeRunning)
            {
                Debug.LogWarning("[SchedulerService] 실시간 체크가 이미 실행 중입니다.");
                return;
            }

            Debug.Log($"[SchedulerService] 실시간 체크 시작 - {_realtimeInterval}초 주기");
            _realtimeCoroutine = StartCoroutine(RealtimeCheckLoop());
            _isRealtimeRunning = true;
        }

        public void StartDataSync()
        {
            if (_isDataSyncRunning)
            {
                Debug.LogWarning("[SchedulerService] 데이터 동기화가 이미 실행 중입니다.");
                return;
            }

            Debug.Log($"[SchedulerService] 데이터 동기화 시작 - {_dataSyncInterval}분 주기");
            _dataSyncCoroutine = StartCoroutine(DataSyncLoop());
            _isDataSyncRunning = true;
        }

        public void StopRealtimeCheck()
        {
            if (_realtimeCoroutine != null)
            {
                StopCoroutine(_realtimeCoroutine);
                _realtimeCoroutine = null;
            }
            _isRealtimeRunning = false;
            Debug.Log("[SchedulerService] 실시간 체크 정지");
        }

        public void StopDataSync()
        {
            if (_dataSyncCoroutine != null)
            {
                StopCoroutine(_dataSyncCoroutine);
                _dataSyncCoroutine = null;
            }
            _isDataSyncRunning = false;
            Debug.Log("[SchedulerService] 데이터 동기화 정지");
        }

        public void StopAll()
        {
            StopRealtimeCheck();
            StopDataSync();
        }

        private IEnumerator RealtimeCheckLoop()
        {
            while (_isRealtimeRunning)
            {
                try
                {
                    Debug.Log($"[SchedulerService] 실시간 체크 트리거 - {DateTime.Now:HH:mm:ss}");
                    OnRealtimeCheckTriggered?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SchedulerService] 실시간 체크 오류: {ex.Message}");
                }

                yield return new WaitForSeconds(_realtimeInterval);
            }
        }

        private IEnumerator DataSyncLoop()
        {
            while (_isDataSyncRunning)
            {
                try
                {
                    Debug.Log($"[SchedulerService] 데이터 동기화 트리거 - {DateTime.Now:HH:mm:ss}");
                    OnDataSyncTriggered?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SchedulerService] 데이터 동기화 오류: {ex.Message}");
                }

                float waitSeconds = _dataSyncInterval * 60f;
                yield return new WaitForSeconds(waitSeconds);
            }
        }

        public void Cleanup()
        {
            StopAll();
            _dataService = null;
            _isInitialized = false;
            _isLoading = false;
            Debug.Log("[SchedulerService] 서비스 정리 완료");
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}