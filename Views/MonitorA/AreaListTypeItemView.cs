using Assets.Scripts_refactoring.Models.MonitorA;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts_refactoring.Views.MonitorA
{
    /// <summary>
    /// 개별 지역 아이템 View - 데이터 바인딩만 담당
    /// </summary>
    public class AreaListTypeItemView : MonoBehaviour
    {
        #region Inspector 설정
        [SerializeField] private TMP_Text lblAreaName;
        [SerializeField] private TMP_Text lblGreenCount;
        [SerializeField] private TMP_Text lblYellowCount;
        [SerializeField] private TMP_Text lblRedCount;
        [SerializeField] private TMP_Text lblPurpleCount;
        [SerializeField] private Button btnNavigate;
        #endregion

        #region Private Fields
        private int currentAreaId;
        #endregion

        #region Events
        /// <summary>
        /// 지역 네비게이션 클릭 이벤트
        /// ⭐ int (AreaId) 전달
        /// </summary>
        public event System.Action<int> OnNavigateClicked;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (btnNavigate != null)
            {
                btnNavigate.onClick.AddListener(OnNavigateClick);
            }
        }

        private void OnDestroy()
        {
            if (btnNavigate != null)
            {
                btnNavigate.onClick.RemoveListener(OnNavigateClick);
            }
        }
        #endregion

        #region 데이터 바인딩
        /// <summary>
        /// 지역 데이터 바인딩
        /// </summary>
        public void Bind(AreaListModel model)
        {
            if (model == null)
            {
                Debug.LogWarning("[AreaListTypeItemView] Bind: model이 null입니다!");
                return;
            }

            currentAreaId = model.AreaId;

            if (lblAreaName != null)
                lblAreaName.text = model.AreaName;

            if (lblGreenCount != null)
                lblGreenCount.text = model.GreenCount.ToString();

            if (lblYellowCount != null)
                lblYellowCount.text = model.YellowCount.ToString();

            if (lblRedCount != null)
                lblRedCount.text = model.RedCount.ToString();

            if (lblPurpleCount != null)
                lblPurpleCount.text = model.PurpleCount.ToString();
        }
        #endregion

        #region 이벤트 핸들러
        /// <summary>
        /// 네비게이션 버튼 클릭 핸들러
        /// </summary>
        private void OnNavigateClick()
        {
            Debug.Log($"[AreaListTypeItemView] 지역 네비게이션 클릭: AreaId={currentAreaId}");

            // ⭐ AreaId 전달
            OnNavigateClicked?.Invoke(currentAreaId);
        }
        #endregion
    }
}