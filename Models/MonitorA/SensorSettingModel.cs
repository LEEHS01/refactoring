using System;

namespace Models.MonitorA
{
    /// <summary>
    /// GET_SETTING 프로시저 반환 모델
    /// 센서 설정 정보
    /// </summary>
    [Serializable]
    public class SensorSettingModel
    {
        public int OBSIDX { get; set; }
        public int HNSIDX { get; set; }
        public int BOARDIDX { get; set; }
        public string HNSNM { get; set; }
        public float ALAHIVAL { get; set; }      // 경보값 (Warning)
        public float ALAHIHIVAL { get; set; }    // 심각값 (Serious)
        public string USEYN { get; set; }        // 사용 여부 ("0" or "1")
        public float ALAHIHISEC { get; set; }
        public string INSPECTIONFLAG { get; set; } // 고정 여부 ("0" or "1")
        public string UNIT { get; set; }

        public SensorSettingModel()
        {
            OBSIDX = 0;
            HNSIDX = 0;
            BOARDIDX = 0;
            HNSNM = "";
            ALAHIVAL = 0f;
            ALAHIHIVAL = 0f;
            USEYN = "0";
            ALAHIHISEC = 0f;
            INSPECTIONFLAG = "0";
            UNIT = "";
        }
    }
}