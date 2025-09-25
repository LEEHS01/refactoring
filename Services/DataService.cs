using Assets.Scripts_refactoring.Services;
using HNS.Core;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HNS.Services
{
    /// <summary>
    /// 통합 데이터 서비스 - 모든 데이터 관리 및 ViewModel 제공 (기존 ModelManager 역할)
    /// </summary>
    public class DataService : MonoBehaviour, IDataService
    {
        [Header("Service Dependencies")]
        [SerializeField] private DatabaseService _databaseService;
        [SerializeField] private SchedulerService _schedulerService;

        [Header("Runtime Status")]
        [SerializeField, ReadOnly] private bool _isInitialized = false;
        [SerializeField, ReadOnly] private bool _isLoading = false;
        [SerializeField, ReadOnly] private bool _isRefreshing = false;

        // 이벤트
        public event Action OnDataChanged;

        /// <summary>
        /// 초기화 상태
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 로딩 상태
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// 새로고침 진행 상태
        /// </summary>
        public bool IsRefreshing => _isRefreshing;

        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[DataService] 이미 초기화되었습니다.");
                return true;
            }

            try
            {
                Debug.Log("[DataService] 초기화 시작...");
                _isLoading = true;

                // TODO: 의존성 서비스 확인
                // TODO: 의존성 서비스들 초기화
                // TODO: 스케줄러 이벤트 연결
                // TODO: 초기 데이터 로드

                _isInitialized = true;
                Debug.Log("[DataService] 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataService] 초기화 실패: {ex.Message}");
                return false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 전체 데이터 새로고침
        /// </summary>
        public async Task RefreshAllDataAsync()
        {
            if (_isRefreshing)
            {
                Debug.LogWarning("[DataService] 이미 새로고침이 진행 중입니다.");
                return;
            }

            _isRefreshing = true;

            try
            {
                Debug.Log("[DataService] 전체 데이터 새로고침 시작...");

                // TODO: 월간 TOP5 쿼리 실행하여 데이터 가져오기
                // TODO: 연간 TOP5 쿼리 실행하여 데이터 가져오기
                // TODO: 알람 데이터 가져오기
                // TODO: 기타 필요한 데이터 가져오기

                Debug.Log("[DataService] 전체 데이터 새로고침 완료");

                // 데이터 변경 이벤트 발생
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataService] 전체 데이터 새로고침 실패: {ex.Message}");
                throw;
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// 스케줄러 실시간 체크 트리거 처리
        /// </summary>
        private async void HandleRealtimeCheckTriggered()
        {
            try
            {
                // TODO: 실시간 알람 체크 로직 구현
                Debug.Log("[DataService] 실시간 체크 처리");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataService] 실시간 체크 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 스케줄러 데이터 동기화 트리거 처리
        /// </summary>
        private async void HandleDataSyncTriggered()
        {
            try
            {
                await RefreshAllDataAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataService] 데이터 동기화 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 서비스 정리
        /// </summary>
        public void Cleanup()
        {
            // TODO: 스케줄러 이벤트 해제
            // TODO: 캐시 데이터 정리

            _isInitialized = false;
            _isLoading = false;
            _isRefreshing = false;

            Debug.Log("[DataService] 서비스 정리 완료");
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnValidate()
        {
            if (_databaseService == null)
                Debug.LogWarning("[DataService] DatabaseService가 연결되지 않았습니다.");

            if (_schedulerService == null)
                Debug.LogWarning("[DataService] SchedulerService가 연결되지 않았습니다.");
        }

        /// <summary>
        /// 스케줄러 이벤트 연결 (초기화 시 호출)
        /// </summary>
        private void SetupSchedulerEvents()
        {
            if (_schedulerService != null)
            {
                _schedulerService.OnRealtimeCheckTriggered += HandleRealtimeCheckTriggered;
                _schedulerService.OnDataSyncTriggered += HandleDataSyncTriggered;
            }
        }

        /// <summary>
        /// 스케줄러 이벤트 해제 (정리 시 호출)
        /// </summary>
        private void CleanupSchedulerEvents()
        {
            if (_schedulerService != null)
            {
                _schedulerService.OnRealtimeCheckTriggered -= HandleRealtimeCheckTriggered;
                _schedulerService.OnDataSyncTriggered -= HandleDataSyncTriggered;
            }
        }
    }
}