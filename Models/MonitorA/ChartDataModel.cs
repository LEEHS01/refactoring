using System;

namespace Models.MonitorA
{
    [Serializable]
    public class ChartDataModel
    {
        public string obsdt;      // yyyyMMddHHmmss
        public int? hnsidx;       // 센서 ID
        public int? obsidx;       // 관측소 ID
        public int? boardidx;     // 보드 ID
        public float val;         // 측정값
        public float aival;       // AI값
    }
}