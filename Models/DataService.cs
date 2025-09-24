using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using HNS.Core;

namespace HNS.Services
{
    /// <summary>
    /// 통합 데이터 서비스 - 기존 ModelManager 역할을 MVVM에 맞게 재구성
    /// DB 연결, 스케줄러, 데이터 캐싱을 모두 담당하는 Model 계층
    /// </summary>
    public class DataService : MonoBehaviour
    {
        [Header("Database Configuration")]
        [SerializeField] private string _apiUrl = "http://localhost:8080/api/data";
        [SerializeField] private float _realtimeCheckInterval = 2f; // 1-3초 실시간 알람 체크
        [SerializeField] private float _dataSyncInterval = 600f; // 10분 데이터 동기화

        [Header("Runtime Status")]
        [SerializeField, ReadOnly] private bool _isInitialized = false;
        [SerializeField, ReadOnly] private bool _isRealtimeRunning = false;
        [SerializeField, ReadOnly] private bool _isDataSyncRunning = false;
        [SerializeField, ReadOnly] private int _activeAlarmCount = 0;

        #region Reactive Properties - ViewModel들이 구독할 데이터

        /// <summary>
        /// 활성 알람 목록 - AlarmListViewModel에서 구독
        /// </summary>
        public ReactiveCollection<AlarmData> ActiveAlarms { get; private set; } = new ReactiveCollection<AlarmData>();

        /// <summary>
        /// 월간 알람 통계 - MonthlyAlarmTop5ViewModel에서 구독  
        /// </summary>
        public ReactiveCollection<AlarmStatistics> MonthlyAlarmStats { get; private set; } = new ReactiveCollection<AlarmStatistics>();

        /// <summary>
        /// 연간 알람 통계 - YearlyAlarmTop5ViewModel에서 구독
        /// </summary>
        public ReactiveCollection<AlarmStatistics> YearlyAlarmStats { get; private set; } = new ReactiveCollection<AlarmStatistics>();

        /// <summary>
        /// 관측소 목록 - ObsMonitoringViewModel에서 구독
        /// </summary>
        public ReactiveCollection<ObservationStationData> ObservationStations { get; private set; } = new ReactiveCollection<ObservationStationData>();

        /// <summary>
        /// 지역 목록 - 각종 ViewModel에서 구독
        /// </summary>
        public ReactiveCollection<AreaData> Areas { get; private set; } = new ReactiveCollection<AreaData>();

        /// <summary>
        /// 서비스 초기화 상태
        /// </summary>
        public ReactiveProperty<bool> IsServiceInitialized { get; private set; } = new ReactiveProperty<bool>(false);

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public ReactiveProperty<DateTime> LastUpdateTime { get; private set; } = new ReactiveProperty<DateTime>(DateTime.MinValue);

        #endregion

        // 내부 데이터 캐시
        private List<AlarmData> _previousAlarms = new List<AlarmData>();
        private Coroutine _realtimeCheckCoroutine;
        private Coroutine _dataSyncCoroutine;

        // 싱글톤 패턴 (기존 ModelManager와 동일)
        public static DataService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        /// <summary>
        /// 비동기 초기화
        /// </summary>
        private IEnumerator InitializeAsync()
        {
            Debug.Log("[DataService] 초기화 시작...");

            // DB 연결 테스트
            yield return StartCoroutine(TestDatabaseConnection());

            // 기본 데이터 로드
            yield return StartCoroutine(LoadInitialData());

            // 스케줄러 시작
            StartRealtimeAlarmCheck();
            StartDataSyncScheduler();

            _isInitialized = true;
            IsServiceInitialized.Value = true;
            Debug.Log("[DataService] 초기화 완료");
        }

        /// <summary>
        /// DB 연결 테스트
        /// </summary>
        private IEnumerator TestDatabaseConnection()
        {
            Debug.Log("[DataService] DB 연결 테스트 중...");

            yield return StartCoroutine(ExecuteApiRequest("SELECT", "SELECT 1 as TestResult", result =>
            {
                if (result.Contains("Error:"))
                {
                    Debug.LogError($"[DataService] DB 연결 실패: {result}");
                }
                else
                {
                    Debug.Log("[DataService] DB 연결 성공");
                }
            }));
        }

