using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Models.MonitorB;
using ViewModels.MonitorB;
using Core;

namespace Views.MonitorB
{
    /// <summary>
    /// Monitor B 센서 모니터링 View
    /// ✅ Area3DViewModel 구독 제거 (SensorMonitorViewModel만 구독)
    /// </summary>
    public class SensorView : BaseView
    {
        [Header("Title")]
        [SerializeField] private TMP_Text txtLocationInfo;

        [Header("Grid Containers")]
        [SerializeField] private Transform gridToxin;
        [SerializeField] private Transform gridWaterQuality;
        [SerializeField] private Transform gridChemicals;

        [Header("Chart View")]
        [SerializeField] private SensorChartView sensorChartView;

        private List<SensorItemView> toxinItems = new List<SensorItemView>();
        private List<SensorItemView> wqItems = new List<SensorItemView>();
        private List<SensorItemView> chemicalItems = new List<SensorItemView>();

        private int currentObsId = -1;
        private string currentAreaName = "";
        private string currentObsName = "";

        #region BaseView 추상 메서드 구현

        protected override void InitializeUIComponents()
        {
            bool isValid = ValidateComponents(
                (txtLocationInfo, "txtLocationInfo"),
                (gridToxin, "gridToxin"),
                (gridWaterQuality, "gridWaterQuality"),
                (gridChemicals, "gridChemicals")
            );

            if (!isValid)
            {
                LogError("일부 컴포넌트가 Inspector에서 연결되지 않았습니다!");
                return;
            }

            CollectPoolItems();
        }

        protected override void SetupViewEvents()
        {
            foreach (var item in toxinItems)
            {
                if (item != null)
                    item.OnItemClicked += OnSensorItemClicked;
            }

            foreach (var item in wqItems)
            {
                if (item != null)
                    item.OnItemClicked += OnSensorItemClicked;
            }

            foreach (var item in chemicalItems)
            {
                if (item != null)
                    item.OnItemClicked += OnSensorItemClicked;
            }
        }

        protected override void ConnectToViewModel()
        {
            // ✅ SensorMonitorViewModel만 구독
            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.OnSensorsLoaded += OnSensorsLoaded;
                SensorMonitorViewModel.Instance.OnError += OnError;
                LogInfo("SensorMonitorViewModel 이벤트 구독 완료");
            }
            else
            {
                LogWarning("SensorMonitorViewModel.Instance가 null입니다.");
            }

            // ❌ Area3DViewModel 구독 제거 (SensorMonitorViewModel이 이미 구독 중)
        }

        protected override void DisconnectViewEvents()
        {
            foreach (var item in toxinItems)
            {
                if (item != null)
                    item.OnItemClicked -= OnSensorItemClicked;
            }

            foreach (var item in wqItems)
            {
                if (item != null)
                    item.OnItemClicked -= OnSensorItemClicked;
            }

            foreach (var item in chemicalItems)
            {
                if (item != null)
                    item.OnItemClicked -= OnSensorItemClicked;
            }
        }

        protected override void DisconnectFromViewModel()
        {
            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.OnSensorsLoaded -= OnSensorsLoaded;
                SensorMonitorViewModel.Instance.OnError -= OnError;
                LogInfo("SensorMonitorViewModel 이벤트 구독 해제 완료");
            }

