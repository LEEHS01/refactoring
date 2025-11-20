using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using Onthesys;
using System;
using System.Collections.Generic;
using UnityEngine;
using AreaDataModel = HNS.MonitorA.Models.AreaData;
using ObsData = HNS.MonitorA.Models.ObsData;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 환경설정 팝업 ViewModel
    /// Repository를 통해 데이터를 가져오고 View에 전달
    /// </summary>
    public class PopupSettingViewModel : MonoBehaviour
    {
        #region Singleton

        public static PopupSettingViewModel Instance { get; private set; }

        #endregion

        #region Repository

        private PopupSettingRepository _repository;

        #endregion

        #region Events

        // 데이터 로드 이벤트
        public event Action<List<AreaDataModel>> OnAreasLoaded;
        public event Action<List<ObsData>> OnObsListLoaded;
        public event Action<ObsSettingData> OnObsSettingLoaded;
        public event Action<List<SensorSettingData>> OnSensorSettingsLoaded;
        public event Action<SystemSettingData> OnSystemSettingLoaded;

        // 상태 이벤트
        public event Action<string> OnError;
        public event Action OnSettingUpdated;

        #endregion

        #region Current Data

        private ObsSettingData _currentObsSetting;
        private SystemSettingData _currentSystemSetting;
        private List<SensorSettingData> _currentSensorSettings;

        #endregion

        #region Properties

        public ObsSettingData CurrentObsSetting => _currentObsSetting;
        public SystemSettingData CurrentSystemSetting => _currentSystemSetting;
        public List<SensorSettingData> CurrentSensorSettings => _currentSensorSettings;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Repository 생성
            _repository = new PopupSettingRepository();

            Debug.Log("[PopupSettingViewModel] Awake 완료");
        }

        private void Start()
        {
            Debug.Log("[PopupSettingViewModel] Start 완료");

            // 시스템 설정 로드
            LoadSystemSetting();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 데이터 로드 메서드 (코루틴 방식)

        /// <summary>
        /// 모든 지역 목록 로드
        /// </summary>
        public void LoadAreas()
        {
            try
            {
                Debug.Log("[PopupSettingViewModel] LoadAreas 시작");

                StartCoroutine(_repository.GetAreas(
                    (areas) =>
                    {
                        if (areas == null || areas.Count == 0)
                        {
                            Debug.LogWarning("[PopupSettingViewModel] 지역 데이터가 없습니다.");
                            OnError?.Invoke("지역 데이터를 불러올 수 없습니다.");
                            return;
                        }

                        Debug.Log($"[PopupSettingViewModel] 지역 {areas.Count}개 로드 완료");
                        OnAreasLoaded?.Invoke(areas);
                    },
                    (error) =>
                    {
                        Debug.LogError($"[PopupSettingViewModel] LoadAreas 실패: {error}");
                        OnError?.Invoke($"지역 목록 로드 실패: {error}");
                    }
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] LoadAreas 실패: {ex.Message}");
                OnError?.Invoke($"지역 목록 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 지역의 관측소 목록 로드
        /// </summary>
        public void LoadObsByArea(int areaId)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] LoadObsByArea 시작 - AreaId:{areaId}");

                StartCoroutine(_repository.GetObsByAreaId(
                    areaId,
                    (obsList) =>
                    {
                        if (obsList == null || obsList.Count == 0)
                        {
                            Debug.LogWarning($"[PopupSettingViewModel] AreaId={areaId}에 해당하는 관측소가 없습니다.");
                            OnError?.Invoke("관측소 데이터를 불러올 수 없습니다.");
                            return;
                        }

                        Debug.Log($"[PopupSettingViewModel] 관측소 {obsList.Count}개 로드 완료");
                        OnObsListLoaded?.Invoke(obsList);
                    },
                    (error) =>
                    {
                        Debug.LogError($"[PopupSettingViewModel] LoadObsByArea 실패: {error}");
                        OnError?.Invoke($"관측소 목록 로드 실패: {error}");
                    }
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] LoadObsByArea 실패: {ex.Message}");
                OnError?.Invoke($"관측소 목록 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 관측소의 설정 정보 로드
        /// </summary>
        public void LoadObsSetting(int obsId)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] LoadObsSetting 시작 - ObsId:{obsId}");

                StartCoroutine(_repository.GetObsSetting(
                    obsId,
                    (setting) =>
                    {
                        if (setting == null)
                        {
                            Debug.LogError($"[PopupSettingViewModel] ObsId={obsId}의 설정을 찾을 수 없습니다.");
                            OnError?.Invoke("관측소 설정을 불러올 수 없습니다.");
                            return;
                        }

                        _currentObsSetting = setting;

                        Debug.Log($"[PopupSettingViewModel] 관측소 설정 로드 완료 - {setting.ObsName}");
                        OnObsSettingLoaded?.Invoke(setting);
                    },
                    (error) =>
                    {
                        Debug.LogError($"[PopupSettingViewModel] LoadObsSetting 실패: {error}");
                        OnError?.Invoke($"관측소 설정 로드 실패: {error}");
                    }
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] LoadObsSetting 실패: {ex.Message}");
                OnError?.Invoke($"관측소 설정 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 관측소의 센서 설정 목록 로드
        /// </summary>
        public void LoadSensorSettings(int obsId)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] LoadSensorSettings 시작 - ObsId:{obsId}");

                StartCoroutine(_repository.GetSensorSettings(
                    obsId,
                    (sensors) =>
                    {
                        if (sensors == null || sensors.Count == 0)
                        {
                            Debug.LogWarning($"[PopupSettingViewModel] ObsId={obsId}의 센서 설정이 없습니다.");
                            OnError?.Invoke("센서 설정을 불러올 수 없습니다.");
                            return;
                        }

                        _currentSensorSettings = sensors;

                        Debug.Log($"[PopupSettingViewModel] 센서 설정 {sensors.Count}개 로드 완료");
                        OnSensorSettingsLoaded?.Invoke(sensors);
                    },
                    (error) =>
                    {
                        Debug.LogError($"[PopupSettingViewModel] LoadSensorSettings 실패: {error}");
                        OnError?.Invoke($"센서 설정 로드 실패: {error}");
                    }
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] LoadSensorSettings 실패: {ex.Message}");
                OnError?.Invoke($"센서 설정 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 시스템 설정 로드
        /// </summary>
        public void LoadSystemSetting()
        {
            try
            {
                Debug.Log("[PopupSettingViewModel] LoadSystemSetting 시작");

                SystemSettingData setting = _repository.GetSystemSetting();

                if (setting == null)
                {
                    Debug.LogError("[PopupSettingViewModel] 시스템 설정을 찾을 수 없습니다.");
                    OnError?.Invoke("시스템 설정을 불러올 수 없습니다.");
                    return;
                }

                _currentSystemSetting = setting;

                Debug.Log("[PopupSettingViewModel] 시스템 설정 로드 완료");
                OnSystemSettingLoaded?.Invoke(setting);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] LoadSystemSetting 실패: {ex.Message}");
                OnError?.Invoke($"시스템 설정 로드 실패: {ex.Message}");
            }
        }

        #endregion

        #region 설정 업데이트 메서드

        /// <summary>
        /// 보드 고정 상태 업데이트
        /// </summary>
        public void UpdateBoardFixing(int obsId, int boardId, bool isFixing)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] UpdateBoardFixing - ObsId:{obsId}, BoardId:{boardId}, IsFixing:{isFixing}");

                bool success = _repository.UpdateBoardFixing(obsId, boardId, isFixing);

                if (success)
                {
                    // 현재 설정 업데이트
                    if (_currentObsSetting != null && _currentObsSetting.ObsId == obsId)
                    {
                        switch (boardId)
                        {
                            case 1:
                                _currentObsSetting.ToxinBoardFixed = isFixing;
                                break;
                            case 2:
                                _currentObsSetting.ChemicalBoardFixed = isFixing;
                                break;
                            case 3:
                                _currentObsSetting.QualityBoardFixed = isFixing;
                                break;
                        }
                    }

                    OnSettingUpdated?.Invoke();
                    Debug.Log("[PopupSettingViewModel] 보드 고정 상태 업데이트 완료");
                }
                else
                {
                    OnError?.Invoke("보드 고정 상태 업데이트 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] UpdateBoardFixing 실패: {ex.Message}");
                OnError?.Invoke($"보드 고정 상태 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 독성도 경보값 업데이트
        /// </summary>
        public void UpdateToxinWarning(int obsId, float warning)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] UpdateToxinWarning - ObsId:{obsId}, Warning:{warning}");

                bool success = _repository.UpdateToxinWarning(obsId, warning);

                if (success)
                {
                    // 현재 설정 업데이트
                    if (_currentObsSetting != null && _currentObsSetting.ObsId == obsId)
                    {
                        _currentObsSetting.ToxinWarning = warning;
                    }

                    OnSettingUpdated?.Invoke();
                    Debug.Log("[PopupSettingViewModel] 독성도 경보값 업데이트 완료");
                }
                else
                {
                    OnError?.Invoke("독성도 경보값 업데이트 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] UpdateToxinWarning 실패: {ex.Message}");
                OnError?.Invoke($"독성도 경보값 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 센서 임계값 업데이트
        /// </summary>
        public void UpdateSensorThreshold(int obsId, int boardId, int hnsId, string field, float value)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] UpdateSensorThreshold - ObsId:{obsId}, BoardId:{boardId}, HnsId:{hnsId}, Field:{field}, Value:{value}");

                bool success = _repository.UpdateSensorThreshold(obsId, boardId, hnsId, field, value);

                if (success)
                {
                    OnSettingUpdated?.Invoke();
                    Debug.Log("[PopupSettingViewModel] 센서 임계값 업데이트 완료");
                }
                else
                {
                    OnError?.Invoke("센서 임계값 업데이트 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] UpdateSensorThreshold 실패: {ex.Message}");
                OnError?.Invoke($"센서 임계값 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// CCTV URL 업데이트
        /// </summary>
        public void UpdateCctvUrl(int obsId, CctvType cctvType, string url)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] UpdateCctvUrl - ObsId:{obsId}, Type:{cctvType}, Url:{url}");

                bool success = _repository.UpdateCctvUrl(obsId, cctvType, url);

                if (success)
                {
                    // 현재 설정 업데이트
                    if (_currentObsSetting != null && _currentObsSetting.ObsId == obsId)
                    {
                        if (cctvType == CctvType.EQUIPMENT)
                            _currentObsSetting.CctvEquipmentUrl = url;
                        else
                            _currentObsSetting.CctvOutdoorUrl = url;
                    }

                    OnSettingUpdated?.Invoke();
                    Debug.Log("[PopupSettingViewModel] CCTV URL 업데이트 완료");
                }
                else
                {
                    OnError?.Invoke("CCTV URL 업데이트 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] UpdateCctvUrl 실패: {ex.Message}");
                OnError?.Invoke($"CCTV URL 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 센서 사용 여부 업데이트
        /// </summary>
        public void UpdateSensorUsing(int obsId, int boardId, int hnsId, bool isUsing)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] UpdateSensorUsing - ObsId:{obsId}, BoardId:{boardId}, HnsId:{hnsId}, IsUsing:{isUsing}");

                bool success = _repository.UpdateSensorUsing(obsId, boardId, hnsId, isUsing);

                if (success)
                {
                    OnSettingUpdated?.Invoke();
                    Debug.Log("[PopupSettingViewModel] 센서 사용 여부 업데이트 완료");
                }
                else
                {
                    OnError?.Invoke("센서 사용 여부 업데이트 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] UpdateSensorUsing 실패: {ex.Message}");
                OnError?.Invoke($"센서 사용 여부 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 알람 임계값 업데이트
        /// </summary>
        public void UpdateAlarmThreshold(ToxinStatus threshold)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] UpdateAlarmThreshold - Threshold:{threshold}");

                bool success = _repository.UpdateAlarmThreshold(threshold);

                if (success)
                {
                    // 현재 설정 업데이트
                    if (_currentSystemSetting != null)
                    {
                        _currentSystemSetting.AlarmThreshold = threshold;
                    }

                    OnSettingUpdated?.Invoke();
                    Debug.Log("[PopupSettingViewModel] 알람 임계값 업데이트 완료");
                }
                else
                {
                    OnError?.Invoke("알람 임계값 업데이트 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] UpdateAlarmThreshold 실패: {ex.Message}");
                OnError?.Invoke($"알람 임계값 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 데이터베이스 URL 업데이트
        /// </summary>
        public void UpdateDatabaseUrl(string url)
        {
            try
            {
                Debug.Log($"[PopupSettingViewModel] UpdateDatabaseUrl - Url:{url}");

                bool success = _repository.UpdateDatabaseUrl(url);

                if (success)
                {
                    // 현재 설정 업데이트
                    if (_currentSystemSetting != null)
                    {
                        _currentSystemSetting.DatabaseUrl = url;
                    }

                    OnSettingUpdated?.Invoke();
                    Debug.Log("[PopupSettingViewModel] 데이터베이스 URL 업데이트 완료");
                }
                else
                {
                    OnError?.Invoke("데이터베이스 URL 업데이트 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingViewModel] UpdateDatabaseUrl 실패: {ex.Message}");
                OnError?.Invoke($"데이터베이스 URL 업데이트 실패: {ex.Message}");
            }
        }

        #endregion
    }
}