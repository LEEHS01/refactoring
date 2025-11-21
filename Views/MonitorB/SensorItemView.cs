using HNS.Common.ViewModels;
using Models.MonitorB;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Views.MonitorB;

namespace Views.MonitorB
{
    public class SensorItemView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtSensorName;
        [SerializeField] private TMP_Text txtValue;
        [SerializeField] private TMP_Text txtUnit;
        [SerializeField] private Button btnItem;

        [Header("Chart Panel")]
        [SerializeField] private SensorChartView chartView;

        private SensorInfoData sensorData;
        private int currentObsId;

        private int _obsIdx;
        private int _boardIdx;
        private int _hnsIdx;

        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;

        public event System.Action<SensorInfoData> OnItemClicked;

        #region Unity Lifecycle

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        private void Start()
        {
            if (btnItem != null)
            {
                btnItem.onClick.AddListener(OnClick);
            }
        }

        private void OnEnable()
        {
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OnSensorVisibilityChanged += OnSensorVisibilityChanged;
                Debug.Log($"[SensorItemView] OnEnable - 이벤트 구독 완료");
            }
        }

        private void OnDisable()
        {
            if (PopupSettingViewModel.Instance != null)
            {
                PopupSettingViewModel.Instance.OnSensorVisibilityChanged -= OnSensorVisibilityChanged;
                Debug.Log($"[SensorItemView] OnDisable - 이벤트 구독 해제");
            }
        }

        private void OnDestroy()
        {
            if (btnItem != null)
            {
                btnItem.onClick.RemoveListener(OnClick);
            }
        }

        #endregion

        #region Public Methods

        public void SetData(SensorInfoData data, int obsId)
        {
            sensorData = data;
            currentObsId = obsId;

            _obsIdx = obsId;
            _boardIdx = data.boardIdx;
            _hnsIdx = data.hnsIdx;

            if (txtSensorName != null)
                txtSensorName.text = data.sensorName;

            if (txtValue != null)
                txtValue.text = data.GetFormattedValue();

            if (txtUnit != null)
                txtUnit.text = data.unit ?? "";

            bool isVisible = data.USEYN?.Trim() == "1";

            Debug.Log($"[SensorItemView] SetData: {data.sensorName}, ObsId={obsId}, BoardId={data.boardIdx}, HnsId={data.hnsIdx}, USEYN={data.USEYN}, Visible={isVisible}");

            UpdateVisibility(isVisible);
        }

        #endregion

        #region Event Handlers

        private void OnSensorVisibilityChanged(int obsIdx, int boardIdx, int hnsIdx, bool isVisible)
        {
            Debug.Log($"[SensorItemView] 이벤트 수신: My(Obs={_obsIdx}, Board={_boardIdx}, Hns={_hnsIdx}) vs Event(Obs={obsIdx}, Board={boardIdx}, Hns={hnsIdx}), Visible={isVisible}");

            // 이 아이템이 해당하는 센서인지 확인
            if (_obsIdx == obsIdx && _boardIdx == boardIdx && _hnsIdx == hnsIdx)
            {
                Debug.Log($"[SensorItemView] ✅ 일치! 표시 변경 실행");
                UpdateVisibility(isVisible);
            }
            else
            {
                Debug.Log($"[SensorItemView] ❌ 불일치 - 무시");
            }
        }

        /// <summary>
        /// ⭐⭐⭐ 실제 표시/숨김 처리 (Layout 재정렬)
        /// </summary>
        private void UpdateVisibility(bool isVisible)
        {
            Debug.Log($"[SensorItemView] UpdateVisibility 시작: {sensorData?.sensorName}, Visible={isVisible}");

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isVisible ? 1f : 0f;
                _canvasGroup.interactable = isVisible;
                _canvasGroup.blocksRaycasts = isVisible;
                Debug.Log($"[SensorItemView] CanvasGroup 설정 완료: alpha={_canvasGroup.alpha}");
            }

            // ⭐⭐⭐ Layout에서 제외/포함
            if (_layoutElement != null)
            {
                _layoutElement.ignoreLayout = !isVisible;
                Debug.Log($"[SensorItemView] LayoutElement 설정 완료: ignoreLayout={_layoutElement.ignoreLayout}");
            }

            // ⭐ 부모 Layout 강제 재계산
            Transform parent = transform.parent;
            if (parent != null)
            {
                var rectTransform = parent.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                    Debug.Log($"[SensorItemView] Layout 재계산 완료: {parent.name}");
                }
            }
        }

        private void OnClick()
        {
            if (sensorData == null)
            {
                Debug.LogWarning("[SensorItemView] 센서 데이터가 없습니다.");
                return;
            }

            Debug.Log($"[SensorItemView] 센서 클릭: {sensorData.sensorName}");

            OnItemClicked?.Invoke(sensorData);
            OpenChartPanel();
        }

        #endregion

        #region Private Methods

        private void OpenChartPanel()
        {
            if (chartView == null)
            {
                Debug.LogError("[SensorItemView] Chart View가 할당되지 않았습니다!");
                return;
            }

            chartView.gameObject.SetActive(true);

            chartView.LoadSensorChart(
                currentObsId,
                sensorData.boardIdx,
                sensorData.hnsIdx,
                sensorData.sensorName
            );
        }

        #endregion
    }
}
