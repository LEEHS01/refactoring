using DG.Tweening;
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
    /// Monitor A/B 표준 패턴: MonoBehaviour + ViewModel 이벤트 구독
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

        [Header("Scheduler")]
        [SerializeField] private SchedulerService schedulerService;  // ⭐ 스케줄러 연결

        [Header("Animation")]
        [SerializeField] private Vector3 visiblePosition = new Vector3(-575f, 0f, 0f);  // 표시 위치 (원본: -575)
        [SerializeField] private float animationDuration = 1f;

        #region Private Fields
        private List<ObsMonitoringItemView> toxinItems = new();
        private List<ObsMonitoringItemView> chemicalItems = new();
        private List<ObsMonitoringItemView> qualityItems = new();

        private Dictionary<string, Tweener> blinkTweeners = new();
        private int currentObsId = -1;
        private Vector3 defaultPos;  // 기본 위치
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 기본 위치 저장
            defaultPos = transform.position;

            // ⭐ 원본 방식: Start에서 기존 아이템들을 미리 찾아서 캐싱!
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

            // 초기에는 모든 아이템 비활성화
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

            // 설정 버튼 이벤트
            if (btnSetting != null)
            {
                btnSetting.onClick.AddListener(OnSettingButtonClick);
            }

            // ⭐ SchedulerService 이벤트 구독
            if (schedulerService != null)
            {
                schedulerService.OnDataSyncTriggered += OnDataSyncTriggered;      // 10분 주기 정기 업데이트
                schedulerService.OnAlarmDetected += OnAlarmDetected;              // 알람 발생 시 즉시 업데이트
                schedulerService.OnAlarmCancelled += OnAlarmCancelled;            // 알람 해제 시 즉시 업데이트
                Debug.Log("[ObsMonitoringView] SchedulerService 이벤트 구독 완료 (10분 주기 + 알람)");
            }
            else
            {
                Debug.LogWarning("[ObsMonitoringView] SchedulerService가 연결되지 않았습니다! Inspector에서 연결하세요.");
            }

            // ⭐ 원본 방식: GameObject는 활성화, 초기 위치는 Canvas 밖 (defaultPos)
            // Hide() 호출 안 함! 위치만으로 숨김 처리
        }

        private void OnDestroy()
        {
            // ViewModel 이벤트 구독 해제
            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.OnToxinLoaded.RemoveListener(OnToxinLoaded);
                ObsMonitoringViewModel.Instance.OnChemicalLoaded.RemoveListener(OnChemicalLoaded);
                ObsMonitoringViewModel.Instance.OnQualityLoaded.RemoveListener(OnQualityLoaded);
                ObsMonitoringViewModel.Instance.OnBoardErrorChanged.RemoveListener(OnBoardErrorChanged);
                ObsMonitoringViewModel.Instance.OnError.RemoveListener(OnError);

                Debug.Log("[ObsMonitoringView] ViewModel 이벤트 구독 해제");
            }

            // 깜빡임 애니메이션 정리
            foreach (var tweener in blinkTweeners.Values)
            {
                tweener?.Kill();
            }
            blinkTweeners.Clear();

            // 버튼 이벤트 해제
            if (btnSetting != null)
            {
                btnSetting.onClick.RemoveListener(OnSettingButtonClick);
            }

            // ⭐ SchedulerService 이벤트 구독 해제
            if (schedulerService != null)
            {
                schedulerService.OnDataSyncTriggered -= OnDataSyncTriggered;
                schedulerService.OnAlarmDetected -= OnAlarmDetected;
                schedulerService.OnAlarmCancelled -= OnAlarmCancelled;
                Debug.Log("[ObsMonitoringView] SchedulerService 이벤트 구독 해제");
            }

            // DOTween 정리
            lblToxin?.DOKill();
            lblChemical?.DOKill();
            lblQuality?.DOKill();
            transform.DOKill();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 관측소 모니터링 표시 (애니메이션 포함)
        /// </summary>
        public void Show(int obsId)
        {
            currentObsId = obsId;

            // ⭐ 원본 방식: CanvasGroup 사용 안 함, 위치 이동 애니메이션만!
            PlayShowAnimation();

            // ViewModel에 데이터 요청
            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.LoadMonitoringData(obsId);
            }

            Debug.Log($"[ObsMonitoringView] 표시: ObsId={obsId}");
        }

        /// <summary>
        /// 관측소 모니터링 숨김 (애니메이션 포함)
        /// </summary>
        public void Hide()
        {
            // ⭐ 원본 방식: 위치 이동 애니메이션만 (Canvas 밖으로)
            PlayHideAnimation();

            // 깜빡임 애니메이션 정지
            StopAllBlinking();

            // ViewModel 데이터 정리
            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.ClearData();
            }

            currentObsId = -1;

            Debug.Log("[ObsMonitoringView] 숨김");
        }

        /// <summary>
        /// 실시간 업데이트 (10분 주기 + 알람 발생/해제 시)
        /// </summary>
        public void UpdateRealtime()
        {
            if (currentObsId <= 0) return;

            if (ObsMonitoringViewModel.Instance != null)
            {
                ObsMonitoringViewModel.Instance.UpdateSensorValues(currentObsId);
            }
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
            // ⭐ ClearItems 제거! 기존 아이템 재사용
            CreateItems(toxinContent, data, toxinItems);
        }

        private void OnChemicalLoaded(List<SensorItemData> data)
        {
            Debug.Log($"[ObsMonitoringView] 화학물질 센서 로드: {data.Count}개");
            // ⭐ ClearItems 제거! 기존 아이템 재사용
            CreateItems(chemicalContent, data, chemicalItems);
        }

        private void OnQualityLoaded(List<SensorItemData> data)
        {
            Debug.Log($"[ObsMonitoringView] 수질 센서 로드: {data.Count}개");
            // ⭐ ClearItems 제거! 기존 아이템 재사용
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

            if (hasError)
            {
                StartBlinking(boardType, targetImage);
            }
            else
            {
                StopBlinking(boardType, targetImage);
            }
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

            // ⭐ 센서가 기존 아이템보다 많으면 동적으로 생성
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

            // ⭐ 센서 데이터를 UI 아이템에 설정 (재사용!)
            for (int i = 0; i < dataList.Count; i++)
            {
                if (i >= itemList.Count) break;  // 안전장치

                ObsMonitoringItemView itemView = itemList[i];
                SensorItemData data = dataList[i];

                itemView.gameObject.SetActive(true);
                itemView.Initialize(data);
            }

            // ⭐ 사용하지 않는 아이템 숨김 (Destroy 안 함!)
            for (int i = dataList.Count; i < itemList.Count; i++)
            {
                itemList[i].gameObject.SetActive(false);
            }

            // 레이아웃 즉시 재계산
            RectTransform rt = parent.GetComponent<RectTransform>();
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            Debug.Log($"[ObsMonitoringView] 아이템 설정 완료: {dataList.Count}개 활성화");
        }
        #endregion

        #region Private Methods - Animations
        /// <summary>
        /// 표시 애니메이션 (Canvas 밖 → 안으로 슬라이드)
        /// 원본: defaultPos - new Vector3(575, 0, 0)
        /// </summary>
        private void PlayShowAnimation()
        {
            Vector3 targetPos = defaultPos + visiblePosition;  // ⭐ -575 이동

            transform.DOKill();
            // ⭐ 원본처럼 position (월드 좌표) 사용
            transform.DOMove(targetPos, animationDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 숨김 애니메이션 (Canvas 안 → 밖으로 슬라이드)
        /// 원본: defaultPos (원위치)
        /// </summary>
        private void PlayHideAnimation()
        {
            transform.DOKill();
            // ⭐ 원본처럼 position (월드 좌표) 사용
            transform.DOMove(defaultPos, animationDuration)
                .SetEase(Ease.OutQuad);
        }
        #endregion

        #region Private Methods - Blinking Effects
        private void StartBlinking(string key, Image image)
        {
            if (blinkTweeners.ContainsKey(key)) return;

            var tweener = image.DOColor(Color.red, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            blinkTweeners[key] = tweener;
        }

        private void StopBlinking(string key, Image image)
        {
            if (blinkTweeners.ContainsKey(key))
            {
                blinkTweeners[key]?.Kill();
                blinkTweeners.Remove(key);
                image.color = Color.white;
            }
        }

        private void StopAllBlinking()
        {
            foreach (var tweener in blinkTweeners.Values)
            {
                tweener?.Kill();
            }
            blinkTweeners.Clear();

            if (lblToxin != null) lblToxin.color = Color.white;
            if (lblChemical != null) lblChemical.color = Color.white;
            if (lblQuality != null) lblQuality.color = Color.white;
        }
        #endregion

        #region Private Methods - Button Handlers
        private void OnSettingButtonClick()
        {
            Debug.Log("[ObsMonitoringView] 설정 버튼 클릭");
            // TODO: 설정 팝업 표시
        }
        #endregion

        #region Private Methods - Scheduler Handlers
        /// <summary>
        /// 10분 주기 정기 업데이트
        /// </summary>
        private void OnDataSyncTriggered()
        {
            // currentObsId가 -1이면 숨김 상태 → 업데이트 안 함!
            if (currentObsId <= 0) return;

            Debug.Log("[ObsMonitoringView] 10분 주기 업데이트");
            UpdateRealtime();
        }

        /// <summary>
        /// 알람 발생 시 즉시 업데이트
        /// </summary>
        private void OnAlarmDetected()
        {
            // currentObsId가 -1이면 숨김 상태 → 업데이트 안 함!
            if (currentObsId <= 0) return;

            Debug.Log("[ObsMonitoringView] 알람 발생 - 즉시 업데이트");
            UpdateRealtime();
        }

        /// <summary>
        /// 알람 해제 시 즉시 업데이트
        /// </summary>
        private void OnAlarmCancelled()
        {
            // currentObsId가 -1이면 숨김 상태 → 업데이트 안 함!
            if (currentObsId <= 0) return;

            Debug.Log("[ObsMonitoringView] 알람 해제 - 즉시 업데이트");
            UpdateRealtime();
        }
        #endregion

        #region Inspector Validation
        private void OnValidate()
        {
            // ⭐ 원본 방식: CanvasGroup 사용 안 함
        }
        #endregion
    }
}