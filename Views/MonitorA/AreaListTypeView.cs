using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts_refactoring.Models.MonitorA;
using Assets.Scripts_refactoring.ViewModels.MonitorA;

namespace Assets.Scripts_refactoring.Views.MonitorA
{
    /// <summary>
    /// 지역별 관측소 현황 View
    /// Monitor B의 SensorView 패턴 참고
    /// </summary>
    public class AreaListTypeView : MonoBehaviour
    {
        #region Inspector 설정

        [SerializeField] private AreaData.AreaType areaType;
        [SerializeField] private Sprite nuclearSprite;
        [SerializeField] private Sprite oceanSprite;
        [SerializeField] private Image imgAreaType;
        [SerializeField] private TMP_Text lblTitle;
        [SerializeField] private Transform listPanel;

        #endregion

        #region Private Fields

        private List<AreaListTypeItemView> itemViews = new();
        private Vector3 defaultPos;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // 컴포넌트 초기화
            InitializeComponents();

            // ViewModel 이벤트 구독
            SubscribeToViewModel();

            // 초기 데이터 요청
            RequestInitialData();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        #endregion

        #region 초기화

        private void InitializeComponents()
        {
            defaultPos = transform.position;

            // 아이콘 설정
            if (imgAreaType != null)
            {
                imgAreaType.sprite = areaType == AreaData.AreaType.Ocean
                    ? oceanSprite
                    : nuclearSprite;
            }

            // 제목 설정
            if (lblTitle != null)
            {
                lblTitle.text = areaType == AreaData.AreaType.Ocean
                    ? "집중우심해역 모니터링 현황"
                    : "주요 발전소 모니터링 현황";
            }

            // ItemView 풀 생성 (Object Pooling)
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

            // 타입에 맞는 이벤트 구독
            if (areaType == AreaData.AreaType.Ocean)
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

            if (areaType == AreaData.AreaType.Ocean)
            {
                AreaListTypeViewModel.Instance.OnOceanAreasChanged -= RenderAreas;
            }
            else
            {
                AreaListTypeViewModel.Instance.OnNuclearAreasChanged -= RenderAreas;
            }
        }

        private void RequestInitialData()
        {
            if (AreaListTypeViewModel.Instance != null)
            {
                AreaListTypeViewModel.Instance.RefreshAreasByType(areaType);
            }
        }

        #endregion

        #region 렌더링 (Object Pooling)

        /// <summary>
        /// 지역 데이터 렌더링 (Monitor B 패턴)
        /// Object Pooling: 미리 생성된 아이템들을 활성화/비활성화
        /// </summary>
        private void RenderAreas(List<AreaListModel> areas)
        {
            LogInfo($"지역 렌더링: {areas.Count}개");

            // Object Pooling: 활성화/비활성화만!
            for (int i = 0; i < itemViews.Count; i++)
            {
                if (i < areas.Count)
                {
                    // 데이터 있음 → 활성화 + 바인딩
                    itemViews[i].gameObject.SetActive(true);
                    itemViews[i].Bind(areas[i]);
                }
                else
                {
                    // 데이터 없음 → 비활성화
                    itemViews[i].gameObject.SetActive(false);
                }
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

        private void LogError(string message)
        {
            Debug.LogError($"[AreaListTypeView-{areaType}] {message}");
        }

        #endregion
    }
}