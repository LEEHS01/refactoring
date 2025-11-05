using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 월간 알람 통계 원본 데이터
    /// </summary>
    [Serializable]
    public class MonthlyAlarmStatData
    {
        public int ObservatoryIndex { get; set; }  // 관측소 인덱스
        public string AreaName { get; set; }       // 지역명
        public int AlarmCount { get; set; }        // 알람 발생 횟수
        public int Year { get; set; }              // 년도
        public int Month { get; set; }             // 월
    }

    /// <summary>
    /// 월간 알람 랭킹 데이터 (Top 5용)
    /// </summary>
    [Serializable]
    public class MonthlyAlarmRankingData
    {
        public int Rank { get; set; }
        public string AreaName { get; set; }
        public int AlarmCount { get; set; }
        public float Percentage { get; set; }
        public int ObservatoryIndex { get; set; }

        public MonthlyAlarmRankingData(int rank, string areaName, int alarmCount, float percentage, int obsIdx)
        {
            Rank = rank;
            AreaName = areaName;
            AlarmCount = alarmCount;
            Percentage = percentage;
            ObservatoryIndex = obsIdx;
        }
    }
}