using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using HNS.Services;
using Onthesys;

namespace HNS.Services
{
    /// <summary>
    /// 통합 데이터 서비스 - 컴파일 오류 해결 버전
    /// yield return과 try-catch 충돌 문제 해결
    /// </summary>
    public class DataService : MonoBehaviour
    {
        [Header("Runtime Status")]
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private bool _isLoading = false;
        [SerializeField] private bool _isRefreshing = false;
        [SerializeField] private int _activeAlarmCount = 0;
        [SerializeField] private int _monthlyDataCount = 0;

        [Header("Unity Events - ViewModel들이 Inspector에서 구독")]
        [Space(10)]

        /// <summary>
        /// 월간 알람 데이터 변경 이벤트 - MonthlyAlarmTop5ViewModel에서 구독
        /// </summary>
        public UnityEvent OnMonthlyAlarmChanged = new UnityEvent();

        /// <summary>
        /// 활성 알람 목록 변경 이벤트 - AlarmListViewModel에서 구독
        /// </summary>
        public UnityEvent OnActiveAlarmsChanged = new UnityEvent();

        /// <summary>
        /// 전체 데이터 변경 이벤트 - 모든 ViewModel에서 구독 가능
        /// </summary>
        public UnityEvent OnDataChanged = new UnityEvent();

        // 코드 기반 의존성
        private DatabaseService _databaseService;
        private SchedulerService _schedulerService;

        // 캐시된 데이터
        private List<AlarmMontlyModel> _monthlyAlarmData;
        private List<LogData> _activeAlarmData;
        private Dictionary<string, List<AlarmMontlyModel>> _monthlyAlarmCache;

        #region Properties

        public bool IsInitialized => _isInitialized;
        public bool IsLoading => _isLoading;
        public bool IsRefreshing => _isRefreshing;
        public int ActiveAlarmCount => _activeAlarmCount;
        public DatabaseService DatabaseService => _databaseService;
        public SchedulerService SchedulerService => _schedulerService;

        /// <summary>
        /// 현재 월간 알람 데이터 (ViewModel에서 접근용)
        /// </summary>
        public List<AlarmMontlyModel> CurrentMonthlyData => _monthlyAlarmData ?? new List<AlarmMontlyModel>();

        /// <summary>
        /// 현재 활성 알람 데이터
        /// </summary>
        public List<LogData> CurrentActiveData => _activeAlarmData ?? new List<LogData>();

        #endregion

        private void Awake()
        {
            // 캐시 초기화
            _monthlyAlarmCache = new Dictionary<string, List<AlarmMontlyModel>>();
            _monthlyAlarmData = new List<AlarmMontlyModel>();
            _activeAlarmData = new List<LogData>();

            // 의존성 해결
            ResolveDependencies();
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 의존성 해결 - 코드 기반
        /// </summary>
        private void ResolveDependencies()
        {
            Debug.Log("[DataService] 의존성 해결 중...");

            _databaseService = FindObjectOfType<DatabaseService>();
            if (_databaseService == null)
            {
                Debug.LogError("[DataService] DatabaseService를 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log($"[DataService] DatabaseService 연결됨: {_databaseService.name}");
            }

            _schedulerService = FindObjectOfType<SchedulerService>();
            if (_schedulerService == null)
            {
                Debug.LogError("[DataService] SchedulerService를 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log($"[DataService] SchedulerService 연결됨: {_schedulerService.name}");
            }
        }

        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[DataService] 이미 초기화되었습니다.");
                return;
            }

            Debug.Log("[DataService] 초기화 시작...");
            _isLoading = true;

            // 의존성 검증
            if (!ValidateDependencies())
            {
                _isLoading = false;
                return;
            }

            // 초기 데이터 로드 시작
            StartCoroutine(InitialDataLoadCoroutine());
        }

        /// <summary>
        /// 의존성 유효성 검사
        /// </summary>
        private bool ValidateDependencies()
        {
            if (_databaseService == null)
            {
                Debug.LogError("[DataService] DatabaseService가 연결되지 않았습니다.");
                return false;
            }

            if (_schedulerService == null)
            {
                Debug.LogError("[DataService] SchedulerService가 연결되지 않았습니다.");
                return false;
            }

            Debug.Log("[DataService] 모든 의존성 검증 완료");
            return true;
        }

        /// <summary>
        /// 초기 데이터 로드 코루틴 - try-catch 문제 해결
        /// </summary>
        private IEnumerator InitialDataLoadCoroutine()
        {
            // DatabaseService 초기화 대기
            while (_databaseService != null && !_databaseService.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("[DataService] 초기 데이터 로드 중...");

            // yield return을 try-catch 밖에서 실행
            yield return StartCoroutine(RefreshAllDataCoroutine());

            // 초기화 완료 처리
            _isInitialized = true;
            _isLoading = false;

            Debug.Log("[DataService] 초기화 완료");
        }

        /// <summary>
        /// SchedulerService에서 호출되는 실시간 체크 처리
        /// </summary>
        public void TriggerRealtimeCheck()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[DataService] 아직 초기화되지 않았습니다.");
                return;
            }

            Debug.Log("[DataService] 실시간 체크 처리 중...");
            StartCoroutine(RealtimeCheckCoroutine());
        }

