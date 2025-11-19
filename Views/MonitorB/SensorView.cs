using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Models.MonitorB;
using ViewModels.MonitorB;
using Core;
using HNS.MonitorA.ViewModels;  // ⭐ 추가

namespace Views.MonitorB
{
    public class SensorView : BaseView
    {
        [Header("Title")]
        [SerializeField] private TMP_Text txtLocationInfo;

        [Header("Grid Containers")]
        [SerializeField] private Transform gridToxin;
        [SerializeField] private Transform gridWaterQuality;
        [SerializeField] private Transform gridChemicals;

        [Header("Chart View")]  // ⭐ 추가
        [SerializeField] private SensorChartView sensorChartView;

        private List<SensorItemView> toxinItems = new List<SensorItemView>();
        private List<SensorItemView> wqItems = new List<SensorItemView>();
        private List<SensorItemView> chemicalItems = new List<SensorItemView>();

        private int currentObsId = -1;
        private string currentAreaName = "";  // ⭐ 추가
        private string currentObsName = "";   // ⭐ 추가

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
            // SensorMonitorViewModel 구독
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

            // ⭐⭐⭐ 추가: Area3DViewModel 구독 (새 이벤트!)
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.AddListener(OnMonitorAObservatoryChanged);
                LogInfo("Area3DViewModel 이벤트 구독 완료");
            }
            else
            {
                LogWarning("Area3DViewModel.Instance가 null입니다.");
            }
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

            // ⭐⭐⭐ 추가: Area3DViewModel 구독 해제 (새 이벤트!)
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoadedWithNames.RemoveListener(OnMonitorAObservatoryChanged);
                LogInfo("Area3DViewModel 이벤트 구독 해제 완료");
            }
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
            currentAreaName = areaName;  // ⭐ 저장
            currentObsName = obsName;    // ⭐ 저장

            // ⭐ 타이틀 업데이트
            UpdateTitle(areaName, obsName);

            LogInfo($"관측소 {obsId} 센서 데이터 로드 시작");

            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.LoadSensorsByObservatory(obsId);
            }
            else
            {
                LogError("SensorMonitorViewModel.Instance가 null입니다!");
            }
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

        // ⭐⭐⭐ 추가: Monitor A에서 관측소 선택 시
        private void OnMonitorAObservatoryChanged(int obsId, string areaName, string obsName)
        {
            LogInfo($"✅ Monitor A 관측소 선택 감지 → ObsId={obsId}, Area={areaName}, Obs={obsName}");

            // 1. 타이틀 + 센서 데이터 로드
            LoadObservatory(obsId, areaName, obsName);

            // 2. ⭐⭐⭐ 기본 차트도 자동 로드
            LoadDefaultChart(obsId, areaName, obsName);
        }

        private void OnSensorsLoaded(List<SensorInfoData> sensors)
        {
            LogInfo($"센서 데이터 수신: {sensors.Count}개");

            RenderToxinSensors();
            RenderWaterQualitySensors();
            RenderChemicalSensors();
        }

        private void OnError(string errorMessage)
        {
            LogError($"에러: {errorMessage}");
        }

        #endregion

        #region 타이틀 업데이트

        // ⭐⭐⭐ 추가: 타이틀 업데이트 메서드
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

        // ⭐⭐⭐ 추가: 기본 차트 로드 (독성도)
        private void LoadDefaultChart(int obsId, string areaName, string obsName)
        {
            if (sensorChartView == null)
            {
                LogWarning("SensorChartView가 연결되지 않았습니다! Inspector에서 연결하세요.");
                return;
            }

            const int DEFAULT_BOARD_ID = 1;
            const int DEFAULT_HNS_ID = 1;
            const string DEFAULT_SENSOR_NAME = "독성도";

            LogInfo($"기본 차트 로드: {DEFAULT_SENSOR_NAME} (obsId={obsId})");

            sensorChartView.LoadSensorChart(
        obsId,
        DEFAULT_BOARD_ID,
        DEFAULT_HNS_ID,
        DEFAULT_SENSOR_NAME
    );
        }

        #endregion

        #region 렌더링 메서드 (Object Pooling)

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

            for (int i = 0; i < itemPool.Count; i++)
            {
                if (i < sensors.Count)
                {
                    itemPool[i].gameObject.SetActive(true);
                    itemPool[i].SetData(sensors[i], currentObsId);
                }
                else
                {
                    itemPool[i].gameObject.SetActive(false);
                }
            }

            if (sensors.Count > itemPool.Count)
            {
                LogWarning($"{gridName} 센서가 {sensors.Count}개인데, 아이템은 {itemPool.Count}개만 있습니다!");
            }
        }

        private void OnSensorItemClicked(SensorInfoData sensor)
        {
            LogInfo($"센서 선택: {sensor.sensorName} (Board: {sensor.boardIdx}, HNS: {sensor.hnsIdx})");
        }

        #endregion

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (txtLocationInfo == null)
                LogWarning("txtLocationInfo가 연결되지 않았습니다.");

            if (gridToxin == null || gridWaterQuality == null || gridChemicals == null)
                LogWarning("Grid Container가 모두 연결되지 않았습니다.");
        }
#endif
    }
}