using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Models.MonitorA;
using System.Collections.Generic;
using System.Linq;
using Onthesys;
using Common.UI;  // ⭐ ChartLineRenderer2 사용

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
        [SerializeField] private ChartLineRenderer2 trdSensor;  // ⭐ UILineRenderer → ChartLineRenderer2
        [SerializeField] private RectTransform chartBounds;      // ⭐ 차트 영역 (LineChart의 부모)

        [Header("Button")]
        [SerializeField] private Button btnSelectCurrentSensor;  // ⭐ 센서 선택 버튼

        #region Private Fields
        private SensorItemData data;
        private string sensorKey;

        // 상태별 색상
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
            // ChartLineRenderer2가 없으면 자동으로 찾기
            if (trdSensor == null)
            {
                trdSensor = GetComponentInChildren<ChartLineRenderer2>();
            }

            // chartBounds가 없으면 자동으로 찾기 (LineChart의 부모)
            if (chartBounds == null && trdSensor != null)
            {
                // LineChart(ChartLineRenderer2)의 부모가 chartBounds 역할
                Transform parent = trdSensor.transform.parent;
                if (parent != null)
                {
                    chartBounds = parent.GetComponent<RectTransform>();
                }
            }

            // Button이 없으면 자동으로 찾기
            if (btnSelectCurrentSensor == null)
            {
                btnSelectCurrentSensor = GetComponent<Button>();
            }

            // 버튼 이벤트 연결
            if (btnSelectCurrentSensor != null)
            {
                btnSelectCurrentSensor.onClick.AddListener(OnClick);
            }
        }

        private void Start()
        {
            // ⭐ ChartLineRenderer2 초기화
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
            UpdateTrendLine();  // ⭐ 트렌드 차트 업데이트
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

            // 정규화를 위한 최대값 계산 (+1은 차트가 맨 위에 붙지 않도록)
            float max = data.Values.Max() + 1;

            // 모든 값을 0~1 범위로 정규화
            foreach (var val in data.Values)
            {
                normalizedValues.Add(val / max);
            }

            Debug.Log($"[ObsMonitoringItem] 트렌드 차트 업데이트: {data.HnsName}, Values={data.Values.Count}개, Max={max}");

            // ⭐ ChartLineRenderer2 업데이트 (UpdateControlPoints → UpdateChart)
            trdSensor.UpdateChart(normalizedValues);

            // 현재값 텍스트 업데이트
            if (txtValue != null)
            {
                txtValue.text = data.GetLastValue().ToString("F2");
            }
        }

        /// <summary>
        /// 센서 키 반환 (boardId_hnsId)
        /// </summary>
        public string GetSensorKey() => sensorKey;
        #endregion

        #region Private Methods
        /// <summary>
        /// UI 전체 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (data == null)
            {
                Debug.LogWarning("[ObsMonitoringItem] UpdateUI: data가 null입니다!");
                return;
            }

            // 센서명
            if (txtSensorName != null)
            {
                txtSensorName.text = data.HnsName;
            }
            else
            {
                Debug.LogWarning($"[ObsMonitoringItem] txtSensorName이 null! ({data.HnsName})");
            }

            // 측정값
            if (txtValue != null)
            {
                if (data.Status == ToxinStatus.Purple)
                {
                    txtValue.text = "설비이상";
                }
                else
                {
                    txtValue.text = FormatValue(data.CurrentValue);
                }
                Debug.Log($"[ObsMonitoringItem] txtValue 설정: {txtValue.text}");
            }
            else
            {
                Debug.LogWarning($"[ObsMonitoringItem] txtValue가 null! ({data.HnsName})");
            }

            // 단위
            if (txtUnit != null)
            {
                txtUnit.text = data.Unit;
            }
            else
            {
                Debug.LogWarning($"[ObsMonitoringItem] txtUnit이 null! ({data.HnsName})");
            }

            // 상태 색상
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
            Debug.Log($"[ObsMonitoringItem] 상태 색상 설정: {data.HnsName} = {data.Status} ({statusColor})");

            // 상태 표시 이미지
            if (imgStatus != null)
            {
                imgStatus.color = statusColor;
                Debug.Log($"[ObsMonitoringItem] imgStatus 색상 설정됨");
            }
            else
            {
                Debug.LogWarning($"[ObsMonitoringItem] imgStatus가 null! ({data.HnsName})");
            }

            // 텍스트 색상
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
            if (value >= 1000f)
                return value.ToString("N0");
            else if (value >= 100f)
                return value.ToString("F1");
            else if (value >= 10f)
                return value.ToString("F2");
            else
                return value.ToString("F3");
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
        /// TODO: 센서 상세 팝업 또는 차트 표시 기능 구현
        /// </summary>
        private void OnClick()
        {
            if (data == null) return;

            Debug.Log($"[ObsMonitoringItem] 센서 선택: {data.HnsName} (Board={data.BoardId}, Hns={data.HnsId})");

            // TODO: 센서 선택 이벤트 발생
            // UiManager.Instance.Invoke(UiEventType.SelectCurrentSensor, (data.BoardId, data.HnsId));
            // 또는
            // ViewModel을 통해 이벤트 발생
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

            // chartBounds 자동 찾기
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