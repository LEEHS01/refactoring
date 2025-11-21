using Assets.Scripts_refactoring.Models.MonitorA;
using Assets.Scripts_refactoring.Views.MonitorA;
using HNS.MonitorA.ViewModels;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Views.MonitorA;
using RefactoredAreaData = HNS.Common.Models.AreaData;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 지역별 관측소 현황 View
    /// ⭐ 아이템 클릭 시 지역 지도 이동 추가
    /// </summary>
    public class AreaListTypeView : MonoBehaviour
    {
        #region Inspector 설정

        [SerializeField] private RefactoredAreaData.AreaType areaType;
        [SerializeField] private Sprite nuclearSprite;
        [SerializeField] private Sprite oceanSprite;
        [SerializeField] private Image imgAreaType;
        [SerializeField] private TMP_Text lblTitle;
        [SerializeField] private Transform listPanel;

        #endregion

        #region Private Fields

        private List<AreaListTypeItemView> itemViews = new();
        private Vector3 defaultPos;
        private bool isInitialized = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            SubscribeToViewModel();
            isInitialized = true;
        }

        private void OnEnable()
        {
            if (isInitialized)
            {
                RequestInitialData();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
            UnsubscribeFromItems();  // ⭐ 추가
        }

        #endregion

        #region 초기화

        private void InitializeComponents()
        {
            defaultPos = transform.position;

            if (imgAreaType != null)
            {
                imgAreaType.sprite = areaType == RefactoredAreaData.AreaType.Ocean
                    ? oceanSprite
                    : nuclearSprite;
            }

            if (lblTitle != null)
            {
                lblTitle.text = areaType == RefactoredAreaData.AreaType.Ocean
                    ? "집중우심해역 모니터링 현황"
                    : "주요 발전소 모니터링 현황";
            }

            if (listPanel != null)
            {
                itemViews = listPanel.GetComponentsInChildren<AreaListTypeItemView>(true)
                    .ToList();
            }

            LogInfo($"{areaType} View 초기화 완료, 아이템 수: {itemViews.Count}");
        }

        #endregion

        #region ViewModel 구독

        private void SubscribeToViewModel()
        {
            if (AreaListTypeViewModel.Instance == null)
            {
                LogError("ViewModel이 없습니다!");
                return;
            }

            if (areaType == RefactoredAreaData.AreaType.Ocean)
            {
                AreaListTypeViewModel.Instance.OnOceanAreasChanged += RenderAreas;
            }
            else
            {
                AreaListTypeViewModel.Instance.OnNuclearAreasChanged += RenderAreas;
            }

            LogInfo("ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (AreaListTypeViewModel.Instance == null) return;

            if (areaType == RefactoredAreaData.AreaType.Ocean)
            {
                AreaListTypeViewModel.Instance.OnOceanAreasChanged -= RenderAreas;
            }
            else
            {
                AreaListTypeViewModel.Instance.OnNuclearAreasChanged -= RenderAreas;
            }
        }

        /// <summary>
        /// ⭐⭐⭐ 아이템 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromItems()
        {
            foreach (var item in itemViews)
            {
                if (item != null)
                {
                    item.OnNavigateClicked -= OnAreaItemClicked;
                }
            }
        }

        private void RequestInitialData()
        {
            if (AreaListTypeViewModel.Instance != null)
            {
                AreaListTypeViewModel.Instance.RefreshAreasByType((Common.Models.AreaData.AreaType)areaType);
                LogInfo($"데이터 요청: {areaType}");
            }
        }

        #endregion

        #region 렌더링 (Object Pooling)

        private void RenderAreas(List<AreaListModel> areas)
        {
            LogInfo($"지역 렌더링: {areas.Count}개");

            // ⭐ 기존 이벤트 구독 해제
            UnsubscribeFromItems();

            for (int i = 0; i < itemViews.Count; i++)
            {
                if (i < areas.Count)
                {
                    itemViews[i].gameObject.SetActive(true);
                    itemViews[i].Bind(areas[i]);

                    // ⭐⭐⭐ 아이템 클릭 이벤트 구독
                    itemViews[i].OnNavigateClicked += OnAreaItemClicked;
                }
                else
                {
                    itemViews[i].gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region 이벤트 핸들러

        /// <summary>
        /// ⭐⭐⭐ 지역 아이템 클릭 시 지역 지도로 이동
        /// </summary>
        private void OnAreaItemClicked(int areaId)
        {
            LogInfo($"지역 선택: AreaId={areaId}");

            // 1. MapAreaViewModel에 데이터 로드 요청
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.LoadAreaData(areaId);
                LogInfo($"✅ MapAreaViewModel.LoadAreaData({areaId}) 호출 완료");
            }
            else
            {
                LogError("MapAreaViewModel.Instance가 null입니다!");
            }

            // 2. 전국 지도 축소 (MapNationView)
            var mapNationView = FindFirstObjectByType<MapNationView>();
            if (mapNationView != null)
            {
                mapNationView.SwitchToMinimapMode();
                LogInfo("✅ 전국 지도 축소 모드 전환");
            }
            else
            {
                LogWarning("MapNationView를 찾을 수 없습니다!");
            }

            // 3. ⭐ 지역 지도 표시 (MapAreaView의 CanvasGroup 활성화)
            var mapAreaView = FindFirstObjectByType<HNS.MonitorA.Views.MapAreaView>();
            if (mapAreaView != null)
            {
                var canvasGroup = mapAreaView.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    LogInfo("✅ 지역 지도 표시 완료");
                }
            }
            else
            {
                LogWarning("MapAreaView를 찾을 수 없습니다!");
            }
        }

        #endregion

        #region 애니메이션

        public void SlideIn()
        {
            StartCoroutine(AnimatePosition(defaultPos, 1f));
        }

        public void SlideOut()
        {
            StartCoroutine(AnimatePosition(defaultPos + new Vector3(800, 0f), 1f));
        }

        private System.Collections.IEnumerator AnimatePosition(Vector3 target, float duration)
        {
            float elapsed = 0f;
            Vector3 start = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            transform.position = target;
        }

        #endregion

        #region 로깅

        private void LogInfo(string message)
        {
            Debug.Log($"[AreaListTypeView-{areaType}] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AreaListTypeView-{areaType}] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AreaListTypeView-{areaType}] {message}");
        }

        #endregion
    }
}