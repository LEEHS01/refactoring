using HNS.MonitorA.Models;
using Onthesys;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Views.MonitorA
{
    /// <summary>
    /// 전국 지도 마커 - 개별 마커 관리
    /// </summary>
    public class MapNationMarkerView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler  // ✅ 추가!
    {
        [Header("UI References")]
        [SerializeField] private Image imgCircle;      // 받침
        [SerializeField] private Image imgPoint;       // 핀
        [SerializeField] private Image imgIcon;        // 아이콘
        [SerializeField] private TMP_Text txtAreaName;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject focusImage;  // ✅ Focus 이펙트

        [Header("Icon Sprites")]
        [SerializeField] private Sprite iconNuclear;
        [SerializeField] private Sprite iconOcean;

        private MapMarkerData _data;

        private static readonly Dictionary<ToxinStatus, Color> StatusColors = new Dictionary<ToxinStatus, Color>()
        {
            { ToxinStatus.Green, new Color(0.478f, 0.898f, 0.675f) },
            { ToxinStatus.Yellow, new Color(0.875f, 0.898f, 0.047f) },
            { ToxinStatus.Red, new Color(0.898f, 0.478f, 0.620f) },
            { ToxinStatus.Purple, new Color(0.424f, 0f, 0.886f) }
        };

        private void Start()
        {
            Debug.Log($"=== MapNationMarkerView Start: {gameObject.name} ===");

            // Focus 이미지 초기 비활성화
            if (focusImage != null)
            {
                focusImage.SetActive(false);
            }

            if (imgCircle != null)
            {
                Debug.Log($"[Circle] enabled: {imgCircle.enabled}");
                Debug.Log($"[Circle] gameObject.activeSelf: {imgCircle.gameObject.activeSelf}");
                Debug.Log($"[Circle] sprite: {(imgCircle.sprite != null ? imgCircle.sprite.name : "NULL")}");
                Debug.Log($"[Circle] color: {imgCircle.color}");
                Debug.Log($"[Circle] rectTransform.sizeDelta: {imgCircle.rectTransform.sizeDelta}");
            }
            else
            {
                Debug.LogError($"[Circle] imgCircle is NULL!");
            }
        }

        public void UpdateData(MapMarkerData data)
        {
            _data = data;
            Debug.Log($"=== UpdateData: {data.AreaName} ===");

            // ✅ 지역명 - 첫 2글자만 표시 (예: "인천광역시" → "인천")
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
            Debug.Log($"Status: {status}, Color: {statusColor}");

            // 받침 - 원본처럼 기존 알파값 유지
            if (imgCircle != null)
            {
                Debug.Log($"[Circle BEFORE] color: {imgCircle.color}");

                imgCircle.color = new Color(statusColor.r, statusColor.g, statusColor.b, imgCircle.color.a);

                Debug.Log($"[Circle AFTER] color: {imgCircle.color}");
            }

            // 핀
            if (imgPoint != null)
            {
                imgPoint.color = new Color(statusColor.r, statusColor.g, statusColor.b, 1.0f);
            }

            // 아이콘
            if (imgIcon != null)
            {
                if (data.AreaType == AreaData.AreaType.Nuclear && iconNuclear != null)
                    imgIcon.sprite = iconNuclear;
                else if (data.AreaType == AreaData.AreaType.Ocean && iconOcean != null)
                    imgIcon.sprite = iconOcean;
            }
        }

        // ✅ 마우스 오버 - 애니메이션 시작
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

            Debug.Log($"[{_data?.AreaName}] 마우스 오버");
        }

        // ✅ 마우스 나감 - 애니메이션 정지
        public void OnPointerExit(PointerEventData eventData)
        {
            StartCoroutine(StopAnimationAfterDelay(1.0f));

            if (focusImage != null)
            {
                focusImage.SetActive(false);
            }

            Debug.Log($"[{_data?.AreaName}] 마우스 나감");
        }

        // ✅ 1초 후 애니메이션 정지
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