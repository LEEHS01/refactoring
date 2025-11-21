using HNS.Common.Models;
using HNS.Services;
using Models.MonitorA;
using Models.MonitorB;
using Repositories.MonitorA;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using ViewModels.MonitorB;

namespace ViewModels.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 ViewModel
    /// ✅ SensorMonitorViewModel 데이터 재사용
    /// ✅ 스케줄러 이벤트만 구독
    /// ✅ 독성도 0 초과 시 무조건 Yellow
    /// ✅ 설비이상은 알람 데이터로 판정
    /// ✅ 관측소 전체 상태 Lamp 업데이트
    /// </summary>
    public class ObsMonitoringViewModel : MonoBehaviour
    {
        #region Singleton
        public static ObsMonitoringViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _repository = new ObsMonitoringRepository();

            SubscribeToScheduler();

            Debug.Log("[ObsMonitoringViewModel] 초기화 완료");
        }

        private void OnDestroy()
        {
            UnsubscribeFromScheduler();

            if (AlarmLogViewModel.Instance != null)
            {
                AlarmLogViewModel.Instance.OnAlarmSelected.RemoveListener(OnAlarmSelected);
            }

            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.OnSensorsLoaded -= OnSensorMonitorLoaded;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Repository & Services
        private ObsMonitoringRepository _repository;
        private SchedulerService _schedulerService;
        #endregion

        #region Current Data
        private int _currentObsId = -1;
        private bool _isActive = false;
        #endregion

        #region Unity Events
        [Serializable]
        public class SensorListEvent : UnityEvent<List<SensorItemData>> { }

        [Serializable]
        public class BoardErrorEvent : UnityEvent<string, bool> { }

        [Serializable]
        public class ObservatoryStatusEvent : UnityEvent<ToxinStatus> { }

        [Serializable]
        public class ErrorEvent : UnityEvent<string> { }

        [Header("Unity Events")]
        public SensorListEvent OnToxinLoaded = new SensorListEvent();
        public SensorListEvent OnChemicalLoaded = new SensorListEvent();
        public SensorListEvent OnQualityLoaded = new SensorListEvent();
        public BoardErrorEvent OnBoardErrorChanged = new BoardErrorEvent();
        public ObservatoryStatusEvent OnObservatoryStatusChanged = new ObservatoryStatusEvent();  // ⭐ 추가
        public ErrorEvent OnError = new ErrorEvent();
        #endregion

        private void Start()
        {
            if (AlarmLogViewModel.Instance != null)
            {
                AlarmLogViewModel.Instance.OnAlarmSelected.AddListener(OnAlarmSelected);
                Debug.Log("[ObsMonitoringViewModel] ✅ AlarmLogViewModel 구독 완료");
            }

            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.OnSensorsLoaded += OnSensorMonitorLoaded;
                Debug.Log("[ObsMonitoringViewModel] ✅ SensorMonitorViewModel 구독 완료 (데이터 재사용)");
            }
        }

        private void OnSensorMonitorLoaded(List<SensorInfoData> sensors)
        {
            if (!_isActive || _currentObsId <= 0) return;

            if (SensorMonitorViewModel.Instance.CurrentObsId != _currentObsId)
            {
                Debug.LogWarning($"[ObsMonitoringViewModel] ObsId 불일치: Current={_currentObsId}, SensorMonitor={SensorMonitorViewModel.Instance.CurrentObsId}");
                return;
            }

            Debug.Log($"[ObsMonitoringViewModel] ✅ SensorMonitorViewModel 데이터 재사용: {sensors.Count}개");

            StartCoroutine(LoadChartDataOnly(_currentObsId, sensors));
        }

        private void OnAlarmSelected(int obsId)
        {
            Debug.Log($"[ObsMonitoringViewModel] ✅ Monitor B 알람 선택 감지 → ObsId={obsId}");

            if (_isActive)
            {
                LoadMonitoringData(obsId);
            }
        }

        #region Public Methods
        public void LoadMonitoringData(int obsId)
        {
            Debug.Log($"[ObsMonitoringViewModel] 데이터 로드 시작: ObsId={obsId}");
            _currentObsId = obsId;
            _isActive = true;

            if (SensorMonitorViewModel.Instance != null &&
                SensorMonitorViewModel.Instance.CurrentObsId == obsId)
            {
                var sensors = SensorMonitorViewModel.Instance.AllSensors;
                if (sensors != null && sensors.Count > 0)
                {
                    Debug.Log($"[ObsMonitoringViewModel] ✅ SensorMonitorViewModel 데이터 재사용: {sensors.Count}개");
                    StartCoroutine(LoadChartDataOnly(obsId, sensors));
                    return;
                }
            }

            StartCoroutine(LoadDataCoroutine(obsId));
        }

        public void UpdateSensorValues(int obsId)
        {
            if (_currentObsId != obsId)
            {
                Debug.LogWarning($"[ObsMonitoringViewModel] ObsId 불일치");
                return;
            }
            LoadMonitoringData(obsId);
        }

        public void RefreshChartData(int obsId)
        {
            if (_currentObsId != obsId)
            {
                Debug.LogWarning($"[ObsMonitoringViewModel] ObsId 불일치");
                return;
            }
            StartCoroutine(RefreshChartDataCoroutine(obsId));
        }

        public void ClearData()
        {
            _currentObsId = -1;
            _isActive = false;
            Debug.Log("[ObsMonitoringViewModel] 데이터 초기화");
        }

        public int GetCurrentObsId()
        {
            return _currentObsId;
        }
        #endregion

        #region Scheduler Integration
        private void SubscribeToScheduler()
        {
            _schedulerService = FindObjectOfType<SchedulerService>();

            if (_schedulerService != null)
            {
                _schedulerService.OnDataSyncTriggered += OnDataSyncTriggered;
                _schedulerService.OnAlarmDetected += OnAlarmDetected;
                _schedulerService.OnAlarmCancelled += OnAlarmCancelled;

                Debug.Log("[ObsMonitoringViewModel] 스케줄러 이벤트 구독 완료");
            }
            else
            {
                Debug.LogWarning("[ObsMonitoringViewModel] SchedulerService를 찾을 수 없습니다!");
            }
        }

        private void UnsubscribeFromScheduler()
        {
            if (_schedulerService != null)
            {
                _schedulerService.OnDataSyncTriggered -= OnDataSyncTriggered;
                _schedulerService.OnAlarmDetected -= OnAlarmDetected;
                _schedulerService.OnAlarmCancelled -= OnAlarmCancelled;
                Debug.Log("[ObsMonitoringViewModel] 스케줄러 이벤트 구독 해제");
            }
        }

        private void OnDataSyncTriggered()
        {
            if (_isActive && _currentObsId > 0)
            {
                Debug.Log($"[ObsMonitoringViewModel] 10분 주기 - SensorMonitorViewModel에게 새로고침 요청");

                if (SensorMonitorViewModel.Instance != null)
                {
                    SensorMonitorViewModel.Instance.RefreshSensors();
                }
            }
        }

        private void OnAlarmDetected()
        {
            if (_isActive && _currentObsId > 0)
            {
                Debug.Log($"[ObsMonitoringViewModel] 알람 발생 - SensorMonitorViewModel에게 새로고침 요청");

                if (SensorMonitorViewModel.Instance != null)
                {
                    SensorMonitorViewModel.Instance.RefreshSensors();
                }
            }
        }

        private void OnAlarmCancelled()
        {
            if (_isActive && _currentObsId > 0)
            {
                Debug.Log($"[ObsMonitoringViewModel] 알람 해제 - SensorMonitorViewModel에게 새로고침 요청");

                if (SensorMonitorViewModel.Instance != null)
                {
                    SensorMonitorViewModel.Instance.RefreshSensors();
                }
            }
        }
        #endregion

        #region Private Coroutines
        private IEnumerator LoadChartDataOnly(int obsId, List<SensorInfoData> sensorInfo)
        {
            List<ChartDataModel> chartData = null;
            int sensorStep = 5;
            bool chartLoaded = false;
            bool stepLoaded = false;

            DateTime endTime = DateTime.Now;
            endTime = new DateTime(endTime.Year, endTime.Month, endTime.Day,
                                   endTime.Hour, (endTime.Minute / 10) * 10, 0);
            DateTime startTime = endTime.AddHours(-12);

            StartCoroutine(_repository.GetChartValue(
                obsId, startTime, endTime, 10,
                data =>
                {
                    chartData = data;
                    chartLoaded = true;
                },
                error =>
                {
                    Debug.LogWarning($"차트 데이터 로드 실패: {error}");
                    chartData = new List<ChartDataModel>();
                    chartLoaded = true;
                }
            ));

            StartCoroutine(_repository.GetSensorStep(
                obsId,
                step =>
                {
                    sensorStep = step;
                    stepLoaded = true;
                },
                error =>
                {
                    Debug.LogWarning($"센서 진행 상태 로드 실패: {error}");
                    stepLoaded = true;
                }
            ));

            yield return new WaitUntil(() => chartLoaded && stepLoaded);

            Debug.Log($"[ObsMonitoringViewModel] ✅ 차트 데이터 로드 완료 (SensorInfo 재사용)");

            var sensorDataList = ConvertToSensorData(sensorInfo, chartData, sensorStep);
            ProcessAndEmitData(sensorDataList);
        }

        private IEnumerator LoadDataCoroutine(int obsId)
        {
            List<SensorInfoData> sensorInfo = null;
            List<ChartDataModel> chartData = null;
            int sensorStep = 5;

            bool sensorLoaded = false;
            bool chartLoaded = false;
            bool stepLoaded = false;

            string errorMsg = null;

            StartCoroutine(_repository.GetSensorInfo(
                obsId,
                data =>
                {
                    sensorInfo = data;
                    sensorLoaded = true;
                },
                error =>
                {
                    errorMsg = $"센서 정보 로드 실패: {error}";
                    sensorLoaded = true;
                }
            ));

            DateTime endTime = DateTime.Now;
            endTime = new DateTime(endTime.Year, endTime.Month, endTime.Day,
                                   endTime.Hour, (endTime.Minute / 10) * 10, 0);
            DateTime startTime = endTime.AddHours(-12);

            StartCoroutine(_repository.GetChartValue(
                obsId, startTime, endTime, 10,
                data =>
                {
                    chartData = data;
                    chartLoaded = true;
                },
                error =>
                {
                    Debug.LogWarning($"차트 데이터 로드 실패: {error}");
                    chartData = new List<ChartDataModel>();
                    chartLoaded = true;
                }
            ));

            StartCoroutine(_repository.GetSensorStep(
                obsId,
                step =>
                {
                    sensorStep = step;
                    stepLoaded = true;
                },
                error =>
                {
                    Debug.LogWarning($"센서 진행 상태 로드 실패: {error}");
                    stepLoaded = true;
                }
            ));

            yield return new WaitUntil(() => sensorLoaded && chartLoaded && stepLoaded);

            if (errorMsg != null)
            {
                Debug.LogError($"[ObsMonitoringViewModel] {errorMsg}");
                OnError?.Invoke(errorMsg);
                yield break;
            }

            var sensorDataList = ConvertToSensorData(sensorInfo, chartData, sensorStep);
            ProcessAndEmitData(sensorDataList);
        }

        private IEnumerator RefreshChartDataCoroutine(int obsId)
        {
            List<SensorInfoData> sensorInfo = null;
            List<ChartDataModel> chartData = null;
            bool sensorLoaded = false;
            bool chartLoaded = false;

            yield return _repository.GetSensorInfo(
                obsId,
                data =>
                {
                    sensorInfo = data;
                    sensorLoaded = true;
                },
                error =>
                {
                    Debug.LogError($"차트 갱신 실패: {error}");
                    sensorLoaded = true;
                }
            );

            DateTime endTime = DateTime.Now;
            endTime = new DateTime(endTime.Year, endTime.Month, endTime.Day,
                                   endTime.Hour, (endTime.Minute / 10) * 10, 0);
            DateTime startTime = endTime.AddHours(-12);

            yield return _repository.GetChartValue(
                obsId, startTime, endTime, 10,
                data =>
                {
                    chartData = data;
                    chartLoaded = true;
                },
                error =>
                {
                    Debug.LogWarning($"차트 데이터 로드 실패: {error}");
                    chartData = new List<ChartDataModel>();
                    chartLoaded = true;
                }
            );

            yield return new WaitUntil(() => sensorLoaded && chartLoaded);

            Debug.Log("[ObsMonitoringViewModel] 차트 데이터 갱신 완료");
        }
        #endregion

        #region Private Methods
        private List<SensorItemData> ConvertToSensorData(
            List<SensorInfoData> sensorInfo,
            List<ChartDataModel> chartData,
            int sensorStep)
        {
            var result = new List<SensorItemData>();

            Debug.Log($"[ObsMonitoringViewModel] 데이터 변환: SensorInfo={sensorInfo.Count}, Chart={chartData.Count}");

            foreach (var sensor in sensorInfo)
            {
                var chartValues = chartData
                    .Where(c => c.boardidx == sensor.BOARDIDX && c.hnsidx == sensor.HNSIDX)
                    .OrderBy(c => c.obsdt)
                    .Select(c => c.val)
                    .ToList();

                var itemData = new SensorItemData
                {
                    BoardId = sensor.BOARDIDX,
                    HnsId = sensor.HNSIDX,
                    HnsName = sensor.HNSNM,
                    Unit = sensor.UNIT ?? "",
                    Serious = sensor.HI,
                    Warning = sensor.HIHI,
                    IsActive = sensor.USEYN?.Trim() == "1",
                    IsFixing = sensor.INSPECTIONFLAG?.Trim() == "1",
                    CurrentValue = sensor.VAL,
                    StateCode = "00",
                    Status = CalculateStatus(sensor, sensorStep),
                    Values = chartValues
                };

                result.Add(itemData);
            }

            return result;
        }

        /// <summary>
        /// ⭐⭐⭐ 센서 상태 계산
        /// - 독성도(Board=1)는 0 초과면 무조건 Yellow
        /// - 설비이상은 알람 데이터로 판정 (여기서는 기본 계산만)
        /// </summary>
        private ToxinStatus CalculateStatus(SensorInfoData sensor, int sensorStep)
        {
            // ⭐ 독성도(BoardId=1)는 0 초과면 무조건 경계(Yellow) 이상
            if (sensor.BOARDIDX == 1)
            {
                float val = sensor.VAL;
                float hihi = sensor.HIHI;
                float hi = sensor.HI;

                // 경보값 체크
                if (hihi > 0 && val >= hihi)
                {
                    return ToxinStatus.Red;
                }

                // 경계값 체크
                if (hi > 0 && val >= hi)
                {
                    return ToxinStatus.Yellow;
                }

                // ⭐⭐⭐ 독성도는 0 초과면 무조건 Yellow
                if (val > 0)
                {
                    return ToxinStatus.Yellow;
                }

                // 0 이하면 정상
                return ToxinStatus.Green;
            }

            // ⭐ 독성도 외 센서 (화학물질, 수질)
            float sensorVal = sensor.VAL;
            float sensorHihi = sensor.HIHI;
            float sensorHi = sensor.HI;

            if (sensorHihi > 0 && sensorVal >= sensorHihi)
            {
                return ToxinStatus.Red;
            }

            if (sensorHi > 0 && sensorVal >= sensorHi)
            {
                return ToxinStatus.Yellow;
            }

            return ToxinStatus.Green;
        }

        private void ProcessAndEmitData(List<SensorItemData> allSensors)
        {
            if (allSensors == null || allSensors.Count == 0)
            {
                Debug.LogWarning("[ObsMonitoringViewModel] SensorData가 비어있습니다.");
                OnError?.Invoke("센서 데이터가 없습니다.");
                return;
            }

            var toxinList = allSensors.Where(s => s.BoardId == 1).ToList();
            var chemicalList = allSensors.Where(s => s.BoardId == 2 && s.HnsId <= 19).ToList();
            var qualityList = allSensors.Where(s => s.BoardId == 3 && s.HnsId <= 7).ToList();

            qualityList.Reverse();

            Debug.Log($"[ObsMonitoringViewModel] 데이터 분류: Toxin={toxinList.Count}, Chemical={chemicalList.Count}, Quality={qualityList.Count}");

            OnToxinLoaded?.Invoke(toxinList);
            OnChemicalLoaded?.Invoke(chemicalList);
            OnQualityLoaded?.Invoke(qualityList);

            UpdateBoardAndObservatoryStatus(toxinList, chemicalList, qualityList);
        }

        /// <summary>
        /// ⭐⭐⭐ 보드별 Legend 색상 + 관측소 전체 Lamp 색상 업데이트
        /// </summary>
        private void UpdateBoardAndObservatoryStatus(
            List<SensorItemData> toxinList,
            List<SensorItemData> chemicalList,
            List<SensorItemData> qualityList)
        {
            // 1. 보드별 상태 계산 (우선순위: Purple > Red > Yellow > Green)
            ToxinStatus toxinStatus = GetBoardStatus(toxinList, 1);
            ToxinStatus chemicalStatus = GetBoardStatus(chemicalList, 2);
            ToxinStatus qualityStatus = GetBoardStatus(qualityList, 3);

            // 2. 보드별 Legend 색상 업데이트 (설비이상만 빨간색으로 표시)
            OnBoardErrorChanged?.Invoke("toxin", toxinStatus == ToxinStatus.Purple);
            OnBoardErrorChanged?.Invoke("chemical", chemicalStatus == ToxinStatus.Purple);
            OnBoardErrorChanged?.Invoke("quality", qualityStatus == ToxinStatus.Purple);

            // 3. ⭐⭐⭐ 관측소 전체 상태 = 가장 높은 우선순위 상태
            ToxinStatus observatoryStatus = GetHighestStatus(toxinStatus, chemicalStatus, qualityStatus);

            OnObservatoryStatusChanged?.Invoke(observatoryStatus);

            Debug.Log($"[ObsMonitoringViewModel] 보드 상태: Toxin={toxinStatus}, Chemical={chemicalStatus}, Quality={qualityStatus}");
            Debug.Log($"[ObsMonitoringViewModel] 관측소 전체 상태: {observatoryStatus}");
        }

        /// <summary>
        /// ⭐⭐⭐ 보드 전체 상태 계산 (설비이상은 알람 데이터로 판정)
        /// Purple(설비이상) > Red(경보) > Yellow(경계) > Green(정상)
        /// </summary>
        private ToxinStatus GetBoardStatus(List<SensorItemData> sensors, int boardId)
        {
            if (sensors == null || sensors.Count == 0)
            {
                return ToxinStatus.Green;
            }

            // ⭐⭐⭐ 설비이상은 알람 데이터(status=0)로 판정!
            bool hasPurpleAlarm = false;
            if (AlarmLogViewModel.Instance != null)
            {
                var activeAlarms = AlarmLogViewModel.Instance.AllLogs?
                    .Where(log => !log.isCancelled && log.obsId == _currentObsId)
                    .ToList();

                if (activeAlarms != null)
                {
                    // 이 보드에 status=0 (설비이상) 알람이 있는지 확인
                    hasPurpleAlarm = activeAlarms.Any(a => a.boardId == boardId && a.status == 0);
                }
            }

            if (hasPurpleAlarm)
            {
                return ToxinStatus.Purple;
            }

            // 센서 측정값 기반 상태 (Red > Yellow > Green)
            if (sensors.Any(s => s.Status == ToxinStatus.Red))
            {
                return ToxinStatus.Red;
            }

            if (sensors.Any(s => s.Status == ToxinStatus.Yellow))
            {
                return ToxinStatus.Yellow;
            }

            return ToxinStatus.Green;
        }

        /// <summary>
        /// ⭐⭐⭐ 가장 높은 우선순위 상태 반환
        /// </summary>
        private ToxinStatus GetHighestStatus(params ToxinStatus[] statuses)
        {
            // Purple(설비이상) > Red(경보) > Yellow(경계) > Green(정상)
            if (statuses.Any(s => s == ToxinStatus.Purple))
                return ToxinStatus.Purple;

            if (statuses.Any(s => s == ToxinStatus.Red))
                return ToxinStatus.Red;

            if (statuses.Any(s => s == ToxinStatus.Yellow))
                return ToxinStatus.Yellow;

            return ToxinStatus.Green;
        }
        #endregion
    }
}