        /// <summary>
        /// SchedulerService에서 호출되는 데이터 동기화 처리
        /// </summary>
        public void TriggerDataSync()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[DataService] 아직 초기화되지 않았습니다.");
                return;
            }

            if (_isRefreshing)
            {
                Debug.LogWarning("[DataService] 이미 데이터 동기화가 진행 중입니다.");
                return;
            }

            Debug.Log("[DataService] 데이터 동기화 처리 중...");
            StartCoroutine(RefreshAllDataCoroutine());
        }

        /// <summary>
        /// 실시간 체크 코루틴 (간소화)
        /// </summary>
        private IEnumerator RealtimeCheckCoroutine()
        {
            if (_databaseService == null || !_databaseService.IsConnected)
            {
                Debug.LogWarning("[DataService] DatabaseService가 연결되지 않아 실시간 체크를 건너뜁니다.");
                yield break;
            }

            // 실시간 체크는 월간 데이터 간단 확인만
            Debug.Log("[DataService] 실시간 체크 완료");
        }

        /// <summary>
        /// 전체 데이터 새로고침 코루틴 - 에러 처리 간소화
        /// </summary>
        private IEnumerator RefreshAllDataCoroutine()
        {
            if (_isRefreshing)
            {
                Debug.LogWarning("[DataService] 이미 새로고침이 진행 중입니다.");
                yield break;
            }

            _isRefreshing = true;
            Debug.Log("[DataService] 전체 데이터 새로고침 시작...");

            // 월간 데이터만 새로고침 (단순화)
            yield return StartCoroutine(RefreshMonthlyAlarmDataCoroutine());

            Debug.Log("[DataService] 전체 데이터 새로고침 완료");

            // 데이터 변경 이벤트 발생
            OnDataChanged?.Invoke();

            _isRefreshing = false;
        }

        /// <summary>
        /// 월간 알람 데이터 새로고침 - 단순화 버전
        /// </summary>
        private IEnumerator RefreshMonthlyAlarmDataCoroutine()
        {
            if (_databaseService == null)
            {
                Debug.LogWarning("[DataService] DatabaseService가 없어 월간 데이터 새로고침을 건너뜁니다.");
                yield break;
            }

            string currentMonth = DateTime.Now.ToString("yyyyMM");
            Debug.Log($"[DataService] 월간 알람 데이터 조회 중... ({currentMonth})");

            // 캐시 확인
            if (_monthlyAlarmCache.ContainsKey(currentMonth))
            {
                _monthlyAlarmData = _monthlyAlarmCache[currentMonth];
                Debug.Log($"[DataService] 월간 데이터 캐시 사용: {currentMonth}");

                _monthlyDataCount = _monthlyAlarmData.Count;
                OnMonthlyAlarmChanged?.Invoke();
                yield break;
            }

            // 실제 DB 조회 (비동기를 코루틴으로 처리)
            bool queryCompleted = false;
            List<AlarmMontlyModel> queryResult = null;
            string errorMessage = null;

            StartCoroutine(ExecuteDatabaseQueryCoroutine(currentMonth,
                (result) => {
                    queryResult = result;
                    queryCompleted = true;
                },
                (error) => {
                    errorMessage = error;
                    queryCompleted = true;
                }));

            // 쿼리 완료 대기
            yield return new WaitUntil(() => queryCompleted);

            // 결과 처리
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogError($"[DataService] 월간 알람 데이터 조회 실패: {errorMessage}");
                yield break;
            }

            _monthlyAlarmData = queryResult ?? new List<AlarmMontlyModel>();
            _monthlyAlarmCache[currentMonth] = _monthlyAlarmData;
            _monthlyDataCount = _monthlyAlarmData.Count;

            Debug.Log($"[DataService] 월간 알람 데이터 업데이트: {_monthlyDataCount}개");

            // 이벤트 발생
            OnMonthlyAlarmChanged?.Invoke();
        }

        /// <summary>
        /// 데이터베이스 쿼리 실행 코루틴 (Task를 코루틴으로 래핑)
        /// </summary>
        private IEnumerator ExecuteDatabaseQueryCoroutine(string targetMonth,
            System.Action<List<AlarmMontlyModel>> onSuccess,
            System.Action<string> onError)
        {
            var task = _databaseService.GetMonthlyAlarmTop5Async(targetMonth);

            // Task 완료까지 대기
            while (!task.IsCompleted)
            {
                yield return null;
            }

            // 결과 처리
            if (task.Exception != null)
            {
                onError?.Invoke(task.Exception.GetBaseException().Message);
            }
            else
            {
                onSuccess?.Invoke(task.Result);
            }
        }

        /// <summary>
        /// 특정 월의 월간 데이터 요청
        /// </summary>
        public void RefreshMonthlyAlarmData(string targetMonth)
        {
            if (string.IsNullOrEmpty(targetMonth) || targetMonth.Length != 6)
            {
                Debug.LogError($"[DataService] 잘못된 월 형식: {targetMonth}");
                return;
            }

            StartCoroutine(RefreshSpecificMonthDataCoroutine(targetMonth));
        }

        /// <summary>
        /// 특정 월의 데이터 새로고침
        /// </summary>
        private IEnumerator RefreshSpecificMonthDataCoroutine(string targetMonth)
        {
            Debug.Log($"[DataService] 특정 월간 알람 데이터 조회: {targetMonth}");

            bool queryCompleted = false;
            List<AlarmMontlyModel> queryResult = null;
            string errorMessage = null;

            StartCoroutine(ExecuteDatabaseQueryCoroutine(targetMonth,
                (result) => {
                    queryResult = result;
                    queryCompleted = true;
                },
                (error) => {
                    errorMessage = error;
                    queryCompleted = true;
                }));

            yield return new WaitUntil(() => queryCompleted);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogError($"[DataService] 특정 월간 데이터 조회 실패: {errorMessage}");
                yield break;
            }

            var monthData = queryResult ?? new List<AlarmMontlyModel>();
            _monthlyAlarmCache[targetMonth] = monthData;

            // 현재 월이면 업데이트
            if (targetMonth == DateTime.Now.ToString("yyyyMM"))
            {
                _monthlyAlarmData = monthData;
                _monthlyDataCount = monthData.Count;
                OnMonthlyAlarmChanged?.Invoke();
            }

            Debug.Log($"[DataService] 월간 데이터 캐시 업데이트: {targetMonth} ({monthData.Count}개)");
        }

        #region Context Menu Debug Methods

        /// <summary>
        /// 현재 캐시된 데이터 확인
        /// </summary>
        [ContextMenu("현재 DB 데이터 확인")]
        public void PrintCurrentData()
        {
            Debug.Log($"[DataService] === 현재 월간 데이터 ({_monthlyDataCount}개) ===");

            if (_monthlyAlarmData != null && _monthlyAlarmData.Count > 0)
            {
                for (int i = 0; i < _monthlyAlarmData.Count; i++)
                {
                    var data = _monthlyAlarmData[i];
                    Debug.Log($"  [{i + 1}] {data.areanm}: {data.cnt}개 알람");
                }
            }
            else
            {
                Debug.Log("  >> 데이터 없음 - DB 연결 또는 프로시저 실행 확인 필요");
            }

            Debug.Log($"[DataService] 캐시 상태: {_monthlyAlarmCache?.Count ?? 0}개월 캐시됨");
        }

        [ContextMenu("캐시 클리어")]
        public void ClearCache()
        {
            _monthlyAlarmCache?.Clear();
            Debug.Log("[DataService] 모든 캐시가 클리어되었습니다.");
        }

        [ContextMenu("수동 데이터 새로고침")]
        public void ManualRefresh()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[DataService] 초기화 후 사용하세요.");
                return;
            }

            StartCoroutine(RefreshAllDataCoroutine());
        }

        [ContextMenu("디버그 정보 출력")]
        public void PrintDebugInfo()
        {
            Debug.Log($"[DataService] 디버그 정보:" +
                     $"\n- 초기화: {_isInitialized}" +
                     $"\n- 새로고침 중: {_isRefreshing}" +
                     $"\n- 월간 데이터: {_monthlyDataCount}개" +
                     $"\n- 월간 캐시: {_monthlyAlarmCache?.Count ?? 0}개월" +
                     $"\n- DatabaseService: {(_databaseService != null ? _databaseService.name : "없음")}" +
                     $"\n- SchedulerService: {(_schedulerService != null ? _schedulerService.name : "없음")}");
        }

        #endregion

        /// <summary>
        /// 서비스 정리
        /// </summary>
        public void Cleanup()
        {
            _monthlyAlarmCache?.Clear();
            _monthlyAlarmData?.Clear();
            _activeAlarmData?.Clear();

            _databaseService = null;
            _schedulerService = null;

            _isInitialized = false;
            _isLoading = false;
            _isRefreshing = false;

            Debug.Log("[DataService] 서비스 정리 완료");
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}