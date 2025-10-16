using System;
using Models.MonitorB;
using Repositories.MonitorB;
using UnityEngine;

namespace ViewModels.MonitorB
{
    public class AlarmDetailViewModel : MonoBehaviour
    {
        private static AlarmDetailViewModel _instance;
        public static AlarmDetailViewModel Instance => _instance;

        public event Action<AlarmDetailData> OnDataLoaded;
        public event Action<string> OnError;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public async void LoadAlarmDetail(
            int obsId,
            int alarmBoardId,
            int alarmHnsId,
            DateTime alarmTime,
            float? alarmCurrVal,
            string obsName,
            string areaName)
        {
            try
            {
                var data = await AlarmDetailRepository.Instance.GetAlarmDetailAsync(
                    obsId, alarmBoardId, alarmHnsId, alarmTime, alarmCurrVal, obsName, areaName);

                OnDataLoaded?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"AlarmDetailViewModel.LoadAlarmDetail 실패: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }
    }
}