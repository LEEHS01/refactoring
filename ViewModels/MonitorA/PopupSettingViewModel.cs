using HNS.Common.Models;
using HNS.Common.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Models.MonitorB;

namespace HNS.Common.ViewModels
{
    /// <summary>
    /// 환경설정 팝업 ViewModel (Singleton)
    /// </summary>
    public class PopupSettingViewModel : MonoBehaviour
    {
        #region Singleton

        public static PopupSettingViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LogInfo("Singleton 등록 완료");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Events

        // 탭 관련
        public event Action<SettingTabType> OnTabChanged;

        // 지역/관측소 목록
        public event Action<List<AreaData>> OnAreasLoaded;
        public event Action<List<ObservatoryData>> OnObservatoriesLoaded;

        // 관측소 설정 데이터
        public event Action<ObservatorySettingData> OnObservatorySettingsLoaded;

        // 시스템 설정
        public event Action<SystemSettingData> OnSystemSettingsLoaded;

        // 업데이트 완료
        public event Action<string> OnUpdateSuccess;
        public event Action<string> OnError;

        // ⭐⭐⭐ 센서 표시/숨김 이벤트 추가
        /// <summary>
        /// 센서 표시/숨김 변경 이벤트
        /// (obsIdx, boardIdx, hnsIdx, isVisible)
        /// </summary>
        public event Action<int, int, int, bool> OnSensorVisibilityChanged;

        #endregion

        #region Private Fields

        private SettingsRepository _repository;
        private int _currentObsId = -1;
        private SettingTabType _currentTab = SettingTabType.Observatory;
        private ObservatorySettingData _cachedObsSettings;
        private SystemSettingData _systemSettings;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _repository = new SettingsRepository();
            LoadSystemSettings();

            LogInfo("ViewModel 초기화 완료 - 지역 목록 로드 시작");
        }

        #endregion

        #region Public Methods - 팝업 열기/닫기

        public void OpenSettings(int obsId = 0)
        {
            _currentObsId = obsId;

            if (obsId > 0)
            {
                LogInfo($"관측소 설정 열기: ObsId={obsId}");
                SwitchTab(SettingTabType.Observatory);
                StartCoroutine(LoadObservatorySettingsCoroutine(obsId));
            }
            else
            {
                LogInfo("전체 설정 열기 - 기본값: 인천, 지역1");
                SwitchTab(SettingTabType.Observatory);
                StartCoroutine(LoadDefaultSettingsCoroutine());
            }
        }

