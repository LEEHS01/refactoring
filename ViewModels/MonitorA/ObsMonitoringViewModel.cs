using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Onthesys;
using Models.MonitorA;
using Repositories.MonitorA;
using HNS.Services;

namespace ViewModels.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 ViewModel
    /// ✅ GET_SENSOR_INFO 통합 프로시저 사용 (Monitor B와 동일)
    /// ✅ 스케줄러 이벤트 구독: 10분 동기화, 알람 발생/해제
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
            // ⭐ 스케줄러 이벤트 구독 해제
            UnsubscribeFromScheduler();

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
        private bool _isActive = false;  // ⭐ 화면 활성화 상태
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

        #region Public Methods
        public void LoadMonitoringData(int obsId)
        {
            Debug.Log($"[ObsMonitoringViewModel] 데이터 로드 시작: ObsId={obsId}");
            _currentObsId = obsId;
            _isActive = true;  // ⭐ 화면 활성화
            StartCoroutine(LoadDataCoroutine(obsId));
        }

        public void UpdateSensorValues(int obsId)
        {
            if (_currentObsId != obsId)
            {
                Debug.LogWarning($"[ObsMonitoringViewModel] ObsId 불일치");
                return;
            }
            LoadMonitoringData(obsId);  // ✅ 전체 재로드 (GET_SENSOR_INFO는 빠름)
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
            _isActive = false;  // ⭐ 화면 비활성화
            Debug.Log("[ObsMonitoringViewModel] 데이터 초기화");
        }

        public int GetCurrentObsId()
        {
            return _currentObsId;
        }
        #endregion

        #region Scheduler Integration
        /// <summary>
        /// 스케줄러 이벤트 구독
        /// </summary>
        private void SubscribeToScheduler()
        {
            _schedulerService = FindObjectOfType<SchedulerService>();

            if (_schedulerService != null)
            {
                // ⭐ 10분 주기 데이터 동기화
                _schedulerService.OnDataSyncTriggered += OnDataSyncTriggered;

                // ⭐ 알람 발생 시
                _schedulerService.OnAlarmDetected += OnAlarmDetected;

                // ⭐ 알람 해제 시
                _schedulerService.OnAlarmCancelled += OnAlarmCancelled;

                Debug.Log("[ObsMonitoringViewModel] 스케줄러 이벤트 구독 완료 (10분 동기화, 알람 발생/해제)");
            }
            else
            {
                Debug.LogWarning("[ObsMonitoringViewModel] SchedulerService를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 스케줄러 이벤트 구독 해제
        /// </summary>
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

        /// <summary>
        /// 10분 주기 데이터 동기화
        /// </summary>
        private void OnDataSyncTriggered()
        {
            // ⭐ 화면이 활성화되어 있고, ObsId가 유효할 때만 갱신
            if (_isActive && _currentObsId > 0)
            {
                Debug.Log($"[ObsMonitoringViewModel] 10분 주기 데이터 동기화: ObsId={_currentObsId}");
                UpdateSensorValues(_currentObsId);
            }
        }

        /// <summary>
        /// 알람 발생 시 업데이트
        /// </summary>
        private void OnAlarmDetected()
        {
            // ⭐ 현재 화면이 활성화되어 있을 때만
            if (_isActive && _currentObsId > 0)
            {
                Debug.Log($"[ObsMonitoringViewModel] 알람 발생 업데이트: ObsId={_currentObsId}");
                UpdateSensorValues(_currentObsId);
            }
        }

        /// <summary>
        /// 알람 해제 시 업데이트
        /// </summary>
        private void OnAlarmCancelled()
        {
            // ⭐ 현재 화면이 활성화되어 있을 때만
            if (_isActive && _currentObsId > 0)
            {
                Debug.Log($"[ObsMonitoringViewModel] 알람 해제 업데이트: ObsId={_currentObsId}");
                UpdateSensorValues(_currentObsId);
            }
        }
        #endregion

        #region Private Coroutines
        /// <summary>
        /// ✅ GET_SENSOR_INFO + GET_CHARTVALUE + GET_SENSOR_STEP (3개만 호출)
        /// </summary>
        private IEnumerator LoadDataCoroutine(int obsId)
        {
            List<SensorInfoModelA> sensorInfo = null;
            List<ChartDataModel> chartData = null;
            int sensorStep = 5;

            bool sensorLoaded = false;
            bool chartLoaded = false;
            bool stepLoaded = false;

            string errorMsg = null;

            // 1. GET_SENSOR_INFO (설정 + 현재값 통합!) ⭐
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

            // 3개 완료 대기
            yield return new WaitUntil(() => sensorLoaded && chartLoaded && stepLoaded);

            if (errorMsg != null)
            {
                Debug.LogError($"[ObsMonitoringViewModel] {errorMsg}");
                OnError?.Invoke(errorMsg);
                yield break;
            }

            // ViewModel에서 변환
            var sensorDataList = ConvertToSensorData(sensorInfo, chartData, sensorStep);
            ProcessAndEmitData(sensorDataList);
        }

        private IEnumerator RefreshChartDataCoroutine(int obsId)
        {
            List<SensorInfoModelA> sensorInfo = null;
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
        /// ✅ SensorInfoModelA → SensorItemData 변환 (한 번에!)
        /// </summary>
        private List<SensorItemData> ConvertToSensorData(
            List<SensorInfoModelA> sensorInfo,
            List<ChartDataModel> chartData,
            int sensorStep)
        {
            var result = new List<SensorItemData>();

            Debug.Log($"[ObsMonitoringViewModel] 데이터 변환: SensorInfo={sensorInfo.Count}, Chart={chartData.Count}");

            foreach (var sensor in sensorInfo)
            {
                // 해당 센서의 차트 데이터 (72개)
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
                    Serious = sensor.HI,                    // ✅ 그대로 사용 (9999도 정상값)
                    Warning = sensor.HIHI,                  // ✅ 그대로 사용 (9999도 정상값)
                    IsActive = sensor.USEYN?.Trim() == "1",  // ⭐ Trim()
                    IsFixing = sensor.INSPECTIONFLAG?.Trim() == "1",  // ⭐ Trim()
                    CurrentValue = sensor.VAL ?? 0f,  // ⭐ VAL 직접 사용!
                    StateCode = "00",  // GET_SENSOR_INFO에는 stcd 없음
                    Status = CalculateStatus(sensor, sensorStep),
                    Values = chartValues
                };

                result.Add(itemData);
            }

            return result;
        }

        /// <summary>
        /// 센서 상태 계산
        /// </summary>
        private ToxinStatus CalculateStatus(SensorInfoModelA sensor, int sensorStep)
        {
            bool isActive = sensor.USEYN?.Trim() == "1";
            bool isFixing = sensor.INSPECTIONFLAG?.Trim() == "1";

            // 1. 비활성화 또는 점검 중
            if (!isActive || isFixing)
            {
                Debug.Log($"[CalculateStatus] {sensor.HNSNM} → Purple (isActive={isActive}, isFixing={isFixing})");
                return ToxinStatus.Purple;
            }

            // 2. 측정값 없음
            if (sensor.VAL == null)
            {
                Debug.Log($"[CalculateStatus] {sensor.HNSNM} → Purple (VAL is null)");
                return ToxinStatus.Purple;
            }

            // 3. 임계값 비교 (9999도 정상 임계값으로 사용)
            float hihi = sensor.HIHI;  // ✅ 그대로 사용
            float hi = sensor.HI;      // ✅ 그대로 사용

            if (hihi > 0 && sensor.VAL >= hihi)
            {
                Debug.Log($"[CalculateStatus] {sensor.HNSNM} → Red (val={sensor.VAL} >= hihi={hihi})");
                return ToxinStatus.Red;
            }

            if (hi > 0 && sensor.VAL >= hi)
            {
                Debug.Log($"[CalculateStatus] {sensor.HNSNM} → Yellow (val={sensor.VAL} >= hi={hi})");
                return ToxinStatus.Yellow;
            }

            Debug.Log($"[CalculateStatus] {sensor.HNSNM} → Green (val={sensor.VAL})");
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