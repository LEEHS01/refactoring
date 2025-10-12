using System;
using TMPro;
using UnityEngine;
using ViewModels.Common;

namespace Views.Common
{
    /// <summary>
    /// 시간을 표시하는 View
    /// Text (TMP) Date와 Text (TMP) Time에 연결
    /// </summary>
    public class TimeView : MonoBehaviour
    {
        [Header("시간 표시 설정 (Inspector 연결)")]
        [SerializeField] private TextMeshProUGUI timeText;  // Text (TMP) Time
        [SerializeField] private TextMeshProUGUI dateText;  // Text (TMP) Date

        [Header("포맷 설정")]
        [Tooltip("시간 포맷 (예: HH:mm:ss, HH:mm)")]
        [SerializeField] private string timeFormat = "HH:mm:ss";

        [Tooltip("날짜 포맷 (예: yyyy/MM/dd, yyyy.MM.dd)")]
        [SerializeField] private string dateFormat = "yyyy/MM/dd";

        [Header("표시 옵션")]
        [SerializeField] private bool showDate = false;  // 날짜도 함께 표시할지

        private void Start()
        {
            // ViewModel이 준비될 때까지 대기
            if (TimeViewModel.Instance != null)
            {
                SubscribeToViewModel();
            }
            else
            {
                // ViewModel이 아직 초기화되지 않은 경우 다음 프레임에 재시도
                StartCoroutine(WaitForViewModel());
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        /// <summary>
        /// ViewModel 이벤트 구독
        /// </summary>
        private void SubscribeToViewModel()
        {
            if (TimeViewModel.Instance != null)
            {
                TimeViewModel.Instance.OnTimeUpdated += UpdateTimeDisplay;

                // 초기 시간 표시
                UpdateTimeDisplay(TimeViewModel.Instance.CurrentTime);
            }
        }

        /// <summary>
        /// ViewModel 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromViewModel()
        {
            if (TimeViewModel.Instance != null)
            {
                TimeViewModel.Instance.OnTimeUpdated -= UpdateTimeDisplay;
            }
        }

        /// <summary>
        /// 시간 표시 업데이트
        /// </summary>
        private void UpdateTimeDisplay(DateTime currentTime)
        {
            if (timeText != null)
            {
                timeText.text = currentTime.ToString(timeFormat);
            }

            if (showDate && dateText != null)
            {
                dateText.text = currentTime.ToString(dateFormat);
            }
        }

        /// <summary>
        /// ViewModel이 초기화될 때까지 대기
        /// </summary>
        private System.Collections.IEnumerator WaitForViewModel()
        {
            while (TimeViewModel.Instance == null)
            {
                yield return null;
            }
            SubscribeToViewModel();
        }

        // 런타임에서 포맷 변경 가능
        public void SetTimeFormat(string format)
        {
            timeFormat = format;
            if (TimeViewModel.Instance != null)
            {
                UpdateTimeDisplay(TimeViewModel.Instance.CurrentTime);
            }
        }

        public void SetDateFormat(string format)
        {
            dateFormat = format;
            if (TimeViewModel.Instance != null)
            {
                UpdateTimeDisplay(TimeViewModel.Instance.CurrentTime);
            }
        }
    }
}