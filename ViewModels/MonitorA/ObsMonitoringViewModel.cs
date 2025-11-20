using HNS.Services;
using Models.MonitorA;
using Models.MonitorB;
using Onthesys;
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
    /// ✅ SensorMonitorViewModel 데이터 재사용 (문제 2 해결)
    /// ✅ 스케줄러 이벤트만 구독 (문제 3 해결)
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

            // ⭐ 스케줄러 이벤트 구독
            SubscribeToScheduler();

            Debug.Log("[ObsMonitoringViewModel] 초기화 완료");
        }

        private void OnDestroy()
        {
            // 스케줄러 이벤트 구독 해제
            UnsubscribeFromScheduler();

            // AlarmLogViewModel 구독 해제
            if (AlarmLogViewModel.Instance != null)
            {
                AlarmLogViewModel.Instance.OnAlarmSelected.RemoveListener(OnAlarmSelected);
            }

            // ⭐⭐⭐ SensorMonitorViewModel 구독 해제
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
        public class ErrorEvent : UnityEvent<string> { }

        [Header("Unity Events")]
        public SensorListEvent OnToxinLoaded = new SensorListEvent();
        public SensorListEvent OnChemicalLoaded = new SensorListEvent();
        public SensorListEvent OnQualityLoaded = new SensorListEvent();
        public BoardErrorEvent OnBoardErrorChanged = new BoardErrorEvent();
        public ErrorEvent OnError = new ErrorEvent();
        #endregion

        private void Start()
        {
            // AlarmLogViewModel 구독
            if (AlarmLogViewModel.Instance != null)
            {
                AlarmLogViewModel.Instance.OnAlarmSelected.AddListener(OnAlarmSelected);
                Debug.Log("[ObsMonitoringViewModel] ✅ AlarmLogViewModel 구독 완료");
            }

            // ⭐⭐⭐ SensorMonitorViewModel 구독 (데이터 재사용)
            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.OnSensorsLoaded += OnSensorMonitorLoaded;
                Debug.Log("[ObsMonitoringViewModel] ✅ SensorMonitorViewModel 구독 완료 (데이터 재사용)");
            }
        }

        // ⭐⭐⭐ SensorMonitorViewModel에서 데이터 로드 완료 시
        private void OnSensorMonitorLoaded(List<SensorInfoData> sensors)
        {
            // 현재 ObsId와 일치하고, 화면이 활성화되어 있을 때만 처리
            if (!_isActive || _currentObsId <= 0) return;

            if (SensorMonitorViewModel.Instance.CurrentObsId != _currentObsId)
            {
                Debug.LogWarning($"[ObsMonitoringViewModel] ObsId 불일치: Current={_currentObsId}, SensorMonitor={SensorMonitorViewModel.Instance.CurrentObsId}");
                return;
            }

            Debug.Log($"[ObsMonitoringViewModel] ✅ SensorMonitorViewModel 데이터 재사용: {sensors.Count}개");

            // ⭐ 차트 데이터만 추가로 로드
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

            // ⭐⭐⭐ SensorMonitorViewModel 데이터가 이미 있으면 재사용
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

            // ⭐ 데이터가 없으면 직접 로드
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

                // ✅ SensorMonitorViewModel에게 새로고침 요청 (OnSensorMonitorLoaded에서 차트 자동 갱신)
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

                // ✅ SensorMonitorViewModel에게 새로고침 요청
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

                // ✅ SensorMonitorViewModel에게 새로고침 요청
                if (SensorMonitorViewModel.Instance != null)
                {
                    SensorMonitorViewModel.Instance.RefreshSensors();
                }
            }
        }
        #endregion

        #region Private Coroutines
        /// <summary>
        /// ⭐⭐⭐ 새 메서드: SensorMonitorViewModel 데이터 + 차트만 로드
        /// </summary>
        private IEnumerator LoadChartDataOnly(int obsId, List<SensorInfoData> sensorInfo)
        {
            List<ChartDataModel> chartData = null;
            int sensorStep = 5;
            bool chartLoaded = false;
            bool stepLoaded = false;

            // 1. GET_CHARTVALUE
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

            // 2. GET_SENSOR_STEP
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

            // ViewModel에서 변환
            var sensorDataList = ConvertToSensorData(sensorInfo, chartData, sensorStep);
            ProcessAndEmitData(sensorDataList);
        }

        /// <summary>
        /// ✅ 직접 로드 (SensorMonitorViewModel 데이터가 없을 때)
        /// </summary>
        private IEnumerator LoadDataCoroutine(int obsId)
        {
            List<SensorInfoData> sensorInfo = null;
            List<ChartDataModel> chartData = null;
            int sensorStep = 5;

            bool sensorLoaded = false;
            bool chartLoaded = false;
            bool stepLoaded = false;

            string errorMsg = null;

            // 1. GET_SENSOR_INFO
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

            // 2. GET_CHARTVALUE
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

            // 3. GET_SENSOR_STEP
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
        /// <summary>
        /// ✅ SensorInfoData → SensorItemData 변환
        /// </summary>
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

        private ToxinStatus CalculateStatus(SensorInfoData sensor, int sensorStep)
        {
            bool isActive = sensor.USEYN?.Trim() == "1";
            bool isFixing = sensor.INSPECTIONFLAG?.Trim() == "1";

            if (!isActive || isFixing)
            {
                return ToxinStatus.Purple;
            }

            // ⭐ VAL이 0이면 Purple (측정 안 됨)
            if (sensor.VAL == 0)
            {
                return ToxinStatus.Purple;
            }

            float hihi = sensor.HIHI;
            float hi = sensor.HI;

            if (hihi > 0 && sensor.VAL >= hihi)
            {
                return ToxinStatus.Red;
            }

            if (hi > 0 && sensor.VAL >= hi)
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

            Debug.Log($"[ObsMonitoringViewModel] 데이터 분류: Toxin={toxinList.Count}, Chemical={chemicalList.Count}, Quality={qualityList.Count}");

            OnToxinLoaded?.Invoke(toxinList);
            OnChemicalLoaded?.Invoke(chemicalList);
            OnQualityLoaded?.Invoke(qualityList);

            UpdateBoardErrors(toxinList, chemicalList, qualityList);
        }

        private void UpdateBoardErrors(
            List<SensorItemData> toxinList,
            List<SensorItemData> chemicalList,
            List<SensorItemData> qualityList)
        {
            bool toxinError = toxinList.Any(s => s.Status == ToxinStatus.Purple);
            bool chemicalError = chemicalList.Any(s => s.Status == ToxinStatus.Purple);
            bool qualityError = qualityList.Any(s => s.Status == ToxinStatus.Purple);

            OnBoardErrorChanged?.Invoke("toxin", toxinError);
            OnBoardErrorChanged?.Invoke("chemical", chemicalError);
            OnBoardErrorChanged?.Invoke("quality", qualityError);

            Debug.Log($"[ObsMonitoringViewModel] 보드 에러: Toxin={toxinError}, Chemical={chemicalError}, Quality={qualityError}");
        }
        #endregion
    }
}