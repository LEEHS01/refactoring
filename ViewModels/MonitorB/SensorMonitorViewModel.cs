using System;
using System.Collections.Generic;
using UnityEngine;
using Models.MonitorB;
using Repositories.MonitorB;
using Core;
using HNS.MonitorA.ViewModels;  // ⭐ 추가

namespace ViewModels.MonitorB
{
    /// <summary>
    /// 센서 모니터링 ViewModel
    /// Repository를 통해 센서 데이터 로드 및 Board별 분류
    /// </summary>
    public class SensorMonitorViewModel : MonoBehaviour
    {
        public static SensorMonitorViewModel Instance { get; private set; }

        // 이벤트
        public event Action<List<SensorInfoData>> OnSensorsLoaded;
        public event Action<string> OnError;

        // 전체 센서 데이터
        private List<SensorInfoData> allSensors = new List<SensorInfoData>();

        // Board별 센서 데이터
        private List<SensorInfoData> toxinSensors = new List<SensorInfoData>();
        private List<SensorInfoData> chemicalSensors = new List<SensorInfoData>();
        private List<SensorInfoData> waterQualitySensors = new List<SensorInfoData>();

        // 프로퍼티
        public List<SensorInfoData> AllSensors => allSensors;
        public List<SensorInfoData> ToxinSensors => toxinSensors;
        public List<SensorInfoData> ChemicalSensors => chemicalSensors;
        public List<SensorInfoData> WaterQualitySensors => waterQualitySensors;

        private int currentObsId = -1;
        public int CurrentObsId => currentObsId;

        #region Unity 생명주기

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LogInfo("초기화 완료");
        }

        // ⭐⭐⭐ 추가: Start에서 Area3DViewModel 구독
        private void Start()
        {
            if (Area3DViewModel.Instance != null)
            {
                // ⭐ 새 이벤트 구독!
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.AddListener(OnObservatoryChanged);
                LogInfo("✅ Area3DViewModel 구독 완료");
            }
        }

        // ⭐⭐⭐ 추가: OnDestroy에서 구독 해제
        private void OnDestroy()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.RemoveListener(OnObservatoryChanged);
            }
        }

        // ⭐⭐⭐ 추가: Monitor A에서 관측소 선택 시 호출
        private void OnObservatoryChanged(int obsId, string areaName, string obsName)
        {
            LogInfo($"✅ Monitor A 관측소 선택: ObsId={obsId}, Area={areaName}, Obs={obsName}");
            LoadSensorsByObservatory(obsId);
        }

        #endregion

        #region Public 메서드
        // 기존 코드 그대로...
        public async void LoadSensorsByObservatory(int obsId)
        {
            if (obsId <= 0)
            {
                LogError($"잘못된 obsId: {obsId}");
                OnError?.Invoke("잘못된 관측소 ID입니다.");
                return;
            }

            currentObsId = obsId;
            LogInfo($"관측소 {obsId} 센서 데이터 로드 시작...");

            try
            {
                var sensors = await SensorRepository.Instance.GetSensorsByObservatoryAsync(obsId);

                if (sensors == null || sensors.Count == 0)
                {
                    LogWarning($"관측소 {obsId}의 센서 데이터가 없습니다.");
                    ClearAllSensors();
                    OnSensorsLoaded?.Invoke(allSensors);
                    return;
                }

                LogInfo($"센서 데이터 로드 성공: {sensors.Count}개");

                ClassifySensorsByBoard(sensors);

                OnSensorsLoaded?.Invoke(allSensors);
            }
            catch (Exception ex)
            {
                LogError($"센서 데이터 로드 실패: {ex.Message}");
                OnError?.Invoke($"센서 데이터 로드 실패: {ex.Message}");
            }
        }

        public void RefreshSensors()
        {
            if (currentObsId > 0)
            {
                LoadSensorsByObservatory(currentObsId);
            }
            else
            {
                LogWarning("새로고침할 관측소가 선택되지 않았습니다.");
            }
        }
        #endregion

        #region Private 메서드
        // 기존 코드 그대로...
        private void ClassifySensorsByBoard(List<SensorInfoData> sensors)
        {
            LogInfo("Board별 센서 분류 시작...");

            allSensors.Clear();
            toxinSensors.Clear();
            chemicalSensors.Clear();
            waterQualitySensors.Clear();

            allSensors.AddRange(sensors);

            foreach (var sensor in sensors)
            {
                switch (sensor.boardIdx)
                {
                    case 1:
                        toxinSensors.Add(sensor);
                        break;
                    case 2:
                        chemicalSensors.Add(sensor);
                        break;
                    case 3:
                        waterQualitySensors.Add(sensor);
                        break;
                    default:
                        LogWarning($"알 수 없는 Board: {sensor.boardIdx} (센서: {sensor.sensorName})");
                        break;
                }
            }

            LogInfo($"Board별 분류 완료:");
            LogInfo($"  - 독성(Board 1): {toxinSensors.Count}개");
            LogInfo($"  - 화학물질(Board 2): {chemicalSensors.Count}개");
            LogInfo($"  - 수질(Board 3): {waterQualitySensors.Count}개");
        }

        private void ClearAllSensors()
        {
            allSensors.Clear();
            toxinSensors.Clear();
            chemicalSensors.Clear();
            waterQualitySensors.Clear();
        }
        #endregion

        #region 로깅
        private void LogInfo(string message)
        {
            Debug.Log($"[SensorMonitorViewModel] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SensorMonitorViewModel] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SensorMonitorViewModel] {message}");
        }
        #endregion
    }
}