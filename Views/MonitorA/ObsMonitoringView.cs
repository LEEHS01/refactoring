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
using DG.Tweening;  // ⭐ DOTween 추가

namespace Views.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 패널 View
    /// ⚡ 설비이상 시 깜박임 효과 추가
    /// ✅ SchedulerService 구독 제거 (ViewModel만 구독)
    /// ✅ 관측소 전체 상태 Lamp 추가
    /// </summary>
    public class ObsMonitoringView : MonoBehaviour
    {
        [Header("Board Legend Images")]
        [SerializeField] private Image lblToxin;
        [SerializeField] private Image lblQuality;
        [SerializeField] private Image lblChemical;

        // ⭐⭐⭐ 관측소 전체 상태 Lamp (1개)
        [Header("Observatory Status Lamp")]
        [SerializeField] private Image lampObservatory;  // 왼쪽 초록 동그라미

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
        private Color originalLegendColor = Color.white;  // ⭐ 원본 색상 저장
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            defaultPos = transform.position;
            CalculateScaledPosition();

            // ⭐ 원본 Legend 색상 저장
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

            // ⭐ Lamp 자동 찾기
            if (lampObservatory == null)
            {
                lampObservatory = transform.Find("Icon_EventPanel_TitleCircle")?.Find("Icon_SignalLamp")?.GetComponent<Image>();
                if (lampObservatory != null)
                {
                    Debug.Log("[ObsMonitoringView] lampObservatory 자동으로 찾음");
                }
                else
                {
                    Debug.LogWarning("[ObsMonitoringView] lampObservatory를 찾을 수 없습니다! Inspector에서 연결하세요.");
                }
            }

            // ViewModel 이벤트 구독
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
            // ⭐ DOTween 정리
            lblToxin?.DOKill();
            lblChemical?.DOKill();
            lblQuality?.DOKill();
            lampObservatory?.DOKill();

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
        /// <summary>
        /// 관측소 모니터링 즉시 표시 (애니메이션 제거)
        /// </summary>
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

        /// <summary>
        /// 관측소 모니터링 즉시 숨김 (애니메이션 제거)
        /// </summary>
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

        /// <summary>
        /// 트렌드 차트 업데이트 (외부 이벤트용)
        /// </summary>
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
        /// ⭐⭐⭐ 보드 Legend 색상 업데이트 (설비이상 시 깜박임)
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

            // ⭐⭐⭐ 깜박임 효과 추가
            SetImageColorEffect(targetImage, hasError);

            Debug.Log($"[ObsMonitoringView] 보드 Legend: {boardType} = {(hasError ? "설비이상(깜박임)" : "정상")}");
        }

        /// <summary>
        /// ⭐⭐⭐ 관측소 전체 상태 Lamp 업데이트 (설비이상 시 깜박임)
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
                ToxinStatus.Purple => HexToColor("#6C00E2"),
                ToxinStatus.Red => Color.red,
                ToxinStatus.Yellow => Color.yellow,
                ToxinStatus.Green => HexToColor("#3EFF00"),
                _ => Color.white
            };

            // ⭐⭐⭐ Purple(설비이상)일 때만 깜박임
            if (status == ToxinStatus.Purple)
            {
                SetLampBlinkEffect(lampObservatory, statusColor);
            }
            else
            {
                lampObservatory.DOKill();
                lampObservatory.color = statusColor;
            }

            Debug.Log($"[ObsMonitoringView] 관측소 Lamp: {status} {(status == ToxinStatus.Purple ? "(깜박임)" : "")}");
        }

        private void OnError(string errorMessage)
        {
            Debug.LogError($"[ObsMonitoringView] ViewModel 에러: {errorMessage}");
        }
        #endregion

        #region Private Methods - Item Management
        /// <summary>
        /// 센서 아이템 설정 (원본 방식: 재사용, Destroy 안 함!)
        /// </summary>
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
        /// <summary>
        /// 해상도에 따른 위치 스케일링 (FHD+ 대응)
        /// </summary>
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

            Debug.Log($"[ObsMonitoringView] 해상도: {actualScreenSize}, 스케일: {scaleX:F2}, 원본위치: {visiblePosition}, 스케일위치: {scaledVisiblePosition}");
        }
        #endregion

        #region Private Methods - Panel Show/Hide
        /// <summary>
        /// 패널 즉시 표시 (애니메이션 제거)
        /// </summary>
        private void ShowPanel()
        {
            Vector3 targetPos = defaultPos + scaledVisiblePosition;
            transform.position = targetPos;
            Debug.Log($"[ObsMonitoringView] 패널 즉시 표시: {targetPos}");
        }

        /// <summary>
        /// 패널 즉시 숨김 (애니메이션 제거)
        /// </summary>
        private void HidePanel()
        {
            transform.position = defaultPos;
            Debug.Log($"[ObsMonitoringView] 패널 즉시 숨김: {defaultPos}");
        }
        #endregion

        #region Private Methods - Blink Effects

        /// <summary>
        /// ⭐⭐⭐ Legend 이미지 깜박임 효과 (원본 방식)
        /// </summary>
        private void SetImageColorEffect(Image image, bool hasError)
        {
            if (image == null) return;

            image.DOKill();  // 기존 애니메이션 중지

            if (hasError)
            {
                // ⭐ 설비이상: 빨간색으로 깜박임
                image.DOColor(Color.red, 0.8f)
                    .SetLoops(-1, LoopType.Yoyo)  // 무한 반복
                    .SetEase(Ease.InOutSine);     // 부드러운 전환
            }
            else
            {
                // 정상: 원래 색상으로 복원
                image.color = originalLegendColor;
            }
        }

        /// <summary>
        /// ⭐⭐⭐ Lamp 깜박임 효과
        /// </summary>
        private void SetLampBlinkEffect(Image lamp, Color targetColor)
        {
            if (lamp == null) return;

            lamp.DOKill();  // 기존 애니메이션 중지

            // ⭐ 보라색으로 깜박임
            lamp.DOColor(targetColor, 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        #endregion

        #region Private Methods - Board Color Management
        /// <summary>
        /// 모든 보드 색상 초기화
        /// </summary>
        private void ResetAllBoardColors()
        {
            boardErrorStates.Clear();

            // ⭐ 애니메이션 중지 후 초기화
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

            // ⭐ Lamp도 초기화
            if (lampObservatory != null)
            {
                lampObservatory.DOKill();
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
                popupView.OpenPopup(currentObsId);  // 현재 관측소
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