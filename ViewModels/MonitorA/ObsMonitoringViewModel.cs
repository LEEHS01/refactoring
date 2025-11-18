using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Onthesys;
using Models.MonitorA;
using Repositories.MonitorA;
using ChartDataModel = Models.MonitorA.ChartDataModel;  // ⭐ Alias 추가

namespace ViewModels.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 ViewModel
    /// Monitor A/B 표준 패턴: MonoBehaviour Singleton
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

            Debug.Log("[ObsMonitoringViewModel] 초기화 완료");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Repository
        private ObsMonitoringRepository _repository;
        #endregion

        #region Current Data
        private int _currentObsId = -1;
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
        /// <summary>
        /// 관측소 모니터링 데이터 로드
        /// </summary>
        public void LoadMonitoringData(int obsId)
        {
            Debug.Log($"[ObsMonitoringViewModel] 데이터 로드 시작: ObsId={obsId}");

            _currentObsId = obsId;
            StartCoroutine(LoadDataCoroutine(obsId));
        }

        /// <summary>
        /// 실시간 센서 값 업데이트 (6초 주기)
        /// </summary>
        public void UpdateSensorValues(int obsId)
        {
            if (_currentObsId != obsId)
            {
                Debug.LogWarning($"[ObsMonitoringViewModel] ObsId 불일치: Current={_currentObsId}, Requested={obsId}");
                return;
            }

            StartCoroutine(UpdateValuesCoroutine(obsId));
        }

        /// <summary>
        /// 차트 데이터 갱신 (10분 주기용)
        /// </summary>
        public void RefreshChartData(int obsId)
        {
            if (_currentObsId != obsId)
            {
                Debug.LogWarning($"[ObsMonitoringViewModel] ObsId 불일치");
                return;
            }

            StartCoroutine(RefreshChartDataCoroutine(obsId));
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void ClearData()
        {
            _currentObsId = -1;
            Debug.Log("[ObsMonitoringViewModel] 데이터 초기화");
        }

        /// <summary>
        /// 현재 관측소 ID 반환
        /// </summary>
        public int GetCurrentObsId()
        {
            return _currentObsId;
        }
        #endregion

        #region Private Coroutines
        /// <summary>
        /// 데이터 로드 코루틴 - 4개 동시 호출
        /// </summary>
        private IEnumerator LoadDataCoroutine(int obsId)
        {
            List<ToxinData> toxinDataList = null;
            List<CurrentDataModel> currentValues = null;
            List<ChartDataModel> chartDataList = null;  // ⭐ 추가
            int sensorStep = 5;

            bool toxinLoaded = false;
            bool currentLoaded = false;
            bool chartLoaded = false;  // ⭐ 추가
            bool stepLoaded = false;

            string errorMsg = null;

            // ⭐ 4개 코루틴 동시 시작
            StartCoroutine(_repository.GetToxinData(
                obsId,
                data =>
                {
                    toxinDataList = data;
                    toxinLoaded = true;
                },
                error =>
                {
                    errorMsg = $"센서 정보 로드 실패: {error}";
                    toxinLoaded = true;
                }
            ));

            StartCoroutine(_repository.GetToxinValueLast(
                obsId,
                data =>
                {
                    currentValues = data;
                    currentLoaded = true;
                },
                error =>
                {
                    errorMsg = $"측정값 로드 실패: {error}";
                    currentLoaded = true;
                }
            ));

            // ⭐ 차트 데이터 로드 추가
            DateTime endTime = DateTime.Now;
            endTime = new DateTime(endTime.Year, endTime.Month, endTime.Day,
                                   endTime.Hour, (endTime.Minute / 10) * 10, 0);
            DateTime startTime = endTime.AddHours(-12);

            StartCoroutine(_repository.GetChartValue(
                obsId,
                startTime,
                endTime,
                10,  // 10분 간격
                data =>
                {
                    chartDataList = data;
                    chartLoaded = true;
                },
                error =>
                {
                    Debug.LogWarning($"차트 데이터 로드 실패 (빈 배열 사용): {error}");
                    chartDataList = new List<ChartDataModel>();
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
                    Debug.LogWarning($"센서 진행 상태 로드 실패 (기본값 사용): {error}");
                    stepLoaded = true;
                }
            ));

            // ⭐ 모든 코루틴 완료 대기 (4개)
            yield return new WaitUntil(() => toxinLoaded && currentLoaded && chartLoaded && stepLoaded);

            // 에러 처리
            if (errorMsg != null)
            {
                Debug.LogError($"[ObsMonitoringViewModel] {errorMsg}");
                OnError?.Invoke(errorMsg);
                yield break;
            }

            // ⭐ 차트 데이터 병합
            MergeChartData(toxinDataList, chartDataList);

            // 데이터 병합 및 이벤트 발생
            ProcessAndEmitData(toxinDataList, currentValues, sensorStep);
        }

        /// <summary>
        /// 실시간 값 업데이트 코루틴
        /// </summary>
        private IEnumerator UpdateValuesCoroutine(int obsId)
        {
            List<CurrentDataModel> currentValues = null;
            bool loaded = false;

            yield return _repository.GetToxinValueLast(
                obsId,
                data =>
                {
                    currentValues = data;
                    loaded = true;
                },
                error =>
                {
                    Debug.LogError($"[ObsMonitoringViewModel] 실시간 값 업데이트 실패: {error}");
                    loaded = true;
                }
            );

            yield return new WaitUntil(() => loaded);

            if (currentValues == null) yield break;

            // TODO: 실시간 업데이트는 기존 데이터와 병합 필요
            // 현재는 간단히 재로드로 구현
            LoadMonitoringData(obsId);
        }

        /// <summary>
        /// 차트 데이터 갱신 코루틴 (10분 주기)
        /// </summary>
        private IEnumerator RefreshChartDataCoroutine(int obsId)
        {
            List<ToxinData> toxinDataList = null;
            List<ChartDataModel> chartDataList = null;
            bool toxinLoaded = false;
            bool chartLoaded = false;

            // 1. 현재 ToxinData 가져오기 (설정 정보)
            yield return _repository.GetToxinData(
                obsId,
                data =>
                {
                    toxinDataList = data;
                    toxinLoaded = true;
                },
                error =>
                {
                    Debug.LogError($"차트 갱신 실패: {error}");
                    toxinLoaded = true;
                }
            );

            // 2. 차트 데이터 로드
            DateTime endTime = DateTime.Now;
            endTime = new DateTime(endTime.Year, endTime.Month, endTime.Day,
                                   endTime.Hour, (endTime.Minute / 10) * 10, 0);
            DateTime startTime = endTime.AddHours(-12);

            yield return _repository.GetChartValue(
                obsId,
                startTime,
                endTime,
                10,
                data =>
                {
                    chartDataList = data;
                    chartLoaded = true;
                },
                error =>
                {
                    Debug.LogWarning($"차트 데이터 로드 실패: {error}");
                    chartDataList = new List<ChartDataModel>();
                    chartLoaded = true;
                }
            );

            yield return new WaitUntil(() => toxinLoaded && chartLoaded);

            // 3. 차트 데이터만 병합
            MergeChartData(toxinDataList, chartDataList);

            Debug.Log("[ObsMonitoringViewModel] 차트 데이터 갱신 완료 (10분 주기)");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 차트 데이터를 ToxinData.values에 병합
        /// </summary>
        private void MergeChartData(List<ToxinData> toxinDataList, List<ChartDataModel> chartDataList)
        {
            if (chartDataList == null || chartDataList.Count == 0)
            {
                Debug.LogWarning("[ObsMonitoringViewModel] 차트 데이터가 없습니다.");
                return;
            }

            Debug.Log($"[ObsMonitoringViewModel] 차트 데이터 병합 시작: {chartDataList.Count}개");

            foreach (var toxin in toxinDataList)
            {
                // 해당 센서의 차트 데이터 필터링
                var sensorChartData = chartDataList
                    .Where(c => c.boardidx == toxin.boardid && c.hnsidx == toxin.hnsid)
                    .OrderBy(c => c.obsdt)
                    .Select(c => c.val)
                    .ToList();

                // values 설정
                toxin.values.Clear();
                toxin.values.AddRange(sensorChartData);

                Debug.Log($"[ObsMonitoringViewModel] {toxin.hnsName}: values={toxin.values.Count}개 병합");
            }
        }

        /// <summary>
        /// 데이터 병합 및 이벤트 발생
        /// </summary>
        private void ProcessAndEmitData(
            List<ToxinData> toxinDataList,
            List<CurrentDataModel> currentValues,
            int sensorStep)
        {
            if (toxinDataList == null || toxinDataList.Count == 0)
            {
                Debug.LogWarning("[ObsMonitoringViewModel] ToxinData가 비어있습니다.");
                OnError?.Invoke("센서 데이터가 없습니다.");
                return;
            }

            // 센서 데이터 병합
            var allSensors = MergeSensorData(toxinDataList, currentValues, sensorStep);

            // 카테고리별 분류
            var toxinList = allSensors.Where(s => s.BoardId == 1).ToList();
            var chemicalList = allSensors.Where(s => s.BoardId == 2 && s.HnsId <= 19).ToList();
            var qualityList = allSensors.Where(s => s.BoardId == 3 && s.HnsId <= 7).ToList();

            Debug.Log($"[ObsMonitoringViewModel] 데이터 분류 완료: Toxin={toxinList.Count}, Chemical={chemicalList.Count}, Quality={qualityList.Count}");

            // 이벤트 발생
            OnToxinLoaded?.Invoke(toxinList);
            OnChemicalLoaded?.Invoke(chemicalList);
            OnQualityLoaded?.Invoke(qualityList);

            // 보드 에러 상태 업데이트
            UpdateBoardErrors(toxinList, chemicalList, qualityList);
        }

        /// <summary>
        /// ToxinData와 CurrentData 병합
        /// </summary>
        private List<SensorItemData> MergeSensorData(
            List<ToxinData> toxinDataList,
            List<CurrentDataModel> currentValues,
            int sensorStep)
        {
            var result = new List<SensorItemData>();

            Debug.Log($"[ObsMonitoringViewModel] 데이터 병합 시작: ToxinData={toxinDataList?.Count ?? 0}개, CurrentValues={currentValues?.Count ?? 0}개");

            foreach (var toxin in toxinDataList)
            {
                // 해당 센서의 최신 측정값 찾기
                var current = currentValues?.FirstOrDefault(c =>
                    c.boardidx == toxin.boardid && c.hnsidx == toxin.hnsid);

                var itemData = new SensorItemData
                {
                    BoardId = toxin.boardid,
                    HnsId = toxin.hnsid,
                    HnsName = toxin.hnsName,
                    Unit = toxin.unit ?? "",
                    Serious = toxin.serious,
                    Warning = toxin.warning,
                    IsActive = toxin.on,
                    IsFixing = toxin.fix,
                    CurrentValue = current?.val ?? 0f,
                    StateCode = current?.stcd ?? "00",
                    Status = CalculateStatus(toxin, current, sensorStep),
                    Values = toxin.values != null ? new List<float>(toxin.values) : new List<float>()  // ⭐ 트렌드 차트용 데이터
                };

                Debug.Log($"[ObsMonitoringViewModel] 센서 병합: {itemData.HnsName}");
                Debug.Log($"  - CurrentValue: {itemData.CurrentValue} (current={(current != null ? "O" : "X")})");
                Debug.Log($"  - Status: {itemData.Status}");
                Debug.Log($"  - Values: {itemData.Values.Count}개");

                result.Add(itemData);
            }

            return result;
        }

        /// <summary>
        /// 센서 상태 계산 (⭐ stcd 체크 제거)
        /// </summary>
        private ToxinStatus CalculateStatus(ToxinData toxin, CurrentDataModel current, int sensorStep)
        {
            // 비활성화 또는 점검 중
            if (!toxin.on || toxin.fix)
                return ToxinStatus.Purple;

            // 측정값이 없으면 설비이상
            if (current == null)
                return ToxinStatus.Purple;

            // ⭐ stcd 체크 제거 (GET_CURRENT_TOXI에 stcd 컬럼 없음)
            // if (current.stcd != "00")
            //     return ToxinStatus.Purple;

            // ⭐ 임계값 비교 (warning > serious)
            // 경보(Red): warning 임계값 초과
            if (toxin.warning > 0 && current.val >= toxin.warning)
                return ToxinStatus.Red;

            // 경계(Yellow): serious 임계값 초과
            if (toxin.serious > 0 && current.val >= toxin.serious)
                return ToxinStatus.Yellow;

            return ToxinStatus.Green;
        }

        /// <summary>
        /// 보드별 에러 상태 업데이트
        /// </summary>
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