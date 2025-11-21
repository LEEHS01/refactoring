using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Models.MonitorA;
using System.Collections.Generic;
using System.Linq;
using Common.UI;
using HNS.Common.Models;

namespace Views.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 개별 센서 아이템
    /// 센서명, 상태, 측정값, 트렌드 차트 표시
    /// </summary>
    public class ObsMonitoringItemView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtSensorName;
        [SerializeField] private TMP_Text txtValue;
        [SerializeField] private TMP_Text txtUnit;
        [SerializeField] private Image imgStatus;

        [Header("Trend Chart")]
        [SerializeField] private ChartLineRenderer2 trdSensor;
        [SerializeField] private RectTransform chartBounds;

        [Header("Button")]
        [SerializeField] private Button btnSelectCurrentSensor;

        #region Private Fields
        private SensorItemData data;
        private string sensorKey;

        private static readonly Dictionary<ToxinStatus, Color> StatusColors = new()
        {
            { ToxinStatus.Green,  HexToColor("#3EFF00") },
            { ToxinStatus.Yellow, HexToColor("#FFF600") },
            { ToxinStatus.Red,    HexToColor("#FF0000") },
            { ToxinStatus.Purple, HexToColor("#6C00E2") }
        };
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (trdSensor == null)
            {
                trdSensor = GetComponentInChildren<ChartLineRenderer2>();
            }

            if (chartBounds == null && trdSensor != null)
            {
                Transform parent = trdSensor.transform.parent;
                if (parent != null)
                {
                    chartBounds = parent.GetComponent<RectTransform>();
                }
            }

            if (btnSelectCurrentSensor == null)
            {
                btnSelectCurrentSensor = GetComponent<Button>();
            }

            if (btnSelectCurrentSensor != null)
            {
                btnSelectCurrentSensor.onClick.AddListener(OnClick);
            }
        }

        private void Start()
        {
            if (trdSensor != null && chartBounds != null)
            {
                trdSensor.Initialize(chartBounds);
            }
            else
            {
                Debug.LogWarning($"[ObsMonitoringItem] trdSensor 또는 chartBounds가 null입니다!");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 센서 아이템 초기화
        /// </summary>
        public void Initialize(SensorItemData itemData)
        {
            this.data = itemData;
            this.sensorKey = $"{itemData.BoardId}_{itemData.HnsId}";

            Debug.Log($"[ObsMonitoringItem] 초기화: {data.HnsName}");
            Debug.Log($"  - CurrentValue: {data.CurrentValue}");
            Debug.Log($"  - Status: {data.Status}");
            Debug.Log($"  - Values Count: {data.Values?.Count ?? 0}");
            Debug.Log($"  - Unit: {data.Unit}");

            UpdateUI();
            UpdateTrendLine();
        }

        /// <summary>
        /// 측정값 및 상태 업데이트 (실시간)
        /// </summary>
        public void UpdateValue(float value, ToxinStatus status)
        {
            if (data == null) return;

            data.CurrentValue = value;
            data.Status = status;

            UpdateUI();
        }

        /// <summary>
        /// 트렌드 라인 업데이트 - 센서 측정값들을 차트로 표시
        /// ✅ 수정: txtValue는 UpdateUI()에서만 업데이트
        /// </summary>
        public void UpdateTrendLine()
        {
            if (data == null)
            {
                Debug.LogWarning("[ObsMonitoringItem] UpdateTrendLine: data가 null!");
                return;
            }

            if (data.Values == null || data.Values.Count == 0)
            {
                Debug.LogWarning($"[ObsMonitoringItem] UpdateTrendLine: Values가 비어있음! ({data.HnsName})");
                return;
            }

            if (trdSensor == null)
            {
                Debug.LogWarning($"[ObsMonitoringItem] UpdateTrendLine: trdSensor가 null! ({data.HnsName})");
                return;
            }

            List<float> normalizedValues = new();

            float max = data.Values.Max() + 1;

            foreach (var val in data.Values)
            {
                normalizedValues.Add(val / max);
            }

            Debug.Log($"[ObsMonitoringItem] 트렌드 차트 업데이트: {data.HnsName}, Values={data.Values.Count}개, Max={max}");

            // ✅ 차트만 업데이트 (txtValue는 UpdateUI()에서 관리)
            trdSensor.UpdateChart(normalizedValues);
        }

        /// <summary>
        /// 센서 키 반환 (boardId_hnsId)
        /// </summary>
        public string GetSensorKey() => sensorKey;
        #endregion

        #region Private Methods
        /// <summary>
        /// UI 전체 업데이트
        /// ✅ 0일 때는 0.00으로 표시
        /// </summary>
        private void UpdateUI()
        {
            if (data == null)
            {
                Debug.LogWarning("[ObsMonitoringItem] UpdateUI: data가 null입니다!");
                return;
            }

            if (txtSensorName != null)
            {
                txtSensorName.text = data.HnsName;
            }

            // ✅ CurrentValue 표시
            if (txtValue != null)
            {
                // Purple이고 VAL이 0이 아닌 경우 → 설비이상
                if (data.Status == ToxinStatus.Purple && data.CurrentValue != 0)
                {
                    txtValue.text = "설비이상";
                }
                // 정상 측정값 (0 포함)
                else
                {
                    txtValue.text = FormatValue(data.CurrentValue);
                }
            }

            if (txtUnit != null)
            {
                txtUnit.text = data.Unit;
            }

            UpdateStatusColor();
        }

        /// <summary>
        /// 상태 색상 업데이트
        /// </summary>
        private void UpdateStatusColor()
        {
            if (!StatusColors.ContainsKey(data.Status))
            {
                Debug.LogWarning($"[ObsMonitoringItem] 상태 색상 없음: {data.Status}");
                return;
            }

            Color statusColor = StatusColors[data.Status];

            if (imgStatus != null)
            {
                imgStatus.color = statusColor;
            }

            if (txtValue != null)
            {
                txtValue.color = statusColor;
            }
        }

        /// <summary>
        /// 측정값 포맷팅
        /// </summary>
        private string FormatValue(float value)
        {
            return value.ToString("F2");
        }

        /// <summary>
        /// Hex 색상 문자열을 Color로 변환
        /// </summary>
        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            return Color.white;
        }

        /// <summary>
        /// 센서 선택 버튼 클릭
        /// </summary>
        private void OnClick()
        {
            if (data == null) return;

            Debug.Log($"[ObsMonitoringItem] 센서 선택: {data.HnsName} (Board={data.BoardId}, Hns={data.HnsId})");
        }
        #endregion

        #region Inspector Validation
        private void OnValidate()
        {
            if (txtSensorName == null)
                txtSensorName = transform.Find("TxtSensorName")?.GetComponent<TMP_Text>();

            if (txtValue == null)
                txtValue = transform.Find("TxtValue")?.GetComponent<TMP_Text>();

            if (txtUnit == null)
                txtUnit = transform.Find("TxtUnit")?.GetComponent<TMP_Text>();

            if (imgStatus == null)
                imgStatus = transform.Find("ImgStatus")?.GetComponent<Image>();

            if (trdSensor == null)
                trdSensor = GetComponentInChildren<ChartLineRenderer2>();

            if (chartBounds == null && trdSensor != null)
            {
                Transform parent = trdSensor.transform.parent;
                if (parent != null)
                {
                    chartBounds = parent.GetComponent<RectTransform>();
                }
            }

            if (btnSelectCurrentSensor == null)
                btnSelectCurrentSensor = GetComponent<Button>();
        }
        #endregion
    }
}