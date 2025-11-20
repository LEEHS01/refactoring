using HNS.Services;
using Models.MonitorA;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ViewModels.MonitorA;

namespace Views.MonitorA
{
    /// <summary>
    /// 관측소 모니터링 패널 View
    /// ⚡ DOTween 제거: 즉시 렌더링 방식으로 성능 최적화
    /// ✅ SchedulerService 구독 제거 (ViewModel만 구독)
    /// </summary>
    public class ObsMonitoringView : MonoBehaviour
    {
        [Header("Board Legend Images")]
        [SerializeField] private Image lblToxin;
        [SerializeField] private Image lblQuality;
        [SerializeField] private Image lblChemical;

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
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            defaultPos = transform.position;
            CalculateScaledPosition();

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

            // ViewModel 이벤트 구독
            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.OnToxinLoaded.AddListener(OnToxinLoaded);
                ObsMonitoringViewModel.Instance.OnChemicalLoaded.AddListener(OnChemicalLoaded);
                ObsMonitoringViewModel.Instance.OnQualityLoaded.AddListener(OnQualityLoaded);
                ObsMonitoringViewModel.Instance.OnBoardErrorChanged.AddListener(OnBoardErrorChanged);
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

            // ❌ SchedulerService 구독 제거 (ViewModel만 구독)
        }

        private void OnDestroy()
        {
            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.OnToxinLoaded.RemoveListener(OnToxinLoaded);
                ObsMonitoringViewModel.Instance.OnChemicalLoaded.RemoveListener(OnChemicalLoaded);
                ObsMonitoringViewModel.Instance.OnQualityLoaded.RemoveListener(OnQualityLoaded);
                ObsMonitoringViewModel.Instance.OnBoardErrorChanged.RemoveListener(OnBoardErrorChanged);
                ObsMonitoringViewModel.Instance.OnError.RemoveListener(OnError);

                Debug.Log("[ObsMonitoringView] ViewModel 이벤트 구독 해제");
            }

            if (btnSetting != null)
            {
                btnSetting.onClick.RemoveListener(OnSettingButtonClick);
            }

            // ❌ SchedulerService 구독 해제 제거
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
            targetImage.color = hasError ? Color.red : Color.white;

            Debug.Log($"[ObsMonitoringView] 보드 상태 변경: {boardType} = {(hasError ? "에러" : "정상")}");
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

        #region Private Methods - Board Color Management
        /// <summary>
        /// 모든 보드 색상 초기화
        /// </summary>
        private void ResetAllBoardColors()
        {
            boardErrorStates.Clear();

            if (lblToxin != null) lblToxin.color = Color.white;
            if (lblChemical != null) lblChemical.color = Color.white;
            if (lblQuality != null) lblQuality.color = Color.white;
        }
        #endregion

        #region Private Methods - Button Handlers
        private void OnSettingButtonClick()
        {
            Debug.Log("[ObsMonitoringView] 설정 버튼 클릭");
        }
        #endregion

        #region Inspector Validation
        private void OnValidate()
        {
        }
        #endregion
    }
}