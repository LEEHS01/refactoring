using System.Collections.Generic;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 지역 알람 차트 데이터 (ViewModel → View)
    /// </summary>
    public class AreaChartData
    {
        public string AreaName { get; set; }
        public List<string> ObservatoryNames { get; set; }  // 3개 관측소 이름
        public List<string> MonthLabels { get; set; }       // 12개월 라벨 (YY/MM)
        public int MaxAlarmCount { get; set; }              // Y축 최대값
        public List<MonthlyAlarmData> MonthlyData { get; set; } // 12개월 데이터

        public AreaChartData()
        {
            ObservatoryNames = new List<string>();
            MonthLabels = new List<string>();
            MonthlyData = new List<MonthlyAlarmData>();
        }
    }

    /// <summary>
    /// 월별 알람 데이터
    /// </summary>
    public class MonthlyAlarmData
    {
        public int Year { get; set; }
        public int Month { get; set; }

        // 각 관측소별 알람 수
        public int ObsA_Count { get; set; }
        public int ObsB_Count { get; set; }
        public int ObsC_Count { get; set; }

        // 정규화된 값 (0.0 ~ 1.0) - 차트 높이용
        public float ObsA_Normalized { get; set; }
        public float ObsB_Normalized { get; set; }
        public float ObsC_Normalized { get; set; }

        public MonthlyAlarmData()
        {
        }

        public MonthlyAlarmData(int year, int month)
        {
            Year = year;
            Month = month;
        }
    }
}