        /// <summary>
        /// 초기 데이터 로드
        /// </summary>
        private IEnumerator LoadInitialData()
        {
            Debug.Log("[DataService] 초기 데이터 로드 중...");

            // 관측소 데이터 로드
            yield return StartCoroutine(LoadObservationStations());

            // 지역 데이터 로드  
            yield return StartCoroutine(LoadAreas());

            // 초기 알람 데이터 로드
            yield return StartCoroutine(LoadActiveAlarms());

            // 통계 데이터 로드
            yield return StartCoroutine(LoadAlarmStatistics());

            LastUpdateTime.Value = DateTime.Now;
            Debug.Log("[DataService] 초기 데이터 로드 완료");
        }

        #region 스케줄러 관리

        /// <summary>
        /// 실시간 알람 체크 시작 (1-3초 주기)
        /// </summary>
        public void StartRealtimeAlarmCheck()
        {
            if (_isRealtimeRunning) return;

            Debug.Log($"[DataService] 실시간 알람 체크 시작 - {_realtimeCheckInterval}초 주기");
            _realtimeCheckCoroutine = StartCoroutine(RealtimeAlarmCheckLoop());
            _isRealtimeRunning = true;
        }

        /// <summary>
        /// 데이터 동기화 시작 (10분 주기)
        /// </summary>
        public void StartDataSyncScheduler()
        {
            if (_isDataSyncRunning) return;

            Debug.Log($"[DataService] 데이터 동기화 시작 - {_dataSyncInterval / 60f}분 주기");
            _dataSyncCoroutine = StartCoroutine(DataSyncLoop());
            _isDataSyncRunning = true;
        }

        /// <summary>
        /// 모든 스케줄러 정지
        /// </summary>
        public void StopAllSchedulers()
        {
            if (_realtimeCheckCoroutine != null)
            {
                StopCoroutine(_realtimeCheckCoroutine);
                _isRealtimeRunning = false;
            }

            if (_dataSyncCoroutine != null)
            {
                StopCoroutine(_dataSyncCoroutine);
                _isDataSyncRunning = false;
            }

            Debug.Log("[DataService] 모든 스케줄러 정지");
        }

        /// <summary>
        /// 실시간 알람 체크 루프
        /// </summary>
        private IEnumerator RealtimeAlarmCheckLoop()
        {
            while (_isRealtimeRunning)
            {
                yield return StartCoroutine(LoadActiveAlarms());
                yield return new WaitForSeconds(_realtimeCheckInterval);
            }
        }

        /// <summary>
        /// 데이터 동기화 루프
        /// </summary>
        private IEnumerator DataSyncLoop()
        {
            while (_isDataSyncRunning)
            {
                Debug.Log("[DataService] 데이터 동기화 실행...");

                yield return StartCoroutine(LoadObservationStations());
                yield return StartCoroutine(LoadAreas());
                yield return StartCoroutine(LoadAlarmStatistics());

                LastUpdateTime.Value = DateTime.Now;
                Debug.Log($"[DataService] 데이터 동기화 완료 - {DateTime.Now:HH:mm:ss}");

                yield return new WaitForSeconds(_dataSyncInterval);
            }
        }

        #endregion

        #region 데이터 로드 메서드들

