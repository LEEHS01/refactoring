using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 지역별 관측소 상태 집계 모델
    /// </summary>
    [Serializable]
    public class AreaObservatoryStatusData
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public AreaData.AreaType AreaType { get; set; }
        public int GreenCount { get; set; }      // 정상
        public int YellowCount { get; set; }     // 경계
        public int RedCount { get; set; }        // 경보
        public int PurpleCount { get; set; }     // 설비이상

        public AreaObservatoryStatusData()
        {
            AreaId = 0;
            AreaName = "";
            AreaType = AreaData.AreaType.Ocean;
            GreenCount = 0;
            YellowCount = 0;
            RedCount = 0;
            PurpleCount = 0;
        }
    }
}
