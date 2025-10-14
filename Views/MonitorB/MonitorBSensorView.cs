using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Models.MonitorB;
using ViewModels.MonitorB;
using Core;

namespace Views.MonitorB
{
    /// <summary>
    /// 모니터B 센서 모니터링 메인 View
    /// Object Pooling 방식 사용
    /// </summary>
    public class MonitorBSensorView : BaseView
    {
        [Header("Title")]
        [SerializeField] private TMP_Text txtLocationInfo;

        [Header("Grid Containers")]
        [SerializeField] private Transform gridToxin;
        [SerializeField] private Transform gridWaterQuality;
        [SerializeField] private Transform gridChemicals;

        // ⭐ 미리 만들어진 아이템들 (Object Pool)
        private List<MonitorBSensorItemView> toxinItems = new List<MonitorBSensorItemView>();
        private List<MonitorBSensorItemView> wqItems = new List<MonitorBSensorItemView>();
        private List<MonitorBSensorItemView> chemicalItems = new List<MonitorBSensorItemView>();

        private int currentObsId = -1;

        #region BaseView 추상 메서드 구현

        protected override void InitializeUIComponents()
        {
            // Inspector 연결 검증
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

            // ⭐ Hierarchy에 미리 만들어진 아이템들 찾기
            CollectPoolItems();
        }

        protected override void SetupViewEvents()
        {
            // ⭐ 미리 만들어진 아이템들의 클릭 이벤트 등록
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
            if (SensorMonitorViewModel.Instance != null)
            {
                SensorMonitorViewModel.Instance.OnSensorsLoaded += OnSensorsLoaded;
                SensorMonitorViewModel.Instance.OnError += OnError;
                LogInfo("ViewModel 이벤트 구독 완료");
            }
            else
            {
                LogWarning("SensorMonitorViewModel.Instance가 null입니다.");
            }
        }

        protected override void DisconnectViewEvents()
        {
            // ⭐ 아이템 클릭 이벤트 해제
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
                LogInfo("ViewModel 이벤트 구독 해제 완료");
            }
        }

        #endregion

        #region Object Pool 초기화

        /// <summary>
        /// Hierarchy에서 미리 만들어진 아이템들 수집
        /// </summary>
        private void CollectPoolItems()
        {
            // Grid_Toxin의 자식들 수집
            if (gridToxin != null)
            {
                toxinItems.Clear();
                foreach (Transform child in gridToxin)
                {
                    var itemView = child.GetComponent<MonitorBSensorItemView>();
                    if (itemView != null)
                    {
                        toxinItems.Add(itemView);
                    }
                }
                LogInfo($"독성 센서 아이템 {toxinItems.Count}개 수집");
            }

            // GridWaterQuality의 자식들 수집
            if (gridWaterQuality != null)
            {
                wqItems.Clear();
                foreach (Transform child in gridWaterQuality)
                {
                    var itemView = child.GetComponent<MonitorBSensorItemView>();
                    if (itemView != null)
                    {
                        wqItems.Add(itemView);
                    }
                }
                LogInfo($"수질 센서 아이템 {wqItems.Count}개 수집");
            }

            // GridChemicals의 자식들 수집
            if (gridChemicals != null)
            {
                chemicalItems.Clear();
                foreach (Transform child in gridChemicals)
                {
                    var itemView = child.GetComponent<MonitorBSensorItemView>();
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

        /// <summary>
        /// 관측소 센서 데이터 로드 (외부에서 호출)
        /// </summary>
        public void LoadObservatory(int obsId, string areaName, string obsName)
        {
            currentObsId = obsId;

            if (txtLocationInfo != null)
            {
                txtLocationInfo.text = $"{areaName} - {obsName} 실시간 상태";
            }

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

        /// <summary>
        /// 데이터 새로고침 (외부에서 호출 가능)
        /// </summary>
        public void RefreshData()
        {
            if (SensorMonitorViewModel.Instance != null && currentObsId > 0)
            {
                SensorMonitorViewModel.Instance.RefreshSensors();
            }
        }

        #endregion

        #region ViewModel 이벤트 핸들러

        /// <summary>
        /// 센서 데이터 로드 완료 시
        /// </summary>
        private void OnSensorsLoaded(List<SensorInfoData> sensors)
        {
            LogInfo($"센서 데이터 수신: {sensors.Count}개");

            RenderToxinSensors();
            RenderWaterQualitySensors();
            RenderChemicalSensors();
        }

        /// <summary>
        /// 에러 발생 시
        /// </summary>
        private void OnError(string errorMessage)
        {
            LogError($"에러: {errorMessage}");
            // TODO: 에러 팝업 표시
        }

        #endregion

        #region 렌더링 메서드 (Object Pooling)

        /// <summary>
        /// 독성 센서 렌더링 (Board 1)
        /// </summary>
        private void RenderToxinSensors()
        {
            if (SensorMonitorViewModel.Instance == null) return;

            var sensors = SensorMonitorViewModel.Instance.ToxinSensors;
            RenderSensorGrid(sensors, toxinItems, "독성");
        }

        /// <summary>
        /// 수질 센서 렌더링 (Board 3)
        /// </summary>
        private void RenderWaterQualitySensors()
        {
            if (SensorMonitorViewModel.Instance == null) return;

            var sensors = SensorMonitorViewModel.Instance.WaterQualitySensors;
            RenderSensorGrid(sensors, wqItems, "수질");
        }

        /// <summary>
        /// 화학물질 센서 렌더링 (Board 2)
        /// </summary>
        private void RenderChemicalSensors()
        {
            if (SensorMonitorViewModel.Instance == null) return;

            var sensors = SensorMonitorViewModel.Instance.ChemicalSensors;
            RenderSensorGrid(sensors, chemicalItems, "화학물질");
        }

        /// <summary>
        /// Object Pooling 방식으로 센서 렌더링
        /// </summary>
        private void RenderSensorGrid(
            List<SensorInfoData> sensors,
            List<MonitorBSensorItemView> itemPool,
            string gridName)
        {
            if (itemPool == null || itemPool.Count == 0)
            {
                LogWarning($"{gridName} 아이템 풀이 비어있습니다!");
                return;
            }

            // ⭐ Object Pooling: 활성화/비활성화만!
            for (int i = 0; i < itemPool.Count; i++)
            {
                if (i < sensors.Count)
                {
                    // 활성화 + 데이터 설정
                    itemPool[i].gameObject.SetActive(true);
                    itemPool[i].SetData(sensors[i]);
                }
                else
                {
                    // 비활성화
                    itemPool[i].gameObject.SetActive(false);
                }
            }

            // 아이템이 부족한 경우 경고
            if (sensors.Count > itemPool.Count)
            {
                LogWarning($"{gridName} 센서가 {sensors.Count}개인데, 아이템은 {itemPool.Count}개만 있습니다!");
            }
        }

        /// <summary>
        /// 센서 아이템 클릭 시
        /// </summary>
        private void OnSensorItemClicked(SensorInfoData sensor)
        {
            LogInfo($"센서 선택: {sensor.sensorName} (Board: {sensor.boardIdx}, HNS: {sensor.hnsIdx})");

            // TODO: 상세 팝업 표시
            // UiManager.Instance.Invoke(UiEventType.SelectCurrentSensor, (sensor.boardIdx, sensor.hnsIdx));
        }

        #endregion

        #region Unity Editor 전용

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

        #endregion
    }
}