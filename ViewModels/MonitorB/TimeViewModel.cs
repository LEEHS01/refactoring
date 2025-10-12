using System;
using UnityEngine;

namespace ViewModels.Common
{
    /// <summary>
    /// ���� �ð��� �����ϴ� ViewModel (Singleton)
    /// </summary>
    public class TimeViewModel : MonoBehaviour
    {
        public static TimeViewModel Instance { get; private set; }

        // ���� �ð� (�б� ����)
        public DateTime CurrentTime { get; private set; }

        // �ð��� ������Ʈ�� �� �߻��ϴ� �̺�Ʈ
        public event Action<DateTime> OnTimeUpdated;

        // ������Ʈ �ֱ� (��)
        [SerializeField] private float updateInterval = 1.0f;
        private float lastUpdateTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // �ʱ� �ð� ����
            CurrentTime = DateTime.Now;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // ������ �ֱ⸶�� �ð� ������Ʈ
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateTime();
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// ���� �ð��� ������Ʈ�ϰ� �̺�Ʈ �߻�
        /// </summary>
        private void UpdateTime()
        {
            CurrentTime = DateTime.Now;
            OnTimeUpdated?.Invoke(CurrentTime);
        }

        /// <summary>
        /// ������Ʈ �ֱ� ����
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.1f, interval);
        }
    }
}