        /// <summary>
        /// 활성 알람 데이터 로드
        /// </summary>
        private IEnumerator LoadActiveAlarms()
        {
            string query = @"
                SELECT 
                    a.ALAIDX as alaidx,
                    a.HNSIDX as hnsidx, 
                    area.AREANM as areaNm,
                    obs.OBSNM as obsName,
                    h.HNSNM as hnsName,
                    a.ALADT as aladt,
                    a.ALACODE as alacode,
                    a.CURRVAL as currval,
                    a.ALAHIVAL as alahival,
                    a.ALAHIHIVAL as alahihival
                FROM TB_ALARM_DATA a
                INNER JOIN TB_HNS_RESOURCE h ON a.HNSIDX = h.HNSIDX
                INNER JOIN TB_OBS_INFO obs ON h.OBSIDX = obs.OBSIDX  
                INNER JOIN TB_AREA_INFO area ON obs.AREAIDX = area.AREAIDX
                WHERE a.TURNOFF_FLAG IS NULL
                ORDER BY a.ALADT DESC";

            yield return StartCoroutine(ExecuteApiRequest("SELECT", query, result =>
            {
                try
                {
                    var alarms = JsonConvert.DeserializeObject<List<AlarmData>>(result) ?? new List<AlarmData>();

                    // 새로운 알람 감지
                    DetectNewAlarms(alarms);

                    // ReactiveCollection 업데이트 (자동으로 구독자들에게 알림)
                    ActiveAlarms.SetItemsWithoutNotify(alarms);
                    ActiveAlarms.NotifyCurrentCollection();

                    _activeAlarmCount = alarms.Count;
                    _previousAlarms = new List<AlarmData>(alarms);

                    Debug.Log($"[DataService] 활성 알람 로드 완료: {alarms.Count}개");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataService] 활성 알람 로드 실패: {ex.Message}");
                }
            }));
        }

        /// <summary>
        /// 월간/연간 알람 통계 로드
        /// </summary>
        private IEnumerator LoadAlarmStatistics()
        {
            // 월간 통계
            string monthlyQuery = @"
                SELECT 
                    a.AREANM as AreaName,
                    COUNT(CASE WHEN ad.ALACODE = 1 THEN 1 END) as SeriousCount,
                    COUNT(CASE WHEN ad.ALACODE = 2 THEN 1 END) as WarningCount,
                    COUNT(CASE WHEN ad.ALACODE = 3 THEN 1 END) as MalfunctionCount,
                    COUNT(*) as TotalCount,
                    ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) as Rank
                FROM TB_ALARM_DATA ad
                INNER JOIN TB_HNS_RESOURCE h ON ad.HNSIDX = h.HNSIDX
                INNER JOIN TB_OBS_INFO o ON h.OBSIDX = o.OBSIDX
                INNER JOIN TB_AREA_INFO a ON o.AREAIDX = a.AREAIDX
                WHERE ad.ALADT >= DATEADD(month, -1, GETDATE())
                GROUP BY a.AREANM
                ORDER BY TotalCount DESC";

