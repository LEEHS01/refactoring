using HNS.Services;
using HNS.Common.Views;
using Models.MonitorA;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ViewModels.MonitorA;
using HNS.Common.Models;
using DG.Tweening;

namespace Views.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 패널 View
    /// ⭐ Lamp는 상태별 색상만 표시 (깜박임 X)
    /// ⭐ Legend만 설비이상 시 깜박임
    /// </summary>
    public class ObsMonitoringView : MonoBehaviour
    {
        [Header("Board Legend Images")]
        [SerializeField] private Image lblToxin;
        [SerializeField] private Image lblQuality;
        [SerializeField] private Image lblChemical;

        [Header("Observatory Status Lamp")]
        [SerializeField] private Image lampObservatory;  // 상태 표시만 (깜박임 X)

        [Header("Content Containers")]
        [SerializeField] private Transform toxinContent;
        [SerializeField] private Transform chemicalContent;
        [SerializeField] private Transform qualityContent;

        [Header("Prefab")]
        [SerializeField] private GameObject sensorItemPrefab;

        [Header("Settings")]
        [SerializeField] private Button btnSetting;

        [Header("Position Settings")]
        [SerializeField] private Vector3 visiblePosition = new Vector3(-575f, 0f, 0f);

        #region Private Fields
        private List<ObsMonitoringItemView> toxinItems = new();
        private List<ObsMonitoringItemView> chemicalItems = new();
        private List<ObsMonitoringItemView> qualityItems = new();

        private int currentObsId = -1;
        private Vector3 defaultPos;
        private Vector3 scaledVisiblePosition;

        private Dictionary<string, bool> boardErrorStates = new();
        private Color originalLegendColor = Color.white;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            defaultPos = transform.position;
            CalculateScaledPosition();

            if (lblToxin != null)
                originalLegendColor = lblToxin.color;

            Transform scrollContent = transform.Find("Scroll").Find("Content");

            if (toxinContent != null)
            {
                toxinItems = toxinContent.GetComponentsInChildren<ObsMonitoringItemView>(true).ToList();
                Debug.Log($"[ObsMonitoringView] 독성 아이템 {toxinItems.Count}개 캐싱");
            }

            if (chemicalContent != null)
            {
                chemicalItems = chemicalContent.GetComponentsInChildren<ObsMonitoringItemView>(true).ToList();
                Debug.Log($"[ObsMonitoringView] 화학 아이템 {chemicalItems.Count}개 캐싱");
            }

            if (qualityContent != null)
            {
                qualityItems = qualityContent.GetComponentsInChildren<ObsMonitoringItemView>(true).ToList();
                Debug.Log($"[ObsMonitoringView] 수질 아이템 {qualityItems.Count}개 캐싱");
            }

            toxinItems.ForEach(item => item.gameObject.SetActive(false));
            chemicalItems.ForEach(item => item.gameObject.SetActive(false));
            qualityItems.ForEach(item => item.gameObject.SetActive(false));

            if (lampObservatory == null)
            {
                lampObservatory = transform.Find("Icon_EventPanel_TitleCircle")?.Find("Icon_SignalLamp")?.GetComponent<Image>();
                if (lampObservatory != null)
                {
                    Debug.Log("[ObsMonitoringView] lampObservatory 자동으로 찾음");
                }
                else
                {
                    Debug.LogWarning("[ObsMonitoringView] lampObservatory를 찾을 수 없습니다!");
                }
            }

            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.OnToxinLoaded.AddListener(OnToxinLoaded);
                ObsMonitoringViewModel.Instance.OnChemicalLoaded.AddListener(OnChemicalLoaded);
                ObsMonitoringViewModel.Instance.OnQualityLoaded.AddListener(OnQualityLoaded);
                ObsMonitoringViewModel.Instance.OnBoardErrorChanged.AddListener(OnBoardErrorChanged);
                ObsMonitoringViewModel.Instance.OnObservatoryStatusChanged.AddListener(OnObservatoryStatusChanged);
                ObsMonitoringViewModel.Instance.OnError.AddListener(OnError);

                Debug.Log("[ObsMonitoringView] ViewModel 이벤트 구독 완료");
            }
            else
            {
                Debug.LogError("[ObsMonitoringView] ObsMonitoringViewModel.Instance가 null입니다!");
            }

            if (btnSetting != null)
            {
                btnSetting.onClick.AddListener(OnSettingButtonClick);
            }
        }

        private void OnDestroy()
        {
            // ⭐ Legend만 DOTween 정리
            lblToxin?.DOKill();
            lblChemical?.DOKill();
            lblQuality?.DOKill();
            // lampObservatory는 깜박임 안 하므로 Kill 불필요

            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.OnToxinLoaded.RemoveListener(OnToxinLoaded);
                ObsMonitoringViewModel.Instance.OnChemicalLoaded.RemoveListener(OnChemicalLoaded);
                ObsMonitoringViewModel.Instance.OnQualityLoaded.RemoveListener(OnQualityLoaded);
                ObsMonitoringViewModel.Instance.OnBoardErrorChanged.RemoveListener(OnBoardErrorChanged);
                ObsMonitoringViewModel.Instance.OnObservatoryStatusChanged.RemoveListener(OnObservatoryStatusChanged);
                ObsMonitoringViewModel.Instance.OnError.RemoveListener(OnError);

                Debug.Log("[ObsMonitoringView] ViewModel 이벤트 구독 해제");
            }

            if (btnSetting != null)
            {
                btnSetting.onClick.RemoveListener(OnSettingButtonClick);
            }
        }
        #endregion

        #region Public Methods
        public void Show(int obsId)
        {
            currentObsId = obsId;
            ShowPanel();

            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.LoadMonitoringData(obsId);
            }

            Debug.Log($"[ObsMonitoringView] 즉시 표시: ObsId={obsId}");
        }

        public void Hide()
        {
            HidePanel();
            ResetAllBoardColors();

            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.ClearData();
            }

            currentObsId = -1;
            Debug.Log("[ObsMonitoringView] 즉시 숨김");
        }

        public void UpdateTrendLines()
        {
            toxinItems.ForEach(item => item.UpdateTrendLine());
            chemicalItems.ForEach(item => item.UpdateTrendLine());
            qualityItems.ForEach(item => item.UpdateTrendLine());
        }
        #endregion

        #region ViewModel Event Handlers
        private void OnToxinLoaded(List<SensorItemData> data)
        {
            Debug.Log($"[ObsMonitoringView] 독성도 센서 로드: {data.Count}개");
            CreateItems(toxinContent, data, toxinItems);
        }

        private void OnChemicalLoaded(List<SensorItemData> data)
        {
            Debug.Log($"[ObsMonitoringView] 화학물질 센서 로드: {data.Count}개");
            CreateItems(chemicalContent, data, chemicalItems);
        }

        private void OnQualityLoaded(List<SensorItemData> data)
        {
            Debug.Log($"[ObsMonitoringView] 수질 센서 로드: {data.Count}개");
            CreateItems(qualityContent, data, qualityItems);
        }

        /// <summary>
        /// ⭐⭐⭐ 보드 Legend만 깜박임 (설비이상 시)
        /// </summary>
        private void OnBoardErrorChanged(string boardType, bool hasError)
        {
            Image targetImage = boardType switch
            {
                "toxin" => lblToxin,
                "chemical" => lblChemical,
                "quality" => lblQuality,
                _ => null
            };

            if (targetImage == null) return;

            boardErrorStates[boardType] = hasError;

            // ⭐⭐⭐ Legend 깜박임 효과
            SetLegendBlinkEffect(targetImage, hasError);

            Debug.Log($"[ObsMonitoringView] 보드 Legend: {boardType} = {(hasError ? "설비이상(깜박임)" : "정상")}");
        }

        /// <summary>
        /// ⭐⭐⭐ Lamp는 색상만 표시 (깜박임 X)
        /// </summary>
        private void OnObservatoryStatusChanged(ToxinStatus status)
        {
            if (lampObservatory == null)
            {
                Debug.LogWarning("[ObsMonitoringView] lampObservatory가 연결되지 않았습니다!");
                return;
            }

            Color statusColor = status switch
            {
                ToxinStatus.Purple => HexToColor("#6C00E2"),  // 보라색 (설비이상)
                ToxinStatus.Red => Color.red,                 // 빨간색 (경보)
                ToxinStatus.Yellow => Color.yellow,           // 노란색 (경계)
                ToxinStatus.Green => HexToColor("#3EFF00"),   // 초록색 (정상)
                _ => Color.white
            };

            // ⭐⭐⭐ Lamp는 단순 색상만 변경 (깜박임 X)
            lampObservatory.color = statusColor;

            Debug.Log($"[ObsMonitoringView] 관측소 Lamp: {status} (색상만 표시)");
        }

        private void OnError(string errorMessage)
        {
            Debug.LogError($"[ObsMonitoringView] ViewModel 에러: {errorMessage}");
        }
        #endregion

        #region Private Methods - Item Management
        private void CreateItems(
            Transform parent,
            List<SensorItemData> dataList,
            List<ObsMonitoringItemView> itemList)
        {
            if (parent == null || sensorItemPrefab == null)
            {
                Debug.LogWarning("[ObsMonitoringView] parent 또는 sensorItemPrefab이 null입니다.");
                return;
            }

            int needToAddCount = dataList.Count - itemList.Count;

            if (needToAddCount > 0)
            {
                Debug.Log($"[ObsMonitoringView] 동적 아이템 추가: {needToAddCount}개");

                for (int i = 0; i < needToAddCount; i++)
                {
                    GameObject itemObj = Instantiate(sensorItemPrefab, parent);
                    ObsMonitoringItemView itemView = itemObj.GetComponent<ObsMonitoringItemView>();

                    if (itemView != null)
                    {
                        itemList.Add(itemView);
                    }
                }
            }

            for (int i = 0; i < dataList.Count; i++)
            {
                if (i >= itemList.Count) break;

                ObsMonitoringItemView itemView = itemList[i];
                SensorItemData data = dataList[i];

                itemView.gameObject.SetActive(true);
                itemView.Initialize(data);
            }

            for (int i = dataList.Count; i < itemList.Count; i++)
            {
                itemList[i].gameObject.SetActive(false);
            }

            RectTransform rt = parent.GetComponent<RectTransform>();
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            Debug.Log($"[ObsMonitoringView] 아이템 설정 완료: {dataList.Count}개 활성화");
        }
        #endregion

        #region Private Methods - Resolution Scaling
        private void CalculateScaledPosition()
        {
            Vector2 referenceFHD = new Vector2(1920f, 1080f);
            Vector2 actualScreenSize = new Vector2(Screen.width, Screen.height);
            float scaleX = actualScreenSize.x / referenceFHD.x;

            scaledVisiblePosition = new Vector3(
                visiblePosition.x * scaleX,
                visiblePosition.y,
                visiblePosition.z
            );

            Debug.Log($"[ObsMonitoringView] 해상도: {actualScreenSize}, 스케일: {scaleX:F2}");
        }
        #endregion

        #region Private Methods - Panel Show/Hide
        private void ShowPanel()
        {
            Vector3 targetPos = defaultPos + scaledVisiblePosition;
            transform.position = targetPos;
            Debug.Log($"[ObsMonitoringView] 패널 즉시 표시: {targetPos}");
        }

        private void HidePanel()
        {
            transform.position = defaultPos;
            Debug.Log($"[ObsMonitoringView] 패널 즉시 숨김: {defaultPos}");
        }
        #endregion

        #region Private Methods - Blink Effects

        /// <summary>
        /// ⭐⭐⭐ Legend 깜박임 효과 (설비이상 시만)
        /// </summary>
        private void SetLegendBlinkEffect(Image legend, bool hasError)
        {
            if (legend == null) return;

            legend.DOKill();  // 기존 애니메이션 중지

            if (hasError)
            {
                // ⭐ 설비이상: 보라색으로 깜박임
                legend.DOColor(HexToColor("#6C00E2"), 0.8f)
                    .SetLoops(-1, LoopType.Yoyo)  // 무한 반복
                    .SetEase(Ease.InOutSine);     // 부드러운 전환
            }
            else
            {
                // 정상: 원래 색상으로 복원
                legend.color = originalLegendColor;
            }
        }

        #endregion

        #region Private Methods - Board Color Management
        private void ResetAllBoardColors()
        {
            boardErrorStates.Clear();

            // ⭐ Legend 애니메이션 중지 후 초기화
            if (lblToxin != null)
            {
                lblToxin.DOKill();
                lblToxin.color = originalLegendColor;
            }

            if (lblChemical != null)
            {
                lblChemical.DOKill();
                lblChemical.color = originalLegendColor;
            }

            if (lblQuality != null)
            {
                lblQuality.DOKill();
                lblQuality.color = originalLegendColor;
            }

            // ⭐ Lamp는 초록색으로 초기화 (깜박임 X)
            if (lampObservatory != null)
            {
                lampObservatory.color = HexToColor("#3EFF00");
            }
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }
        #endregion

        #region Private Methods - Button Handlers
        private void OnSettingButtonClick()
        {
            Debug.Log("[ObsMonitoringView] 설정 버튼 클릭");
            PopupSettingView popupView = FindFirstObjectByType<PopupSettingView>();
            if (popupView != null)
            {
                popupView.OpenPopup(currentObsId);
            }
        }
        #endregion

        #region Inspector Validation
        private void OnValidate()
        {
        }
        #endregion
    }
}