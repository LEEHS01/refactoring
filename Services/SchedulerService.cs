using System;
using System.Collections;
using UnityEngine;

namespace HNS.Services
{
    /// <summary>
    /// 스케줄러 서비스 - 주기적 작업 관리
    /// - 5초 주기: 실시간 알람 체크
    /// - 10분 주기: 데이터 동기화
    /// </summary>
    public class SchedulerService : MonoBehaviour
    {
        [Header("Scheduler Configuration")]
        [SerializeField] private float realtimeInterval = 5f;      // 5초
        [SerializeField] private float dataSyncInterval = 10f;      // 10분

        [Header("Runtime Status")]
        [SerializeField] private bool isRealtimeRunning = false;
        [SerializeField] private bool isDataSyncRunning = false;

        // 코루틴 참조
        private Coroutine _realtimeCoroutine;
        private Coroutine _dataSyncCoroutine;

        // 이벤트
        public event Action OnRealtimeCheckTriggered;   // 5초마다 호출
        public event Action OnDataSyncTriggered;         // 10분마다 호출
        public event Action OnAlarmDetected;             // 알람 발생 시
        public event Action OnAlarmCancelled;            // 알람 해제 시

        /// <summary>
        /// 실시간 체크 실행 상태
        /// </summary>
        public bool IsRealtimeRunning => isRealtimeRunning;

        /// <summary>
        /// 데이터 동기화 실행 상태
        /// </summary>
        public bool IsDataSyncRunning => isDataSyncRunning;

        #region Unity 생명주기

        private void Start()
        {
            Debug.Log("[SchedulerService] 스케줄러 시작");
            StartRealtimeCheck();
            StartDataSync();
        }

        private void OnDestroy()
        {
            Debug.Log("[SchedulerService] 스케줄러 종료");
            StopAll();
        }

        #endregion

        #region 스케줄러 제어

        /// <summary>
        /// 실시간 체크 시작 (5초 주기)
        /// </summary>
        public void StartRealtimeCheck()
        {
            if (isRealtimeRunning)
            {
                Debug.LogWarning("[SchedulerService] 실시간 체크가 이미 실행 중입니다.");
                return;
            }

            isRealtimeRunning = true;  // ⭐ CRITICAL: StartCoroutine 전에 먼저 설정!
            Debug.Log($"[SchedulerService] 실시간 체크 시작 - {realtimeInterval}초 주기");
            _realtimeCoroutine = StartCoroutine(RealtimeCheckLoop());
        }

        /// <summary>
        /// 데이터 동기화 시작 (10분 주기)
        /// </summary>
        public void StartDataSync()
        {
            if (isDataSyncRunning)
            {
                Debug.LogWarning("[SchedulerService] 데이터 동기화가 이미 실행 중입니다.");
                return;
            }

            isDataSyncRunning = true;  // ⭐ CRITICAL: StartCoroutine 전에 먼저 설정!
            Debug.Log($"[SchedulerService] 데이터 동기화 시작 - {dataSyncInterval}분 주기");
            _dataSyncCoroutine = StartCoroutine(DataSyncLoop());
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
                isRealtimeRunning = false;
                Debug.Log("[SchedulerService] 실시간 체크 정지");
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
                isDataSyncRunning = false;
                Debug.Log("[SchedulerService] 데이터 동기화 정지");
            }
        }

        #endregion

        #region 이벤트 트리거

        /// <summary>
        /// 알람 발생 시 외부에서 호출
        /// </summary>
        public void TriggerAlarmDetected()
        {
            Debug.Log("[SchedulerService] 알람 발생 트리거");
            OnAlarmDetected?.Invoke();
        }

        /// <summary>
        /// 알람 해제 시 외부에서 호출
        /// </summary>
        public void TriggerAlarmCancelled()
        {
            Debug.Log("[SchedulerService] 알람 해제 트리거");
            OnAlarmCancelled?.Invoke();
        }

        #endregion

        #region 코루틴 루프

        /// <summary>
        /// 실시간 체크 루프 (5초 주기)
        /// - 알람 발생/해제 감지
        /// </summary>
        private IEnumerator RealtimeCheckLoop()
        {
            while (isRealtimeRunning)
            {
                try
                {
                    Debug.Log($"[SchedulerService] 실시간 체크 트리거 - {DateTime.Now:HH:mm:ss}");
                    OnRealtimeCheckTriggered?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SchedulerService] 실시간 체크 오류: {ex.Message}\n{ex.StackTrace}");
                }

                yield return new WaitForSeconds(realtimeInterval);
            }
        }

        /// <summary>
        /// 데이터 동기화 루프 (10분 주기)
        /// - 센서 데이터, 차트 데이터, 알람 로그 갱신
        /// </summary>
        private IEnumerator DataSyncLoop()
        {
            while (isDataSyncRunning)
            {
                try
                {
                    Debug.Log($"[SchedulerService] 데이터 동기화 트리거 - {DateTime.Now:HH:mm:ss}");
                    OnDataSyncTriggered?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SchedulerService] 데이터 동기화 오류: {ex.Message}\n{ex.StackTrace}");
                }

                float waitSeconds = dataSyncInterval * 60f; // 분 → 초
                yield return new WaitForSeconds(waitSeconds);
            }
        }

        #endregion

        #region 설정 변경

        /// <summary>
        /// 실시간 체크 주기 변경
        /// </summary>
        public void SetRealtimeInterval(float seconds)
        {
            if (seconds < 1f || seconds > 60f)
            {
                Debug.LogWarning($"[SchedulerService] 잘못된 실시간 체크 주기: {seconds}초. 1~60초 범위여야 합니다.");
                return;
            }

            realtimeInterval = seconds;
            Debug.Log($"[SchedulerService] 실시간 체크 주기 변경: {seconds}초");

            // 재시작 필요
            if (isRealtimeRunning)
            {
                StopRealtimeCheck();
                StartRealtimeCheck();
            }
        }

        /// <summary>
        /// 데이터 동기화 주기 변경
        /// </summary>
        public void SetDataSyncInterval(float minutes)
        {
            if (minutes < 1f || minutes > 60f)
            {
                Debug.LogWarning($"[SchedulerService] 잘못된 데이터 동기화 주기: {minutes}분. 1~60분 범위여야 합니다.");
                return;
            }

            dataSyncInterval = minutes;
            Debug.Log($"[SchedulerService] 데이터 동기화 주기 변경: {minutes}분");

            // 재시작 필요
            if (isDataSyncRunning)
            {
                StopDataSync();
                StartDataSync();
            }
        }

        #endregion
    }
}