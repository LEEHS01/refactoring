using System;

namespace Models.MonitorB
{
    /// <summary>
    /// GET_SENSOR_INFO 프로시저 반환 모델
    /// </summary>
    [Serializable]
    public class SensorInfoModel
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

        public SensorInfoModel()
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
}