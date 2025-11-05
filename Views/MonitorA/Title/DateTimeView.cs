using HNS.MonitorA.Views;
using System;
using TMPro;
using UnityEngine;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// Monitor A 타이틀의 날짜/시간 표시 View
    /// txtDate와 txtTime을 실시간으로 업데이트
    /// </summary>
    public class DateTimeView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI txtDate;
        [SerializeField] private TextMeshProUGUI txtTime;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 1f; // 업데이트 간격 (초)
        [SerializeField] private string dateFormat = "yyyy.MM.dd";
        [SerializeField] private string timeFormat = "HH:mm:ss";

        private float timer;

        private void Start()
        {
            // 초기 시간 설정
            UpdateDateTime();
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= updateInterval)
            {
                UpdateDateTime();
                timer = 0f;
            }
        }

        /// <summary>
        /// 현재 시간으로 UI 업데이트
        /// </summary>
        private void UpdateDateTime()
        {
            DateTime now = DateTime.Now;

            if (txtDate != null)
            {
                txtDate.text = now.ToString(dateFormat);
            }

            if (txtTime != null)
            {
                txtTime.text = now.ToString(timeFormat);
            }
        }

        #region Editor Helper
#if UNITY_EDITOR
        [ContextMenu("Preview Current Time")]
        private void PreviewTime()
        {
            UpdateDateTime();
            Debug.Log($"📅 현재 시간: {DateTime.Now:yyyy.MM.dd HH:mm:ss}");
        }
#endif
        #endregion
    }
}