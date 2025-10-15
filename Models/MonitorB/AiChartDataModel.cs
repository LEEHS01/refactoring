using System;

namespace Models
{
    /// <summary>
    /// GET_CHARTVALUE 프로시저 결과 모델
    /// </summary>
    [Serializable]
    public class ChartDataModel
    {
        public string obsdt;      // 관측 시간
        public int? hnsidx;       // 센서 ID
        public int? obsidx;       // 관측소 ID
        public int? boardidx;     // 보드 ID

        public float val;         // 측정값
        public float aival;       // AI값
    }
}