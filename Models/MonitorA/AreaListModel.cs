using System;

namespace Assets.Scripts_refactoring.Models.MonitorA
{
    /// <summary>
    /// 지역별 알람 현황 모델
    /// </summary>
    [Serializable]
    public class AreaListModel
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public int GreenCount { get; set; }
        public int YellowCount { get; set; }
        public int RedCount { get; set; }
        public int PurpleCount { get; set; }

        public AreaListModel()
        {
            AreaId = 0;
            AreaName = "";
            GreenCount = 0;
            YellowCount = 0;
            RedCount = 0;
            PurpleCount = 0;
        }
    }
}