            yield return StartCoroutine(ExecuteApiRequest("SELECT", monthlyQuery, result =>
            {
                try
                {
                    var monthlyStats = JsonConvert.DeserializeObject<List<AlarmStatistics>>(result) ?? new List<AlarmStatistics>();
                    MonthlyAlarmStats.SetItemsWithoutNotify(monthlyStats);
                    MonthlyAlarmStats.NotifyCurrentCollection();
                    Debug.Log($"[DataService] 월간 통계 로드 완료: {monthlyStats.Count}개");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataService] 월간 통계 로드 실패: {ex.Message}");
                }
            }));

            // 연간 통계
            string yearlyQuery = monthlyQuery.Replace("DATEADD(month, -1, GETDATE())", "DATEADD(year, -1, GETDATE())");

            yield return StartCoroutine(ExecuteApiRequest("SELECT", yearlyQuery, result =>
            {
                try
                {
                    var yearlyStats = JsonConvert.DeserializeObject<List<AlarmStatistics>>(result) ?? new List<AlarmStatistics>();
                    YearlyAlarmStats.SetItemsWithoutNotify(yearlyStats);
                    YearlyAlarmStats.NotifyCurrentCollection();
                    Debug.Log($"[DataService] 연간 통계 로드 완료: {yearlyStats.Count}개");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataService] 연간 통계 로드 실패: {ex.Message}");
                }
            }));
        }

        /// <summary>
        /// 관측소 데이터 로드
        /// </summary>
        private IEnumerator LoadObservationStations()
        {
            string query = "SELECT OBSIDX as obsidx, OBSNM as obsName, AREANM as areaName FROM TB_OBS_INFO o INNER JOIN TB_AREA_INFO a ON o.AREAIDX = a.AREAIDX";

            yield return StartCoroutine(ExecuteApiRequest("SELECT", query, result =>
            {
                try
                {
                    var stations = JsonConvert.DeserializeObject<List<ObservationStationData>>(result) ?? new List<ObservationStationData>();
                    ObservationStations.SetItemsWithoutNotify(stations);
                    ObservationStations.NotifyCurrentCollection();
                    Debug.Log($"[DataService] 관측소 데이터 로드 완료: {stations.Count}개");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataService] 관측소 데이터 로드 실패: {ex.Message}");
                }
            }));
        }

        /// <summary>
        /// 지역 데이터 로드
        /// </summary>
        private IEnumerator LoadAreas()
        {
            string query = "SELECT AREAIDX as areaId, AREANM as areaName FROM TB_AREA_INFO";

            yield return StartCoroutine(ExecuteApiRequest("SELECT", query, result =>
            {
                try
                {
                    var areas = JsonConvert.DeserializeObject<List<AreaData>>(result) ?? new List<AreaData>();
                    Areas.SetItemsWithoutNotify(areas);
                    Areas.NotifyCurrentCollection();
                    Debug.Log($"[DataService] 지역 데이터 로드 완료: {areas.Count}개");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataService] 지역 데이터 로드 실패: {ex.Message}");
                }
            }));
        }

        #endregion

        #region 알람 감지 로직

        /// <summary>
        /// 새로운 알람 감지
        /// </summary>
        private void DetectNewAlarms(List<AlarmData> currentAlarms)
        {
            var existingIds = _previousAlarms.Select(a => a.alaidx).ToHashSet();
            var newAlarms = currentAlarms.Where(a => !existingIds.Contains(a.alaidx)).ToList();

            if (newAlarms.Count > 0)
            {
                Debug.Log($"[DataService] 새 알람 {newAlarms.Count}개 감지!");

                foreach (var alarm in newAlarms)
                {
                    Debug.Log($"[DataService] 새 알람: {alarm.areaNm} - {alarm.obsName} - {alarm.hnsName} (코드: {alarm.alacode})");
                }
            }
        }

        #endregion

        #region API 통신

        /// <summary>
        /// API 요청 실행 (기존 UiManager 방식)
        /// </summary>
        private IEnumerator ExecuteApiRequest(string sqlType, string query, System.Action<string> callback)
        {
            var data = new
            {
                SQLType = sqlType,
                SQLquery = query
            };

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            byte[] jsonToSend = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request = new UnityWebRequest(_apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback(request.downloadHandler.text);
                }
                else
                {
                    string errorMsg = $"Error: {request.error}";
                    Debug.LogError($"[DataService] API 요청 실패: {errorMsg}");
                    callback(errorMsg);
                }
            }
        }

        #endregion

        #region 공개 메서드 (ViewModel에서 호출)

        /// <summary>
        /// 수동으로 데이터 새로고침
        /// </summary>
        public void RefreshAllData()
        {
            if (!_isInitialized) return;

            Debug.Log("[DataService] 수동 데이터 새로고침 시작");
            StartCoroutine(LoadInitialData());
        }

        /// <summary>
        /// 특정 알람 확인 처리
        /// </summary>
        public void AcknowledgeAlarm(int alarmId)
        {
            StartCoroutine(ExecuteApiRequest("UPDATE",
                $"UPDATE TB_ALARM_DATA SET TURNOFF_FLAG = 1, TURNOFF_DT = GETDATE() WHERE ALAIDX = {alarmId}",
                result =>
                {
                    if (!result.Contains("Error:"))
                    {
                        Debug.Log($"[DataService] 알람 확인 처리 완료: {alarmId}");
                        // 즉시 알람 목록 새로고침
                        StartCoroutine(LoadActiveAlarms());
                    }
                    else
                    {
                        Debug.LogError($"[DataService] 알람 확인 처리 실패: {result}");
                    }
                }));
        }

        #endregion

        private void OnDestroy()
        {
            StopAllSchedulers();
        }

        #region Unity Inspector 헬퍼

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_apiUrl))
            {
                Debug.LogWarning("[DataService] API URL이 설정되지 않았습니다.");
            }
        }

        #endregion
    }
}