            // ❌ Area3DViewModel 구독 해제 제거
        }

        #endregion

        #region Object Pool 초기화

        private void CollectPoolItems()
        {
            if (gridToxin != null)
            {
                toxinItems.Clear();
                foreach (Transform child in gridToxin)
                {
                    var itemView = child.GetComponent<SensorItemView>();
                    if (itemView != null)
                    {
                        toxinItems.Add(itemView);
                    }
                }
                LogInfo($"독성 센서 아이템 {toxinItems.Count}개 수집");
            }

            if (gridWaterQuality != null)
            {
                wqItems.Clear();
                foreach (Transform child in gridWaterQuality)
                {
                    var itemView = child.GetComponent<SensorItemView>();
                    if (itemView != null)
                    {
                        wqItems.Add(itemView);
                    }
                }
                LogInfo($"수질 센서 아이템 {wqItems.Count}개 수집");
            }

            if (gridChemicals != null)
            {
                chemicalItems.Clear();
                foreach (Transform child in gridChemicals)
                {
                    var itemView = child.GetComponent<SensorItemView>();
                    if (itemView != null)
                    {
                        chemicalItems.Add(itemView);
                    }
                }
                LogInfo($"화학물질 센서 아이템 {chemicalItems.Count}개 수집");
            }
        }

        #endregion

        #region 공개 메서드

        public void LoadObservatory(int obsId, string areaName, string obsName)
        {
            currentObsId = obsId;
            currentAreaName = areaName;
            currentObsName = obsName;

            UpdateTitle(areaName, obsName);

            LogInfo($"관측소 {obsId} 센서 데이터 로드 시작");

            // ✅ SensorMonitorViewModel이 자동으로 Area3DViewModel 구독하고 있음
            // View는 그냥 타이틀만 업데이트
        }

        public void RefreshData()
        {
            if (SensorMonitorViewModel.Instance != null && currentObsId > 0)
            {
                SensorMonitorViewModel.Instance.RefreshSensors();
            }
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        private void OnSensorsLoaded(List<SensorInfoData> sensors)
        {
            LogInfo($"센서 데이터 수신: {sensors.Count}개");

            RenderToxinSensors();
            RenderWaterQualitySensors();
            RenderChemicalSensors();

            // ⭐ 기본 차트 자동 로드 (독성도)
            LoadDefaultChart();
        }

        private void OnError(string errorMessage)
        {
            LogError($"에러: {errorMessage}");
        }

        #endregion

        #region 타이틀 업데이트

        private void UpdateTitle(string areaName, string obsName)
        {
            if (txtLocationInfo != null)
            {
                txtLocationInfo.text = $"{areaName} - {obsName} 실시간 상태";
                LogInfo($"타이틀 업데이트: {txtLocationInfo.text}");
            }
        }

        #endregion

        #region 차트 로드

        private void LoadDefaultChart()
        {
            if (sensorChartView == null)
            {
                LogWarning("SensorChartView가 연결되지 않았습니다!");
                return;
            }

            if (SensorMonitorViewModel.Instance == null || currentObsId <= 0)
            {
                LogWarning("SensorMonitorViewModel 또는 ObsId가 유효하지 않습니다!");
                return;
            }

            var toxinSensors = SensorMonitorViewModel.Instance.ToxinSensors;
            if (toxinSensors != null && toxinSensors.Count > 0)
            {
                var firstToxin = toxinSensors[0];
                LogInfo($"기본 차트 로드: {firstToxin.sensorName}");

                sensorChartView.gameObject.SetActive(true);
                sensorChartView.LoadSensorChart(
                    currentObsId,
                    firstToxin.boardIdx,
                    firstToxin.hnsIdx,
                    firstToxin.sensorName
                );
            }
            else
            {
                LogWarning("독성 센서 데이터가 없습니다!");
            }
        }

        #endregion

        #region 렌더링 메서드

        private void RenderToxinSensors()
        {
            if (SensorMonitorViewModel.Instance == null) return;

            var sensors = SensorMonitorViewModel.Instance.ToxinSensors;
            RenderSensorGrid(sensors, toxinItems, "독성");
        }

        private void RenderWaterQualitySensors()
        {
            if (SensorMonitorViewModel.Instance == null) return;

            var sensors = SensorMonitorViewModel.Instance.WaterQualitySensors;
            RenderSensorGrid(sensors, wqItems, "수질");
        }

        private void RenderChemicalSensors()
        {
            if (SensorMonitorViewModel.Instance == null) return;

            var sensors = SensorMonitorViewModel.Instance.ChemicalSensors;
            RenderSensorGrid(sensors, chemicalItems, "화학물질");
        }

        private void RenderSensorGrid(
            List<SensorInfoData> sensors,
            List<SensorItemView> itemPool,
            string gridName)
        {
            if (itemPool == null || itemPool.Count == 0)
            {
                LogWarning($"{gridName} 아이템 풀이 비어있습니다!");
                return;
            }

            for (int i = 0; i < sensors.Count && i < itemPool.Count; i++)
            {
                itemPool[i].gameObject.SetActive(true);
                itemPool[i].SetData(sensors[i], currentObsId);
            }

            for (int i = sensors.Count; i < itemPool.Count; i++)
            {
                itemPool[i].gameObject.SetActive(false);
            }

            LogInfo($"{gridName} 센서 {sensors.Count}개 렌더링 완료");
        }

        #endregion

        #region 센서 클릭 핸들러

        private void OnSensorItemClicked(SensorInfoData sensorData)
        {
            LogInfo($"센서 클릭: {sensorData.sensorName}");

            if (sensorChartView != null)
            {
                sensorChartView.gameObject.SetActive(true);
                sensorChartView.LoadSensorChart(
                    currentObsId,
                    sensorData.boardIdx,
                    sensorData.hnsIdx,
                    sensorData.sensorName
                );
            }
        }

        #endregion

        #region 로깅

        private void LogInfo(string message)
        {
            Debug.Log($"[SensorView] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SensorView] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SensorView] {message}");
        }

        #endregion
    }
}