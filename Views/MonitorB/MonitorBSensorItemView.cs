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
    public class MonitorBSensorItemView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtSensorName;  // Text_DataItem
        [SerializeField] private TMP_Text txtValue;       // Text_DataValue
        [SerializeField] private TMP_Text txtUnit;        // Text_DataUnit
        [SerializeField] private Button btnItem;          // 클릭 버튼 (선택사항)

        private SensorInfoData sensorData;

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
        public void SetData(SensorInfoData data)
        {
            sensorData = data;

            if (txtSensorName != null)
                txtSensorName.text = data.sensorName;

            if (txtValue != null)
                txtValue.text = data.GetFormattedValue();

            if (txtUnit != null)
                txtUnit.text = data.unit ?? "";

            // 점검 중이면 비활성화 표시 (선택사항)
            /*if (data.isInspection)
            {
                if (txtValue != null)
                    txtValue.text = "점검중";
            }*/
        }

        private void OnClick()
        {
            OnItemClicked?.Invoke(sensorData);
            Debug.Log($"[MonitorBSensorItemView] 센서 클릭: {sensorData.sensorName}");
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