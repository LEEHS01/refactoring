using System;

namespace Models.MonitorB
{
    [Serializable]
    public class ObservatoryModel
    {
        public string AREANM;        // 지역명
        public int AREAIDX;          // 지역 ID
        public string OBSNM;         // 관측소명
        public string AREATYPE;      // 지역 유형
        public int OBSIDX;           // 관측소 ID
        public string OUT_CCTVURL;   // 외부 CCTV URL
        public string IN_CCTVURL;    // 내부 CCTV URL
    }
}