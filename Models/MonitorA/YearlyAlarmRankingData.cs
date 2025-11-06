using System;

namespace HNS.MonitorA.Models
{
    [Serializable]
    public class YearlyAlarmStatData
    {
        public int ObservatoryIndex { get; set; }
        public string AreaName { get; set; }
        public int TotalCount { get; set; }  // 전체 알람 합계
        public int PurpleCount { get; set; } // ALA0 (설비이상)
        public int YellowCount { get; set; } // ALA1 (경계)
        public int RedCount { get; set; }    // ALA2 (경보)
        public int Year { get; set; }
    }

    [Serializable]
    public class YearlyAlarmRankingData
    {
        public int Rank { get; set; }
        public string AreaName { get; set; }
        public int TotalCount { get; set; }
        public int PurpleCount { get; set; }
        public int YellowCount { get; set; }
        public int RedCount { get; set; }
        public float Percentage { get; set; }
        public int ObservatoryIndex { get; set; }

        public YearlyAlarmRankingData(int rank, string areaName, int total, int purple, int yellow, int red, float percentage, int obsIdx)
        {
            Rank = rank;
            AreaName = areaName;
            TotalCount = total;
            PurpleCount = purple;
            YellowCount = yellow;
            RedCount = red;
            Percentage = percentage;
            ObservatoryIndex = obsIdx;
        }
    }
}