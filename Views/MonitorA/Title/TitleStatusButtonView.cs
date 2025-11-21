using HNS.Common.Models;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 타이틀 상태 버튼 View (표시 전용)
    /// 상태별 색상과 텍스트만 표시
    /// </summary>
    public class TitleStatusButtonView : MonoBehaviour
    {
        [Header("Status Type")]
        [SerializeField] private ToxinStatus statusType;

        [Header("UI References")]
        [SerializeField] private Image imgSignalLamp;
        [SerializeField] private TMP_Text lblText;

        // 상태별 색상
        private static readonly Dictionary<ToxinStatus, Color> StatusColors = new()
        {
            { ToxinStatus.Green,  new Color(0.243f, 1f, 0f) },      // #3EFF00
            { ToxinStatus.Yellow, new Color(1f, 0.965f, 0f) },      // #FFF600
            { ToxinStatus.Red,    new Color(1f, 0f, 0f) },          // #FF0000
            { ToxinStatus.Purple, new Color(0.424f, 0f, 0.886f) }   // #6C00E2
        };

        // 상태별 텍스트
        private static readonly Dictionary<ToxinStatus, string> StatusTexts = new()
        {
            { ToxinStatus.Green,  "정상" },
            { ToxinStatus.Yellow, "경계" },
            { ToxinStatus.Red,    "경고" },
            { ToxinStatus.Purple, "설비이상" }
        };

        private void Awake()
        {
            UpdateUI();
        }

        /// <summary>
        /// UI 업데이트 (색상, 텍스트)
        /// </summary>
        private void UpdateUI()
        {
            if (lblText != null && StatusTexts.ContainsKey(statusType))
                lblText.text = StatusTexts[statusType];

            if (imgSignalLamp != null && StatusColors.ContainsKey(statusType))
                imgSignalLamp.color = StatusColors[statusType];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Editor에서 statusType 변경 시 자동 프리뷰
            if (lblText == null)
                lblText = GetComponentInChildren<TMP_Text>();

            if (imgSignalLamp == null)
            {
                Transform lamp = transform.Find("SignalLamp_Green");
                if (lamp != null)
                    imgSignalLamp = lamp.GetComponent<Image>();
            }

            UpdateUI();
        }
#endif
    }
}