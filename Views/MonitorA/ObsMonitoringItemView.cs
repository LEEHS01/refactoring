using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Models.MonitorA;
using System.Collections.Generic;
using System.Linq;
using Common.UI;
using HNS.Common.Models;
using HNS.Common.ViewModels;
using Views.MonitorB;  // ⭐ 추가

namespace Views.MonitorA
{
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

        private int _obsIdx;
        private int _boardIdx;
        private int _hnsIdx;

        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;

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
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = gameObject.AddComponent<LayoutElement>();

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
        }

        private void OnEnable()
        {
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OnSensorVisibilityChanged += OnSensorVisibilityChanged;
            }
        }

        private void OnDisable()
        {
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OnSensorVisibilityChanged -= OnSensorVisibilityChanged;
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(SensorItemData itemData)
        {
            this.data = itemData;
            this.sensorKey = $"{itemData.BoardId}_{itemData.HnsId}";

            _obsIdx = ViewModels.MonitorA.ObsMonitoringViewModel.Instance?.GetCurrentObsId() ?? -1;
            _boardIdx = itemData.BoardId;
            _hnsIdx = itemData.HnsId;

            Debug.Log($"[ObsMonitoringItem] 초기화: {data.HnsName}, IsActive={itemData.IsActive}");

            UpdateUI();
            UpdateTrendLine();

            UpdateVisibility(itemData.IsActive);
        }

        public void UpdateValue(float value, ToxinStatus status)
        {
            if (data == null) return;

            data.CurrentValue = value;
            data.Status = status;

            UpdateUI();
        }

        public void UpdateTrendLine()
        {
            if (data == null || data.Values == null || data.Values.Count == 0)
            {
                Debug.LogWarning($"[ObsMonitoringItem] UpdateTrendLine 실패: {data?.HnsName}");
                return;
            }

            if (trdSensor == null) return;

            List<float> normalizedValues = new();
            float max = data.Values.Max() + 1;

            foreach (var val in data.Values)
            {
                normalizedValues.Add(val / max);
            }

            trdSensor.UpdateChart(normalizedValues);
        }

        public string GetSensorKey() => sensorKey;
        #endregion

        #region Event Handlers
        private void OnSensorVisibilityChanged(int obsIdx, int boardIdx, int hnsIdx, bool isVisible)
        {
            if (_obsIdx == obsIdx && _boardIdx == boardIdx && _hnsIdx == hnsIdx)
            {
                UpdateVisibility(isVisible);
                Debug.Log($"[ObsMonitoringItem] 표시 변경: {data.HnsName}, Visible={isVisible}");
            }
        }

        private void UpdateVisibility(bool isVisible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isVisible ? 1f : 0f;
                _canvasGroup.interactable = isVisible;
                _canvasGroup.blocksRaycasts = isVisible;
            }

            if (_layoutElement != null)
            {
                _layoutElement.ignoreLayout = !isVisible;
            }

            Transform parent = transform.parent;
            if (parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
            }
        }
        #endregion

        #region Private Methods
        private void UpdateUI()
        {
            if (data == null) return;

            if (txtSensorName != null)
                txtSensorName.text = data.HnsName;

            if (txtValue != null)
            {
                if (data.Status == ToxinStatus.Purple && data.CurrentValue != 0)
                {
                    txtValue.text = "설비이상";
                }
                else
                {
                    txtValue.text = FormatValue(data.CurrentValue);
                }
            }

            if (txtUnit != null)
                txtUnit.text = data.Unit;

            UpdateStatusColor();
        }

        private void UpdateStatusColor()
        {
            if (!StatusColors.ContainsKey(data.Status)) return;

            Color statusColor = StatusColors[data.Status];

            if (imgStatus != null)
                imgStatus.color = statusColor;

            if (txtValue != null)
                txtValue.color = statusColor;
        }

        private string FormatValue(float value)
        {
            return value.ToString("F2");
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            return Color.white;
        }

        /// <summary>
        /// ⭐⭐⭐ 아이템 클릭 시 Monitor B 차트 표시
        /// </summary>
        private void OnClick()
        {
            if (data == null)
            {
                Debug.LogWarning("[ObsMonitoringItem] 센서 데이터가 없습니다.");
                return;
            }

            Debug.Log($"[ObsMonitoringItem] 센서 클릭: {data.HnsName} (Board={data.BoardId}, Hns={data.HnsId})");

            // ⭐ Monitor B의 SensorChartView 찾기
            var chartView = FindFirstObjectByType<SensorChartView>();
            if (chartView == null)
            {
                Debug.LogError("[ObsMonitoringItem] SensorChartView를 찾을 수 없습니다!");
                return;
            }

            // ⭐ 차트 활성화 및 데이터 로드
            chartView.gameObject.SetActive(true);
            chartView.LoadSensorChart(_obsIdx, data.BoardId, data.HnsId, data.HnsName);

            Debug.Log($"[ObsMonitoringItem] ✅ Monitor B 차트 로드: ObsId={_obsIdx}, Board={data.BoardId}, Hns={data.HnsId}, Name={data.HnsName}");
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
                    chartBounds = parent.GetComponent<RectTransform>();
            }

            if (btnSelectCurrentSensor == null)
                btnSelectCurrentSensor = GetComponent<Button>();
        }
        #endregion
    }
}