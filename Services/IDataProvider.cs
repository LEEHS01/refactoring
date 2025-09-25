using System;
using System.Threading.Tasks;

namespace HNS.Services
{
    /// <summary>
    /// 데이터 제공자 베이스 인터페이스
    /// 모든 서비스가 공통으로 구현해야 할 기본 계약
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// 서비스 초기화 상태
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 로딩 상태
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 서비스 초기화
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// 서비스 정리
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// 데이터베이스 서비스 인터페이스
    /// </summary>
    public interface IDatabaseService : IDataProvider
    {
        /// <summary>
        /// 데이터베이스 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// SQL 쿼리 실행
        /// </summary>
        Task<T> ExecuteQueryAsync<T>(string query, object parameters = null);

        /// <summary>
        /// 연결 테스트
        /// </summary>
        Task<bool> TestConnectionAsync();
    }

    /// <summary>
    /// 스케줄러 서비스 인터페이스
    /// </summary>
    public interface ISchedulerService : IDataProvider
    {
        /// <summary>
        /// 실시간 알람 체크 시작
        /// </summary>
        void StartRealtimeCheck();

        /// <summary>
        /// 데이터 동기화 시작
        /// </summary>
        void StartDataSync();

        /// <summary>
        /// 모든 스케줄러 정지
        /// </summary>
        void StopAll();

        /// <summary>
        /// 실시간 체크 실행 상태
        /// </summary>
        bool IsRealtimeRunning { get; }

        /// <summary>
        /// 데이터 동기화 실행 상태
        /// </summary>
        bool IsDataSyncRunning { get; }
    }

    /// <summary>
    /// 통합 데이터 서비스 인터페이스
    /// </summary>
    public interface IDataService : IDataProvider
    {
        /// <summary>
        /// 전체 데이터 새로고침
        /// </summary>
        Task RefreshAllDataAsync();

        /// <summary>
        /// 데이터 변경 이벤트
        /// </summary>
        event Action OnDataChanged;
    }
}