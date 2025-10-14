using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Models.MonitorB;

namespace Views.MonitorB
{
    /// <summary>
    /// 모니터B 센서 아이템 View (프리팹)
    /// ItemToxin, ItemWQ, ItemChemical에 사용
    /// </summary>
    public class SensorItemView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtSensorName;  // Text_DataItem
        [SerializeField] private TMP_Text txtValue;       // Text_DataValue
        [SerializeField] private TMP_Text txtUnit;        // Text_DataUnit
        [SerializeField] private Button btnItem;          // 클릭 버튼

        [Header("Chart Panel")]
        [SerializeField] private SensorChartView chartView;

        private SensorInfoData sensorData;
        private int currentObsId;

        public event System.Action<SensorInfoData> OnItemClicked;

        private void Start()
        {
            if (btnItem != null)
            {
                btnItem.onClick.AddListener(OnClick);
            }
        }

        /// <summary>
        /// 센서 데이터 설정
        /// </summary>
        public void SetData(SensorInfoData data, int obsId)
        {
            sensorData = data;
            currentObsId = obsId;

            if (txtSensorName != null)
                txtSensorName.text = data.sensorName;

            if (txtValue != null)
                txtValue.text = data.GetFormattedValue();

            if (txtUnit != null)
                txtUnit.text = data.unit ?? "";
        }

        private void OnClick()
        {
            if (sensorData == null)
            {
                Debug.LogWarning("[SensorItemView] 센서 데이터가 없습니다.");
                return;
            }

            Debug.Log($"[SensorItemView] 센서 클릭: {sensorData.sensorName}");

            // 이벤트 발생
            OnItemClicked?.Invoke(sensorData);

            // 차트 패널 열기
            OpenChartPanel();
        }

        /// <summary>
        /// 차트 패널 열기
        /// </summary>
        private void OpenChartPanel()
        {
            if (chartView == null)
            {
                Debug.LogError("[SensorItemView] Chart View가 할당되지 않았습니다!");
                return;
            }

            // 차트 패널 활성화
            chartView.gameObject.SetActive(true);

            // 차트 데이터 로드
            chartView.LoadSensorChart(
                currentObsId,
                sensorData.boardIdx,
                sensorData.hnsIdx,
                sensorData.sensorName
            );

            Debug.Log($"[SensorItemView] 차트 열기: obsId={currentObsId}, board={sensorData.boardIdx}, hns={sensorData.hnsIdx}");
        }

        private void OnDestroy()
        {
            if (btnItem != null)
            {
                btnItem.onClick.RemoveListener(OnClick);
            }
        }
    }
}