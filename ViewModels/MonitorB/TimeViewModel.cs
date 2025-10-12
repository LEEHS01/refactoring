using System;
using UnityEngine;

namespace ViewModels.Common
{
    /// <summary>
    /// 현재 시간을 관리하는 ViewModel (Singleton)
    /// </summary>
    public class TimeViewModel : MonoBehaviour
    {
        public static TimeViewModel Instance { get; private set; }

        // 현재 시간 (읽기 전용)
        public DateTime CurrentTime { get; private set; }

        // 시간이 업데이트될 때 발생하는 이벤트
        public event Action<DateTime> OnTimeUpdated;

        // 업데이트 주기 (초)
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

            // 초기 시간 설정
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
            // 설정된 주기마다 시간 업데이트
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateTime();
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// 현재 시간을 업데이트하고 이벤트 발생
        /// </summary>
        private void UpdateTime()
        {
            CurrentTime = DateTime.Now;
            OnTimeUpdated?.Invoke(CurrentTime);
        }

        /// <summary>
        /// 업데이트 주기 변경
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.1f, interval);
        }
    }
}