using HNS.MonitorA.Models;
using Onthesys;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AreaDataModel = HNS.MonitorA.Models.AreaData;  
using ObsDataModel = HNS.MonitorA.Models.ObsData;    

namespace Views.MonitorA
{
    public class MapNationMarkerView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image imgCircle;
        [SerializeField] private Image imgPoint;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TMP_Text txtAreaName;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject focusImage;
        [SerializeField] private Button btnNavigate; 

        [Header("Icon Sprites")]
        [SerializeField] private Sprite iconNuclear;
        [SerializeField] private Sprite iconOcean;

        private MapMarkerData _data;

        private static readonly System.Collections.Generic.Dictionary<ToxinStatus, Color> StatusColors =
            new System.Collections.Generic.Dictionary<ToxinStatus, Color>()
        {
            { ToxinStatus.Green, new Color(0.478f, 0.898f, 0.675f) },
            { ToxinStatus.Yellow, new Color(0.875f, 0.898f, 0.047f) },
            { ToxinStatus.Red, new Color(0.898f, 0.478f, 0.620f) },
            { ToxinStatus.Purple, new Color(0.424f, 0f, 0.886f) }
        };

        // 클릭 이벤트 추가!
        public event Action<int> OnAreaClicked;

        private void Start()
        {
            // Focus 이미지 초기 비활성화
            if (focusImage != null)
            {
                focusImage.SetActive(false);
            }

            // 버튼 클릭 이벤트 연결
            if (btnNavigate != null)
            {
                btnNavigate.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            if (btnNavigate != null)
            {
                btnNavigate.onClick.RemoveListener(OnClick);
            }
        }

        public void UpdateData(MapMarkerData data)
        {
            _data = data;
            Debug.Log($"=== UpdateData: {data.AreaName} ===");

            // 지역명 - 첫 2글자만 표시
            if (txtAreaName != null)
            {
                string displayName = data.AreaName.Length >= 2
                    ? data.AreaName.Substring(0, 2)
                    : data.AreaName;
                txtAreaName.text = displayName;
            }

            // int를 ToxinStatus로 변환
            ToxinStatus status = (ToxinStatus)data.Status;

            Color statusColor;
            if (!StatusColors.TryGetValue(status, out statusColor))
            {
                statusColor = Color.white;
            }

            // 받침 - 기존 알파값 유지
            if (imgCircle != null)
            {
                imgCircle.color = new Color(statusColor.r, statusColor.g, statusColor.b, imgCircle.color.a);
            }

            // 핀
            if (imgPoint != null)
            {
                imgPoint.color = new Color(statusColor.r, statusColor.g, statusColor.b, 1.0f);
            }

            // 아이콘
            if (imgIcon != null)
            {
                if (data.AreaType == AreaDataModel.AreaType.Nuclear && iconNuclear != null)
                    imgIcon.sprite = iconNuclear;
                else if (data.AreaType == AreaDataModel.AreaType.Ocean && iconOcean != null)
                    imgIcon.sprite = iconOcean;
            }
        }

        // 버튼 클릭 핸들러
        private void OnClick()
        {
            if (_data != null)
            {
                Debug.Log($"[MapNationMarkerView] 지역 클릭: {_data.AreaName} (ID: {_data.AreaId})");
                OnAreaClicked?.Invoke(_data.AreaId);
            }
        }

        // 마우스 오버 - 애니메이션 시작
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (animator != null)
            {
                animator.SetTrigger("Play");
            }

            if (focusImage != null)
            {
                focusImage.SetActive(true);
            }
        }

        // 마우스 나감 - 애니메이션 정지
        public void OnPointerExit(PointerEventData eventData)
        {
            StartCoroutine(StopAnimationAfterDelay(1.0f));

            if (focusImage != null)
            {
                focusImage.SetActive(false);
            }
        }

        // 1초 후 애니메이션 정지
        private IEnumerator StopAnimationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (animator != null)
            {
                animator.SetTrigger("Stop");
            }
        }
    }
}