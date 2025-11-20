using Onthesys;
using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 시스템 설정 데이터
    /// </summary>
    [Serializable]
    public class SystemSettingData
    {
        public ToxinStatus AlarmThreshold { get; set; }
        public string DatabaseUrl { get; set; }

        public SystemSettingData()
        {
            AlarmThreshold = ToxinStatus.Yellow;
            DatabaseUrl = string.Empty;
        }

        public SystemSettingData(ToxinStatus alarmThreshold, string databaseUrl)
        {
            AlarmThreshold = alarmThreshold;
            DatabaseUrl = databaseUrl;
        }
    }
}