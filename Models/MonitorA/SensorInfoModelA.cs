/*using System;

namespace Models.MonitorA
{
    /// <summary>
    /// GET_SENSOR_INFO 프로시저 결과 모델
    /// Monitor B와 동일한 구조 (데이터 일관성 보장)
    /// </summary>
    [Serializable]
    public class SensorInfoModelA
    {
        public int HNSIDX { get; set; }
        public int OBSIDX { get; set; }
        public int BOARDIDX { get; set; }
        public string HNSNM { get; set; }
        public string UNIT { get; set; }
        public float? VAL { get; set; }    
        public string USEYN { get; set; }
        public string INSPECTIONFLAG { get; set; }
        public float HIHI { get; set; }
        public float HI { get; set; }

        public SensorInfoModelA()
        {
            HNSIDX = 0;
            OBSIDX = 0;
            BOARDIDX = 0;
            HNSNM = "";
            UNIT = "";
            VAL = null;
            USEYN = "0";
            INSPECTIONFLAG = "0";
            HIHI = 0f;
            HI = 0f;
        }
    }
}*/