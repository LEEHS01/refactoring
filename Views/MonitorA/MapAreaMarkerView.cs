using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.Common.Models;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HNS.MonitorA.Views 
{
    /// <summary>
    /// 관측소 마커 ItemView
    /// Monitor B의 MonitorBSensorItemView 패턴 적용
    /// </summary>
    public class MapAreaMarkerView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Inspector 설정
        [SerializeField] private Image imgCircle;       // location_Circle (받침)
        [SerializeField] private Image imgLine;         // location_DottedLine (점선)
        [SerializeField] private Image imgButton;       // location_Btn (메인 버튼)
        [SerializeField] private TMP_Text txtObsName;   // location_Btn > Text
        [SerializeField] private Button btnNavigate;    // location_Btn의 Button 컴포넌트
        [SerializeField] private Animator animator;     // 애니메이터
        #endregion

        #region Private Fields
        private ObsMarkerData _currentData;

        private static readonly System.Collections.Generic.Dictionary<ToxinStatus, Color> StatusColors =
            new System.Collections.Generic.Dictionary<ToxinStatus, Color>()
        {
            { ToxinStatus.Green, new Color(0.478f, 0.898f, 0.675f) },    // #7AE5AC
            { ToxinStatus.Yellow, new Color(0.875f, 0.898f, 0.047f) },   // #DFE50C
            { ToxinStatus.Red, new Color(0.898f, 0.478f, 0.620f) },      // #E57A9E
            { ToxinStatus.Purple, new Color(0.424f, 0f, 0.886f) }        // #6C00E2
        };
        #endregion

        #region Events
        /// <summary>
        /// 관측소 클릭 이벤트
        /// </summary>
        public event Action<int, string> OnObsClicked;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (btnNavigate != null)
            {
                btnNavigate.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (btnNavigate != null)
            {
                btnNavigate.onClick.RemoveListener(OnClick);
            }
        }
        #endregion

        #region 데이터 바인딩
        /// <summary>
        /// 관측소 데이터 바인딩
        /// </summary>
        public void Bind(ObsMarkerData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MapAreaMarkerView] Bind: data가 null입니다!");
                return;
            }

            _currentData = data;

            // 관측소 이름
            if (txtObsName != null)
                txtObsName.text = data.ObsName;

            // 상태 색상 적용
            if (StatusColors.TryGetValue(data.Status, out Color statusColor))
            {
                // 받침 - 기존 알파값 유지
                if (imgCircle != null)
                {
                    imgCircle.color = new Color(statusColor.r, statusColor.g, statusColor.b, imgCircle.color.a);
                }

                // 점선
                if (imgLine != null)
                {
                    imgLine.color = statusColor;
                }

                // 메인 버튼
                if (imgButton != null)
                {
                    imgButton.color = statusColor;
                }
            }

            // 위치 설정
            transform.localPosition = data.LocalPosition;
        }
        #endregion

        #region 이벤트 핸들러
        /// <summary>
        /// 버튼 클릭
        /// </summary>
        private void OnClick()
        {
            OnObsClicked?.Invoke(_currentData.ObsId, _currentData.ObsName);  // ⭐ obsName 추가
        }

        /// <summary>
        /// 마우스 오버
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (animator != null)
            {
                animator.SetTrigger("Start");
            }
        }

        /// <summary>
        /// 마우스 나감
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (animator != null)
            {
                animator.SetTrigger("Stop");
            }
        }
        #endregion
    }
}
