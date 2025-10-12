using System;
using TMPro;
using UnityEngine;
using ViewModels.Common;

namespace Views.Common
{
    /// <summary>
    /// �ð��� ǥ���ϴ� View
    /// Text (TMP) Date�� Text (TMP) Time�� ����
    /// </summary>
    public class TimeView : MonoBehaviour
    {
        [Header("�ð� ǥ�� ���� (Inspector ����)")]
        [SerializeField] private TextMeshProUGUI timeText;  // Text (TMP) Time
        [SerializeField] private TextMeshProUGUI dateText;  // Text (TMP) Date

        [Header("���� ����")]
        [Tooltip("�ð� ���� (��: HH:mm:ss, HH:mm)")]
        [SerializeField] private string timeFormat = "HH:mm:ss";

        [Tooltip("��¥ ���� (��: yyyy/MM/dd, yyyy.MM.dd)")]
        [SerializeField] private string dateFormat = "yyyy/MM/dd";

        [Header("ǥ�� �ɼ�")]
        [SerializeField] private bool showDate = false;  // ��¥�� �Բ� ǥ������

        private void Start()
        {
            // ViewModel�� �غ�� ������ ���
            if (TimeViewModel.Instance != null)
            {
                SubscribeToViewModel();
            }
            else
            {
                // ViewModel�� ���� �ʱ�ȭ���� ���� ��� ���� �����ӿ� ��õ�
                StartCoroutine(WaitForViewModel());
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        /// <summary>
        /// ViewModel �̺�Ʈ ����
        /// </summary>
        private void SubscribeToViewModel()
        {
            if (TimeViewModel.Instance != null)
            {
                TimeViewModel.Instance.OnTimeUpdated += UpdateTimeDisplay;

                // �ʱ� �ð� ǥ��
                UpdateTimeDisplay(TimeViewModel.Instance.CurrentTime);
            }
        }

        /// <summary>
        /// ViewModel �̺�Ʈ ���� ����
        /// </summary>
        private void UnsubscribeFromViewModel()
        {
            if (TimeViewModel.Instance != null)
            {
                TimeViewModel.Instance.OnTimeUpdated -= UpdateTimeDisplay;
            }
        }

        /// <summary>
        /// �ð� ǥ�� ������Ʈ
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
        /// ViewModel�� �ʱ�ȭ�� ������ ���
        /// </summary>
        private System.Collections.IEnumerator WaitForViewModel()
        {
            while (TimeViewModel.Instance == null)
            {
                yield return null;
            }
            SubscribeToViewModel();
        }

        // ��Ÿ�ӿ��� ���� ���� ����
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