        private IEnumerator LoadDefaultSettingsCoroutine()
        {
            LogInfo("지역 목록 로드 시작");

            bool isComplete = false;
            List<AreaData> areas = null;

            yield return _repository.GetAllAreas(
                (data) => { areas = data; isComplete = true; },
                (error) => { OnError?.Invoke(error); isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (areas != null && areas.Count > 0)
            {
                OnAreasLoaded?.Invoke(areas);
                LogInfo($"지역 목록 로드 완료: {areas.Count}개");

                int incheonAreaId = 1;
                StartCoroutine(LoadObservatoriesByAreaCoroutine(incheonAreaId));
            }
        }

        public void SwitchTab(SettingTabType tab)
        {
            _currentTab = tab;
            OnTabChanged?.Invoke(tab);
            LogInfo($"탭 전환: {tab}");
        }

        #endregion

        #region Public Methods - 지역/관측소 선택

        public void SelectArea(int areaId)
        {
            LogInfo($"지역 선택: AreaId={areaId}");
            StartCoroutine(LoadObservatoriesByAreaCoroutine(areaId));
        }

        public void SelectObservatory(int obsId)
        {
            _currentObsId = obsId;
            LogInfo($"관측소 선택: ObsId={obsId}");
            StartCoroutine(LoadObservatorySettingsCoroutine(obsId));
        }

        #endregion

        #region Public Methods - 설정 업데이트

        public void UpdateToxinWarning(float warningValue)
        {
            if (_currentObsId <= 0) return;

            LogInfo($"독성도 경고값 변경: {warningValue}");
            StartCoroutine(UpdateToxinWarningCoroutine(warningValue));
        }

        public void ToggleBoardFixing(int boardId, bool isFixed)
        {
            if (_currentObsId <= 0) return;

            LogInfo($"보드 고정 변경: BoardId={boardId}, Fixed={isFixed}");
            StartCoroutine(UpdateBoardFixingCoroutine(boardId, isFixed));
        }

        /// <summary>
        /// ⭐⭐⭐ 센서 사용 토글 - 이벤트 발생 추가
        /// </summary>
        public void ToggleSensorUsing(int boardId, int sensorId, bool isUsing)
        {
            if (_currentObsId <= 0)
            {
                LogError("관측소가 선택되지 않았습니다!");
                return;
            }

            LogInfo($"센서 사용 변경: BoardId={boardId}, SensorId={sensorId}, Using={isUsing}");
            StartCoroutine(UpdateSensorUsingCoroutine(boardId, sensorId, isUsing));
        }

        public void UpdateCctvUrl(CctvType cctvType, string url)
        {
            if (_currentObsId <= 0) return;

            LogInfo($"CCTV URL 변경: Type={cctvType}, URL={url}");
            StartCoroutine(UpdateCctvUrlCoroutine(cctvType, url));
        }

        public void ChangeAlarmThreshold(ToxinStatus threshold)
        {
            _systemSettings.AlarmThreshold = threshold;
            PlayerPrefs.SetInt("AlarmThreshold", (int)threshold);

            OnSystemSettingsLoaded?.Invoke(_systemSettings);
            LogInfo($"알람 임계값 변경: {threshold}");
        }

        public void ChangeDatabaseUrl(string url)
        {
            _systemSettings.DatabaseUrl = url;
            PlayerPrefs.SetString("DatabaseUrl", url);

            LogInfo($"DB URL 변경: {url}");
        }

        public IEnumerator UpdateSensorThreshold(
            int obsId, int boardId, int sensorId,
            string columnName, float value)
        {
            LogInfo($"센서 임계값 변경: ObsId={obsId}, BoardId={boardId}, SensorId={sensorId}, {columnName}={value}");

            bool isComplete = false;

            yield return _repository.UpdateSensorThreshold(
                obsId, boardId, sensorId, columnName, value,
                () => {
                    OnUpdateSuccess?.Invoke($"센서 임계값이 업데이트되었습니다. ({columnName})");
                    isComplete = true;
                },
                (error) => {
                    OnError?.Invoke($"센서 임계값 업데이트 실패: {error}");
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;
        }

        #endregion

        #region Private Coroutines - 데이터 로드

        private IEnumerator LoadAreasCoroutine()
        {
            LogInfo("지역 목록 로드 시작");

            bool isComplete = false;
            List<AreaData> areas = null;

            yield return _repository.GetAllAreas(
                (data) => { areas = data; isComplete = true; },
                (error) => { OnError?.Invoke(error); isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (areas != null && areas.Count > 0)
            {
                OnAreasLoaded?.Invoke(areas);
                LogInfo($"지역 목록 로드 완료: {areas.Count}개");
            }
        }

        private IEnumerator LoadObservatoriesByAreaCoroutine(int areaId)
        {
            LogInfo($"관측소 목록 로드 시작: AreaId={areaId}");

            bool isComplete = false;
            List<ObservatoryData> observatories = null;

            yield return _repository.GetObservatoriesByArea(
                areaId,
                (data) => { observatories = data; isComplete = true; },
                (error) => { OnError?.Invoke(error); isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (observatories != null && observatories.Count > 0)
            {
                OnObservatoriesLoaded?.Invoke(observatories);
                LogInfo($"관측소 목록 로드 완료: {observatories.Count}개");
            }
        }

        private IEnumerator LoadObservatorySettingsCoroutine(int obsId)
        {
            LogInfo($"관측소 설정 로드 시작: ObsId={obsId}");

            bool isComplete = false;
            ObservatorySettingData settings = null;

            yield return _repository.GetObservatorySettings(
                obsId,
                (data) => { settings = data; isComplete = true; },
                (error) => { OnError?.Invoke(error); isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (settings == null)
            {
                OnError?.Invoke("관측소 설정을 불러올 수 없습니다.");
                yield break;
            }

            isComplete = false;
            yield return _repository.GetObservatoryWithCctv(
                obsId,
                (obsModel) =>
                {
                    settings.CctvEquipmentUrl = obsModel.IN_CCTVURL ?? "";
                    settings.CctvOutdoorUrl = obsModel.OUT_CCTVURL ?? "";

                    LogInfo($"CCTV URL 로드 완료:");
                    LogInfo($"  - 설비: {settings.CctvEquipmentUrl}");
                    LogInfo($"  - 실외: {settings.CctvOutdoorUrl}");

                    isComplete = true;
                },
                (error) =>
                {
                    LogWarning($"CCTV URL 로드 실패: {error}");
                    settings.CctvEquipmentUrl = "";
                    settings.CctvOutdoorUrl = "";
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            _cachedObsSettings = settings;
            OnObservatorySettingsLoaded?.Invoke(settings);
            LogInfo("관측소 설정 로드 완료");
        }

        private void LoadSystemSettings()
        {
            _systemSettings = new SystemSettingData
            {
                AlarmThreshold = (ToxinStatus)PlayerPrefs.GetInt("AlarmThreshold", 1),
                DatabaseUrl = PlayerPrefs.GetString("DatabaseUrl", "http://192.168.1.20:2000/")
            };

            OnSystemSettingsLoaded?.Invoke(_systemSettings);
            LogInfo("시스템 설정 로드 완료");
        }

        #endregion

        #region Private Coroutines - 업데이트

        private IEnumerator UpdateToxinWarningCoroutine(float warningValue)
        {
            bool isComplete = false;

            yield return _repository.UpdateToxinWarning(
                _currentObsId,
                warningValue,
                () => {
                    OnUpdateSuccess?.Invoke("독성도 경고값이 업데이트되었습니다.");
                    isComplete = true;
                },
                (error) => {
                    OnError?.Invoke($"독성도 경고값 업데이트 실패: {error}");
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;
        }

        private IEnumerator UpdateBoardFixingCoroutine(int boardId, bool isFixed)
        {
            bool isComplete = false;

            yield return _repository.UpdateBoardFixing(
                _currentObsId,
                boardId,
                isFixed,
                () => {
                    OnUpdateSuccess?.Invoke("보드 고정 상태가 업데이트되었습니다.");
                    isComplete = true;
                },
                (error) => {
                    OnError?.Invoke($"보드 고정 업데이트 실패: {error}");
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;
        }

        /// <summary>
        /// ⭐⭐⭐ 센서 사용 업데이트 - 이벤트 발생
        /// </summary>
        private IEnumerator UpdateSensorUsingCoroutine(int boardId, int sensorId, bool isUsing)
        {
            bool isComplete = false;

            yield return _repository.UpdateSensorUsing(
                _currentObsId,
                boardId,
                sensorId,
                isUsing,
                () => {
                    // ⭐ DB 업데이트 성공 후 이벤트 발생
                    OnSensorVisibilityChanged?.Invoke(_currentObsId, boardId, sensorId, isUsing);

                    OnUpdateSuccess?.Invoke("센서 사용 여부가 업데이트되었습니다.");
                    LogInfo($"✅ 센서 표시/숨김 이벤트 발생: Obs={_currentObsId}, Board={boardId}, Sensor={sensorId}, Visible={isUsing}");

                    isComplete = true;
                },
                (error) => {
                    OnError?.Invoke($"센서 사용 업데이트 실패: {error}");
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            // ⭐ 설정 다시 로드 (UI 동기화)
            yield return LoadObservatorySettingsCoroutine(_currentObsId);
        }

        private IEnumerator UpdateCctvUrlCoroutine(CctvType cctvType, string url)
        {
            bool isComplete = false;

            yield return _repository.UpdateCctvUrl(
                _currentObsId,
                cctvType,
                url,
                () => {
                    OnUpdateSuccess?.Invoke("CCTV URL이 업데이트되었습니다.");
                    isComplete = true;
                },
                (error) => {
                    OnError?.Invoke($"CCTV URL 업데이트 실패: {error}");
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;
        }

        #endregion

        #region Properties

        public int CurrentObsId => _currentObsId;
        public SettingTabType CurrentTab => _currentTab;
        public ObservatorySettingData CachedObsSettings => _cachedObsSettings;
        public SystemSettingData SystemSettings => _systemSettings;

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[PopupSettingViewModel] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[PopupSettingViewModel] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PopupSettingViewModel] {message}");
        }

        #endregion
    }

    public enum SettingTabType
    {
        Observatory,
        System
    }
}