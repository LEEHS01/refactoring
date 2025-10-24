using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Models.MonitorB;
using Repositories.MonitorB;

namespace ViewModels.MonitorB
{
    /// <summary>
    /// PopUpToxinDetail2 ViewModel
    /// 실시간 센서 현재값 + 12시간 차트 데이터 로드
    /// </summary>
    public class PopUpToxinDetail2ViewModel : MonoBehaviour
    {
        public static PopUpToxinDetail2ViewModel Instance { get; private set; }

        // 이벤트
        public event Action<SensorInfoData, ChartData> OnDataLoaded;
        public event Action<string> OnError;

        private SensorInfoData currentSensorData;
        private ChartData currentChartData;

        public SensorInfoData CurrentSensorData => currentSensorData;
        public ChartData CurrentChartData => currentChartData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PopUpToxinDetail2ViewModel] 초기화 완료");
        }

        /// <summary>
        /// 센서 상세 데이터 로드 (실시간 현재값 + 12시간 차트)
        /// </summary>
        public async void LoadSensorDetail(int obsId, int boardId, int hnsId)
        {
            if (obsId <= 0 || boardId <= 0 || hnsId <= 0)
            {
                Debug.LogError($"[PopUpToxinDetail2ViewModel] 잘못된 파라미터: obs={obsId}, board={boardId}, hns={hnsId}");
                OnError?.Invoke("잘못된 센서 정보입니다.");
                return;
            }

            Debug.Log($"[PopUpToxinDetail2ViewModel] 데이터 로드 시작: obs={obsId}, board={boardId}, hns={hnsId}");

            try
            {
                // 1. 현재 실시간 센서 데이터 조회
                var allSensors = await SensorRepository.Instance.GetSensorsByObservatoryAsync(obsId);

                var sensorData = allSensors.FirstOrDefault(s =>
                    s.boardIdx == boardId && s.hnsIdx == hnsId);

                if (sensorData == null)
                {
                    Debug.LogWarning($"[PopUpToxinDetail2ViewModel] 센서 데이터 없음: Board={boardId}, HNS={hnsId}");
                    OnError?.Invoke("센서 데이터를 찾을 수 없습니다.");
                    return;
                }

                Debug.Log($"[PopUpToxinDetail2ViewModel] 센서 데이터 조회 완료: {sensorData.sensorName}, 현재값={sensorData.currentValue}");

                // 2. 12시간 차트 데이터 조회
                var chartData = await ChartRepository.Instance.GetSensorChartDataAsync(obsId, boardId, hnsId);

                if (chartData == null)
                {
                    Debug.LogWarning($"[PopUpToxinDetail2ViewModel] 차트 데이터 없음");
                    // 빈 차트 데이터 생성
                    chartData = new ChartData
                    {
                        values = new System.Collections.Generic.List<float>(),
                        timeLabels = new System.Collections.Generic.List<DateTime>(),
                        startTime = DateTime.Now.AddHours(-12),
                        endTime = DateTime.Now,
                        maxValue = 0,
                        minValue = 0
                    };
                }

                Debug.Log($"[PopUpToxinDetail2ViewModel] 차트 데이터 로드 완료: {chartData.values.Count}개");

                currentSensorData = sensorData;
                currentChartData = chartData;

                Debug.Log($"[PopUpToxinDetail2ViewModel] 데이터 로드 완료!");
                OnDataLoaded?.Invoke(sensorData, chartData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopUpToxinDetail2ViewModel] 데이터 로드 실패: {ex.Message}\n{ex.StackTrace}");
                OnError?.Invoke($"데이터 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 정규화된 차트 값 반환 (0~1 범위)
        /// </summary>
        public System.Collections.Generic.List<float> GetNormalizedChartValues()
        {
            if (currentChartData == null || currentChartData.values.Count == 0)
                return new System.Collections.Generic.List<float>();

            float max = currentChartData.maxValue;

            // 최댓값에 +1 여백 추가
            float displayMax = max + 1f;

            if (displayMax <= 0)
            {
                return currentChartData.values.Select(_ => 0f).ToList();
            }

            return currentChartData.values.Select(v => v / displayMax).ToList();
        }

        /// <summary>
        /// 세로축 라벨 값 계산
        /// </summary>
        public System.Collections.Generic.List<float> GetVerticalLabels(int labelCount = 6)
        {
            var labels = new System.Collections.Generic.List<float>();

            if (currentChartData == null)
            {
                for (int i = 0; i < labelCount; i++)
                    labels.Add(0);
                return labels;
            }

            float displayMax = currentChartData.maxValue + 1f;
            float interval = displayMax / (labelCount - 1);

            for (int i = 0; i < labelCount; i++)
            {
                labels.Add(interval * i);
            }

            return labels;
        }
    }
}