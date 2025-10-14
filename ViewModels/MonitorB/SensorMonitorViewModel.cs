using System;
using System.Collections.Generic;
using UnityEngine;
using Models.MonitorB;
using Repositories.MonitorB;
using Core;

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
        private List<SensorInfoData> toxinSensors = new List<SensorInfoData>();      // Board 1
        private List<SensorInfoData> chemicalSensors = new List<SensorInfoData>();   // Board 2
        private List<SensorInfoData> waterQualitySensors = new List<SensorInfoData>(); // Board 3

        // 프로퍼티
        public List<SensorInfoData> AllSensors => allSensors;
        public List<SensorInfoData> ToxinSensors => toxinSensors;
        public List<SensorInfoData> ChemicalSensors => chemicalSensors;
        public List<SensorInfoData> WaterQualitySensors => waterQualitySensors;

        private int currentObsId = -1;

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

        #endregion

        #region Public 메서드

        /// <summary>
        /// 관측소별 센서 데이터 로드
        /// </summary>
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
                // Repository를 통해 데이터 조회
                var sensors = await SensorRepository.Instance.GetSensorsByObservatoryAsync(obsId);

                if (sensors == null || sensors.Count == 0)
                {
                    LogWarning($"관측소 {obsId}의 센서 데이터가 없습니다.");
                    ClearAllSensors();
                    OnSensorsLoaded?.Invoke(allSensors);
                    return;
                }

                LogInfo($"센서 데이터 로드 성공: {sensors.Count}개");

                // Board별 분류
                ClassifySensorsByBoard(sensors);

                // 이벤트 발생
                OnSensorsLoaded?.Invoke(allSensors);
            }
            catch (Exception ex)
            {
                LogError($"센서 데이터 로드 실패: {ex.Message}");
                OnError?.Invoke($"센서 데이터 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 센서 데이터 새로고침
        /// </summary>
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

        /// <summary>
        /// 센서 데이터를 Board별로 분류
        /// </summary>
        private void ClassifySensorsByBoard(List<SensorInfoData> sensors)
        {
            LogInfo("Board별 센서 분류 시작...");

            // 초기화
            allSensors.Clear();
            toxinSensors.Clear();
            chemicalSensors.Clear();
            waterQualitySensors.Clear();

            // 전체 센서 저장
            allSensors.AddRange(sensors);

            // Board별 분류
            foreach (var sensor in sensors)
            {
                Debug.Log($"[센서 분류] {sensor.sensorName} - Board: {sensor.boardIdx}, HNS: {sensor.hnsIdx}, 값: {sensor.currentValue}, 단위: {sensor.unit}");


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

        /// <summary>
        /// 모든 센서 데이터 초기화
        /// </summary>
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