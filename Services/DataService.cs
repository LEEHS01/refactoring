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
    /// 통합 데이터 서비스 - 모든 데이터 관리 및 ViewModel 제공 (기존 ModelManager 역할)
    /// 의존성은 코드로 해결, Unity Events만 Inspector에서 ViewModel과 연결
    /// </summary>
    public class DataService : MonoBehaviour
    {
        [Header("Runtime Status")]
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private bool _isLoading = false;
        [SerializeField] private bool _isRefreshing = false;
        [SerializeField] private int _activeAlarmCount = 0;
        [SerializeField] private int _monthlyDataCount = 0;
        [SerializeField] private int _yearlyDataCount = 0;

        [Header("Unity Events - ViewModel들이 Inspector에서 구독")]
        [Space(10)]

        /// <summary>
        /// 월간 알람 데이터 변경 이벤트 - MonthlyAlarmTop5ViewModel에서 구독
        /// </summary>
        public UnityEvent OnMonthlyAlarmChanged = new UnityEvent();

        /// <summary>
        /// 연간 알람 데이터 변경 이벤트 - YearlyAlarmTop5ViewModel에서 구독
        /// </summary>
        public UnityEvent OnYearlyAlarmChanged = new UnityEvent();

        /// <summary>
        /// 활성 알람 목록 변경 이벤트 - AlarmListViewModel에서 구독
        /// </summary>
        public UnityEvent OnActiveAlarmsChanged = new UnityEvent();

        /// <summary>
        /// 전체 데이터 변경 이벤트 - 모든 ViewModel에서 구독 가능
        /// </summary>
        public UnityEvent OnDataChanged = new UnityEvent();

        // 코드 기반 의존성 - Inspector 필드 제거
        private DatabaseService _databaseService;
        private SchedulerService _schedulerService;

        // 캐시된 데이터 - 타입 통일: AlarmMontlyModel 사용
        private List<AlarmMontlyModel> _monthlyAlarmData;
        private List<AlarmYearlyModel> _yearlyAlarmData;
        private List<LogData> _activeAlarmData;
        private Dictionary<string, List<AlarmMontlyModel>> _monthlyAlarmCache;

        #region Properties

        /// <summary>
        /// 초기화 상태
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 로딩 상태
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// 새로고침 진행 상태 (동시 실행 방지용)
        /// </summary>
        public bool IsRefreshing => _isRefreshing;

        /// <summary>
        /// 현재 활성 알람 개수
        /// </summary>
        public int ActiveAlarmCount => _activeAlarmCount;

        /// <summary>
        /// 연결된 DatabaseService 참조
        /// </summary>
        public DatabaseService DatabaseService => _databaseService;

        /// <summary>
        /// 연결된 SchedulerService 참조
        /// </summary>
        public SchedulerService SchedulerService => _schedulerService;

        /// <summary>
        /// 현재 월간 알람 데이터 (읽기 전용) - ViewModel에서 접근용
        /// 타입 통일: List<AlarmMontlyModel> 반환
        /// </summary>
        public List<AlarmMontlyModel> CurrentMonthlyData => _monthlyAlarmData ?? new List<AlarmMontlyModel>();

        /// <summary>
        /// 현재 연간 알람 데이터 (읽기 전용)
        /// </summary>
        public List<AlarmYearlyModel> CurrentYearlyData => _yearlyAlarmData ?? new List<AlarmYearlyModel>();

        /// <summary>
        /// 현재 활성 알람 데이터 (읽기 전용)
        /// </summary>
        public List<LogData> CurrentActiveData => _activeAlarmData ?? new List<LogData>();

        #endregion

        private void Awake()
        {
            // 캐시 초기화 - 타입 통일
            _monthlyAlarmCache = new Dictionary<string, List<AlarmMontlyModel>>();

            // 코드 기반 의존성 해결
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
            try
            {
                Debug.Log("[DataService] 의존성 해결 중...");

                // DatabaseService 찾기
                _databaseService = FindObjectOfType<DatabaseService>();
                if (_databaseService == null)
                {
                    Debug.LogError("[DataService] DatabaseService를 찾을 수 없습니다.");
                }
                else
                {
                    Debug.Log($"[DataService] DatabaseService 연결됨: {_databaseService.name}");
                }

                // SchedulerService 찾기
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
            catch (Exception ex)
            {
                Debug.LogError($"[DataService] 의존성 해결 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 서비스 초기화 - 간단한 버전
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

            // 의존성 검증 (예외 발생시 로그만 출력)
            if (_databaseService == null)
            {
                Debug.LogError("[DataService] DatabaseService가 연결되지 않았습니다.");
                _isLoading = false;
                return;
            }

            if (_schedulerService == null)
            {
                Debug.LogError("[DataService] SchedulerService가 연결되지 않았습니다.");
                _isLoading = false;
                return;
            }

            Debug.Log("[DataService] 모든 의존성 검증 완료");

            // 초기 데이터 로드
            StartCoroutine(InitialDataLoadCoroutine());
        }



        /// <summary>
        /// 초기 데이터 로드 (코루틴) - 간단한 버전
        /// </summary>
        private IEnumerator InitialDataLoadCoroutine()
        {
            // DatabaseService 초기화 대기
            while (_databaseService != null && !_databaseService.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("[DataService] 초기 데이터 로드 중...");

            // yield return을 그대로 사용 (예외 처리는 RefreshAllDataCoroutine 내부에서)
            yield return StartCoroutine(RefreshAllDataCoroutine());

            _isInitialized = true;
            _isLoading = false;

            Debug.Log("[DataService] 초기화 완료");
        }

        /// <summary>
        /// SchedulerService에서 호출되는 실시간 체크 처리 - 간단한 버전
        /// </summary>
        public void TriggerRealtimeCheck()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[DataService] 아직 초기화되지 않았습니다.");
                return;
            }

            Debug.Log("[DataService] 실시간 체크 처리 중...");

            // 실시간 알람 체크 (에러 처리는 코루틴 내부에서)
            StartCoroutine(RealtimeCheckCoroutine());
        }

        /// <summary>
        /// SchedulerService에서 호출되는 데이터 동기화 처리 - 간단한 버전
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

            // 전체 데이터 새로고침 (에러 처리는 코루틴 내부에서)
            StartCoroutine(RefreshAllDataCoroutine());
        }

        /// <summary>
        /// 실시간 체크 코루틴 - 간단한 버전
        /// </summary>
        private IEnumerator RealtimeCheckCoroutine()
        {
            if (_databaseService == null || !_databaseService.IsConnected)
            {
                Debug.LogWarning("[DataService] 데이터베이스 서비스가 연결되지 않았습니다.");
                yield break;
            }

            Debug.Log("[DataService] 실시간 알람 체크 중...");

            // 활성 알람만 빠르게 체크 (에러 처리는 내부에서)
            yield return StartCoroutine(RefreshActiveAlarmsCoroutine());
        }

        /// <summary>
        /// 전체 데이터 새로고침 코루틴 - 간단한 버전
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

            // 순차적으로 데이터 새로고침 (에러 처리는 각 메서드 내부에서)
            yield return StartCoroutine(RefreshMonthlyAlarmDataCoroutine());

            Debug.Log("[DataService] 전체 데이터 새로고침 완료");

            // 데이터 변경 이벤트 발생
            OnDataChanged?.Invoke();

            _isRefreshing = false;
        }

        /// <summary>
        /// 월간 알람 데이터 새로고침 - 간단한 버전
        /// </summary>
        private IEnumerator RefreshMonthlyAlarmDataCoroutine()
        {
            if (_databaseService == null)
            {
                Debug.LogWarning("[DataService] DatabaseService가 없습니다.");
                yield break;
            }

            string currentMonth = DateTime.Now.ToString("yyyyMM");

            // 캐시 확인
            if (_monthlyAlarmCache.ContainsKey(currentMonth))
            {
                _monthlyAlarmData = _monthlyAlarmCache[currentMonth];
                Debug.Log($"[DataService] 월간 데이터 캐시 사용: {currentMonth}");
            }
            else
            {
                Debug.Log($"[DataService] 월간 알람 데이터 조회 중... ({currentMonth})");

                // 비동기 호출을 코루틴으로 래핑 (에러 처리 포함)
                bool completed = false;
                List<AlarmMontlyModel> result = null;

                StartCoroutine(ExecuteAsyncSafely(() => _databaseService.GetMonthlyAlarmTop5Async(currentMonth),
                    (data) => { result = data; completed = true; },
                    () => completed = true)); // 에러시에도 완료 처리

                // 완료 대기 (try-catch 밖으로 이동)
                while (!completed)
                {
                    yield return null;
                }

                _monthlyAlarmData = result ?? new List<AlarmMontlyModel>();
                _monthlyAlarmCache[currentMonth] = _monthlyAlarmData;
            }

            _monthlyDataCount = _monthlyAlarmData.Count;
            Debug.Log($"[DataService] 월간 알람 데이터 업데이트: {_monthlyDataCount}개");

            // 월간 데이터 변경 이벤트
            OnMonthlyAlarmChanged?.Invoke();
        }

        /// <summary>
        /// 활성 알람 데이터 새로고침 - 간단한 버전
        /// </summary>
        private IEnumerator RefreshActiveAlarmsCoroutine()
        {
            Debug.Log("[DataService] 활성 알람 데이터 조회 중...");

            // TODO: DatabaseService에 활성 알람 조회 메서드 추가 후 구현
            // 임시로 빈 리스트 설정
            _activeAlarmData = new List<LogData>();
            _activeAlarmCount = _activeAlarmData.Count;

            Debug.Log($"[DataService] 활성 알람 데이터 업데이트: {_activeAlarmCount}개");

            // 활성 알람 변경 이벤트
            OnActiveAlarmsChanged?.Invoke();

            yield return null; // 코루틴 완료를 위한 yield
        }

        /// <summary>
        /// 비동기 작업을 코루틴으로 래핑하는 헬퍼 - 간단한 버전 (에러 무시)
        /// </summary>
        private IEnumerator ExecuteAsyncSafely<T>(Func<System.Threading.Tasks.Task<T>> asyncFunc, Action<T> onSuccess, Action onComplete)
        {
            var task = asyncFunc();

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Exception != null)
            {
                Debug.LogError($"[DataService] 비동기 작업 실패: {task.Exception.GetBaseException().Message}");
                onSuccess?.Invoke(default(T)); // 실패시 기본값 반환
            }
            else
            {
                onSuccess?.Invoke(task.Result);
            }

            onComplete?.Invoke(); // 성공/실패 관계없이 완료 처리
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

            StartCoroutine(RefreshMonthlyAlarmDataCoroutine(targetMonth));
        }

        /// <summary>
        /// 특정 월의 월간 알람 데이터 새로고침 - 간단한 버전
        /// </summary>
        private IEnumerator RefreshMonthlyAlarmDataCoroutine(string targetMonth)
        {
            if (_databaseService == null)
            {
                Debug.LogWarning("[DataService] DatabaseService가 없습니다.");
                yield break;
            }

            Debug.Log($"[DataService] 특정 월간 알람 데이터 조회: {targetMonth}");

            bool completed = false;
            List<AlarmMontlyModel> result = null;

            StartCoroutine(ExecuteAsyncSafely(() => _databaseService.GetMonthlyAlarmTop5Async(targetMonth),
                (data) => { result = data; completed = true; },
                () => completed = true)); // 에러시에도 완료 처리

            // 완료 대기 (try-catch 밖으로 이동)
            while (!completed)
            {
                yield return null;
            }

            var monthData = result ?? new List<AlarmMontlyModel>();
            _monthlyAlarmCache[targetMonth] = monthData;

            // 현재 월이면 캐시된 데이터 업데이트
            if (targetMonth == DateTime.Now.ToString("yyyyMM"))
            {
                _monthlyAlarmData = monthData;
                _monthlyDataCount = monthData.Count;
                OnMonthlyAlarmChanged?.Invoke();
            }

            Debug.Log($"[DataService] 월간 데이터 캐시 업데이트: {targetMonth} ({monthData.Count}개)");
        }

        /// <summary>
        /// 캐시 클리어
        /// </summary>
        [ContextMenu("캐시 클리어")]
        public void ClearCache()
        {
            _monthlyAlarmCache?.Clear();
            Debug.Log("[DataService] 모든 캐시가 클리어되었습니다.");
        }

        /// <summary>
        /// 서비스 정리
        /// </summary>
        public void Cleanup()
        {
            // 캐시 정리
            _monthlyAlarmCache?.Clear();

            // 참조 정리
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

        /// <summary>
        /// 런타임 디버그 정보
        /// </summary>
        [ContextMenu("디버그 정보 출력")]
        public void PrintDebugInfo()
        {
            Debug.Log($"[DataService] 디버그 정보:" +
                     $"\n- 초기화: {_isInitialized}" +
                     $"\n- 새로고침 중: {_isRefreshing}" +
                     $"\n- 활성 알람: {_activeAlarmCount}개" +
                     $"\n- 월간 데이터: {_monthlyDataCount}개" +
                     $"\n- 월간 캐시: {_monthlyAlarmCache?.Count ?? 0}개월" +
                     $"\n- DatabaseService: {(_databaseService != null ? _databaseService.name : "없음")}" +
                     $"\n- SchedulerService: {(_schedulerService != null ? _schedulerService.name : "없음")}");
        }

        /// <summary>
        /// 수동 데이터 새로고침 (디버그용)
        /// </summary>
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
    }
}